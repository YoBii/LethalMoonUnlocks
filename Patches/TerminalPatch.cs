using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch {
        private static string buyMoon;
        private static int buyCredits;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void TerminalStartPatch(ref Terminal __instance) {
            Plugin.Instance.Mls.LogInfo($"Terminal is booting up!");
            Plugin.Instance.terminal = __instance;
            if (Plugin.Instance.IsServer()) {
                //NetworkManager.Instance.BackupOriginalPrices();
                //NetworkManager.Instance.ApplyDataFromSavefile();
                //NetworkManager.Instance.ServerSyncOriginalPrices();
                //NetworkManager.Instance.ServerSyncUnlockedMoons();
                UnlockManager.Instance.OnLobbyStart();
            } else {
                NetworkManager.Instance.ClientRequestSync();
            }
        }


        [HarmonyPatch("LoadNewNodeIfAffordable")]
        [HarmonyPrefix]
        private static void TerminalLoadNewNodeIfAffordablePrefix(TerminalNode node) {
            foreach (LMUnlockable unlock in UnlockManager.Instance.Unlocks) {
                if (unlock.ExtendedLevel.SelectableLevel.levelID == node.buyRerouteToMoon) {
                    buyMoon = unlock.ExtendedLevel.NumberlessPlanetName;
                    buyCredits = Plugin.Instance.terminal.groupCredits;
                    Plugin.Instance.Mls.LogInfo($"Routing to moon {buyMoon} with ID {node.buyRerouteToMoon}");
                    break;
                }
            }
        }

        [HarmonyPatch("LoadNewNodeIfAffordable")]
        [HarmonyPostfix]
        private static void TerminalLoadNewNodeIfAffordablePostfix() {
            if (buyCredits > Plugin.Instance.terminal.groupCredits) {
                int cost = buyCredits - Plugin.Instance.terminal.groupCredits;
                Plugin.Instance.Mls.LogInfo($"Successfully bought moon {buyMoon} for {cost} credits!");
                if (Plugin.Instance.IsServer()) {
                    UnlockManager.Instance.BuyMoon(buyMoon);
                    //NetworkManager.Instance.ServerUnlockMoon(buyMoon);
                } else {
                    //NetworkManager.Instance.ClientUnlockMoon(buyMoon);
                }
                buyMoon = null;
                buyCredits = 0;
            }

        }
    }
}
