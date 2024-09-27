using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PlasmaCoreSpikestripContent.Content.Elites;
using R2API;
using RoR2;
using System;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.DirectorAPI.Helpers;

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
            if (!enabled || !Main.IsEnabled(CloakedElite.instance)) return;

            const SpawnCard.EliteRules NOVEILED = (SpawnCard.EliteRules)339001;
            DirectorAPI.MonsterActions += (dccsPool, mixEnemyArtifactMonsters, currentStage) =>
            {
                if (dccsPool) ForEachPoolEntryInDccsPool(dccsPool, (poolEntry) =>
                {
                    foreach (var category in poolEntry.dccs.categories)
                        foreach (var card in category.cards)
                            if (card.spawnCard.name.Contains("Assassin2"))
                                ((CharacterSpawnCard)card.spawnCard).eliteRules = NOVEILED; // some random number
                });
            };
            On.RoR2.CombatDirector.EliteTierDef.CanSelect += (orig, self, rules) =>
            {
                if (rules == NOVEILED) return orig(self, SpawnCard.EliteRules.Default);
                return orig(self, rules);
            };
            On.RoR2.CombatDirector.CalcHighestEliteCostMultiplier += (orig, rules) =>
            {
                if (rules == NOVEILED) return orig(SpawnCard.EliteRules.Default);
                return orig(rules);
            };
            On.RoR2.CombatDirector.ResetEliteType += (orig, self) =>
            {
                orig(self);
                if (self.currentMonsterCard.spawnCard.eliteRules == NOVEILED)
                    self.currentActiveEliteDef = self.rng.NextElementUniform(self.currentActiveEliteTier.eliteTypes.Where(x => (bool)x && x.IsAvailable() && !x.name.Contains("Cloaked")).ToList());
            };
            IL.RoR2.CombatDirector.PrepareNewMonsterWave += (il) =>
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchStfld<CombatDirector>(nameof(CombatDirector.currentActiveEliteDef)));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<EliteDef, CombatDirector, EliteDef>>((orig, self) =>
                {
                    if (self.currentMonsterCard.spawnCard.eliteRules == NOVEILED) return self.rng.NextElementUniform(self.currentActiveEliteTier.eliteTypes.Where(x => (bool)x && x.IsAvailable() && !x.name.Contains("Cloaked")).ToList());
                    return orig;
                });
            };

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
                Cooldown.isDebuff = true;
                Cooldown.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffCloakIcon.tif").WaitForCompletion();
                Cooldown.buffColor = Color.gray;
                ContentAddition.AddBuffDef(Cooldown);

                On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, _victim) =>
                {
                    CharacterBody victim = _victim.GetComponent<CharacterBody>();
                    if ((bool)victim && victim.HasBuff(CloakedElite.instance.AffixBuff) && victim.HasBuff(RoR2Content.Buffs.Cloak))
                    {
                        victim.SetBuffCount(RoR2Content.Buffs.Cloak.buffIndex, 0);
                        victim.AddTimedBuff(Cooldown, VisibleTime.Value);
                        victim.outOfCombatStopwatch = 0;
                    }
                    orig(self, damageInfo, _victim);
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
                    orig(self, idx);
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
