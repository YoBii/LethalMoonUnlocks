using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalMoonUnlocks.Compatibility {
    [Serializable]
    internal sealed class LethalConstellationsSyncData {
        public List<LMConstellationUnlockableSyncData> constellations = new();
        public List<NamedStringListSyncData> constellationRotationMoons = new();
        public List<NamedStringListSyncData> localConstellationDiscoveries = new();

        internal static LethalConstellationsSyncData FromSaveData(LethalConstellationsSaveData saveData) {
            var syncData = new LethalConstellationsSyncData();
            if (saveData == null) {
                return syncData;
            }

            if (saveData.Constellations != null) {
                foreach (var entry in saveData.Constellations
                             .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null)
                             .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)) {
                    var state = entry.Value.BuildSyncData();
                    state.name = entry.Key;
                    syncData.constellations.Add(state);
                }
            }

            syncData.constellationRotationMoons = BuildNamedLists(saveData.ConstellationRotationMoons);
            syncData.localConstellationDiscoveries = BuildNamedLists(saveData.LocalConstellationDiscoveries);
            return syncData;
        }

        internal LethalConstellationsSaveData ToSaveData() {
            var saveData = new LethalConstellationsSaveData();

            foreach (var state in constellations ?? new List<LMConstellationUnlockableSyncData>()) {
                if (state == null || string.IsNullOrWhiteSpace(state.name)) {
                    continue;
                }

                var constellationState = new LMConstellationUnlockable(state.name);
                constellationState.ApplySyncData(state);
                saveData.Constellations[state.name] = constellationState;
            }

            CopyNamedLists(constellationRotationMoons, saveData.ConstellationRotationMoons);
            CopyNamedLists(localConstellationDiscoveries, saveData.LocalConstellationDiscoveries);
            return saveData;
        }

        private static List<NamedStringListSyncData> BuildNamedLists(Dictionary<string, List<string>> source) {
            var result = new List<NamedStringListSyncData>();
            if (source == null) {
                return result;
            }

            foreach (var entry in source.Where(entry => !string.IsNullOrWhiteSpace(entry.Key)).OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)) {
                result.Add(new NamedStringListSyncData {
                    name = entry.Key,
                    values = entry.Value == null
                        ? new List<string>()
                        : entry.Value.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                });
            }

            return result;
        }

        private static void CopyNamedLists(IEnumerable<NamedStringListSyncData> source, Dictionary<string, List<string>> target) {
            if (source == null || target == null) {
                return;
            }

            foreach (var entry in source) {
                if (entry == null || string.IsNullOrWhiteSpace(entry.name)) {
                    continue;
                }

                target[entry.name] = entry.values == null
                    ? new List<string>()
                    : entry.values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
        }
    }
}
