using HarmonyLib;
using LethalLevelLoader;
using LethalMoonUnlocks.Util;
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

            Plugin.Instance.Mls.LogInfo("Register Network messages..");

            UnlockablesMessage = LNetworkMessage<List<LMUnlockable>>.Connect("LMU_Unlocks", onClientReceived: ClientReceiveUnlockables);
            BuyMoonMessage = LNetworkMessage<string>.Connect("LMU_BuyMoonMessage", onServerReceived: ServerReceiveBuyMoon);
            RequestSyncEvent = LNetworkEvent.Connect("LMU_RequestSyncEvent", onServerReceived: ServerReceiveRequestSyncEvent);

            AlertMessage = LNetworkMessage<Notification>.Connect("LMU_AlertMessage", onClientReceived: ClientReceiveAlertMessage);
            SendAlertQueueEvent = LNetworkEvent.Connect("LMU_SendAlertQueueEvent", onClientReceived: ClientReceiveSendAlertQueueEvent);

            Plugin.Instance.Mls.LogInfo($"NetworkManager created.");
        }
        public static NetworkManager Instance { get; private set; }

        private static LNetworkMessage<List<LMUnlockable>> UnlockablesMessage;
        private static LNetworkMessage<string> BuyMoonMessage;
        private static LNetworkEvent RequestSyncEvent;

        private static LNetworkMessage<Notification> AlertMessage;
        private static LNetworkEvent SendAlertQueueEvent;

        internal bool IsServer() {
            return Unity.Netcode.NetworkManager.Singleton.IsServer;
        }
        internal void ServerSendUnlockables(List<LMUnlockable> unlockables, ulong client_id = 0) {
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
        internal void ServerSendAlertMessage(Notification alert) {
            if (!IsServer()) return;
            AlertMessage.SendClients(alert);
        }
        internal void ServerSendAlertQueueEvent() {
            if (!IsServer()) return;
            SendAlertQueueEvent.InvokeClients();
        }
        internal void ClientBuyMoon(string moon) {
            if (IsServer()) return;
            Plugin.Instance.Mls.LogInfo($"Sending buy message to host..");
            BuyMoonMessage.SendServer(moon);
        }
        internal void ClientRequestSync() {
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
        private void ClientReceiveAlertMessage(Notification alert) {
            if (!ConfigManager.ShowAlerts) return;
            Plugin.Instance.Mls.LogDebug($"Receiving alert message..");
            NotificationHelper.AddNotificationToQueue(alert);
        }
        private void ClientReceiveSendAlertQueueEvent() {
            if (!ConfigManager.ShowAlerts) return;
            DelayHelper.Instance.StartCoroutine(NotificationHelper.SendQueuedNotifications());
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
