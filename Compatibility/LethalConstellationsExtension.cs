using LethalConstellations.PluginCore;
using LethalMoonUnlocks.Util;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LethalMoonUnlocks.Compatibility {
    internal class LethalConstellationsExtension {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal LethalConstellationsExtension() {
            LethalConstellations.EventStuff.NewEvents.RouteConstellationSuccess.AddListener(BuyConstellations);
            //Constellations = Collections.ConstellationStuff;
        }

        //[NonSerialized]
        //List<ClassMapper> Constellations;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void ApplyUnlocks() {
            if (ConfigManager.DiscoveryMode) {
                ApplyVisibility();
                ApplyDefaultMoons();
                AddDiscoveryCount();
                HideUnlocksNotInCurrentConstellation();
            } else {
                HideUnlocksNotInCurrentConstellation();
                ShowUnlocksInCurrentConstellation();
            }
            if (ConfigManager.LethalConstellationsOverridePrice) {
                ApplyPrices();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal string GetConstellationName(LMUnlockable unlock) {
            ClassMapper constellation = Collections.ConstellationStuff.Where(con => con.constelMoons.Any(moon => moon == unlock.Name)).FirstOrDefault();
            if (constellation != null)
            {
                return constellation.consName;
            }
            return string.Empty;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void BuyConstellations() {
            var currentConstellation = Collections.ConstellationStuff.Where(c => c.consName == Collections.CurrentConstellation).FirstOrDefault();
            var unlock = UnlockManager.Instance.Unlocks.Where(unlock => unlock.ExtendedLevel.NumberlessPlanetName == currentConstellation.defaultMoon).FirstOrDefault();
            if (unlock != null) {
                Plugin.Instance.Mls.LogInfo($"Routing to constellation {currentConstellation.consName} -> default moon {unlock.Name} with ID {unlock.ExtendedLevel.SelectableLevel.levelID}!");
                if (unlock.ExtendedLevel.RoutePrice > 0) {
                    if (ConfigManager.LethalConstellationsOverridePrice) {
                        Plugin.Instance.Mls.LogInfo($"Route to {unlock.Name} was paid ({unlock.ExtendedLevel.RoutePrice} credits).");
                    }
                    if (NetworkManager.Instance.IsServer()) {
                        UnlockManager.Instance.BuyMoon(unlock.Name);
                    } else {
                        NetworkManager.Instance.ClientBuyMoon(unlock.Name);
                    }
                } else {
                    Plugin.Instance.Mls.LogInfo($"Route to {unlock.Name} was free.");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyVisibility() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff) {
                if (constellation.constelMoons.Any(constellation => UnlockManager.Instance.Unlocks.Where(unlock => unlock.Discovered).Any(unlock => constellation == unlock.Name))) {
                    if (constellation.isHidden == true && NetworkManager.Instance.IsServer()) {
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = "New Discovery!", Text = $"{LethalConstellations.ConfigManager.Configuration.ConstellationWord.Value} <color=yellow>{constellation.consName}</color> available for routing.", IsWarning = true, Key = "LMU_ConstellationDiscovered" });
                    }
                    constellation.isHidden = false;
                    constellation.isLocked = false;
                    Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: There are discovered moons in this constellation. Constellation is also discovered.");
                } else {
                    constellation.isHidden = true;
                    constellation.isLocked = true;
                    Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: No moon in this constellation is discovered. Hiding constellation.");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyDefaultMoons() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff.Where(constellation => constellation.isHidden == false)) {
                var constellationUnlocks = UnlockManager.Instance.Unlocks.Where(unlock => unlock.Discovered && constellation.constelMoons.Any(moon => unlock.Name == moon)).OrderBy(unlock => unlock.ExtendedLevel.RoutePrice).ToList();
                constellation.defaultMoon = constellationUnlocks.First().Name;
                Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: set default moon to {constellationUnlocks.First().Name} ({constellation.defaultMoon})");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddDiscoveryCount() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff.Where(constellation => constellation.isHidden == false)) {
                var constellationUnlocks = UnlockManager.Instance.Unlocks.Where(unlock => unlock.Discovered && constellation.constelMoons.Any(moon => unlock.Name == moon)).ToList();
                constellation.optionalParams = $"\nMoons discovered: {constellationUnlocks.Count}";
                if (constellationUnlocks.Count == constellation.constelMoons.Count) {
                    constellation.optionalParams = $"\nAll moons discovered!";
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyPrices() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff.Where(constellation => constellation.isHidden == false)) {
                var constellationDefaultMoonUnlock = UnlockManager.Instance.Unlocks.Where(unlock => unlock.Name == constellation.defaultMoon).FirstOrDefault();
                if (constellationDefaultMoonUnlock != null) {
                    constellation.constelPrice = constellationDefaultMoonUnlock.ExtendedLevel.RoutePrice;
                    Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: set constellation price to {constellation.constelPrice}");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HideUnlocksNotInCurrentConstellation() {
            var currentConstellation = Collections.ConstellationStuff.Where(constellation => constellation.consName == Collections.CurrentConstellation).FirstOrDefault();
            if (currentConstellation == null) return;
            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                if (currentConstellation.constelMoons.All(moon => moon != unlock.Name)) {
                    unlock.ExtendedLevel.IsRouteHidden = true;
                    unlock.ExtendedLevel.IsRouteLocked = true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ShowUnlocksInCurrentConstellation() {
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
