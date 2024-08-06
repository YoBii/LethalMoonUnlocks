using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Util {
    internal class Notification {
        internal Notification() {
        }
        internal string Header { get; init; } = "";
        internal string Text { get; init; } = "";
        internal bool IsWarning { get; init; } = false;
        internal bool UseSave { get; init; } = false;
        internal string Key { get; init; } = "LMU_";
    }
}
