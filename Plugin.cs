using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalConstellations.PluginCore;
using LethalMoonUnlocks.Compatibility;
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
    [BepInDependency("imabatby.lethallevelloader", "1.3.8")]
    [BepInDependency("LethalNetworkAPI", "3.1.1")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

        internal static Plugin Instance {  get; private set; }
        internal static bool LQPresent = false;
        internal static bool LethalConstellationsPresent = false;
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

            // Refresh config
            ConfigManager.RefreshConfig();
            
            // Create Managers
            NetworkManager = new NetworkManager();
            UnlockManager = new UnlockManager();

            // print all plugin keys
            //Mls.LogFatal(string.Join(", ", BepInEx.Bootstrap.Chainloader.PluginInfos.Select(plugin => plugin.Key)));

            // Check for compatible mods
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
            
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LethalConstellations.Plugin.PluginInfo.PLUGIN_GUID)) {
                LethalConstellationsPresent = true;
                _harmony.PatchAll(typeof(Patches.LethalConstellationsPatch));
            }

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