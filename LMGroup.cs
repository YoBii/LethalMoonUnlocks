using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using UnityEngine.UIElements;

namespace LethalMoonUnlocks {
    internal readonly struct LMGroup() {
        internal string Name { get; init; } = string.Empty;
        internal List<LMUnlockable> Members { get; init; } = new List<LMUnlockable>();

    }
}
