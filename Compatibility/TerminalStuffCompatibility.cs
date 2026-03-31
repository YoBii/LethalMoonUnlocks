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
        internal static void OnUpdateMoonsDisplayed(List<MoonInfo> moons) {
            var levelField = AccessTools.Field(typeof(MoonInfo), "Level");

            foreach (var moon in moons) {
                if (!ConfigManager.DisplayTerminalTags) {
                    moon.AdditionalInfo = string.Empty;
                    continue;
                }
                try {
                    var level = (SelectableLevel)levelField.GetValue(moon);
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
        [HarmonyPatch(typeof(MoonInfo), "SelectThisMoon")]
        [HarmonyPrefix]
        private static void SelectThisMoonPrefix(MoonInfo __instance) {
            _groupCredits = UnlockManager.Instance.Terminal.groupCredits;
        }
        
        [HarmonyPatch(typeof(MoonInfo), "SelectThisMoon")]
        [HarmonyPostfix]
        private static void SelectThisMoonPostfix(MoonInfo __instance) {
            if (_groupCredits > UnlockManager.Instance.Terminal.groupCredits) {
                var pricePaid = _groupCredits - UnlockManager.Instance.Terminal.groupCredits;
                var levelField = AccessTools.Field(typeof(MoonInfo), "Level");
                var level = (SelectableLevel) levelField.GetValue(__instance);
                
                Logger.LogInfo($"Route to {level.PlanetName} was paid ({pricePaid} credits) (routed via MoonsPlus).");

                if (UnlockManager.Instance.Unlocks.FirstOrDefault(x => x.ExtendedLevel.SelectableLevel == level) is
                    { } unlock) {
                    if (NetworkManager.Instance.IsServer()) {
                        UnlockManager.Instance.BuyMoon(unlock.Name);
                    } else {
                        NetworkManager.Instance.ClientBuyMoon(unlock.Name);
                    }
                }
            }
        }
    }
}
