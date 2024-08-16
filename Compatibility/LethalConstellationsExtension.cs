using LethalConstellations.PluginCore;
using LethalMoonUnlocks.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LethalMoonUnlocks.Compatibility {
    internal class LethalConstellationsExtension {
        internal static void ApplyUnlocks() {
            if (Plugin.LethalConstellationsPresent) {
                if (ConfigManager.DiscoveryMode) {
                    ApplyVisibility();
                }
                ApplyDefaultMoons();
                HideUnlocksNotInCurrentConstellation();
                } else {
                    HideUnlocksNotInCurrentConstellation();
                    ShowUnlocksInCurrentConstellation();
            }
                if (ConfigManager.LethalConstellationsOverridePrice) {
                    ApplyPrices();
        }
            }
        }
        private static void ApplyVisibility() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff) {
                if (constellation.constelMoons.Any(constellation => UnlockManager.Instance.Unlocks.Where(unlock => unlock.Discovered).Any(unlock => constellation == unlock.Name))) {
                    if (constellation.isHidden == true && NetworkManager.Instance.IsServer()) {
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = "New Discovery!", Text = $"{LethalConstellations.ConfigManager.Configuration.ConstellationWord.Value} <color=yellow>{constellation.consName}</color> available for routing.", IsWarning = true, Key = "LMU_ConstellationDiscovered" });
                    }
                    constellation.isHidden = false;
                    Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: There are discovered moons in this constellation. Constellation is also discovered.");
                } else {
                    constellation.isHidden = true;
                    Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: No moon in this constellation is discovered. Hiding constellation.");
                }
            }
        }
        private static void ApplyDefaultMoons() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff.Where(constellation => constellation.isHidden == false)) {
                var constellationUnlocks = UnlockManager.Instance.Unlocks.Where(unlock => unlock.Discovered && constellation.constelMoons.Any(moon => unlock.Name == moon)).OrderBy(unlock => unlock.ExtendedLevel.RoutePrice);
                constellation.defaultMoon = constellationUnlocks.First().Name;
                constellation.constelPrice = constellationUnlocks.First().ExtendedLevel.RoutePrice;
                Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: set default moon to {constellationUnlocks.First().Name} ({constellation.defaultMoon})");
            }
        private static void ApplyPrices() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff.Where(constellation => constellation.isHidden == false)) {
                var constellationDefaultMoonUnlock = UnlockManager.Instance.Unlocks.Where(unlock => unlock.Name == constellation.defaultMoon).FirstOrDefault();
                if (constellationDefaultMoonUnlock != null) {
                    constellation.constelPrice = constellationDefaultMoonUnlock.ExtendedLevel.RoutePrice;
                    Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: set constellation price to {constellation.constelPrice}");
        }
            }
        }
        private static void HideUnlocksNotInCurrentConstellation() {
            var currentConstellation = Collections.ConstellationStuff.Where(constellation => constellation.consName == Collections.CurrentConstellation).FirstOrDefault();
            if (currentConstellation == null) return;
            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                if (currentConstellation.constelMoons.All(moon => moon != unlock.Name)) {
                    unlock.ExtendedLevel.IsRouteHidden = true;
                    unlock.ExtendedLevel.IsRouteLocked = true;
                }
            }
        }
        private static void ShowUnlocksInCurrentConstellation() {
            var currentConstellation = Collections.ConstellationStuff.Where(constellation => constellation.consName == Collections.CurrentConstellation).FirstOrDefault();
            if (currentConstellation == null) return;
            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                if (currentConstellation.constelMoons.Any(moon => moon == unlock.Name)) {
                    if (unlock.OriginallyHidden) {
                        unlock.ExtendedLevel.IsRouteHidden = true;
                    } else {
                        unlock.ExtendedLevel.IsRouteHidden = false;
                    }
                    unlock.ExtendedLevel.IsRouteLocked = false;
                    Plugin.Instance.Mls.LogDebug($"Showing moon {unlock.Name} as part of the current constellation!");
                }
            }
        }
    }
}
