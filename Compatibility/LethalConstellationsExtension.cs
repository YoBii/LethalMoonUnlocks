using LethalConstellations.PluginCore;
using LethalMoonUnlocks.Util;
using System.Collections.Generic;
using System.Linq;

namespace LethalMoonUnlocks.Compatibility {
    public class LethalConstellationsExtension {
        private readonly LethalConstellationsSaveData _constellationSaveData = new();
        private LethalConstellationsSaveData _pendingSaveData;
        private LethalConstellationsManager _constellationManager = null!;
        private readonly Dictionary<string, int> _pendingConstellationRoutePrices = new(System.StringComparer.OrdinalIgnoreCase);

        public LethalConstellationsExtension() {
            LethalConstellations.EventStuff.NewEvents.RouteConstellationSuccess.AddListener(OnConstellationBought);
        }

        internal void SetManager(LethalConstellationsManager manager) {
            _constellationManager = manager;
        }

        internal IReadOnlyDictionary<string, LMConstellationUnlockable> ConstellationStates => _constellationSaveData.Constellations;

        public void ApplyUnlocks() {
            TryApplyPendingSaveData();

            if (!HasConstellationDefinitions()) {
                Logger.LogWarning("LethalConstellationsExtension: Constellation definitions are unavailable. Skipping unlock application.");
                return;
            }

            if (!_constellationManager.Bootstrap()) {
                Logger.LogError("LethalConstellations bootstrap failed; skipping constellation-local unlock application.");
                return;
            }

            ApplyVisibility();

            if (ConfigManager.DiscoveryMode && UnlockManager.Instance != null && UnlockManager.Instance.UseConstellationDiscovery) {
                _constellationManager.ApplyCurrentConstellationVisibility();
                HideUnlocksNotInCurrentConstellation();
                ShowUnlocksInCurrentConstellation();
            } else {
                HideUnlocksNotInCurrentConstellation();
                ShowUnlocksInCurrentConstellation();
            }
            _constellationManager.ApplyConstellationState();
            AddDiscoveryCount();

            foreach (var constellation in Collections.ConstellationStuff) {
                Logger.LogDebug($"LethalConstellationsExtension: Constellation {constellation.consName}: {constellation.constelMoons.Count} moons, hidden state {constellation.isHidden}, locked state {constellation.isLocked}, default moon {constellation.defaultMoon}, price {constellation.constelPrice}, optional params: {constellation.optionalParams}");
            }
        }

        internal LethalConstellationsSaveData GetSaveData() {
            if (_pendingSaveData != null) {
                return _pendingSaveData.Copy();
            }

            return _constellationSaveData.Copy();
        }

        internal void LoadSaveData(LethalConstellationsSaveData saveData) {
            if (!HasConstellationDefinitions()) {
                _constellationSaveData.Clear();
                _pendingSaveData = saveData?.Copy();
                _constellationManager.SetHasLoadedPersistedState(saveData?.Constellations?.Values.Any(state => state != null && state.HasData()) == true);
                if (saveData != null) {
                    Logger.LogWarning("LethalConstellationsExtension: Constellation definitions are unavailable while trying to load save data. Skipping now. Will attempt again.");
                }
                return;
            }

            ApplySaveData(saveData);
        }

        private void ApplySaveData(LethalConstellationsSaveData saveData, bool refreshDefinitions = true) {
            _pendingSaveData = null;
            _constellationSaveData.Clear();
            SyncConstellationStates();

            if (saveData?.Constellations != null) {
                foreach (var constellation in saveData.Constellations) {
                    if (string.IsNullOrWhiteSpace(constellation.Key) || constellation.Value == null) {
                        Logger.LogWarning("LethalConstellationsExtension: Dropping invalid saved constellation entry (no valid name or payload).");
                        continue;
                    }

                    if (!_constellationSaveData.TryGet(constellation.Key, out var state) || state == null) {
                        Logger.LogError($"LethalConstellationsExtension: Dropping orphaned saved constellation '{constellation.Key}' because no constellation definition with that name exists.");
                        continue;
                    }

                    state.OverrideData(constellation.Value);
                    if (_constellationManager.TryGetConstellation(constellation.Key, out var constellationDefinition)) {
                        state.UpdateFromConstellation(constellationDefinition);
                    }
                }
            }

            _constellationManager.SetHasLoadedPersistedState(_constellationSaveData.Constellations.Values.Any(state => state != null && state.HasData()));
            if (refreshDefinitions) {
                _constellationManager.RefreshDefinitions(false);
            }
        }

        internal void TryApplyPendingSaveData(bool refreshDefinitions = true) {
            if (_pendingSaveData == null || !HasConstellationDefinitions()) {
                return;
            }

            ApplySaveData(_pendingSaveData, refreshDefinitions);
        }

        internal bool TryGetConstellationState(string constellationName, out LMConstellationUnlockable constellationState) {
            return _constellationSaveData.TryGet(constellationName, out constellationState);
        }

        internal LMConstellationUnlockable GetOrCreateConstellationState(string constellationName) {
            return _constellationSaveData.GetOrCreate(constellationName);
        }

        internal void SyncConstellationStates() {
            foreach (var constellation in Collections.ConstellationStuff) {
                if (string.IsNullOrWhiteSpace(constellation.consName)) {
                    continue;
                }

                GetOrCreateConstellationState(constellation.consName)?.UpdateFromConstellation(constellation);
            }
        }

        public string GetConstellationName(LMUnlockable unlock) {
            return _constellationManager.GetConstellationName(unlock);
        }

        private void OnConstellationBought() {
            if (!HasConstellationDefinitions()) {
                Logger.LogError("LethalConstellationsExtension: Route success fired before constellation definitions were available. Something is most likely broken.");
                return;
            }

            if (!TryGetCurrentConstellationDefinition(out var currentConstellation)) {
                return;
            }

            if (!TryConsumePendingConstellationRoute(currentConstellation.consName, out int chargedPrice)) {
                if (!TryResolveFallbackRoutePrice(currentConstellation.consName, out chargedPrice)) {
                    Logger.LogError($"LethalConstellationsExtension: Missing captured route price for constellation '{currentConstellation.consName}' when the route success event fired, and no  fallback price could be resolved either.");
                    return;
                }

                Logger.LogWarning($"LethalConstellationsExtension: Missing captured route price for constellation '{currentConstellation.consName}' when the route success event fired. Falling back to constellation price from definition: {chargedPrice}.");
            }

            if (NetworkManager.Instance.IsServer()) {
                HandleConstellationRoute(currentConstellation.consName, chargedPrice);
            } else {
                NetworkManager.Instance.ClientRouteConstellation(currentConstellation.consName, chargedPrice);
            }
        }

        internal void RecordPendingConstellationRoute(string constellationName, int chargedPrice) {
            if (string.IsNullOrWhiteSpace(constellationName)) {
                return;
            }

            _pendingConstellationRoutePrices[constellationName] = chargedPrice;
        }

        private bool TryConsumePendingConstellationRoute(string constellationName, out int chargedPrice) {
            chargedPrice = 0;
            if (string.IsNullOrWhiteSpace(constellationName) || !_pendingConstellationRoutePrices.TryGetValue(constellationName, out chargedPrice)) {
                return false;
            }

            _pendingConstellationRoutePrices.Remove(constellationName);
            return true;
        }

        private bool TryResolveFallbackRoutePrice(string constellationName, out int chargedPrice) {
            chargedPrice = 0;
            if (string.IsNullOrWhiteSpace(constellationName)) {
                return false;
            }

            if (_constellationManager.TryGetConstellationEconomyTarget(constellationName, out var target) && target != null) {
                chargedPrice = target.EffectivePrice;
                return true;
            }

            var constellation = Collections.ConstellationStuff?.FirstOrDefault(c =>
                string.Equals(c.consName, constellationName, System.StringComparison.OrdinalIgnoreCase));
            if (constellation == null) {
                return false;
            }

            chargedPrice = constellation.constelPrice;
            return true;
        }

        internal void HandleConstellationRoute(string constellationName, int chargedPrice) {
            if (string.IsNullOrWhiteSpace(constellationName) || UnlockManager.Instance == null) {
                return;
            }

            if (!HasConstellationDefinitions()) {
                Logger.LogError($"LethalConstellationsExtension: Unable to handle route for '{constellationName}' because constellation definitions are unavailable.");
                return;
            }

            if (!_constellationManager.TryGetConstellation(constellationName, out var currentConstellation)) {
                Logger.LogError($"LethalConstellationsExtension: Unable to handle route for missing constellation '{constellationName}'.");
                return;
            }

            Collections.CurrentConstellation = currentConstellation.consName;
            Collections.CurrentConstellationCM = currentConstellation;

            bool routeWasPaid = chargedPrice > 0;
            _constellationManager.TryGetDefaultMoon(currentConstellation.consName, out var defaultMoonUnlock);

            if (defaultMoonUnlock != null) {
                Logger.LogInfo($"Routing to constellation {currentConstellation.consName} -> default moon {defaultMoonUnlock.Name} with charged price {chargedPrice} and ID {defaultMoonUnlock.ExtendedLevel.SelectableLevel.levelID}!");
            }

            _constellationManager.HandleConstellationRoute(currentConstellation.consName, chargedPrice);

            if (ConfigManager.LethalConstellationsMirrorDefaultMoonRoute && defaultMoonUnlock != null) {
                Logger.LogInfo($"Mirroring constellation route '{currentConstellation.consName}' onto default moon '{defaultMoonUnlock.Name}'.");
                UnlockManager.Instance.ApplyMoonRouteProgression(defaultMoonUnlock, routeWasPaid, allowTravelDiscovery: true, broadcastState: false);
            }

            UnlockManager.Instance.IterateUnlocks();
            NetworkManager.Instance.ServerSendUnlockables(UnlockManager.Instance.Unlocks);
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 2);
        }

        private void ApplyVisibility() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff) {
                bool constellationIsAvailable = false;
                if (TryGetConstellationState(constellation.consName, out var constellationState)) {
                    constellationIsAvailable = !ConfigManager.DiscoveryMode
                        ? constellationState.StoryIsUnlocked || constellationState.Discovered
                        : constellationState.Discovered;
                }

                if (constellationIsAvailable) {
                    constellation.isHidden = false;
                    constellation.isLocked = false;
                    Logger.LogDebug($"Constellation {constellation.consName} is discovered and routable.");
                } else {
                    constellation.isHidden = true;
                    constellation.isLocked = true;
                    constellation.optionalParams = string.Empty;
                    Logger.LogDebug($"Constellation {constellation.consName} is hidden and locked.");
                }
            }
        }

        private void AddDiscoveryCount() {
            foreach (var constellation in Collections.ConstellationStuff) {
                if (constellation.isHidden) {
                    constellation.optionalParams = string.Empty;
                    continue;
                }

                string additionalInfo = string.Empty;
                if (ConfigManager.DiscoveryMode) {
                    List<LMUnlockable> constellationUnlocks = UnlockManager.Instance != null && UnlockManager.Instance.UseConstellationDiscovery
                        ? _constellationManager.GetVisibleConstellationUnlocks(constellation.consName)
                        : UnlockManager.Instance?.Unlocks.Where(unlock =>
                            (unlock.Discovered || unlock.PermanentlyDiscovered) && _constellationManager.IsMoonInConstellation(unlock, constellation.consName)).ToList();
                    additionalInfo = $"\nMoons discovered: {constellationUnlocks?.Count}";
                    if (constellationUnlocks?.Count == constellation.constelMoons.Count) {
                        additionalInfo = "\nAll moons discovered!";
                    }
                }

                if (TryGetConstellationState(constellation.consName, out var constellationState)) {
                    additionalInfo += constellationState.BuildAdditionalInfoString();
                }

                constellation.optionalParams = additionalInfo;
            }
        }

        private void HideUnlocksNotInCurrentConstellation() {
            if (!TryGetCurrentConstellationDefinition(out var currentConstellation)) return;
            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                if (!_constellationManager.IsMoonInConstellation(unlock, currentConstellation.consName)) {
                    unlock.LockAndHide();
                    unlock.ApplyVisibility();
                }
            }
        }

        private void ShowUnlocksInCurrentConstellation() {
            if (!TryGetCurrentConstellationDefinition(out var currentConstellation)) return;
            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                if (!_constellationManager.IsMoonInConstellation(unlock, currentConstellation.consName)) continue;
                unlock.ApplyState();
                unlock.ApplyVisibility();
            }
        }
        
        internal void Reset() {
            Collections.ConstellationStuff?.Clear();
            _pendingConstellationRoutePrices.Clear();
            _pendingSaveData = null;
            _constellationManager.Reset();
            _constellationSaveData.Clear();
        }

        private static bool HasConstellationDefinitions() {
            return Collections.ConstellationStuff.Count > 0;
        }

        private bool TryGetCurrentConstellationDefinition(out ClassMapper currentConstellation) {
            currentConstellation = null;
            return !string.IsNullOrWhiteSpace(Collections.CurrentConstellation)
                && _constellationManager.TryGetConstellation(Collections.CurrentConstellation, out currentConstellation);
        }
    }
}
