using BepInEx.Configuration;
using GrooveSaladSpikestripContent.Content;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PlasmaCoreSpikestripContent.Content.Elites;
using R2API;
using RoR2;
using UnityEngine;

namespace NemesisSpikestrip.Changes
{
    public class Warped
    {
        public static bool enabled;
        public static ConfigEntry<float> Duration;
        public static ConfigEntry<float> BreakoutCoefficient;
        public static void Init()
        {
            enabled = Main.Config.Bind(nameof(Warped), "Enabled", true, "").Value;
            Duration = Main.Config.Bind(nameof(Warped), "Duration", 4f, "in seconds");
            BreakoutCoefficient = Main.Config.Bind(nameof(Warped), "Breakout Coefficient", 5f, "1 = 100% faster when standing completely still, hyperbolic");
            LanguageAPI.AddOverlay($"EQUIPMENT_AFFIX{nameof(Warped).ToUpper()}_DESCRIPTION", enabled
                ? $"Attacks <style=cIsUtility>levitate</style> on hit for <style=cIsUtility>{Duration.Value / BreakoutCoefficient.Value}s</style> or more." // changed
                : $"Attacks <style=cIsUtility>levitate</style> on hit for <style=cIsUtility>4s</style>."); // default
            if (!enabled || !Main.IsEnabled(WarpedElite.instance)) return;
            if (Duration.Value != 4f) Main.Harmony.PatchAll(typeof(PatchDuration));
            if (BreakoutCoefficient.Value > 0) Main.Harmony.PatchAll(typeof(PatchBreakout));
        }

        [HarmonyPatch(typeof(WarpedElite), nameof(WarpedElite.OnHitEnemyServer))]
        public class PatchDuration
        {
            public static void ILManipulator(ILContext il)
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, x => x.MatchLdcR4(4f));
                c.Emit(OpCodes.Pop);
                c.EmitDelegate(() => Duration.Value);
            }
        }

        [HarmonyPatch(typeof(WarpedElite.GravityBuffBehaviour), nameof(WarpedElite.GravityBuffBehaviour.ServerFixedUpdate))]
        public class PatchBreakout
        {
            public static void Postfix(WarpedElite.GravityBuffBehaviour __instance)
            {
                if (!(bool)__instance || !(bool)__instance.body) return;
                if ((bool)__instance.body.healthComponent)
                    __instance.body.healthComponent.TakeDamageForce(Vector3.up * Time.fixedDeltaTime * -__instance.body.corePosition.y);
                Vector3 velocity = ((bool)__instance.body.characterMotor) ? __instance.body.characterMotor.velocity : Vector3.zero;
                CharacterBody.TimedBuff gravityTimer = (__instance.body.timedBuffs != null) ? __instance.body.timedBuffs.Find(x => x.buffIndex == WarpedElite.gravityBuff.buffIndex) : null;
                if (gravityTimer != null && gravityTimer.timer > 0)
                {
                    gravityTimer.timer -= (Time.fixedDeltaTime * BreakoutCoefficient.Value / Mathf.Sqrt((velocity.x * velocity.x) + (velocity.z * velocity.z) + 1));
                    if (gravityTimer.timer < 0)
                    {
                        __instance.body.timedBuffs.Remove(gravityTimer);
                        __instance.body.RemoveBuff(WarpedElite.gravityBuff);
                    }
                }
            }
        }
    }
}
