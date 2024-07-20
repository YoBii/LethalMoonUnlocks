using LethalLevelLoader;
using System.Collections.Generic;

namespace LethalMoonUnlocks {
    internal static class SaveManager {
        public static Dictionary<string, object> Load() {
            Dictionary<string, object> savedata = new Dictionary<string, object>();
            if (!ES3.KeyExists("LMU_UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName) &&
                !ES3.KeyExists("LMU_OriginalMoonPrices", GameNetworkManager.Instance.currentSaveFileName) &&
                !ES3.KeyExists("LMU_QuotaCount", GameNetworkManager.Instance.currentSaveFileName)) {
                Plugin.Instance.Mls.LogInfo($"No LMU save data found. Checking for Permanent Moons save data to import..");
                savedata = ImportPermanentMoonsData();
                if (savedata.Count > 0) return savedata;
            } else {
                Plugin.Instance.Mls.LogInfo($"Save data found!");
                savedata = LoadSaveData();
                if (savedata.Count > 0) return savedata;
            }
            Plugin.Instance.Mls.LogInfo("No LMU or PM data to load. New save.");
            return savedata;
        }

        public static Dictionary<string, object> ImportPermanentMoonsData() {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            List<ExtendedLevel> extendedLevels = PatchedContent.ExtendedLevels;
            if (ES3.KeyExists("UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName)) {
                List<string> PMunlockedMoons = ES3.Load<List<string>>("UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName);
                Dictionary<string, int> unlockedMoons = new Dictionary<string, int>();
                foreach (var moon in PMunlockedMoons) {
                    foreach (var level in extendedLevels) {
                        if (moon.Contains(level.NumberlessPlanetName, System.StringComparison.OrdinalIgnoreCase)) {
                            unlockedMoons.TryAdd(level.NumberlessPlanetName, 1);
                        }
                    }
                }
                if (unlockedMoons.Count > 0) {
                    dictionary.Add("LMU_UnlockedMoons", unlockedMoons);
                    Plugin.Instance.Mls.LogInfo($"Imported UnlockedMoons from Permanent Moons: {string.Join(", ", unlockedMoons)}");
                } else {
                    Plugin.Instance.Mls.LogInfo($"No Permanent moons data found.");
                    return dictionary;
                }
            } else {
                Plugin.Instance.Mls.LogInfo($"No Permanent moons data found.");
                return dictionary;
            }
            if (ES3.KeyExists("OriginalMoonPrices", GameNetworkManager.Instance.currentSaveFileName)) {
                Dictionary<string, int> PMoriginalPrices = ES3.Load<Dictionary<string, int>>("OriginalMoonPrices", GameNetworkManager.Instance.currentSaveFileName);
                Dictionary<string, int> originalPrices = new Dictionary<string, int>();
                foreach (var price in PMoriginalPrices) {
                    foreach (var level in extendedLevels) {
                        if (price.Key.Contains(level.NumberlessPlanetName, System.StringComparison.OrdinalIgnoreCase)) {
                            originalPrices.TryAdd(level.NumberlessPlanetName, price.Value);
                        }
                    }
                }
                if (originalPrices.Count > 0) {
                    dictionary.Add("LMU_OriginalMoonPrices", originalPrices);
                    Plugin.Instance.Mls.LogInfo($"Imported OriginalMoonPrices from Permanent Moons: {string.Join(", ", originalPrices)}");
                }
            }
            if (ES3.KeyExists("MoonQuotaNum", GameNetworkManager.Instance.currentSaveFileName)) {
                int quotaCount = ES3.Load<int>("MoonQuotaNum", GameNetworkManager.Instance.currentSaveFileName);
                dictionary.Add("LMU_QuotaCount", quotaCount);
                Plugin.Instance.Mls.LogInfo($"Imported MoonQuotaNum from Permanent Moons: {quotaCount}");
            }
            return dictionary;
        }

        private static Dictionary<string, object> LoadSaveData() {
            Plugin.Instance.Mls.LogInfo($"Loading save data..");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            if (ES3.KeyExists("LMU_UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName)) {
                Dictionary<string, int> unlockedMoons = ES3.Load<Dictionary<string, int>>("LMU_UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName);
                dictionary.Add("LMU_UnlockedMoons", unlockedMoons);
                Plugin.Instance.Mls.LogInfo($"Loaded LMU_UnlockedMoons: {string.Join(", ", unlockedMoons)}");
            }
            if (ES3.KeyExists("LMU_OriginalMoonPrices", GameNetworkManager.Instance.currentSaveFileName)) {
                Dictionary<string, int> originalPrices = ES3.Load<Dictionary<string, int>>("LMU_OriginalMoonPrices", GameNetworkManager.Instance.currentSaveFileName);
                dictionary.Add("LMU_OriginalMoonPrices", originalPrices);
                Plugin.Instance.Mls.LogInfo($"Loaded LMU_OriginalMoonPrices: {string.Join(", ", originalPrices)}");
            }
            if (ES3.KeyExists("LMU_QuotaCount", GameNetworkManager.Instance.currentSaveFileName)) {
                int quotaCount = ES3.Load<int>("LMU_QuotaCount", GameNetworkManager.Instance.currentSaveFileName);
                dictionary.Add("LMU_QuotaCount", quotaCount);
                Plugin.Instance.Mls.LogInfo($"Loaded LMU_QuotaCount: {quotaCount}");
            }
            return dictionary;
        }

        public static void StoreSaveData() {
            Plugin.Instance.Mls.LogInfo($"Saving data..");
            if (UnlockManager.Instance.UnlockedMoons.Count != 0) {
                ES3.Save<Dictionary<string, int>>("LMU_UnlockedMoons", UnlockManager.Instance.UnlockedMoons, GameNetworkManager.Instance.currentSaveFileName);
                Plugin.Instance.Mls.LogInfo($"Saved LMU_UnlockedMoons: {string.Join(", ", UnlockManager.Instance.UnlockedMoons)}");
            } else if (ES3.KeyExists("LMU_UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName)) {
                ES3.DeleteKey("LMU_UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName);
            }
            if (UnlockManager.Instance.OriginalPrices.Count != 0) {
                ES3.Save<Dictionary<string, int>>("LMU_OriginalMoonPrices", UnlockManager.Instance.OriginalPrices, GameNetworkManager.Instance.currentSaveFileName);
                Plugin.Instance.Mls.LogInfo($"Saved LMU_OriginalMoonPrices: {string.Join(", ", UnlockManager.Instance.UnlockedMoons)}");
            } else if (ES3.KeyExists("LMU_OriginalMoonPrices", GameNetworkManager.Instance.currentSaveFileName)) {
                ES3.DeleteKey("LMU_OriginalMoonPrices", GameNetworkManager.Instance.currentSaveFileName);
            }
            if (UnlockManager.Instance.QuotaCount > 0) {
                ES3.Save<int>("LMU_QuotaCount", UnlockManager.Instance.QuotaCount, GameNetworkManager.Instance.currentSaveFileName);
                Plugin.Instance.Mls.LogInfo($"Saved LMU_QuotaCount: {string.Join(", ", UnlockManager.Instance.UnlockedMoons)}");
            } else if (ES3.KeyExists("LMU_QuotaCount", GameNetworkManager.Instance.currentSaveFileName)) {
                ES3.DeleteKey("LMU_QuotaCount", GameNetworkManager.Instance.currentSaveFileName);
            }
        }
    }
}
