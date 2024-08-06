using HarmonyLib;
using LethalQuantities.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LethalMoonUnlocks.Compatibility {
    [HarmonyPatch]
    internal class MalfunctionsCompatibility {  
        private static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("Malfunctions.Patches.StartOfRoundPatches");
            MethodBase mb = AccessTools.FirstMethod(type, method => method.Name.Contains("HandleRollNavigation"));
            return mb;

        }
        [HarmonyPostfix]
        private static void HandleRollNavigationPostfix(bool result, int level) {
            // invert checks from original function + my config
            if (!result || StartOfRound.Instance.currentLevel.name == "CompanyBuildingLevel" || TimeOfDay.Instance.daysUntilDeadline < 2 || !ConfigManager.MalfunctionsNavigation) return;
            var unlock = UnlockManager.Instance.Unlocks.Where(unlock => unlock.ExtendedLevel.SelectableLevel.levelID == level).FirstOrDefault();
            if (unlock != null && NetworkManager.Instance.IsServer() && unlock.ExtendedLevel.RoutePrice > 0) {
                Plugin.Instance.Mls.LogInfo($"Interpreting navigation malfunction as buying the moon..");
                UnlockManager.Instance.BuyMoon(unlock.Name);
            }
        }
    }
}
