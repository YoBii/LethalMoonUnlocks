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
            if (Plugin.Instance.IsServer()) {
                UnlockManager.Instance.DayCount++;
                UnlockManager.Instance.OnNewDay();
                Plugin.Instance.Mls.LogInfo($"New day! Completed days: {UnlockManager.Instance.DayCount}");
            }
        }

        [HarmonyPatch("ArriveAtLevel")]
        [HarmonyPostfix]
        private static void ArriveAtLevelPatch() {
            if (Plugin.Instance.IsServer()) {
                Plugin.Instance.Mls.LogInfo($"After travel arriving at: {LevelManager.CurrentExtendedLevel.NumberlessPlanetName}");
                UnlockManager.Instance.OnArrive();
            }
        }
        //[HarmonyPatch(nameof(StartOfRound.ResetShip))]
        //[HarmonyPostfix]
        //private static void ResetShipPatch() {
        //    Plugin.Instance.Mls.LogInfo($"ResetShipPatch..");
        //    if (!Plugin.Instance.keepUnlocks) {
        //        Plugin.Instance.ResetMoons();
        //    }
        //}
    }
}
