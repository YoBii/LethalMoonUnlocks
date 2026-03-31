using System;

namespace LethalMoonUnlocks {
    [Serializable]
    internal sealed class ConstellationRouteSyncData {
        public string constellationName = string.Empty;
        public int chargedPrice;

        internal ConstellationRouteSyncData() {
        }

        internal ConstellationRouteSyncData(string constellationName, int chargedPrice) {
            this.constellationName = constellationName ?? string.Empty;
            this.chargedPrice = chargedPrice;
        }
    }
}
