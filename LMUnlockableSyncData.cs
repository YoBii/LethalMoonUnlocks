using System;

namespace LethalMoonUnlocks {
    [Serializable]
    internal sealed class LMUnlockableSyncData {
        public string name = string.Empty;
        public int routePrice;
        public int originalPrice;
        public bool originallyLocked;
        public bool originallyHidden;
        public bool remainingHidden;
        public bool storyUnlock;
        public bool storyIsUnlocked;
        public int buyCount;
        public int visitCount;
        public int freeVisitCount;
        public int landingCount;
        public bool discovered;
        public bool newDiscovery;
        public bool discoveredOnce;
        public bool permanentlyDiscovered;
        public bool onSale;
        public int salesRate;
    }
}
