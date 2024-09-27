using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using NemesisSpikestrip.Changes;
using System.Collections.Generic;
using GrooveSaladSpikestripContent;
using UnityEngine;

namespace NemesisSpikestrip
{
    [BepInDependency("com.groovesalad.GrooveSaladSpikestripContent")]
    [BepInDependency("_com.groovesalad.GrooveUnsharedUtils")]
    [BepInDependency("com.plasmacore.PlasmaCoreSpikestripContent")]
    [BepInDependency("com.TPDespair.ZetAspects", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "prodzpod";
        public const string PluginName = "NemesisSpikestrip";
        public const string PluginVersion = "1.1.1";
        public static ManualLogSource Log;
        public static PluginInfo pluginInfo;
        public static Harmony Harmony;
        public static ConfigFile Config;
        public static ConfigEntry<float> SigmaLaserLength;
        public static ConfigEntry<float> SigmaLaserThickness;
        public static ConfigEntry<float> SigmaLaserDamage;
        public static ConfigEntry<float> LivelyPotTrailSize;
        public static ConfigEntry<float> LivelyPotTrailLife;
        public static ConfigEntry<float> LivelyPotTrailDamage;
        public static Dictionary<string, string> SuperOverrides = [];

        public void Awake()
        {
            pluginInfo = Info;
            Log = Logger;
            Harmony = new Harmony(PluginGUID);
            Config = new ConfigFile(System.IO.Path.Combine(Paths.ConfigPath, PluginGUID + ".cfg"), true);
            SigmaLaserLength = Config.Bind("Rebalance", "Sigma Construct Laser Length", 60f, "in meter");
            SigmaLaserThickness = Config.Bind("Rebalance", "Sigma Construct Laser Thickness", 1f, "in meter");
            SigmaLaserDamage = Config.Bind("Rebalance", "Sigma Construct Laser Damage", 1.2f, "1 = 100% coeff");
            LivelyPotTrailSize = Config.Bind("Rebalance", "Lively Pot Trail Size", 3f, "in meter");
            LivelyPotTrailLife = Config.Bind("Rebalance", "Lively Pot Trail Life", 3.5f, "in second");
            LivelyPotTrailDamage = Config.Bind("Rebalance", "Lively Pot Trail Damage Per Second", 1.5f, "1 = 100% coeff");

            Plated.Init();
            Warped.Init();
            Veiled.Init();
            Aragonite.Init();

            if (Chainloader.PluginInfos.ContainsKey("com.TPDespair.ZetAspects"))
            {
                Log.LogDebug("ZetAspect compat loaded :3");
                Harmony.PatchAll(typeof(PatchSuperOverrides));
            }

            PlasmaCoreSpikestripContent.Content.Monsters.States.SigmaBeam.maxRange = SigmaLaserLength.Value;
            PlasmaCoreSpikestripContent.Content.Monsters.States.SigmaBeam.width = SigmaLaserThickness.Value;
            PlasmaCoreSpikestripContent.Content.Monsters.States.SigmaBeam.damageCoefficient = SigmaLaserDamage.Value;
            Harmony.PatchAll(typeof(PatchPotMobile));
        }

        [HarmonyPatch(typeof(GrooveSaladSpikestripContent.Content.PotMobile.InstantiatePotMobileDamageTrail), nameof(GrooveSaladSpikestripContent.Content.PotMobile.InstantiatePotMobileDamageTrail.Start))]
        public class PatchPotMobile
        {
            public static void Postfix(GrooveSaladSpikestripContent.Content.PotMobile.InstantiatePotMobileDamageTrail __instance)
            {
                var trail = __instance.trailInstance;
                trail.pointLifetime *= LivelyPotTrailLife.Value / 9f;
                trail.radius *= LivelyPotTrailSize.Value / 2f;
                trail.damagePerSecond *= LivelyPotTrailDamage.Value / 1.5f;
                trail.segmentPrefab.GetComponent<ParticleSystemRenderer>().minParticleSize *= LivelyPotTrailSize.Value / 2f;
                trail.segmentPrefab.GetComponent<ParticleSystemRenderer>().maxParticleSize *= LivelyPotTrailSize.Value / 2f;
                var main = trail.segmentPrefab.GetComponent<ParticleSystem>().main;
                main.simulationSpeed /= (LivelyPotTrailLife.Value + 0.5f) / 9f;
            }
        }

        public static bool IsEnabled(SpikestripContentBase initialContent)
        {
            Base spikestrip = Chainloader.PluginInfos["com.groovesalad.GrooveSaladSpikestripContent"].Instance as Base;    
            bool ret = initialContent.IsEnabled && (initialContent.GetType().Assembly == typeof(Base).Assembly ?
                GrooveUnsharedUtils.Main.SpikestripContentLegacyConfig.Bind("Legacy " + spikestrip.SafeForConfig(initialContent.ConfigSection), "Enable " + spikestrip.SafeForConfig(initialContent.ConfigName), defaultValue: false, "Enable/Disable : " + spikestrip.SafeForConfig(initialContent.ConfigName)).Value
                : Base.SpikestripContentConfig.Bind(spikestrip.SafeForConfig(initialContent.ConfigSection), "Enable " + spikestrip.SafeForConfig(initialContent.ConfigName), defaultValue: true, "Enable/Disable : " + spikestrip.SafeForConfig(initialContent.ConfigName)).Value);
            Log.LogDebug(initialContent.GetType().Name + ": " + (ret ? "Loaded" : "Disabled"));
            return ret;
        }
    }
}
