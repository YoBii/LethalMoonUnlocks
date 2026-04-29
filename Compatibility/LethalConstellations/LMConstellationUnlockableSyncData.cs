using System;

namespace LethalMoonUnlocks.Compatibility {
    [Serializable]
    internal sealed class LMConstellationUnlockableSyncData {
        public string name = string.Empty;
        public bool storyIsUnlocked;
        public bool customConditionUnlocked;
        public bool discovered;
        public bool discoveredOnce;
        public bool newDiscovery;
        public int buyCount;
        public int visitCount;
        public int freeVisitCount;
        public int routePrice;
        public int originalPrice;
        public bool hasOriginalPriceSnapshot;
        public bool onSale;
        public int salesRate;
    }
}
