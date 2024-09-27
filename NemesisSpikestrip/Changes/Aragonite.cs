using BepInEx.Configuration;
using HarmonyLib;
using PlasmaCoreSpikestripContent.Content.Elites;
using R2API;
using RoR2;
using UnityEngine;
using static PlasmaCoreSpikestripContent.Content.Elites.RagingElite;

namespace NemesisSpikestrip.Changes
{
    public class Aragonite
    {
        public static bool enabled;
        public static ConfigEntry<bool> IgnoreDrone;
        public static void Init()
        {
            enabled = Main.Config.Bind(nameof(Aragonite), "Enabled", true, "").Value;
            IgnoreDrone = Main.Config.Bind(nameof(Aragonite), "Ignore Followers", true, "Hitting followers such as drones does not trigger the special attack");
            LanguageAPI.AddOverlay($"EQUIPMENT_AFFIX{nameof(Aragonite).ToUpper()}_DESCRIPTION", "On hit, unleash a <style=cIsDamage>deadly wave</style> that deals <style=cIsDamage>500%</style> base damage."); // default
            if (!enabled || !Main.IsEnabled(RagingElite.instance)) return;
            LanguageAPI.AddOverlay("EQUIPMENT_AFFIXARAGONITE_NAME", "Anger from Below");
            Main.SuperOverrides.Add("AFFIX_ARAGONITE_NAME", "Anger from Below");
            if (IgnoreDrone.Value)
            {
                Main.Harmony.PatchAll(typeof(PatchIgnoreDrone));
                Main.Harmony.PatchAll(typeof(PatchIgnoreDrone2));
            }
        }

        [HarmonyPatch(typeof(RageAffixBuffBehaviourServer), nameof(RageAffixBuffBehaviourServer.OnTakeDamageServer))]
        public class PatchIgnoreDrone
        {
            public static bool Prefix(DamageReport damageReport)
            {
                return damageReport != null
                        && damageReport.damageInfo != null
                        && (bool)damageReport.damageInfo.attacker 
                        && (bool)damageReport.damageInfo.attacker.GetComponent<CharacterBody>() 
                        && (bool)damageReport.damageInfo.attacker.GetComponent<CharacterBody>().master 
                        && (bool)damageReport.damageInfo.attacker.GetComponent<CharacterBody>().master.minionOwnership 
                        && (bool)damageReport.damageInfo.attacker.GetComponent<CharacterBody>().master.minionOwnership.ownerMaster;
            }
        }

        [HarmonyPatch(typeof(RageAffixBuffBehaviourServer), nameof(RageAffixBuffBehaviourServer.OnKilledServer))]
        public class PatchIgnoreDrone2
        {
            public static bool Prefix(DamageReport damageReport)
            {
                return damageReport != null
                    && (bool)damageReport.attackerMaster
                    && (bool)damageReport.attackerMaster.minionOwnership
                    && (bool)damageReport.attackerMaster.minionOwnership.ownerMaster;
            }
        }
    }

}
