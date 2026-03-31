using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalMoonUnlocks {
    internal class ProgressionManager {
        internal static ProgressionManager Instance { get; private set; } = null!;
        internal int PaintingsSold { get; set; }
        private readonly HashSet<string> _readBestiaryEntries = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _readStoryLogs = new(StringComparer.OrdinalIgnoreCase);
        internal IReadOnlyCollection<string> ReadBestiaryEntries => _readBestiaryEntries;
        internal IReadOnlyCollection<string> ReadStoryLogs => _readStoryLogs;

        internal ProgressionManager() {
            if (Instance == null) {
                Instance = this;
            }
            Reset();
        }

        internal ProgressionSaveData GetSaveData() {
            return new ProgressionSaveData() {
                PaintingsSold = PaintingsSold,
                ReadBestiaryEntries = _readBestiaryEntries.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList(),
                ReadStoryLogs = _readStoryLogs.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        internal void LoadSaveData(ProgressionSaveData progressionSaveData) {
            Reset();
            if (progressionSaveData == null) {
                return;
            }

            PaintingsSold = progressionSaveData.PaintingsSold;
            LoadEntries(progressionSaveData.ReadBestiaryEntries, _readBestiaryEntries);
            LoadEntries(progressionSaveData.ReadStoryLogs, _readStoryLogs);
        }

        internal void Reset() {
            PaintingsSold = 0;
            _readBestiaryEntries.Clear();
            _readStoryLogs.Clear();
        }

        internal bool RecordBestiaryRead(string entryName) {
            return RecordRead(_readBestiaryEntries, entryName, "bestiary");
        }

        internal bool RecordStoryLogRead(string entryName) {
            return RecordRead(_readStoryLogs, entryName, "story log");
        }

        internal bool HasReadBestiaryEntry(string entryName) {
            string normalizedName = NormalizeEntryName(entryName);
            return normalizedName.Length > 0 && _readBestiaryEntries.Contains(normalizedName);
        }

        internal bool HasReadStoryLog(string entryName) {
            string normalizedName = NormalizeEntryName(entryName);
            return normalizedName.Length > 0 && _readStoryLogs.Contains(normalizedName);
        }

        private static string NormalizeEntryName(string entryName) {
            return string.IsNullOrWhiteSpace(entryName) ? string.Empty : entryName.Trim();
        }

        private static void LoadEntries(IEnumerable<string> source, HashSet<string> target) {
            if (source == null) {
                return;
            }

            foreach (string entryName in source) {
                string normalizedName = NormalizeEntryName(entryName);
                if (normalizedName.Length > 0) {
                    target.Add(normalizedName);
                }
            }
        }

        private static bool RecordRead(HashSet<string> target, string entryName, string categoryName) {
            string normalizedName = NormalizeEntryName(entryName);
            if (normalizedName.Length == 0) {
                Logger.LogDebug($"Ignoring blank {categoryName} progression entry.");
                return false;
            }

            if (!target.Add(normalizedName)) {
                Logger.LogDebug($"{categoryName}: '{normalizedName}' was already recorded.");
                return false;
            }

            Logger.LogInfo($"Recorded {categoryName} progression entry '{normalizedName}'.");
            return true;
        }
    }
}
