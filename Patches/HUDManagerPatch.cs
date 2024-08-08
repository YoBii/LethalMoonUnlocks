using HarmonyLib;
using LethalMoonUnlocks.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch {
        [HarmonyPatch(nameof(HUDManager.DisplayTip))]
        [HarmonyPrefix]
        private static bool DisplayTipPatch(string headerText, string bodyText, bool isWarning, bool useSave, string prefsKey) {
            if (!ConfigManager.AlertMessageQueueing || !ConfigManager.ShowAlerts)
                return true;
            if (!HUDManager.Instance.CanTipDisplay(isWarning, useSave, prefsKey)) {
                return false;
            }
            if (!prefsKey.StartsWith("LMU_")) {
                Plugin.Instance.Mls.LogDebug($"Intercepted alert: header = {headerText}, body = {bodyText}, warning = {isWarning}, useSave = {useSave}, key = {prefsKey}!");
                NotificationHelper.AddNotificationToQueue(new Notification() { Header = headerText, Text = bodyText, IsWarning = isWarning, UseSave = false, Key = "LMU_Intercept_" + prefsKey });
                DelayHelper.Instance.StartCoroutine(NotificationHelper.SendQueuedNotifications());
                return false;
            }
            return true;
        }
    }
}
