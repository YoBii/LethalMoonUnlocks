using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch {

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
                Plugin.Instance.Mls.LogInfo($"After travel arriving at: {LevelManager.CurrentExtendedLevel.NumberlessPlanetName}");
                UnlockManager.Instance.OnArrive();
            }
        }
    }
}
