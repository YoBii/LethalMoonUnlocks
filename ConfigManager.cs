using BepInEx.Configuration;
using System.IO;
using BepInEx;
using System.Linq;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using LethalLevelLoader;
using DunGen;
using UnityEngine.Networking;

namespace LethalMoonUnlocks {
    public class ConfigManager {
        public static ConfigManager Instance { get; private set; }

        private static string _configPath = Path.Combine(Paths.ConfigPath, "LethalMoonUnlocks.cfg");
        private static ConfigFile _configFile;

        public static bool ResetWhenFired { get; private set; }
        public static bool DisplayTerminalTags { get; private set; }
        public static bool ShowTagInOrbit { get; private set; }
        public static bool ShowTagNewDiscovery { get; private set; }
        public static bool ShowTagExplored { get; private set; }
        public static bool ShowTagUnlockDiscount { get; private set; }
        public static bool ShowTagPermanentDiscovery { get; private set; }
        public static bool ShowTagSale { get; private set; }
        public static bool ShowTagGroups { get; private set; }
        public static bool ShowAlerts { get; private set; }
        public static bool ChatMessages { get; private set; }
        public static bool UnlockMode { get; private set; }
        public static int UnlocksResetAfterVisits { get; private set; }
        public static bool UnlocksResetAfterVisitsPermDiscovery { get; private set; }
        public static bool QuotaUnlocks { get; private set; }
        public static int QuotaUnlockChance { get; private set; }
        public static int QuotaUnlockCount { 
            get {
                return UnityEngine.Random.Range(_quotaUnlockCountMin, _quotaUnlockCountMax + 1);
            } 
        }
        private static int _quotaUnlockCountMin;
        private static int _quotaUnlockCountMax;
        public static int QuotaUnlockMaxCount { get; private set; }
        public static int QuotaUnlockMaxPrice { get; private set; }
        public static bool DiscountMode { get; private set; }
        private static string DiscountsString { get; set; }
        public static List<int> Discounts {
            get {
                string[] discounts = DiscountsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                List<int> list = [];
                foreach (string discount in discounts) list.Add(int.Parse(discount));
                return list;
            }
        }
        public static int DiscountsCount {
            get {
                return Discounts.Count();
            }
        }
        public static int DiscountsResetAfterVisits { get; private set; }
        public static bool DiscountsResetAfterVisitsPermDiscovery { get; private set; }
        public static bool QuotaDiscounts {  get; private set; }
        public static int QuotaDiscountChance { get; private set; }
        public static int QuotaDiscountCount {
            get {
                return UnityEngine.Random.Range(_quotaDiscountCountMin, _quotaDiscountCountMax + 1);
            }
        }
        private static int _quotaDiscountCountMin;
        private static int _quotaDiscountCountMax;
        public static int QuotaDiscountMaxCount { get; private set; }
        public static bool QuotaFullDiscounts { get; private set; }
        public static int QuotaFullDiscountChance { get; private set; }
        public static int QuotaFullDiscountCount {
            get {
                return UnityEngine.Random.Range(_quotaFullDiscountCountMin, _quotaFullDiscountCountMax + 1);
            }
        }
        private static int _quotaFullDiscountCountMin;
        private static int _quotaFullDiscountCountMax;
        public static int QuotaFullDiscountMaxCount { get; private set; }
        public static int QuotaFullDiscountMaxPrice { get; private set; }
        public static bool DiscoveryMode { get; private set; }
        private static string DiscoveryWhitelist { get; set; }
        public static List<string> DiscoveryWhitelistMoons {
            get {
                return DiscoveryWhitelist.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
        public static bool DiscoveryKeepUnlocks { get; private set; }
        public static bool DiscoveryKeepDiscounts { get; private set; }
        public static int DiscoveryFreeCountBase { get; private set; }
        public static int DiscoveryFreeCountIncreaseBy { get; private set; }
        public static int DiscoveryDynamicFreeCountBase { get; private set; }
        public static int DiscoveryDynamicFreeCountIncreaseBy { get; private set; }
        public static int DiscoveryPaidCountBase { get; private set; }
        public static int DiscoveryPaidCountIncreaseBy { get; private set; }
        public static int PermanentlyDiscoverFreeMoonsOnLanding { get; private set; }
        public static int PermanentlyDiscoverPaidMoonsOnLanding { get; private set; }
        public static bool PermanentlyDiscoverHiddenMoonsOnVisit { get; private set; }
        public static bool DiscoveryShuffleEveryDay { get; private set; }
        public static bool DiscoveryNeverShuffle { get; private set; }
        public static bool QuotaDiscoveries { get; private set; }
        public static int QuotaDiscoveryChance { get; private set; }
        public static int QuotaDiscoveryCount {
            get {
                return UnityEngine.Random.Range(_quotaDiscoveryCountMin, _quotaDiscoveryCountMax + 1);
            }
        }
        private static int _quotaDiscoveryCountMin;
        private static int _quotaDiscoveryCountMax;
        public static bool QuotaDiscoveryPermanent { get; private set; }
        public static bool TravelDiscoveries { get; private set; }
        public static int TravelDiscoveryChance { get; private set; }
        public static int TravelDiscoveryCount {
            get {
                return UnityEngine.Random.Range(_travelDiscoveryCountMin, _travelDiscoveryCountMax + 1);
            }
        }
        private static int _travelDiscoveryCountMin;
        private static int _travelDiscoveryCountMax;
        public static bool TravelDiscoveryPermanent { get; private set; }
        public static bool TravelDiscoveryMatchGroup { get; private set; }
        public static bool NewDayDiscoveries { get; private set; }
        public static int NewDayDiscoveryChance { get; private set; }
        public static int NewDayDiscoveryCount {
            get {
                return UnityEngine.Random.Range(_newDayDiscoveryCountMin, _newDayDiscoveryCountMax + 1);
            }
        }
        private static int _newDayDiscoveryCountMin;
        private static int _newDayDiscoveryCountMax;
        public static bool NewDayDiscoveryPermanent { get; private set; }
        public static bool NewDayDiscoveryMatchGroup { get; private set; }
        public static bool Sales { get; private set; }
        public static int SalesChance { get; private set; }
        public static bool SalesShuffleDaily { get; private set; }
        public static int SalesRate {
            get { return UnityEngine.Random.Range(_salesRateMin, _salesRateMax); }
        }
        private static int _salesRateMin;
        private static int _salesRateMax;
        public static float CheapMoonBias {
            get {
                return Math.Clamp(_cheapMoonBiasValue, 0.0f, 1.0f);
            }
            set {
                _cheapMoonBiasValue = value;
            }
        }
        private static float _cheapMoonBiasValue;
        public static bool CheapMoonBiasPaidRotation { get; private set; }
        public static bool CheapMoonBiasQuotaDiscovery { get; private set; }
        public static bool CheapMoonBiasNewDayDiscovery { get; private set; }
        public static bool CheapMoonBiasTravelDiscovery { get; private set; }
        public static string MoonGroupMatchingMethod { get; private set; }
        public static int MoonGroupMatchingPriceRange { get; private set; }
        private static string MoonGroupMatchingCustom {  get; set; }
        public static Dictionary<string, List<string>> MoonGroupMatchingCustomDict {  get; private set; }
        private static bool MoonGroupMatchingCustomHelper { get; set; }
        public static int TerminalTagLineWidth { get; set; }
        public static bool PreferLQRisk {  get; private set; }
        public static bool MalfunctionsNavigation {  get; private set; }
        public static bool AlertMessageQueueing {  get; private set; }


        public ConfigManager() {
            if (Instance == null)
                Instance = this;
            Plugin.Instance.Mls.LogInfo($"ConfigManager created");
            RefreshConfig();
        }

        public static void RefreshConfig() {
            Plugin.Instance.Mls.LogInfo("Refreshing config..");
            _configFile = null;
            EnsureConfigExists();
            RefreshValues();
            
        }
        public static Dictionary<string, List<string>> ParseCustomMoonGroups() {
            if (MoonGroupMatchingCustomHelper) {
                Plugin.Instance.Mls.LogWarning($"Printing available moon names for custom moon groups..");
                Plugin.Instance.Mls.LogWarning($"{string.Join(", ", PatchedContent.ExtendedLevels.Where(level => level.NumberlessPlanetName != "Gordion" && level.NumberlessPlanetName != "Liquidation").Select(level => level.name))}");
            }
            Dictionary<string, List<string>> customGroups = new Dictionary<string, List<string>>();
            if (MoonGroupMatchingCustom == string.Empty) {
                Plugin.Instance.Mls.LogInfo($"No custom moon group defined. Skip parsing..");
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
                    Plugin.Instance.Mls.LogWarning("Couldn't parse custom moon group name. Make sure you're using the correct format!");
                    continue;
                } else if (groupMemberString == string.Empty || members.Count == 0) {
                    Plugin.Instance.Mls.LogWarning("Couldn't parse custom moon group! Name null or empty or members not found!");
                    continue;
                } else {
                    Plugin.Instance.Mls.LogInfo($"Parsed custom moon group: Name = {groupName}, Members = [ {string.Join(", ", members)} ]");
                    customGroups[groupName] = members;
                }
            }
            return customGroups;
        }
        private static void RefreshValues() {
            // TO DO
            // - Add min max for Count (e.g. QuotaUnlockCount)

            ResetWhenFired = GetConfigValue("1 - General settings", "Reset when fired", true, "Reset your progress when being fired. Unlocks, Discounts, and permanently discovered moons will all be wiped.\n" +
                "Unlocks, Discounts, Permanently Discovered moons, ..  all of it will persist unless you create a new save.\n" +
                "The only exception to this option is the base selection of moons in Discovery Mode.");
            ChatMessages = GetConfigValue("1 - General settings", "Show chat messages", true, "When enabled, LethalMoonUnlocks will send messages to the in-game chat whenever something relevant happens.");
            ShowAlerts = GetConfigValue("1 - General settings", "Show alert messages", false, "When enabled, LethalMoonUnlocks will display alert messages whenever something relevant happens.");

            DisplayTerminalTags = GetConfigValue("1.1 - Terminal moon tags", "Display tags in terminal", false, "When enabled, LethalMoonUnlocks will display additional tags in the Terminal moon catalogue.\n" +
                "These tags will indicate various conditions, such as a moon being unlocked or discounted, being on sale, etc.\n" +
                "If custom moon groups or matching by LLL tag are enabled, you'll also see the custom groups or LLL tags a moon is associated with.\n" +
                "NOTE: At this time additional tags will only show in the standard LLL moon catalogue and TerminalFormatter. Any other mod replacing the 'moons' command will probably cause issues.");
            ShowTagInOrbit = GetConfigValue("1.1 - Terminal moon tags", "In orbit tag", true, "Display a tag to indicate the moon you're currently orbiting.");
            ShowTagExplored = GetConfigValue("1.1 - Terminal moon tags", "Exploration tag", true, "Display a tag to indicate which moons have not been landed on yet. After landing once, it will keep track of how many times you've landed in total.");
            ShowTagUnlockDiscount = GetConfigValue("1.1 - Terminal moon tags", "Unlock discount tag", true, "Display a tag to indicate Unlocks and Discounts as well as how many routes are left before they expire.");
            ShowTagNewDiscovery = GetConfigValue("1.1 - Terminal moon tags", "New discovery tag", true, "Discovery Mode only: display a tag to indicate which moons are new discoveries i.e. available in the moon catalogue for the first time. The tag will vanish when you route to the moon or the moon catalogue is shuffled.");
            ShowTagPermanentDiscovery = GetConfigValue("1.1 - Terminal moon tags", "Permanent discovery tag", true, "Discovery Mode only: display a tag to indicate permanently discovered moons.\n" +
                "Displays as [PINNED].");
            ShowTagSale = GetConfigValue("1.1 - Terminal moon tags", "Sales tag", true, "Moon Sales only: display a tag to indicate which moons are on sale, as well as the percentage of the sale.");
            ShowTagGroups = GetConfigValue("1.1 - Terminal moon tags", "Group tag", true, "Moon Group Matching only: display a tag to indicate groups a moon belongs to. Limited to custom group and LLL tag matching methods.");

            UnlockMode = GetConfigValue("2 - Unlock Mode (Default)", "Enable Unlock Mode", true, "Unlock Mode is the default mode, akin to the original Permanent Moons mod. In Unlock Mode, when you buy a paid moon, it will be 'unlocked'.\n" +
                "Once unlocked, moons are completely free, and by default, will stay free permanently.\n" +
                "NOTE: This setting and all settings relating to Unlocks will have no effect if Discount Mode is enabled!");
            UnlocksResetAfterVisits = GetConfigValue("2 - Unlock Mode (Default)", "Unlocks expire", 0, "Unlocks will expire after a set number of free routes, after which they will become paid again.\n" +
                "Set to 0 to disable this feature.");
            DiscoveryKeepUnlocks = GetConfigValue("2 - Unlock Mode (Default)", "Unlocked moons are permanently discovered", false, "Discovery Mode only: Every unlocked moon is also permanently discovered i.e. added to the moon catalogue on top of your base selection.");
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
            Plugin.Instance.Mls.LogInfo($"Discount rates (% off): {string.Join(", ", Discounts.Select(discount => discount + "%"))}");
            DiscountsResetAfterVisits = GetConfigValue("3 - Discount Mode", "Discounts expire", 0, "Discounts will expire after a set number of free routes, after which they will return to their original price.\n" +
                "Set to 0 to disable this feature." +
                "NOTE: The final discount rate must be set to '100' to use this setting!");
            DiscoveryKeepDiscounts = GetConfigValue("3 - Discount Mode", "Discounted moons are permanently discovered", false, "Discovery Mode only: Every discounted moon is also permanently discovered i.e. added to the moon catalogue on top of your base selection.");
            DiscountsResetAfterVisitsPermDiscovery = GetConfigValue("3 - Discount Mode", "Reset permanent discoveries on discount expiry", false, "Discovery Mode only: Reset a moon's permanent discovery status when its discount expires.\n" +
                "This is the only way permanent discoveries can vanish during a run in Discount Mode.\n");

            QuotaDiscounts = GetConfigValue("3.1 - Quota Discounts", "Enable Quota Discounts", false, "Quota Discounts are rewarded for meeting the quota. When triggered Quota Discounts will grant you one or more discounts for free.\n" +
                "The moons that are discounted are randomly selected.");
            QuotaDiscountChance = GetConfigValue("3.1 - Quota Discounts", "Quota Discount trigger chance", 100, "The chance to trigger a Quota Discount every time you meet the quota.\n", new AcceptableValueRange<int>(0, 100));
            _quotaDiscountCountMin = GetConfigValue("3.1 - Quota Discounts", "Minimum discounted moon count", 1, "The minimum number of moons that will receive a discount each time a Quota Discount is triggered.", new AcceptableValueRange<int>(1, 10));
            _quotaDiscountCountMax = GetConfigValue("3.1 - Quota Discounts", "Maximum discounted moon count", 1, "The maximum number of moons that will receive a discount each time a Quota Discount is triggered.", new AcceptableValueRange<int>(1, 10));
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

            DiscoveryMode = GetConfigValue("4 - Discovery Mode", "Enable Discovery Mode", false, "In Discovery Mode, you start with a limited selection of moons in the Terminal's moon catalogue.\n" +
                "By default, this base selection of moons will be shuffled after every quota, and can also be configured to expand over time.\n" +
                "There are also various options to discover additional moons as you play.\n" +
                "Permanently discovered moons are added to the moon catalogue on top of the base selection, and are not lost on shuffle.\n" +
                "Use the configuration options below to customize your experience!");
            
            DiscoveryNeverShuffle = GetConfigValue("4 - Discovery Mode", "Never shuffle", false, "Never shuffle the rotation of moons available in the moon catalogue.\n" +
                "New moons must be discovered through other means, but once discovered, they won't vanish, since the selection is never shuffled.\n" +
                "NOTE: Overrides the 'Shuffle every day' option.\n");
            DiscoveryShuffleEveryDay = GetConfigValue("4 - Discovery Mode", "Shuffle every day", false, "Shuffle the rotation of moons available in the moon catalogue every day, instead of after every quota.");
            
            DiscoveryWhitelist = GetConfigValue("4 - Discovery Mode", "Whitelist", "", "List of moons to keep discovered at all times.\n" +
                "For example, 'Whitelist = Experimentation, Assurance, Vow' would make these three moons start out as permanently discovered on every run.\n" +
                "Moon names must be separated by commas and must be exact matches. You can print the moon names to console/log by using the option in 'Advanced Settings'.");

            DiscoveryFreeCountBase = GetConfigValue("4 - Discovery Mode", "Free moons base count", 1, "The base amount of randomly selected free moons available in the moon catalogue.\n" +
                "NOTE: 'Free' only considers moons that are free by default, or configured to be free. Moons that are free due to unlocks or discounts are excluded!");
            DiscoveryDynamicFreeCountBase = GetConfigValue("4 - Discovery Mode", "Dynamic free moons base count", 2, "The base amount of randomly selected dynamic free moons available in the moon catalogue.\n" +
                "NOTE: 'Dynamic free' considers moons that are free due to unlocks or discounts in addition to those that are free by default, or configured to be free.");
            DiscoveryPaidCountBase = GetConfigValue("4 - Discovery Mode", "Paid moons base count", 3, "The base amount of randomly selected paid moons available in the moon catalogue.\n" +
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

            TravelDiscoveries = GetConfigValue("4.2 - Travel Discoveries", "Enable Travel Discoveries", false, "Travel Discoveries grant additional moon discoveries when routing to a paid moon\n" +
                "The moons that are discovered are randomly selected.");
            TravelDiscoveryChance  = GetConfigValue("4.2 - Travel Discoveries", "Travel Discovery trigger chance", 20, "The chance to trigger a Travel Discovery every time you route to a paid moon.", new AcceptableValueRange<int>(0, 100));
            _travelDiscoveryCountMin  = GetConfigValue("4.2 - Travel Discoveries", "Minimum travel discovery moon count", 1, "The minimum number of moons that will be discovered each time a Travel Discovery is triggered.", new AcceptableValueRange<int>(1, 10));
            _travelDiscoveryCountMax  = GetConfigValue("4.2 - Travel Discoveries", "Maximum travel discovery moon count", 1, "The maximum number of moons that will be discovered each time a Travel Discovery is triggered.", new AcceptableValueRange<int>(1, 10));
            TravelDiscoveryPermanent = GetConfigValue("4.2 - Travel Discoveries", "Travel Discoveries are permanent", false, "Moons discovered through Travel Discoveries will stay permanently discovered i.e. they won't vanish on shuffle.");
            TravelDiscoveryMatchGroup = GetConfigValue("4.2 - Travel Discoveries", "Travel Discovery group matching", false, "Only consider moons of the same group you're routing to for Travel Discoveries.");

            NewDayDiscoveries = GetConfigValue("4.3 - New Day Discoveries", "Enable New Day Discoveries", false, "New Day Discoveries grant additional moon discoveries at the start of a new day.\n" +
                "The moons that are discovered are randomly selected.");
            NewDayDiscoveryChance  = GetConfigValue("4.3 - New Day Discoveries", "New Day Discovery trigger chance", 20, "The chance to trigger a New Day Discovery at the start of a new day.\n" +
                "Make it a random occurence or guaranteed.", new AcceptableValueRange<int>(0, 100));
            _newDayDiscoveryCountMin  = GetConfigValue("4.3 - New Day Discoveries", "Minimum new day discovery moon count", 1, "The minimum number of moons to be discovered each time a New Day Discovery is granted.", new AcceptableValueRange<int>(1, 10));
            _newDayDiscoveryCountMax  = GetConfigValue("4.3 - New Day Discoveries", "Maximum new day discovery moon count", 1, "The maximum number of moons to be discovered each time a New Day Discovery is granted.", new AcceptableValueRange<int>(1, 10));
            NewDayDiscoveryPermanent = GetConfigValue("4.3 - New Day Discoveries", "New Day Discoveries are permanent", false, "Moons discovered through New Day Discoveries will stay permanently discovered i.e. they won't vanish on shuffle.");
            NewDayDiscoveryMatchGroup = GetConfigValue("4.3 - New Day Discoveries", "New Day Discovery group matching", false, "Only consider moons of the same group as the moon you're currently orbiting for New Day Discoveries.");

            Sales = GetConfigValue("5 - Moon Sales", "Moon Sales", false, "Each moon has a chance to go on sale for a reduced routing price.\n" +
                "By default, Moon Sales are shuffled after every quota. Only non-free moons can go on sale.\n" +
                "NOTE: These sales are separate from discounts received via Discount Mode.");
            SalesShuffleDaily = GetConfigValue("5 - Moon Sales", "Shuffle sales daily", false, "Shuffle moon sales daily, instead of after every quota");
            SalesChance = GetConfigValue("5 - Moon Sales", "Moon Sale chance", 20, "The chance for each moon to go on sale every time sales are shuffled.", new AcceptableValueRange<int>(0, 100));
            _salesRateMin = GetConfigValue("5 - Moon Sales", "Minimum sale percent", 5, "The minimum sale percentage a moon can receive.", new AcceptableValueRange<int>(0, 100));
            _salesRateMax = GetConfigValue("5 - Moon Sales", "Maximum sale percent", 30, "The maximum sale percentage a moon can receive", new AcceptableValueRange<int>(1, 100));

            GetConfigValue("6 - Advanced settings", "I have read this", "false", "This section contains advanced configuration options for various features of the mod. Incorrectly tweaking these might cause unexpected behaviour!\n" +
                "This setting has no effect.");

            CheapMoonBias = GetConfigValue("6.1 - Discovery Mode: Cheap Moon Bias", "Cheap Moon Bias", 0.66f, "With Cheap Moon Bias, cheap moons are more likely to be unlocked than expensive moons.\n" +
                "Adjust the value below to modify the bias. 0.0 = no bias, all moons are equally considered. 1.0 = heavily prefer cheaper moons.", new AcceptableValueRange<float>(0.0f, 1.0f));
            CheapMoonBiasPaidRotation = GetConfigValue("6.1 - Discovery Mode: Cheap Moon Bias", "Enable for paid moon rotation", true, "Use Cheap Moon Bias when selecting moons for the paid moon rotation when it's shuffled.");
            CheapMoonBiasQuotaDiscovery = GetConfigValue("6.1 - Discovery Mode: Cheap Moon Bias", "Enable for Quota Discovery", true, "Use Cheap Moon Bias when selecting moons during Quota Discovery.");
            CheapMoonBiasNewDayDiscovery = GetConfigValue("6.1 - Discovery Mode: Cheap Moon Bias", "Enable for New Day Discovery", true, "Use Cheap Moon Bias when selecting moons during New Day Discovery.");
            CheapMoonBiasTravelDiscovery = GetConfigValue("6.1 - Discovery Mode: Cheap Moon Bias", "Enable for Travel Discovery", true, "Use Cheap Moon Bias when selecting moons to discover during Travel Discovery.");
            
            MoonGroupMatchingMethod = _configFile.Bind("6.2 - Moon Group Matching", "Group Matching Method", "Price",
                new ConfigDescription("The method used to group moons. Group Matching can be used to limit some discoveries to moons of the same group.\n" +
                "'Price': All moons of the same price are considered a group. This method ignores price changes by unlocks, discounts, or sales.\n" +
                "'PriceRange': All moons within a set price range are considered a group. Upper and lower range is defined by the price range setting below.\n" +
                "'PriceRangeUpper': All moons within a set upper price range are considered a group. Upper range is defined by the price range setting below.\n" +
                "'Tag': All moons that have at least one tag in common are considered a group.\n" +
                "'Custom': Define custom named groups of moons below.",
                new AcceptableValueList<string>(["Price", "PriceRange", "PriceRangeUpper", "Tag", "Custom"]),
                Array.Empty<object>())).Value;
            MoonGroupMatchingPriceRange = GetConfigValue("6.2 - Moon Group Matching", "Price range", 200, "The price range used for matching moons via 'PriceRange' and 'PriceRangeUpper' methods.\n" +
                "It will match all moons priced within the original price +- this value (+ this value for upper range).");
            MoonGroupMatchingCustom = GetConfigValue("6.2 - Moon Group Matching", "Custom moon groups", "", "Define your own custom moon groups.\n" +
                "Expected Format: Separate moon groups by \"|\" and moons by \",\".\n" +
                "Example: 'Group name 1: Experimentation, Assurance, Vow | Group name 2: Offense, March, Adamance'\n" +
                "Names must be exact matches. The option below can be used to get the names.");
            MoonGroupMatchingCustomDict = ParseCustomMoonGroups();
            MoonGroupMatchingCustomHelper = GetConfigValue("6.2 - Moon Group Matching", "Print moon names to console", false, "Print the names you need to define your custom groups to console/log. They will be logged after you've loaded into a save game.");

            TerminalTagLineWidth = GetConfigValue("6.3 - Terminal", "Maximum tag line length", 49, "By default LMU tries to fit as many tags as possible into a single line.\n" +
                "Decrease this value if you want to have a more organized look at the cost of more scrolling depending on the amount of tags you see.\n" +
                "NOTE: Don't worry about setting it too low. It will always put at least one tag per line. Only if any additional tag would exceed this value it puts a line break.", new AcceptableValueRange<int>(0, 49));

            AlertMessageQueueing = GetConfigValue("6.4 - Compatibility", "Avoid alert messages overlapping", true, "When enabled, LethalMoonUnlocks will intercept all alert messages (yellow/red pop-up) and add them to a queue. This avoids alert messages from other mods and Vanilla from overlapping or not showing at all. Disable if you experience issues.");
            PreferLQRisk = GetConfigValue("6.4 - Compatibility", "Prefer LethalQuantities risk level", false, "Show the moon risk levels set by LethalQuantities in the moon catalogue instead of the default risk levels.");
            MalfunctionsNavigation = GetConfigValue("6.4 - Compatibility", "Malfunctions navigation buys moon", false, "When the Malfunctions navigation malfunction is triggered LMU will interpret it as if the moon routed to was bought.");
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

        private static void EnsureConfigExists() {
            if (_configFile == null) {
                _configFile = new ConfigFile(_configPath, true);
            }
        }
    }
}
