using LethalLevelLoader;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.UIElements.UIR.BestFitAllocator;

namespace LethalMoonUnlocks {
    public static class SaveManager {
        public static Dictionary<string, object> Savedata {
            get { return Load(); }
        }

        private static Dictionary<string, object> Load() {
            Plugin.Instance.Mls.LogInfo($"Loading save data..");
            var currentSave = GameNetworkManager.Instance.currentSaveFileName;
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            if (ES3.KeyExists("LMU_Unlockables", currentSave)) {
                List<LMUnlockable> unlockedMoons = ES3.Load<List<LMUnlockable>>("LMU_Unlockables", currentSave);
                dictionary.Add("LMU_Unlockables", unlockedMoons);
                Plugin.Instance.Mls.LogInfo($"Loading LMU_Unlockables: {string.Join(", ", unlockedMoons.Select(unlock => unlock.Name))}");
                if (ES3.KeyExists("LMU_QuotaCount", currentSave)) {
                    int quotaCount = ES3.Load<int>("LMU_QuotaCount", currentSave);
                    dictionary.Add("LMU_QuotaCount", quotaCount);
                    Plugin.Instance.Mls.LogInfo($"Loading LMU_QuotaCount: {quotaCount}");
                }
                if (ES3.KeyExists("LMU_DayCount", currentSave)) {
                    int dayCount = ES3.Load<int>("LMU_DayCount", currentSave);
                    dictionary.Add("LMU_DayCount", dayCount);
                    Plugin.Instance.Mls.LogInfo($"Loading LMU_DayCount: {dayCount}");
                }
                if (ES3.KeyExists("LMU_QuotaUnlocksCount", currentSave)) {
                    int quotaUnlocksCount = ES3.Load<int>("LMU_QuotaUnlocksCount", currentSave);
                    dictionary.Add("LMU_QuotaUnlocksCount", quotaUnlocksCount);
                    Plugin.Instance.Mls.LogInfo($"Loading LMU_QuotaUnlocksCount: {quotaUnlocksCount}");
                }
                if (ES3.KeyExists("LMU_QuotaFullDiscountsCount", currentSave)) {
                    int quotaFullDiscountsCount = ES3.Load<int>("LMU_QuotaFullDiscountsCount", currentSave);
                    dictionary.Add("LMU_QuotaFullDiscountsCount", quotaFullDiscountsCount);
                    Plugin.Instance.Mls.LogInfo($"Loading LMU_QuotaFullDiscountsCount: {quotaFullDiscountsCount}");
                }
                return dictionary;
            } else {
                // Old and deprecated keys
                if (ES3.KeyExists("LMU_UnlockedMoons", currentSave)) {
                    Dictionary<string, int> unlockedMoons = ES3.Load<Dictionary<string, int>>("LMU_UnlockedMoons", currentSave);
                    dictionary.Add("LMU_UnlockedMoons", unlockedMoons);
                    Plugin.Instance.Mls.LogInfo($"Loading deprecated LMU_UnlockedMoons: {string.Join(", ", unlockedMoons)}");
                }
                if (ES3.KeyExists("LMU_OriginalMoonPrices", currentSave)) {
                    Dictionary<string, int> originalPrices = ES3.Load<Dictionary<string, int>>("LMU_OriginalMoonPrices", currentSave);
                    dictionary.Add("LMU_OriginalMoonPrices", originalPrices);
                    Plugin.Instance.Mls.LogInfo($"Loading deprecated LMU_OriginalMoonPrices: {string.Join(", ", originalPrices)}");
                }
                // Permanent moons data
                if (ES3.KeyExists("UnlockedMoons", currentSave)) {
                    List<string> unlockedMoons = ES3.Load<List<string>>("UnlockedMoons", currentSave);
                    dictionary.Add("UnlockedMoons", unlockedMoons);
                    Plugin.Instance.Mls.LogInfo($"Loading Permanent Moons data UnlockedMoons: {string.Join(", ", unlockedMoons)}");
                }
                if (ES3.KeyExists("MoonQuotaNum", currentSave)) {
                    int quotaCount = ES3.Load<int>("MoonQuotaNum", currentSave);
                    dictionary.Add("MoonQuotaNum", quotaCount);
                    Plugin.Instance.Mls.LogInfo($"Loading Permanet Moons data MoonQuotaNum: {quotaCount}");
                }
                return dictionary;
            }            
        }

        public static void StoreSaveData() {
            Plugin.Instance.Mls.LogInfo($"Saving data..");
            var currentSave = GameNetworkManager.Instance.currentSaveFileName;
            if (UnlockManager.Instance.Unlocks.Count != 0) {
                ES3.Save<List<LMUnlockable>>("LMU_Unlockables", UnlockManager.Instance.Unlocks, currentSave);
                Plugin.Instance.Mls.LogInfo($"Saving LMU_Unlockables..");
                UnlockManager.Instance.LogUnlockables();
            } else if (ES3.KeyExists("LMU_Unlockables", currentSave)) {
                ES3.DeleteKey("LMU_Unlockables", currentSave);
            }
            if (UnlockManager.Instance.QuotaCount > 0) {
                ES3.Save<int>("LMU_QuotaCount", UnlockManager.Instance.QuotaCount, currentSave);
                Plugin.Instance.Mls.LogInfo($"Saving LMU_QuotaCount: {string.Join(", ", UnlockManager.Instance.QuotaCount)}");
            } else if (ES3.KeyExists("LMU_QuotaCount", currentSave)) {
                ES3.DeleteKey("LMU_QuotaCount", currentSave);
            }
            if (UnlockManager.Instance.DayCount > 0) {
                ES3.Save<int>("LMU_DayCount", UnlockManager.Instance.DayCount, currentSave);
                Plugin.Instance.Mls.LogInfo($"Saving LMU_DayCount: {string.Join(", ", UnlockManager.Instance.DayCount)}");
            } else if (ES3.KeyExists("LMU_DayCount", currentSave)) {
                ES3.DeleteKey("LMU_DayCount", currentSave);
            }
            if (UnlockManager.Instance.QuotaUnlocksCount > 0) {
                ES3.Save<int>("LMU_QuotaUnlocksCount", UnlockManager.Instance.QuotaUnlocksCount, currentSave);
                Plugin.Instance.Mls.LogInfo($"Saving LMU_QuotaUnlocksCount: {string.Join(", ", UnlockManager.Instance.QuotaUnlocksCount)}");
            } else if (ES3.KeyExists("LMU_QuotaUnlocksCount", currentSave)) {
                ES3.DeleteKey("LMU_QuotaUnlocksCount", currentSave);
            }
            if (UnlockManager.Instance.QuotaDiscountsCount > 0) {
                ES3.Save<int>("LMU_QuotaDiscountsCount", UnlockManager.Instance.QuotaDiscountsCount, currentSave);
                Plugin.Instance.Mls.LogInfo($"Saving LMU_QuotaDiscountsCount: {string.Join(", ", UnlockManager.Instance.QuotaDiscountsCount)}");
            } else if (ES3.KeyExists("LMU_QuotaDiscountsCount", currentSave)) {
                ES3.DeleteKey("LMU_QuotaDiscountsCount", currentSave);
            }
            if (UnlockManager.Instance.QuotaFullDiscountsCount > 0) {
                ES3.Save<int>("LMU_QuotaFullDiscountsCount", UnlockManager.Instance.QuotaCount, currentSave);
                Plugin.Instance.Mls.LogInfo($"Saving LMU_QuotaFullDiscountsCount: {string.Join(", ", UnlockManager.Instance.QuotaFullDiscountsCount)}");
            } else if (ES3.KeyExists("LMU_QuotaFullDiscountsCount", currentSave)) {
                ES3.DeleteKey("LMU_QuotaFullDiscountsCount", currentSave);
            }

            // Delete deprecated fields in existing savefiles
            if (ES3.KeyExists("LMU_UnlockedMoons", currentSave)) {
                Plugin.Instance.Mls.LogInfo($"Deleting deprecated save field: LMU_UnlockedMoons");
                ES3.DeleteKey("LMU_UnlockedMoons", currentSave);
            }
            if (ES3.KeyExists("LMU_OriginalMoonPrices", currentSave)) {
                Plugin.Instance.Mls.LogInfo($"Deleting deprecated save field: LMU_OriginalMoonPrices");
                ES3.DeleteKey("LMU_OriginalMoonPrices", currentSave);
            }
            //if (ES3.KeyExists("UnlockedMoons", currentSave)) {
            //    ES3.DeleteKey("UnlockedMoons", currentSave);
            //}
            //if (ES3.KeyExists("OriginalMoonPrices", currentSave)) {
            //    ES3.DeleteKey("OriginalMoonPrices", currentSave);
            //}
            //if (ES3.KeyExists("MoonQuotaNum", currentSave)) {
            //    ES3.DeleteKey("MoonQuotaNum", currentSave);
            //}
        }
    }
}
