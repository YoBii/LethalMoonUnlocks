using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalMoonUnlocks {
    internal class Patches {
        private static string buyMoon;
        private static int buyCredits;

        [HarmonyPatch(typeof(Terminal), "Start")]
        [HarmonyPostfix]
        private static void TerminalStartPatch(ref Terminal __instance) {
            Plugin.Instance.Mls.LogInfo($"Terminal is booting up!");
            Plugin.Instance.terminal = __instance;
            if (Plugin.Instance.IsServer()) {
                UnlockManager.Instance.ApplyDataFromSavefile();
                UnlockManager.Instance.ServerSyncData();
            } else {
                UnlockManager.Instance.ClientRequestSync();
            }
        }

        [HarmonyPatch(typeof(Terminal), "LoadNewNodeIfAffordable")]
        [HarmonyPrefix]
        private static void TerminalLoadNewNodeIfAffordablePrefix(TerminalNode node) {
            List<ExtendedLevel> LLLMoons = PatchedContent.ExtendedLevels;
            foreach (ExtendedLevel level in LLLMoons) {
                if (level.SelectableLevel.levelID == node.buyRerouteToMoon) {
                    buyMoon = level.NumberlessPlanetName;
                    buyCredits = Plugin.Instance.terminal.groupCredits;
                    Plugin.Instance.Mls.LogInfo($"Attempting to buy moon {buyMoon} (ID {node.buyRerouteToMoon})");
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), "LoadNewNodeIfAffordable")]
        [HarmonyPostfix]
        private static void TerminalLoadNewNodeIfAffordablePostfix() {
            if (buyCredits > Plugin.Instance.terminal.groupCredits) {
                int cost = buyCredits - Plugin.Instance.terminal.groupCredits;
                Plugin.Instance.Mls.LogInfo($"Successfully bought moon {buyMoon} for {cost} credits!");
                if (Plugin.Instance.IsServer()) {
                    UnlockManager.Instance.ServerUnlockMoon(buyMoon, cost);
                } else {
                    UnlockManager.Instance.ClientUnlockMoon(buyMoon, cost);
                }
                buyMoon = null;
                buyCredits = 0;
            }

        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetNewProfitQuota))]
        [HarmonyPrefix]
        private static void SetNewProfitQuotaPatch() {
            if (Plugin.Instance.IsServer()) {
                UnlockManager.Instance.QuotaCount++;
                Plugin.Instance.Mls.LogInfo($"New quota! Increase quota count to: {UnlockManager.Instance.QuotaCount}");
                UnlockManager.Instance.ServerUnlockMoonNewQuota();
            }
        }

        //[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShip))]
        //[HarmonyPostfix]
        //private static void ResetShipPatch() {
        //    Plugin.Instance.Mls.LogInfo($"ResetShipPatch..");
        //    if (!Plugin.Instance.keepUnlocks) {
        //        Plugin.Instance.ResetMoons();
        //    }
        //}

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        private static void DisconnectPatch() {
            Plugin.Instance.Mls.LogInfo($"Disconnecting from lobby. Clearing variables..");
            UnlockManager.Instance.UnlockedMoons.Clear();
            UnlockManager.Instance.OriginalPrices.Clear();
            UnlockManager.Instance.QuotaCount = 0;
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveGame))]
        [HarmonyPostfix]
        private static void SaveGameValuesPatch() {
            if (!Plugin.Instance.IsServer()) {
                return;
            }
            try {
                Plugin.Instance.Mls.LogInfo($"Host is saving game.. Saving our data to savefile.");
                SaveManager.StoreSaveData();
            } catch (Exception e) {
                Plugin.Instance.Mls.LogError($"Failed to save unlock data: {e}");
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ResetSavedGameValues))]
        [HarmonyPostfix]
        private static void ResetSavedGameValuesPatch() {
            Plugin.Instance.Mls.LogInfo($"You are fired!");
            if (Plugin.Instance.IsServer() && Plugin.ResetWhenFired) {
                Plugin.Instance.Mls.LogInfo($"Resetting..");
                UnlockManager.Instance.ServerSendResetMoonsEvent();
            }
        }
    }
}
