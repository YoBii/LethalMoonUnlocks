using HarmonyLib;
using LethalLevelLoader;
using LethalNetworkAPI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace LethalMoonUnlocks {
    internal class NetworkManager {
        public NetworkManager() {
            if (Instance == null) {
                Instance = this;
            }

            UnlockablesMessage = LNetworkMessage<List<LMUnlockable>>.Connect("LMU_Unlocks", onClientReceived: ClientReceiveUnlockables);
            BuyMoonMessage = LNetworkMessage<string>.Connect("LMU_BuyMoonMessage", onServerReceived: ServerReceiveBuyMoon);
            RequestSyncEvent = LNetworkEvent.Connect("LMU_RequestSyncEvent", onServerReceived: ServerReceiveRequestSyncEvent);

            Plugin.Instance.Mls.LogInfo($"UnlockManager created.");
        }
        public static NetworkManager Instance { get; private set; }

        private static LNetworkMessage<List<LMUnlockable>> UnlockablesMessage;
        private static LNetworkMessage<string> BuyMoonMessage;
        private static LNetworkEvent RequestSyncEvent;

        public bool IsServer() {
            return Unity.Netcode.NetworkManager.Singleton.IsServer;
        }
        public void ServerSendUnlockables(List<LMUnlockable> unlockables, ulong client_id = 0) {
            if (!IsServer()) return;
            if (client_id > 0) {
                Plugin.Instance.Mls.LogInfo($"Syncing unlockables to client with id {client_id}");
            } else {
                Plugin.Instance.Mls.LogInfo($"Syncing unlockables to all clients..");
            }
            if (client_id > 0) {
                UnlockablesMessage.SendClient(unlockables, client_id);
            } else {
                UnlockablesMessage.SendClients(unlockables);
            }
        }
        public void ClientBuyMoon(string moon) {
            if (IsServer()) return;
            Plugin.Instance.Mls.LogInfo($"Sending buy message to host..");
            BuyMoonMessage.SendServer(moon);
        }
        public void ClientRequestSync() {
            if (IsServer()) return;
            Plugin.Instance.Mls.LogInfo($"Requesting sync from host..");
            RequestSyncEvent.InvokeServer();
        }

        private void ClientReceiveUnlockables(List<LMUnlockable> payload) {
            if (!IsServer()) {
                Plugin.Instance.Mls.LogInfo($"Receiving LMU data..");
                UnlockManager.Instance.ImportUnlockableData(payload);        
            }
            UnlockManager.Instance.ApplyUnlocks();
        }
        private void ServerReceiveBuyMoon(string moon, ulong id) {
            if (!IsServer()) return;
            Plugin.Instance.Mls.LogInfo($"Received buy message for moon {moon} from client with id {id}.");
            UnlockManager.Instance.BuyMoon(moon);
        }
        private void ServerReceiveRequestSyncEvent(ulong client_id) {
            Plugin.Instance.Mls.LogInfo($"Received sync request from client with id {client_id}..");
            ServerSendUnlockables(UnlockManager.Instance.Unlocks, client_id);
        }
    }
}
