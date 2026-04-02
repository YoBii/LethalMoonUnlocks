using System;
using System.Collections.Generic;
using LethalMoonUnlocks.Compatibility;

namespace LethalMoonUnlocks {
    [Serializable]
    internal sealed class UnlockSyncData {
        public List<LMUnlockableSyncData> Unlockables = new();
        public LethalConstellationsSyncData LethalConstellationsSyncData = new();

        internal UnlockSyncData() {
        }

        internal UnlockSyncData(List<LMUnlockableSyncData> unlockables, LethalConstellationsSyncData lethalConstellationsSyncData) {
            Unlockables = unlockables ?? new List<LMUnlockableSyncData>();
            LethalConstellationsSyncData = lethalConstellationsSyncData ?? new LethalConstellationsSyncData();
        }
    }
}
