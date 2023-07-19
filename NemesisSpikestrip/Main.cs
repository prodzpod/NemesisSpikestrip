using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using NemesisSpikestrip.Changes;
using System.Collections.Generic;

namespace NemesisSpikestrip
{
    [BepInDependency("com.groovesalad.GrooveSaladSpikestripContent")]
    [BepInDependency("com.plasmacore.PlasmaCoreSpikestripContent")]
    [BepInDependency("com.TPDespair.ZetAspects", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "prodzpod";
        public const string PluginName = "NemesisSpikestrip";
        public const string PluginVersion = "1.0.0";
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
    }
}
