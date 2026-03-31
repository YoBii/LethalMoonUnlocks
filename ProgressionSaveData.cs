using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalMoonUnlocks {
    [Serializable]
    internal class ProgressionSaveData {
        internal int PaintingsSold { get; set; }
        internal List<string> ReadBestiaryEntries { get; set; } = new();
        internal List<string> ReadStoryLogs { get; set; } = new();

        internal ProgressionSaveData Copy() {
            return new ProgressionSaveData {
                PaintingsSold = PaintingsSold,
                ReadBestiaryEntries = ReadBestiaryEntries?.ToList() ?? new List<string>(),
                ReadStoryLogs = ReadStoryLogs?.ToList() ?? new List<string>()
            };
        }

        internal bool HasData() {
            return PaintingsSold > 0
                || (ReadBestiaryEntries?.Count ?? 0) > 0
                || (ReadStoryLogs?.Count ?? 0) > 0;
        }
    }
}
