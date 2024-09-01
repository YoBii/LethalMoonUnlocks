using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalMoonUnlocks.Util {
    [Serializable]
    internal class Notification {
        internal Notification() { }
        [SerializeField]
        internal string Header { get; init; } = "";
        [SerializeField]
        internal string Text { get; init; } = "";
        [SerializeField]
        internal bool IsWarning { get; init; } = false;
        [SerializeField]
        internal bool UseSave { get; init; } = false;
        [SerializeField]
        internal string Key { get; init; } = "LMU_";
        [SerializeField]
        internal string ExceptWhenKey { get; init; } = "";
    }
}
