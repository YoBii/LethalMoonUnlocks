using BepInEx.Configuration;
using System.IO;
using BepInEx;

namespace LethalMoonUnlocks
{
    public class ConfigManager
    {
        private static string _configPath = Path.Combine(Paths.ConfigPath, "LethalMoonUnlocks.cfg");
        private static ConfigFile _configFile;

        private static T GetConfigValue<T>(string section, string key, T defaultValue, string description = null) {
            return _configFile.Bind(section, key, defaultValue, description).Value;
        }

        private static void RefreshValues()
        {
            Plugin.ResetWhenFired = GetConfigValue("1 - General settings", "Reset when fired", true, "When disabled you will keep your unlocked moons/discounts when being fired. Everything will persist unless you create a new save.");
            Plugin.QuotaUnlocks = GetConfigValue("2 - Additional settings", "Unlock on new Quota", false, "Will unlock a random moon eaech time you complete a quota.");
            Plugin.DiscountMode = GetConfigValue("2 - Additional settings", "Discount mode", false, "Will unlock a random moon eaech time you complete a quota.");
            Plugin.QuotaUnlockMaxCount = GetConfigValue("2.1 - Unlock on new Quota", "Number of unlocks", 0, "Only unlock a random moon on new quota this number of times. 0 for no limit.");
            Plugin.QuotaUnlocksMaxPrice = GetConfigValue("2.1 - Unlock on new Quota", "Max unlock price", 0, "Only unlock a random moon on new quota up to this moon price. 0 for no limit.");
            Plugin.Discounts = GetConfigValue("2.2 - Discount mode settings", "Discount rates", "50,75,100", "Define the discount rates that are applied to moon prices as % off of the original price." +
                "\nFor example the default string '50,75,100' would make each moon 50% off after the first purchase, 75% off after the second purchase and free after the third purchase.");
            
        }

        private static void EnsureConfigExists() {
            if (_configFile == null) {
                _configFile = new ConfigFile(_configPath, true);
            }
        }
        public static void RefreshConfig() {
            Plugin.Instance.Mls.LogInfo("Refreshing config..");
            _configFile = null;
            EnsureConfigExists();
            RefreshValues();
        }
    }
}