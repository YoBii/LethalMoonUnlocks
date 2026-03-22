using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch {
        
        [HarmonyPrefix]
        [HarmonyAfter("evaisa.lethallib", "imabatby.lethallevelloader", "com.github.teamxiaolan.dawnlib")]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        private static void StartOfRoundStartPrefix(StartOfRound __instance, bool __runOriginal) {
            if (Plugin.DawnLibPresent) {
                UnlockManager.Instance.InitializeUnlocksDawnLib();
            }
        }

        [HarmonyPatch("PassTimeToNextDay")]
        [HarmonyPostfix]
        private static void PassTimeToNextDay() {
            if (NetworkManager.Instance.IsServer()) {
                UnlockManager.Instance.OnNewDay();
            }
        }

        [HarmonyPatch("ArriveAtLevel")]
        [HarmonyPostfix]
        private static void ArriveAtLevelPatch() {
            if (NetworkManager.Instance.IsServer()) {
                Logger.LogInfo($"After travel arriving at: {LevelManager.CurrentExtendedLevel.NumberlessPlanetName}");
                UnlockManager.Instance.OnArrive();
            }
        }
    }
}
