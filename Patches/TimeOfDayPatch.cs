using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(TimeOfDay))]
    internal class TimeOfDayPatch {
        [HarmonyPatch(nameof(TimeOfDay.SetNewProfitQuota))]
        [HarmonyPrefix]
        private static void SetNewProfitQuotaPatch() {
            if (NetworkManager.Instance.IsServer()) {
                UnlockManager.Instance.OnNewQuota();
            }
        }
    }
}
