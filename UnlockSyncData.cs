using System;
using System.Collections.Generic;
using LethalMoonUnlocks.Compatibility;

namespace LethalMoonUnlocks {
    [Serializable]
    internal sealed class UnlockSyncData {
        public List<LMUnlockable> Unlockables { get; set; } = new();
        public LethalConstellationsSaveData LethalConstellationsSaveData { get; set; } = new();

        internal UnlockSyncData() {
        }

        internal UnlockSyncData(List<LMUnlockable> unlockables, LethalConstellationsSaveData lethalConstellationsSaveData) {
            Unlockables = unlockables ?? new List<LMUnlockable>();
            LethalConstellationsSaveData = lethalConstellationsSaveData?.Copy() ?? new LethalConstellationsSaveData();
        }
    }
}
