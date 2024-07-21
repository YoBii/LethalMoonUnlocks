using HarmonyLib;
using LethalLevelLoader;
using LethalNetworkAPI;
using System.Collections.Generic;
using System.Linq;

namespace LethalMoonUnlocks {
    public class UnlockManager {
        public static UnlockManager Instance { get; private set; }
        public Dictionary<string, int> UnlockedMoons { get; private set; } = new Dictionary<string, int>();
        public Dictionary<string, int> OriginalPrices { get; set; } = new Dictionary<string, int>();
        public int QuotaCount { get; set; } = 0;

        private static LNetworkMessage<Dictionary<string, int>> UnlockedMoonsMessage;
        private static LNetworkMessage<Dictionary<string, int>> OriginalPricesMessage;
        private static LNetworkMessage<KeyValuePair<string, int>> BuyMoonMessage;
        //private static LNetworkMessage<string> BuyMoonMessage;
        private static LNetworkEvent RequestSyncEvent;
        private static LNetworkEvent ResetMoonsEvent;

        public UnlockManager() {
            if (Instance == null) {
                Instance = this;
            }

            UnlockedMoonsMessage = LNetworkMessage<Dictionary<string, int>>.Create("LMU_UnlockedMoonsMessage", onClientReceived: ClientReceiveUnlockedMoonsMessage);
            OriginalPricesMessage = LNetworkMessage<Dictionary<string, int>>.Create("LMU_OriginalPricesMessage", onClientReceived: ClientReceiveOriginalPricesMessage);
            BuyMoonMessage = LNetworkMessage<KeyValuePair<string, int>>.Create("LMU_BuyMoonMessage", onServerReceived: ServerReceiveBuyMoonMessage);
            //BuyMoonMessage = LNetworkMessage<string>.Create("LMU_BuyMoonMessage", onServerReceived: ServerReceiveBuyMoonMessage);
            ResetMoonsEvent = LNetworkEvent.Create("LMU_ResetMoonsEvent", onClientReceived: ClientReceiveResetMoonsEvent);
            RequestSyncEvent = LNetworkEvent.Create("LMU_RequestSyncEvent", onServerReceived: ServerReceiveRequestSyncEvent);

            Plugin.Instance.Mls.LogInfo($"UnlockManager created.");
        }

        public void Reset() {
            if (Instance == null) {
                return;
            }
            Plugin.Instance.Mls.LogInfo($"Resetting..");
            UnlockedMoons.Clear();
            OriginalPrices.Clear();
            QuotaCount = 0;
        }

        public List<ExtendedLevel> GetLevels() {
            if (AllExtendedLevels == null || AllExtendedLevels.Count == 0) {
                AllExtendedLevels = PatchedContent.ExtendedLevels;
                Plugin.Instance.Mls.LogInfo($"Found LLL levels: {string.Join(", ", AllExtendedLevels.Select(level => level.NumberlessPlanetName))}");
            }
            return AllExtendedLevels;
        }

        public void BackupOriginalPrices() {
            Plugin.Instance.Mls.LogInfo($"Storing original prices..");
            OriginalPrices.Clear();
            foreach (var level in GetLevels()) {
                OriginalPrices.TryAdd(level.NumberlessPlanetName, level.RoutePrice);
            }
        }

        public void ApplyDataFromSavefile() {
            Dictionary<string, object> savedata = SaveManager.Load();
            if (savedata.ContainsKey("LMU_UnlockedMoons"))
                UnlockedMoons = (Dictionary<string, int>)savedata["LMU_UnlockedMoons"];
            if (savedata.ContainsKey("LMU_OriginalMoonPrices"))
                OriginalPrices = (Dictionary<string, int>)savedata["LMU_OriginalMoonPrices"];
            if (savedata.ContainsKey("LMU_QuotaCount"))
                QuotaCount = (int)savedata["LMU_QuotaCount"];
        }

        public void ApplyPrices() {
            if (Plugin.DiscountMode) Plugin.Instance.Mls.LogInfo($"Applying discounts..");
            else Plugin.Instance.Mls.LogInfo($"Applying unlocks..");
            if (UnlockedMoons.Count == 0) {
                if (Plugin.DiscountMode) Plugin.Instance.Mls.LogInfo($"No discounts to apply");
                else Plugin.Instance.Mls.LogInfo($"No discounts to apply");
                return;
            }
            //Plugin.Instance.Mls.LogInfo($"Bought moons: {string.Join(", ", unlockedMoons)}");
            List<ExtendedLevel> extendedLevels = PatchedContent.ExtendedLevels;
            Plugin.Instance.Mls.LogInfo($"Found levels: [{string.Join(", ", extendedLevels.Select(level => level.NumberlessPlanetName))}]");
            if (Plugin.DiscountMode)
                Plugin.Instance.Mls.LogInfo($"Discount rates (% off): {string.Join(", ", Plugin.Discounts.Split(",").Select(discount => discount + "%"))}");
            foreach (var unlock in unlockedMoons) {
                foreach (var level in extendedLevels) {
                    if (level != null && level.NumberlessPlanetName == unlock.Key) {
                        if (Plugin.DiscountMode) {
                            level.RoutePrice = (int)(OriginalPrices.GetValueSafe(level.NumberlessPlanetName) * Plugin.GetDiscountRate(unlock.Value));
                        } else {
                            level.RoutePrice = 0;
                        }
                        Plugin.Instance.Mls.LogInfo($"Applying price ({level.RoutePrice}) to: {level.NumberlessPlanetName}");
                        break;
                    }
                }
            }
        }

        public void RestorePrices() {
            Plugin.Instance.Mls.LogInfo($"Restoring original prices..");
            if (OriginalPrices.Count == 0) {
                Plugin.Instance.Mls.LogInfo($"No prices to restore");
                return;
            }
            //Plugin.Instance.Mls.LogInfo($"Original Prices: {string.Join(", ", originalPrices)}");
            List<ExtendedLevel> extendedLevels = PatchedContent.ExtendedLevels;
            Plugin.Instance.Mls.LogInfo($"Found levels: [{string.Join(", ", extendedLevels.Select(level => level.NumberlessPlanetName))}]");
            foreach (var kv in originalPrices) {
                foreach (var level in extendedLevels) {
                    if (level != null && level.NumberlessPlanetName == kv.Key) {
                        //Plugin.Instance.Mls.LogInfo($"Restoring price of {level.NumberlessPlanetName} to: {kv.Value}");
                        level.RoutePrice = kv.Value;
                        break;
                    }
                }
            }
        }

        public void ServerUnlockMoon(string moon) {
            if (!Plugin.Instance.IsServer()) return;
            if (!UnlockedMoons.ContainsKey(moon)) {
                Plugin.Instance.Mls.LogInfo($"New moon bought: {moon}");
                UnlockedMoons.TryAdd(moon, 1);
            } else {
                Plugin.Instance.Mls.LogInfo($"Adding visit to moon: {moon}");
                UnlockedMoons[moon]++;
            }
            ServerSyncOriginalPrices();
            ServerSyncUnlockedMoons();
        }

        public void ServerUnlockMoonNewQuota() {
            if (Plugin.QuotaUnlocks) {
                var extendedLevels = PatchedContent.ExtendedLevels.Where(level => level.RoutePrice > 0 && level.IsRouteHidden == false && level.IsRouteLocked == false).ToList();
                if (Plugin.QuotaUnlockMaxCount > 0) {
                    if (QuotaCount > Plugin.QuotaUnlockMaxCount) {
                        Plugin.Instance.Mls.LogInfo($"New quota random unlock limit ({Plugin.QuotaUnlockMaxCount}) reached! No more unlocks.");
                        return;
                    }
                }
                var candidateLevels = GetLevels().Where(level => level.RoutePrice > 0 && level.IsRouteHidden == false && level.IsRouteLocked == false).ToList();
                if (Plugin.QuotaUnlocksMaxPrice > 0) {
                    extendedLevels.RemoveAll(level => level.RoutePrice > Plugin.QuotaUnlocksMaxPrice);
                }
                if (extendedLevels.Count == 0) {
                    Plugin.Instance.Mls.LogInfo($"New quota random unlock limit reached! No more unlocks.");
                    return;
                }
                var randomLevel = extendedLevels[UnityEngine.Random.Range(0, extendedLevels.Count)];
                if (HUDManager.Instance != null) {
                    if (Plugin.DiscountMode) {
                        Plugin.Instance.Mls.LogInfo($"New random discount unlocked for moon: {randomLevel.NumberlessPlanetName}");
                        HUDManager.Instance.AddTextToChatOnServer($"New discount unlocked:\n<color=green> {randomLevel.NumberlessPlanetName}</color>");
                    } else {
                        Plugin.Instance.Mls.LogInfo($"New random moon unlocked: {randomLevel.NumberlessPlanetName}");
                        HUDManager.Instance.AddTextToChatOnServer($"New moon unlocked:\n<color=green>{randomLevel.NumberlessPlanetName}</color>");
                    }
                if (HUDManager.Instance != null) {
                    if (Plugin.DiscountMode)
                        HUDManager.Instance.AddTextToChatOnServer($"<color=white>New moon discount unlocked:</color>\n<color=green> {randomLevel.NumberlessPlanetName}</color>");
                    else
                        HUDManager.Instance.AddTextToChatOnServer($"<color=white>New moon unlocked:</color>\n<color=green>{randomLevel.NumberlessPlanetName}</color>");
                } else {
                    Plugin.Instance.Mls.LogWarning($"HUDManager not found!");
                }
                ServerUnlockMoon(randomLevel.NumberlessPlanetName);
            }
        }

        public void ClientUnlockMoon(string moon) {
            if (Plugin.Instance.IsServer()) return;
            Plugin.Instance.Mls.LogInfo($"Sending buy message to server..");
            BuyMoonMessage.SendServer(new KeyValuePair<string, int>(moon, 0));
        }

        public void ServerSyncUnlockedMoons(ulong client_id = 0) {
            if (!Plugin.Instance.IsServer()) return;
            if (client_id > 0) {
                Plugin.Instance.Mls.LogInfo($"Syncing bought moons to client with id {client_id}");
            } else {
                Plugin.Instance.Mls.LogInfo($"Syncing bought moons to all clients..");
            }
            if (client_id > 0) {
                UnlockedMoonsMessage.SendClient(UnlockedMoons, client_id);
            } else {
                UnlockedMoonsMessage.SendClients(UnlockedMoons);
            }
            }
        
        public void ServerSyncOriginalPrices(ulong client_id = 0) {
            if (!Plugin.Instance.IsServer()) return;
            if (client_id > 0) {
                Plugin.Instance.Mls.LogInfo($"Syncing original prices to client with id {client_id}");
            } else {
                Plugin.Instance.Mls.LogInfo($"Syncing original prices to all clients..");
            }
            Plugin.Instance.Mls.LogInfo($"Sending bought moons: {string.Join(", ", UnlockedMoons)}");
            if (client_id > 0) {
                OriginalPricesMessage.SendClient(OriginalPrices, client_id);
            } else {
                OriginalPricesMessage.SendClients(OriginalPrices);
            }
        }
        public void ServerSendResetMoonsEvent() {
            if (!Plugin.Instance.IsServer()) return;
            Plugin.Instance.Mls.LogInfo($"Sending reset bought moons event to all clients..");
            OriginalPricesMessage.SendClients(OriginalPrices);
            ResetMoonsEvent.InvokeClients();
        }

        public void ClientRequestSync() {
            Plugin.Instance.Mls.LogInfo($"Requesting sync from host..");
            RequestSyncEvent.InvokeServer();
        }

        private void ServerReceiveBuyMoonMessage(KeyValuePair<string, int> payload, ulong id) {
            string moon = payload.Key;
            int originalPrice = payload.Value;
            Plugin.Instance.Mls.LogInfo($"Received buy message from client with id {id}.");
            ServerUnlockMoon(payload.Key);
        }

        private void ClientReceiveOriginalPricesMessage(Dictionary<string, int> payload) {
            Plugin.Instance.Mls.LogInfo($"Received original prices: {string.Join(", ", payload)}");
            OriginalPrices = new Dictionary<string, int>(payload);
            RestorePrices();
        }

        private void ClientReceiveUnlockedMoonsMessage(Dictionary<string, int> payload) {
            Plugin.Instance.Mls.LogInfo($"Received bought moons: {string.Join(", ", payload)}");
            UnlockedMoons = new Dictionary<string, int>(payload);
            ApplyPrices();
        }

        private void ServerReceiveRequestSyncEvent(ulong client_id) {
            Plugin.Instance.Mls.LogInfo($"Received sync request from client with id {client_id}..");
            ServerSyncOriginalPrices(client_id);
            ServerSyncUnlockedMoons(client_id);
        }

        private void ClientReceiveResetMoonsEvent() {
            Plugin.Instance.Mls.LogInfo($"Received reset event..");
            Reset();
        }
    }
}
