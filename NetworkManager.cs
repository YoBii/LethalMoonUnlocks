using HarmonyLib;
using LethalLevelLoader;
using LethalMoonUnlocks.Compatibility;
using LethalMoonUnlocks.Util;
using LethalNetworkAPI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace LethalMoonUnlocks {
    public class NetworkManager {
        internal NetworkManager() {
            if (Instance == null) {
                Instance = this;
            }

            Logger.LogInfo("Register Network messages..");

            _unlockablesMessage = LNetworkMessage<UnlockSyncData>.Connect("LMU_Unlocks", onClientReceived: ClientReceiveUnlockables);
            _buyMoonMessage = LNetworkMessage<string>.Connect("LMU_BuyMoonMessage", onServerReceived: ServerReceiveBuyMoon);
            _routeConstellationMessage = LNetworkMessage<ConstellationRouteSyncData>.Connect("LMU_RouteConstellationMessage", onServerReceived: ServerReceiveRouteConstellation);
            _requestSyncEvent = LNetworkEvent.Connect("LMU_RequestSyncEvent", onServerReceived: ServerReceiveRequestSyncEvent);

            _alertMessage = LNetworkMessage<Notification>.Connect("LMU_AlertMessage", onClientReceived: ClientReceiveAlertMessage);
            _sendAlertQueueEvent = LNetworkEvent.Connect("LMU_SendAlertQueueEvent", onClientReceived: ClientReceiveSendAlertQueueEvent);

            Logger.LogInfo($"NetworkManager created.");
        }
        public static NetworkManager Instance { get; private set; }

        private static LNetworkMessage<UnlockSyncData> _unlockablesMessage;
        private static LNetworkMessage<string> _buyMoonMessage;
        private static LNetworkMessage<ConstellationRouteSyncData> _routeConstellationMessage;
        private static LNetworkEvent _requestSyncEvent;

        private static LNetworkMessage<Notification> _alertMessage;
        private static LNetworkEvent _sendAlertQueueEvent;

        public bool IsServer() {
            return Unity.Netcode.NetworkManager.Singleton.IsServer;
        }
        internal void ServerSendUnlockables(List<LMUnlockable> unlockables, ulong client_id = 0) {
            if (!IsServer()) return;
            UnlockSyncData payload = new(unlockables, BuildConstellationSyncData());
            if (client_id > 0) {
                Logger.LogInfo($"Syncing unlockables to client with id {client_id}");
                _unlockablesMessage.SendClient(payload, client_id);
            } else {
                Logger.LogInfo($"Syncing unlockables to all clients..");
                _unlockablesMessage.SendClients(payload);
            }
        }
        public void ServerSendAlertMessage(Notification alert) {
            if (!IsServer()) return;
            _alertMessage.SendClients(alert);
        }
        internal void ServerSendAlertQueueEvent() {
            if (!IsServer()) return;
            _sendAlertQueueEvent.InvokeClients();
        }
        public void ClientBuyMoon(string moon) {
            if (IsServer()) return;
            Logger.LogInfo($"Sending buy message to host..");
            _buyMoonMessage.SendServer(moon);
        }
        internal void ClientRouteConstellation(string constellationName, int chargedPrice) {
            if (IsServer()) return;
            Logger.LogInfo($"Sending constellation route message to host for {constellationName} at price {chargedPrice}..");
            _routeConstellationMessage.SendServer(new ConstellationRouteSyncData(constellationName, chargedPrice));
        }
        internal void ClientRequestSync() {
            if (IsServer()) return;
            Logger.LogInfo($"Requesting sync from host..");
            _requestSyncEvent.InvokeServer();
        }

        private void ClientReceiveUnlockables(UnlockSyncData payload) {
            if (payload == null) {
                return;
            }

            if (!IsServer()) {
                Logger.LogInfo($"Receiving LMU data..");
                UnlockManager.Instance.ImportUnlockableData(payload.Unlockables);
            }
            ApplyConstellationSyncData(payload.LethalConstellationsSaveData);
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
        private void ServerReceiveRouteConstellation(ConstellationRouteSyncData routeData, ulong id) {
            if (!IsServer()) return;
            if (routeData == null || string.IsNullOrWhiteSpace(routeData.ConstellationName)) {
                Logger.LogWarning($"Received invalid constellation route message from client with id {id}.");
                return;
            }

            if (Plugin.ConstellationManager == null || !Plugin.ConstellationManager.TryGetConstellationEconomyTarget(routeData.ConstellationName, out var target) || target == null) {
                Logger.LogError($"Received constellation route message for '{routeData.ConstellationName}' from client with id {id}, but the host could not resolve route pricing. Ignoring client-supplied price {routeData.ChargedPrice}.");
                return;
            }

            Logger.LogInfo($"Received constellation route message for {routeData.ConstellationName} from client with id {id}. Using host-side route price {target.EffectivePrice} instead of client-supplied price {routeData.ChargedPrice}.");
            Plugin.LethalConstellationsExtension?.HandleConstellationRoute(routeData.ConstellationName, target.EffectivePrice);
        }
        private void ServerReceiveRequestSyncEvent(ulong client_id) {
            Logger.LogInfo($"Received sync request from client with id {client_id}..");
            ServerSendUnlockables(UnlockManager.Instance.Unlocks, client_id);
        }

        private static LethalConstellationsSaveData BuildConstellationSyncData() {
            var syncData = new LethalConstellationsSaveData();
            if (!Plugin.LethalConstellationsPresent || Plugin.LethalConstellationsExtension == null) {
                return syncData;
            }

            syncData = Plugin.LethalConstellationsExtension.GetSaveData();
            syncData.ConstellationRotationMoons = Plugin.ConstellationManager != null
                ? Plugin.ConstellationManager.GetAllConstellationRotations()
                : new Dictionary<string, List<string>>();
            syncData.LocalConstellationDiscoveries = Plugin.ConstellationManager != null
                ? Plugin.ConstellationManager.GetAllLocalMoonDiscoveries()
                : new Dictionary<string, List<string>>();
            return syncData;
        }

        private static void ApplyConstellationSyncData(LethalConstellationsSaveData syncData) {
            if (UnlockManager.Instance == null || !Plugin.LethalConstellationsPresent || Plugin.LethalConstellationsExtension == null || syncData == null) {
                return;
            }

            Plugin.LethalConstellationsExtension.LoadSaveData(syncData);
            Plugin.ConstellationManager?.ReplaceLocalMoonDiscoveries(syncData.LocalConstellationDiscoveries);
            Plugin.ConstellationManager?.ReplaceAllConstellationRotations(syncData.ConstellationRotationMoons);
        }
    }
}
