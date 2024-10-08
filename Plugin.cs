﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalMoonUnlocks.Compatibility;
using LethalMoonUnlocks.Patches;
using LethalMoonUnlocks.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalMoonUnlocks
{
    [BepInPlugin("com.xmods.lethalmoonunlocks", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("imabatby.lethallevelloader", "1.3.10")]
    [BepInDependency("LethalNetworkAPI", "3.2.1")]
    [BepInDependency(LethalConstellations.Plugin.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(OpenLib.Plugin.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(WeatherTweaks.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

        internal static Plugin Instance {  get; private set; }
        internal static bool LQPresent = false;
        internal static bool LethalConstellationsPresent = false;
        internal static bool WeatherTweaksPresent = false;
        internal static LethalConstellationsExtension LethalConstellationsExtension { get; private set; }
        internal NetworkManager NetworkManager { get; private set; }
        internal UnlockManager UnlockManager { get; private set; }

        internal ManualLogSource Mls;
        private bool _loaded;

        private void Awake()
        {
            if (Instance == null) {
                Instance = this;
            }

            _harmony.PatchAll(typeof(Patches.GameNetworkManagerPatch));
            _harmony.PatchAll(typeof(Patches.RoundManagerPatch));
            _harmony.PatchAll(typeof(Patches.StartOfRoundPatch));
            _harmony.PatchAll(typeof(Patches.TerminalPatch));
            _harmony.PatchAll(typeof(Patches.TimeOfDayPatch));

            _harmony.PatchAll(typeof(Patches.HUDManagerPatch));

            Mls = BepInEx.Logging.Logger.CreateLogSource("LethalMoonUnlocks");

            if (!_loaded) Initialize();
        }

        public void Start()
        {
            if (!_loaded) Initialize();
        }

        public void OnDestroy()
        {
            if (!_loaded) Initialize();
        }

        public void Initialize()
        {
            GameObject delayHelper = new GameObject("DelayHelper");
            DontDestroyOnLoad(delayHelper);
            delayHelper.hideFlags = (HideFlags)61;
            delayHelper.AddComponent<DelayHelper>();

            SceneManager.sceneUnloaded += AfterGameInit;

            Mls.LogInfo($"LethalMoonUnlocks " + PluginInfo.PLUGIN_VERSION + " initialized!");
            _loaded = true;
        }

        private void AfterGameInit(Scene scene) {
            //Mls.LogInfo($"Scene name: {scene.name}");
            if (scene.name != "InitScene" && scene.name != "InitSceneLANMode") {
                return;
            }

            // Check for compatible mods
            Mls.LogInfo("Checking for compatible mods..");

            // print all plugin keys
            //Mls.LogFatal(string.Join(", ", BepInEx.Bootstrap.Chainloader.PluginInfos.Select(plugin => plugin.Key)));

            // LethalQuantities (risk level)
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LethalQuantities.PluginInfo.PLUGIN_GUID)) {
                Mls.LogInfo("Lethal Quantities found! Enabling compatibility..");
                LQPresent = true;
            }
            // Malfunctions
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.zealsprince.malfunctions")) {
                Mls.LogInfo("Malfunctions found! Enabling compatibility..");
                _harmony.PatchAll(typeof(MalfunctionsCompatibility));
            }
            // LethalConstellations
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LethalConstellations.Plugin.PluginInfo.PLUGIN_GUID)) {
                Mls.LogInfo("LethalConstellations found! Enabling compatibility..");
                LethalConstellationsPresent = true;
                LethalConstellationsExtension = new LethalConstellationsExtension();
            }
            // WeatherTweaks
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(WeatherTweaks.PluginInfo.PLUGIN_GUID)) {
                Mls.LogInfo("WeatherTweaks found! Enabling compatibility..");
                WeatherTweaksPresent = true;
                _harmony.PatchAll(typeof(WTCompatibility));
            }

            // Refresh config
            ConfigManager.RefreshConfig();
            
            // Patch Terminal scrolling
            if (ConfigManager.TerminalScrollAmount > 0) {
                Mls.LogInfo("TerminalScrollAmount is set to a positive value! Patching scroll amount..");
                _harmony.PatchAll(typeof(PlayerControllerBPatch));
            }
            
            // Create Managers
            NetworkManager = new NetworkManager();
            UnlockManager = new UnlockManager();

            // Unload this
            SceneManager.sceneUnloaded -= AfterGameInit;
        }

        public static float GetDiscountRate(int discount_number) {
            List<int> discountRates = new List<int>();
            foreach (var discount in ConfigManager.Discounts) {
                discountRates.Add(100 - Mathf.Clamp(discount, 0, 100));
            }
            if (discount_number > discountRates.Count) {
                discount_number = discountRates.Count;
            }    
            float rate = discountRates[discount_number - 1] / 100f;
            return rate;

        }
    }
}