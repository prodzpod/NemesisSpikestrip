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
            if (!enabled) return;
            LanguageAPI.AddOverlay("EQUIPMENT_AFFIXARAGONITE_NAME", "Anger from Below");
            Main.SuperOverrides.Add("AFFIX_ARAGONITE_NAME", "Anger from Below");
            if (IgnoreDrone.Value)
            {
                Main.Harmony.PatchAll(typeof(PatchIgnoreDrone));
                Main.Harmony.PatchAll(typeof(PatchIgnoreDrone2));
            }
        }

        [HarmonyPatch(typeof(RagingElite), nameof(RagingElite.GlobalEventManager_OnHitEnemy))]
        public class PatchIgnoreDrone
        {
            public static bool Prefix(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
            {
                if (victim?.GetComponent<CharacterBody>()?.master?.minionOwnership?.ownerMaster ?? false)
                {
                    orig(self, damageInfo, victim);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RageAffixBuffBehaviourServer), nameof(RageAffixBuffBehaviourServer.OnKilledServer))]
        public class PatchIgnoreDrone2
        {
            public static bool Prefix(DamageReport damageReport)
            {
                if (damageReport.attackerMaster?.minionOwnership?.ownerMaster ?? false) return false;
                return true;
            }
        }
    }

}
