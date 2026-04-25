using BepInEx.Bootstrap;
using HarmonyLib;
using LethalMoonUnlocks.Compatibility;
using LethalMoonUnlocks.Util;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch {
        private static readonly string GameAssemblyName = typeof(HUDManagerPatch).Assembly.GetName().Name;
        private static readonly string HarmonyAssemblyName = typeof(Harmony).Assembly.GetName().Name;

        [HarmonyPatch(nameof(HUDManager.DisplayTip))]
        [HarmonyPrefix]
        private static bool DisplayTipPatch(string headerText, string bodyText, bool isWarning, bool useSave, string prefsKey) {
            string safePrefsKey = prefsKey ?? string.Empty;

            if (!ConfigManager.AlertMessageQueueing)
                return true;
            if (!HUDManager.Instance.CanTipDisplay(isWarning, useSave, safePrefsKey)) {
                return false;
            }
            if (!string.IsNullOrEmpty(bodyText) && headerText == "Route Discovered!" && bodyText.StartsWith("Location: ")) {
                if (ConfigManager.DiscoveryMode && ConfigManager.MoonStoryReleaseBehavior == StoryReleaseBehavior.HiddenBacklog) {
                    JLLCompatibility.ReplaceJLLAlertDiscovery(bodyText);
                }
                else {
                    JLLCompatibility.ReplaceJLLAlert(bodyText);
                }
                return false;
            }
            if (!safePrefsKey.StartsWith("LMU_", StringComparison.Ordinal)) {
                string callerAssemblyName = TryGetAlertCallerAssembly();
                string loggedCallerAssemblyName = string.IsNullOrWhiteSpace(callerAssemblyName) ? "unknown" : callerAssemblyName;
                string callerPluginGuid = TryResolveAlertCallerPlugin(callerAssemblyName);
                string loggedCallerPluginGuid = string.IsNullOrWhiteSpace(callerPluginGuid) ? "unresolved" : callerPluginGuid;

                if (!string.IsNullOrWhiteSpace(callerPluginGuid) && ConfigManager.AlertMessageQueueExcludedPlugins.Contains(callerPluginGuid)) {
                    Logger.LogDebug($"Bypassing alert queue for excluded plugin: plugin = {loggedCallerPluginGuid}, source = {loggedCallerAssemblyName}, key = {safePrefsKey}!");
                    return true;
                }

                Logger.LogDebug($"Intercepted alert: plugin = {loggedCallerPluginGuid}, source = {loggedCallerAssemblyName}, header = {headerText}, warning = {isWarning}, useSave = {useSave}, key = {safePrefsKey}!");
                Logger.LogDebug($"Intercepted alert: body = {bodyText}!");
                NotificationHelper.AddNotificationToQueue(new Notification() { Header = headerText, Text = bodyText, IsWarning = isWarning, UseSave = false, Key = "LMU_Intercept_" + safePrefsKey });
                DelayHelper.Instance.StartCoroutine(NotificationHelper.SendQueuedNotifications());
                return false;
            }
            return true;
        }

        private static string TryGetAlertCallerAssembly() {
            StackFrame[] frames = new StackTrace(1, false).GetFrames();
            if (frames == null) {
                return string.Empty;
            }

            foreach (StackFrame frame in frames) {
                MethodBase method = frame.GetMethod();
                Type declaringType = method?.DeclaringType;
                string assemblyName = declaringType?.Assembly?.GetName().Name;

                if (string.IsNullOrWhiteSpace(assemblyName)) {
                    continue;
                }

                if (string.Equals(assemblyName, GameAssemblyName, StringComparison.Ordinal)
                    || string.Equals(assemblyName, HarmonyAssemblyName, StringComparison.Ordinal)
                    || string.Equals(assemblyName, "DynamicMethodsAssembly", StringComparison.Ordinal)
                    || declaringType == typeof(HUDManager)) {
                    continue;
                }

                return assemblyName;
            }

            return string.Empty;
        }

        private static string TryResolveAlertCallerPlugin(string callerAssemblyName) {
            if (string.IsNullOrWhiteSpace(callerAssemblyName)) {
                return null;
            }

            foreach (var plugin in Chainloader.PluginInfos.Values.Where(plugin => plugin != null)) {
                string pluginAssemblyName = plugin?.GetType()?.Assembly?.GetName()?.Name;
                if (string.Equals(pluginAssemblyName, callerAssemblyName, StringComparison.OrdinalIgnoreCase)) {
                    return plugin.Metadata.GUID;
                }
            }
            
            return null;
        }
    }
}
