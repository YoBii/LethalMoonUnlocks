using System.Collections.Generic;
using System.Linq;
using LethalMoonUnlocks.Compatibility;

namespace LethalMoonUnlocks {
    internal static class SaveManager {
        private static readonly string[] CurrentSaveKeys = [
            "LMU_Unlockables",
            "LMU_QuotaCount",
            "LMU_DayCount",
            "LMU_QuotaUnlocksCount",
            "LMU_QuotaDiscountsCount",
            "LMU_QuotaFullDiscountsCount",
            "LMU_Progression",
            "LMU_LethalConstellations"
        ];

        internal static Dictionary<string, object> Savedata {
            get { return Load(); }
        }

        private static bool HasCurrentSaveData(string currentSave) {
            return CurrentSaveKeys.Any(key => ES3.KeyExists(key, currentSave));
        }

        private static Dictionary<string, object> Load() {
            Logger.LogInfo($"Loading save data..");
            var currentSave = GameNetworkManager.Instance.currentSaveFileName;           
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            if (HasCurrentSaveData(currentSave)) {
                if (ES3.KeyExists("LMU_Unlockables", currentSave)) {
                    List<LMUnlockable> unlockedMoons = ES3.Load<List<LMUnlockable>>("LMU_Unlockables", currentSave);
                    dictionary.Add("LMU_Unlockables", unlockedMoons);
                    Logger.LogInfo($"Found LMU_Unlockables: {string.Join(", ", unlockedMoons.Select(unlock => unlock.Name))}");
                }
                if (ES3.KeyExists("LMU_QuotaCount", currentSave)) {
                    int quotaCount = ES3.Load<int>("LMU_QuotaCount", currentSave);
                    dictionary.Add("LMU_QuotaCount", quotaCount);
                    Logger.LogInfo($"Found LMU_QuotaCount: {quotaCount}");
                }
                if (ES3.KeyExists("LMU_DayCount", currentSave)) {
                    int dayCount = ES3.Load<int>("LMU_DayCount", currentSave);
                    dictionary.Add("LMU_DayCount", dayCount);
                    Logger.LogInfo($"Found LMU_DayCount: {dayCount}");
                }
                if (ES3.KeyExists("LMU_QuotaUnlocksCount", currentSave)) {
                    int quotaUnlocksCount = ES3.Load<int>("LMU_QuotaUnlocksCount", currentSave);
                    dictionary.Add("LMU_QuotaUnlocksCount", quotaUnlocksCount);
                    Logger.LogInfo($"Found LMU_QuotaUnlocksCount: {quotaUnlocksCount}");
                }
                if (ES3.KeyExists("LMU_QuotaDiscountsCount", currentSave)) {
                    int quotaDiscountsCount = ES3.Load<int>("LMU_QuotaDiscountsCount", currentSave);
                    dictionary.Add("LMU_QuotaDiscountsCount", quotaDiscountsCount);
                    Logger.LogInfo($"Found LMU_QuotaDiscountsCount: {quotaDiscountsCount}");
                }
                if (ES3.KeyExists("LMU_QuotaFullDiscountsCount", currentSave)) {
                    int quotaFullDiscountsCount = ES3.Load<int>("LMU_QuotaFullDiscountsCount", currentSave);
                    dictionary.Add("LMU_QuotaFullDiscountsCount", quotaFullDiscountsCount);
                    Logger.LogInfo($"Found LMU_QuotaFullDiscountsCount: {quotaFullDiscountsCount}");
                }
                if (ES3.KeyExists("LMU_Progression", currentSave)) {
                    ProgressionSaveData progressionSaveData = ES3.Load<ProgressionSaveData>("LMU_Progression", currentSave);
                    dictionary.Add("LMU_Progression", progressionSaveData);
                    Logger.LogInfo($"Found LMU_Progression: PaintingsSold={progressionSaveData.PaintingsSold}");
                }
                if (ES3.KeyExists("LMU_LethalConstellations", currentSave)) {
                    LethalConstellationsSaveData lethalConstellationsSaveData = ES3.Load<LethalConstellationsSaveData>("LMU_LethalConstellations", currentSave);
                    dictionary.Add("LMU_LethalConstellations", lethalConstellationsSaveData);
                    Logger.LogInfo($"Found LMU_LethalConstellations: {lethalConstellationsSaveData?.Constellations?.Count ?? 0} constellations, seed={lethalConstellationsSaveData?.DiscoveryRotationSeed ?? 0}");
                }

                // BAND AID FIX for credits being wacky
                if (ConfigManager.GroupCreditsSavingBandAid) {
                    if (ES3.KeyExists("GroupCredits", currentSave)) {
                        dictionary.Add("GroupCredits", ES3.Load<int>("GroupCredits", currentSave));
                    }
                }

                return dictionary;
            } else {
                // Old and deprecated keys
                if (ES3.KeyExists("LMU_UnlockedMoons", currentSave)) {
                    Dictionary<string, int> unlockedMoons = ES3.Load<Dictionary<string, int>>("LMU_UnlockedMoons", currentSave);
                    dictionary.Add("LMU_UnlockedMoons", unlockedMoons);
                    Logger.LogInfo($"Found deprecated LMU_UnlockedMoons: {string.Join(", ", unlockedMoons)}");
                }
                if (ES3.KeyExists("LMU_OriginalMoonPrices", currentSave)) {
                    Dictionary<string, int> originalPrices = ES3.Load<Dictionary<string, int>>("LMU_OriginalMoonPrices", currentSave);
                    dictionary.Add("LMU_OriginalMoonPrices", originalPrices);
                    Logger.LogInfo($"Found deprecated LMU_OriginalMoonPrices: {string.Join(", ", originalPrices)}");
                }
                // Permanent moons data
                if (ES3.KeyExists("UnlockedMoons", currentSave)) {
                    List<string> unlockedMoons = ES3.Load<List<string>>("UnlockedMoons", currentSave);
                    dictionary.Add("UnlockedMoons", unlockedMoons);
                    Logger.LogInfo($"Found Permanent Moons data UnlockedMoons: {string.Join(", ", unlockedMoons)}");
                }
                if (ES3.KeyExists("MoonQuotaNum", currentSave)) {
                    int quotaCount = ES3.Load<int>("MoonQuotaNum", currentSave);
                    dictionary.Add("MoonQuotaNum", quotaCount);
                    Logger.LogInfo($"Found Permanent Moons data MoonQuotaNum: {quotaCount}");
                }
                return dictionary;
            }            
        }

        internal static void StoreSaveData() {
            Logger.LogInfo($"Saving data..");
            var currentSave = GameNetworkManager.Instance.currentSaveFileName;
            if (UnlockManager.Instance.Unlocks.Count != 0) {
                ES3.Save<List<LMUnlockable>>("LMU_Unlockables", UnlockManager.Instance.Unlocks, currentSave);
                Logger.LogInfo($"Saving LMU_Unlockables..");
                UnlockManager.Instance.LogUnlockables();
            } else if (ES3.KeyExists("LMU_Unlockables", currentSave)) {
                ES3.DeleteKey("LMU_Unlockables", currentSave);
            }
            if (UnlockManager.Instance.QuotaCount > 0) {
                ES3.Save<int>("LMU_QuotaCount", UnlockManager.Instance.QuotaCount, currentSave);
                Logger.LogInfo($"Saving LMU_QuotaCount: {string.Join(", ", UnlockManager.Instance.QuotaCount)}");
            } else if (ES3.KeyExists("LMU_QuotaCount", currentSave)) {
                ES3.DeleteKey("LMU_QuotaCount", currentSave);
            }
            if (UnlockManager.Instance.DayCount > 0) {
                ES3.Save<int>("LMU_DayCount", UnlockManager.Instance.DayCount, currentSave);
                Logger.LogInfo($"Saving LMU_DayCount: {string.Join(", ", UnlockManager.Instance.DayCount)}");
            } else if (ES3.KeyExists("LMU_DayCount", currentSave)) {
                ES3.DeleteKey("LMU_DayCount", currentSave);
            }
            if (UnlockManager.Instance.QuotaUnlocksCount > 0) {
                ES3.Save<int>("LMU_QuotaUnlocksCount", UnlockManager.Instance.QuotaUnlocksCount, currentSave);
                Logger.LogInfo($"Saving LMU_QuotaUnlocksCount: {string.Join(", ", UnlockManager.Instance.QuotaUnlocksCount)}");
            } else if (ES3.KeyExists("LMU_QuotaUnlocksCount", currentSave)) {
                ES3.DeleteKey("LMU_QuotaUnlocksCount", currentSave);
            }
            if (UnlockManager.Instance.QuotaDiscountsCount > 0) {
                ES3.Save<int>("LMU_QuotaDiscountsCount", UnlockManager.Instance.QuotaDiscountsCount, currentSave);
                Logger.LogInfo($"Saving LMU_QuotaDiscountsCount: {string.Join(", ", UnlockManager.Instance.QuotaDiscountsCount)}");
            } else if (ES3.KeyExists("LMU_QuotaDiscountsCount", currentSave)) {
                ES3.DeleteKey("LMU_QuotaDiscountsCount", currentSave);
            }
            if (UnlockManager.Instance.QuotaFullDiscountsCount > 0) {
                ES3.Save<int>("LMU_QuotaFullDiscountsCount", UnlockManager.Instance.QuotaFullDiscountsCount, currentSave);
                Logger.LogInfo($"Saving LMU_QuotaFullDiscountsCount: {string.Join(", ", UnlockManager.Instance.QuotaFullDiscountsCount)}");
            } else if (ES3.KeyExists("LMU_QuotaFullDiscountsCount", currentSave)) {
                ES3.DeleteKey("LMU_QuotaFullDiscountsCount", currentSave);
            }
            if (ProgressionManager.Instance != null) {
                ProgressionSaveData progressionSaveData = ProgressionManager.Instance.GetSaveData();
                if (progressionSaveData.HasData()) {
                    ES3.Save<ProgressionSaveData>("LMU_Progression", progressionSaveData, currentSave);
                    Logger.LogInfo("Saving LMU_Progression.");
                } else if (ES3.KeyExists("LMU_Progression", currentSave)) {
                    ES3.DeleteKey("LMU_Progression", currentSave);
                }
            }
            if (Plugin.LethalConstellationsPresent && Plugin.LethalConstellationsExtension != null) {
                LethalConstellationsSaveData lethalConstellationsSaveData = Plugin.LethalConstellationsExtension.GetSaveData();
                lethalConstellationsSaveData.DiscoveryRotationSeed = UnlockManager.Instance?.DiscoveryRotationSeed ?? 0;
                lethalConstellationsSaveData.LocalConstellationDiscoveries = Plugin.ConstellationManager != null
                    ? Plugin.ConstellationManager.GetAllLocalMoonDiscoveries()
                    : new Dictionary<string, List<string>>();

                if (lethalConstellationsSaveData.HasData()) {
                    ES3.Save<LethalConstellationsSaveData>("LMU_LethalConstellations", lethalConstellationsSaveData, currentSave);
                    Logger.LogInfo($"Saving LMU_LethalConstellations: {lethalConstellationsSaveData.Constellations.Count} constellations, seed={lethalConstellationsSaveData.DiscoveryRotationSeed}");
                } else if (ES3.KeyExists("LMU_LethalConstellations", currentSave)) {
                    ES3.DeleteKey("LMU_LethalConstellations", currentSave);
                }
            }

            // BAND AID FIX for group credits being wacky
            if (ConfigManager.GroupCreditsSavingBandAid) {
                int groupCredits = UnlockManager.Instance.Terminal.groupCredits;
                Logger.LogInfo($"BAND-AID: Saving group credits ({groupCredits})..");
                ES3.Save<int>("GroupCredits", groupCredits, currentSave);
            }

            // Delete deprecated fields in existing savefiles
            if (ES3.KeyExists("LMU_UnlockedMoons", currentSave)) {
                Logger.LogInfo($"Deleting deprecated save field: LMU_UnlockedMoons");
                ES3.DeleteKey("LMU_UnlockedMoons", currentSave);
            }
            if (ES3.KeyExists("LMU_OriginalMoonPrices", currentSave)) {
                Logger.LogInfo($"Deleting deprecated save field: LMU_OriginalMoonPrices");
                ES3.DeleteKey("LMU_OriginalMoonPrices", currentSave);
            }
        }
    }
}
