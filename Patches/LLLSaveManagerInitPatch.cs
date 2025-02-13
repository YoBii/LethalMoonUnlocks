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
}
