using BepInEx.Configuration;
using System.IO;
using BepInEx;
using System.Linq;
using System;
using System.Collections.Generic;
using LethalLevelLoader;

namespace LethalMoonUnlocks {
    public class ConfigManager {
        private static ConfigFile _configFile;

        internal static bool ResetWhenFired { get; private set; }
        internal static bool DisplayTerminalTags { get; private set; }
        internal static bool ShowTagInOrbit { get; private set; }
        internal static bool ShowTagNewDiscovery { get; private set; }
        internal static bool ShowTagExplored { get; private set; }
        internal static bool ShowTagUnlockDiscount { get; private set; }
        internal static bool ShowTagPermanentDiscovery { get; private set; }
        internal static bool ShowTagSale { get; private set; }
        internal static bool ShowTagGroups { get; private set; }
        internal static bool ShowAlerts { get; private set; }
        internal static bool ChatMessages { get; private set; }
        internal static bool UnlockMode { get; private set; }
        internal static int UnlocksResetAfterVisits { get; private set; }
        internal static bool UnlocksResetAfterVisitsPermDiscovery { get; private set; }
        internal static bool QuotaUnlocks { get; private set; }
        internal static int QuotaUnlockChance { get; private set; }
        internal static int QuotaUnlockCount { 
            get {
                return RandomHelper.Range(_quotaUnlockCountMin, _quotaUnlockCountMax + 1);
            } 
        }
        private static int _quotaUnlockCountMin;
        private static int _quotaUnlockCountMax;
        internal static int QuotaUnlockMaxCount { get; private set; }
        internal static int QuotaUnlockMaxPrice { get; private set; }
        internal static bool DiscountMode { get; private set; }
        private static string DiscountsString { get; set; }
        internal static List<int> Discounts {
            get {
                string[] discounts = DiscountsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                List<int> list = [];
                foreach (string discount in discounts) list.Add(int.Parse(discount));
                return list;
            }
        }
        internal static int DiscountsCount {
            get {
                return Discounts.Count();
            }
        }
        internal static int DiscountsResetAfterVisits { get; private set; }
        internal static bool DiscountsResetAfterVisitsPermDiscovery { get; private set; }
        internal static bool QuotaDiscounts {  get; private set; }
        internal static int QuotaDiscountChance { get; private set; }
        internal static int QuotaDiscountCount {
            get {
                return RandomHelper.Range(_quotaDiscountCountMin, _quotaDiscountCountMax + 1);
            }
        }
        private static int _quotaDiscountCountMin;
        private static int _quotaDiscountCountMax;
        internal static int QuotaDiscountMaxCount { get; private set; }
        internal static int QuotaDiscountMaxPrice { get; private set; }
        internal static bool QuotaFullDiscounts { get; private set; }
        internal static int QuotaFullDiscountChance { get; private set; }
        internal static int QuotaFullDiscountCount {
            get {
                return RandomHelper.Range(_quotaFullDiscountCountMin, _quotaFullDiscountCountMax + 1);
            }
        }
        private static int _quotaFullDiscountCountMin;
        private static int _quotaFullDiscountCountMax;
        internal static int QuotaFullDiscountMaxCount { get; private set; }
        internal static int QuotaFullDiscountMaxPrice { get; private set; }
        public static bool DiscoveryMode { get; private set; }
        private static string DiscoveryWhitelist { get; set; }
        internal static List<string> DiscoveryWhitelistMoons {
            get {
                return DiscoveryWhitelist.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()).ToList();
            }
        }
        internal static bool DiscoveryKeepUnlocks { get; private set; }
        internal static bool DiscoveryKeepDiscounts { get; private set; }
        internal static int DiscoveryFreeCountBase { get; private set; }
        internal static int DiscoveryFreeCountIncreaseBy { get; private set; }
        internal static int DiscoveryDynamicFreeCountBase { get; private set; }
        internal static int DiscoveryDynamicFreeCountIncreaseBy { get; private set; }
        internal static int DiscoveryPaidCountBase { get; private set; }
        internal static int DiscoveryPaidCountIncreaseBy { get; private set; }
        internal static int PermanentlyDiscoverFreeMoonsOnLanding { get; private set; }
        internal static int PermanentlyDiscoverPaidMoonsOnLanding { get; private set; }
        internal static bool PermanentlyDiscoverHiddenMoonsOnVisit { get; private set; }
        internal static bool DiscoveryShuffleEveryDay { get; private set; }
        internal static bool DiscoveryNeverShuffle { get; private set; }
        internal static bool QuotaDiscoveries { get; private set; }
        internal static int QuotaDiscoveryChance { get; private set; }
        internal static int QuotaDiscoveryCount {
            get {
                return RandomHelper.Range(_quotaDiscoveryCountMin, _quotaDiscoveryCountMax + 1);
            }
        }
        private static int _quotaDiscoveryCountMin;
        private static int _quotaDiscoveryCountMax;
        internal static bool QuotaDiscoveryPermanent { get; private set; }
        internal static bool QuotaDiscoveryCheapestGroup { get; private set; }
        internal static bool QuotaDiscoveryCheapestGroupFallback { get; private set; }
        internal static bool QuotaDiscoveryCheapestConstellation { get; private set; }
        //internal static bool QuotaDiscoveryForceConstellationProgression { get; private set; }
        internal static bool TravelDiscoveries { get; private set; }
        internal static int TravelDiscoveryChance { get; private set; }
        internal static int TravelDiscoveryCount {
            get {
                return RandomHelper.Range(_travelDiscoveryCountMin, _travelDiscoveryCountMax + 1);
            }
        }
        private static int _travelDiscoveryCountMin;
        private static int _travelDiscoveryCountMax;
        internal static bool TravelDiscoveryPermanent { get; private set; }
        internal static bool TravelDiscoveryMatchGroup { get; private set; }
        internal static bool TravelDiscoveryMatchGroupFallback { get; private set; }
        internal static bool NewDayDiscoveries { get; private set; }
        internal static int NewDayDiscoveryChance { get; private set; }
        internal static int NewDayDiscoveryCount {
            get {
                return RandomHelper.Range(_newDayDiscoveryCountMin, _newDayDiscoveryCountMax + 1);
            }
        }
        private static int _newDayDiscoveryCountMin;
        private static int _newDayDiscoveryCountMax;
        internal static bool NewDayDiscoveryPermanent { get; private set; }
        internal static bool NewDayDiscoveryMatchGroup { get; private set; }
        internal static bool NewDayDiscoveryMatchGroupFallback { get; private set; }
        internal static bool Sales { get; private set; }
        internal static int SalesChance { get; private set; }
        internal static bool SalesShuffleDaily { get; private set; }
        internal static int SalesMinDayCount { get; private set; }
        internal static int SalesRate {
            get { return RandomHelper.Range(_salesRateMin, _salesRateMax); }
        }
        private static int _salesRateMin;
        private static int _salesRateMax;
        private static bool AdvancedPrintMoonNames { get; set; }
        internal static bool AutoRerouteToCompany { get; set; }
        internal static bool GroupCreditsSavingBandAid { get; private set; }
        internal static bool EnableStoryProgression { get; private set; }
        internal static bool LMUStoryProgression { get; private set; }
        internal static bool GaletryStoryLock { get; private set; }
        internal static int GaletryStoryLockPaintingsAmount { get; private set; }
        internal static bool CheapMoonBiasIgnorePriceChanges { get; private set; }
        internal static bool CheapMoonBiasPaidRotation { get; private set; }
        internal static float CheapMoonBiasPaidRotationValue { get; private set; }
        internal static bool CheapMoonBiasQuotaDiscovery { get; private set; }
        internal static float CheapMoonBiasQuotaDiscoveryValue { get; private set; }
        internal static bool CheapMoonBiasNewDayDiscovery { get; private set; }
        internal static float CheapMoonBiasNewDayDiscoveryValue { get; private set; }
        internal static bool CheapMoonBiasTravelDiscovery { get; private set; }
        internal static float CheapMoonBiasTravelDiscoveryValue { get; private set; }
        internal static bool CheapMoonBiasQuotaUnlock { get; private set; }
        internal static float CheapMoonBiasQuotaUnlockValue { get; private set; }
        internal static bool CheapMoonBiasQuotaDiscount { get; private set; }
        internal static float CheapMoonBiasQuotaDiscountValue { get; private set; }
        internal static bool CheapMoonBiasQuotaFullDiscount { get; private set; }
        internal static float CheapMoonBiasQuotaFullDiscountValue { get; private set; }
        internal static string MoonGroupMatchingMethod { get; private set; }
        internal static int MoonGroupMatchingPriceRange { get; private set; }
        private static string MoonGroupMatchingCustom { get; set; }
        internal static Dictionary<string, List<string>> MoonGroupMatchingCustomDict { get; private set; }
        internal static int TerminalTagLineWidth { get; set; }
        internal static bool TerminalFontSizeOverride { get; set; }
        internal static float TerminalFontSize { get; set; }
        internal static int TerminalScrollAmount { get; set; }
        internal static bool TerminalShowRiskWeather { get; set; }
        internal static bool PreferLQRisk { get; private set; }
        internal static bool MalfunctionsNavigation { get; private set; }
        internal static bool AlertMessageQueueing { get; private set; }
        public static bool LethalConstellationsOverridePrice { get; private set; }
        internal static bool PreferGaletry { get; private set; }

        internal static bool OverrideHidden { get; private set; }
        private static string OverrideHiddenList { get; set; }
        internal static List<string> OverrideHiddenListMoons {
            get {
                return OverrideHiddenList.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()).ToList();
            }
        }
        internal static bool OverrideLocked { get; private set; }
        private static string OverrideLockedList { get; set; }
        internal static List<string> OverrideLockedListMoons {
            get {
                return OverrideLockedList.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()).ToList();
            }
        }

        internal static void Initialize(ConfigFile cfg) {
            string legacyConfigPath = Path.Combine(Paths.ConfigPath, "LethalMoonUnlocks.cfg");
            if (File.Exists(legacyConfigPath)) {
                Logger.LogWarning("Legacy config file found. Migrating to default config..");
                MigrateLegacyConfig(legacyConfigPath, cfg);
            } else {
                _configFile = cfg;
            }
        }

        internal static void RefreshConfig() {
            Logger.LogInfo("Refreshing config..");
            RefreshValues();
            if (MoonGroupMatchingCustomDict == null) {
                MoonGroupMatchingCustomDict = ParseCustomMoonGroups();
            }
        }
        internal static Dictionary<string, List<string>> ParseCustomMoonGroups() {
            if (AdvancedPrintMoonNames) {
                Logger.LogWarning($"Printing available moon names for custom moon groups..");
                Logger.LogWarning($"{string.Join(", ", PatchedContent.ExtendedLevels.Where(level => level.NumberlessPlanetName != "Gordion" && level.NumberlessPlanetName != "Liquidation").Select(level => level.name))}");
            }
            Dictionary<string, List<string>> customGroups = new Dictionary<string, List<string>>();
            if (MoonGroupMatchingCustom == string.Empty) {
                Logger.LogInfo($"No custom moon group defined. Skip parsing..");
                return customGroups;
            }
            string[] groupStrings = MoonGroupMatchingCustom.Split('|');
            foreach (string groupString in groupStrings) {
                string groupName = groupString.Split(":").First().Trim();
                string groupMemberString = groupString.Split(':').Last().Trim();
                string[] groupMembers = groupMemberString.Split(",");
                List <string> members = new List<string>();
                foreach (string member in groupMembers) {
                    members.Add(member.Trim());
                }
                if (groupName == null || groupName == string.Empty) {
                    Logger.LogWarning("Couldn't parse custom moon group name. Make sure you're using the correct format!");
                    continue;
                } else if (groupMemberString == string.Empty || members.Count == 0) {
                    Logger.LogWarning("Couldn't parse custom moon group! Name null or empty or members not found!");
                    continue;
                } else {
                    customGroups[groupName] = members;
                }
            }
            Logger.LogInfo($"Parsing custom moon groups:");
            // fancy table
            Logger.LogInfo($"{"Name", -32} {"Members", -60}");
            foreach (var group in customGroups) {
                Logger.LogInfo($"{group.Key,-32} [{string.Join(", ", group.Value) + ']',-60}");
            }
            return customGroups;
        }
        private static void RefreshValues() {
            ResetWhenFired = GetConfigValue("1 - General settings", "Reset when fired", true, "Reset your progress when being fired. Unlocks, Discounts, and permanently discovered moons will all be wiped.\n" +
                "Unlocks, Discounts, Permanently Discovered moons, ..  all of it will persist unless you create a new save.\n" +
                "The only exception to this option is the base selection of moons in Discovery Mode.");
            ChatMessages = GetConfigValue("1 - General settings", "Show chat messages", true, "When enabled, LethalMoonUnlocks will send messages to the in-game chat whenever something relevant happens.");
            ShowAlerts = GetConfigValue("1 - General settings", "Show alert messages", false, "When enabled, LethalMoonUnlocks will display alert messages whenever something relevant happens.");

            DisplayTerminalTags = GetConfigValue("1.1 - Terminal moon tags", "Display tags in terminal", false, "When enabled, LethalMoonUnlocks will display additional tags in the Terminal moon catalog.\n" +
                "These tags will indicate various conditions, such as a moon being unlocked or discounted, being on sale, etc.\n" +
                "If custom moon groups or matching by LLL tag are enabled, you'll also see the custom groups or LLL tags a moon is associated with.\n" +
                "NOTE: At this time additional tags will only show in the standard LLL moon catalog and TerminalFormatter. Any other mod replacing the 'moons' command will probably cause issues.");
            ShowTagInOrbit = GetConfigValue("1.1 - Terminal moon tags", "In orbit tag", true, "Display a tag to indicate the moon you're currently orbiting.");
            ShowTagExplored = GetConfigValue("1.1 - Terminal moon tags", "Exploration tag", true, "Display a tag to indicate which moons have not been landed on yet. After landing once, it will keep track of how many times you've landed in total.");
            ShowTagUnlockDiscount = GetConfigValue("1.1 - Terminal moon tags", "Unlock discount tag", true, "Display a tag to indicate Unlocks and Discounts as well as how many routes are left before they expire.");
            ShowTagNewDiscovery = GetConfigValue("1.1 - Terminal moon tags", "New discovery tag", true, "Discovery Mode only: display a tag to indicate which moons are new discoveries i.e. available in the moon catalog for the first time. The tag will vanish when you route to the moon or the moon catalog is shuffled.");
            ShowTagPermanentDiscovery = GetConfigValue("1.1 - Terminal moon tags", "Permanent discovery tag", true, "Discovery Mode only: display a tag to indicate permanently discovered moons.\n" +
                "Displays as [PINNED].");
            ShowTagSale = GetConfigValue("1.1 - Terminal moon tags", "Sales tag", true, "Moon Sales only: display a tag to indicate which moons are on sale, as well as the percentage of the sale.");
            ShowTagGroups = GetConfigValue("1.1 - Terminal moon tags", "Group tag", true, "Moon Group Matching only: display a tag to indicate groups a moon belongs to. Limited to custom group, LethalConstellations and LLL tag matching methods.");

            UnlockMode = GetConfigValue("2 - Unlock Mode (Default)", "Enable Unlock Mode", true, "Unlock Mode is the default mode, akin to the original Permanent Moons mod. In Unlock Mode, when you buy a paid moon, it will be 'unlocked'.\n" +
                "Once unlocked, moons are completely free, and by default, will stay free permanently.\n" +
                "NOTE: This setting and all settings relating to Unlocks will have no effect if Discount Mode is enabled!");
            UnlocksResetAfterVisits = GetConfigValue("2 - Unlock Mode (Default)", "Unlocks expire", 0, "Unlocks will expire after a set number of free routes, after which they will become paid again.\n" +
                "Set to 0 to disable this feature.");
            DiscoveryKeepUnlocks = GetConfigValue("2 - Unlock Mode (Default)", "Unlocked moons are permanently discovered", false, "Discovery Mode only: Every unlocked moon is also permanently discovered i.e. added to the moon catalog on top of your base selection.");
            UnlocksResetAfterVisitsPermDiscovery = GetConfigValue("2 - Unlock Mode (Default)", "Reset permanent discovery on unlock expiry", false, "Discovery Mode only: Reset a moon's permanent discovery status when its unlock expires.\n" +
                "This is the only way permanent discoveries can vanish during a run in Unlock Mode.\n");

            QuotaUnlocks = GetConfigValue("2.1 - Quota Unlocks", "Enable Quota Unlocks", false, "Quota Unlocks are rewarded for meeting the quota. When triggered, Quota Unlocks will grant you one or more unlocks for free.\n" +
                "The moons that are unlocked are randomly selected.");
            QuotaUnlockChance = GetConfigValue("2.1 - Quota Unlocks", "Quota Unlock trigger chance", 100, "The chance to trigger a Quota Unlock every time you meet the quota.", new AcceptableValueRange<int>(0, 100));
            _quotaUnlockCountMin = GetConfigValue("2.1 - Quota Unlocks", "Minimum unlocked moon count", 1, "The minimum number of moons that will be unlocked each time a Quota Unlock is triggered.", new AcceptableValueRange<int>(1, 10));
            _quotaUnlockCountMax = GetConfigValue("2.1 - Quota Unlocks", "Maximum unlocked moon count", 1, "The maximum number of moons that will be unlocked each time a Quota Unlock is triggered.", new AcceptableValueRange<int>(1, 10));
            QuotaUnlockMaxPrice = GetConfigValue("2.1 - Quota Unlocks", "Maximum moon price to unlock", 0, "Only consider moons up to this price to be unlocked.\n" +
                "Set to 0 to disable this feature.");
            QuotaUnlockMaxCount = GetConfigValue("2.1 - Quota Unlocks", "Limit number of unlocks", 0, "Limit how many Quota Unlocks you can receive during a run. After reaching the limit, Quota Unlocks will no longer be granted.\n" +
                "Set to 0 to disable this feature.");

            DiscountMode = GetConfigValue("3 - Discount Mode", "Enable Discount Mode", false, "In Discount Mode, Unlocks are replaced with Discounts.\n" +
                "Each time you route to a paid moon, you will unlock the next available discount rate until the final discount is reached.\n" +
                "The discount rates are fully customizable.");
            DiscountsString = GetConfigValue("3 - Discount Mode", "Discount rates", "50,75,100", "The discount rates that are applied to moon prices as a % off of the original routing price.\n" +
                "For example, '50,75,100', would make each moon 50% off after the first purchase, 75% off after the second purchase, and free after the third purchase.\n" +
                "Discount rates are separated by commas and can contain any number of rates");
            Logger.LogInfo($"Discount rates (% off): {string.Join(", ", Discounts.Select(discount => discount + "%"))}");
            DiscountsResetAfterVisits = GetConfigValue("3 - Discount Mode", "Discounts expire", 0, "Discounts will expire after a set number of free routes, after which they will return to their original price.\n" +
                "Set to 0 to disable this feature.\n" +
                "NOTE: The final discount rate must be set to '100' for this to work!");
            DiscoveryKeepDiscounts = GetConfigValue("3 - Discount Mode", "Discounted moons are permanently discovered", false, "Discovery Mode only: Every discounted moon is also permanently discovered i.e. added to the moon catalog on top of your base selection.");
            DiscountsResetAfterVisitsPermDiscovery = GetConfigValue("3 - Discount Mode", "Reset permanent discoveries on discount expiry", false, "Discovery Mode only: Reset a moon's permanent discovery status when its discount expires.\n" +
                "This is the only way permanent discoveries can vanish during a run in Discount Mode.\n");

            QuotaDiscounts = GetConfigValue("3.1 - Quota Discounts", "Enable Quota Discounts", false, "Quota Discounts are rewarded for meeting the quota. When triggered Quota Discounts will grant you one or more discounts for free.\n" +
                "The moons that are discounted are randomly selected.");
            QuotaDiscountChance = GetConfigValue("3.1 - Quota Discounts", "Quota Discount trigger chance", 100, "The chance to trigger a Quota Discount every time you meet the quota.\n", new AcceptableValueRange<int>(0, 100));
            _quotaDiscountCountMin = GetConfigValue("3.1 - Quota Discounts", "Minimum discounted moon count", 1, "The minimum number of moons that will receive a discount each time a Quota Discount is triggered.", new AcceptableValueRange<int>(1, 10));
            _quotaDiscountCountMax = GetConfigValue("3.1 - Quota Discounts", "Maximum discounted moon count", 1, "The maximum number of moons that will receive a discount each time a Quota Discount is triggered.", new AcceptableValueRange<int>(1, 10));
            QuotaDiscountMaxPrice = GetConfigValue("3.1 - Quota Discounts", "Maximum moon price to discount", 0, "Only consider moons up to this price to receive a discount.\n" +
                "Set to 0 to disable this feature");
            QuotaDiscountMaxCount = GetConfigValue("3.1 - Quota Discounts", "Limit number of discounts", 0, "Limit how many Quota Discounts you can receive during a run. After reaching the limit, Quota Discounts will no longer be granted.\n" +
                "Set to 0 to disable this feature");

            QuotaFullDiscounts = GetConfigValue("3.2 - Quota Full Discounts", "Enable Quota Full Discounts", false, "Quota Full Discounts are rewarded for meeting the quota. When triggered, Quota Full Discounts will apply the final discount rate to one or more moons for free.\n" +
                "The moons that are discounted are randomly selected.");
            QuotaFullDiscountChance = GetConfigValue("3.2 - Quota Full Discounts", "Quota Full Discount trigger chance", 100, "The chance to trigger a Quota Full Discount every time you meet the quota.", new AcceptableValueRange<int>(0, 100));
            _quotaFullDiscountCountMin = GetConfigValue("3.2 - Quota Full Discounts", "Minimum fully discounted moon count", 1, "The minimum number of moons that will receive a full discount each time a Quota Full Discount is triggered.", new AcceptableValueRange<int>(1, 10));
            _quotaFullDiscountCountMax = GetConfigValue("3.2 - Quota Full Discounts", "Maximum fully discounted moon count", 1, "The maximum number of moons that will receive a full discount each time a Quota Full Discount is triggered.", new AcceptableValueRange<int>(1, 10));
            QuotaFullDiscountMaxPrice = GetConfigValue("3.2 - Quota Full Discounts", "Maximum moon price to fully discount", 0, "Only consider moons up to this price to receive a full discount.\n" +
                "Set to 0 to disable this feature");
            QuotaFullDiscountMaxCount = GetConfigValue("3.2 - Quota Full Discounts", "Limit number of full discounts", 0, "Limit how many Quota Full Discounts you can receive during a run. After reaching the limit, Quota Full Discounts will no longer be granted.\n" +
                "Set to 0 to disable this feature");

            DiscoveryMode = GetConfigValue("4 - Discovery Mode", "Enable Discovery Mode", false, "In Discovery Mode, you start with a limited selection of moons in the Terminal's moon catalog.\n" +
                "By default, this base selection of moons will be shuffled after every quota, and can also be configured to expand over time.\n" +
                "There are also various options to discover additional moons as you play.\n" +
                "Permanently discovered moons are added to the moon catalog on top of the base selection, and are not lost on shuffle.");
            
            DiscoveryNeverShuffle = GetConfigValue("4 - Discovery Mode", "Never shuffle", false, "Never shuffle the rotation of moons available in the moon catalog.\n" +
                "New moons must be discovered through other means, but once discovered, they won't vanish, since the selection is never shuffled.\n" +
                "NOTE: Overrides the 'Shuffle every day' option.");
            DiscoveryShuffleEveryDay = GetConfigValue("4 - Discovery Mode", "Shuffle every day", false, "Shuffle the rotation of moons available in the moon catalog every day, instead of after every quota.");
            
            DiscoveryWhitelist = GetConfigValue("4 - Discovery Mode", "Whitelist", "", "List of moons to keep discovered at all times.\n" +
                "For example, 'Experimentation, Assurance, Vow' would make these three moons start out as permanently discovered on every run.\n" +
                "Moon names must be separated by commas and must be exact matches. You can print the moon names to console/log by using the option in 'Advanced Settings'.");

            DiscoveryFreeCountBase = GetConfigValue("4 - Discovery Mode", "Free moons base count", 1, "The base amount of randomly selected free moons available in the moon catalog.\n" +
                "NOTE: 'Free' only considers moons that are free by default, or configured to be free. Moons that are free due to unlocks or discounts are excluded!");
            DiscoveryDynamicFreeCountBase = GetConfigValue("4 - Discovery Mode", "Dynamic free moons base count", 2, "The base amount of randomly selected dynamic free moons available in the moon catalog.\n" +
                "NOTE: 'Dynamic free' considers moons that are free due to unlocks or discounts in addition to those that are free by default, or configured to be free.");
            DiscoveryPaidCountBase = GetConfigValue("4 - Discovery Mode", "Paid moons base count", 3, "The base amount of randomly selected paid moons available in the moon catalog.\n" +
                "This is your paid moon rotation and typically the main way to discover new moons to buy - earning unlocks and discounts as you progress.");
            
            DiscoveryFreeCountIncreaseBy = GetConfigValue("4 - Discovery Mode", "Increase free moon count on shuffle", 0, "The amount of randomly selected free moons added to the rotation each time it's shuffled.\n" +
                "Set to 0 to disable this feature.");
            DiscoveryDynamicFreeCountIncreaseBy = GetConfigValue("4 - Discovery Mode", "Increase dynamic free moon count on shuffle by", 0, "The amount of randomly selected dynamic free moons added to the rotation each time it's shuffled.\n" +
                "Set to 0 to disable this feature.");
            DiscoveryPaidCountIncreaseBy = GetConfigValue("4 - Discovery Mode", "Increase paid moon count on shuffle", 0, "The amount of randomly selected paid moons added to the rotation each time it's shuffled.\n" +
                "Set to 0 to disable this feature.");

            PermanentlyDiscoverFreeMoonsOnLanding = GetConfigValue("4 - Discovery Mode", "Landings required to permanently discover free moons", -1, "Any free moon will be permanently discovered after a set amount of landings.\n" +
                "Set to -1 to disable this feature.\n" +
                "NOTE: A value of 0 makes every free moon ever discovered in any way permanently discovered. Not recommended.");
            PermanentlyDiscoverPaidMoonsOnLanding = GetConfigValue("4 - Discovery Mode", "Landings required to permanently discover paid moons", -1, "Any free moon will be permanently discovered after a set amount of landings.\n" +
                "Set to -1 to disable this feature.\n" +
                "NOTE: A value of 0 makes every paid moon ever discovered in any way permanently discovered. Not recommended.");
            PermanentlyDiscoverHiddenMoonsOnVisit = GetConfigValue("4 - Discovery Mode", "Permanently discover hidden moons after routing", false, "Any hidden (LLL config e.g. Embrion) will be permanently discovered after routed to once.");

            QuotaDiscoveries = GetConfigValue("4.1 - Quota Discoveries", "Enable Quota Discoveries", false, "Quota Discoveries grant additional moon discoveries when a new quota begins.\n" +
                "The moons that are discovered are randomly selected.");
            QuotaDiscoveryChance  = GetConfigValue("4.1 - Quota Discoveries", "Quota Discovery trigger chance", 100, "The chance to trigger a Quota Discovery every time you meet the quota.", new AcceptableValueRange<int>(0, 100));
            _quotaDiscoveryCountMin  = GetConfigValue("4.1 - Quota Discoveries", "Minimum quota discovery moon count", 1, "The minimum number of moons that will be discovered each time a Quota Discovery is triggered.", new AcceptableValueRange<int>(1, 10));
            _quotaDiscoveryCountMax  = GetConfigValue("4.1 - Quota Discoveries", "Maximum quota discovery moon count", 1, "The maximum number of moons that will be discovered each time a Quota Discovery is triggered.", new AcceptableValueRange<int>(1, 10));
            QuotaDiscoveryPermanent = GetConfigValue("4.1 - Quota Discoveries", "Quota Discoveries are permanent", false, "Moons discovered through Quota Discoveries will stay permanently discovered i.e. they won't vanish on shuffle.");
            QuotaDiscoveryCheapestGroup = GetConfigValue("4.1 - Quota Discoveries", "Quota Discovery match cheapest group", false, "Only considers moons from the group/constellation that has the currently cheapest undiscovered moon.\n" +
                "Can effectively discover the 'next tier' or group of moons. Set counts high to discover the entire group.\n" +
                "NOTE: Highly recommended to only use this with 'Quota Discoveries are permanent' or 'Never shuffle'!");
            QuotaDiscoveryCheapestGroupFallback = GetConfigValue("4.1 - Quota Discoveries", "Quota Discovery match cheapest group fallback", true, "When enabled will fallback to selecting from all discoverable moons when no moons could be matched.\n" +
                "NOTE: Only relevant when you have moons that are not assigned to any group/constellation.");
            QuotaDiscoveryCheapestConstellation = GetConfigValue("4.1 - Quota Discoveries", "Quota Discovery match cheapest constellation", false, "Only consider moons of the cheapest constellation. Overrides behaviour of 'match cheapest group'. \n" +
                "NOTE: Match cheapest group needs to be enabled.");
            //QuotaDiscoveryForceConstellationProgression = GetConfigValue("4.1 - Quota Discoveries", "Quota discovery force constellation progression", false, "When enabled forces players to buy every available constellation at least once. If there are constellations available that have not been bought no moons can be discovered by Quota discovery.\nNote that this includes discovering moons within the ")

            TravelDiscoveries = GetConfigValue("4.2 - Travel Discoveries", "Enable Travel Discoveries", false, "Travel Discoveries grant additional moon discoveries when routing to a paid moon\n" +
                "The moons that are discovered are randomly selected.");
            TravelDiscoveryChance  = GetConfigValue("4.2 - Travel Discoveries", "Travel Discovery trigger chance", 20, "The chance to trigger a Travel Discovery every time you route to a paid moon.", new AcceptableValueRange<int>(0, 100));
            _travelDiscoveryCountMin  = GetConfigValue("4.2 - Travel Discoveries", "Minimum travel discovery moon count", 1, "The minimum number of moons that will be discovered each time a Travel Discovery is triggered.", new AcceptableValueRange<int>(1, 10));
            _travelDiscoveryCountMax  = GetConfigValue("4.2 - Travel Discoveries", "Maximum travel discovery moon count", 1, "The maximum number of moons that will be discovered each time a Travel Discovery is triggered.", new AcceptableValueRange<int>(1, 10));
            TravelDiscoveryPermanent = GetConfigValue("4.2 - Travel Discoveries", "Travel Discoveries are permanent", false, "Moons discovered through Travel Discoveries will stay permanently discovered i.e. they won't vanish on shuffle.");
            TravelDiscoveryMatchGroup = GetConfigValue("4.2 - Travel Discoveries", "Travel Discovery group matching", false, "Only consider moons of the same group you're routing to for Travel Discoveries.");
            TravelDiscoveryMatchGroupFallback = GetConfigValue("4.2 - Travel Discoveries", "Travel Discovery group matching fallback", true, "When enabled will fallback to selecting from all discoverable moons when no moons could be matched.\n" +
                "NOTE: It is recommended to keep this on for matching by exact price but with other methods you might prefer to turn it off.");

            NewDayDiscoveries = GetConfigValue("4.3 - New Day Discoveries", "Enable New Day Discoveries", false, "New Day Discoveries grant additional moon discoveries at the start of a new day.\n" +
                "The moons that are discovered are randomly selected.");
            NewDayDiscoveryChance  = GetConfigValue("4.3 - New Day Discoveries", "New Day Discovery trigger chance", 20, "The chance to trigger a New Day Discovery at the start of a new day.\n" +
                "Make it a random occurence or guaranteed.", new AcceptableValueRange<int>(0, 100));
            _newDayDiscoveryCountMin  = GetConfigValue("4.3 - New Day Discoveries", "Minimum new day discovery moon count", 1, "The minimum number of moons to be discovered each time a New Day Discovery is granted.", new AcceptableValueRange<int>(1, 10));
            _newDayDiscoveryCountMax  = GetConfigValue("4.3 - New Day Discoveries", "Maximum new day discovery moon count", 1, "The maximum number of moons to be discovered each time a New Day Discovery is granted.", new AcceptableValueRange<int>(1, 10));
            NewDayDiscoveryPermanent = GetConfigValue("4.3 - New Day Discoveries", "New Day Discoveries are permanent", false, "Moons discovered through New Day Discoveries will stay permanently discovered i.e. they won't vanish on shuffle.");
            NewDayDiscoveryMatchGroup = GetConfigValue("4.3 - New Day Discoveries", "New Day Discovery group matching", false, "Only consider moons of the same group as the moon you're currently orbiting for New Day Discoveries.");
            NewDayDiscoveryMatchGroupFallback = GetConfigValue("4.3 - New Day Discoveries", "New Day Discovery group matching fallback", true, "When enabled will fallback to selecting from all discoverable moons when no moons could be matched.\n" +
                "NOTE: It is recommended to keep this on for matching by exact price but with other methods you might prefer to turn it off.");

            Sales = GetConfigValue("5 - Moon Sales", "Moon Sales", false, "Each moon has a chance to go on sale for a reduced routing price.\n" +
                "By default, Moon Sales are shuffled after every quota. Only non-free moons can go on sale.\n" +
                "NOTE: These sales are separate from discounts received via Discount Mode.");
            SalesShuffleDaily = GetConfigValue("5 - Moon Sales", "Shuffle sales daily", false, "Shuffle moon sales daily, instead of after every quota");
            SalesMinDayCount = GetConfigValue("5 - Moon Sales", "Minimum completed days before sales", 0, "Do not allow any moon sales until at least this many days have passed.\n" +
                "Before this threshold is reached, you can not get new sales when they're shuffled.", new AcceptableValueRange<int>(0, 30));
            SalesChance = GetConfigValue("5 - Moon Sales", "Moon Sale chance", 20, "The chance for each moon to go on sale every time sales are shuffled.", new AcceptableValueRange<int>(0, 100));
            _salesRateMin = GetConfigValue("5 - Moon Sales", "Minimum sale percent", 5, "The minimum sale percentage a moon can receive.", new AcceptableValueRange<int>(0, 100));
            _salesRateMax = GetConfigValue("5 - Moon Sales", "Maximum sale percent", 30, "The maximum sale percentage a moon can receive", new AcceptableValueRange<int>(1, 100));

            GetConfigValue("6 - Advanced Settings", "I have read this", "false", "This section contains advanced configuration options for various features of the mod. Incorrectly tweaking these might cause unexpected behaviour!\n" +
                "This setting has no effect.");
            GroupCreditsSavingBandAid= GetConfigValue("6 - Advanced Settings", "Group credits saving fix", true, "When LMU saves data it will also save the credits balance.\n" +
                "This prevents the 'free moon exploit'. This band aid should not cause any issues but I don't think I should need to do this in the first place..");
            AdvancedPrintMoonNames = GetConfigValue("6 - Advanced Settings", "Print moon names to console", false, "Print the names you need to define your custom groups to console/log. They will be logged after you've loaded into a save game. " +
                "You can also grab moons names from the LMU table that is periodically printed to logs even when this is not enabled.");
            AutoRerouteToCompany = GetConfigValue("6 - Advanced Settings", "Auto reroute to company", true, "When enabled automatically reroutes the ship to the company on deadline day.");
            
            const string cheapMoonBiasValueDescription =
                "Controls how strongly cheaper moons are favored when Cheap Moon Bias is enabled.\n" +
                "LMU compares each moon's price against the average price of the current candidate pool and turns that into a selection weight.\n" +
                "0.0 gives all candidates equal weight.\n" +
                "1.0 uses inverse-price weighting, so a 100 credit moon is 4x as likely as a 400 credit moon.\n" +
                "2.0 squares that effect, so the same 100 credit moon is 16x as likely as the 400 credit moon.\n" +
                "Values between 0.0 and 1.0 soften the bias. Values above 1.0 strengthen it.\n" +
                "The calculation uses original prices or current prices depending on the Ignore price changes setting.";

            CheapMoonBiasPaidRotation = GetConfigValue("6.1 - Cheap Moon Bias", "Discovery Mode paid rotation", true, "Use Cheap Moon Bias when selecting moons for the paid moon rotation when it's shuffled.");
            CheapMoonBiasPaidRotationValue = GetConfigValue("6.1 - Cheap Moon Bias", "Discovery Mode paid rotation bias value", 0.66f, cheapMoonBiasValueDescription, new AcceptableValueRange<float>(0.0f, 2.0f));
            CheapMoonBiasQuotaDiscovery = GetConfigValue("6.1 - Cheap Moon Bias", "Quota Discovery", true, "Use Cheap Moon Bias when selecting moons during Quota Discovery.");
            CheapMoonBiasQuotaDiscoveryValue = GetConfigValue("6.1 - Cheap Moon Bias", "Quota Discovery bias value", 0.66f, cheapMoonBiasValueDescription, new AcceptableValueRange<float>(0.0f, 2.0f));
            CheapMoonBiasTravelDiscovery = GetConfigValue("6.1 - Cheap Moon Bias", "Travel Discovery", true, "Use Cheap Moon Bias when selecting moons to discover during Travel Discovery.");
            CheapMoonBiasTravelDiscoveryValue = GetConfigValue("6.1 - Cheap Moon Bias", "Travel Discovery bias value", 0.66f, cheapMoonBiasValueDescription, new AcceptableValueRange<float>(0.0f, 2.0f));
            CheapMoonBiasNewDayDiscovery = GetConfigValue("6.1 - Cheap Moon Bias", "New Day Discovery", true, "Use Cheap Moon Bias when selecting moons during New Day Discovery.");
            CheapMoonBiasNewDayDiscoveryValue = GetConfigValue("6.1 - Cheap Moon Bias", "New Day Discovery bias value", 0.66f, cheapMoonBiasValueDescription, new AcceptableValueRange<float>(0.0f, 2.0f));
            CheapMoonBiasQuotaUnlock = GetConfigValue("6.1 - Cheap Moon Bias", "Quota Unlock", true, "Use Cheap Moon Bias when selecting moons during Quota Unlocks.");
            CheapMoonBiasQuotaUnlockValue = GetConfigValue("6.1 - Cheap Moon Bias", "Quota Unlock bias value", 0.66f, cheapMoonBiasValueDescription, new AcceptableValueRange<float>(0.0f, 2.0f));
            CheapMoonBiasQuotaDiscount = GetConfigValue("6.1 - Cheap Moon Bias", "Quota Discount", true, "Use Cheap Moon Bias when selecting moons during Quota Discounts.");
            CheapMoonBiasQuotaDiscountValue = GetConfigValue("6.1 - Cheap Moon Bias", "Quota Discount bias value", 0.66f, cheapMoonBiasValueDescription, new AcceptableValueRange<float>(0.0f, 2.0f));
            CheapMoonBiasQuotaFullDiscount = GetConfigValue("6.1 - Cheap Moon Bias", "Quota Full Discount", true, "Use Cheap Moon Bias when selecting moons during Quota Full Discounts.");
            CheapMoonBiasQuotaFullDiscountValue= GetConfigValue("6.1 - Cheap Moon Bias", "Quota Full Discount bias value", 0.66f, cheapMoonBiasValueDescription, new AcceptableValueRange<float>(0.0f, 2.0f));
            CheapMoonBiasIgnorePriceChanges = GetConfigValue("6.1 - Cheap Moon Bias", "Ignore price changes", true, "Ignore any changes to moon prices by discounts or sales and only consider original price for biased selections.");
            
            MoonGroupMatchingMethod = _configFile.Bind("6.2 - Moon Group Matching", "Group Matching Method", "Price",
                new ConfigDescription("The method used to group moons. Group Matching can be used to limit some discoveries to moons of the same group.\n" +
                "'Price': All moons of the same price are considered a group. This method ignores price changes by unlocks, discounts, or sales.\n" +
                "'PriceRange': All moons within a set price range are considered a group. Upper and lower range is defined by the price range setting below.\n" +
                "'PriceRangeUpper': All moons within a set upper price range are considered a group. Upper range is defined by the price range setting below.\n" +
                "'Tag': All moons that have at least one tag in common are considered a group.\n" +
                "'LethalConstellations': Match moons to their constellations as they are configured in LethalConstellations. See settings in Advanced section." +
                "'Custom': Define custom named groups of moons below.",
                new AcceptableValueList<string>(["Price", "PriceRange", "PriceRangeUpper", "Tag", "LethalConstellations", "Custom"]),
                Array.Empty<object>())).Value;
            MoonGroupMatchingPriceRange = GetConfigValue("6.2 - Moon Group Matching", "Price range", 200, "The price range used for matching moons via 'PriceRange' and 'PriceRangeUpper' methods.\n" +
                "It will match all moons priced within the original price +- this value (+ this value for upper range).");
            MoonGroupMatchingCustom = GetConfigValue("6.2 - Moon Group Matching", "Custom moon groups", "", "Define your own custom moon groups.\n" +
                "Expected Format: Separate moon groups by \"|\" and moons by \",\".\n" +
                "Example: 'Group name 1: Experimentation, Assurance, Vow | Group name 2: Offense, March, Adamance'\n" +
                "Names must be exact matches. The option below can be used to get the names.");

            TerminalTagLineWidth = GetConfigValue("6.3 - Terminal", "Maximum tag line length", 49, "By default LMU tries to fit as many tags as possible into a single line.\n" +
                "Decrease this value if you want to have a more organized look at the cost of more scrolling depending on the amount of tags you see.\n" +
                "NOTE: Don't worry about setting it too low. It will always put at least one tag per line. Only if any additional tag would exceed this value it puts a line break.\n" +
                "Do not set it larger than default unless you are also decreasing font size below.", new AcceptableValueRange<int>(10, 100));
            TerminalFontSizeOverride = GetConfigValue("6.3 - Terminal", "Override Terminal font size", true, "Override the font size in the Terminal's moon catalog.\n" +
                "Prevents inconsistencies with formatting. Disable to let LLL dynamically size the font depending on the number of moons visible\n" +
                "NOTE: With very few moons you might see some ugly line breaks with custom weathers with long names (Meteor Shower).");
            TerminalFontSize = GetConfigValue("6.3 - Terminal", "Terminal font size", 15f, "Customize the Terminal's moon catalog font size.\n" +
                "NOTE: When using smaller fonts you can increase the maximum tag line width above.", new AcceptableValueRange<float>(8f, 15f));
            TerminalScrollAmount = GetConfigValue("6.3 - Terminal", "Terminal scroll amount", 0, "Override the Terminal's moon catalog scroll amount. 1 is close to vanilla scroll amount but normalized to account for more text/moons. Increase to make scrolling smoother or rather steps smaller. 0 to disable\n" +
                "NOTE: This can help when you have so many moons that some are skipped when scrolling.", new AcceptableValueRange<int>(0, 20));
            TerminalShowRiskWeather = GetConfigValue("6.3 - Terminal", "Terminal show weather in risk preview", false, "Also show the weather when using `preview difficulty`");

            AlertMessageQueueing = GetConfigValue("6.4 - Compatibility", "Avoid alert messages overlapping", true, "When enabled, LethalMoonUnlocks will intercept all alert messages (yellow/red pop-up) and add them to a queue. This avoids alert messages from other mods and Vanilla from overlapping or not showing at all. Disable if you experience issues.");
            PreferLQRisk = GetConfigValue("6.4 - Compatibility", "Prefer LethalQuantities risk level", false, "Show the moon risk levels set by LethalQuantities in the moon catalog instead of the default risk levels.");
            MalfunctionsNavigation = GetConfigValue("6.4 - Compatibility", "Malfunctions navigation buys moon", false, "When the Malfunctions navigation malfunction is triggered LMU will interpret it as if the moon routed to was bought.");
            LethalConstellationsOverridePrice = GetConfigValue("6.4 - Compatibility", "LethalConstellations override price", false, "When enabled and LethalConstellations is present override the constellation routing price with the default moon's routing price.\n" + "Routing to the constellation will be considered buying the default moon. Consequently unlocks, discounts and sales of the default moon will be granted and will also apply to the constellation routing price.\n" +
                "NOTE: In Discovery Mode the default moon will always be set to the cheapest currently discovered moon of that constellation regardless of this setting.");
            PreferGaletry = GetConfigValue("6.4 - Compatibility", "Prefer Galetry over Gordion", true, "When enabled and Galetry (from Wesley's moons journey) is available and routable, LMU will auto reroute the ship to Galetry instead of Gordion (the company).");

            OverrideHidden = GetConfigValue("6.5 - Overrides", "Override moons hidden by default", false, "Enable to hard override any hidden by default information using the list below. Any other information will be ignored. This includes moons hidden in vanilla, via LLL config, etc.");
            OverrideHiddenList = GetConfigValue("6.5 - Overrides", "Override hidden list", "", "List of moons LMU will consider to be hidden by default.\n" +
                "For example, 'Vow, March, Artifice'. Those three will be the only moons hidden by default. You can still unhide them in various ways. Note that setting this would make Embrion not hidden.\n" +
                "Moon names must be separated by commas and must be exact matches. You can print the moon names to console/log by using the option in 'Advanced Settings'.");

            OverrideLocked = GetConfigValue("6.5 - Overrides", "Override moons locked by default", false, "Enable to hard override any locked by default information using the list below. Any other information will be ignored. This includes moons locked in vanilla, via LLL config, etc.");
            OverrideLockedList = GetConfigValue("6.5 - Overrides", "Override locked list", "", "List of moons LMU will consider to be locked by default.\n" +
                "For example, 'Vow, March, Artifice'. Those three will be the only moons locked by default.\n" +
                "Moon names must be separated by commas and must be exact matches. You can print the moon names to console/log by using the option in 'Advanced Settings'.");

            EnableStoryProgression = GetConfigValue("6.6 - Story Progression", "Enable Story Progression", true, "Story progression allows locking moons behind various conditions. This can be employed by other mods like Wesley's moons (JLL).\nDisabling this settings will globally ignore any requests to lock moons behind story progressions inlcuding LMU's own Vanilla Story progression.");
            LMUStoryProgression = GetConfigValue("6.6 - Story Progression", "Vanilla Story Progression", false, "Enable to lock the two hidden vanilla moons behind story progression. To release the lock for Artifice you have to land three times on Adamance, for Embrion you have to scan an old bird. After completing these tasks the moons will be available (for discovery). They will not be hidden.");
            GaletryStoryLock = GetConfigValue("6.6 - Story Progression", "Restrict access to Galetry", false, "When enabled and Wesley's moons is installed Galetry is not available from the start. To gain access you will need to sell a specified number of paintings to the company.");
            GaletryStoryLockPaintingsAmount = GetConfigValue("6.6 - Story Progression", "Galetry number of paintings", 3, "The number of sold paintings required to gain access to Galetry.");


        }
        private static T GetConfigValue<T>(string section, string key, T defaultValue, string description) {
            return _configFile.Bind(section, key, defaultValue, description).Value;
        }

        private static T GetConfigValue<T>(string section, string key, T defaultValue, string description, AcceptableValueRange<int> range) {
            return _configFile.Bind(section, key, defaultValue, new ConfigDescription(description, range)).Value;
        }

        private static T GetConfigValue<T>(string section, string key, T defaultValue, string description, AcceptableValueRange<float> range) {
            return _configFile.Bind(section, key, defaultValue, new ConfigDescription(description, range)).Value;
        }

        private static void MigrateLegacyConfig(string legacyConfigPath, ConfigFile cfg) {
            File.Copy(legacyConfigPath, Path.Combine(Paths.ConfigPath, PluginMetadata.PLUGIN_GUID + ".cfg"), true);
            _configFile = cfg;
            _configFile.Reload();
            RefreshValues();
            Logger.LogInfo("Legacy configuration migrated. Renaming legacy config file..");
            // Keep a backup around
            File.Copy(legacyConfigPath, Path.Combine(Paths.ConfigPath, PluginMetadata.PLUGIN_GUID + ".cfg.legacy"), true);
            File.Delete(legacyConfigPath);
        }
    }
}
