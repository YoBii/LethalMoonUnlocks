using System;
using System.Collections.Generic;

namespace LethalMoonUnlocks.Compatibility {
    [Serializable]
    internal sealed class NamedStringListSyncData {
        public string name = string.Empty;
        public List<string> values = new();
    }
}
