using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TerminalStuff.Configs;
using TerminalStuff.MoonsTweaks;
using TerminalStuff.SpecialStuff;
using TerminalStuff.StoreTweaks;

namespace LethalMoonUnlocks.Compatibility {
    internal static class TerminalStuffCompatibility {
        private static readonly FieldInfo LevelField = AccessTools.Field(typeof(MoonInfo), "Level");
        private static readonly FieldInfo PurchaseNodeField = AccessTools.Field(typeof(MoonInfo), "PurchaseNode");
        private static readonly PropertyInfo DisplayPriceProperty = AccessTools.Property(typeof(MoonInfo), "DisplayPrice");

        internal static void OnUpdateMoonsDisplayed(List<MoonInfo> moons) {
            foreach (var moon in moons) {
                if (!ConfigManager.DisplayTerminalTags) {
                    moon.AdditionalInfo = string.Empty;
                    continue;
                }
                try {
                    var level = (SelectableLevel)LevelField.GetValue(moon);
                    var unlock =
                        UnlockManager.Instance.Unlocks.FirstOrDefault(x => x.ExtendedLevel.SelectableLevel == level);
                    if (unlock != null) {
                        moon.AdditionalInfo = unlock.BuildAdditionalInfoString();
                    }
                    else if (level.PlanetName.Contains("Gordion")) {
                        moon.AdditionalInfo = string.Empty + "\n";
                    }
                    else {
                        throw new Exception("TerminalStuffCompatibility: Moon not found in UnlockManager!");
                    }
                }
                catch (Exception ex) {
                    Logger.LogError("TerminalStuffCompatibility: Failed to get MoonInfo.Level!");
                    Logger.LogError(ex.Message);
                }
            }
        }

        private static int _groupCredits;
        private static bool _shouldHandleDirectMoonsPlusPurchase;
        private static int _selectedMoonDisplayPrice;
        [HarmonyPatch(typeof(MoonInfo), "SelectThisMoon")]
        [HarmonyPrefix]
        private static void SelectThisMoonPrefix(MoonInfo __instance) {
            _groupCredits = UnlockManager.Instance.Terminal.groupCredits;
            _selectedMoonDisplayPrice = 0;
            _shouldHandleDirectMoonsPlusPurchase = false;

            if (__instance == null || UnlockManager.Instance?.Terminal == null || StartOfRound.Instance == null) {
                return;
            }

            var level = (SelectableLevel)LevelField?.GetValue(__instance);
            if (level == null) {
                return;
            }

            int displayPrice = GetDisplayPrice(__instance);
            bool usesVanillaPurchaseNode = MoonsPlusConfig.UseVanillaPurchaseNodes.Value
                && PurchaseNodeField?.GetValue(__instance) is TerminalNode;

            _selectedMoonDisplayPrice = displayPrice;
            _shouldHandleDirectMoonsPlusPurchase =
                !StartOfRound.Instance.travellingToNewLevel
                && StartOfRound.Instance.inShipPhase
                && StartOfRound.Instance.currentLevel != level
                && displayPrice <= _groupCredits
                && !usesVanillaPurchaseNode;
        }
        
        [HarmonyPatch(typeof(MoonInfo), "SelectThisMoon")]
        [HarmonyPostfix]
        private static void SelectThisMoonPostfix(MoonInfo __instance) {
            if (!_shouldHandleDirectMoonsPlusPurchase) {
                return;
            }

            var level = (SelectableLevel) LevelField.GetValue(__instance);
            if (level == null) {
                Logger.LogWarning("TerminalStuffCompatibility: Failed to resolve selected moon level after MoonsPlus route.");
                return;
            }

            if (UnlockManager.Instance.Unlocks.FirstOrDefault(x => x.ExtendedLevel.SelectableLevel == level) is not { } unlock) {
                Logger.LogWarning($"TerminalStuffCompatibility: Failed to resolve LMUnlockable for MoonsPlus route '{level.PlanetName}'.");
                return;
            }

            if (_selectedMoonDisplayPrice <= 0) {
                Logger.LogInfo($"Route to {unlock.ExtendedLevel.SelectableLevel.PlanetName} was free (routed via MoonsPlus).");
                return;
            }

            Logger.LogInfo($"Route to {unlock.ExtendedLevel.SelectableLevel.PlanetName} was paid ({_selectedMoonDisplayPrice} credits) (routed via MoonsPlus).");

            if (NetworkManager.Instance.IsServer()) {
                UnlockManager.Instance.BuyMoon(unlock.Name);
            } else {
                NetworkManager.Instance.ClientBuyMoon(unlock.Name);
            }
        }

        private static int GetDisplayPrice(MoonInfo moonInfo) {
            if (moonInfo == null || DisplayPriceProperty == null) {
                return 0;
            }

            try {
                object rawValue = DisplayPriceProperty.GetValue(moonInfo);
                return rawValue is int displayPrice ? displayPrice : 0;
            } catch (Exception ex) {
                Logger.LogWarning($"TerminalStuffCompatibility: Failed to resolve DisplayPrice for MoonsPlus route. {ex.Message}");
                return 0;
            }
        }
    }
}
