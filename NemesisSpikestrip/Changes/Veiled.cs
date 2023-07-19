using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PlasmaCoreSpikestripContent.Content.Elites;
using R2API;
using RoR2;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace NemesisSpikestrip.Changes
{
    public class Veiled
    {
        public static bool enabled;
        public static Material fakeCloakedMaterial;
        public static BuffDef Cooldown;
        public static ConfigEntry<bool> BetterVisibility;
        public static ConfigEntry<bool> HitToShow;
        public static ConfigEntry<float> VisibleTime;
        public static void Init()
        {
            enabled = Main.Config.Bind(nameof(Veiled), "Enabled", true, "").Value;
            BetterVisibility = Main.Config.Bind(nameof(Veiled), "Better Visibility", true, "make cloaked enemies easier to spot");
            HitToShow = Main.Config.Bind(nameof(Veiled), "Hit to Show Enemy", true, "hit enemy to make it visible");
            VisibleTime = Main.Config.Bind(nameof(Veiled), "Duration", 4f, "in seconds");
            LanguageAPI.AddOverlay($"EQUIPMENT_AFFIX{nameof(Veiled).ToUpper()}_DESCRIPTION", enabled
                ? $"You are <style=cIsUtility>cloaked</style>. Getting hit makes you decloak." // changed
                : $"Attacks <style=cIsUtility>cloak</style> you on hit."); // default
            if (!enabled) return;

            if (BetterVisibility.Value)
            {
                fakeCloakedMaterial = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matCloakedEffect.mat").WaitForCompletion());
                fakeCloakedMaterial.SetFloat("_Magnitude", 2.5f);
                Main.Harmony.PatchAll(typeof(PatchVisibility));
            }
            if (HitToShow.Value)
            {
                Cooldown = ScriptableObject.CreateInstance<BuffDef>();
                Cooldown.isCooldown = true;
                Cooldown.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffCloakIcon.tif").WaitForCompletion();
                Cooldown.buffColor = Color.gray;
                ContentAddition.AddBuffDef(Cooldown);

                On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, _victim) =>
                {
                    CharacterBody victim = _victim.GetComponent<CharacterBody>();
                    if (victim != null && victim.HasBuff(CloakedElite.instance.AffixBuff) && victim.HasBuff(RoR2Content.Buffs.Cloak))
                    {
                        victim.SetBuffCount(RoR2Content.Buffs.Cloak.buffIndex, 0);
                        victim.AddTimedBuff(Cooldown, VisibleTime.Value);
                    }
                };
                On.RoR2.CharacterBody.RemoveBuff_BuffIndex += (orig, self, idx) =>
                {
                    if (NetworkServer.active && idx == Cooldown.buffIndex)
                    {
                        self.AddBuff(RoR2Content.Buffs.Cloak);
                        EffectManager.SpawnEffect(CloakedElite.SmokebombEffect, new EffectData
                        {
                            origin = self.transform.position,
                            rotation = self.transform.rotation,
                            scale = self.bestFitRadius * 0.2f
                        }, transmit: true);
                    }
                };
                Main.Harmony.PatchAll(typeof(PatchCloak));
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.UpdateRendererMaterials))]
        public class PatchVisibility
        {
            public static void ILManipulator(ILContext il)
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, x => x.MatchLdsfld<CharacterModel>(nameof(CharacterModel.cloakedMaterial)));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Material, CharacterModel, Material>>((mat, self) =>
                {
                    if (self.body.HasBuff(CloakedElite.instance.AffixBuff)) return fakeCloakedMaterial;
                    return mat;
                });
            }
        }

        [HarmonyPatch(typeof(CloakedElite), nameof(CloakedElite.GlobalEventManager_OnHitEnemy))]
        public class PatchCloak
        {
            public static bool Prefix(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
            {
                orig(self, damageInfo, victim);
                if (damageInfo.procCoefficient > 0f && (bool)damageInfo.attacker)
                {
                    CharacterBody component = damageInfo.attacker.GetComponent<CharacterBody>();
                    if ((bool)component && !component.HasBuff(RoR2Content.Buffs.Cloak) && component.HasBuff(CloakedElite.instance.AffixBuff))
                    {
                        component.AddBuff(RoR2Content.Buffs.Cloak);
                        EffectManager.SpawnEffect(CloakedElite.SmokebombEffect, new EffectData
                        {
                            origin = damageInfo.attacker.transform.position,
                            rotation = damageInfo.attacker.transform.rotation,
                            scale = component.bestFitRadius * 0.2f
                        }, transmit: true);
                    }
                }
                return false;
            }
        }
    }
}
