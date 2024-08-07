using HarmonyLib;
using LethalLevelLoader;
using LethalMoonUnlocks.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalMoonUnlocks {
    internal class UnlockManager {

        internal static UnlockManager Instance { get; private set; }
        internal static string LogFormatString { get; } = "| {0, -20} | {1, 7} | {2, 7} | {3, 6} | {4, 11} | {5, 6} | {6, 6} | {7, 10} | {8, 7} | {9, 5} | {10, 8} | {11, 8} |";
        internal static List<string> LogHeader { get; } = ["LMUnlockable", "Bought", "Visits", "Free", "Discovered", "New", "Once",  "Permanent", "OnSale", "Rate", "OGPrice", "Ignored"];
        internal Terminal Terminal { get; set; }
        internal List<ExtendedLevel> AllLevels { get; private set; } = PatchedContent.ExtendedLevels;
        internal List<LMUnlockable> Unlocks { get; set; } = new List<LMUnlockable>();
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
                return Unlocks.Where(unlock => unlock.ExtendedLevel.RoutePrice == 0).ToList();
            }
        }
        internal List<LMUnlockable> PaidMoons {
            get {
                return Unlocks.Where(unlock => unlock.ExtendedLevel.RoutePrice > 0).ToList();
            }
        }
        internal List<LMUnlockable> DiscoveryCandidates {
            get {
                return Unlocks.Where(unlock => !unlock.OriginallyLocked && !unlock.OriginallyHidden && !unlock.Discovered && !unlock.PermanentlyDiscovered).ToList();
            }
        }
        internal List<LMUnlockable> DiscoveryFreeCandidates {
            get {
                return DiscoveryCandidates.Where(candidate => candidate.OriginalPrice == 0).ToList();
            }
        }
        internal List<LMUnlockable> DiscoveryDynamicFreeCandidates {
            get {
                return DiscoveryCandidates.Where(candidate => candidate.ExtendedLevel.RoutePrice == 0).ToList();
            }
        }
        internal List<LMUnlockable> DiscoveryPaidCandidates {
            get {
                return DiscoveryCandidates.Where(candidate => candidate.ExtendedLevel.RoutePrice > 0).ToList();
            }
        }

        public UnlockManager() {
            if (Instance == null)
                Instance = this;
            TerminalManager.onBeforePreviewInfoTextAdded += ReplaceTerminalPreview;
        }

        public void LogUnlockables(bool debug = true) {
            if (debug) {
                Plugin.Instance.Mls.LogDebug(string.Format(LogFormatString, LogHeader.ToArray()));
                foreach (var unlock in Unlocks) {
                    Plugin.Instance.Mls.LogDebug(unlock);
                }
            } else {
                Plugin.Instance.Mls.LogInfo(string.Format(LogFormatString, LogHeader.ToArray()));
                foreach (var unlock in Unlocks) {
                    Plugin.Instance.Mls.LogInfo(unlock);
                }
            }
        }
        public void OnLobbyStart() {
            // Refresh config and create LMUnlockable for all moons
            ConfigManager.RefreshConfig();
            DiscoveredFreeCount = ConfigManager.DiscoveryFreeCountBase;
            DiscoveredDynamicFreeCount = ConfigManager.DiscoveryDynamicFreeCountBase;
            DiscoveredPaidCount = ConfigManager.DiscoveryPaidCountBase;

            // Create LMUnlockables from LLL Extended Levels
            InitializeUnlocks();

            // Load save data
            // if save exists apply all unlockable data and continue
            // init new game
            bool loadSuccess = LoadAndImportSavaData();
            if (!loadSuccess) {
                InitializeNewGame();
            }

            LogUnlockables();

            // Apply everything
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 2);
        }


        public void OnNewQuota() {
            QuotaCount++;
            Plugin.Instance.Mls.LogInfo($"New quota! Completed quota count: {QuotaCount}");
            ConfigManager.RefreshConfig();
            if (ConfigManager.DiscoveryMode) {
                // Remove [NEW] discovery tags
                Unlocks.Where(unlock => unlock.NewDiscovery).Do(unlock => { unlock.NewDiscovery = false; });
                // SHUFFLE ON NEW QUOTA
                if (!ConfigManager.DiscoveryNeverShuffle) {
                //if (!ConfigManager.DiscoveryNeverShuffle && !ConfigManager.DiscoveryShuffleEveryDay) {
                    Plugin.Instance.Mls.LogInfo($"Shuffling moon rotation on new Quota..");
                    ShuffleDiscoverable();
                }
                // QUOTA DISCOVERY
                if (ConfigManager.QuotaDiscoveries && DiscoveryCandidates.Count > 0 && UnityEngine.Random.Range(0, 100) < ConfigManager.QuotaDiscoveryChance) {
                    Plugin.Instance.Mls.LogInfo($"Quota Discovery triggered! (Chance: {ConfigManager.QuotaDiscoveryChance}%)");
                    QuotaDiscovery();
                }
            }
            // SHUFFLE SALES
            if (ConfigManager.Sales) {
            //if (ConfigManager.Sales && !ConfigManager.SalesShuffleDaily) {
                Unlocks.Do(unlock => unlock.RefreshSale());
            }
            // Apply Unlocks to make sure in discovery mode unlocks/discounts are not granted to undiscovered moons
            ApplyUnlocks();

            // QUOTA UNLOCK
            if (!ConfigManager.DiscountMode && ConfigManager.QuotaUnlocks && PaidMoons.Count > 0) {
                if (UnityEngine.Random.Range(0, 100) < ConfigManager.QuotaUnlockChance && (ConfigManager.QuotaUnlockMaxCount < 1 || QuotaUnlocksCount < ConfigManager.QuotaUnlockMaxCount)) {
                    Plugin.Instance.Mls.LogInfo($"Quota unlock triggered! (Chance: {ConfigManager.QuotaUnlockChance}%)");
                    QuotaUnlock();
                }
            }
            // DISCOUNT MODE
            if (ConfigManager.DiscountMode) {
                // QUOTA DISCOUNT
                if (ConfigManager.QuotaDiscounts && PaidMoons.Count > 0 && UnityEngine.Random.Range(0, 100) < ConfigManager.QuotaDiscountChance && (ConfigManager.QuotaDiscountMaxCount < 1 || QuotaDiscountsCount < ConfigManager.QuotaDiscountMaxCount)) {
                    Plugin.Instance.Mls.LogInfo($"Quota Discount triggered! (Chance: {ConfigManager.QuotaDiscountChance}%)");
                    QuotaDiscount();
                }
                // QUOTA FULL DISCOUNT
                if (ConfigManager.QuotaFullDiscounts && PaidMoons.Count > 0 && UnityEngine.Random.Range(0, 100) < ConfigManager.QuotaFullDiscountChance && (ConfigManager.QuotaFullDiscountMaxCount < 1 || QuotaFullDiscountsCount < ConfigManager.QuotaFullDiscountMaxCount)) {
                    Plugin.Instance.Mls.LogInfo($"Quota Full Discount triggered! (Chance: {ConfigManager.QuotaFullDiscountChance}%)");
                    QuotaFullDiscount();
                }
            }

            // APPLY ALL
            LogUnlockables(false);
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 5);
        }

        private void QuotaDiscovery() {
            var quotaDiscoveries = DiscoveryCandidates;
            if (ConfigManager.CheapMoonBias > 0f && ConfigManager.CheapMoonBiasQuotaDiscovery) {
                quotaDiscoveries = RandomSelector.GetWeighted(RandomSelector.CalculateBiasedWeights(quotaDiscoveries), ConfigManager.QuotaDiscoveryCount);
            } else {
                quotaDiscoveries = RandomSelector.Get(quotaDiscoveries, ConfigManager.QuotaDiscoveryCount);
            }
            if (quotaDiscoveries.Count == 0) {
                Plugin.Instance.Mls.LogInfo($"No moons for Quota Discovery available!");
                return;
            }
            foreach (var qd in quotaDiscoveries) {
                qd.Discovered = true;
                if (ConfigManager.QuotaDiscoveryPermanent) {
                    qd.PermanentlyDiscovered = true;
                    Plugin.Instance.Mls.LogInfo($"Quota Discovery is permanent: {qd.Name}");
                }
            }
            if (ConfigManager.ChatMessages) {
                HUDManager.Instance.AddTextToChatOnServer($"{(quotaDiscoveries.Count > 1 ? "Moons" : "Moon")} discovered:\n <color=white>{(quotaDiscoveries.Count > 1 ? string.Join(", ", quotaDiscoveries.Select(ndd => ndd.Name)) :  quotaDiscoveries.FirstOrDefault().Name)}</color>");
            }
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"New {quotaDiscoveries.Count.SinglePluralWord("Discovery")}!", Text = $"Received new coordinates:\n{string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaDiscovery" });
            Plugin.Instance.Mls.LogInfo($"New Quota Discoveries: {string.Join(", ", quotaDiscoveries.Select(unlock => unlock.Name))}");
        }

        private void QuotaUnlock() {
            List<LMUnlockable> quotaUnlocks = PaidMoons;
            if (ConfigManager.DiscoveryMode) {
                quotaUnlocks = quotaUnlocks.Where(unlock => unlock.Discovered == true || unlock.PermanentlyDiscovered == true).ToList();
            }
            if (ConfigManager.QuotaUnlockMaxPrice > 0) {
                quotaUnlocks = quotaUnlocks.Where(moon => moon.ExtendedLevel.RoutePrice <= ConfigManager.QuotaUnlockMaxPrice).ToList();
            }
            quotaUnlocks = RandomSelector.Get(quotaUnlocks, ConfigManager.QuotaUnlockCount);
            if (quotaUnlocks.Count == 0) {
                Plugin.Instance.Mls.LogInfo($"No moons for Quota Unlock available!");
                return;
            }
            foreach (var unlock in quotaUnlocks) {
                unlock.BuyCount++;
                unlock.FreeVisitCount = 1;
            }
            QuotaUnlocksCount++;
            if (ConfigManager.ChatMessages) {
                if (quotaUnlocks.Count > 1) {
                    HUDManager.Instance.AddTextToChatOnServer($"New moons unlocked:\n <color=green>{string.Join(", ", quotaUnlocks.Select(unlock => unlock.Name))}</color>");
                } else if (quotaUnlocks.Count > 1) {
                    HUDManager.Instance.AddTextToChatOnServer($"New moon unlocked:\n <color=green>{quotaUnlocks.FirstOrDefault().Name}</color>");
                }
            }
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"{quotaUnlocks.Count.SinglePluralWord("Unlock")} granted!", Text = $"You earned unlocks for:\n{string.Join(", ", quotaUnlocks.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaUnlock" });
            Plugin.Instance.Mls.LogInfo($"New Quota Unlocks: {string.Join(", ", quotaUnlocks.Select(unlock => unlock.Name))}");
        }

        private void QuotaDiscount() {
            var quotaDiscounts = PaidMoons;
            if (ConfigManager.DiscoveryMode) {
                quotaDiscounts = quotaDiscounts.Where(unlock => unlock.Discovered || unlock.PermanentlyDiscovered).ToList();
            }
            quotaDiscounts = RandomSelector.Get(quotaDiscounts, ConfigManager.QuotaDiscountCount);
            if (quotaDiscounts.Count == 0) {
                Plugin.Instance.Mls.LogInfo($"No moons for Quota Discount available!");
                return;
            }
            foreach (var discount in quotaDiscounts) {
                discount.BuyCount++;
            }
            QuotaDiscountsCount++;
            if (ConfigManager.ChatMessages) {
                if (quotaDiscounts.Count == 1) HUDManager.Instance.AddTextToChatOnServer($"Discount granted:\n <color=green>{quotaDiscounts.FirstOrDefault().Name}</color>");
                else if (quotaDiscounts.Count > 1) HUDManager.Instance.AddTextToChatOnServer($"Discounts granted:\n <color=green>{string.Join(", ", quotaDiscounts.Select(unlock => unlock.Name))}</color>");
            }
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"{quotaDiscounts.Count.SinglePluralWord("Discount")} granted!", Text = $"You earned discounts for:\n{string.Join(", ", quotaDiscounts.Select(discount => discount.Name + " " + (100 - (int)(Plugin.GetDiscountRate(discount.BuyCount) * 100)) + "%"))}", Key = "LMU_NewQuotaDiscount" });
            Plugin.Instance.Mls.LogInfo($"New Quota Discounts: {string.Join(", ", quotaDiscounts.Select(unlock => unlock.Name))}");
        }

        private void QuotaFullDiscount() {
            List<LMUnlockable> quotaFullDiscounts = PaidMoons;
            if (ConfigManager.DiscoveryMode) {
                quotaFullDiscounts = quotaFullDiscounts.Where(unlock => unlock.Discovered || unlock.PermanentlyDiscovered).ToList();
            }
            if (ConfigManager.QuotaFullDiscountMaxPrice > 0) {
                quotaFullDiscounts = quotaFullDiscounts.Where(moon => moon.ExtendedLevel.RoutePrice <= ConfigManager.QuotaFullDiscountMaxPrice).ToList();
            }
            if (ConfigManager.Discounts[ConfigManager.Discounts.Count - 1] < 100) {
                quotaFullDiscounts = quotaFullDiscounts.Where(unlock => unlock.BuyCount < ConfigManager.DiscountsCount).ToList();
            }
            quotaFullDiscounts = RandomSelector.Get(quotaFullDiscounts, ConfigManager.QuotaFullDiscountCount);
            if (quotaFullDiscounts.Count == 0) {
                Plugin.Instance.Mls.LogInfo($"No moons for Quota Full Discount available!");
                return;
            }
            foreach (var fullDiscount in quotaFullDiscounts) {
                fullDiscount.BuyCount = ConfigManager.DiscountsCount;
                fullDiscount.FreeVisitCount = 1;
            }
            QuotaFullDiscountsCount++;
            if (ConfigManager.ChatMessages) {
                if (quotaFullDiscounts.Count == 1) HUDManager.Instance.AddTextToChatOnServer($"Full discount granted:\n <color=green>{quotaFullDiscounts.FirstOrDefault().Name}</color>");
                else if (quotaFullDiscounts.Count > 1) HUDManager.Instance.AddTextToChatOnServer($"Full discounts granted:\n <color=green>{string.Join(", ", quotaFullDiscounts.Select(unlock => unlock.Name))}</color>");
            }
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $" Full {quotaFullDiscounts.Count.SinglePluralWord("Discount")} granted!", Text = $"You earned full discounts for:\n{string.Join(", ", quotaFullDiscounts.Select(unlock => unlock.Name))}", Key = "LMU_NewQuotaFullDiscount" });
            Plugin.Instance.Mls.LogInfo($"New Quota Full Discounts: {string.Join(", ", quotaFullDiscounts.Select(unlock => unlock.Name))}");
        }

        public void OnNewDay() {
            Plugin.Instance.Mls.LogDebug($"DaysUntilDeadlineHUD: {(int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime)}, DaysUntilDeadline: {TimeOfDay.Instance.daysUntilDeadline}, deadlineDaysAmount: {TimeOfDay.Instance.quotaVariables.deadlineDaysAmount}");
            // NEW QUOTA DAY
            if ((int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime) == TimeOfDay.Instance.quotaVariables.deadlineDaysAmount || (int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime) < 0) {
                DayCount++;
                Plugin.Instance.Mls.LogInfo($"New day! Completed days: {DayCount}");
                Plugin.Instance.Mls.LogInfo($"New day is also new quota! Skip new day routine..");
            // LAST DAY OF QUOTA - REROUTE SHIP TO COMPANY AND SKIP REST
            } else if ((int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime) == 0 && ConfigManager.DiscoveryMode) {
                Plugin.Instance.Mls.LogInfo($"New day is last day of the quota! Not shuffling.");
                var company = AllLevels.Where(level => level.NumberlessPlanetName == "Gordion").FirstOrDefault();
                if (company == null) {
                    Plugin.Instance.Mls.LogError($"Couldn't find company level!");
                } else if (LevelManager.CurrentExtendedLevel != company) {
                    Plugin.Instance.Mls.LogInfo($"Rerouting ship to company!");
                    // wait a bit or the level change fails
                    DelayHelper.Instance.ExecuteAfterDelay(() => { StartOfRound.Instance.ChangeLevelServerRpc(company.SelectableLevel.levelID, Terminal.groupCredits); }, 3f);
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Deadline!", Text = $"Auto routing ship to the Company building.", Key = "LMU_RerouteCompany" });
                } else {
                    Plugin.Instance.Mls.LogInfo($"Already at company. No need to reroute.");
                }
            } else {
                // NEW DAY - NOT NEW QUOTA
                DayCount++;
                Plugin.Instance.Mls.LogInfo($"New day! Completed days: {DayCount}");
                ConfigManager.RefreshConfig();
                if (ConfigManager.DiscoveryMode) {
                    // Remove [NEW] discovery tags
                    Unlocks.Where(unlock => unlock.NewDiscovery).Do(unlock => { unlock.NewDiscovery = false; });
                    // Apply unlocks to make sure discovery selections are correct
                    ApplyUnlocks();
                    // Shuffle NEW DAY - EVERY DAY
                    if (ConfigManager.DiscoveryShuffleEveryDay) {
                        Plugin.Instance.Mls.LogInfo($"Shuffling moon rotation on new day!");
                        ShuffleDiscoverable();
                    }
                    // NEW DAY DISCOVERY
                    if (ConfigManager.NewDayDiscoveries && DiscoveryCandidates.Count > 0 && UnityEngine.Random.Range(0, 100) < ConfigManager.NewDayDiscoveryChance) {
                        Plugin.Instance.Mls.LogInfo($"New Day Discovery triggered! (Chance: {ConfigManager.NewDayDiscoveryChance}%)");
                        NewDayDiscovery();
                    }
                }
                if (ConfigManager.Sales && ConfigManager.SalesShuffleDaily) {
                    Unlocks.Do(unlock => unlock.RefreshSale());
                }
            }
            LogUnlockables(false);
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 3);
        }

        private void NewDayDiscovery() {
            Plugin.Instance.Mls.LogInfo($"New Day Discovery Candidates: {string.Join(", ", DiscoveryCandidates.Select(unlock => unlock.Name))}");

            var unlock = Unlocks.Where(unlock => unlock.ExtendedLevel == LevelManager.CurrentExtendedLevel).FirstOrDefault();
            List<LMUnlockable> newDayDiscoveries;
            List<LMUnlockable> nddCandidates = DiscoveryCandidates;
            string ndDiscoveryGroupName = "nearby";
            if (ConfigManager.NewDayDiscoveryMatchGroup && unlock != null) {
                LMGroup moonGroup = MatchMoonGroup(unlock, DiscoveryCandidates);
                if (moonGroup.Members != null && moonGroup.Members.Count > 0) {
                    nddCandidates = moonGroup.Members;
                    if (!string.IsNullOrEmpty(moonGroup.Name)) ndDiscoveryGroupName = $"in <color=red>{moonGroup.Name}</color>";
                }
            }
            if (ConfigManager.CheapMoonBias > 0f && ConfigManager.CheapMoonBiasNewDayDiscovery) {
                newDayDiscoveries = RandomSelector.GetWeighted(RandomSelector.CalculateBiasedWeights(nddCandidates), ConfigManager.NewDayDiscoveryCount);
            } else {
                newDayDiscoveries = RandomSelector.Get(nddCandidates, ConfigManager.NewDayDiscoveryCount);
            }
            foreach (var d in newDayDiscoveries) {
                d.Discovered = true;
                if (ConfigManager.TravelDiscoveryPermanent) {
                    d.PermanentlyDiscovered = true;
                    Plugin.Instance.Mls.LogDebug($"{d.Name}: Discovery is permanent");
                }
            }
            if (ConfigManager.ChatMessages) {
                if (newDayDiscoveries.Count == 1) {
                    HUDManager.Instance.AddTextToChatOnServer($"Autopilot discovered moon suitable for landing {ndDiscoveryGroupName}:\n <color=white>{newDayDiscoveries.FirstOrDefault().Name}</color>");
                    Plugin.Instance.Mls.LogInfo($"New Day Discoveries: [ {string.Join(", ", newDayDiscoveries.Select(discovery => discovery.Name))} ]");
                }
                if (newDayDiscoveries.Count > 1) {
                    HUDManager.Instance.AddTextToChatOnServer($"Autopilot discovered moons suitable for landing {ndDiscoveryGroupName}:\n <color=white>{string.Join(", ", newDayDiscoveries.Select(ndd => ndd.Name))}</color>");
                    Plugin.Instance.Mls.LogInfo($"New Day Discovery: [ {string.Join(", ", newDayDiscoveries.Select(discovery => discovery.Name))} ]");
                }
            }
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"New Day {newDayDiscoveries.Count.SinglePluralWord("Discovery")}!", Text = $"Autopilot discovered new {newDayDiscoveries.Count.SinglePluralWord("moon")} suitable for landing {ndDiscoveryGroupName}.\n" +
                $"Moon catalogue updated!", Key = "LMU_NewDayDiscovery" });
            Plugin.Instance.Mls.LogInfo($"New Day Discoveries: {string.Join(", ", newDayDiscoveries.Select(unlock => unlock.Name))}");

        }
        public void OnArrive() {
            var unlock = Unlocks.Where(unlock => unlock.Name == LevelManager.CurrentExtendedLevel.NumberlessPlanetName).FirstOrDefault();
            if (unlock != null) {
                Plugin.Instance.Mls.LogInfo($"Visiting moon {unlock.Name}!");
                unlock.VisitMoon();
            }
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
        }
        public void OnLanding(SelectableLevel level) {
            var unlock = Unlocks.Where(unlock => unlock.ExtendedLevel.SelectableLevel.levelID == level.levelID).FirstOrDefault();
            if (unlock != null) {
                unlock.Land();
            }
        }
        public void OnResetGame() {
            if (ConfigManager.ResetWhenFired) {
                Plugin.Instance.Mls.LogInfo($"Resetting all progress on getting fired!");
                Reset();
                InitializeUnlocks();
                DelayHelper.Instance.ExecuteAfterDelay(() => { InitializeNewGame(); }, 8.0f);
            }
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
        }
        public void OnDisconnect() {
            Reset();
        }

        public void ImportUnlockableData(List<LMUnlockable> newData) {
            Plugin.Instance.Mls.LogInfo("Importing LMU data..");
            foreach (LMUnlockable importUnlock in newData) {
                foreach (LMUnlockable unlock in Unlocks) {
                    if (unlock.Name == importUnlock.Name) {
                        unlock.OverrideData(importUnlock);
                    }
                }
            }
        }


        public void BuyMoon(string moon) {
            Plugin.Instance.Mls.LogInfo($"{moon}: Moon was bought!");
            var unlock = Unlocks.Where(unlock => unlock.Name == moon).FirstOrDefault();
            if (ConfigManager.DiscountMode) {
                if (unlock.BuyCount < ConfigManager.DiscountsCount) {
                    unlock.BuyCount++;
                    unlock.ApplyPrice();
                }
            } else {
                unlock.BuyCount++;
                unlock.ApplyPrice();
            }
            Plugin.Instance.Mls.LogInfo($"{unlock.Name}: Set buy count to {unlock.BuyCount}");

            if (ConfigManager.DiscoveryMode) {
                // TRAVEL DISCOVERY
                if (ConfigManager.TravelDiscoveries && DiscoveryCandidates.Count > 0) {
                    TravelDiscovery(unlock);
                }
            }
            NetworkManager.Instance.ServerSendUnlockables(Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 2);
        }

        private void TravelDiscovery(LMUnlockable unlock) {
            if (UnityEngine.Random.Range(0, 100) < ConfigManager.TravelDiscoveryChance) {
                Plugin.Instance.Mls.LogInfo($"Travel Discovery triggered! (Chance: {ConfigManager.TravelDiscoveryChance}%)");
                Plugin.Instance.Mls.LogInfo($"Travel Discovery Candidates: {string.Join(", ", DiscoveryCandidates.Select(unlock => unlock.Name))}");
                List<LMUnlockable> tdCandidates = DiscoveryCandidates;
                
                string tdMessageGroupName = string.Empty;
                if (ConfigManager.TravelDiscoveryMatchGroup) {
                    LMGroup moonGroup = MatchMoonGroup(unlock, DiscoveryCandidates);
                    if (moonGroup.Members != null && moonGroup.Members.Count > 0) {
                        Plugin.Instance.Mls.LogDebug($"Group name: {moonGroup.Name}");
                        tdCandidates = moonGroup.Members;
                        if (!string.IsNullOrEmpty(moonGroup.Name)) tdMessageGroupName = $" to <color=red>{moonGroup.Name}</color>";
                    }
                }

                List<LMUnlockable> travelDiscoveries;
                if (ConfigManager.CheapMoonBias > 0f && ConfigManager.CheapMoonBiasTravelDiscovery) {
                    travelDiscoveries = RandomSelector.GetWeighted(RandomSelector.CalculateBiasedWeights(tdCandidates), ConfigManager.TravelDiscoveryCount);
                } else {
                    travelDiscoveries = RandomSelector.Get(tdCandidates, ConfigManager.TravelDiscoveryCount);
                }

                foreach (var d in travelDiscoveries) {
                    d.Discovered = true;
                    if (ConfigManager.TravelDiscoveryPermanent) {
                        d.PermanentlyDiscovered = true;
                        Plugin.Instance.Mls.LogDebug($"{d.Name}: Discovery is permanent");
                    }
                }

                if (travelDiscoveries.Count > 1) {
                    HUDManager.Instance.AddTextToChatOnServer($"Discovered new moons on route{tdMessageGroupName}:\n <color=white>{string.Join(", ", travelDiscoveries.Select(td => td.Name))}</color>");
                    Plugin.Instance.Mls.LogInfo($"Travel Discoveries: [ {string.Join(", ", travelDiscoveries)} ]");
                } else if (travelDiscoveries.Count == 1) {
                    HUDManager.Instance.AddTextToChatOnServer($"Discovered new moon on route{tdMessageGroupName}:\n <color=white>{travelDiscoveries.FirstOrDefault().Name}</color>");
                    Plugin.Instance.Mls.LogInfo($"Travel Discovery: [ {string.Join(", ", travelDiscoveries.Select(discovery => discovery.Name))} ]");
                }
                NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"New {travelDiscoveries.Count.SinglePluralWord("Discovery")}!", Text = $"Autopilot discovered new {travelDiscoveries.Count.SinglePluralWord("moon")} during travel{tdMessageGroupName}.\n" +
                    $"Moon catalogue updated!", Key = "LMU_TravelDiscovery" });
                Plugin.Instance.Mls.LogInfo($"Travel Discoveries: {string.Join(", ", travelDiscoveries.Select(unlock => unlock.Name))}");
            }
        }

        internal void InitializeUnlocks() {
            if (AllLevels == null || AllLevels.Count == 0) {
                Plugin.Instance.Mls.LogFatal($"Unable to find levels!");
            }
            Plugin.Instance.Mls.LogInfo("Initializing LMUnlockables from Extended levels..");
            foreach (var level in AllLevels) {
                if (level == null || level.SelectableLevel == null || level.NumberlessPlanetName == "Liquidation" || level.NumberlessPlanetName == "Gordion")
                    continue;
                Unlocks.Add(new LMUnlockable(level.NumberlessPlanetName, level.RoutePrice, level.IsRouteHidden, level.IsRouteLocked));
            }
            LogUnlockables(true);
        }

        private void InitializeNewGame() {
            if (ConfigManager.DiscoveryMode) {
                Plugin.Instance.Mls.LogInfo($"Shuffling moon rotation on new game init");
                ShuffleDiscoverable();
                // Hide [NEW] discovery tag permanently from all moons in initial rotation
                Unlocks.Where(unlock => unlock.Discovered).Do(unlock => { unlock.DiscoveredOnce = true; });
            }
            // Shuffle Moon Sales
            if (ConfigManager.Sales) {
                foreach (var unlock in Unlocks) {
                        unlock.RefreshSale();
                } 
            }
        }

        private void ShuffleDiscoverable() {
            // Reset rotation
            foreach (var candidate in Unlocks.Where(unlock => unlock.OriginallyHidden == false && unlock.OriginallyLocked == false)) {
                if (candidate.NewDiscovery) candidate.NewDiscovery = false;
                candidate.Discovered = false;
            }
            // increase counts
            if (ConfigManager.DiscoveryFreeCountIncreaseBy > 0) {
                var newFreeCount = ConfigManager.DiscoveryFreeCountBase + (ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryFreeCountIncreaseBy;
                DiscoveredFreeCount = newFreeCount;
                Plugin.Instance.Mls.LogInfo($"Increasing DiscoverFreeCount (Base = {ConfigManager.DiscoveryFreeCountBase}, Increase = {(ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryFreeCountIncreaseBy}, Result = {newFreeCount}, Corrected = {DiscoveredFreeCount})");
            } else {
                DiscoveredFreeCount = ConfigManager.DiscoveryFreeCountBase;
                Plugin.Instance.Mls.LogInfo($"DiscoveredFreeCount (Config value = {ConfigManager.DiscoveryFreeCountBase}, Corrected = {DiscoveredFreeCount})");
            }
            if (ConfigManager.DiscoveryDynamicFreeCountIncreaseBy > 0 ) {
                var newDynamicFreeCount = ConfigManager.DiscoveryDynamicFreeCountBase + (ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryDynamicFreeCountIncreaseBy;
                DiscoveredDynamicFreeCount = newDynamicFreeCount;
                Plugin.Instance.Mls.LogInfo($"Increasing DiscoveredDynamicFreeCount (Base = {ConfigManager.DiscoveryDynamicFreeCountBase}, Increase = {(ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryDynamicFreeCountIncreaseBy}, Result = {newDynamicFreeCount}, Corrected = {DiscoveredDynamicFreeCount})");
            } else {
                DiscoveredDynamicFreeCount = ConfigManager.DiscoveryDynamicFreeCountBase;
                Plugin.Instance.Mls.LogInfo($"DiscoveredDynamicFreeCount (Config value = {ConfigManager.DiscoveryDynamicFreeCountBase}, Corrected = {DiscoveredDynamicFreeCount})");
            }
            if (ConfigManager.DiscoveryPaidCountIncreaseBy > 0) {
                var newPaidCount = ConfigManager.DiscoveryPaidCountBase + (ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryPaidCountIncreaseBy;
                DiscoveredPaidCount = newPaidCount;
                Plugin.Instance.Mls.LogInfo($"Increasing DiscoveredPaidCount (Base = {ConfigManager.DiscoveryPaidCountBase}, Increase = {(ConfigManager.DiscoveryShuffleEveryDay ? DayCount : QuotaCount) * ConfigManager.DiscoveryPaidCountIncreaseBy}, Result = {newPaidCount}, Corrected = {DiscoveredPaidCount})");
            } else {
                DiscoveredPaidCount = ConfigManager.DiscoveryPaidCountBase;
                Plugin.Instance.Mls.LogInfo($"DiscoveredPaidCount (Config value = {ConfigManager.DiscoveryPaidCountBase}, Corrected = {DiscoveredPaidCount})");
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
                Plugin.Instance.Mls.LogWarning("All moons would have been hidden from the terminal! Force discovering a free moon..");
                var unlock = Unlocks.Where(unlock => unlock.ExtendedLevel.RoutePrice == 0).FirstOrDefault();
                if (unlock == null) {
                    Plugin.Instance.Mls.LogWarning("Can't find any free moon to display in moon catalogue! You probably want at least one free moon available at all times.. Falling back to a paid moon!");
                }
                unlock = Unlocks.FirstOrDefault();
                if (unlock == null) {
                    Plugin.Instance.Mls.LogError("Can't find any moon! No moons initialized. Please check your configs (LMU + LLL). If this persists report it on GitHub or Discord.");
                    return;
                }
                if (unlock != null) {
                    unlock.Discovered = true;
                }
            }
            if (DayCount > 0) {
                HUDManager.Instance.AddTextToChatOnServer("Moon catalogue updated!");
                NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Moon catalogue updated!", Text = $"New moons available. Use the computer terminal to route the ship.", Key = "LMU_Shuffle" });
            }
            Plugin.Instance.Mls.LogInfo($"After shuffling check if we have to reroute to a discovered free moon..");
        }

        private void RerouteShipToFreeMoon() {
            if (Unlocks.Any(unlock => (unlock.Discovered || unlock.PermanentlyDiscovered ) && unlock.Name == LevelManager.CurrentExtendedLevel.NumberlessPlanetName) || LevelManager.CurrentExtendedLevel.NumberlessPlanetName == "Gordion") {
                Plugin.Instance.Mls.LogInfo($"Current moon is discovered. Not rerouting ship.");
            } else {
                var currentDiscoveredFreeMoons = DynamicFreeMoons.Where(unlock => !unlock.OriginallyLocked && !unlock.OriginallyHidden && (unlock.Discovered || unlock.PermanentlyDiscovered)).ToList();
                if (currentDiscoveredFreeMoons.Count < 1) {
                    Plugin.Instance.Mls.LogWarning("Can't find any free and discovered moon! You probably want at least one free moon available at all times.. Abort auto routing ship!");
                    return;
                }
                var randomDiscoveredFreeMoon = currentDiscoveredFreeMoons[UnityEngine.Random.Range(0, currentDiscoveredFreeMoons.Count)].ExtendedLevel;
                Plugin.Instance.Mls.LogInfo($"Current moon is not discovered! Rerouting ship to {randomDiscoveredFreeMoon.NumberlessPlanetName}..");
                if (DayCount > 0) {
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Dangerous conditions!", Text = $"Conditions too dangerous to stay in orbit! Auto routing the ship to a safe moon..", Key = "LMU_RerouteFree" });
                }
                DelayHelper.Instance.ExecuteAfterDelay(() => { StartOfRound.Instance.ChangeLevelServerRpc(randomDiscoveredFreeMoon.SelectableLevel.levelID, Terminal.groupCredits); }, 3.5f);
            }
        }

        private void AddFreeToRotation(int amount) {
            foreach (var candidate in RandomSelector.Get(DiscoveryFreeCandidates, amount)) {
                candidate.Discovered = true;
            }
        }

        private void AddDynamicFreeToRotation(int amount) {
            foreach (var candidate in RandomSelector.Get(DiscoveryDynamicFreeCandidates, amount)) {
                candidate.Discovered = true;
            }
        }
        
        private void AddPaidToRotation(int amount) {
            List<LMUnlockable> candidates = new List<LMUnlockable>();
            if (ConfigManager.CheapMoonBias > 0f && ConfigManager.CheapMoonBiasPaidRotation) {
                candidates.AddRange(RandomSelector.GetWeighted(RandomSelector.CalculateBiasedWeights(DiscoveryPaidCandidates),amount));
            }
            else {
                candidates.AddRange(RandomSelector.Get(DiscoveryPaidCandidates, amount));
            }
            foreach (var candidate in candidates) {
                candidate.Discovered = true;
            }
        }
        private void ApplyDiscoveryWhitelist() {
            if (ConfigManager.DiscoveryMode && ConfigManager.DiscoveryWhitelistMoons.Count > 0) {
                Plugin.Instance.Mls.LogInfo($"Whitelist: {string.Join(", ", ConfigManager.DiscoveryWhitelistMoons)}");
                foreach (var entry in ConfigManager.DiscoveryWhitelistMoons) {
                    bool matched = false;
                    foreach (var unlock in Unlocks) {
                        if (unlock.Name.Contains(entry.Trim(), StringComparison.OrdinalIgnoreCase)) {
                            matched = true;
                            unlock.Discovered = true;
                            break;
                        }
                    }
                    if (!matched) Plugin.Instance.Mls.LogWarning($"Couldn't match whitelist entry! Is this a valid moon name: {entry} ?");
                }
            }
        }
        private LMGroup MatchMoonGroup(LMUnlockable matchingUnlock, List<LMUnlockable> unlocksToMatch) {
            Plugin.Instance.Mls.LogInfo($"Matching moon {matchingUnlock.Name}: Matching against = [ {string.Join(", ", unlocksToMatch.Select(unlock => unlock.Name))} ]");
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
                        Plugin.Instance.Mls.LogInfo($"Matching moon {matchingUnlock.Name}: Matches by price = [ {string.Join(", ", priceMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Members = priceMatches };
                    }
                    break;
                case "PriceRange":
                    List<LMUnlockable> pricerangeMatches = new List<LMUnlockable>();
                    foreach (var unlock in unlocksToMatch) {
                        if (unlock.OriginalPrice >= matchingUnlock.OriginalPrice - ConfigManager.MoonGroupMatchingPriceRange && unlock.OriginalPrice <= matchingUnlock.OriginalPrice + ConfigManager.MoonGroupMatchingPriceRange) {
                            pricerangeMatches.Add(unlock);
                        }
                    }
                    if (pricerangeMatches.Count > 0) {
                        Plugin.Instance.Mls.LogInfo($"Matching moon {matchingUnlock.Name}: Matches by price range = [ {string.Join(", ", pricerangeMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Members = pricerangeMatches };
                    }
                    break;
                case "PriceRangeUpper":
                    List<LMUnlockable> pricerangeUpperMatches = new List<LMUnlockable>();
                    foreach (var unlock in unlocksToMatch) {
                        if (unlock.OriginalPrice >= matchingUnlock.OriginalPrice && unlock.OriginalPrice <= matchingUnlock.OriginalPrice + ConfigManager.MoonGroupMatchingPriceRange) {
                            pricerangeUpperMatches.Add(unlock);
                        }
                    }
                    if (pricerangeUpperMatches.Count > 0) {
                        Plugin.Instance.Mls.LogInfo($"Matching moon {matchingUnlock.Name}: Matches by price range = [ {string.Join(", ", pricerangeUpperMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Members = pricerangeUpperMatches };
                    }
                    break;
                case "Tag":
                    List<LMUnlockable> tagMatches = new List<LMUnlockable>();
                    List<ContentTag> matchingTags = matchingUnlock.ExtendedLevel.ContentTags;
                    ContentTag randomTag = matchingTags[UnityEngine.Random.Range(0, matchingTags.Count)];
                    foreach (var unlock in unlocksToMatch) {
                        if (unlock.ExtendedLevel.ContentTags.Select(tag => tag.contentTagName.ToLower()).Contains(randomTag.contentTagName.ToLower()) && !tagMatches.Contains(unlock))
                            tagMatches.Add(unlock);        
                    }
                    if (tagMatches.Count > 0) {
                        Plugin.Instance.Mls.LogInfo($"Matching moon {matchingUnlock.Name}: Matches by LLL tags = [ {string.Join(", ", tagMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Members = tagMatches };
                    }
                    break;
                case "Custom":
                    Dictionary<string, List<string>> matchingCustomGroups = matchingUnlock.GetMatchingCustomGroups();
                    if (matchingCustomGroups == null || matchingCustomGroups.Count == 0)
                        break;
                    string randomCustomGroupName = matchingCustomGroups.Keys.ToList()[UnityEngine.Random.Range(0, matchingCustomGroups.Count)];
                    if (matchingCustomGroups.Count > 1) {
                        Plugin.Instance.Mls.LogInfo($"Matching moon {matchingUnlock.Name}: Moon is member of multiple groups. Selected {randomCustomGroupName} for matching.");
                    }
                    List<string> randomCustomGroup = matchingCustomGroups[randomCustomGroupName];
                    Plugin.Instance.Mls.LogInfo($"Matching moon {matchingUnlock.Name}: {randomCustomGroupName} members = [ {string.Join(", ", randomCustomGroup)} ]");
                    List<LMUnlockable> groupMatches = new List<LMUnlockable>();
                    foreach (var unlock in unlocksToMatch) {
                        if (randomCustomGroup.Contains(unlock.Name)) {
                            groupMatches.Add(unlock);
                        }
                    }
                    if (groupMatches.Count > 0) {
                        Plugin.Instance.Mls.LogInfo($"Matching moon {matchingUnlock.Name}: Matches by custom groups = [ {string.Join(", ", groupMatches.Select(unlock => unlock.Name))} ]");
                        return new LMGroup() { Name = randomCustomGroupName, Members = groupMatches };
                    }
                    break;
                default:
                    Plugin.Instance.Mls.LogError($"Missing moon group matching method!");
                    return new LMGroup();
            }    
            Plugin.Instance.Mls.LogInfo($"No matching moons found!");
            return new LMGroup();
        }
        private string ReplaceTerminalPreview(ExtendedLevel extendedLevel, PreviewInfoType infoType) {
            var unlock = Unlocks.Where(unlock => unlock.ExtendedLevel == extendedLevel).FirstOrDefault();
            if (unlock == null) {
                Plugin.Instance.Mls.LogError($"Couldn't get unlock for Terminal preview text replacement!");
                return string.Empty;
            }
            return unlock.GetMoonPreviewText(infoType);
        }
        public void ApplyUnlocks() {
            foreach (var unlock in Unlocks) {
                unlock.ApplyPrice();
                if (ConfigManager.DiscoveryMode) {
                    unlock.ApplyDiscoverability();
                }
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
        }

        private bool LoadAndImportSavaData() {
            Dictionary<string, object> savedata = SaveManager.Savedata;
            if (savedata != null && savedata.ContainsKey("LMU_Unlockables")) {
                Plugin.Instance.Mls.LogInfo($"LMU save data detected!");
                Plugin.Instance.Mls.LogInfo($"Importing LMU data..");
                ImportUnlockableData((List<LMUnlockable>)savedata["LMU_Unlockables"]);
                if (savedata.ContainsKey("LMU_QuotaCount")) {
                    QuotaCount = (int)savedata["LMU_QuotaCount"];
                    Plugin.Instance.Mls.LogInfo($"Loading QuotaCount: {QuotaCount}.");
                }
                if (savedata.ContainsKey("LMU_DayCount")) {
                    DayCount = (int)savedata["LMU_DayCount"];
                    Plugin.Instance.Mls.LogInfo($"Loading DayCount: {DayCount}.");
                }
                if (savedata.ContainsKey("LMU_QuotaUnlocksCount")) {
                    QuotaUnlocksCount = (int)savedata["LMU_QuotaUnlocksCount"];
                    Plugin.Instance.Mls.LogInfo($"Loading QuotaUnlocksCount: {QuotaUnlocksCount}.");
                }
                if (savedata.ContainsKey("LMU_QuotaDiscountsCount")) {
                    QuotaDiscountsCount = (int)savedata["LMU_QuotaDiscountsCount"];
                    Plugin.Instance.Mls.LogInfo($"Loading QuotaDiscountsCount: {QuotaDiscountsCount}.");
                }
                if (savedata.ContainsKey("LMU_QuotaFullDiscountsCount")) {
                    QuotaFullDiscountsCount = (int)savedata["LMU_QuotaFullDiscountsCount"];
                    Plugin.Instance.Mls.LogInfo($"Loading QuotaFullDiscountsCount: {QuotaFullDiscountsCount}.");
                }
                Plugin.Instance.Mls.LogInfo($"Finished loading LMU save data.");
                return true;
            } else if (savedata != null &&  savedata.ContainsKey("LMU_UnlockedMoons")) {
                Plugin.Instance.Mls.LogInfo($"Legacy LMU save data detected! Migrating..");
                Dictionary<string, int> unlockedMoon = (Dictionary<string, int>)savedata["LMU_UnlockedMoons"];
                foreach (var moon in unlockedMoon) {
                    foreach (var unlock in Unlocks) {
                        if (unlock.Name == moon.Key) {
                            unlock.BuyCount = moon.Value;
                            Plugin.Instance.Mls.LogInfo($"Migrated unlock data for {moon.Key}: {moon.Value}.");
                        }
                    }
                }
                if (savedata.ContainsKey("LMU_QuotaCount")) {
                    QuotaCount = (int)savedata["LMU_QuotaCount"];
                    Plugin.Instance.Mls.LogInfo($"Migrating QuotaCount: {QuotaCount}.");
                }
                Plugin.Instance.Mls.LogInfo($"Finished migrating legacy LMU save data.");
                Plugin.Instance.Mls.LogInfo($"Loading done. Applying migrated data before new game init..");
                ApplyUnlocks();
                InitializeNewGame();
                return true;
            } else if (savedata != null &&  savedata.ContainsKey("UnlockedMoons")) {
                Plugin.Instance.Mls.LogInfo($"Permanent Moons save data detected! Migrating..");
                List<string> pmMoons = (List<string>)savedata["UnlockedMoons"];
                foreach (var moon in pmMoons) {
                    foreach (var unlock in Unlocks) {
                        if (moon.Contains(unlock.Name, StringComparison.OrdinalIgnoreCase)) {
                            unlock.BuyCount = 1;
                            Plugin.Instance.Mls.LogInfo($"Migrated PM unlock for {unlock.Name}.");
                        }
                    }
                }
                if (savedata.ContainsKey("MoonQuotaNum")) {
                    QuotaCount = (int)savedata["MoonQuotaNum"];
                    Plugin.Instance.Mls.LogInfo($"Migrated PM MoonQuotaNum (QuotaCount): {QuotaCount}.");
                }
                Plugin.Instance.Mls.LogInfo($"Finished migrating Permanent Moons save data.");
                Plugin.Instance.Mls.LogInfo($"Loading done. Applying migrated data before new game init.");
                ApplyUnlocks();
                InitializeNewGame();
                return true;
            } else {
                Plugin.Instance.Mls.LogInfo($"No save data found! New save..");
                return false;
            }
        }
    }
}
