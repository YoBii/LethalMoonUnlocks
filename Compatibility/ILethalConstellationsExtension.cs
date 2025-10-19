using System;
using System.Collections.Generic;
using System.Text;

namespace LethalMoonUnlocks.Compatibility {
    public interface ILethalConstellationsExtension {
        void ApplyUnlocks();
        string GetConstellationName(LMUnlockable unlock);
        List<LMUnlockable> GetConstellationMatchesForMoon(LMUnlockable matchingUnlock, List<LMUnlockable> unlocksToMatch);
        LMGroup GetCheapestUndiscoveredConstellation();
    }
}
