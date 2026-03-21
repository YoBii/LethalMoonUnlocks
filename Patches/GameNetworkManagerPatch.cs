using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch {
        [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        private static void DisconnectPatch() {
            if (GameNetworkManager.Instance.currentLobby != null) {
                Logger.LogInfo($"Disconnecting from lobby. Restoring original prices and clearing variables..");
                UnlockManager.Instance.OnDisconnect();
            }
        }

        [HarmonyPatch(nameof(GameNetworkManager.SaveGame))]
        [HarmonyPostfix]
        private static void SaveGameValuesPatch() {
            if (!NetworkManager.Instance.IsServer()) return;
            try {
                Logger.LogInfo($"Host is saving game..");
                SaveManager.StoreSaveData();
            } catch (Exception e) {
                Logger.LogError($"Failed to save unlock data: {e}");
            }
        }

        [HarmonyPatch(nameof(GameNetworkManager.ResetSavedGameValues))]
        [HarmonyPostfix]
        private static void ResetSavedGameValuesPatch() {
            Logger.LogInfo($"You are fired!");
            if (NetworkManager.Instance.IsServer() && ConfigManager.ResetWhenFired) {
                UnlockManager.Instance.OnResetGame();
            }
        }
    }
}
