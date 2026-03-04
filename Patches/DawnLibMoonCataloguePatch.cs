using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Dawn;
using HarmonyLib;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch]
    internal class DawnLibMoonCataloguePatch {
        private static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("MoonRegistrationHandler");
            return AccessTools.Method(type, "FormatMoonEntry");
        }

        private static void Postfix(DawnMoonInfo moonInfo, TerminalPurchaseResult result, ref string __result) {
            if (!ConfigManager.DisplayTerminalTags) {
                return;
            }
            
            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                if (unlock.Name == moonInfo.GetNumberlessPlanetName()) {
                    string tags = unlock.BuildTagString();
                    if (!string.IsNullOrEmpty(tags)) {
                        __result += tags;
                    }
                }
            }
        }
    }
}
