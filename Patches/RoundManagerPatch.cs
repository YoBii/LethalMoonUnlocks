using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch {
        [HarmonyPatch(nameof(RoundManager.LoadNewLevel))]
        [HarmonyPostfix]
        private static void LoadNewLevelPatch(ref SelectableLevel newLevel) {
            Plugin.Instance.Mls.LogInfo($"Landing on moon {newLevel.PlanetName} with id {newLevel.levelID}");
            UnlockManager.Instance.OnLanding(newLevel);
        }
    }
}
