using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch {
        [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        private static void DisconnectPatch() {
            Logger.LogInfo($"Disconnecting from lobby. Restoring original prices and clearing variables..");
            UnlockManager.Instance.OnDisconnect();
        }

        [HarmonyPatch(nameof(GameNetworkManager.SaveGame))]
        [HarmonyPostfix]
        private static void SaveGameValuesPatch() {
            if (!NetworkManager.Instance.IsServer()) return;
            var inShipPhase = StartOfRound.Instance != null && StartOfRound.Instance.inShipPhase;
            var allowMidRoundSave = ConfigManager.GroupCreditsSavingBandAid;
            Logger.LogDebug($"inShipPhase: {inShipPhase}");

            if (!inShipPhase && !allowMidRoundSave) {
                Logger.LogInfo("Skipping LMU save because the game is not in ship phase. Mid-round LMU progression will roll back on load.");
                return;
            }
            try {
                Logger.LogInfo($"Host is saving game..");
                if (!inShipPhase) {
                    Logger.LogWarning("Saving LMU data outside ship phase because the legacy compatibility band-aid is enabled.");
                }
                SaveManager.StoreSaveData();
            } catch (Exception e) {
                Logger.LogError($"Failed to save unlock data: {e}");
            }
        }

        [HarmonyPatch(nameof(GameNetworkManager.ResetSavedGameValues))]
        [HarmonyPostfix]
        private static void ResetSavedGameValuesPatch() {
            Logger.LogInfo($"You are fired!");
            if (NetworkManager.Instance.IsServer() && ConfigManager.ResetWhenFired != ResetWhenFiredBehavior.Nothing) {
                UnlockManager.Instance.OnResetGame();
            }
        }
    }
}
