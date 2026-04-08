using Dawn;
using HarmonyLib;
using LethalLevelLoader;
using LethalMoonUnlocks.Compatibility;
using LethalMoonUnlocks.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalMoonUnlocks {
    public class UnlockManager {

        public static UnlockManager Instance { get; private set; }
        internal static string LogFormatString { get; } = "| {0, -20} | {1, 6} | {2, 7} | {3, 7} | {4, 8} | {5, 11} | {6, 5} | {7, 12} | {8, 12} | {9, 11} |";
        internal static List<string> LogHeader { get; } = ["Name", "Price", "Bought", "Visits", "Catalog", "Discovered", "Sale", "Orig. Price", "Orig. State", "Story Lock"];
        internal static string FormatLogRow(params object[] values) => string.Format(LogFormatString, values);
        internal Terminal Terminal { get; set; }
        internal List<ExtendedLevel> AllLevels { get; private set; } = PatchedContent.ExtendedLevels;
        public List<LMUnlockable> Unlocks { get; set; } = new List<LMUnlockable>();
        internal int QuotaCount { get; set; }
        internal int DayCount { get; set; }
        internal int QuotaUnlocksCount { get; set; }
        internal int QuotaDiscountsCount { get; set; }
        internal int QuotaFullDiscountsCount { get; set; }
        private const string DiscoveryTargetModeMoonsOnly = "MoonsOnly";
        private const string DiscoveryTargetModeMoonsAndConstellations = "MoonsAndConstellations";
        private const string DiscoveryTargetModeConstellationsOnly = "ConstellationsOnly";
        private const string DiscoveryTargetModeConstellationsOnlyWithMoonFallback = "ConstellationsOnlyWithMoonFallback";
        internal bool UseConstellationEconomy =>
            Plugin.LethalConstellationsPresent
            && Plugin.LethalConstellationsExtension != null
            && Plugin.ConstellationManager != null;
        internal bool UseConstellationDiscovery =>
            ConfigManager.DiscoveryMode
            && Plugin.LethalConstellationsPresent
            && Plugin.LethalConstellationsExtension != null
            && Plugin.ConstellationManager != null;
        internal int DiscoveredFreeCount {
            get {
                if (UseConstellationDiscovery) {
                    return _discoveredFreeCount;
                }

                return _discoveredFreeCount > DiscoveryFreeCandidates.Count ? DiscoveryFreeCandidates.Count : _discoveredFreeCount;
            }
            set { _discoveredFreeCount = value; }
        }
        private int _discoveredFreeCount;
        internal int DiscoveredDynamicFreeCount {
            get {
                if (UseConstellationDiscovery) {
                    return _discoveredDynamicFreeCount;
                }

                return _discoveredDynamicFreeCount > DiscoveryDynamicFreeCandidates.Count ? DiscoveryDynamicFreeCandidates.Count : _discoveredDynamicFreeCount;
            }
            set { _discoveredDynamicFreeCount = value; }
        }
        private int _discoveredDynamicFreeCount;
        internal int DiscoveredPaidCount {
            get {
                if (UseConstellationDiscovery) {
                    return _discoveredPaidCount;
                }

                return _discoveredPaidCount > DiscoveryPaidCandidates.Count ? DiscoveryPaidCandidates.Count : _discoveredPaidCount;
            }
            set { _discoveredPaidCount = value; }
        }
        private int _discoveredPaidCount;
        internal List<LMUnlockable> FreeMoons {
            get {
                return Unlocks.Where(unlock => unlock.OriginalPrice == 0).ToList();
            }
        }
        internal List<LMUnlockable> DynamicFreeMoons {
            get {
                return Unlocks.Where(unlock => unlock.RoutePrice == 0).ToList();
            }
        }
        internal List<LMUnlockable> PaidMoons {
            get {
                return Unlocks.Where(unlock => unlock.RoutePrice > 0).ToList();
            }
        }
        internal List<LMUnlockable> DiscoveryCandidates {
            get {
                return Unlocks.Where(unlock =>
                    ((!unlock.OriginallyLocked && !unlock.OriginallyHidden && !unlock.StoryUnlock)
                     || (unlock.StoryUnlock && unlock.StoryIsUnlocked))
                    && !IsGloballyDiscovered(unlock)).ToList();
            }
        }
        internal List<LMUnlockable> DiscoveryFreeCandidates {
            get {
                return DiscoveryCandidates.Where(candidate => candidate.OriginalPrice == 0).ToList();
            }
        }
        internal List<LMUnlockable> DiscoveryDynamicFreeCandidates {
            get {
                return DiscoveryCandidates.Where(candidate => candidate.RoutePrice == 0).ToList();
            }
        }
        internal List<LMUnlockable> DiscoveryPaidCandidates {
            get {
                return DiscoveryCandidates.Where(candidate => candidate.RoutePrice > 0).ToList();
            }
        }

        [Obsolete("Story-lock designation callbacks are obsolete. Do not subscribe to this API anymore; initialize story-gated moons as hidden+locked at startup and call TryReleaseStoryLock* when progression should release the gate.", false)]
        public delegate List<string> DesignateStoryUnlocks();

        /// <summary>
        /// Compatibility-only callback fired on lobby creation after <c>Terminal.Start</c>.
        /// <br>Do not use this to designate story-gated moons anymore.</br>
        /// <br></br>
        /// Story-gated moons should now start hidden and locked, then call <see cref="TryReleaseStoryLock(string)"/> or <see cref="TryReleaseStoryLockShowAlert(string)"/> when progression should release them.
        /// <br></br>
        /// <br></br>
        /// <example>For example:
        /// <code>
        /// UnlockManager.OnCollectStoryLockedMoons += MySubscriber;
        /// private List&lt;string&gt; MySubscriber() { return new List&lt;string&gt; { "Infernis", "Penumbra" } }
        /// </code>
        /// </example>
        /// <remarks>
        /// This event remains available only so older integrations continue to load without missing-member failures.
        /// </remarks>
        /// <seealso cref="TryReleaseStoryLock(string)"/>
        /// </summary>
        [Obsolete("Story-lock designation callbacks are obsolete. Do not subscribe to this API anymore; initialize story-gated moons as hidden+locked at startup and call TryReleaseStoryLock* when progression should release the gate.", false)]
        public static event DesignateStoryUnlocks OnCollectStoryLockedMoons;

        internal UnlockManager() {
            if (Instance == null)
                Instance = this;
        }

        /// <summary>
        /// Release story lock of your moon allowing LMU to handle it like any other i.e. add it to moon catalog, etc..
        /// <br>When you would otherwise unhide and unlock your moon via LLL call this method instead.</br>
        /// </summary>
        /// <param name="numberlessPlanetName">The name of the moon to release from story locked state.</param>
        /// <returns>
        /// <c>true</c> if the moon was found.
        /// <br></br>
        /// <c>false</c> if the moon could not be found or is not currently inferred as story-gated.
        /// </returns>
        public static bool TryReleaseStoryLock (string numberlessPlanetName) {
            if (!TryReleaseStoryLockInternal(numberlessPlanetName, out _, out _)) {
                return false;
            }

            Instance?.IterateUnlocks();
            NetworkManager.Instance?.ServerSendUnlockables(Instance?.Unlocks);
            return true;
        }

        /// <summary>
        /// Release story lock of your moon allowing LMU to handle it like any other i.e. add it to moon catalog, etc..
        /// <br>When you would otherwise unhide and unlock your moon via LLL call this method instead.</br>
        /// <br>Also display a generic alert message.</br>
        /// </summary>
        /// <param name="numberlessPlanetName">The name of the moon to release from story locked state.</param>
        /// <returns>
        /// <c>true</c> if the moon was found.
        /// <br></br>
        /// <c>false</c> if the moon could not be found or is not currently inferred as story-gated.
        /// </returns>
        public static bool TryReleaseStoryLockShowAlert (string numberlessPlanetName) {
            if (!TryReleaseStoryLockInternal(numberlessPlanetName, out var unlock, out var constellationReleaseResult)) {
                return false;
            }

            Instance?.IterateUnlocks();
            NetworkManager.Instance?.ServerSendUnlockables(Instance?.Unlocks);
            NetworkManager.Instance?.ServerSendAlertMessage(new Notification { Header = "Autopilot", Text = "Location data detected!\nQueued for processing.", Key = "LMU_StoryLockReleasedGeneric" });
            SendStoryReleaseAlert(unlock, constellationReleaseResult);
            NetworkManager.Instance?.ServerSendAlertQueueEvent();
            return true;
        }

        private static bool TryReleaseStoryLockInternal(string numberlessPlanetName, out LMUnlockable unlock, out LethalConstellationsManager.StoryReleaseResult constellationReleaseResult) {
            unlock = null;
            constellationReleaseResult = null;

            if (!ConfigManager.EnableStoryProgression) {
                Logger.LogInfo("Received request to release story lock but story locks are ignored by user config.");
                return false;
            }

            unlock = Instance?.Unlocks.FirstOrDefault(u => u.Name == numberlessPlanetName);
            if (unlock == null) {
                Logger.LogWarning("Received request to release story lock but the LMUnlockable associated with the level name was not found!");
                return false;
            }

            if (!unlock.StoryUnlock) {
                Logger.LogWarning("Received request to release story lock but the LMUnlockable associated with the level name is not currently inferred as story-gated!");
                return false;
            }

            unlock.StoryIsUnlocked = true;
            if (ConfigManager.DiscoveryMode && IsImmediateMoonStoryReleaseBehavior()) {
                unlock.SetDiscoveryState(true);
            }
            if (Plugin.LethalConstellationsPresent && LethalConstellationsManager.Instance != null) {
                constellationReleaseResult = LethalConstellationsManager.Instance.ReleaseStoryLockForMoon(numberlessPlanetName);
            }

            Logger.LogInfo($"{unlock.Name}: Request to release story lock received! Releasing lock.. {unlock.Name} now available (for discovery).");
            return true;
        }

        private static void SendStoryReleaseAlert(LMUnlockable unlock, LethalConstellationsManager.StoryReleaseResult constellationReleaseResult) {
            if (constellationReleaseResult != null && constellationReleaseResult.AnyAffected) {
                if (constellationReleaseResult.ImmediateDiscovery) {
                    string discoveredNames = string.Join(", ", constellationReleaseResult.AffectedConstellations);
                    NetworkManager.Instance?.ServerSendAlertMessage(new Notification {
                        Header = "Autopilot",
                        Text = $"Success! New {constellationReleaseResult.AffectedConstellations.Count.SinglePluralWord("constellation")} discovered:\n{discoveredNames}.",
                        Key = "LMU_StoryLockReleasedGeneric"
                    });
                    return;
                }

                NetworkManager.Instance?.ServerSendAlertMessage(new Notification {
                    Header = "Autopilot",
                    Text = "Destination unreachable! Status: UNKNOWN. Writing location data to backlog...",
                    IsWarning = true,
                    Key = "LMU_StoryLockReleasedGeneric"
                });
                return;
            }

            if (!ConfigManager.DiscoveryMode || IsImmediateMoonStoryReleaseBehavior()) {
                NetworkManager.Instance?.ServerSendAlertMessage(new Notification { Header = "Autopilot", Text = $"Success! New moon discovered:\n{unlock.ExtendedLevel.SelectableLevel.PlanetName}.", Key = "LMU_StoryLockReleasedGeneric" });
                return;
            }

            NetworkManager.Instance?.ServerSendAlertMessage(new Notification { Header = "Autopilot", Text = "Destination unreachable! Status: UNKNOWN. Writing location data to backlog...", IsWarning = true, Key = "LMU_StoryLockReleasedGeneric" });
        }

        private static bool IsImmediateMoonStoryReleaseBehavior() {
            return ConfigManager.MoonStoryReleaseBehavior == StoryReleaseBehavior.ImmediateDiscovery;
        }

        internal void InitializeUnlocks() {
            if (AllLevels == null || AllLevels.Count == 0) {
                Logger.LogFatal($"Unable to find levels!");
                return;
            }
            Logger.LogInfo("Initializing LMUnlockables from Extended levels..");
            foreach (var level in AllLevels) {
                if (level == null || level.SelectableLevel == null
                    //|| level.IsRouteRemoved == true //not respected currently. still shows in terminal
                    || level.NumberlessPlanetName == "Liquidation" || level.NumberlessPlanetName == "Gordion"
                    || Unlocks.Any(unlock => unlock.Name == level.NumberlessPlanetName)) {
                    string levelName = string.Empty;
                    if (level != null && level.SelectableLevel != null)
                        levelName = ": " + level.NumberlessPlanetName;
                    Logger.LogDebug($"Skipping level{levelName}..");
                    continue;
                }
                Unlocks.Add(new LMUnlockable(level));
            }
            Unlocks = Unlocks.OrderBy(unlock => unlock.OriginalPrice).ToList();
            LogUnlockables(true);
            
            // (Un-)subscribe to/from LLL event to replace preview text
            if (ConfigManager.DisplayTerminalTags || ConfigManager.TerminalFontSizeOverride) {
                TerminalManager.onBeforePreviewInfoTextAdded -= ReplaceTerminalPreview;
                TerminalManager.onBeforePreviewInfoTextAdded += ReplaceTerminalPreview;
            } else {
                TerminalManager.onBeforePreviewInfoTextAdded -= ReplaceTerminalPreview;
            }
        }

        internal void InitializeUnlocksDawnLib() {
            if (LethalContent.Moons.Count == 0) {
                Logger.LogFatal($"Unable to find levels!");
                return;
            }
            Logger.LogInfo("Initializing LMUnlockables from DawnLib registry..");
            foreach (var dawnMoon in LethalContent.Moons.Values) {
                if (dawnMoon == null
                    || dawnMoon.Key.Key == "test"
                    || dawnMoon.GetNumberlessPlanetName() == "Liquidation" || dawnMoon.GetNumberlessPlanetName() == "Gordion") {
                    string levelName = string.Empty;
                    if (dawnMoon != null && dawnMoon.Level)
                        levelName = ": " + dawnMoon.GetNumberlessPlanetName();
                    Logger.LogDebug($"Skipping level {levelName}..");
                    continue;
                }

                if (Unlocks.FirstOrDefault(u =>
                        u.ExtendedLevel.SelectableLevel.levelID == dawnMoon.Level.levelID) is { } unlock) {
                    unlock.OverrideDefaultsDawnLib(dawnMoon);
                }
                else {
                    Logger.LogWarning(
                        $"Got moon {dawnMoon.GetNumberlessPlanetName()} from DawnLib registry that we didn't previously initialize from LLL. Will probably cause errors or misbehaviour.");
                }
                
 
                
            }
            Unlocks = Unlocks.OrderBy(unlock => unlock.OriginalPrice).ToList();
            LogUnlockables(true);
        }

        internal void ImportUnlockableData(List<LMUnlockable> newData) {
            Logger.LogInfo("Importing LMU_Unlockable data..");
            if (newData == null) {
                Logger.LogWarning("Received null LMU_Unlockable data list. Skipping import.");
                return;
            }

            foreach (LMUnlockable importUnlock in newData) {
                if (importUnlock == null) {
                    Logger.LogWarning("Received null LMUnlockable entry during import. Skipping entry.");
                    continue;
                }

                foreach (LMUnlockable unlock in Unlocks) {
                    if (unlock.Name == importUnlock.Name) {
                        unlock.OverrideData(importUnlock);
                    }
                }
            }
        }

        internal void ImportUnlockableSyncData(List<LMUnlockableSyncData> newData) {
            Logger.LogInfo("Importing LMU unlock sync data..");
            if (newData == null) {
                Logger.LogWarning("Received null LMU unlock sync data list. Skipping import.");
                return;
            }

            foreach (LMUnlockableSyncData importUnlock in newData) {
                if (importUnlock == null) {
                    Logger.LogWarning("Received null LMUnlockable sync entry during import. Skipping entry.");
                    continue;
                }

                foreach (LMUnlockable unlock in Unlocks) {
                    if (string.Equals(unlock.Name, importUnlock.name, StringComparison.OrdinalIgnoreCase)) {
                        unlock.ApplySyncData(importUnlock);
                    }
                }
            }
        }

        internal void CollectStoryLockedMoons() {
            if (OnCollectStoryLockedMoons != null) {
                var subscribers = OnCollectStoryLockedMoons.GetInvocationList();

                foreach (DesignateStoryUnlocks subscriber in subscribers) {
                    try {
                        List<string> response = subscriber();
                        if (response == null || response.Count == 0) {
                            Logger.LogDebug($"Observed story-lock designation callback '{subscriber.Method.DeclaringType?.FullName}.{subscriber.Method.Name}' with no moon names returned. Ignoring callback result.");
                            continue;
                        }

                        Logger.LogDebug($"Observed obsolete story-lock designation callback '{subscriber.Method.DeclaringType?.FullName}.{subscriber.Method.Name}' with moon names [{string.Join(", ", response)}]. Ignoring callback result.");
                    } catch (Exception ex) {
                        Logger.LogError($"Couldn't handle subscriber response while collecting story locked moons! Error: {ex.Message}");
                    }
                }
            }
        }

        internal void IterateUnlocks() {
            Logger.LogDebug("Iterating states..");
            foreach (var unlock in Unlocks) {
                unlock.IterateState();
            }
        }

        internal void ApplyUnlocks() {
            foreach (var unlock in Unlocks) {
                unlock.ApplyState();
                unlock.ApplyVisibility();
            }
            if (Plugin.LethalConstellationsPresent) {
                Plugin.LethalConstellationsExtension.ApplyUnlocks();
            }
            LogUnlockables();
        }

        public void BuyMoon(string moon) {
            Logger.LogInfo($"{moon}: Moon was bought!");
            var unlock = Unlocks.FirstOrDefault(candidate => candidate.Name == moon);
            if (unlock == null) {
                Logger.LogError($"Couldn't find moon '{moon}' for route progression.");
                return;
            }

            ApplyMoonRouteProgression(unlock, wasPaid: true, allowTravelDiscovery: true, broadcastState: true);
        }

        internal void ApplyLocalClientMoonPurchasePreview(string moon) {
            if (string.IsNullOrWhiteSpace(moon)) {
                return;
            }

            var unlock = Unlocks.FirstOrDefault(candidate => string.Equals(candidate.Name, moon, StringComparison.OrdinalIgnoreCase));
            if (unlock == null) {
                Logger.LogWarning($"Couldn't find moon '{moon}' for local client route progression preview.");
                return;
            }

            if (ConfigManager.DiscountMode) {
                if (unlock.BuyCount < ConfigManager.DiscountsCount) {
                    unlock.BuyCount++;
                }
            } else {
                unlock.BuyCount++;
            }

            unlock.RefreshCalculatedPrice();
            unlock.ApplyState();
            unlock.ApplyVisibility();
            Logger.LogInfo($"{unlock.Name}: Applied local client route progression preview. Buy count is now {unlock.BuyCount}, price is now {unlock.RoutePrice}.");
        }

        internal void ApplyMoonRouteProgression(LMUnlockable unlock, bool wasPaid, bool allowTravelDiscovery, bool broadcastState) {
            if (unlock == null) {
                return;
            }

            if (wasPaid) {
                if (ConfigManager.DiscountMode) {
                    if (unlock.BuyCount < ConfigManager.DiscountsCount) {
                        unlock.BuyCount++;
                    }
                } else {
                    unlock.BuyCount++;
                }
                Logger.LogInfo($"{unlock.Name}: Set buy count to {unlock.BuyCount}");
            }

            if (allowTravelDiscovery && ConfigManager.DiscoveryMode) {
                var travelDiscoveryCandidates = GetTriggerMoonDiscoveryCandidates();
                if (ConfigManager.TravelDiscoveries
                    && (UseConstellationDiscovery
                        ? HasDiscoveryTargets(GetConstellationDiscoveryTargetMode("Travel"), travelDiscoveryCandidates)
                        : DiscoveryCandidates.Count > 0)
                    && RandomHelper.Chance(ConfigManager.TravelDiscoveryChance)) {
                    Logger.LogInfo($"Travel Discovery triggered! (Chance: {ConfigManager.TravelDiscoveryChance}%)");
                    if (UseConstellationDiscovery) {
                        TryHandleDiscoveryTrigger("Travel", travelDiscoveryCandidates, candidates => TravelDiscovery(unlock, candidates));
                    } else {
                        TravelDiscovery(unlock, DiscoveryCandidates);
                    }
                }
            }

            if (!broadcastState) {
                return;
            }

            IterateUnlocks();
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 2);
        }

        internal void LogUnlockables(bool debug = false) {
            void LogLine(string message) {
                if (debug) {
                    Logger.LogDebug(message);
                } else {
                    Logger.LogInfo(message);
                }
            }

            string header = FormatLogRow(LogHeader.Cast<object>().ToArray());
            string separator = FormatLogRow(new string('-', 20), new string('-', 6), new string('-', 7), new string('-', 7), new string('-', 8), new string('-', 11), new string('-', 5), new string('-', 12), new string('-', 12), new string('-', 11));
            LogLine("| LMUnlockable state table");

            if (Plugin.LethalConstellationsPresent
                && Plugin.LethalConstellationsExtension != null
                && Plugin.ConstellationManager != null) {
                var groupedUnlocks = new Dictionary<string, List<LMUnlockable>>();
                var unmatchedUnlocks = new List<LMUnlockable>();

                foreach (var unlock in Unlocks) {
                    string constellationName = Plugin.LethalConstellationsExtension.GetConstellationName(unlock);
                    if (string.IsNullOrWhiteSpace(constellationName)) {
                        unmatchedUnlocks.Add(unlock);
                        continue;
                    }

                    if (!groupedUnlocks.TryGetValue(constellationName, out var constellationUnlocks)) {
                        constellationUnlocks = new List<LMUnlockable>();
                        groupedUnlocks[constellationName] = constellationUnlocks;
                    }
                    constellationUnlocks.Add(unlock);
                }

                if (groupedUnlocks.Count > 0) {
                    string currentConstellationName = Plugin.ConstellationManager.GetCurrentConstellationName() ?? string.Empty;

                    foreach (var constellation in groupedUnlocks) {
                        string groupHeader = $"| Constellation: {constellation.Key}";
                        if (!string.IsNullOrWhiteSpace(currentConstellationName)
                            && string.Equals(currentConstellationName, constellation.Key, StringComparison.OrdinalIgnoreCase)) {
                            groupHeader += " (Current)";
                        }
                        LogLine(groupHeader);
                        LogLine(separator);
                        LogLine(header);
                        LogLine(separator);
                        foreach (var unlock in constellation.Value) {
                            LogLine(unlock.ToString());
                        }
                        LogLine(separator);
                    }

                    if (unmatchedUnlocks.Count > 0) {
                        LogLine("| Moons without Constellation");
                        LogLine(separator);
                        LogLine(header);
                        LogLine(separator);
                        foreach (var unlock in unmatchedUnlocks) {
                            LogLine(unlock.ToString());
                        }
                        LogLine(separator);
                        Logger.LogWarning($"Found {unmatchedUnlocks.Count} moon(s) without a LethalConstellations group while constellation grouping is active. If this happens past round initialization something is broken: {string.Join(", ", unmatchedUnlocks.Select(unlock => unlock.Name))}");
                    }

                    return;
                }
            }

            LogLine(header);
            
            foreach (var unlock in Unlocks.Select((value, i) => new { i, value })) {
                if (unlock.i % 5 == 0) {
                    LogLine(separator);
                }
                LogLine(unlock.value.ToString());
            }
        }

        internal void OnLobbyStart() {
            // Refresh config and create LMUnlockable for all moons
            ConfigManager.RefreshConfig();
            DiscoveredFreeCount = ConfigManager.DiscoveryFreeCountBase;
            DiscoveredDynamicFreeCount = ConfigManager.DiscoveryDynamicFreeCountBase;
            DiscoveredPaidCount = ConfigManager.DiscoveryPaidCountBase;

            // Load save data
            // if save exists apply all unlockable data and continue
            // else init new game
            bool loadSuccess = LoadAndImportSavaData();
            if (!loadSuccess) {
                InitializeNewGame();
            }

            TryEvaluateConstellationUnlockConditions();

            if (loadSuccess && UseConstellationDiscovery) {
                Plugin.ConstellationManager.ApplyCurrentConstellationVisibility();
            }
            
            // Force shuffle if discovery mode is enabled and no moons are discovered
            // This can happen after loading a save that didn't have Discovery mode enabled
            if (ShouldForceDiscoveryShuffleOnLoad(loadSuccess)) {
                ShuffleDiscoverable();
            }

            IterateUnlocks();

            // Apply everything
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 2);
        }


        internal void OnNewQuota() {
            QuotaCount++;
            Logger.LogInfo($"New quota! Completed quota count: {QuotaCount}");
            ConfigManager.RefreshConfig();
            TryEvaluateConstellationUnlockConditions();
            if (ConfigManager.DiscoveryMode) {
                // Remove [NEW] discovery tags
                Unlocks.Where(unlock => unlock.NewDiscovery).Do(unlock => { unlock.NewDiscovery = false; });
                // SHUFFLE ON NEW QUOTA
                if (!ConfigManager.DiscoveryNeverShuffle) {
                    Logger.LogInfo($"Shuffling moon rotation on new Quota..");
                    ShuffleDiscoverable();
                }
                // QUOTA DISCOVERY
                var quotaDiscoveryCandidates = GetTriggerMoonDiscoveryCandidates();
                if (ConfigManager.QuotaDiscoveries
                    && (UseConstellationDiscovery
                        ? HasDiscoveryTargets(GetConstellationDiscoveryTargetMode("Quota"), quotaDiscoveryCandidates)
                        : DiscoveryCandidates.Count > 0)
                    && RandomHelper.Chance(ConfigManager.QuotaDiscoveryChance)) {
                    Logger.LogInfo($"Quota Discovery triggered! (Chance: {ConfigManager.QuotaDiscoveryChance}%)");
                    if (UseConstellationDiscovery) {
                        TryHandleDiscoveryTrigger(
                            "Quota",
                            quotaDiscoveryCandidates,
                            candidates => (ConfigManager.QuotaDiscoveryCheapestGroup && ConfigManager.MoonGroupMatchingMethod == "Custom")
                                ? QuotaDiscoveryGroup(candidates)
                                : QuotaDiscovery(candidates),
                            preferCheapestConstellation: ConfigManager.QuotaDiscoveryCheapestConstellation);
                    } else if (ConfigManager.QuotaDiscoveryCheapestGroup && ConfigManager.MoonGroupMatchingMethod == "Custom") {
                        QuotaDiscoveryGroup(DiscoveryCandidates);
                    } else {
                        QuotaDiscovery(DiscoveryCandidates);
                    }
                }
            }
            // SHUFFLE SALES
            if (ConfigManager.Sales) {
                RefreshSales();
            }
            // Iterate Unlocks to make sure in discovery mode unlocks/discounts are not granted to undiscovered moons
            IterateUnlocks();

            // QUOTA UNLOCK
            if (!ConfigManager.DiscountMode && ConfigManager.QuotaUnlocks && HasQuotaRewardMoonCandidates()) {
                if (RandomHelper.Chance(ConfigManager.QuotaUnlockChance) && (ConfigManager.QuotaUnlockMaxCount < 1 || QuotaUnlocksCount < ConfigManager.QuotaUnlockMaxCount)) {
                    Logger.LogInfo($"Quota unlock triggered! (Chance: {ConfigManager.QuotaUnlockChance}%)");
                    QuotaUnlock();
                }
            }
            // DISCOUNT MODE
            if (ConfigManager.DiscountMode) {
                // QUOTA DISCOUNT
                if (ConfigManager.QuotaDiscounts && HasQuotaRewardMoonCandidates() && RandomHelper.Chance(ConfigManager.QuotaDiscountChance) && (ConfigManager.QuotaDiscountMaxCount < 1 || QuotaDiscountsCount < ConfigManager.QuotaDiscountMaxCount)) {
                    Logger.LogInfo($"Quota Discount triggered! (Chance: {ConfigManager.QuotaDiscountChance}%)");
                    QuotaDiscount();
                }
                // QUOTA FULL DISCOUNT
                if (ConfigManager.QuotaFullDiscounts && HasQuotaRewardMoonCandidates() && RandomHelper.Chance(ConfigManager.QuotaFullDiscountChance) && (ConfigManager.QuotaFullDiscountMaxCount < 1 || QuotaFullDiscountsCount < ConfigManager.QuotaFullDiscountMaxCount)) {
                    Logger.LogInfo($"Quota Full Discount triggered! (Chance: {ConfigManager.QuotaFullDiscountChance}%)");
                    QuotaFullDiscount();
                }
            }

            // APPLY ALL
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 5);
        }

        internal void OnNewDay() {
            Logger.LogDebug($"DaysUntilDeadlineHUD: {(int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime)}, DaysUntilDeadline: {TimeOfDay.Instance.daysUntilDeadline}, deadlineDaysAmount: {TimeOfDay.Instance.quotaVariables.deadlineDaysAmount}");
            // NEW QUOTA DAY
            if ((int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime) == TimeOfDay.Instance.quotaVariables.deadlineDaysAmount || (int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime) < 0) {
                DayCount++;
                Logger.LogInfo($"New day! Completed days: {DayCount}");
                Logger.LogInfo($"New day is also new quota! Skip new day routine..");
                // LAST DAY OF QUOTA - REROUTE SHIP TO COMPANY AND SKIP REST
            } else if ((int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime) == 0 && ConfigManager.DiscoveryMode) {
                Logger.LogInfo($"New day is last day of the quota! Not shuffling.");
                if (ConfigManager.AutoRerouteToCompany) {
                    ExtendedLevel destination;
                    ExtendedLevel galetry = AllLevels.FirstOrDefault(level => level.NumberlessPlanetName == "Galetry");
                    if (ConfigManager.PreferGaletry && galetry != null && !galetry.IsRouteHidden && !galetry.IsRouteLocked) {
                        destination = galetry;
                    } else {
                        destination = AllLevels.FirstOrDefault(level => level.NumberlessPlanetName == "Gordion");
                    }

                    if (destination == null) {
                        Logger.LogError($"Couldn't find reroute destination!");
                    } else if (LevelManager.CurrentExtendedLevel != destination) {
                        string destinationName = destination.NumberlessPlanetName == "Gordion" ? "the Company building" : destination.NumberlessPlanetName;
                        Logger.LogInfo($"Rerouting ship to {destinationName}!");
                        // wait a bit or the level change fails
                        DelayHelper.Instance.ExecuteAfterDelay(() => { StartOfRound.Instance.ChangeLevelServerRpc(destination.SelectableLevel.levelID, Terminal.groupCredits); }, 3f);
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Deadline!", Text = $"Auto routing ship to {destinationName}.", Key = "LMU_RerouteCompany" });
                    } else {
                        string destinationName = destination.NumberlessPlanetName == "Gordion" ? "the Company building" : destination.NumberlessPlanetName;
                        Logger.LogInfo($"Already at {destinationName}. No need to reroute.");
                    }
                }
            } else {
                // NEW DAY - NOT NEW QUOTA
                DayCount++;
                Logger.LogInfo($"New day! Completed days: {DayCount}");
                ConfigManager.RefreshConfig();
                if (ConfigManager.DiscoveryMode) {
                    // Remove [NEW] discovery tags
                    Unlocks.Where(unlock => unlock.NewDiscovery).Do(unlock => { unlock.NewDiscovery = false; });
                    // Apply unlocks to make sure discovery selections are correct
                    IterateUnlocks();
                    // Shuffle NEW DAY - EVERY DAY
                    if (ConfigManager.DiscoveryShuffleEveryDay) {
                        Logger.LogInfo($"Shuffling moon rotation on new day!");
                        ShuffleDiscoverable();
                    }
                    // NEW DAY DISCOVERY
                    var newDayDiscoveryCandidates = GetTriggerMoonDiscoveryCandidates();
                    if (ConfigManager.NewDayDiscoveries
                        && (UseConstellationDiscovery
                            ? HasDiscoveryTargets(GetConstellationDiscoveryTargetMode("NewDay"), newDayDiscoveryCandidates)
                            : DiscoveryCandidates.Count > 0)
                        && RandomHelper.Chance(ConfigManager.NewDayDiscoveryChance)) {
                        Logger.LogInfo($"New Day Discovery triggered! (Chance: {ConfigManager.NewDayDiscoveryChance}%)");
                        if (UseConstellationDiscovery) {
                            TryHandleDiscoveryTrigger("NewDay", newDayDiscoveryCandidates, NewDayDiscovery);
                        } else {
                            NewDayDiscovery(DiscoveryCandidates);
                        }
                    }
                }
                if (ConfigManager.Sales && ConfigManager.SalesShuffleDaily) {
                    RefreshSales();
                }
            }
            IterateUnlocks();
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 3);
        }

        private void RefreshSales() {
            if (DayCount < ConfigManager.SalesMinDayCount) {
                Logger.LogInfo($"Skipping moon sales shuffle because completed days {DayCount} is below configured minimum {ConfigManager.SalesMinDayCount}.");
                Unlocks.Do(unlock => {
                    unlock.OnSale = false;
                    unlock.SalesRate = 0;
                });
                if (UseConstellationEconomy) {
                    foreach (var constellation in Plugin.LethalConstellationsExtension.ConstellationStates.Values) {
                        constellation.OnSale = false;
                        constellation.SalesRate = 0;
                    }

                    Plugin.ConstellationManager.ApplyConstellationState();
                }
                return;
            }

            Unlocks.Do(unlock => unlock.RefreshSale());
            if (UseConstellationEconomy) {
                Plugin.ConstellationManager.RefreshConstellationSales();
            }
        }

        internal void OnArrive() {
            var unlock = Unlocks.FirstOrDefault(unlock => unlock.Name == LevelManager.CurrentExtendedLevel.NumberlessPlanetName);
            if (unlock != null) {
                Logger.LogInfo($"Visiting moon {unlock.Name}!");
                unlock.VisitMoon();
            }
            TryEvaluateConstellationUnlockConditions();
            IterateUnlocks();
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
        }
        internal void OnLanding(SelectableLevel level) {
            var unlock = Unlocks.FirstOrDefault(unlock => unlock.ExtendedLevel.SelectableLevel.levelID == level.levelID);
            if (unlock != null) {
                unlock.Land();
            }
        }
        internal void OnResetGame() {
            if (ConfigManager.ResetWhenFired) {
                Logger.LogInfo($"Resetting all progress on getting fired!");
                ProgressionManager.Instance?.Reset();
                Reset();
                InitializeUnlocks();
                DelayHelper.Instance.ExecuteAfterDelay(() => {
                    InitializeNewGame();
                    IterateUnlocks();
                    NetworkManager.Instance.ServerSendUnlockables(Unlocks);
                }, 8.0f);
            } else {
                NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            }
        }
        internal void OnDisconnect() {
            ProgressionManager.Instance?.Reset();
            Reset();
        }

        internal bool HandleRecordedTerminalRead(TerminalReadKind readKind, string entryName) {
            string normalizedName = string.IsNullOrWhiteSpace(entryName) ? string.Empty : entryName.Trim();
            if (normalizedName.Length == 0) {
                Logger.LogDebug($"Skipping recorded terminal-read handling for blank {readKind} entry name.");
                return false;
            }

            bool anyChanged = false;
            LMUnlockable releasedUnlock = null;
            LethalConstellationsManager.StoryReleaseResult constellationReleaseResult = null;

            if (readKind == TerminalReadKind.Bestiary
                && ConfigManager.LMUStoryProgression
                && ProgressionManager.Instance?.HasReadBestiaryEntry("Old birds") == true) {
                anyChanged |= TryReleaseStoryLockInternal("Embrion", out releasedUnlock, out constellationReleaseResult);
            }

            anyChanged |= TryEvaluateConstellationUnlockConditions();
            if (!anyChanged) {
                Logger.LogDebug($"No dependent progression changes after recorded {readKind} entry '{normalizedName}'.");
                return false;
            }

            IterateUnlocks();
            NetworkManager.Instance?.ServerSendUnlockables(Unlocks);

            if (releasedUnlock != null) {
                NetworkManager.Instance?.ServerSendAlertMessage(new Notification { Header = "Autopilot", Text = "Location data detected!\nQueued for processing.", Key = "LMU_StoryLockReleasedGeneric" });
                SendStoryReleaseAlert(releasedUnlock, constellationReleaseResult);
                NetworkManager.Instance?.ServerSendAlertQueueEvent();
            }

            return true;
        }

        private bool TryEvaluateConstellationUnlockConditions() {
            if (!Plugin.LethalConstellationsPresent || Plugin.ConstellationManager == null) {
                return false;
            }

            return Plugin.ConstellationManager.EvaluateCustomUnlockConditions();
        }

        private List<LMUnlockable> GetTriggerMoonDiscoveryCandidates() {
            if (!UseConstellationDiscovery || Plugin.ConstellationManager == null) {
                return DiscoveryCandidates;
            }

            return Plugin.ConstellationManager.GetCurrentConstellationDiscoveryCandidates();
        }

        private static string GetDiscoveryTriggerName(string triggerKey) {
            return triggerKey switch {
                "Quota" => "Quota Discovery",
                "Travel" => "Travel Discovery",
                "NewDay" => "New Day Discovery",
                _ => "Discovery"
            };
        }

        private string GetConstellationDiscoveryTargetMode(string triggerKey) {
            return triggerKey switch {
                "Quota" => ConfigManager.LethalConstellationsQuotaDiscoveryTargetMode,
                "Travel" => ConfigManager.LethalConstellationsTravelDiscoveryTargetMode,
                "NewDay" => ConfigManager.LethalConstellationsNewDayDiscoveryTargetMode,
                _ => DiscoveryTargetModeMoonsOnly
            };
        }

        private int GetConstellationDiscoveryChance(string triggerKey) {
            return triggerKey switch {
                "Quota" => ConfigManager.LethalConstellationsQuotaDiscoveryChance,
                "Travel" => ConfigManager.LethalConstellationsTravelDiscoveryChance,
                "NewDay" => ConfigManager.LethalConstellationsNewDayDiscoveryChance,
                _ => 0
            };
        }

        private List<LMUnlockable> GetQuotaRewardMoonCandidates() {
            if (UseConstellationDiscovery && Plugin.ConstellationManager != null) {
                return Plugin.ConstellationManager.GetQuotaRewardMoonTargets();
            }

            return PaidMoons;
        }

        private bool HasQuotaRewardMoonCandidates() {
            return GetQuotaRewardMoonCandidates().Any(unlock => unlock.RoutePrice > 0);
        }

        private bool HasDiscoveryTargets(string targetMode, List<LMUnlockable> moonCandidates) {
            bool hasMoonCandidates = moonCandidates.Count > 0;
            bool hasUndiscoveredConstellations = Plugin.ConstellationManager?.HasUndiscoveredConstellations() == true;
            bool hasEligibleUndiscoveredConstellations = Plugin.ConstellationManager?.HasEligibleUndiscoveredConstellations() == true;

            return targetMode switch {
                DiscoveryTargetModeMoonsOnly => hasMoonCandidates,
                DiscoveryTargetModeMoonsAndConstellations => hasMoonCandidates || hasEligibleUndiscoveredConstellations,
                DiscoveryTargetModeConstellationsOnly => hasEligibleUndiscoveredConstellations,
                DiscoveryTargetModeConstellationsOnlyWithMoonFallback => hasUndiscoveredConstellations ? hasEligibleUndiscoveredConstellations : hasMoonCandidates,
                _ => hasMoonCandidates
            };
        }

        private void ApplyMoonDiscoveries(List<LMUnlockable> discoveries, bool permanent) {
            if (discoveries == null || discoveries.Count == 0) {
                return;
            }

            foreach (var discovery in discoveries) {
                if (UseConstellationDiscovery) {
                    discovery.SetDiscoveryState(true);
                } else {
                    discovery.Discovered = true;
                    if (!discovery.DiscoveredOnce) {
                        discovery.DiscoveredOnce = true;
                        discovery.NewDiscovery = true;
                    }
                }

                if (permanent) {
                    discovery.PermanentlyDiscovered = true;
                    Logger.LogDebug($"{discovery.Name}: Discovery is permanent");
                }
            }

            if (UseConstellationDiscovery) {
                if (!permanent) {
                    Plugin.ConstellationManager.AddLocalMoonDiscoveriesForCurrentConstellation(discoveries);
                }
                Plugin.ConstellationManager.ApplyCurrentConstellationVisibility();
            }
        }

        private bool TryDiscoverConstellationForTrigger(string triggerKey, bool preferCheapestConstellation) {
            if (Plugin.ConstellationManager == null || !Plugin.ConstellationManager.TryDiscoverConstellation(preferCheapestConstellation, out var constellationName)) {
                return false;
            }

            string triggerName = GetDiscoveryTriggerName(triggerKey);
            string constellationWord = Plugin.ConstellationManager.GetConstellationWord();
            NotificationHelper.SendChatMessage($"{triggerName} granted {constellationWord.ToLowerInvariant()}:\n<color=red>{constellationName}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                Header = $"{triggerName}!",
                Text = $"Received coordinates for constellation: {constellationName}",
                IsWarning = true,
                Key = $"LMU_{triggerKey}ConstellationDiscovery"
            });
            Logger.LogInfo($"{triggerName}: Discovered constellation '{constellationName}'.");
            return true;
        }

        private bool TryHandleDiscoveryTrigger(string triggerKey, List<LMUnlockable> moonCandidates, Func<List<LMUnlockable>, bool> moonDiscoveryAction, bool preferCheapestConstellation = false) {
            if (!UseConstellationDiscovery || Plugin.ConstellationManager == null) {
                return false;
            }

            string targetMode = GetConstellationDiscoveryTargetMode(triggerKey);
            bool hasUndiscoveredConstellations = Plugin.ConstellationManager.HasUndiscoveredConstellations();
            bool hasEligibleUndiscoveredConstellations = Plugin.ConstellationManager.HasEligibleUndiscoveredConstellations();
            int constellationDiscoveryChance = GetConstellationDiscoveryChance(triggerKey);
            bool changed = false;
            Logger.LogInfo(
                $"{GetDiscoveryTriggerName(triggerKey)}: Handling trigger with target mode '{targetMode}', moon candidates={moonCandidates.Count}, undiscovered constellations={(hasUndiscoveredConstellations ? "yes" : "no")}, eligible undiscovered constellations={(hasEligibleUndiscoveredConstellations ? "yes" : "no")}, constellation chance={constellationDiscoveryChance}%.");

            switch (targetMode) {
                case DiscoveryTargetModeMoonsOnly:
                    return moonCandidates.Count > 0 && moonDiscoveryAction(moonCandidates);

                case DiscoveryTargetModeMoonsAndConstellations:
                    if (moonCandidates.Count > 0) {
                        changed |= moonDiscoveryAction(moonCandidates);
                    }
                    if (hasEligibleUndiscoveredConstellations && RandomHelper.Chance(constellationDiscoveryChance)) {
                        changed |= TryDiscoverConstellationForTrigger(triggerKey, preferCheapestConstellation);
                    }
                    return changed;

                case DiscoveryTargetModeConstellationsOnly:
                    if (!hasEligibleUndiscoveredConstellations) {
                        Logger.LogInfo($"{GetDiscoveryTriggerName(triggerKey)}: No eligible undiscovered constellations are available.");
                        return false;
                    }
                    if (!RandomHelper.Chance(constellationDiscoveryChance)) {
                        Logger.LogInfo($"{GetDiscoveryTriggerName(triggerKey)}: Constellation discovery chance missed ({constellationDiscoveryChance}%).");
                        return false;
                    }
                    return TryDiscoverConstellationForTrigger(triggerKey, preferCheapestConstellation);

                case DiscoveryTargetModeConstellationsOnlyWithMoonFallback:
                    if (hasUndiscoveredConstellations) {
                        if (!hasEligibleUndiscoveredConstellations) {
                            Logger.LogInfo($"{GetDiscoveryTriggerName(triggerKey)}: Undiscovered constellations remain story-locked or otherwise ineligible. Moon fallback is blocked.");
                            return false;
                        }
                        if (!RandomHelper.Chance(constellationDiscoveryChance)) {
                            Logger.LogInfo($"{GetDiscoveryTriggerName(triggerKey)}: Constellation discovery chance missed ({constellationDiscoveryChance}%). Moon fallback is blocked until all constellations are discovered.");
                            return false;
                        }
                        return TryDiscoverConstellationForTrigger(triggerKey, preferCheapestConstellation);
                    }

                    return moonCandidates.Count > 0 && moonDiscoveryAction(moonCandidates);

                default:
                    Logger.LogWarning($"{GetDiscoveryTriggerName(triggerKey)}: Unknown target mode '{targetMode}'. Falling back to moon discoveries only.");
                    return moonCandidates.Count > 0 && moonDiscoveryAction(moonCandidates);
            }
        }

        private bool QuotaDiscovery(List<LMUnlockable> candidates) {
            var quotaDiscoveries = candidates;
            if (ConfigManager.CheapMoonBiasQuotaDiscovery) {
                quotaDiscoveries = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(quotaDiscoveries, ConfigManager.CheapMoonBiasQuotaDiscoveryValue), ConfigManager.QuotaDiscoveryCount);
            } else {
                quotaDiscoveries = RandomHelper.Select(quotaDiscoveries, ConfigManager.QuotaDiscoveryCount);
            }
            if (quotaDiscoveries.Count == 0) {
                Logger.LogInfo($"No moons for Quota Discovery available!");
                return false;
            }
            ApplyMoonDiscoveries(quotaDiscoveries, ConfigManager.QuotaDiscoveryPermanent);
            NotificationHelper.SendChatMessage($"{quotaDiscoveries.Count.SinglePluralWord("Discovery")} granted:\n<color=white>{string.Join(", ", quotaDiscoveries.Select(ndd => ndd.Name))}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"{quotaDiscoveries.Count.SinglePluralWord("Discovery")} granted!", Text = $"Received coordinates for:\n{string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaDiscovery", ExceptWhenKey = "LMU_NewQuotaDiscoveryGroup" });
            Logger.LogInfo($"New Quota Discoveries: {string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}");
            return true;
        }

        private bool QuotaDiscoveryGroup(List<LMUnlockable> candidates) {
            var quotaDiscoveries = candidates;
            List<LMUnlockable> discoveryGroup = new List<LMUnlockable>();
            LMGroup group = new LMGroup();
            foreach (var candidate in quotaDiscoveries.OrderBy(c => c.OriginalPrice)) {
                Logger.LogInfo($"Got cheapest candidate: {candidate.Name}");
                Logger.LogInfo($"Checking for groups..");
                group = MatchMoonGroup(candidate, quotaDiscoveries, false);
                if (group.Members.Count > 0) {
                    foreach (var member in group.Members) {
                        if (!IsGloballyDiscovered(member)) {
                            discoveryGroup.Add(member);
                        }
                    }
                    break;
                } else {
                    Logger.LogInfo("Candidate has no group matches. Try next..");
                }
            }
            if (group.Members.Count <= discoveryGroup.Count) {
                NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Loyalty reward!", Text = $"The company facilitates your missions. Route to: <color=red>{group.Name}</color> established.", Key = "LMU_NewQuotaDiscoveryGroup" });
            }
            if (discoveryGroup.Count < 1 && ConfigManager.QuotaDiscoveryCheapestGroupFallback) {
                Logger.LogInfo($"Couldn't match any moons for cheapest group. Fallback to all moons..");
                discoveryGroup = quotaDiscoveries;
                // reset group to reset name for chat message
                group = new LMGroup();
            }
            if (ConfigManager.CheapMoonBiasQuotaDiscovery) {
                discoveryGroup = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(discoveryGroup, ConfigManager.CheapMoonBiasQuotaDiscoveryValue), ConfigManager.QuotaDiscoveryCount);
            } else {
                discoveryGroup = RandomHelper.Select(discoveryGroup, ConfigManager.QuotaDiscoveryCount);
            }
            if (discoveryGroup.Count == 0) {
                Logger.LogInfo($"No moons for Quota Discovery available!");
                return false;
            }
            ApplyMoonDiscoveries(discoveryGroup, ConfigManager.QuotaDiscoveryPermanent);
            string messageGroupname = string.Empty;
            if (group.Name != string.Empty) {
                messageGroupname = $" in <color=red>{group.Name}</color>";
            }
            NotificationHelper.SendChatMessage($"{discoveryGroup.Count.SinglePluralWord("Discovery")} granted{messageGroupname}:\n<color=white>{string.Join(", ", discoveryGroup.Select(qd => qd.Name))}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"{quotaDiscoveries.Count.SinglePluralWord("Discovery")} granted!", Text = $"Received coordinates for:\n{string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaDiscovery", ExceptWhenKey = "LMU_NewQuotaDiscoveryGroup" });
            Logger.LogInfo($"New Quota Discoveries: {string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}");
            return true;
        }

        private void QuotaUnlock() {
            List<LMUnlockable> quotaUnlocks = GetQuotaRewardMoonCandidates().Where(unlock => unlock.RoutePrice > 0).ToList();
            if (ConfigManager.DiscoveryMode) {
                quotaUnlocks = quotaUnlocks.Where(IsGloballyDiscovered).ToList();
            }
            if (ConfigManager.QuotaUnlockMaxPrice > 0) {
                quotaUnlocks = quotaUnlocks.Where(moon => moon.RoutePrice <= ConfigManager.QuotaUnlockMaxPrice).ToList();
            }
            if (ConfigManager.CheapMoonBiasQuotaUnlock) {
                quotaUnlocks = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(quotaUnlocks, ConfigManager.CheapMoonBiasQuotaUnlockValue), ConfigManager.QuotaUnlockCount);
            } else {
                quotaUnlocks = RandomHelper.Select(quotaUnlocks, ConfigManager.QuotaUnlockCount);
            }
            if (quotaUnlocks.Count == 0) {
                Logger.LogInfo($"No moons for Quota Unlock available!");
                return;
            }
            foreach (var unlock in quotaUnlocks) {
                unlock.BuyCount++;
                unlock.FreeVisitCount = 1;
                unlock.IterateState();
            }
            QuotaUnlocksCount++;
            if (quotaUnlocks.Count > 1) {
                NotificationHelper.SendChatMessage($"New moons unlocked:\n<color=green>{string.Join(", ", quotaUnlocks.Select(unlock => unlock.Name))}</color>");
            } else if (quotaUnlocks.Count == 1) {
                NotificationHelper.SendChatMessage($"New moon unlocked:\n<color=green>{quotaUnlocks.FirstOrDefault()?.Name}</color>");
            }
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"{quotaUnlocks.Count.SinglePluralWord("Unlock")} granted!", Text = $"You earned unlocks for:\n{string.Join(", ", quotaUnlocks.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaUnlock" });
            Logger.LogInfo($"New Quota Unlocks: {string.Join(", ", quotaUnlocks.Select(unlock => unlock.Name))}");
        }

        private void QuotaDiscount() {
            var quotaDiscounts = GetQuotaRewardMoonCandidates().Where(unlock => unlock.RoutePrice > 0).ToList();
            if (ConfigManager.DiscoveryMode) {
                quotaDiscounts = quotaDiscounts.Where(IsGloballyDiscovered).ToList();
            }
            if (ConfigManager.QuotaDiscountMaxPrice > 0) {
                quotaDiscounts = quotaDiscounts.Where(moon => moon.RoutePrice <= ConfigManager.QuotaDiscountMaxPrice).ToList();
            }

            if (ConfigManager.CheapMoonBiasQuotaDiscount) {
                quotaDiscounts = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(quotaDiscounts, ConfigManager.CheapMoonBiasQuotaDiscountValue), ConfigManager.QuotaDiscountCount);
            } else {
                quotaDiscounts = RandomHelper.Select(quotaDiscounts, ConfigManager.QuotaDiscountCount);
            }
            if (quotaDiscounts.Count == 0) {
                Logger.LogInfo($"No moons for Quota Discount available!");
                return;
            }
            foreach (var discount in quotaDiscounts) {
                discount.BuyCount++;
                discount.IterateState();
            }
            QuotaDiscountsCount++;
            if (quotaDiscounts.Count == 1) NotificationHelper.SendChatMessage($"Discount granted:\n<color=green>{quotaDiscounts.First().Name}</color>");
            else if (quotaDiscounts.Count > 1) NotificationHelper.SendChatMessage($"Discounts granted:\n<color=green>{string.Join(", ", quotaDiscounts.Select(unlock => unlock.Name))}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"{quotaDiscounts.Count.SinglePluralWord("Discount")} granted!", Text = $"You earned discounts for:\n{string.Join(", ", quotaDiscounts.Select(discount => discount.Name + " " + Plugin.GetDiscountPercentOff(discount.BuyCount) + "%"))}", Key = "LMU_NewQuotaDiscount" });
            Logger.LogInfo($"New Quota Discounts: {string.Join(", ", quotaDiscounts.Select(unlock => unlock.Name))}");
        }

        private void QuotaFullDiscount() {
            List<LMUnlockable> quotaFullDiscounts = GetQuotaRewardMoonCandidates().Where(unlock => unlock.RoutePrice > 0).ToList();
            if (ConfigManager.DiscoveryMode) {
                quotaFullDiscounts = quotaFullDiscounts.Where(IsGloballyDiscovered).ToList();
            }
            if (ConfigManager.QuotaFullDiscountMaxPrice > 0) {
                quotaFullDiscounts = quotaFullDiscounts.Where(moon => moon.RoutePrice <= ConfigManager.QuotaFullDiscountMaxPrice).ToList();
            }
            if (ConfigManager.Discounts[ConfigManager.Discounts.Count - 1] < 100) {
                quotaFullDiscounts = quotaFullDiscounts.Where(unlock => unlock.BuyCount < ConfigManager.DiscountsCount).ToList();
            }
            if (ConfigManager.CheapMoonBiasQuotaFullDiscount) {
                quotaFullDiscounts = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(quotaFullDiscounts, ConfigManager.CheapMoonBiasQuotaFullDiscountValue), ConfigManager.QuotaFullDiscountCount);
            } else {
                quotaFullDiscounts = RandomHelper.Select(quotaFullDiscounts, ConfigManager.QuotaFullDiscountCount);
            }
            if (quotaFullDiscounts.Count == 0) {
                Logger.LogInfo($"No moons for Quota Full Discount available!");
                return;
            }
            foreach (var fullDiscount in quotaFullDiscounts) {
                fullDiscount.BuyCount = ConfigManager.DiscountsCount;
                fullDiscount.FreeVisitCount = 1;
                fullDiscount.IterateState();
            }
            QuotaFullDiscountsCount++;
            if (quotaFullDiscounts.Count == 1) NotificationHelper.SendChatMessage($"Full discount granted:\n<color=green>{quotaFullDiscounts.First().Name}</color>");
            else if (quotaFullDiscounts.Count > 1) NotificationHelper.SendChatMessage($"Full discounts granted:\n<color=green>{string.Join(", ", quotaFullDiscounts.Select(unlock => unlock.Name))}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $" Full {quotaFullDiscounts.Count.SinglePluralWord("Discount")} granted!", Text = $"You earned full discounts for:\n{string.Join(", ", quotaFullDiscounts.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaFullDiscount" });
            Logger.LogInfo($"New Quota Full Discounts: {string.Join(", ", quotaFullDiscounts.Select(unlock => unlock.Name))}");
        }

        private bool NewDayDiscovery(List<LMUnlockable> candidates) {
            Logger.LogInfo($"New Day Discovery Candidates: {string.Join(", ", candidates.Select(unlock => unlock.Name))}");

            var currentLevelUnlock = Unlocks.FirstOrDefault(unlock => unlock.ExtendedLevel.NumberlessPlanetName == LevelManager.CurrentExtendedLevel.NumberlessPlanetName);
            List<LMUnlockable> newDayDiscoveries;
            List<LMUnlockable> nddCandidates = candidates;
            string ndDiscoveryGroupName = "nearby";
            if (ConfigManager.NewDayDiscoveryMatchGroup && currentLevelUnlock != null) {
                LMGroup moonGroup = MatchMoonGroup(currentLevelUnlock, candidates, ConfigManager.NewDayDiscoveryMatchGroupFallback);
                nddCandidates = moonGroup.Members;
                if (!string.IsNullOrEmpty(moonGroup.Name)) ndDiscoveryGroupName = $"in <color=red>{moonGroup.Name}</color>";
            }
            if (nddCandidates.Count < 1) {
                Logger.LogInfo($"No discoverable moons found!");
                return false;
            }
            if (ConfigManager.CheapMoonBiasNewDayDiscovery) {
                newDayDiscoveries = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(nddCandidates, ConfigManager.CheapMoonBiasNewDayDiscoveryValue), ConfigManager.NewDayDiscoveryCount);
            } else {
                newDayDiscoveries = RandomHelper.Select(nddCandidates, ConfigManager.NewDayDiscoveryCount);
            }
            ApplyMoonDiscoveries(newDayDiscoveries, ConfigManager.NewDayDiscoveryPermanent);
            if (newDayDiscoveries.Count == 1) {
                NotificationHelper.SendChatMessage($"Autopilot discovered moon suitable for landing {ndDiscoveryGroupName}:\n<color=white>{newDayDiscoveries.First().Name}</color>");
                Logger.LogInfo($"New Day Discoveries: [ {string.Join(", ", newDayDiscoveries.Select(discovery => discovery.Name))} ]");
            }
            if (newDayDiscoveries.Count > 1) {
                NotificationHelper.SendChatMessage($"Autopilot discovered moons suitable for landing {ndDiscoveryGroupName}:\n<color=white>{string.Join(", ", newDayDiscoveries.Select(ndd => ndd.Name))}</color>");
                Logger.LogInfo($"New Day Discovery: [ {string.Join(", ", newDayDiscoveries.Select(discovery => discovery.Name))} ]");
            }
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"New Day {newDayDiscoveries.Count.SinglePluralWord("Discovery")}!", Text = $"Autopilot discovered new {newDayDiscoveries.Count.SinglePluralWord("moon")} {ndDiscoveryGroupName}.\n" +
                $"Moon catalog updated!", Key = "LMU_NewDayDiscovery" });
            Logger.LogInfo($"New Day Discoveries: {string.Join(", ", newDayDiscoveries.Select(unlock => unlock.Name))}");
            return true;
        }

        private bool TravelDiscovery(LMUnlockable unlock, List<LMUnlockable> candidates) {
            Logger.LogInfo($"Travel Discovery Candidates: {string.Join(", ", candidates.Select(candidate => candidate.Name))}");
            List<LMUnlockable> tdCandidates = candidates;
            List<LMUnlockable> travelDiscoveries;
                
            string tdMessageGroupName = string.Empty;
            if (ConfigManager.TravelDiscoveryMatchGroup) {
                LMGroup moonGroup = MatchMoonGroup(unlock, candidates, ConfigManager.TravelDiscoveryMatchGroupFallback);
                tdCandidates = moonGroup.Members;
                if (!string.IsNullOrEmpty(moonGroup.Name)) tdMessageGroupName = $" to <color=red>{moonGroup.Name}</color>";
            }
            if (tdCandidates.Count < 1) {
                Logger.LogInfo($"No discoverable moons found!");
                return false;
            }
            if (ConfigManager.CheapMoonBiasTravelDiscovery) {
                travelDiscoveries = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(tdCandidates, ConfigManager.CheapMoonBiasTravelDiscoveryValue), ConfigManager.TravelDiscoveryCount);
            } else {
                travelDiscoveries = RandomHelper.Select(tdCandidates, ConfigManager.TravelDiscoveryCount);
            }

            ApplyMoonDiscoveries(travelDiscoveries, ConfigManager.TravelDiscoveryPermanent);

            if (travelDiscoveries.Count > 1) {
                NotificationHelper.SendChatMessage($"Discovered new moons on route{tdMessageGroupName}:\n<color=white>{string.Join(", ", travelDiscoveries.Select(td => td.Name))}</color>");
            } else if (travelDiscoveries.Count == 1) {
                NotificationHelper.SendChatMessage($"Discovered new moon on route{tdMessageGroupName}:\n<color=white>{travelDiscoveries.First().Name}</color>");
            }
            Logger.LogInfo($"Travel Discovery: [ {string.Join(", ", travelDiscoveries.Select(discovery => discovery.Name))} ]");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"New {travelDiscoveries.Count.SinglePluralWord("Discovery")}!", Text = $"Autopilot discovered new {travelDiscoveries.Count.SinglePluralWord("moon")} during travel{tdMessageGroupName}.\n" +
                $"Moon catalog updated!", Key = "LMU_TravelDiscovery" });
            Logger.LogInfo($"Travel Discoveries: {string.Join(", ", travelDiscoveries.Select(u => u.Name))}");
            return true;
        }

        private LMGroup MatchMoonGroup(LMUnlockable matchingUnlock, List<LMUnlockable> unlocksToMatch, bool fallback) {
            Logger.LogDebug($"Matching moon {matchingUnlock.Name}: Matching against = [ {string.Join(", ", unlocksToMatch.Select(unlock => unlock.Name))} ]");
            switch (ConfigManager.MoonGroupMatchingMethod) {
                case "Price":
                    List<LMUnlockable> priceMatches = new List<LMUnlockable>();
                    foreach (var unlock in unlocksToMatch) {
                        if (unlock.OriginalPrice == matchingUnlock.OriginalPrice) {
                            priceMatches.Add(unlock);
                        }
                    }
                    if (priceMatches.Count > 0) {
                        Logger.LogInfo($"Matching moon {matchingUnlock.Name}: Matches by price = [ {string.Join(", ", priceMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Members = priceMatches };
                    } else {
                        break;
                    }
                case "PriceRange":
                    List<LMUnlockable> pricerangeMatches = new List<LMUnlockable>();
                    foreach (var unlock in unlocksToMatch) {
                        if (unlock.OriginalPrice >= matchingUnlock.OriginalPrice - ConfigManager.MoonGroupMatchingPriceRange && unlock.OriginalPrice <= matchingUnlock.OriginalPrice + ConfigManager.MoonGroupMatchingPriceRange) {
                            pricerangeMatches.Add(unlock);
                        }
                    }
                    if (pricerangeMatches.Count > 0) {
                        Logger.LogInfo($"Matching moon {matchingUnlock.Name}: Matches by price range = [ {string.Join(", ", pricerangeMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Members = pricerangeMatches };
                    } else {
                        break;
                    }
                case "PriceRangeUpper":
                    List<LMUnlockable> pricerangeUpperMatches = new List<LMUnlockable>();
                    foreach (var unlock in unlocksToMatch) {
                        if (unlock.OriginalPrice >= matchingUnlock.OriginalPrice && unlock.OriginalPrice <= matchingUnlock.OriginalPrice + ConfigManager.MoonGroupMatchingPriceRange) {
                            pricerangeUpperMatches.Add(unlock);
                        }
                    }
                    if (pricerangeUpperMatches.Count > 0) {
                        Logger.LogInfo($"Matching moon {matchingUnlock.Name}: Matches by price range = [ {string.Join(", ", pricerangeUpperMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Members = pricerangeUpperMatches };
                    } else {
                        break;
                    }
                case "Tag":
                    List<LMUnlockable> tagMatches = new List<LMUnlockable>();
                    List<ContentTag> matchingTags = matchingUnlock.ExtendedLevel.ContentTags;
                    ContentTag randomTag = matchingTags[RandomHelper.Range(0, matchingTags.Count)];
                    foreach (var unlock in unlocksToMatch) {
                        if (unlock.ExtendedLevel.ContentTags.Select(tag => tag.contentTagName.ToLower()).Contains(randomTag.contentTagName.ToLower()) && !tagMatches.Contains(unlock))
                            tagMatches.Add(unlock);
                    }
                    if (tagMatches.Count > 0) {
                        Logger.LogInfo($"Matching moon {matchingUnlock.Name}: Matches by LLL tags = [ {string.Join(", ", tagMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Members = tagMatches };
                    } else {
                        break;
                    }
                case "Custom":
                    Dictionary<string, List<string>> matchingCustomGroups = matchingUnlock.GetMatchingCustomGroups();
                    if (matchingCustomGroups == null || matchingCustomGroups.Count == 0)
                        break;
                    string randomCustomGroupName = matchingCustomGroups.Keys.ToList()[RandomHelper.Range(0, matchingCustomGroups.Count)];
                    if (matchingCustomGroups.Count > 1) {
                        Logger.LogInfo($"Matching moon {matchingUnlock.Name}: Moon is member of multiple groups. Selected {randomCustomGroupName} for matching.");
                    }
                    List<string> randomCustomGroup = matchingCustomGroups[randomCustomGroupName];
                    Logger.LogInfo($"Matching moon {matchingUnlock.Name}: {randomCustomGroupName} members = [ {string.Join(", ", randomCustomGroup)} ]");
                    List<LMUnlockable> groupMatches = new List<LMUnlockable>();
                    foreach (var unlock in unlocksToMatch) {
                        if (randomCustomGroup.Contains(unlock.Name)) {
                            groupMatches.Add(unlock);
                        }
                    }
                    if (groupMatches.Count > 0) {
                        Logger.LogInfo($"Matching moon {matchingUnlock.Name}: Matched by custom group [{randomCustomGroupName}]; Matches = [ {string.Join(", ", groupMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Name = randomCustomGroupName, Members = groupMatches };
                    } else {
                        break;
                    }
                default:
                    Logger.LogError($"Missing moon group matching method!");
                    break;
            }
            Logger.LogInfo($"No matching moons found!");
            if (fallback) {
                return new LMGroup() { Members = unlocksToMatch };
            } else {
                return new LMGroup();
            }
        }

        private void InitializeNewGame() {
            Logger.LogInfo($"New game initialization..");

            InitializeBuiltInStoryLocks();

            if (ConfigManager.EnableStoryProgression) {
                CollectStoryLockedMoons();
            }
            
            if (ConfigManager.DiscoveryMode) {
                ShuffleDiscoverable(suppressNewDiscovery: true);
            }

            // Shuffle Moon Sales
            if (ConfigManager.Sales) {
                RefreshSales();
            }
        }

        private void InitializeBuiltInStoryLocks() {
            if (!ConfigManager.EnableStoryProgression) {
                return;
            }

            if (ConfigManager.LMUStoryProgression) {
                ForceStoryLockAtStartup("Artifice");
                ForceStoryLockAtStartup("Embrion");
            }

            if (ConfigManager.GaletryStoryLock && AllLevels.Any(level => level.NumberlessPlanetName == "Galetry")) {
                ForceStoryLockAtStartup("Galetry");
            }
        }

        private void ForceStoryLockAtStartup(string numberlessPlanetName) {
            var unlock = Unlocks.FirstOrDefault(candidate => candidate.Name == numberlessPlanetName);
            if (unlock == null) {
                Logger.LogWarning($"{numberlessPlanetName}: Unable to force startup story lock because the moon was not found.");
                return;
            }

            unlock.ForceStoryLockAtStartup();
            Logger.LogInfo($"{unlock.Name}: Forced built-in story lock startup state (hidden + locked).");
        }

        private void ShuffleDiscoverable(bool suppressNewDiscovery = false) {
            Logger.LogInfo("Shuffling discovered moon rotations.. ");

            // Reset rotation
            foreach (var candidate in Unlocks.Where(unlock => unlock.OriginallyHidden == false && unlock.OriginallyLocked == false && !unlock.PermanentlyDiscovered)) {
                if (candidate.NewDiscovery) candidate.NewDiscovery = false;
                candidate.Discovered = false;
            }
            RefreshDiscoveryRotationCounts();
            
            // select new rotation
            ApplyDiscoveryWhitelist();
            if (UseConstellationDiscovery) {
                Plugin.ConstellationManager.ClearAllConstellationRotations();
                Plugin.ConstellationManager.ClearLocalMoonDiscoveries();
                Plugin.ConstellationManager.RegenerateAllConstellationRotations();
                Logger.LogInfo("Regenerated stored LethalConstellations moon rotations.");
                Plugin.ConstellationManager.ApplyCurrentConstellationVisibility(suppressNewDiscovery);
            } else {
                AddFreeToRotation(DiscoveredFreeCount);
                AddDynamicFreeToRotation(DiscoveredDynamicFreeCount);
                AddPaidToRotation(DiscoveredPaidCount);
            }

            // Make sure there's at least one moon discovered
            bool oneMoonDiscovered = Unlocks.Any(unlock => unlock.Discovered);
            if (oneMoonDiscovered) {
                RerouteShipToFreeMoon();
            } else {
                Logger.LogWarning("All moons would have been hidden from the terminal! Force discovering a free moon..");
                var unlock = Unlocks.Where(unlock => unlock.RoutePrice == 0).FirstOrDefault();
                if (unlock == null) {
                    Logger.LogWarning("Can't find any free moon to display in moon catalog! You probably want at least one free moon available at all times.. Falling back to a paid moon!");
                    unlock = Unlocks.FirstOrDefault();
                } 
                if (unlock == null) {
                    Logger.LogError("Can't find any moon! No unlockable data initialized. Please check your configs (LMU + LLL). If this persists report it on GitHub or Discord.");
                    return;
                } else {
                    if (UseConstellationDiscovery) {
                        unlock.SetDiscoveryState(true, suppressNewDiscovery);
                    } else {
                        unlock.Discovered = true;
                        if (suppressNewDiscovery) {
                            unlock.DiscoveredOnce = true;
                            unlock.NewDiscovery = false;
                        }
                    }
                    RerouteShipToFreeMoon();
                }
            }
            if (DayCount > 0) {
                NotificationHelper.SendChatMessage("Moon catalog updated!");
                NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Moon catalog updated!", Text = $"New moons available. Use the computer terminal to route the ship.", Key = "LMU_Shuffle" });
            }
        }

        private void AddFreeToRotation(int amount) {
            var freeMoons = RandomHelper.Select(DiscoveryFreeCandidates, amount);
            Logger.LogInfo($"New free rotation: [ {string.Join(", ", freeMoons.Select(moon => moon.Name))} ]");
            foreach (var candidate in freeMoons) {
                candidate.Discovered = true;
            }
        }

        private void AddDynamicFreeToRotation(int amount) {
            var dynamicFreeMoons = RandomHelper.Select(DiscoveryDynamicFreeCandidates, amount);
            Logger.LogInfo($"New dynamic free rotation: [ {string.Join(", ", dynamicFreeMoons.Select(moon => moon.Name))} ]");
            foreach (var candidate in dynamicFreeMoons) {
                candidate.Discovered = true;
            }
        }
        
        private void AddPaidToRotation(int amount) {
            List<LMUnlockable> paidMoons;
            if (ConfigManager.CheapMoonBiasPaidRotation) {
                paidMoons = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(DiscoveryPaidCandidates, ConfigManager.CheapMoonBiasPaidRotationValue),amount);
            }
            else {
                paidMoons = RandomHelper.Select(DiscoveryPaidCandidates, amount);
            }
            Logger.LogInfo($"New paid rotation: [ {string.Join(", ", paidMoons.Select(moon => moon.Name))} ]");
            foreach (var candidate in paidMoons) {
                candidate.Discovered = true;
            }
        }

        private void RerouteShipToFreeMoon() {
            Logger.LogInfo($"After shuffling check if we have to reroute to a discovered safe destination..");
            if (Unlocks.Any(unlock => (unlock.Discovered || unlock.PermanentlyDiscovered ) && unlock.Name == LevelManager.CurrentExtendedLevel.NumberlessPlanetName) || LevelManager.CurrentExtendedLevel.NumberlessPlanetName == "Gordion") {
                Logger.LogInfo($"Current moon is discovered. Not rerouting ship.");
            } else {
                ExtendedLevel rerouteDestination;
                if (UseConstellationDiscovery) {
                    var currentDiscoveredFreeMoons = Plugin.ConstellationManager.GetCurrentVisibleUnlocks()
                        .Where(unlock => !unlock.OriginallyLocked && !unlock.OriginallyHidden && (unlock.Discovered || unlock.PermanentlyDiscovered) && unlock.RoutePrice == 0)
                        .ToList();
                    if (currentDiscoveredFreeMoons.Count > 0) {
                        rerouteDestination = currentDiscoveredFreeMoons[RandomHelper.Range(0, currentDiscoveredFreeMoons.Count)].ExtendedLevel;
                    } else if (Plugin.ConstellationManager.TryGetCurrentDefaultMoon(out var defaultMoon)
                        && defaultMoon?.ExtendedLevel
                        && (defaultMoon.Discovered || defaultMoon.PermanentlyDiscovered)) {
                        rerouteDestination = defaultMoon.ExtendedLevel;
                        Logger.LogInfo($"No free discovered moon is available in the current constellation. Falling back to default moon '{defaultMoon.Name}'.");
                    } else {
                        Logger.LogWarning("Can't find any free discovered moon in the current constellation, and no discovered default moon fallback is available. Abort auto routing ship!");
                        return;
                    }
                } else {
                    var currentDiscoveredFreeMoons = DynamicFreeMoons.Where(unlock => !unlock.OriginallyLocked && !unlock.OriginallyHidden && (unlock.Discovered || unlock.PermanentlyDiscovered)).ToList();
                    if (currentDiscoveredFreeMoons.Count < 1) {
                        Logger.LogWarning("Can't find any free and discovered moon! You probably want at least one free moon available at all times.. Abort auto routing ship!");
                        return;
                    }

                    rerouteDestination = currentDiscoveredFreeMoons[RandomHelper.Range(0, currentDiscoveredFreeMoons.Count)].ExtendedLevel;
                }
                Logger.LogInfo($"Current moon is not discovered! Rerouting ship to {rerouteDestination.NumberlessPlanetName}..");
                if (DayCount > 0) {
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Dangerous conditions!", Text = $"Conditions too dangerous to stay in orbit! Auto routing ship to a safe moon..", Key = "LMU_RerouteFree" });
                }
                DelayHelper.Instance.ExecuteAfterDelay(() => { StartOfRound.Instance.ChangeLevelServerRpc(rerouteDestination.SelectableLevel.levelID, Terminal.groupCredits); }, 3.5f);
            }
        }

        private void ApplyDiscoveryWhitelist() {
            if (ConfigManager.DiscoveryMode && ConfigManager.DiscoveryWhitelistMoons.Count > 0) {
                Logger.LogInfo($"Whitelist: {string.Join(", ", ConfigManager.DiscoveryWhitelistMoons)}");
                foreach (var entry in ConfigManager.DiscoveryWhitelistMoons) {
                    bool matched = false;
                    foreach (var unlock in Unlocks) {
                        if (unlock.Name.Contains(entry.Trim(), StringComparison.OrdinalIgnoreCase)) {
                            matched = true;
                            if (UseConstellationDiscovery) {
                                unlock.PermanentlyDiscovered = true;
                                unlock.DiscoveredOnce = true;
                                unlock.NewDiscovery = false;
                                Logger.LogDebug($"Whitelist entry set to permanently discovered: {entry}");
                            } else {
                                unlock.Discovered = true;
                                Logger.LogDebug($"Whitelist entry set to discovered: {entry}");
                            }
                            break;
                        }
                    }
                    if (!matched) Logger.LogWarning($"Couldn't match whitelist entry! Is this a valid moon name: {entry} ?");
                }
            }
        }

        private bool ShouldForceDiscoveryShuffleOnLoad(bool loadSuccess) {
            if (!loadSuccess || !ConfigManager.DiscoveryMode) {
                return false;
            }

            if (UseConstellationDiscovery) {
                return false;
            }

            return Unlocks.All(unlock => !unlock.Discovered);
        }

        private bool IsGloballyDiscovered(LMUnlockable unlock) {
            if (unlock == null) {
                return false;
            }

            if (!UseConstellationDiscovery) {
                return unlock.Discovered || unlock.PermanentlyDiscovered;
            }

            return unlock.PermanentlyDiscovered || Plugin.ConstellationManager.IsDerivedVisibleAnywhere(unlock);
        }

        private void RefreshDiscoveryRotationCounts() {
            if (ConfigManager.DiscoveryFreeCountIncreaseBy > 0) {
                var newFreeCount = ConfigManager.DiscoveryFreeCountBase + (ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryFreeCountIncreaseBy;
                DiscoveredFreeCount = newFreeCount;
                Logger.LogInfo($"Increasing DiscoverFreeCount (Base = {ConfigManager.DiscoveryFreeCountBase}, Increase = {(ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryFreeCountIncreaseBy}, Result = {newFreeCount}, Corrected = {DiscoveredFreeCount})");
            } else {
                DiscoveredFreeCount = ConfigManager.DiscoveryFreeCountBase;
                Logger.LogInfo($"DiscoveredFreeCount (Config value = {ConfigManager.DiscoveryFreeCountBase}, Corrected = {DiscoveredFreeCount})");
            }

            if (ConfigManager.DiscoveryDynamicFreeCountIncreaseBy > 0 ) {
                var newDynamicFreeCount = ConfigManager.DiscoveryDynamicFreeCountBase + (ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryDynamicFreeCountIncreaseBy;
                DiscoveredDynamicFreeCount = newDynamicFreeCount;
                Logger.LogInfo($"Increasing DiscoveredDynamicFreeCount (Base = {ConfigManager.DiscoveryDynamicFreeCountBase}, Increase = {(ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryDynamicFreeCountIncreaseBy}, Result = {newDynamicFreeCount}, Corrected = {DiscoveredDynamicFreeCount})");
            } else {
                DiscoveredDynamicFreeCount = ConfigManager.DiscoveryDynamicFreeCountBase;
                Logger.LogInfo($"DiscoveredDynamicFreeCount (Config value = {ConfigManager.DiscoveryDynamicFreeCountBase}, Corrected = {DiscoveredDynamicFreeCount})");
            }

            if (ConfigManager.DiscoveryPaidCountIncreaseBy > 0) {
                var newPaidCount = ConfigManager.DiscoveryPaidCountBase + (ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryPaidCountIncreaseBy;
                DiscoveredPaidCount = newPaidCount;
                Logger.LogInfo($"Increasing DiscoveredPaidCount (Base = {ConfigManager.DiscoveryPaidCountBase}, Increase = {(ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryPaidCountIncreaseBy}, Result = {newPaidCount}, Corrected = {DiscoveredPaidCount})");
            } else {
                DiscoveredPaidCount = ConfigManager.DiscoveryPaidCountBase;
                Logger.LogInfo($"DiscoveredPaidCount (Config value = {ConfigManager.DiscoveryPaidCountBase}, Corrected = {DiscoveredPaidCount})");
            }
        }

        private bool LoadAndImportSavaData() {
            Dictionary<string, object> savedata = SaveManager.Savedata;
            ProgressionManager.Instance?.Reset();
            if (savedata != null && (
                savedata.ContainsKey("LMU_Unlockables")
                || savedata.ContainsKey("LMU_QuotaCount")
                || savedata.ContainsKey("LMU_DayCount")
                || savedata.ContainsKey("LMU_QuotaUnlocksCount")
                || savedata.ContainsKey("LMU_QuotaDiscountsCount")
                || savedata.ContainsKey("LMU_QuotaFullDiscountsCount")
                || savedata.ContainsKey("LMU_Progression")
                || (savedata.ContainsKey("LMU_LethalConstellations") && Plugin.LethalConstellationsPresent && Plugin.LethalConstellationsExtension != null))) {
                Logger.LogInfo($"LMU save data detected!");
                Logger.LogInfo($"Loading LMU data from save..");
                if (savedata.ContainsKey("LMU_Unlockables")) {
                    ImportUnlockableData((List<LMUnlockable>)savedata["LMU_Unlockables"]);
                }
                if (savedata.ContainsKey("LMU_QuotaCount")) {
                    QuotaCount = (int)savedata["LMU_QuotaCount"];
                    Logger.LogInfo($"Loading QuotaCount: {QuotaCount}.");
                }
                if (savedata.ContainsKey("LMU_DayCount")) {
                    DayCount = (int)savedata["LMU_DayCount"];
                    Logger.LogInfo($"Loading DayCount: {DayCount}.");
                }
                if (savedata.ContainsKey("LMU_QuotaUnlocksCount")) {
                    QuotaUnlocksCount = (int)savedata["LMU_QuotaUnlocksCount"];
                    Logger.LogInfo($"Loading QuotaUnlocksCount: {QuotaUnlocksCount}.");
                }
                if (savedata.ContainsKey("LMU_QuotaDiscountsCount")) {
                    QuotaDiscountsCount = (int)savedata["LMU_QuotaDiscountsCount"];
                    Logger.LogInfo($"Loading QuotaDiscountsCount: {QuotaDiscountsCount}.");
                }
                if (savedata.ContainsKey("LMU_QuotaFullDiscountsCount")) {
                    QuotaFullDiscountsCount = (int)savedata["LMU_QuotaFullDiscountsCount"];
                    Logger.LogInfo($"Loading QuotaFullDiscountsCount: {QuotaFullDiscountsCount}.");
                }
                if (savedata.ContainsKey("LMU_Progression")) {
                    ProgressionManager.Instance?.LoadSaveData((ProgressionSaveData)savedata["LMU_Progression"]);
                    Logger.LogInfo($"Loading ProgressionManager state: PaintingsSold={ProgressionManager.Instance?.PaintingsSold}, BestiaryReads={ProgressionManager.Instance?.ReadBestiaryEntries.Count ?? 0}, StoryLogReads={ProgressionManager.Instance?.ReadStoryLogs.Count ?? 0}.");
                }
                if (Plugin.LethalConstellationsPresent && Plugin.LethalConstellationsExtension != null) {
                    if (savedata.ContainsKey("LMU_LethalConstellations")) {
                        LethalConstellationsSaveData lethalConstellationsSaveData = (LethalConstellationsSaveData)savedata["LMU_LethalConstellations"];
                        Plugin.LethalConstellationsExtension.LoadSaveData(lethalConstellationsSaveData);
                        Plugin.ConstellationManager?.ReplaceLocalMoonDiscoveries(lethalConstellationsSaveData?.LocalConstellationDiscoveries);
                        Plugin.ConstellationManager?.ReplaceAllConstellationRotations(lethalConstellationsSaveData?.ConstellationRotationMoons);
                        Plugin.ConstellationManager?.ApplyConstellationState();
                        Logger.LogInfo($"Loading LethalConstellations state: {lethalConstellationsSaveData?.Constellations?.Count ?? 0} constellations, rotations={lethalConstellationsSaveData?.ConstellationRotationMoons?.Count ?? 0}, locals={lethalConstellationsSaveData?.LocalConstellationDiscoveries?.Count ?? 0}.");
                    } else {
                        Plugin.LethalConstellationsExtension.LoadSaveData(null);
                        Plugin.ConstellationManager?.ReplaceLocalMoonDiscoveries(null);
                        Plugin.ConstellationManager?.ReplaceAllConstellationRotations(null);
                        Plugin.ConstellationManager?.ApplyConstellationState();
                        Logger.LogInfo("No saved LethalConstellations progression found. Reinitialized constellation definitions.");
                    }
                }
                Logger.LogInfo($"Finished loading LMU save data.");
                return true;
            } else if (savedata != null &&  savedata.ContainsKey("LMU_UnlockedMoons")) {
                Logger.LogInfo($"Legacy LMU save data detected! Migrating..");
                Dictionary<string, int> unlockedMoon = (Dictionary<string, int>)savedata["LMU_UnlockedMoons"];
                foreach (var moon in unlockedMoon) {
                    foreach (var unlock in Unlocks) {
                        if (unlock.Name == moon.Key) {
                            unlock.BuyCount = moon.Value;
                            Logger.LogInfo($"Migrated unlock data for {moon.Key}: {moon.Value}.");
                        }
                    }
                }
                if (savedata.ContainsKey("LMU_QuotaCount")) {
                    QuotaCount = (int)savedata["LMU_QuotaCount"];
                    Logger.LogInfo($"Migrating QuotaCount: {QuotaCount}.");
                }
                Logger.LogInfo($"Finished migrating legacy LMU save data.");
                Logger.LogInfo($"Loading done. Applying migrated data before new game init..");
                // return false to run InitializeNewGame()
                return false;
            } else if (savedata != null &&  savedata.ContainsKey("UnlockedMoons")) {
                Logger.LogInfo($"Permanent Moons save data detected! Migrating..");
                List<string> pmMoons = (List<string>)savedata["UnlockedMoons"];
                foreach (var moon in pmMoons) {
                    foreach (var unlock in Unlocks) {
                        if (moon.Contains(unlock.Name, StringComparison.OrdinalIgnoreCase)) {
                            unlock.BuyCount = 1;
                            Logger.LogInfo($"Migrated PM unlock for {unlock.Name}.");
                        }
                    }
                }
                if (savedata.ContainsKey("MoonQuotaNum")) {
                    QuotaCount = (int)savedata["MoonQuotaNum"];
                    Logger.LogInfo($"Migrated PM MoonQuotaNum (QuotaCount): {QuotaCount}.");
                }
                Logger.LogInfo($"Finished migrating Permanent Moons save data.");
                Logger.LogInfo($"Loading done. Applying migrated data before new game init.");
                // return false to run InitializeNewGame()
                return false;
            } else {
                Logger.LogInfo($"No save data found! New save..");
                return false;
            }
        }

        private void Reset() {
            foreach (var unlock in Unlocks) {
                unlock.RestoreOriginalState();
            }
            Unlocks.Clear();
            QuotaCount = 0;
            DayCount = 0;
            QuotaUnlocksCount = 0;
            QuotaDiscountsCount = 0;
            QuotaFullDiscountsCount = 0;
            if (Plugin.LethalConstellationsPresent) {
                Plugin.LethalConstellationsExtension.Reset();
            }
        }

        private string ReplaceTerminalPreview(ExtendedLevel extendedLevel, PreviewInfoType infoType) {
            // override font size
            if (ConfigManager.TerminalFontSizeOverride) {
                Terminal.screenText.textComponent.fontSize = ConfigManager.TerminalFontSize;
            }
            var unlock = Unlocks.Where(unlock => unlock.ExtendedLevel == extendedLevel).FirstOrDefault();
            if (unlock == null) {
                Logger.LogError($"Couldn't get unlock for Terminal preview text replacement!");
                return string.Empty;
            }
            return unlock.GetMoonPreviewText(infoType);
        }

    }
}
