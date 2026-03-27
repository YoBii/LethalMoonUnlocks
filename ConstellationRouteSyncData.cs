using System;

namespace LethalMoonUnlocks {
    [Serializable]
    internal sealed class ConstellationRouteSyncData {
        public string ConstellationName { get; set; } = string.Empty;
        public int ChargedPrice { get; set; }

        internal ConstellationRouteSyncData() {
        }

        internal ConstellationRouteSyncData(string constellationName, int chargedPrice) {
            ConstellationName = constellationName ?? string.Empty;
            ChargedPrice = chargedPrice;
        }
    }
}
