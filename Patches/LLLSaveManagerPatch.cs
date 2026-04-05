using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch]
    internal class LLLSaveManagerInitPatch {
        private static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("LethalLevelLoader.SaveManager");
            return AccessTools.Method(type, "InitializeSave");
        }

        private static void Prefix() {
            UnlockManager.Instance.InitializeUnlocks();
        }
    }
    [HarmonyPatch]
    internal class LLLSaveManagerSavePatch {
        private static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("LethalLevelLoader.SaveManager");
            return AccessTools.Method(type, "SaveGameValues");
        }

        private static void Prefix() {
            UnlockManager.Instance.Unlocks.Do(unlock => unlock.RestoreOriginalState());
        }

        private static void Postfix() {
            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                unlock.RefreshCalculatedPrice();
                unlock.ApplyState();
                unlock.ApplyVisibility();
            }
        }
    }
}
