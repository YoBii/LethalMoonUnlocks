using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dawn;
using Dawn.Utils;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch {
        private static string buyMoon = string.Empty;
        private static int buyCredits = 0;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void TerminalStartPatch(ref Terminal __instance) {
            Logger.LogInfo($"Terminal is booting up!");
            UnlockManager.Instance.Terminal = __instance;
            if (NetworkManager.Instance.IsServer()) {
                UnlockManager.Instance.OnLobbyStart();
            } else {
                ConfigManager.RefreshConfig();
                UnlockManager.Instance.InitializeUnlocks();
                NetworkManager.Instance.ClientRequestSync();
            }
        }


        [HarmonyPatch("LoadNewNodeIfAffordable")]
        [HarmonyPrefix]
        private static void TerminalLoadNewNodeIfAffordablePrefix(TerminalNode node) {
            if (node is null) {
                Logger.LogFatal("Terminal node in Terminal.LoadNewNodeIfAffordable is null!");
                return;
            }
            Logger.LogDebug($"Loading new terminal node! Name: {node.name}, ID: {node.buyRerouteToMoon}");
            foreach (LMUnlockable unlock in UnlockManager.Instance.Unlocks) {
                if (unlock.ExtendedLevel is null) {
                    Logger.LogWarning($"LMUnlockable {unlock.Name} has no ExtendedLevel! Skipping..");
                    continue;
                }
                Logger.LogDebug($"Checking node against moon {unlock.ExtendedLevel.NumberlessPlanetName} with ID {unlock.ExtendedLevel.SelectableLevel.levelID}");
                if (unlock.ExtendedLevel.SelectableLevel.levelID == node.buyRerouteToMoon) {
                    buyMoon = unlock.ExtendedLevel.NumberlessPlanetName;
                    buyCredits = UnlockManager.Instance.Terminal.groupCredits;
                    Logger.LogInfo($"Routing to moon {buyMoon} with ID {node.buyRerouteToMoon}!");
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
                    Logger.LogInfo($"Route to {buyMoon} was paid ({cost} credits).");
                    if (NetworkManager.Instance.IsServer()) {
                        UnlockManager.Instance.BuyMoon(buyMoon);
                    } else {
                        NetworkManager.Instance.ClientBuyMoon(buyMoon);
                    }
                } else {
                    Logger.LogInfo($"Route to {buyMoon} was free.");
                }
            }
            buyMoon = string.Empty;
            buyCredits = 0;
        }
        
        [HarmonyPatch("AttemptLoadCreatureFileNode")]
        [HarmonyPrefix]
        private static void AttemptLoadCreatureFileNodePrefix(TerminalNode node) {
            Logger.LogDebug($"Loading bestiary node! Name: {node.creatureName}, FileID: {node.creatureFileID}");
            if (node.creatureName == "Old birds" && UnlockManager.Instance.Terminal.newlyScannedEnemyIDs.Contains(
                                                     node.creatureFileID)) {
                UnlockManager.TryReleaseStoryLockShowAlert("Embrion");
            }
        }

        [HarmonyPatch("AttemptLoadStoryLogFileNode")]
        [HarmonyPrefix]
        private static void AttemptLoadStoryLogFileNodePrefix(TerminalNode node) {
            if (LethalContent.StoryLogs.Values.FirstOrDefault(log =>
                    log.StoryLogTerminalNode.storyLogFileID == node.storyLogFileID) is { } dawnStoryLogInfo) {
                Logger.LogInfo($"Loading story log file node! " +
                                $"Name='{dawnStoryLogInfo.StoryLogTerminalNode.name}', " +
                                $"FileID='{dawnStoryLogInfo.StoryLogTerminalNode.storyLogFileID}'");
            }
        }

    }
}
