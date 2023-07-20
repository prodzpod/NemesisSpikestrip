using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using NemesisSpikestrip.Changes;
using System.Collections.Generic;
using GrooveSaladSpikestripContent;

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
        public const string PluginVersion = "1.0.3";
        public static ManualLogSource Log;
        public static PluginInfo pluginInfo;
        public static Harmony Harmony;
        public static ConfigFile Config;
        public static Dictionary<string, string> SuperOverrides = new();

        public void Awake()
        {
            pluginInfo = Info;
            Log = Logger;
            Harmony = new Harmony(PluginGUID);
            Config = new ConfigFile(System.IO.Path.Combine(Paths.ConfigPath, PluginGUID + ".cfg"), true);

            Plated.Init();
            Warped.Init();
            Veiled.Init();
            Aragonite.Init();

            if (Chainloader.PluginInfos.ContainsKey("com.TPDespair.ZetAspects"))
            {
                Log.LogDebug("ZetAspect compat loaded :3");
                Harmony.PatchAll(typeof(PatchSuperOverrides));
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
