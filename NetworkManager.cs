using HarmonyLib;
using LethalLevelLoader;
using LethalMoonUnlocks.Util;
using LethalNetworkAPI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace LethalMoonUnlocks {
    internal class NetworkManager {
        internal NetworkManager() {
            if (Instance == null) {
                Instance = this;
            }

            Logger.LogInfo("Register Network messages..");

            UnlockablesMessage = LNetworkMessage<List<LMUnlockable>>.Connect("LMU_Unlocks", onClientReceived: ClientReceiveUnlockables);
            BuyMoonMessage = LNetworkMessage<string>.Connect("LMU_BuyMoonMessage", onServerReceived: ServerReceiveBuyMoon);
            RequestSyncEvent = LNetworkEvent.Connect("LMU_RequestSyncEvent", onServerReceived: ServerReceiveRequestSyncEvent);

            AlertMessage = LNetworkMessage<Notification>.Connect("LMU_AlertMessage", onClientReceived: ClientReceiveAlertMessage);
            SendAlertQueueEvent = LNetworkEvent.Connect("LMU_SendAlertQueueEvent", onClientReceived: ClientReceiveSendAlertQueueEvent);

            Logger.LogInfo($"NetworkManager created.");
        }
        internal static NetworkManager Instance { get; private set; }

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
                Logger.LogInfo($"Syncing unlockables to client with id {client_id}");
                UnlockablesMessage.SendClient(unlockables, client_id);
            } else {
                Logger.LogInfo($"Syncing unlockables to all clients..");
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
            Logger.LogInfo($"Sending buy message to host..");
            BuyMoonMessage.SendServer(moon);
        }
        internal void ClientRequestSync() {
            if (IsServer()) return;
            Logger.LogInfo($"Requesting sync from host..");
            RequestSyncEvent.InvokeServer();
        }

        private void ClientReceiveUnlockables(List<LMUnlockable> payload) {
            if (!IsServer()) {
                Logger.LogInfo($"Receiving LMU data..");
                UnlockManager.Instance.ImportUnlockableData(payload);        
            }
            UnlockManager.Instance.ApplyUnlocks();
        }
        private void ClientReceiveAlertMessage(Notification alert) {
            if (!ConfigManager.ShowAlerts) return;
            Logger.LogDebug($"Receiving alert message..");
            NotificationHelper.AddNotificationToQueue(alert);
        }
        private void ClientReceiveSendAlertQueueEvent() {
            if (!ConfigManager.ShowAlerts) return;
            DelayHelper.Instance.StartCoroutine(NotificationHelper.SendQueuedNotifications());
        }
        private void ServerReceiveBuyMoon(string moon, ulong id) {
            if (!IsServer()) return;
            Logger.LogInfo($"Received buy message for moon {moon} from client with id {id}.");
            UnlockManager.Instance.BuyMoon(moon);
        }
        private void ServerReceiveRequestSyncEvent(ulong client_id) {
            Logger.LogInfo($"Received sync request from client with id {client_id}..");
            ServerSendUnlockables(UnlockManager.Instance.Unlocks, client_id);
        }
    }
}
