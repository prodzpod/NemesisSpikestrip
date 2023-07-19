using BepInEx.Configuration;
using GrooveSaladSpikestripContent;
using GrooveSaladSpikestripContent.Content;
using HarmonyLib;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static GrooveSaladSpikestripContent.Content.PlatedElite;

namespace NemesisSpikestrip.Changes
{
    public class Plated
    {
        public static bool enabled;
        public static BuffDef Stack;
        public static BuffDef OnDeath;
        public static ConfigEntry<int> Hits;
        public static ConfigEntry<float> MaxHPPenalty;
        public static ConfigEntry<float> OnDeathRange;
        public static ConfigEntry<float> OnDeathArmor;
        public static ConfigEntry<float> OnDeathDuration;
        public static GameObject newPlatedBlockEffectPrefab;
        public static void Init()
        {
            enabled = Main.Config.Bind(nameof(Plated), "Enabled", true, "").Value;
            Hits = Main.Config.Bind(nameof(Plated), "Hits", 6, "Every n hit goes through immunity");
            MaxHPPenalty = Main.Config.Bind(nameof(Plated), "HP Penalty", 0.2f, "multiplied to max hp");
            OnDeathRange = Main.Config.Bind(nameof(Plated), "On Death Range", 13f, "in meters");
            OnDeathArmor = Main.Config.Bind(nameof(Plated), "On Death Armor", 30f, "default: 1 ruckler");
            OnDeathDuration = Main.Config.Bind(nameof(Plated), "On Death Duration", 8f, "in seconds");
            LanguageAPI.AddOverlay($"EQUIPMENT_AFFIX{nameof(Plated).ToUpper()}_DESCRIPTION", enabled
                ? $"All but every {Hits.Value} hit is mitigated. Attacks <style=cIsUtility>stifle</style> on hit for <style=cIsUtility>8s</style>, reducing damage dealt by <style=cIsUtility>100%</style> base damage per stack." // changed
                : $"Gain defensive plating that mitigates heavy damage. Attacks <style=cIsUtility>stifle</style> on hit for <style=cIsUtility>8s</style>, reducing damage dealt by <style=cIsUtility>100%</style> base damage per stack." ); // default
            if (!enabled) return;
            if (Hits.Value > 0)
            {
                Main.SuperOverrides.Add("PASSIVE_DEFENSE_PLATING", $"All but every {Hits.Value} hit is mitigated.");
                Stack = ScriptableObject.CreateInstance<BuffDef>();
                Stack.canStack = true;
                Stack.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion();
                Stack.buffColor = new Color(155f / 255f, 144f / 255f, 122f / 255f);
                ContentAddition.AddBuffDef(Stack);

                Main.Harmony.PatchAll(typeof(PatchHits));
                Main.Harmony.PatchAll(typeof(PatchHits2));
                On.RoR2.UI.HealthBar.UpdateHealthbar -= PlatedElite.instance.HealthBar_UpdateHealthbar;
                newPlatedBlockEffectPrefab = platedBlockEffectPrefab.InstantiateClone("PlatingBlockEffect 2");
                newPlatedBlockEffectPrefab.GetComponent<EffectComponent>().soundName = "Play_item_proc_crowbar";
                SpikestripContentBase.effectDefContent.Add(new EffectDef(newPlatedBlockEffectPrefab));

                On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
                {
                    CharacterBody victim = self?.body;
                    if (!damageInfo.rejected && damageInfo.procCoefficient > 0f && victim != null && victim.HasBuff(PlatedElite.instance.AffixBuff))
                    {
                        if (victim.HasBuff(Stack))
                        {
                            damageInfo.rejected = true;
                            EffectData effectData = new()
                            {
                                origin = damageInfo.position,
                                rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : Random.onUnitSphere)
                            };
                            EffectManager.SpawnEffect(newPlatedBlockEffectPrefab, effectData, transmit: true);
                            victim.AddBuff(Stack);
                            if (victim.GetBuffCount(Stack) >= Hits.Value) victim.SetBuffCount(Stack.buffIndex, 0);
                        }
                        else victim.AddBuff(Stack);
                    }
                    orig(self, damageInfo);
                };
            }
            if (MaxHPPenalty.Value != 1) RecalculateStatsAPI.GetStatCoefficients += (self, args) => { if (self.HasBuff(PlatedElite.instance.AffixBuff)) args.healthMultAdd += MaxHPPenalty.Value - 1; };
            if (OnDeathRange.Value > 0)
            {
                OnDeath = ScriptableObject.CreateInstance<BuffDef>();
                OnDeath.canStack = false;
                OnDeath.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Junk/Common/texBuffBodyArmorIcon.tif").WaitForCompletion();
                OnDeath.buffColor = new Color(155f / 255f, 144f / 255f, 122f / 255f);
                ContentAddition.AddBuffDef(OnDeath);
                On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, damageReport) =>
                {
                    orig(self, damageReport);
                    if (damageReport.victimBody?.HasBuff(PlatedElite.instance.AffixBuff) ?? false)
                    {
                        SphereSearch sphereSearch = new()
                        {
                            radius = OnDeathRange.Value + damageReport.victimBody.radius,
                            queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                            mask = LayerIndex.entityPrecise.mask,
                            origin = damageReport.victimBody.corePosition
                        };
                        sphereSearch.RefreshCandidates();
                        sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                        TeamMask mask = default; mask.AddTeam(damageReport.victimBody.teamComponent.teamIndex);
                        sphereSearch.FilterCandidatesByHurtBoxTeam(mask);
                        sphereSearch.GetHurtBoxes().Do(hurtBox => { if (hurtBox?.healthComponent?.body != damageReport.victimBody) hurtBox?.healthComponent?.body?.AddBuff(OnDeath); });
                    }
                };
                RecalculateStatsAPI.GetStatCoefficients += (self, args) => { if (self.HasBuff(OnDeath)) args.armorAdd += OnDeathArmor.Value; };
            }
        }
        [HarmonyPatch(typeof(PlatedAffixBuffBehaviour), nameof(PlatedAffixBuffBehaviour.OnTakeDamageServer))]
        public class PatchHits { public static bool Prefix() { return false; } }
        [HarmonyPatch(typeof(PlatedAffixBuffBehaviour), nameof(PlatedAffixBuffBehaviour.OnIncomingDamageServer))]
        public class PatchHits2 { public static bool Prefix() { return false; } }
    }

}
