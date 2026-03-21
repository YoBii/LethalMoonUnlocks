using HarmonyLib;
using LethalMoonUnlocks.Compatibility;
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
            if (!ConfigManager.AlertMessageQueueing)
                return true;
            if (!HUDManager.Instance.CanTipDisplay(isWarning, useSave, prefsKey)) {
                return false;
            }
            if (!string.IsNullOrEmpty(bodyText) && headerText == "Route Discovered!" && bodyText.StartsWith("Location: ")) {
                if (ConfigManager.DiscoveryMode) {
                    JLLCompatibility.ReplaceJLLAlertDiscovery(bodyText);
                }
                else {
                    JLLCompatibility.ReplaceJLLAlert(bodyText);
                }
                return false;
            }
            if (!prefsKey.StartsWith("LMU_")) {
                Logger.LogDebug($"Intercepted alert: key = {prefsKey}!");
                NotificationHelper.AddNotificationToQueue(new Notification() { Header = headerText, Text = bodyText, IsWarning = isWarning, UseSave = false, Key = "LMU_Intercept_" + prefsKey });
                DelayHelper.Instance.StartCoroutine(NotificationHelper.SendQueuedNotifications());
                return false;
            }
            return true;
        }
    }
}
