using BepInEx;
using BepInEx.Logging;
using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMoonUnlocks.Compatibility;
using LethalMoonUnlocks.Patches;
using LethalMoonUnlocks.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using TerminalStuff.MoonsTweaks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LethalMoonUnlocks
{
    [BepInPlugin(PluginMetadata.PLUGIN_GUID, PluginMetadata.PLUGIN_NAME, PluginMetadata.PLUGIN_VERSION)]
    [BepInDependency("imabatby.lethallevelloader", "1.4.11")]
    [BepInDependency("LethalNetworkAPI", "3.3.2")]
    [BepInDependency(LethalConstellations.Plugin.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(TerminalStuff.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(OpenLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(WeatherTweaks.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(DawnLib.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

        internal static Plugin Instance { get; private set; }
        internal static bool LQPresent = false;
        internal static bool LethalConstellationsPresent = false;
        internal static bool DarmuhsTerminalStuffPresent = false;
        internal static bool WeatherTweaksPresent = false;
        internal static bool DawnLibPresent = false; 
        internal static LethalConstellationsExtension LethalConstellationsExtension { get; private set; }
        internal NetworkManager NetworkManager { get; private set; }
        internal UnlockManager UnlockManager { get; private set; }
        internal ProgressionManager ProgressionManager { get; private set; }

        private bool _loaded;

        private void Awake()
        {
            if (Instance == null) {
                Instance = this;
            }
            ManualLogSource Mls = BepInEx.Logging.Logger.CreateLogSource("LethalMoonUnlocks");
            LethalMoonUnlocks.Logger.Initialize(Mls);

            Logger.LogInfo("Hello world (:"); 
            Logger.LogInfo("Applying patches.."); 

            _harmony.PatchAll(typeof(Patches.GameNetworkManagerPatch));
            _harmony.PatchAll(typeof(Patches.RoundManagerPatch));
            _harmony.PatchAll(typeof(Patches.StartOfRoundPatch));
            _harmony.PatchAll(typeof(Patches.TerminalPatch));
            _harmony.PatchAll(typeof(Patches.TimeOfDayPatch));
            _harmony.PatchAll(typeof(Patches.HUDManagerPatch));
            _harmony.PatchAll(typeof(Patches.LLLSaveManagerInitPatch));
            _harmony.PatchAll(typeof(Patches.DepositItemsDeskPatch));
            
            Logger.LogInfo("Patching complete.");
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
            Logger.LogInfo("Initializing.."); 

            GameObject delayHelper = new GameObject("DelayHelper");
            DontDestroyOnLoad(delayHelper);
            delayHelper.hideFlags = (HideFlags)61;
            delayHelper.AddComponent<DelayHelper>();

            SceneManager.sceneUnloaded += AfterGameInit;

            ConfigManager.Initialize(Config);

            Logger.LogInfo($"LethalMoonUnlocks " + PluginInfo.PLUGIN_VERSION + " initialized!");
            _loaded = true;
        }

        private void AfterGameInit(Scene scene) {
            //Mls.LogInfo($"Scene name: {scene.name}");
            if (scene.name != "InitScene" && scene.name != "InitSceneLANMode") {
                return;
            }

            // Check for compatible mods
            Logger.LogInfo("Checking for compatible mods..");

            // print all plugin keys
            //Mls.LogFatal(string.Join(", ", BepInEx.Bootstrap.Chainloader.PluginInfos.Select(plugin => plugin.Key)));

            // LethalQuantities (risk level)
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LethalQuantities.PluginInfo.PLUGIN_GUID)) {
                Logger.LogInfo("Lethal Quantities found! Enabling compatibility..");
                LQPresent = true;
            }
            // Malfunctions
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.zealsprince.malfunctions")) {
                Logger.LogInfo("Malfunctions found! Enabling compatibility..");
                _harmony.PatchAll(typeof(MalfunctionsCompatibility));
            }
            // LethalConstellations
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LethalConstellations.Plugin.PluginInfo.PLUGIN_GUID)) {
                Logger.LogInfo("LethalConstellations found! Enabling compatibility..");
                LoadLethalConstellationsExtension();
                LethalConstellationsPresent = true;
            }
            // darmuhsTerminalStuff (MoonsPlus)
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("darmuh.TerminalStuff")) {
                Logger.LogInfo("darmuhsTerminalStuff found! Enabling compatibility..");
                DarmuhsTerminalStuffPresent = true;
                RegisterTerminalStuffEvent();
                _harmony.PatchAll(typeof(TerminalStuffCompatibility));
            }
            // WeatherTweaks
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(WeatherTweaks.PluginInfo.PLUGIN_GUID)) {
                Logger.LogInfo("WeatherTweaks found! Enabling compatibility..");
                WeatherTweaksPresent = true;
                _harmony.PatchAll(typeof(WTCompatibility));
            }
            // DawnLib
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(DawnLib.PLUGIN_GUID)) {
                Logger.LogInfo("DawnLib found! Enabling compatibility..");
                DawnLibPresent = true;
                _harmony.PatchAll(typeof(DawnLibMoonCataloguePatch));
                RegisterDawnLibEvent();
            }

            // Refresh config
            ConfigManager.RefreshConfig();
            
            // Patch Terminal scrolling
            if (ConfigManager.TerminalScrollAmount > 0) {
                Logger.LogInfo("TerminalScrollAmount is set to a positive value! Patching scroll amount..");
                _harmony.PatchAll(typeof(PlayerControllerBPatch));

                Logger.LogInfo("Unpatching other terminal scroll..");
                var methodInfo = typeof(PlayerControllerB).GetMethod(nameof(PlayerControllerB.ScrollMouse_performed), BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(InputAction.CallbackContext) }, null);
                Logger.LogInfo(methodInfo.Name);
                _harmony.Unpatch(methodInfo, HarmonyPatchType.Prefix, "imabatby.lethallevelloader");
            }
            
            // Create Managers
            NetworkManager = new NetworkManager();
            UnlockManager = new UnlockManager();
            ProgressionManager = new ProgressionManager();

            // Unload this
            SceneManager.sceneUnloaded -= AfterGameInit;
        }

        private void RegisterDawnLibEvent() {
            LethalContent.Moons.OnFreeze += () => UnlockManager.InitializeUnlocksDawnLib();
        }

        private void RegisterTerminalStuffEvent() {
            MoonsPlus.UpdateMoonsDisplayed.AddListener(TerminalStuffCompatibility.OnUpdateMoonsDisplayed);
        }

        private void LoadLethalConstellationsExtension() {
            try {
                LethalConstellationsExtension = new LethalConstellationsExtension();
            } catch (Exception ex) {
                Logger.LogError($"Failed to load LethalConstellations compatibility due to {ex}");
                LethalConstellationsExtension = null;
            }
        }

        internal static float GetDiscountRate(int discount_number) {
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
