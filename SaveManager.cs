using LethalLevelLoader;
using System.Collections.Generic;

namespace LethalMoonUnlocks {
    internal static class SaveManager {
        public static Dictionary<string, object> Load() {
            Dictionary<string, object> savedata = new Dictionary<string, object>();
            if (!ES3.KeyExists("LMU_UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName) &&
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
            var currentSave = GameNetworkManager.Instance.currentSaveFileName;
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            if (ES3.KeyExists("UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName)) {
                Plugin.Instance.Mls.LogInfo($"Permanent moons save data found! Importing..");
                List<string> PMunlockedMoons = ES3.Load<List<string>>("UnlockedMoons", GameNetworkManager.Instance.currentSaveFileName);
                Dictionary<string, int> unlockedMoons = new Dictionary<string, int>();
                foreach (var moon in PMunlockedMoons) {
                    foreach (var level in UnlockManager.Instance.GetLevels()) {
                        if (moon.Contains(level.NumberlessPlanetName, System.StringComparison.OrdinalIgnoreCase)) {
                            unlockedMoons.TryAdd(level.NumberlessPlanetName, 1);
                        }
                    }
                }
                if (unlockedMoons.Count > 0) {
                    dictionary.Add("LMU_UnlockedMoons", unlockedMoons);
                    Plugin.Instance.Mls.LogInfo($"Imported UnlockedMoons from Permanent Moons: {string.Join(", ", unlockedMoons)}");
                } else {
                    Plugin.Instance.Mls.LogInfo($"No moons match the Permanent moons save data.");
                    return dictionary;
                }
            } else {
                Plugin.Instance.Mls.LogInfo($"No Permanent moons data found.");
                return dictionary;
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
            var currentSave = GameNetworkManager.Instance.currentSaveFileName;
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            if (ES3.KeyExists("LMU_UnlockedMoons", currentSave)) {
                Dictionary<string, int> unlockedMoons = ES3.Load<Dictionary<string, int>>("LMU_UnlockedMoons", currentSave);
                dictionary.Add("LMU_UnlockedMoons", unlockedMoons);
                Plugin.Instance.Mls.LogInfo($"Loaded LMU_UnlockedMoons: {string.Join(", ", unlockedMoons)}");
            }
            if (ES3.KeyExists("LMU_QuotaCount", currentSave)) {
                int quotaCount = ES3.Load<int>("LMU_QuotaCount", currentSave);
                dictionary.Add("LMU_QuotaCount", quotaCount);
                Plugin.Instance.Mls.LogInfo($"Loaded LMU_QuotaCount: {quotaCount}");
            }
            return dictionary;
        }

        public static void StoreSaveData() {
            Plugin.Instance.Mls.LogInfo($"Saving data..");
            var currentSave = GameNetworkManager.Instance.currentSaveFileName;
            if (UnlockManager.Instance.UnlockedMoons.Count != 0) {
                ES3.Save<Dictionary<string, int>>("LMU_UnlockedMoons", UnlockManager.Instance.UnlockedMoons, currentSave);
                Plugin.Instance.Mls.LogInfo($"Saved LMU_UnlockedMoons: {string.Join(", ", UnlockManager.Instance.UnlockedMoons)}");
            } else if (ES3.KeyExists("LMU_UnlockedMoons", currentSave)) {
                ES3.DeleteKey("LMU_UnlockedMoons", currentSave);
            }
            if (ES3.KeyExists("LMU_OriginalMoonPrices", currentSave)) {
                // Delete deprecated original prices field in existing savefiles
                ES3.DeleteKey("LMU_OriginalMoonPrices", currentSave);
            }
            if (UnlockManager.Instance.QuotaCount > 0) {
                ES3.Save<int>("LMU_QuotaCount", UnlockManager.Instance.QuotaCount, currentSave);
                Plugin.Instance.Mls.LogInfo($"Saved LMU_QuotaCount: {string.Join(", ", UnlockManager.Instance.QuotaCount)}");
            } else if (ES3.KeyExists("LMU_QuotaCount", currentSave)) {
                ES3.DeleteKey("LMU_QuotaCount", currentSave);
            }
        }
    }
}
