using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using UnityEngine.UIElements;

namespace LethalMoonUnlocks {
    internal readonly struct LMGroup() {
        public string Name { get; init; } = string.Empty;
        public List<LMUnlockable> Members { get; init; } = new List<LMUnlockable>();

    }
}
