using LethalConstellations.PluginCore;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalMoonUnlocks.Compatibility {
    [Serializable]
    [ES3Serializable]
    internal class LethalConstellationsSaveData {
        [ES3Serializable] internal Dictionary<string, LMConstellationUnlockable> Constellations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [ES3Serializable] internal Dictionary<string, List<string>> ConstellationRotationMoons { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [ES3Serializable] internal Dictionary<string, List<string>> LocalConstellationDiscoveries { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        internal LMConstellationUnlockable GetOrCreate(string constellationName) {
            if (string.IsNullOrWhiteSpace(constellationName)) {
                return null;
            }

            Constellations ??= new Dictionary<string, LMConstellationUnlockable>(StringComparer.OrdinalIgnoreCase);
            if (!Constellations.TryGetValue(constellationName, out var constellationState) || constellationState == null) {
                constellationState = new LMConstellationUnlockable(constellationName);
                Constellations[constellationName] = constellationState;
            }

            return constellationState;
        }

        internal bool TryGet(string constellationName, out LMConstellationUnlockable constellationState) {
            constellationState = null;
            if (string.IsNullOrWhiteSpace(constellationName)) {
                return false;
            }

            Constellations ??= new Dictionary<string, LMConstellationUnlockable>(StringComparer.OrdinalIgnoreCase);
            return Constellations.TryGetValue(constellationName, out constellationState) && constellationState != null;
        }

        internal void Set(LMConstellationUnlockable constellationState) {
            if (constellationState == null || string.IsNullOrWhiteSpace(constellationState.Name)) {
                return;
            }

            Constellations ??= new Dictionary<string, LMConstellationUnlockable>(StringComparer.OrdinalIgnoreCase);
            Constellations[constellationState.Name] = constellationState;
        }

        internal void SynchronizeDefinitions(IEnumerable<ClassMapper> constellations) {
            if (constellations == null) {
                return;
            }

            foreach (var constellation in constellations) {
                if (constellation == null || string.IsNullOrWhiteSpace(constellation.consName)) {
                    continue;
                }

                GetOrCreate(constellation.consName)?.UpdateFromConstellation(constellation);
            }
        }

        internal bool HasData() {
            return (Constellations?.Values.Any(constellation => constellation != null && constellation.HasData()) ?? false)
                || (ConstellationRotationMoons?.Any(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null && entry.Value.Count > 0) ?? false)
                || (LocalConstellationDiscoveries?.Any(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null && entry.Value.Count > 0) ?? false);
        }

        internal LethalConstellationsSaveData Copy() {
            var copy = new LethalConstellationsSaveData();
            if (Constellations != null) {
                foreach (var constellation in Constellations) {
                    if (constellation.Value == null) {
                        continue;
                    }

                    copy.Constellations[constellation.Key] = constellation.Value.Clone();
                }
            }

            if (ConstellationRotationMoons != null) {
                foreach (var entry in ConstellationRotationMoons) {
                    if (string.IsNullOrWhiteSpace(entry.Key)) {
                        continue;
                    }

                    copy.ConstellationRotationMoons[entry.Key] = entry.Value == null
                        ? new List<string>()
                        : entry.Value.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                }
            }
            if (LocalConstellationDiscoveries != null) {
                foreach (var entry in LocalConstellationDiscoveries) {
                    if (string.IsNullOrWhiteSpace(entry.Key)) {
                        continue;
                    }

                    copy.LocalConstellationDiscoveries[entry.Key] = entry.Value == null
                        ? new List<string>()
                        : entry.Value.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                }
            }

            return copy;
        }

        internal void Clear() {
            Constellations?.Clear();
            ConstellationRotationMoons?.Clear();
            LocalConstellationDiscoveries?.Clear();
        }
    }
}
