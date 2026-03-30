using LethalConstellations.PluginCore;
using LethalMoonUnlocks.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalMoonUnlocks.Compatibility {
    internal sealed class LethalConstellationsManager {
        private const string StartingConstellationPolicyRandom = "Random";
        private const string StoryReleaseBehaviorImmediateDiscovery = "ImmediateDiscovery";

        internal static LethalConstellationsManager Instance { get; private set; }

        private readonly LethalConstellationsExtension _extension;
        private readonly Dictionary<string, ClassMapper> _constellationLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _moonNameToLevelIdLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, LMUnlockable> _unlockByLevelIdLookup = [];
        private readonly Dictionary<int, string> _moonToConstellationLookup = [];
        private readonly Dictionary<string, HashSet<int>> _constellationMoonLevelLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _defaultMoonLevelLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<int>> _rotationLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<int>> _localDiscoveryLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _invalidVisitedMoonRuleWarnings = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<string>> _pendingRotationLoad;
        private Dictionary<string, List<string>> _pendingLocalDiscoveryLoad;

        private bool _bootstrapped;
        private bool _hasLoadedPersistedState;

        internal sealed class StoryReleaseResult {
            internal List<string> AffectedConstellations { get; } = new();
            internal bool ImmediateDiscovery { get; set; }
            internal bool AnyAffected => AffectedConstellations.Count > 0;
        }

        internal sealed class ConstellationEconomyTarget {
            internal ClassMapper Constellation { get; set; }
            internal LMConstellationUnlockable State { get; set; }
            internal LMUnlockable DefaultMoon { get; set; }
            internal int EffectivePrice { get; set; }
            internal int EffectiveOriginalPrice { get; set; }
            internal string Name => State?.Name ?? Constellation?.consName ?? string.Empty;
        }

        internal LethalConstellationsManager(LethalConstellationsExtension extension) {
            this._extension = extension ?? throw new ArgumentNullException(nameof(extension));
            Instance = this;
        }

        internal void Reset() {
            _constellationLookup.Clear();
            _moonNameToLevelIdLookup.Clear();
            _unlockByLevelIdLookup.Clear();
            _moonToConstellationLookup.Clear();
            _constellationMoonLevelLookup.Clear();
            _defaultMoonLevelLookup.Clear();
            _rotationLookup.Clear();
            _localDiscoveryLookup.Clear();
            _invalidVisitedMoonRuleWarnings.Clear();
            _pendingRotationLoad = null;
            _pendingLocalDiscoveryLoad = null;
            _bootstrapped = false;
            _hasLoadedPersistedState = false;
        }

        internal void SetHasLoadedPersistedState(bool value) {
            _hasLoadedPersistedState = value;
        }

        internal void RefreshDefinitions(bool logProblems) {
            _constellationLookup.Clear();
            _moonNameToLevelIdLookup.Clear();
            _unlockByLevelIdLookup.Clear();
            _moonToConstellationLookup.Clear();
            _constellationMoonLevelLookup.Clear();
            _defaultMoonLevelLookup.Clear();
            Plugin.ConstellationUnlockConditions?.RefreshDefinitions(saveChanges: logProblems);

            if (Collections.ConstellationStuff.Count == 0) {
                if (logProblems) {
                    Logger.LogError("LethalConstellationsManager: No constellations are available to index.");
                }
                return;
            }

            _extension.TryApplyPendingSaveData(refreshDefinitions: false);
            _extension.SyncConstellationStates();

            if (UnlockManager.Instance?.Unlocks != null) {
                foreach (var unlock in UnlockManager.Instance.Unlocks) {
                    if (!TryGetLevelId(unlock, out var levelId)) {
                        continue;
                    }

                    _unlockByLevelIdLookup[levelId] = unlock;
                    if (!_moonNameToLevelIdLookup.ContainsKey(unlock.Name)) {
                        _moonNameToLevelIdLookup[unlock.Name] = levelId;
                    }
                }
            }

            foreach (var constellation in Collections.ConstellationStuff) {
                if (string.IsNullOrWhiteSpace(constellation.consName)) {
                    if (logProblems) {
                        Logger.LogError("LethalConstellationsManager: Encountered a constellation with no name while indexing.");
                    }
                    continue;
                }

                if (!_constellationLookup.TryAdd(constellation.consName, constellation) && logProblems) {
                    Logger.LogWarning($"LethalConstellationsManager: Duplicate constellation name '{constellation.consName}' encountered. Keeping the first definition.");
                }

                if (!_constellationMoonLevelLookup.TryGetValue(constellation.consName, out var memberLevelIds)) {
                    memberLevelIds = new HashSet<int>();
                    _constellationMoonLevelLookup[constellation.consName] = memberLevelIds;
                }

                foreach (var moon in constellation.constelMoons) {
                    if (string.IsNullOrWhiteSpace(moon)) {
                        continue;
                    }

                    if (!_moonNameToLevelIdLookup.TryGetValue(moon, out var levelId)) {
                        continue;
                    }

                    memberLevelIds.Add(levelId);
                    if (!_moonToConstellationLookup.TryAdd(levelId, constellation.consName) && _moonToConstellationLookup.TryGetValue(levelId, out var existingConstellation) && !string.Equals(existingConstellation, constellation.consName, StringComparison.OrdinalIgnoreCase) && logProblems) {
                        Logger.LogWarning($"LethalConstellationsManager: Moon '{moon}' is present in multiple constellations. Keeping the first mapping to '{existingConstellation}'.");
                    }
                }
            }

            foreach (var constellation in _constellationLookup.Values) {
                var constellationState = _extension.GetOrCreateConstellationState(constellation.consName);
                if (constellationState == null) {
                    if (logProblems) {
                        Logger.LogError($"LethalConstellationsManager: Failed to create state for constellation '{constellation.consName}' while indexing.");
                    }
                    continue;
                }

                if (!TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                    if (logProblems) {
                        Logger.LogError($"LethalConstellationsManager: Constellation '{constellation.consName}' has no valid configured default moon.");
                    }
                    continue;
                }

                if (TryGetLevelId(defaultMoon, out var defaultMoonLevelId)) {
                    _defaultMoonLevelLookup[constellation.consName] = defaultMoonLevelId;
                }
                SyncConstellationStateFromDefaultMoon(constellation, constellationState, defaultMoon);
            }

            if (logProblems) {
                foreach (var savedConstellationName in _extension.ConstellationStates.Keys) {
                    if (!_constellationLookup.ContainsKey(savedConstellationName)) {
                        Logger.LogError($"LethalConstellationsManager: Saved constellation state '{savedConstellationName}' has no matching constellation definition.");
                    }
                }
            }

            TryApplyPendingLocalMoonDiscoveries();
            TryApplyPendingConstellationRotation();
            EnsureRotationsForDiscoveredConstellations();
        }

        internal bool Bootstrap() {
            if (_bootstrapped) {
                return true;
            }

            RefreshDefinitions(true);
            ApplyWhitelist();

            if (_hasLoadedPersistedState || HasDiscoveredConstellations()) {
                if (!TryEnsureCurrentConstellation()) {
                    Logger.LogError("LethalConstellationsManager: Failed to recover a current constellation when constellation discovery progression data is available.");
                    return false;
                }

                EnsureRotationsForDiscoveredConstellations();
                _bootstrapped = true;
                return true;
            }

            if (!TryResolveBootstrapConstellation(out var bootstrapConstellation)) {
                return false;
            }

            EnsureCurrentConstellationIsValid(bootstrapConstellation);
            EnsureRotationsForDiscoveredConstellations();
            _bootstrapped = true;
            return true;
        }

        internal StoryReleaseResult ReleaseStoryLockForMoon(string moonName) {
            var result = new StoryReleaseResult();
            if (string.IsNullOrWhiteSpace(moonName) || Collections.ConstellationStuff.Count == 0) {
                return result;
            }

            bool immediateDiscovery = IsImmediateDiscoveryStoryReleaseBehavior();
            result.ImmediateDiscovery = immediateDiscovery;

            foreach (var constellation in Collections.ConstellationStuff.Where(constellation =>
                         !string.IsNullOrWhiteSpace(constellation.defaultMoon) &&
                         string.Equals(constellation.defaultMoon, moonName, StringComparison.OrdinalIgnoreCase))) {
                var constellationState = _extension.GetOrCreateConstellationState(constellation.consName);
                if (constellationState == null) {
                    continue;
                }

                if (!TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                    Logger.LogError($"LethalConstellationsManager: Unable to resolve the configured default moon for constellation '{constellation.consName}' during story release.");
                    continue;
                }

                if (TryApplyConstellationStoryRelease(
                        constellation,
                        constellationState,
                        defaultMoon,
                        $"story release of constellation '{constellation.consName}' via moon '{moonName}'",
                        out bool appliedImmediateDiscovery)) {
                    result.AffectedConstellations.Add(constellation.consName);
                    Logger.LogInfo($"LethalConstellationsManager: Released story lock for constellation '{constellation.consName}' via moon '{moonName}' ({(appliedImmediateDiscovery ? "immediate discovery" : "hidden backlog")}).");
                    continue;
                }

                Logger.LogInfo($"LethalConstellationsManager: Released moon-side story lock for constellation '{constellation.consName}' via moon '{moonName}', but additional custom unlock conditions are still required.");
            }

            if (result.AffectedConstellations.Any(name => string.Equals(name, Collections.CurrentConstellation, StringComparison.OrdinalIgnoreCase))) {
                ApplyCurrentConstellationVisibility();
            }

            return result;
        }

        internal bool EvaluateCustomUnlockConditions() {
            if (Plugin.ConstellationUnlockConditions == null || Collections.ConstellationStuff.Count == 0) {
                return false;
            }

            Plugin.ConstellationUnlockConditions.RefreshDefinitions(saveChanges: false);
            EnsureIndexed();

            bool anyChanged = false;
            foreach (var constellation in _constellationLookup.Values) {
                if (constellation == null || string.IsNullOrWhiteSpace(constellation.consName)) {
                    continue;
                }

                var constellationState = _extension.GetOrCreateConstellationState(constellation.consName);
                if (constellationState == null) {
                    continue;
                }

                if (TryUnlockCustomCondition(constellation, constellationState)) {
                    anyChanged = true;
                    continue;
                }

                if (TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                    SyncConstellationStateFromDefaultMoon(constellation, constellationState, defaultMoon);
                }
            }

            return anyChanged;
        }

        internal bool TryGetConstellation(string constellationName, out ClassMapper constellation) {
            EnsureIndexed();
            constellation = null;

            if (string.IsNullOrWhiteSpace(constellationName)) {
                return false;
            }

            return _constellationLookup.TryGetValue(constellationName, out constellation) && constellation != null;
        }

        internal string GetConstellationName(LMUnlockable unlock) {
            if (!TryGetLevelId(unlock, out var levelId)) {
                return string.Empty;
            }

            EnsureIndexed();
            return _moonToConstellationLookup.TryGetValue(levelId, out var constellationName) ? constellationName : string.Empty;
        }

        internal bool TryGetDefaultMoon(string constellationName, out LMUnlockable defaultMoon) {
            defaultMoon = null;

            EnsureIndexed();
            if (!TryGetConstellation(constellationName, out var constellation) || string.IsNullOrWhiteSpace(constellation.defaultMoon)) {
                return false;
            }

            if (_defaultMoonLevelLookup.TryGetValue(constellationName, out var cachedDefaultMoonLevelId)
                && _unlockByLevelIdLookup.TryGetValue(cachedDefaultMoonLevelId, out defaultMoon)
                && defaultMoon != null) {
                if (!defaultMoon.StoryUnlock && (defaultMoon.OriginallyHidden || defaultMoon.OriginallyLocked)) {
                    Logger.LogWarning($"LethalConstellationsManager: Configured default moon '{constellation.defaultMoon}' for constellation '{constellation.consName}' is hidden or locked without a story gate. Ignoring the constellation's default moon.");
                    _defaultMoonLevelLookup.Remove(constellationName);
                    defaultMoon = null;
                    return false;
                }

                return true;
            }

            if (!_moonNameToLevelIdLookup.TryGetValue(constellation.defaultMoon, out var defaultMoonLevelId)
                || !_unlockByLevelIdLookup.TryGetValue(defaultMoonLevelId, out defaultMoon)
                || defaultMoon == null) {
                return false;
            }

            if (!defaultMoon.StoryUnlock && (defaultMoon.OriginallyHidden || defaultMoon.OriginallyLocked)) {
                Logger.LogWarning($"LethalConstellationsManager: Configured default moon '{constellation.defaultMoon}' for constellation '{constellation.consName}' is hidden or locked without a story gate. Ignoring the constellation's default moon.");
                defaultMoon = null;
                return false;
            }

            _defaultMoonLevelLookup[constellationName] = defaultMoonLevelId;
            return true;
        }

        internal bool TryGetCurrentDefaultMoon(out LMUnlockable defaultMoon) {
            defaultMoon = null;
            return !string.IsNullOrWhiteSpace(Collections.CurrentConstellation)
                && TryGetDefaultMoon(Collections.CurrentConstellation, out defaultMoon);
        }

        internal string GetCurrentConstellationName() {
            return Collections.CurrentConstellation ?? string.Empty;
        }

        internal string GetConstellationWord() {
            return LethalConstellations.ConfigManager.Configuration.ConstellationWord.Value;
        }

        internal bool IsMoonInConstellation(LMUnlockable unlock, string constellationName) {
            if (!TryGetLevelId(unlock, out var levelId) || string.IsNullOrWhiteSpace(constellationName)) {
                return false;
            }

            EnsureIndexed();
            return _constellationMoonLevelLookup.TryGetValue(constellationName, out var memberLevelIds)
                   && memberLevelIds.Contains(levelId);
        }

        internal bool TryGetConstellationEconomyTarget(string constellationName, out ConstellationEconomyTarget target) {
            EnsureIndexed();
            target = null;

            if (string.IsNullOrWhiteSpace(constellationName)
                || !_constellationLookup.TryGetValue(constellationName, out var constellation)
                || constellation == null
                || !TryGetConstellationState(constellation.consName, out var state)) {
                return false;
            }

            target = CreateEconomyTarget(constellation, state);
            return target != null;
        }

        internal bool TryGetCurrentConstellationEconomyTarget(out ConstellationEconomyTarget target) {
            return TryGetConstellationEconomyTarget(Collections.CurrentConstellation, out target);
        }

        internal List<ConstellationEconomyTarget> GetQuotaRewardTargets() {
            EnsureIndexed();

            var targets = new List<ConstellationEconomyTarget>();
            if (string.Equals(ConfigManager.LethalConstellationsQuotaRewardScope, "CurrentConstellationOnly", StringComparison.OrdinalIgnoreCase)) {
                if (TryGetCurrentConstellationEconomyTarget(out var currentTarget) && IsConstellationAvailable(currentTarget.State)) {
                    targets.Add(currentTarget);
                }

                return targets;
            }

            foreach (var constellation in _constellationLookup.Values) {
                if (!TryGetConstellationState(constellation.consName, out var state) || !IsConstellationAvailable(state)) {
                    continue;
                }

                var target = CreateEconomyTarget(constellation, state);
                if (target != null) {
                    targets.Add(target);
                }
            }

            return targets;
        }

        internal void RefreshConstellationSales() {
            EnsureIndexed();

            foreach (var constellation in _constellationLookup.Values) {
                if (!TryGetConstellationState(constellation.consName, out var state)) {
                    continue;
                }

                state.UpdateFromConstellation(constellation);
                TryGetDefaultMoon(constellation.consName, out var defaultMoon);
                int effectiveOriginalPrice = GetEffectiveOriginalPrice(state, defaultMoon);
                state.IterateState(effectiveOriginalPrice);
                if (IsConstellationAvailable(state)) {
                    state.RefreshSale(effectiveOriginalPrice);
                } else {
                    state.OnSale = false;
                    state.SalesRate = 0;
                }

                state.IterateState(effectiveOriginalPrice);
            }

            ApplyConstellationState();
        }

        internal void ApplyConstellationState() {
            EnsureIndexed();

            foreach (var constellation in _constellationLookup.Values) {
                if (!TryGetConstellationState(constellation.consName, out var state)) {
                    continue;
                }

                state.UpdateFromConstellation(constellation);
                bool hasDefaultMoon = TryGetDefaultMoon(constellation.consName, out var defaultMoon);
                int effectiveOriginalPrice = GetEffectiveOriginalPrice(state, defaultMoon);
                state.IterateState(effectiveOriginalPrice);
                ApplyConstellationState(constellation, state);

                if (hasDefaultMoon) {
                    if (TryGetLevelId(defaultMoon, out var defaultMoonLevelId)) {
                        _defaultMoonLevelLookup[constellation.consName] = defaultMoonLevelId;
                    }
                    constellation.defaultMoonLevel = defaultMoon.ExtendedLevel;
                }

                constellation.constelPrice = state.RoutePrice;
                constellation.oneTimePurchase = ConfigManager.UnlockMode && !ConfigManager.DiscountMode && constellation.constelPrice <= 0;
            }

            if (!string.IsNullOrWhiteSpace(Collections.CurrentConstellation) && _constellationLookup.TryGetValue(Collections.CurrentConstellation, out var currentConstellation)) {
                Collections.CurrentConstellationCM = currentConstellation;
            }
        }

        internal bool IsImmediateDiscoveryStoryReleaseBehavior() {
            return string.Equals(ConfigManager.LCStoryReleaseBehavior, StoryReleaseBehaviorImmediateDiscovery, StringComparison.OrdinalIgnoreCase);
        }

        internal List<LMUnlockable> GetVisibleConstellationUnlocks(string constellationName) {
            var visibleMoons = new List<LMUnlockable>();
            if (UnlockManager.Instance == null || !UnlockManager.Instance.UseConstellationDiscovery) {
                return visibleMoons;
            }

            if (!Bootstrap() || !TryGetConstellation(constellationName, out var constellation) || !TryGetConstellationState(constellationName, out var constellationState)) {
                return visibleMoons;
            }

            if (!constellationState.Discovered) {
                return visibleMoons;
            }

            var memberUnlocks = GetConstellationUnlocks(constellation);
            if (memberUnlocks.Count == 0) {
                return visibleMoons;
            }

            EnsureConstellationRotation(constellationName);

            var visibleMoonIds = new HashSet<int>();
            void AddVisibleMoon(LMUnlockable unlock) {
                if (TryGetLevelId(unlock, out var levelId)) {
                    visibleMoonIds.Add(levelId);
                }
            }

            if (TryGetDefaultMoon(constellationName, out var defaultMoon)) {
                AddVisibleMoon(defaultMoon);
            }

            foreach (var permanentlyDiscoveredMoon in memberUnlocks.Where(unlock => unlock.PermanentlyDiscovered)) {
                AddVisibleMoon(permanentlyDiscoveredMoon);
            }

            if (_rotationLookup.TryGetValue(constellationName, out var rotationMoons)) {
                foreach (var rotationMoon in memberUnlocks.Where(unlock => TryGetLevelId(unlock, out var levelId) && rotationMoons.Contains(levelId))) {
                    AddVisibleMoon(rotationMoon);
                }
            }

            if (_localDiscoveryLookup.TryGetValue(constellationName, out var localDiscoveries)) {
                foreach (var localDiscovery in memberUnlocks.Where(unlock => TryGetLevelId(unlock, out var levelId) && localDiscoveries.Contains(levelId))) {
                    AddVisibleMoon(localDiscovery);
                }
            }

            return memberUnlocks.Where(unlock => TryGetLevelId(unlock, out var levelId) && visibleMoonIds.Contains(levelId)).ToList();
        }

        internal bool ApplyCurrentConstellationVisibility(bool suppressNewDiscovery = false) {
            if (UnlockManager.Instance == null || !UnlockManager.Instance.UseConstellationDiscovery) {
                return false;
            }

            if (!Bootstrap()) {
                Logger.LogError("LethalConstellationsManager: Unable to apply moon visibility because initialization failed.");
                return false;
            }

            if (!TryEnsureCurrentConstellation()) {
                Logger.LogError("LethalConstellationsManager: Failed applying visibility status! No current constellation is available. This should not happen at runtime.");
                return false;
            }

            string currentConstellationName = Collections.CurrentConstellation;
            if (string.IsNullOrWhiteSpace(currentConstellationName)) {
                Logger.LogError("LethalConstellationsManager: Failed applying visibility status! No current constellation is available. This should not happen at runtime.");
                return false;
            }

            var visibleMoons = GetVisibleConstellationUnlocks(currentConstellationName);
            var visibleMoonIds = new HashSet<int>(visibleMoons
                .Select(unlock => TryGetLevelId(unlock, out var levelId) ? levelId : -1)
                .Where(levelId => levelId >= 0));

            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                unlock.SetDiscoveryState(TryGetLevelId(unlock, out var levelId) && visibleMoonIds.Contains(levelId), suppressNewDiscovery);
            }

            Logger.LogInfo($"LethalConstellationsManager: Applied moon visibility for '{currentConstellationName}' -> [ {string.Join(", ", visibleMoons.Select(unlock => unlock.Name))} ]");
            return true;
        }

        internal List<LMUnlockable> GetCurrentVisibleUnlocks() {
            return GetVisibleConstellationUnlocks(Collections.CurrentConstellation);
        }

        internal List<LMUnlockable> GetCurrentConstellationDiscoveryCandidates() {
            if (!Bootstrap()) {
                return new List<LMUnlockable>();
            }

            string currentConstellationName = Collections.CurrentConstellation;
            if (string.IsNullOrWhiteSpace(currentConstellationName) || !TryGetConstellation(currentConstellationName, out var constellation)) {
                Logger.LogError("LethalConstellationsManager: No valid current constellation is available for moon discovery selection.");
                return new List<LMUnlockable>();
            }

            var visibleMoonIds = new HashSet<int>(GetVisibleConstellationUnlocks(currentConstellationName)
                .Select(unlock => TryGetLevelId(unlock, out var levelId) ? levelId : -1)
                .Where(levelId => levelId >= 0));
            return GetConstellationUnlocks(constellation)
                .Where(unlock => IsEligibleForLocalRotation(unlock) && (!TryGetLevelId(unlock, out var levelId) || !visibleMoonIds.Contains(levelId)))
                .ToList();
        }

        internal bool HasUndiscoveredConstellations() {
            return GetUndiscoveredConstellations(includeStoryLocked: true, requireResolvableDefaultMoon: false).Count > 0;
        }

        internal bool HasEligibleUndiscoveredConstellations() {
            return GetUndiscoveredConstellations(includeStoryLocked: false, requireResolvableDefaultMoon: true).Count > 0;
        }

        internal bool TryDiscoverConstellation(bool preferCheapest, out string constellationName) {
            constellationName = string.Empty;

            var candidates = GetUndiscoveredConstellations(includeStoryLocked: false, requireResolvableDefaultMoon: true);
            if (candidates.Count == 0) {
                return false;
            }

            var constellation = preferCheapest
                ? candidates.OrderBy(candidate => candidate.constelPrice).ThenBy(candidate => candidate.consName, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
                : candidates[RandomHelper.Range(0, candidates.Count)];
            if (constellation == null) {
                return false;
            }

            var constellationState = _extension.GetOrCreateConstellationState(constellation.consName);
            if (constellationState == null) {
                Logger.LogError($"LethalConstellationsManager: Failed to create state while discovering constellation '{constellation.consName}'.");
                return false;
            }

            if (!TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                Logger.LogError($"LethalConstellationsManager: Unable to resolve the configured default moon while discovering constellation '{constellation.consName}'.");
                return false;
            }

            if (!DiscoverConstellation(constellation, constellationState, $"trigger discovery of constellation '{constellation.consName}'")) {
                return false;
            }

            constellationName = constellation.consName;
            Logger.LogInfo($"LethalConstellationsManager: Discovered constellation '{constellationName}' via trigger progression. Marked discovered and permanently available for this run; default moon '{defaultMoon.Name}' forced available.");
            return true;
        }

        internal bool HandleConstellationRoute(string constellationName, int chargedPrice) {
            if (string.IsNullOrWhiteSpace(constellationName) || !TryGetConstellation(constellationName, out var constellation)) {
                Logger.LogError($"LethalConstellationsManager: Unable to handle route progression for missing constellation '{constellationName}'.");
                return false;
            }

            var constellationState = _extension.GetOrCreateConstellationState(constellation.consName);
            if (constellationState == null) {
                Logger.LogError($"LethalConstellationsManager: Failed to create state for routed constellation '{constellation.consName}'.");
                return false;
            }

            constellationState.UpdateFromConstellation(constellation);
            TryGetDefaultMoon(constellation.consName, out var defaultMoon);
            int effectiveOriginalPrice = GetEffectiveOriginalPrice(constellationState, defaultMoon);
            constellationState.IterateState(effectiveOriginalPrice);

            bool routeWasPaid = chargedPrice > 0;
            int previousBuyCount = constellationState.BuyCount;
            int previousFreeVisitCount = constellationState.FreeVisitCount;
            if (routeWasPaid) {
                constellationState.AdvanceBuyProgression();
                Logger.LogInfo($"Constellation {constellation.consName}: Set buy count to {constellationState.BuyCount}");
            }

            constellationState.IterateState(effectiveOriginalPrice);
            constellationState.VisitRoute(effectiveOriginalPrice);
            constellationState.IterateState(effectiveOriginalPrice);
            ApplyConstellationState();

            if (ConfigManager.UnlockMode && !ConfigManager.DiscountMode && ConfigManager.UnlocksResetAfterVisits > 0) {
                if (previousFreeVisitCount > 0 && constellationState.FreeVisitCount == 0 && constellationState.BuyCount == 0) {
                    NotificationHelper.SendChatMessage($"Unlock expired:\n<color=red>{constellation.consName}</color>");
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                        Header = "Unlock expired!",
                        Text = $"Your unlock for constellation {constellation.consName} has been used {previousFreeVisitCount.NumberOfWords("time")} and expired.",
                        IsWarning = true,
                        Key = "LMU_LethalConstellationsUnlockExpired"
                    });
                } else if (constellationState.FreeVisitCount > previousFreeVisitCount && constellationState.FreeVisitCount > 1) {
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                        Header = constellation.consName,
                        Text = $"Unlock redeemed! {(constellationState.FreeVisitCount - 1).CountToText()} use.\nYou have {(ConfigManager.UnlocksResetAfterVisits - constellationState.FreeVisitCount + 1).NumberOfWords("use")} left.",
                        Key = "LMU_LethalConstellationsUnlockUsed"
                    });
                }
            }

            if (ConfigManager.DiscountMode && ConfigManager.DiscountsResetAfterVisits > 0) {
                if (previousFreeVisitCount > 0 && constellationState.FreeVisitCount == 0 && previousBuyCount > 0 && constellationState.BuyCount == 0) {
                    NotificationHelper.SendChatMessage($"Discount expired:\n<color=red>{constellation.consName}</color>");
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                        Header = "Discount expired!",
                        Text = $"Your discount for constellation {constellation.consName} has been used {previousFreeVisitCount.NumberOfWords("time")} and expired.",
                        IsWarning = true,
                        Key = "LMU_LethalConstellationsDiscountExpired"
                    });
                } else if (constellationState.FreeVisitCount > previousFreeVisitCount && constellationState.FreeVisitCount > 1) {
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                        Header = $"Discount: {constellation.consName}",
                        Text = $"Discount redeemed! {(constellationState.FreeVisitCount - 1).CountToText()} use.\nYou have {(ConfigManager.DiscountsResetAfterVisits - constellationState.FreeVisitCount + 1).NumberOfWords("use")} left.",
                        Key = "LMU_LethalConstellationsDiscountUsed"
                    });
                }
            }

            return routeWasPaid || constellationState.FreeVisitCount != previousFreeVisitCount || constellationState.BuyCount != previousBuyCount;
        }

        internal void ClearAllConstellationRotations() {
            _rotationLookup.Clear();
            _pendingRotationLoad = null;
        }

        internal void RegenerateAllConstellationRotations() {
            if (!Bootstrap()) {
                return;
            }

            _rotationLookup.Clear();
            foreach (var constellation in _constellationLookup.Values) {
                if (!TryGetConstellationState(constellation.consName, out var state) || !state.Discovered) {
                    continue;
                }

                RegenerateConstellationRotation(constellation);
            }
        }

        internal void ClearLocalMoonDiscoveries() {
            _localDiscoveryLookup.Clear();
        }

        internal void AddLocalMoonDiscoveries(string constellationName, IEnumerable<LMUnlockable> moons) {
            if (string.IsNullOrWhiteSpace(constellationName) || moons == null) {
                return;
            }

            if (!_localDiscoveryLookup.TryGetValue(constellationName, out var localDiscoveries)) {
                localDiscoveries = new HashSet<int>();
                _localDiscoveryLookup[constellationName] = localDiscoveries;
            }

            foreach (var moon in moons) {
                if (TryGetLevelId(moon, out var levelId)) {
                    localDiscoveries.Add(levelId);
                }
            }
        }

        internal void AddLocalMoonDiscoveriesForCurrentConstellation(IEnumerable<LMUnlockable> moons) {
            AddLocalMoonDiscoveries(Collections.CurrentConstellation, moons);
        }

        internal void SetLocalMoonDiscoveries(string constellationName, IEnumerable<string> moonNames) {
            if (string.IsNullOrWhiteSpace(constellationName)) {
                return;
            }

            var localDiscoveries = new HashSet<int>();
            if (moonNames != null) {
                foreach (var moonName in moonNames) {
                    if (!string.IsNullOrWhiteSpace(moonName) && _moonNameToLevelIdLookup.TryGetValue(moonName, out var levelId)) {
                        localDiscoveries.Add(levelId);
                    }
                }
            }

            _localDiscoveryLookup[constellationName] = localDiscoveries;
        }

        internal Dictionary<string, List<string>> GetAllConstellationRotations() {
            if (_pendingRotationLoad != null) {
                return CopyConstellationRotations(_pendingRotationLoad);
            }

            var rotationSelections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _rotationLookup) {
                rotationSelections[entry.Key] = ResolveMoonNames(entry.Value);
            }

            return rotationSelections;
        }

        internal Dictionary<string, List<string>> GetAllLocalMoonDiscoveries() {
            if (_pendingLocalDiscoveryLoad != null) {
                return CopyLocalMoonDiscoveries(_pendingLocalDiscoveryLoad);
            }

            var localDiscoveries = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _localDiscoveryLookup) {
                localDiscoveries[entry.Key] = ResolveMoonNames(entry.Value);
            }

            return localDiscoveries;
        }

        internal void ReplaceAllConstellationRotations(Dictionary<string, List<string>> rotationSelections) {
            _rotationLookup.Clear();
            _pendingRotationLoad = null;
            if (rotationSelections == null) {
                return;
            }

            if (Collections.ConstellationStuff.Count == 0) {
                _pendingRotationLoad = CopyConstellationRotations(rotationSelections);
                Logger.LogWarning("LethalConstellationsManager: Constellation definitions are unavailable. Deferring rotation restore.");
                return;
            }

            EnsureIndexed();
            ApplyValidatedConstellationRotations(rotationSelections);
            EnsureRotationsForDiscoveredConstellations();
        }

        internal void ReplaceLocalMoonDiscoveries(Dictionary<string, List<string>> localDiscoveries) {
            _localDiscoveryLookup.Clear();
            _pendingLocalDiscoveryLoad = null;
            if (localDiscoveries == null) {
                return;
            }

            if (Collections.ConstellationStuff.Count == 0) {
                _pendingLocalDiscoveryLoad = CopyLocalMoonDiscoveries(localDiscoveries);
                Logger.LogWarning("LethalConstellationsManager: Constellation definitions are unavailable. Deferring local discovery restore.");
                return;
            }

            EnsureIndexed();
            ApplyValidatedLocalMoonDiscoveries(localDiscoveries);
            EnsureRotationsForDiscoveredConstellations();
        }

        private void ApplyValidatedConstellationRotations(Dictionary<string, List<string>> rotationSelections) {
            if (rotationSelections == null) {
                return;
            }

            foreach (var entry in rotationSelections) {
                if (string.IsNullOrWhiteSpace(entry.Key)) {
                    Logger.LogWarning("LethalConstellationsManager: Dropping rotation entry with no constellation name.");
                    continue;
                }

                if (!_constellationLookup.TryGetValue(entry.Key, out var constellation)) {
                    Logger.LogError($"LethalConstellationsManager: Dropping orphaned rotation state for missing constellation '{entry.Key}'.");
                    continue;
                }

                if (!_constellationMoonLevelLookup.TryGetValue(constellation.consName, out var constellationMoonIds)) {
                    SetConstellationRotation(entry.Key, Array.Empty<int>());
                    continue;
                }

                var resolvedMoonIds = new List<int>();
                if (entry.Value != null) {
                    foreach (var moonName in entry.Value) {
                        if (!string.IsNullOrWhiteSpace(moonName)
                            && _moonNameToLevelIdLookup.TryGetValue(moonName, out var levelId)
                            && constellationMoonIds.Contains(levelId)) {
                            resolvedMoonIds.Add(levelId);
                        }
                    }
                }

                SetConstellationRotation(entry.Key, resolvedMoonIds);
            }
        }

        private void ApplyValidatedLocalMoonDiscoveries(Dictionary<string, List<string>> localDiscoveries) {
            if (localDiscoveries == null) {
                return;
            }

            foreach (var entry in localDiscoveries) {
                if (string.IsNullOrWhiteSpace(entry.Key)) {
                    Logger.LogWarning("LethalConstellationsManager: Dropping local discovery entry with no constellation name.");
                    continue;
                }

                if (!_constellationLookup.ContainsKey(entry.Key)) {
                    Logger.LogError($"LethalConstellationsManager: Dropping orphaned local discovery state for missing constellation '{entry.Key}'.");
                    continue;
                }

                SetLocalMoonDiscoveries(entry.Key, entry.Value);
            }
        }

        private void TryApplyPendingConstellationRotation() {
            if (_pendingRotationLoad == null || _constellationLookup.Count == 0) {
                return;
            }

            var pendingRotationSelections = _pendingRotationLoad;
            _pendingRotationLoad = null;
            _rotationLookup.Clear();
            ApplyValidatedConstellationRotations(pendingRotationSelections);
        }

        private void TryApplyPendingLocalMoonDiscoveries() {
            if (_pendingLocalDiscoveryLoad == null || _constellationLookup.Count == 0) {
                return;
            }

            var pendingLocalDiscoveries = _pendingLocalDiscoveryLoad;
            _pendingLocalDiscoveryLoad = null;
            _localDiscoveryLookup.Clear();
            ApplyValidatedLocalMoonDiscoveries(pendingLocalDiscoveries);
        }

        private static Dictionary<string, List<string>> CopyConstellationRotations(Dictionary<string, List<string>> rotationSelections) {
            var copy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (rotationSelections == null) {
                return copy;
            }

            foreach (var entry in rotationSelections) {
                if (string.IsNullOrWhiteSpace(entry.Key)) {
                    continue;
                }

                copy[entry.Key] = entry.Value == null
                    ? new List<string>()
                    : entry.Value.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            return copy;
        }

        private static Dictionary<string, List<string>> CopyLocalMoonDiscoveries(Dictionary<string, List<string>> localDiscoveries) {
            var copy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (localDiscoveries == null) {
                return copy;
            }

            foreach (var entry in localDiscoveries) {
                if (string.IsNullOrWhiteSpace(entry.Key)) {
                    continue;
                }

                copy[entry.Key] = entry.Value == null
                    ? new List<string>()
                    : entry.Value.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            return copy;
        }

        internal bool IsDerivedVisibleAnywhere(LMUnlockable unlock) {
            if (!TryGetLevelId(unlock, out var levelId)) {
                return false;
            }

            return GetAllVisibleMoonIds().Contains(levelId);
        }

        private void EnsureIndexed() {
            if (_constellationLookup.Count > 0) {
                return;
            }

            RefreshDefinitions(false);
        }

        private List<ClassMapper> GetUndiscoveredConstellations(bool includeStoryLocked, bool requireResolvableDefaultMoon) {
            if (!Bootstrap()) {
                return new List<ClassMapper>();
            }

            var undiscoveredConstellations = new List<ClassMapper>();
            foreach (var constellation in _constellationLookup.Values) {
                if (!TryGetConstellationState(constellation.consName, out var constellationState)) {
                    continue;
                }

                if (constellationState.Discovered) {
                    continue;
                }

                if (!includeStoryLocked && !constellationState.StoryIsUnlocked) {
                    continue;
                }

                if (requireResolvableDefaultMoon && !TryGetDefaultMoon(constellation.consName, out _)) {
                    continue;
                }

                undiscoveredConstellations.Add(constellation);
            }

            return undiscoveredConstellations;
        }

        private void EnsureRotationsForDiscoveredConstellations() {
            foreach (var constellation in _constellationLookup.Values) {
                if (!TryGetConstellationState(constellation.consName, out var state) || !state.Discovered) {
                    continue;
                }

                EnsureConstellationRotation(constellation.consName);
            }
        }

        private void EnsureConstellationRotation(string constellationName) {
            if (string.IsNullOrWhiteSpace(constellationName) || _rotationLookup.ContainsKey(constellationName)) {
                return;
            }

            if (!_constellationLookup.TryGetValue(constellationName, out var constellation)) {
                return;
            }

            if (!TryGetConstellationState(constellation.consName, out var state) || !state.Discovered) {
                return;
            }

            RegenerateConstellationRotation(constellation);
        }

        private void RegenerateConstellationRotation(ClassMapper constellation) {
            if (constellation == null || UnlockManager.Instance == null) {
                return;
            }

            if (!TryGetConstellationState(constellation.consName, out var state) || !state.Discovered) {
                _rotationLookup.Remove(constellation?.consName ?? string.Empty);
                return;
            }

            var memberUnlocks = GetConstellationUnlocks(constellation);
            var reservedMoonIds = new HashSet<int>();

            if (TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                if (TryGetLevelId(defaultMoon, out var defaultMoonLevelId)) {
                    reservedMoonIds.Add(defaultMoonLevelId);
                }
            }

            foreach (var permanentlyDiscoveredMoon in memberUnlocks.Where(unlock => unlock.PermanentlyDiscovered)) {
                if (TryGetLevelId(permanentlyDiscoveredMoon, out var permanentLevelId)) {
                    reservedMoonIds.Add(permanentLevelId);
                }
            }

            if (_localDiscoveryLookup.TryGetValue(constellation.consName, out var localDiscoveries)) {
                foreach (var localDiscovery in localDiscoveries) {
                    reservedMoonIds.Add(localDiscovery);
                }
            }

            var rotationSelection = new List<int>();
            void AddSelection(IEnumerable<LMUnlockable> selection) {
                if (selection == null) {
                    return;
                }

                foreach (var unlock in selection) {
                    if (!TryGetLevelId(unlock, out var levelId) || !reservedMoonIds.Add(levelId)) {
                        continue;
                    }

                    rotationSelection.Add(levelId);
                }
            }

            var freeCandidates = memberUnlocks
                .Where(unlock => IsEligibleForLocalRotation(unlock) && (!TryGetLevelId(unlock, out var levelId) || !reservedMoonIds.Contains(levelId)) && unlock.OriginalPrice == 0)
                .ToList();
            AddSelection(RandomHelper.Select(freeCandidates, UnlockManager.Instance.DiscoveredFreeCount));

            var dynamicFreeCandidates = memberUnlocks
                .Where(unlock => IsEligibleForLocalRotation(unlock) && (!TryGetLevelId(unlock, out var levelId) || !reservedMoonIds.Contains(levelId)) && unlock.RoutePrice == 0)
                .ToList();
            AddSelection(RandomHelper.Select(dynamicFreeCandidates, UnlockManager.Instance.DiscoveredDynamicFreeCount));

            var paidCandidates = memberUnlocks
                .Where(unlock => IsEligibleForLocalRotation(unlock) && (!TryGetLevelId(unlock, out var levelId) || !reservedMoonIds.Contains(levelId)) && unlock.RoutePrice > 0)
                .ToList();
            AddSelection(ConfigManager.CheapMoonBiasPaidRotation
                ? RandomHelper.SelectWeighted(RandomHelper.CalculateBiasedWeights(paidCandidates, ConfigManager.CheapMoonBiasPaidRotationValue), UnlockManager.Instance.DiscoveredPaidCount)
                : RandomHelper.Select(paidCandidates, UnlockManager.Instance.DiscoveredPaidCount));

            SetConstellationRotation(constellation.consName, rotationSelection);
            Logger.LogInfo($"LethalConstellationsManager: Generated stored moon rotation for '{constellation.consName}' -> [ {string.Join(", ", ResolveMoonNames(rotationSelection))} ]");
        }

        private void SetConstellationRotation(string constellationName, IEnumerable<int> moonLevelIds) {
            if (string.IsNullOrWhiteSpace(constellationName)) {
                return;
            }

            var rotationMoons = new HashSet<int>();
            if (moonLevelIds != null) {
                foreach (var moonLevelId in moonLevelIds) {
                    rotationMoons.Add(moonLevelId);
                }
            }

            _rotationLookup[constellationName] = rotationMoons;
        }

        private void ApplyWhitelist() {
            if (ConfigManager.LethalConstellationsWhitelist.Count == 0) {
                return;
            }

            foreach (var constellationName in ConfigManager.LethalConstellationsWhitelist) {
                if (!TryGetConstellation(constellationName, out var constellation)) {
                    Logger.LogError($"LethalConstellationsManager: Whitelisted constellation '{constellationName}' does not exist.");
                    continue;
                }

                var state = _extension.GetOrCreateConstellationState(constellation.consName);
                if (state == null) {
                    Logger.LogError($"LethalConstellationsManager: Failed to create state for whitelisted constellation '{constellation.consName}'.");
                    continue;
                }

                ForceDiscoverConstellation(constellation, state, $"whitelisted constellation '{constellation.consName}'");
            }
        }

        private bool TryResolveBootstrapConstellation(out ClassMapper bootstrapConstellation) {
            bootstrapConstellation = null;

            var starterCandidates = GetBootstrapCandidates();
            if (starterCandidates.Count == 0) {
                Logger.LogError("LethalConstellationsManager: No eligible constellations exist for startup bootstrap.");
                return false;
            }

            bootstrapConstellation = SelectCandidate(starterCandidates);
            if (bootstrapConstellation == null) {
                Logger.LogError("LethalConstellationsManager: Failed to select a startup constellation.");
                return false;
            }

            return ApplyBootstrapState(bootstrapConstellation, $"startup constellation '{bootstrapConstellation.consName}'");
        }

        private bool ApplyBootstrapState(ClassMapper constellation, string reason) {
            if (constellation == null) {
                return false;
            }

            var bootstrapState = _extension.GetOrCreateConstellationState(constellation.consName);
            if (bootstrapState == null) {
                Logger.LogError($"LethalConstellationsManager: Failed to create state for {reason}.");
                return false;
            }

            if (!ForceDiscoverConstellation(constellation, bootstrapState, reason)) {
                return false;
            }

            ApplySilentStartupProgression(constellation, bootstrapState);
            return true;
        }

        private List<ClassMapper> GetBootstrapCandidates() {
            var candidates = new List<ClassMapper>();
            var acceptableStartingConstellations = ConfigManager.AcceptableStartingConstellations;

            foreach (var constellation in _constellationLookup.Values) {
                if (!TryGetConstellationState(constellation.consName, out var state)) {
                    continue;
                }

                if (!state.StoryIsUnlocked) {
                    continue;
                }

                if (!TryGetDefaultMoon(constellation.consName, out _)) {
                    continue;
                }

                if (acceptableStartingConstellations.Count > 0 && !acceptableStartingConstellations.Any(name => string.Equals(name, constellation.consName, StringComparison.OrdinalIgnoreCase))) {
                    continue;
                }

                candidates.Add(constellation);
            }

            return candidates;
        }

        private ClassMapper SelectCandidate(List<ClassMapper> candidates) {
            if (candidates == null || candidates.Count == 0) {
                return null;
            }

            if (string.Equals(ConfigManager.LCStartingConstellationSelectionPolicy, StartingConstellationPolicyRandom, StringComparison.OrdinalIgnoreCase)) {
                return candidates[RandomHelper.Range(0, candidates.Count)];
            }

            return candidates
                .OrderBy(candidate => candidate.constelPrice)
                .ThenBy(candidate => candidate.consName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private bool TryEnsureCurrentConstellation() {
            string currentConstellationName = Collections.CurrentConstellation;
            if (!string.IsNullOrWhiteSpace(currentConstellationName) && _constellationLookup.TryGetValue(currentConstellationName, out var currentConstellation) && currentConstellation != null) {
                if (TryGetConstellationState(currentConstellation.consName, out var currentState)
                    && IsConstellationAvailable(currentState)
                    && TryGetDefaultMoon(currentConstellation.consName, out _)) {
                    Collections.CurrentConstellationCM = currentConstellation;
                    return true;
                }

                Logger.LogError($"LethalConstellationsManager: Current constellation '{currentConstellationName}' is no longer available.");
            } else if (string.IsNullOrWhiteSpace(currentConstellationName)) {
                Logger.LogError("LethalConstellationsManager: No current constellation exists even though constellation discovery progression data is available. This should not happen at runtime.");
            } else {
                Logger.LogError($"LethalConstellationsManager: Current constellation '{currentConstellationName}' no longer exists even though constellation discovery progression data is available.");
            }

            if (!TryResolveRecoveryConstellation(out var recoveryConstellation)) {
                Collections.CurrentConstellationCM = null;
                return false;
            }

            Logger.LogWarning($"LethalConstellationsManager: Recovering missing current constellation with '{recoveryConstellation.consName}'.");
            Collections.CurrentConstellation = recoveryConstellation.consName;
            Collections.CurrentConstellationCM = recoveryConstellation;
            return true;
        }

        private bool TryResolveRecoveryConstellation(out ClassMapper recoveryConstellation) {
            recoveryConstellation = _constellationLookup.Values
                .Select(constellation => CreateRecoveryCandidate(constellation))
                .Where(candidate => candidate != null)
                .OrderBy(candidate => candidate.EffectivePrice)
                .ThenBy(candidate => candidate.Constellation.consName, StringComparer.OrdinalIgnoreCase)
                .Select(candidate => candidate.Constellation)
                .FirstOrDefault();

            return recoveryConstellation != null;
        }

        private RecoveryConstellationCandidate CreateRecoveryCandidate(ClassMapper constellation) {
            if (constellation == null || !TryGetConstellationState(constellation.consName, out var state) || !IsConstellationAvailable(state)) {
                return null;
            }

            if (!TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                return null;
            }

            var target = CreateEconomyTarget(constellation, state);
            return target == null
                ? null
                : new RecoveryConstellationCandidate(constellation, target.EffectivePrice);
        }

        private void EnsureCurrentConstellationIsValid(ClassMapper bootstrapConstellation) {
            if (bootstrapConstellation == null) {
                return;
            }

            var currentConstellationName = Collections.CurrentConstellation;
            if (!string.IsNullOrWhiteSpace(currentConstellationName) && !string.Equals(currentConstellationName, bootstrapConstellation.consName, StringComparison.OrdinalIgnoreCase)) {
                Logger.LogWarning($"LethalConstellationsManager: Replacing existing current constellation '{currentConstellationName}' with startup constellation '{bootstrapConstellation.consName}'.");
            }

            Collections.CurrentConstellation = bootstrapConstellation.consName;
            Collections.CurrentConstellationCM = bootstrapConstellation;
        }

        private void EnsureDefaultMoonAvailable(ClassMapper constellation, bool warnOnStoryLockOverride, string reason) {
            if (constellation == null) {
                return;
            }

            if (!TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                Logger.LogError($"LethalConstellationsManager: Unable to resolve the configured default moon for {reason}.");
                return;
            }

            if (defaultMoon.StoryUnlock && !defaultMoon.StoryIsUnlocked && warnOnStoryLockOverride) {
                Logger.LogWarning($"LethalConstellationsManager: {reason} requires default moon '{defaultMoon.Name}' to be available, overriding its story lock.");
            }

            if (defaultMoon.StoryUnlock) {
                defaultMoon.StoryIsUnlocked = true;
            }
        }

        private bool DiscoverConstellation(ClassMapper constellation, LMConstellationUnlockable constellationState, string reason) {
            if (constellation == null || constellationState == null) {
                return false;
            }

            if (!TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                Logger.LogError($"LethalConstellationsManager: Unable to resolve the configured default moon for {reason}.");
                return false;
            }

            bool alreadyDiscovered = constellationState.Discovered;
            constellationState.StoryIsUnlocked = true;
            constellationState.Discovered = true;
            constellationState.DiscoveredOnce = true;
            constellationState.NewDiscovery = !alreadyDiscovered;

            EnsureDefaultMoonAvailable(constellation, warnOnStoryLockOverride: true, reason);
            SyncConstellationStateFromDefaultMoon(constellation, constellationState, defaultMoon, allowDefaultMoonNewDiscovery: true);
            EnsureConstellationRotation(constellation.consName);

            if (!alreadyDiscovered) {
                SendConstellationDiscoveredAlert(constellation.consName);
            }

            return true;
        }

        private bool ForceDiscoverConstellation(ClassMapper constellation, LMConstellationUnlockable constellationState, string reason) {
            if (constellation == null || constellationState == null) {
                return false;
            }

            if (!TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                Logger.LogError($"LethalConstellationsManager: Unable to resolve the configured default moon for {reason}.");
                return false;
            }

            constellationState.StoryIsUnlocked = true;
            constellationState.Discovered = true;
            constellationState.DiscoveredOnce = true;
            constellationState.NewDiscovery = false;

            EnsureDefaultMoonAvailable(constellation, warnOnStoryLockOverride: true, reason);
            SyncConstellationStateFromDefaultMoon(constellation, constellationState, defaultMoon, allowDefaultMoonNewDiscovery: false);
            EnsureConstellationRotation(constellation.consName);
            return true;
        }

        private void SendConstellationDiscoveredAlert(string constellationName) {
            if (!ConfigManager.DiscoveryMode || string.IsNullOrWhiteSpace(constellationName) || NetworkManager.Instance == null || !NetworkManager.Instance.IsServer()) {
                return;
            }

            string constellationWord = LethalConstellations.ConfigManager.Configuration.ConstellationWord.Value;
            NotificationHelper.SendChatMessage($"Autopilot discovered new {constellationWord.ToLowerInvariant()}:\n<color=red>{constellationName}</color>");
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                Header = "New Discovery!",
                Text = $"{LethalConstellations.ConfigManager.Configuration.ConstellationWord.Value} <color=yellow>{constellationName}</color> available for routing.",
                IsWarning = true,
                Key = "LMU_ConstellationDiscovered",
                ExceptWhenKey = "LMU_NewQuotaDiscoveryGroup"
            });
        }

        private void ApplySilentStartupProgression(ClassMapper constellation, LMConstellationUnlockable constellationState) {
            if (constellation == null || constellationState == null) {
                return;
            }

            constellationState.UpdateFromConstellation(constellation);
            TryGetDefaultMoon(constellation.consName, out var defaultMoon);
            int effectiveOriginalPrice = GetEffectiveOriginalPrice(constellationState, defaultMoon);
            constellationState.IterateState(effectiveOriginalPrice);
            constellationState.AdvanceBuyProgression();
            constellationState.IterateState(effectiveOriginalPrice);
            constellationState.VisitRoute(effectiveOriginalPrice);
            constellationState.IterateState(effectiveOriginalPrice);
            ApplyConstellationState(constellation, constellationState);
        }

        private bool TryUnlockCustomCondition(ClassMapper constellation, LMConstellationUnlockable constellationState) {
            if (constellation == null || constellationState == null) {
                return false;
            }

            if (!TryGetActiveCustomUnlockRule(constellation.consName, out var ruleDefinition)) {
                return false;
            }

            if (!TryGetDefaultMoon(constellation.consName, out var defaultMoon)) {
                Logger.LogError($"LethalConstellationsManager: Unable to resolve the configured default moon for constellation '{constellation.consName}' while evaluating custom unlock conditions.");
                return false;
            }

            bool customConditionJustUnlocked = false;
            if (!constellationState.CustomConditionUnlocked) {
                if (!AreCustomUnlockConditionsSatisfied(constellation.consName, ruleDefinition)) {
                    return false;
                }

                constellationState.CustomConditionUnlocked = true;
                customConditionJustUnlocked = true;
            }

            bool needsStoryReleaseApplication = !constellationState.StoryIsUnlocked
                || (IsImmediateDiscoveryStoryReleaseBehavior() && !constellationState.Discovered);
            if (!needsStoryReleaseApplication) {
                return customConditionJustUnlocked;
            }

            if (TryApplyConstellationStoryRelease(
                    constellation,
                    constellationState,
                    defaultMoon,
                    $"custom unlock conditions for constellation '{constellation.consName}'",
                    out bool appliedImmediateDiscovery)) {
                if (customConditionJustUnlocked) {
                    Logger.LogInfo($"LethalConstellationsManager: Custom unlock conditions satisfied for constellation '{constellation.consName}', releasing constellation story lock ({(appliedImmediateDiscovery ? "immediate discovery" : "hidden backlog")}).");
                } else {
                    Logger.LogInfo($"LethalConstellationsManager: Applied pending constellation story release for previously satisfied custom unlock conditions on '{constellation.consName}' ({(appliedImmediateDiscovery ? "immediate discovery" : "hidden backlog")}).");
                }
            } else if (customConditionJustUnlocked) {
                Logger.LogInfo($"LethalConstellationsManager: Custom unlock conditions satisfied for constellation '{constellation.consName}', but its default moon story lock is still closed.");
            }

            return true;
        }

        private bool TryGetActiveCustomUnlockRule(string constellationName, out ConstellationUnlockRuleDefinition ruleDefinition) {
            ruleDefinition = null;
            return Plugin.ConstellationUnlockConditions != null
                && Plugin.ConstellationUnlockConditions.TryGetRuleDefinition(constellationName, out ruleDefinition)
                && ruleDefinition != null
                && ruleDefinition.IsEnabled;
        }

        private bool AreCustomUnlockConditionsSatisfied(string constellationName, ConstellationUnlockRuleDefinition ruleDefinition) {
            if (ruleDefinition == null || !ruleDefinition.IsEnabled) {
                return false;
            }

            var activeConditionResults = new List<bool>();
            if (ruleDefinition.RequiredQuotaCount > 0) {
                activeConditionResults.Add(UnlockManager.Instance != null && UnlockManager.Instance.QuotaCount >= ruleDefinition.RequiredQuotaCount);
            }

            if (ruleDefinition.RequiredVisitedMoons.Count > 0) {
                activeConditionResults.Add(HaveVisitedAllMoons(constellationName, ruleDefinition.RequiredVisitedMoons));
            }

            if (ruleDefinition.RequiredUniqueMoonVisits > 0) {
                activeConditionResults.Add(CountUniqueVisitedMoons() >= ruleDefinition.RequiredUniqueMoonVisits);
            }

            if (activeConditionResults.Count == 0) {
                return false;
            }

            return ruleDefinition.MatchMode == ConstellationUnlockMatchMode.All
                ? activeConditionResults.All(result => result)
                : activeConditionResults.Any(result => result);
        }

        private static bool IsDefaultMoonStoryGateOpen(LMUnlockable defaultMoon) {
            return defaultMoon != null && (!defaultMoon.StoryUnlock || defaultMoon.StoryIsUnlocked);
        }

        private bool TryApplyConstellationStoryRelease(ClassMapper constellation, LMConstellationUnlockable constellationState, LMUnlockable defaultMoon, string reason, out bool appliedImmediateDiscovery) {
            appliedImmediateDiscovery = false;
            if (constellation == null || constellationState == null || defaultMoon == null) {
                return false;
            }

            if (!IsConstellationStoryGateOpen(constellation.consName, constellationState, defaultMoon, out _)) {
                SyncConstellationStateFromDefaultMoon(constellation, constellationState, defaultMoon, allowDefaultMoonNewDiscovery: false);
                return false;
            }

            if (IsImmediateDiscoveryStoryReleaseBehavior()) {
                appliedImmediateDiscovery = true;
                return DiscoverConstellation(constellation, constellationState, reason);
            }

            constellationState.StoryIsUnlocked = true;
            SyncConstellationStateFromDefaultMoon(constellation, constellationState, defaultMoon, allowDefaultMoonNewDiscovery: false);
            return true;
        }

        private bool TryGetCustomConditionGateState(string constellationName, LMConstellationUnlockable constellationState, out bool customGateOpen, out bool ignoreDefaultMoonStoryLock) {
            customGateOpen = true;
            ignoreDefaultMoonStoryLock = false;
            if (constellationState == null) {
                return false;
            }

            bool hasPersistedCustomGate = constellationState.CustomConditionUnlocked;
            if (!TryGetActiveCustomUnlockRule(constellationName, out var ruleDefinition)) {
                customGateOpen = hasPersistedCustomGate;
                return hasPersistedCustomGate;
            }

            customGateOpen = constellationState.CustomConditionUnlocked;
            ignoreDefaultMoonStoryLock = ruleDefinition.IgnoreDefaultMoonStoryLock;
            return true;
        }

        private bool IsConstellationStoryGateOpen(string constellationName, LMConstellationUnlockable constellationState, LMUnlockable defaultMoon, out bool ignoreDefaultMoonStoryLock) {
            bool baseGateOpen = IsDefaultMoonStoryGateOpen(defaultMoon);
            if (!TryGetCustomConditionGateState(constellationName, constellationState, out bool customGateOpen, out ignoreDefaultMoonStoryLock)) {
                return baseGateOpen;
            }

            return ignoreDefaultMoonStoryLock
                ? customGateOpen
                : baseGateOpen && customGateOpen;
        }

        private bool HaveVisitedAllMoons(string constellationName, IReadOnlyList<string> moonNames) {
            return moonNames != null
                && moonNames.Count > 0
                && moonNames.All(moonName => HasVisitedMoon(constellationName, moonName));
        }

        private bool HasVisitedMoon(string constellationName, string moonName) {
            if (!_moonNameToLevelIdLookup.ContainsKey(moonName)) {
                string warningKey = $"{constellationName}|{moonName}";
                if (_invalidVisitedMoonRuleWarnings.Add(warningKey)) {
                    Logger.LogWarning($"LethalConstellationsManager: Custom unlock condition for constellation '{constellationName}' references unknown moon '{moonName}'.");
                }

                return false;
            }

            return UnlockManager.Instance?.Unlocks.Any(unlock =>
                string.Equals(unlock.Name, moonName, StringComparison.OrdinalIgnoreCase)
                && unlock.VisitCount > 0) == true;
        }

        private static int CountUniqueVisitedMoons() {
            if (UnlockManager.Instance?.Unlocks == null) {
                return 0;
            }

            return UnlockManager.Instance.Unlocks.Count(unlock => unlock.VisitCount > 0);
        }

        private void SyncConstellationStateFromDefaultMoon(ClassMapper constellation, LMConstellationUnlockable constellationState, LMUnlockable defaultMoon, bool allowDefaultMoonNewDiscovery = false) {
            if (constellation == null || constellationState == null || defaultMoon == null) {
                return;
            }

            bool effectiveStoryGateOpen = IsConstellationStoryGateOpen(constellation.consName, constellationState, defaultMoon, out bool ignoreDefaultMoonStoryLock);
            bool constellationDiscovered = constellationState.Discovered;
            constellationState.StoryIsUnlocked = effectiveStoryGateOpen || constellationDiscovered;

            ApplyConstellationState(constellation, constellationState);
            SyncDefaultMoonAvailability(
                constellationState,
                defaultMoon,
                allowDefaultMoonNewDiscovery,
                releaseDefaultMoonStoryLock: defaultMoon.StoryUnlock && (!ignoreDefaultMoonStoryLock || defaultMoon.StoryIsUnlocked));
        }

        private void ApplyConstellationState(ClassMapper constellation, LMConstellationUnlockable constellationState) {
            if (constellation == null || constellationState == null) {
                return;
            }

            bool constellationVisible = IsConstellationAvailable(constellationState);
            constellation.isHidden = !constellationVisible;
            constellation.isLocked = !constellationVisible;
        }

        private ConstellationEconomyTarget CreateEconomyTarget(ClassMapper constellation, LMConstellationUnlockable state) {
            if (constellation == null || state == null) {
                return null;
            }

            state.UpdateFromConstellation(constellation);
            TryGetDefaultMoon(constellation.consName, out var defaultMoon);
            int effectiveOriginalPrice = GetEffectiveOriginalPrice(state, defaultMoon);
            state.IterateState(effectiveOriginalPrice);

            return new ConstellationEconomyTarget {
                Constellation = constellation,
                State = state,
                DefaultMoon = defaultMoon,
                EffectivePrice = state.RoutePrice,
                EffectiveOriginalPrice = effectiveOriginalPrice
            };
        }

        private static bool IsConstellationDiscovered(LMConstellationUnlockable state) {
            return state != null && state.Discovered;
        }

        private static bool IsConstellationAvailable(LMConstellationUnlockable state) {
            if (state == null) {
                return false;
            }

            if (!ConfigManager.DiscoveryMode) {
                return state.StoryIsUnlocked || state.Discovered;
            }

            return IsConstellationDiscovered(state);
        }

        private bool HasDiscoveredConstellations() {
            foreach (var constellation in _constellationLookup.Values) {
                if (TryGetConstellationState(constellation.consName, out var state) && IsConstellationAvailable(state)) {
                    return true;
                }
            }

            return false;
        }

        private static int GetEffectiveOriginalPrice(LMConstellationUnlockable state, LMUnlockable defaultMoon) {
            if (ConfigManager.LethalConstellationsOverridePrice && defaultMoon != null) {
                return defaultMoon.OriginalPrice;
            }

            return state?.OriginalPrice ?? 0;
        }

        private void SyncDefaultMoonAvailability(LMConstellationUnlockable constellationState, LMUnlockable defaultMoon, bool allowDefaultMoonNewDiscovery, bool releaseDefaultMoonStoryLock = true) {
            if (constellationState == null || defaultMoon == null) {
                return;
            }

            if (!constellationState.Discovered) {
                return;
            }

            if (releaseDefaultMoonStoryLock && defaultMoon.StoryUnlock) {
                defaultMoon.StoryIsUnlocked = true;
            }

            if (UnlockManager.Instance != null && UnlockManager.Instance.UseConstellationDiscovery) {
                defaultMoon.SetDiscoveryState(true, suppressNewDiscovery: !allowDefaultMoonNewDiscovery);
            } else {
                defaultMoon.Discovered = true;
                if (!defaultMoon.DiscoveredOnce) {
                    defaultMoon.DiscoveredOnce = true;
                    defaultMoon.NewDiscovery = allowDefaultMoonNewDiscovery;
                }
            }

        }

        private sealed class RecoveryConstellationCandidate {
            internal RecoveryConstellationCandidate(ClassMapper constellation, int effectivePrice) {
                Constellation = constellation;
                EffectivePrice = effectivePrice;
            }

            internal ClassMapper Constellation { get; }
            internal int EffectivePrice { get; }
        }

        private bool TryGetConstellationState(string constellationName, out LMConstellationUnlockable constellationState) {
            return _extension.TryGetConstellationState(constellationName, out constellationState);
        }

        private HashSet<int> GetAllVisibleMoonIds() {
            var visibleMoonIds = new HashSet<int>();
            if (UnlockManager.Instance == null || !UnlockManager.Instance.UseConstellationDiscovery || !Bootstrap()) {
                return visibleMoonIds;
            }

            foreach (var constellation in _constellationLookup.Values) {
                if (!TryGetConstellationState(constellation.consName, out var constellationState)
                    || !constellationState.Discovered) {
                    continue;
                }

                foreach (var unlock in GetVisibleConstellationUnlocks(constellation.consName)) {
                    if (TryGetLevelId(unlock, out var levelId)) {
                        visibleMoonIds.Add(levelId);
                    }
                }
            }

            return visibleMoonIds;
        }

        private List<LMUnlockable> GetConstellationUnlocks(ClassMapper constellation) {
            var matches = new List<LMUnlockable>();
            if (constellation == null || !_constellationMoonLevelLookup.TryGetValue(constellation.consName, out var constellationMoonIds)) {
                return matches;
            }

            foreach (var levelId in constellationMoonIds) {
                if (_unlockByLevelIdLookup.TryGetValue(levelId, out var unlock) && unlock != null) {
                    matches.Add(unlock);
                }
            }

            return matches;
        }

        private List<string> ResolveMoonNames(IEnumerable<int> moonLevelIds) {
            var moonNames = new List<string>();
            if (moonLevelIds == null) {
                return moonNames;
            }

            foreach (var levelId in moonLevelIds) {
                if (TryResolveMoonName(levelId, out var moonName)) {
                    moonNames.Add(moonName);
                }
            }

            return moonNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private bool TryResolveMoonName(int levelId, out string moonName) {
            moonName = string.Empty;
            if (!_unlockByLevelIdLookup.TryGetValue(levelId, out var unlock) || unlock == null || string.IsNullOrWhiteSpace(unlock.Name)) {
                return false;
            }

            moonName = unlock.Name;
            return true;
        }

        private static bool TryGetLevelId(LMUnlockable unlock, out int levelId) {
            levelId = -1;
            if (unlock?.ExtendedLevel?.SelectableLevel == null) {
                return false;
            }

            levelId = unlock.ExtendedLevel.SelectableLevel.levelID;
            return levelId >= 0;
        }

        private static bool IsEligibleForLocalRotation(LMUnlockable unlock) {
            if (unlock == null || unlock.PermanentlyDiscovered) {
                return false;
            }

            return (!unlock.OriginallyLocked && !unlock.OriginallyHidden && !unlock.StoryUnlock)
                   || (unlock.StoryUnlock && unlock.StoryIsUnlocked);
        }
    }
}
