using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch {
        private static string buyMoon = string.Empty;
        private static int buyCredits = 0;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void TerminalStartPatch(ref Terminal __instance) {
            Plugin.Instance.Mls.LogInfo($"Terminal is booting up!");
            UnlockManager.Instance.Terminal = __instance;
            if (NetworkManager.Instance.IsServer()) {
                UnlockManager.Instance.OnLobbyStart();
            } else {
                UnlockManager.Instance.InitializeUnlocks();
                NetworkManager.Instance.ClientRequestSync();
            }
        }


        [HarmonyPatch("LoadNewNodeIfAffordable")]
        [HarmonyPrefix]
        private static void TerminalLoadNewNodeIfAffordablePrefix(TerminalNode node) {
            foreach (LMUnlockable unlock in UnlockManager.Instance.Unlocks) {
                if (unlock.ExtendedLevel.SelectableLevel.levelID == node.buyRerouteToMoon) {
                    buyMoon = unlock.ExtendedLevel.NumberlessPlanetName;
                    buyCredits = UnlockManager.Instance.Terminal.groupCredits;
                    Plugin.Instance.Mls.LogInfo($"Routing to moon {buyMoon} with ID {node.buyRerouteToMoon}!");
                    break;
                }
            }
        }

        [HarmonyPatch("LoadNewNodeIfAffordable")]
        [HarmonyPostfix]
        private static void TerminalLoadNewNodeIfAffordablePostfix() {
            if (buyMoon != string.Empty) {
                if (buyCredits > UnlockManager.Instance.Terminal.groupCredits) {
                    int cost = buyCredits - UnlockManager.Instance.Terminal.groupCredits;
                    Plugin.Instance.Mls.LogInfo($"Route to {buyMoon} was paid ({cost} credits).");
                    if (NetworkManager.Instance.IsServer()) {
                        UnlockManager.Instance.BuyMoon(buyMoon);
                    } else {
                        NetworkManager.Instance.ClientBuyMoon(buyMoon);
                    }
                } else {
                    Plugin.Instance.Mls.LogInfo($"Route to {buyMoon} was free.");
                }
            }
            buyMoon = string.Empty;
            buyCredits = 0;
        }
    }
}
