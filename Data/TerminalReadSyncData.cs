using System;

namespace LethalMoonUnlocks {
    internal enum TerminalReadKind {
        Bestiary,
        StoryLog
    }

    [Serializable]
    internal sealed class TerminalReadSyncData {
        public TerminalReadKind readKind;
        public string entryName = string.Empty;

        internal TerminalReadSyncData() {
        }

        internal TerminalReadSyncData(TerminalReadKind readKind, string entryName) {
            this.readKind = readKind;
            this.entryName = entryName ?? string.Empty;
        }
    }
}
