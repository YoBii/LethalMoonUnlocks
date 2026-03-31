using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Linq;

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
            string entryName = ResolveBestiaryEntryName(node);
            Logger.LogDebug($"Loading bestiary node! Name: {entryName}, FileID: {node?.creatureFileID ?? -1}");
            ReportTerminalRead(TerminalReadKind.Bestiary, entryName);
        }

        [HarmonyPatch("AttemptLoadStoryLogFileNode")]
        [HarmonyPrefix]
        private static void AttemptLoadStoryLogFileNodePrefix(TerminalNode node) {
            if (!Plugin.DawnLibPresent) {
                Logger.LogDebug($"Skipping story log terminal-read handling because DawnLib is not present. FileID='{node?.storyLogFileID ?? -1}'");
                return;
            }

            string entryName = ResolveStoryLogEntryName(node);
            Logger.LogInfo($"Loading story log file node! Name='{entryName}', FileID='{node?.storyLogFileID ?? -1}'");
            ReportTerminalRead(TerminalReadKind.StoryLog, entryName);
        }

        private static void ReportTerminalRead(TerminalReadKind readKind, string entryName) {
            string normalizedName = NormalizeEntryName(entryName);
            if (normalizedName.Length == 0) {
                Logger.LogDebug($"Skipping {readKind} terminal-read report with blank entry name.");
                return;
            }

            if (ProgressionManager.Instance == null) {
                Logger.LogWarning($"Skipping {readKind} terminal-read report for '{normalizedName}' because ProgressionManager is unavailable.");
                return;
            }

            if (NetworkManager.Instance.IsServer()) {
                bool added = readKind switch {
                    TerminalReadKind.Bestiary => ProgressionManager.Instance.RecordBestiaryRead(normalizedName),
                    TerminalReadKind.StoryLog => ProgressionManager.Instance.RecordStoryLogRead(normalizedName),
                    _ => false
                };

                if (!added) {
                    return;
                }

                UnlockManager.Instance?.HandleRecordedTerminalRead(readKind, normalizedName);
                return;
            }

            NetworkManager.Instance.ClientReportTerminalRead(new TerminalReadSyncData(readKind, normalizedName));
        }

        private static string ResolveBestiaryEntryName(TerminalNode node) {
            return NormalizeEntryName(node?.creatureName);
        }

        private static string ResolveStoryLogEntryName(TerminalNode node) {
            if (node == null || !Plugin.DawnLibPresent) {
                return string.Empty;
            }

            if (Dawn.LethalContent.StoryLogs.Values.FirstOrDefault(log =>
                    log?.StoryLogTerminalNode != null && log.StoryLogTerminalNode.storyLogFileID == node.storyLogFileID) is { } dawnStoryLogInfo) {
                return NormalizeEntryName(dawnStoryLogInfo.StoryLogTerminalNode.name);
            }

            Logger.LogDebug($"Could not resolve story log metadata for file ID {node.storyLogFileID}. Falling back to terminal node name.");
            return NormalizeEntryName(node.name);
        }

        private static string NormalizeEntryName(string entryName) {
            return string.IsNullOrWhiteSpace(entryName) ? string.Empty : entryName.Trim();
        }

    }
}
