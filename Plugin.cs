using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalMoonUnlocks
{
    [BepInPlugin("com.xmods.lethalmoonunlocks", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("imabatby.lethallevelloader")]
    [BepInDependency("LethalNetworkAPI")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

        public static Plugin Instance {  get; private set; }

        private UnlockManager UnlockManager { get; set; }

        public Terminal terminal;

        private bool _loaded;
        public ManualLogSource Mls;

        //config values
        public static bool ResetWhenFired { get; set; }
        public static bool QuotaUnlocks { get; set; }
        public static bool DiscountMode { get; set; }
        public static int QuotaUnlocksMaxPrice { get; set; }
        public static int QuotaUnlockMaxCount { get; set; }
        public static string Discounts { get; set; }


        private void Awake()
        {
            if (Instance == null) {
                Instance = this;
            }

            _harmony.PatchAll(typeof(Plugin));
            _harmony.PatchAll(typeof(Patches));

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
            UnlockManager = new UnlockManager();

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

            // Unload this
            SceneManager.sceneUnloaded -= AfterGameInit;
        }

        public bool IsServer() {
            return NetworkManager.Singleton.IsServer;
        }
        public static float GetDiscountRate(int discount_number) {
            List<int> discountRates = new List<int>();
            foreach (var discount in Discounts.Split(",")) {
                discountRates.Add(100 - Mathf.Clamp(int.Parse(discount.Trim()), 0, 100));
            }
            if (discount_number > discountRates.Count) {
                discount_number = discountRates.Count;
            }    
            float rate = discountRates[discount_number - 1] / 100f;
            return rate;

        }
    }
}