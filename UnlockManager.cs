using Dawn;
using Dawn.Utils;
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
        internal int QuotaCount { get; set; } = 0;
        internal int DayCount { get; set; } = 0;
        internal int QuotaUnlocksCount { get; set; } = 0;
        internal int QuotaDiscountsCount { get; set; } = 0;
        internal int QuotaFullDiscountsCount { get; set; } = 0;
        internal int DiscoveredFreeCount {
            get { return _discoveredFreeCount > DiscoveryFreeCandidates.Count ? DiscoveryFreeCandidates.Count : _discoveredFreeCount; }
            set { _discoveredFreeCount = value; }
        }
        private int _discoveredFreeCount;
        internal int DiscoveredDynamicFreeCount {
            get { return _discoveredDynamicFreeCount > DiscoveryDynamicFreeCandidates.Count ? DiscoveryDynamicFreeCandidates.Count : _discoveredDynamicFreeCount; }
            set { _discoveredDynamicFreeCount = value; }
        }
        private int _discoveredDynamicFreeCount;
        internal int DiscoveredPaidCount {
            get { return _discoveredPaidCount > DiscoveryPaidCandidates.Count ? DiscoveryPaidCandidates.Count : _discoveredPaidCount; }
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
                return Unlocks.Where( unlock => (
                    (!unlock.OriginallyLocked && !unlock.OriginallyHidden && !unlock.StoryUnlock
                    || (unlock.StoryUnlock && unlock.StoryIsUnlocked))
                    && !unlock.Discovered && !unlock.PermanentlyDiscovered)
                    ).ToList();
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

        public delegate List<string> DesignateStoryUnlocks();

        /// <summary>
        /// Occurs on lobby creation after <c>Terminal.Start</c> and optionally after being fired (user config).
        /// <br>Allows subscribers to designate moons that should be exclusively locked behind story progression.</br>
        /// <br></br>
        /// Subscribe with a method that returns a list of strings containing 'NumberlessPlanetName's of your moons.
        /// <br></br>
        /// <br></br>
        /// <example>For example:
        /// <code>
        /// UnlockManager.OnCollectStoryLockedMoons += MySubscriber;
        /// private List&lt;string&gt; MySubscriber() { return new List&lt;string&gt; { "Infernis", "Penumbra" } }
        /// </code>
        /// </example>
        /// <remarks>
        /// <br></br>
        /// When you want to release the story lock for your moon use 
        /// </remarks>
        /// <seealso cref="TryReleaseStoryLock(string)"/>
        /// </summary>
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
        /// <c>false</c> if the moon could not be found or wasn't designated to be locked behind story progression.
        /// </returns>
        public static bool TryReleaseStoryLock (string numberlessPlanetName) {
            if (!ConfigManager.EnableStoryProgression) {
                Logger.LogInfo("Received request to release story lock but story locks are ignored by user config.");
                return false;
            }
            var unlock = Instance?.Unlocks.FirstOrDefault(u => u.Name == numberlessPlanetName);
            if (unlock == null) {
                Logger.LogWarning("Received request to release story lock but the LMUnlockable associated with the level name was not found!");
                return false;
            } else if (!unlock.StoryUnlock) {
                Logger.LogWarning("Received request to release story lock but the LMUnlockable associated with the level name is not desiganted as story lock!");
                return false;
            }
            unlock.StoryIsUnlocked = true;
            Logger.LogInfo($"{unlock.Name}: Request to release story lock received! Releasing lock.. {unlock.Name} now available (for discovery).");
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
        /// <c>false</c> if the moon could not be found or wasn't designated to be locked behind story progression.
        /// </returns>
        public static bool TryReleaseStoryLockShowAlert (string numberlessPlanetName) {
            if (!ConfigManager.EnableStoryProgression) {
                Logger.LogInfo("Received request to release story lock but story locks are ignored by user config.");
                return false;
            }
            var unlock = Instance?.Unlocks.FirstOrDefault(u => u.Name == numberlessPlanetName);
            if (unlock == null) {
                Logger.LogWarning("Received request to release story lock but the LMUnlockable associated with the level name was not found!");
                return false;
            } else if (!unlock.StoryUnlock) {
                Logger.LogWarning("Received request to release story lock but the LMUnlockable associated with the level name is not designated as story lock!");
                return false;
            }
            unlock.StoryIsUnlocked = true;
            Logger.LogInfo($"{unlock.Name}: Request to release story lock received! Releasing lock.. {unlock.Name} now available (for discovery).");
            Instance?.IterateUnlocks();
            NetworkManager.Instance?.ServerSendUnlockables(Instance?.Unlocks);
            NetworkManager.Instance?.ServerSendAlertMessage(new Notification { Header = "Autopilot", Text = "Location data detected!\nQueued for processing.", Key = "LMU_StoryLockReleasedGeneric" });
            if (!ConfigManager.DiscoveryMode) {
                NetworkManager.Instance?.ServerSendAlertMessage(new Notification { Header = "Autopilot", Text = $"Success! New moon discovered:\n{unlock.ExtendedLevel.SelectableLevel.PlanetName}.", Key = "LMU_StoryLockReleasedGeneric" });
            }
            else {
                NetworkManager.Instance?.ServerSendAlertMessage(new Notification { Header = "Autopilot", Text = $"Destination unreachable! Status: UNKNOWN. Writing location data to backlog...", IsWarning = true, Key = "LMU_StoryLockReleasedGeneric" });
            }
            NetworkManager.Instance?.ServerSendAlertQueueEvent();
            return true;
        }

        internal void InitializeUnlocks() {
            if (AllLevels == null || AllLevels.Count == 0) {
                Logger.LogFatal($"Unable to find levels!");
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
            foreach (LMUnlockable importUnlock in newData) {
                foreach (LMUnlockable unlock in Unlocks) {
                    if (unlock.Name == importUnlock.Name) {
                        unlock.OverrideData(importUnlock);
                    }
                }
            }
        }

        internal void CollectStoryLockedMoons() {
            var storyLocks = new List<string>();

            if (OnCollectStoryLockedMoons != null) {
                var subscribers = OnCollectStoryLockedMoons.GetInvocationList();

                foreach (DesignateStoryUnlocks subscriber in subscribers) {
                    try {
                        List<string> response = subscriber();
                        storyLocks.AddRange(response);
                        Logger.LogInfo($"Collected the following story locked moons: {string.Join(", ", response)}");
                    } catch (Exception ex) {
                        Logger.LogError($"Couldn't handle subscriber response while collecting story locked moons! Error: {ex.Message}");
                    }
                }
                foreach (var storyLock in storyLocks) {
                    var unlock = Instance?.Unlocks.FirstOrDefault(u => u.Name == storyLock && !u.StoryUnlock);
                    unlock?.DesignateAsStoryLocked();
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
            LogUnlockables(false);
        }

        public void BuyMoon(string moon) {
            Logger.LogInfo($"{moon}: Moon was bought!");
            var unlock = Unlocks.Where(unlock => unlock.Name == moon).FirstOrDefault();
            if (ConfigManager.DiscountMode) {
                if (unlock.BuyCount < ConfigManager.DiscountsCount) {
                    unlock.BuyCount++;
                }
            } else {
                unlock.BuyCount++;
            }
            Logger.LogInfo($"{unlock.Name}: Set buy count to {unlock.BuyCount}");

            if (ConfigManager.DiscoveryMode) {
                // TRAVEL DISCOVERY
                if (ConfigManager.TravelDiscoveries && DiscoveryCandidates.Count > 0) {
                    if (RandomHelper.Chance(ConfigManager.TravelDiscoveryChance)) {
                        Logger.LogInfo($"Travel Discovery triggered! (Chance: {ConfigManager.TravelDiscoveryChance}%)");
                        TravelDiscovery(unlock);
                    }
                }
            }
            IterateUnlocks();
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 2);
        }

        internal void LogUnlockables(bool debug = true) {
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

            if (Plugin.LethalConstellationsPresent && Plugin.LethalConstellationsExtension != null) {
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

                foreach (var constellation in groupedUnlocks) {
                    LogLine($"| Constellation: {constellation.Key}");
                    LogLine(separator);
                    LogLine(header);
                    LogLine(separator);
                    foreach (var unlock in constellation.Value) {
                        LogLine(unlock.ToString());
                    }
                    LogLine(separator);
                }

                if (unmatchedUnlocks.Count > 0) {
                    LogLine("| Unmatched moons");
                    LogLine(separator);
                    LogLine(header);
                    LogLine(separator);
                    foreach (var unlock in unmatchedUnlocks) {
                        LogLine(unlock.ToString());
                    }
                    LogLine(separator);
                    Logger.LogWarning($"Found {unmatchedUnlocks.Count} moon(s) without a LethalConstellations group while constellation matching is active. This should not exist at runtime: {string.Join(", ", unmatchedUnlocks.Select(unlock => unlock.Name))}");
                }

                return;
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
            
            // Force shuffle if discovery mode is enabled and no moons are discovered
            // This can happen after loading a save that didn't have Discovery mode enabled
            if (loadSuccess && ConfigManager.DiscoveryMode && Unlocks.All(unlock => !unlock.Discovered)) {
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
            if (ConfigManager.DiscoveryMode) {
                // Remove [NEW] discovery tags
                Unlocks.Where(unlock => unlock.NewDiscovery).Do(unlock => { unlock.NewDiscovery = false; });
                // SHUFFLE ON NEW QUOTA
                if (!ConfigManager.DiscoveryNeverShuffle) {
                //if (!ConfigManager.DiscoveryNeverShuffle && !ConfigManager.DiscoveryShuffleEveryDay) {
                    Logger.LogInfo($"Shuffling moon rotation on new Quota..");
                    ShuffleDiscoverable();
                }
                // QUOTA DISCOVERY
                if (ConfigManager.QuotaDiscoveries && DiscoveryCandidates.Count > 0 && RandomHelper.Chance(ConfigManager.QuotaDiscoveryChance)) {
                    Logger.LogInfo($"Quota Discovery triggered! (Chance: {ConfigManager.QuotaDiscoveryChance}%)");
                    if (ConfigManager.QuotaDiscoveryCheapestGroup && (ConfigManager.MoonGroupMatchingMethod == "Custom" || ConfigManager.MoonGroupMatchingMethod == "LethalConstellations")) {
                        QuotaDiscoveryGroup();
                    } else {
                        QuotaDiscovery();
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
            if (!ConfigManager.DiscountMode && ConfigManager.QuotaUnlocks && PaidMoons.Count > 0) {
                if (RandomHelper.Chance(ConfigManager.QuotaUnlockChance) && (ConfigManager.QuotaUnlockMaxCount < 1 || QuotaUnlocksCount < ConfigManager.QuotaUnlockMaxCount)) {
                    Logger.LogInfo($"Quota unlock triggered! (Chance: {ConfigManager.QuotaUnlockChance}%)");
                    QuotaUnlock();
                }
            }
            // DISCOUNT MODE
            if (ConfigManager.DiscountMode) {
                // QUOTA DISCOUNT
                if (ConfigManager.QuotaDiscounts && PaidMoons.Count > 0 && RandomHelper.Chance(ConfigManager.QuotaDiscountChance) && (ConfigManager.QuotaDiscountMaxCount < 1 || QuotaDiscountsCount < ConfigManager.QuotaDiscountMaxCount)) {
                    Logger.LogInfo($"Quota Discount triggered! (Chance: {ConfigManager.QuotaDiscountChance}%)");
                    QuotaDiscount();
                }
                // QUOTA FULL DISCOUNT
                if (ConfigManager.QuotaFullDiscounts && PaidMoons.Count > 0 && RandomHelper.Chance(ConfigManager.QuotaFullDiscountChance) && (ConfigManager.QuotaFullDiscountMaxCount < 1 || QuotaFullDiscountsCount < ConfigManager.QuotaFullDiscountMaxCount)) {
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
                    if (ConfigManager.NewDayDiscoveries && DiscoveryCandidates.Count > 0 && RandomHelper.Chance(ConfigManager.NewDayDiscoveryChance)) {
                        Logger.LogInfo($"New Day Discovery triggered! (Chance: {ConfigManager.NewDayDiscoveryChance}%)");
                        NewDayDiscovery();
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
                return;
            }

            Unlocks.Do(unlock => unlock.RefreshSale());
        }

        internal void OnArrive() {
            var unlock = Unlocks.FirstOrDefault(unlock => unlock.Name == LevelManager.CurrentExtendedLevel.NumberlessPlanetName);
            if (unlock != null) {
                Logger.LogInfo($"Visiting moon {unlock.Name}!");
                unlock.VisitMoon();
            }
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

        private void QuotaDiscovery() {
            var quotaDiscoveries = DiscoveryCandidates;
            if (ConfigManager.CheapMoonBiasQuotaDiscovery) {
                quotaDiscoveries = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(quotaDiscoveries, ConfigManager.CheapMoonBiasQuotaDiscoveryValue), ConfigManager.QuotaDiscoveryCount);
            } else {
                quotaDiscoveries = RandomHelper.Select(quotaDiscoveries, ConfigManager.QuotaDiscoveryCount);
            }
            if (quotaDiscoveries.Count == 0) {
                Logger.LogInfo($"No moons for Quota Discovery available!");
                return;
            }
            foreach (var qd in quotaDiscoveries) {
                qd.Discovered = true;
                if (ConfigManager.QuotaDiscoveryPermanent) {
                    qd.PermanentlyDiscovered = true;
                    Logger.LogInfo($"Quota Discovery is permanent: {qd.Name}");
                }
            }
            NotificationHelper.SendChatMessage($"{quotaDiscoveries.Count.SinglePluralWord("Discovery")} granted:\n<color=white>{string.Join(", ", quotaDiscoveries.Select(ndd => ndd.Name))}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"New {quotaDiscoveries.Count.SinglePluralWord("Discovery")}!", Text = $"Received coordinates:\n{string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaDiscovery", ExceptWhenKey = "LMU_NewQuotaDiscoveryGroup" });
            Logger.LogInfo($"New Quota Discoveries: {string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}");
        }

        private void QuotaDiscoveryGroup() {
            var quotaDiscoveries = DiscoveryCandidates;
            List<LMUnlockable> discoveryGroup = new List<LMUnlockable>();
            LMGroup group = new LMGroup();
            if (Plugin.LethalConstellationsPresent && ConfigManager.QuotaDiscoveryCheapestConstellation && ConfigManager.MoonGroupMatchingMethod == "LethalConstellations") {
                group = Plugin.LethalConstellationsExtension.GetCheapestUndiscoveredConstellation();
                foreach (var member in group.Members) {
                    if (!member.Discovered && !member.PermanentlyDiscovered) {
                        discoveryGroup.Add(member);
                    }
                }
            } else {
                foreach (var candidate in quotaDiscoveries.OrderBy(c => c.OriginalPrice)) {
                    Logger.LogInfo($"Got cheapest candidate: {candidate.Name}");
                    Logger.LogInfo($"Checking for groups..");
                    group = MatchMoonGroup(candidate, quotaDiscoveries, false);
                    if (group.Members.Count > 0) {
                        foreach (var member in group.Members) {
                            if (!member.Discovered && !member.PermanentlyDiscovered) {
                                discoveryGroup.Add(member);
                            }
                        }
                        break;
                    } else {
                        Logger.LogInfo("Candidate has no group matches. Try next..");
                    }
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
                return;
            }
            foreach (var qd in discoveryGroup) {
                qd.Discovered = true;
                if (ConfigManager.QuotaDiscoveryPermanent) {
                    qd.PermanentlyDiscovered = true;
                    Logger.LogInfo($"Quota Discovery is permanent: {qd.Name}");
                }
            }
            string message_groupname = string.Empty;
            if (group.Name != string.Empty) {
                message_groupname = $" in <color=red>{group.Name}</color>";
            }
            NotificationHelper.SendChatMessage($"{discoveryGroup.Count.SinglePluralWord("Discovery")} granted{message_groupname}:\n<color=white>{string.Join(", ", discoveryGroup.Select(qd => qd.Name))}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"New {quotaDiscoveries.Count.SinglePluralWord("Discovery")}!", Text = $"Received coordinates:\n{string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaDiscovery", ExceptWhenKey = "LMU_NewQuotaDiscoveryGroup" });
            Logger.LogInfo($"New Quota Discoveries: {string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}");
        }

        private void QuotaUnlock() {
            List<LMUnlockable> quotaUnlocks = PaidMoons;
            if (ConfigManager.DiscoveryMode) {
                quotaUnlocks = quotaUnlocks.Where(unlock => unlock.Discovered == true || unlock.PermanentlyDiscovered == true).ToList();
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
            }
            QuotaUnlocksCount++;
            if (quotaUnlocks.Count > 1) {
                NotificationHelper.SendChatMessage($"New moons unlocked:\n<color=green>{string.Join(", ", quotaUnlocks.Select(unlock => unlock.Name))}</color>");
            } else if (quotaUnlocks.Count == 1) {
                NotificationHelper.SendChatMessage($"New moon unlocked:\n<color=green>{quotaUnlocks.FirstOrDefault().Name}</color>");
            }
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"{quotaUnlocks.Count.SinglePluralWord("Unlock")} granted!", Text = $"You earned unlocks for:\n{string.Join(", ", quotaUnlocks.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaUnlock" });
            Logger.LogInfo($"New Quota Unlocks: {string.Join(", ", quotaUnlocks.Select(unlock => unlock.Name))}");
        }

        private void QuotaDiscount() {
            var quotaDiscounts = PaidMoons;
            if (ConfigManager.DiscoveryMode) {
                quotaDiscounts = quotaDiscounts.Where(unlock => unlock.Discovered || unlock.PermanentlyDiscovered).ToList();
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
            }
            QuotaDiscountsCount++;
            if (quotaDiscounts.Count == 1) NotificationHelper.SendChatMessage($"Discount granted:\n<color=green>{quotaDiscounts.FirstOrDefault().Name}</color>");
            else if (quotaDiscounts.Count > 1) NotificationHelper.SendChatMessage($"Discounts granted:\n<color=green>{string.Join(", ", quotaDiscounts.Select(unlock => unlock.Name))}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"{quotaDiscounts.Count.SinglePluralWord("Discount")} granted!", Text = $"You earned discounts for:\n{string.Join(", ", quotaDiscounts.Select(discount => discount.Name + " " + (100 - (int)(Plugin.GetDiscountRate(discount.BuyCount) * 100)) + "%"))}", Key = "LMU_NewQuotaDiscount" });
            Logger.LogInfo($"New Quota Discounts: {string.Join(", ", quotaDiscounts.Select(unlock => unlock.Name))}");
        }

        private void QuotaFullDiscount() {
            List<LMUnlockable> quotaFullDiscounts = PaidMoons;
            if (ConfigManager.DiscoveryMode) {
                quotaFullDiscounts = quotaFullDiscounts.Where(unlock => unlock.Discovered || unlock.PermanentlyDiscovered).ToList();
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
            }
            QuotaFullDiscountsCount++;
            if (quotaFullDiscounts.Count == 1) NotificationHelper.SendChatMessage($"Full discount granted:\n<color=green>{quotaFullDiscounts.FirstOrDefault().Name}</color>");
            else if (quotaFullDiscounts.Count > 1) NotificationHelper.SendChatMessage($"Full discounts granted:\n<color=green>{string.Join(", ", quotaFullDiscounts.Select(unlock => unlock.Name))}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $" Full {quotaFullDiscounts.Count.SinglePluralWord("Discount")} granted!", Text = $"You earned full discounts for:\n{string.Join(", ", quotaFullDiscounts.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaFullDiscount" });
            Logger.LogInfo($"New Quota Full Discounts: {string.Join(", ", quotaFullDiscounts.Select(unlock => unlock.Name))}");
        }

        private void NewDayDiscovery() {
            Logger.LogInfo($"New Day Discovery Candidates: {string.Join(", ", DiscoveryCandidates.Select(unlock => unlock.Name))}");

            var currentLevelUnlock = Unlocks.FirstOrDefault(unlock => unlock.ExtendedLevel.NumberlessPlanetName == LevelManager.CurrentExtendedLevel.NumberlessPlanetName);
            List<LMUnlockable> newDayDiscoveries;
            List<LMUnlockable> nddCandidates = DiscoveryCandidates;
            string ndDiscoveryGroupName = "nearby";
            if (ConfigManager.NewDayDiscoveryMatchGroup && currentLevelUnlock != null) {
                LMGroup moonGroup = MatchMoonGroup(currentLevelUnlock, DiscoveryCandidates, ConfigManager.NewDayDiscoveryMatchGroupFallback);
                nddCandidates = moonGroup.Members;
                if (!string.IsNullOrEmpty(moonGroup.Name)) ndDiscoveryGroupName = $"in <color=red>{moonGroup.Name}</color>";
            }
            if (nddCandidates.Count < 1) {
                Logger.LogInfo($"No discoverable moons found!");
                return;
            }
            if (ConfigManager.CheapMoonBiasNewDayDiscovery) {
                newDayDiscoveries = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(nddCandidates, ConfigManager.CheapMoonBiasNewDayDiscoveryValue), ConfigManager.NewDayDiscoveryCount);
            } else {
                newDayDiscoveries = RandomHelper.Select(nddCandidates, ConfigManager.NewDayDiscoveryCount);
            }
            foreach (var d in newDayDiscoveries) {
                d.Discovered = true;
                if (ConfigManager.NewDayDiscoveryPermanent) {
                    d.PermanentlyDiscovered = true;
                    Logger.LogDebug($"{d.Name}: Discovery is permanent");
                }
            }
            if (newDayDiscoveries.Count == 1) {
                NotificationHelper.SendChatMessage($"Autopilot discovered moon suitable for landing {ndDiscoveryGroupName}:\n<color=white>{newDayDiscoveries.FirstOrDefault().Name}</color>");
                Logger.LogInfo($"New Day Discoveries: [ {string.Join(", ", newDayDiscoveries.Select(discovery => discovery.Name))} ]");
            }
            if (newDayDiscoveries.Count > 1) {
                NotificationHelper.SendChatMessage($"Autopilot discovered moons suitable for landing {ndDiscoveryGroupName}:\n<color=white>{string.Join(", ", newDayDiscoveries.Select(ndd => ndd.Name))}</color>");
                Logger.LogInfo($"New Day Discovery: [ {string.Join(", ", newDayDiscoveries.Select(discovery => discovery.Name))} ]");
            }
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"New Day {newDayDiscoveries.Count.SinglePluralWord("Discovery")}!", Text = $"Autopilot discovered new {newDayDiscoveries.Count.SinglePluralWord("moon")} {ndDiscoveryGroupName}.\n" +
                $"Moon catalog updated!", Key = "LMU_NewDayDiscovery" });
            Logger.LogInfo($"New Day Discoveries: {string.Join(", ", newDayDiscoveries.Select(unlock => unlock.Name))}");

        }

        private void TravelDiscovery(LMUnlockable unlock) {
            Logger.LogInfo($"Travel Discovery Candidates: {string.Join(", ", DiscoveryCandidates.Select(unlock => unlock.Name))}");
            List<LMUnlockable> tdCandidates = DiscoveryCandidates;
            List<LMUnlockable> travelDiscoveries;
                
            string tdMessageGroupName = string.Empty;
            if (ConfigManager.TravelDiscoveryMatchGroup) {
                LMGroup moonGroup = MatchMoonGroup(unlock, DiscoveryCandidates, ConfigManager.TravelDiscoveryMatchGroupFallback);
                tdCandidates = moonGroup.Members;
                if (!string.IsNullOrEmpty(moonGroup.Name)) tdMessageGroupName = $" to <color=red>{moonGroup.Name}</color>";
            }
            if (tdCandidates.Count < 1) {
                Logger.LogInfo($"No discoverable moons found!");
                return;
            }
            if (ConfigManager.CheapMoonBiasTravelDiscovery) {
                travelDiscoveries = RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(tdCandidates, ConfigManager.CheapMoonBiasTravelDiscoveryValue), ConfigManager.TravelDiscoveryCount);
            } else {
                travelDiscoveries = RandomHelper.Select(tdCandidates, ConfigManager.TravelDiscoveryCount);
            }

            foreach (var d in travelDiscoveries) {
                d.Discovered = true;
                if (ConfigManager.TravelDiscoveryPermanent) {
                    d.PermanentlyDiscovered = true;
                    Logger.LogDebug($"{d.Name}: Discovery is permanent");
                }
            }

            if (travelDiscoveries.Count > 1) {
                NotificationHelper.SendChatMessage($"Discovered new moons on route{tdMessageGroupName}:\n<color=white>{string.Join(", ", travelDiscoveries.Select(td => td.Name))}</color>");
            } else if (travelDiscoveries.Count == 1) {
                NotificationHelper.SendChatMessage($"Discovered new moon on route{tdMessageGroupName}:\n<color=white>{travelDiscoveries.FirstOrDefault().Name}</color>");
            }
            Logger.LogInfo($"Travel Discovery: [ {string.Join(", ", travelDiscoveries.Select(discovery => discovery.Name))} ]");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"New {travelDiscoveries.Count.SinglePluralWord("Discovery")}!", Text = $"Autopilot discovered new {travelDiscoveries.Count.SinglePluralWord("moon")} during travel{tdMessageGroupName}.\n" +
                $"Moon catalog updated!", Key = "LMU_TravelDiscovery" });
            Logger.LogInfo($"Travel Discoveries: {string.Join(", ", travelDiscoveries.Select(unlock => unlock.Name))}");
        }

        private LMGroup MatchMoonGroup(LMUnlockable matchingUnlock, List<LMUnlockable> unlocksToMatch, bool fallback) {
            Logger.LogDebug($"Matching moon {matchingUnlock.Name}: Matching against = [ {string.Join(", ", unlocksToMatch.Select(unlock => unlock.Name))} ]");
            if (matchingUnlock == null) return new LMGroup() { Members = unlocksToMatch };
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
                case "LethalConstellations":
                    if (!Plugin.LethalConstellationsPresent) break;
                    string constellationName = Plugin.LethalConstellationsExtension.GetConstellationName(matchingUnlock);
                    List<LMUnlockable> constellationMatches = Plugin.LethalConstellationsExtension.GetConstellationMatchesForMoon(matchingUnlock, unlocksToMatch);
                    if (constellationMatches.Count > 0) {
                        Logger.LogInfo($"Matching moon {matchingUnlock.Name}: Matched by constellation [{constellationName}]; Matches = [ {string.Join(", ", constellationMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Name = constellationName, Members = constellationMatches };
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
            
            // LMU Story
            if (ConfigManager.LMUStoryProgression) {
                OnCollectStoryLockedMoons -= ProgressionManager.LMUStoryLocks;
                OnCollectStoryLockedMoons += ProgressionManager.LMUStoryLocks;
            } else {
                OnCollectStoryLockedMoons -= ProgressionManager.LMUStoryLocks;
            }

            if (ConfigManager.GaletryStoryLock && AllLevels.Any(level => level.NumberlessPlanetName == "Galetry")) {
                OnCollectStoryLockedMoons -= ProgressionManager.GaletryStoryLock;
                OnCollectStoryLockedMoons += ProgressionManager.GaletryStoryLock;
            } else {
                OnCollectStoryLockedMoons -= ProgressionManager.GaletryStoryLock;
            }
            if (ConfigManager.EnableStoryProgression) {
                CollectStoryLockedMoons();
            }
            
            if (ConfigManager.DiscoveryMode) {
                ShuffleDiscoverable();
                // Hide [NEW] discovery tag permanently from all moons in initial rotation
                Unlocks.Where(unlock => unlock.Discovered).Do(unlock => { unlock.DiscoveredOnce = true; });
            }

            // Shuffle Moon Sales
            if (ConfigManager.Sales) {
                RefreshSales();
            }
        }

        private void ShuffleDiscoverable() {
            Logger.LogInfo("Shuffling discovered moon rotations.. ");

            // Reset rotation
            foreach (var candidate in Unlocks.Where(unlock => unlock.OriginallyHidden == false && unlock.OriginallyLocked == false && !unlock.PermanentlyDiscovered)) {
                if (candidate.NewDiscovery) candidate.NewDiscovery = false;
                candidate.Discovered = false;
            }
            // increase counts
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
            
            // select new rotation
            ApplyDiscoveryWhitelist();
            AddFreeToRotation(DiscoveredFreeCount);
            AddDynamicFreeToRotation(DiscoveredDynamicFreeCount);
            AddPaidToRotation(DiscoveredPaidCount);

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
                    unlock.Discovered = true;
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
            Logger.LogInfo($"After shuffling check if we have to reroute to a discovered free moon..");
            if (Unlocks.Any(unlock => (unlock.Discovered || unlock.PermanentlyDiscovered ) && unlock.Name == LevelManager.CurrentExtendedLevel.NumberlessPlanetName) || LevelManager.CurrentExtendedLevel.NumberlessPlanetName == "Gordion") {
                Logger.LogInfo($"Current moon is discovered. Not rerouting ship.");
            } else {
                var currentDiscoveredFreeMoons = DynamicFreeMoons.Where(unlock => !unlock.OriginallyLocked && !unlock.OriginallyHidden && (unlock.Discovered || unlock.PermanentlyDiscovered)).ToList();
                if (currentDiscoveredFreeMoons.Count < 1) {
                    Logger.LogWarning("Can't find any free and discovered moon! You probably want at least one free moon available at all times.. Abort auto routing ship!");
                    return;
                }
                var randomDiscoveredFreeMoon = currentDiscoveredFreeMoons[RandomHelper.Range(0, currentDiscoveredFreeMoons.Count)].ExtendedLevel;
                Logger.LogInfo($"Current moon is not discovered! Rerouting ship to {randomDiscoveredFreeMoon.NumberlessPlanetName}..");
                if (DayCount > 0) {
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Dangerous conditions!", Text = $"Conditions too dangerous to stay in orbit! Auto routing ship to a safe moon..", Key = "LMU_RerouteFree" });
                }
                DelayHelper.Instance.ExecuteAfterDelay(() => { StartOfRound.Instance.ChangeLevelServerRpc(randomDiscoveredFreeMoon.SelectableLevel.levelID, Terminal.groupCredits); }, 3.5f);
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
                            unlock.Discovered = true;
                            Logger.LogDebug($"Whitelist entry set to discovered: {entry}");
                            break;
                        }
                    }
                    if (!matched) Logger.LogWarning($"Couldn't match whitelist entry! Is this a valid moon name: {entry} ?");
                }
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
                || savedata.ContainsKey("LMU_Progression"))) {
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
                    Logger.LogInfo($"Loading ProgressionManager state: PaintingsSold={ProgressionManager.Instance?.PaintingsSold}.");
                }
                if (savedata.ContainsKey("GroupCredits") && ConfigManager.GroupCreditsSavingBandAid) {
                    Terminal.groupCredits = (int)savedata["GroupCredits"];
                    Logger.LogInfo($"BAND-AID: Restored group credits ({Terminal.groupCredits}) from save file..");
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

