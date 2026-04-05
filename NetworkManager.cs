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
            _terminalReadMessage = LNetworkMessage<TerminalReadSyncData>.Connect("LMU_TerminalReadMessage", onServerReceived: ServerReceiveTerminalRead);
            _requestSyncEvent = LNetworkEvent.Connect("LMU_RequestSyncEvent", onServerReceived: ServerReceiveRequestSyncEvent);

            _alertMessage = LNetworkMessage<Notification>.Connect("LMU_AlertMessage", onClientReceived: ClientReceiveAlertMessage);
            _sendAlertQueueEvent = LNetworkEvent.Connect("LMU_SendAlertQueueEvent", onClientReceived: ClientReceiveSendAlertQueueEvent);

            Logger.LogInfo($"NetworkManager created.");
        }
        public static NetworkManager Instance { get; private set; }

        private static LNetworkMessage<UnlockSyncData> _unlockablesMessage;
        private static LNetworkMessage<string> _buyMoonMessage;
        private static LNetworkMessage<ConstellationRouteSyncData> _routeConstellationMessage;
        private static LNetworkMessage<TerminalReadSyncData> _terminalReadMessage;
        private static LNetworkEvent _requestSyncEvent;

        private static LNetworkMessage<Notification> _alertMessage;
        private static LNetworkEvent _sendAlertQueueEvent;

        public bool IsServer() {
            return Unity.Netcode.NetworkManager.Singleton.IsServer;
        }
        internal void ServerSendUnlockables(List<LMUnlockable> unlockables, ulong client_id = 0) {
            if (!IsServer()) return;
            UnlockSyncData payload = new(BuildUnlockableSyncData(unlockables), BuildConstellationSyncData());
            int unlockableCount = payload.Unlockables?.Count ?? -1;
            int constellationCount = payload.LethalConstellationsSyncData?.constellations?.Count ?? -1;
            if (client_id > 0) {
                Logger.LogInfo($"Syncing unlockables to client with id {client_id} (Unlockables={unlockableCount}, Constellations={constellationCount})");
                _unlockablesMessage.SendClient(payload, client_id);
            } else {
                Logger.LogInfo($"Syncing unlockables to all clients (Unlockables={unlockableCount}, Constellations={constellationCount})..");
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
            UnlockManager.Instance?.ApplyLocalClientMoonPurchasePreview(moon);
            Logger.LogInfo($"Sending buy message to host..");
            _buyMoonMessage.SendServer(moon);
        }
        internal void ClientRouteConstellation(string constellationName, int chargedPrice) {
            if (IsServer()) return;
            Logger.LogInfo($"Sending constellation route message to host for {constellationName} at price {chargedPrice}..");
            _routeConstellationMessage.SendServer(new ConstellationRouteSyncData(constellationName, chargedPrice));
        }
        internal void ClientReportTerminalRead(TerminalReadSyncData terminalReadData) {
            if (IsServer()) return;
            if (terminalReadData == null || string.IsNullOrWhiteSpace(terminalReadData.entryName)) {
                Logger.LogDebug("Skipping terminal-read report with blank payload.");
                return;
            }

            Logger.LogInfo($"Sending terminal-read message to host for {terminalReadData.readKind}: '{terminalReadData.entryName}'.");
            _terminalReadMessage.SendServer(terminalReadData);
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
                Logger.LogInfo($"LMU sync payload status: UnlockablesNull={payload.Unlockables == null}, UnlockableCount={(payload.Unlockables?.Count ?? -1)}, ConstellationsNull={payload.LethalConstellationsSyncData == null}, ConstellationCount={(payload.LethalConstellationsSyncData?.constellations?.Count ?? -1)}");
                if (payload.Unlockables == null) {
                    Logger.LogWarning("Received LMU unlock sync with a null unlock list. Skipping import.");
                } else {
                    UnlockManager.Instance.ImportUnlockableSyncData(payload.Unlockables);
                }
            }
            ApplyConstellationSyncData(payload.LethalConstellationsSyncData);
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
            if (routeData == null || string.IsNullOrWhiteSpace(routeData.constellationName)) {
                Logger.LogWarning($"Received invalid constellation route message from client with id {id}.");
                return;
            }

            if (Plugin.ConstellationManager == null || !Plugin.ConstellationManager.TryGetConstellationEconomyTarget(routeData.constellationName, out var target) || target == null) {
                Logger.LogError($"Received constellation route message for '{routeData.constellationName}' from client with id {id}, but the host could not resolve route pricing. Ignoring client-supplied price {routeData.chargedPrice}.");
                return;
            }

            Logger.LogInfo($"Received constellation route message for {routeData.constellationName} from client with id {id}. Using host-side route price {target.EffectivePrice} instead of client-supplied price {routeData.chargedPrice}.");
            Plugin.LethalConstellationsExtension?.HandleConstellationRoute(routeData.constellationName, target.EffectivePrice);
        }
        private void ServerReceiveTerminalRead(TerminalReadSyncData terminalReadData, ulong id) {
            if (!IsServer()) return;
            if (terminalReadData == null || string.IsNullOrWhiteSpace(terminalReadData.entryName) || ProgressionManager.Instance == null) {
                Logger.LogWarning($"Received invalid terminal-read message from client with id {id}.");
                return;
            }

            bool added = terminalReadData.readKind switch {
                TerminalReadKind.Bestiary => ProgressionManager.Instance.RecordBestiaryRead(terminalReadData.entryName),
                TerminalReadKind.StoryLog => ProgressionManager.Instance.RecordStoryLogRead(terminalReadData.entryName),
                _ => false
            };

            if (!added) {
                Logger.LogDebug($"Ignoring duplicate terminal-read message from client with id {id} for {terminalReadData.readKind}: '{terminalReadData.entryName}'.");
                return;
            }

            Logger.LogInfo($"Recorded terminal-read message from client with id {id} for {terminalReadData.readKind}: '{terminalReadData.entryName}'.");
            UnlockManager.Instance?.HandleRecordedTerminalRead(terminalReadData.readKind, terminalReadData.entryName);
        }
        private void ServerReceiveRequestSyncEvent(ulong client_id) {
            Logger.LogInfo($"Received sync request from client with id {client_id}..");
            ServerSendUnlockables(UnlockManager.Instance.Unlocks, client_id);
        }

        private static List<LMUnlockableSyncData> BuildUnlockableSyncData(List<LMUnlockable> unlockables) {
            return unlockables == null
                ? new List<LMUnlockableSyncData>()
                : unlockables.Where(unlock => unlock != null).Select(unlock => unlock.BuildSyncData()).ToList();
        }

        private static LethalConstellationsSyncData BuildConstellationSyncData() {
            var syncData = new LethalConstellationsSyncData();
            if (!Plugin.LethalConstellationsPresent || Plugin.LethalConstellationsExtension == null) {
                return syncData;
            }

            LethalConstellationsSaveData saveData = Plugin.LethalConstellationsExtension.GetSaveData();
            saveData.ConstellationRotationMoons = Plugin.ConstellationManager != null
                ? Plugin.ConstellationManager.GetAllConstellationRotations()
                : new Dictionary<string, List<string>>();
            saveData.LocalConstellationDiscoveries = Plugin.ConstellationManager != null
                ? Plugin.ConstellationManager.GetAllLocalMoonDiscoveries()
                : new Dictionary<string, List<string>>();
            return LethalConstellationsSyncData.FromSaveData(saveData);
        }
        private static void ApplyConstellationSyncData(LethalConstellationsSyncData syncData) {
            if (UnlockManager.Instance == null || !Plugin.LethalConstellationsPresent || Plugin.LethalConstellationsExtension == null || syncData == null) {
                return;
            }

            LethalConstellationsSaveData saveData = syncData.ToSaveData();

            Plugin.LethalConstellationsExtension.LoadSaveData(saveData);
            Plugin.ConstellationManager?.ReplaceLocalMoonDiscoveries(saveData.LocalConstellationDiscoveries);
            Plugin.ConstellationManager?.ReplaceAllConstellationRotations(saveData.ConstellationRotationMoons);
        }
    }
}
