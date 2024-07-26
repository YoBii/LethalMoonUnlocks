using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch {
        [HarmonyPatch(nameof(RoundManager.LoadNewLevel))]
        [HarmonyPostfix]
        private static void LoadNewLevelPatch(ref SelectableLevel level) {
            Plugin.Instance.Mls.LogInfo($"Visiting moon {level.PlanetName} with id {level.levelID}");
        }
    }
}
