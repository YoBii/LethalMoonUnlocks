using System;
using System.Collections.Generic;
using System.Text;
using TerminalStuff.SpecialStuff;

namespace LethalMoonUnlocks.Compatibility {
    internal static class TerminalStuffCompatibility {
        internal static void ApplyAdditionalInfo(LMUnlockable unlock) {
            if (MoonsPlus.TryGetMoon(unlock.ExtendedLevel.SelectableLevel, out MoonInfo moonInfo)) {
                Logger.LogDebug($"Setting MoonsPlus additional for: {unlock.Name}");
                moonInfo.AdditionalInfo = unlock.GetMoonTagsText();
            }
        }
    }
}
