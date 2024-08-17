using LethalConstellations.PluginCore;
using LethalMoonUnlocks.Util;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LethalMoonUnlocks.Compatibility {
    internal class LethalConstellationsExtension {
        internal LethalConstellationsExtension() {
            LethalConstellations.EventStuff.NewEvents.RouteConstellationSuccess.AddListener(BuyConstellations);
        }

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

        internal string GetConstellationName(LMUnlockable unlock) {
            ClassMapper constellation = Collections.ConstellationStuff.Where(constellation => constellation.constelMoons.Any(moon => moon == unlock.Name)).FirstOrDefault();
            if (constellation != null)
            {
                return constellation.consName;
            }
            return string.Empty;
        }

        internal List<LMUnlockable> GetConstellationMatchesForMoon(LMUnlockable matchingUnlock, List<LMUnlockable> unlocksToMatch) {
            ClassMapper constellation = Collections.ConstellationStuff.Where(con => con.constelMoons.Any(moon => moon == matchingUnlock.Name)).FirstOrDefault();
            List<LMUnlockable> constellationMatches = new List<LMUnlockable>();
            if (constellation != null) {
                foreach (var moon in constellation.constelMoons) {
                    var unlock = unlocksToMatch.Where(unlock => unlock.Name == moon).FirstOrDefault();
                    if (unlock != null) {
                        constellationMatches.Add(unlock);
                    }
                }
            }
            return constellationMatches;
        }

        private void BuyConstellations() {
            //ClassMapper currentConstellation = Collections.ConstellationStuff.Where(constellation => constellation.consName == Collections.CurrentConstellation).FirstOrDefault();
            //TIL: Linq generates 'DisplayClasses' from lambda expressions. If those classes are of or contain (idk) a referenced type that's not present and they are picked up via reflection at runtime.. things can go wrong
            ClassMapper currentConstellation = null;
            foreach (var constellation in Collections.ConstellationStuff) {
                if (constellation.consName == Collections.CurrentConstellation) {
                    currentConstellation = constellation;
                    break;
                }
            }
            if (currentConstellation == null) return;
            string constellationDefaultMoon = currentConstellation.defaultMoon;

            var unlock = UnlockManager.Instance.Unlocks.Where(unlock => unlock.ExtendedLevel.NumberlessPlanetName == constellationDefaultMoon).FirstOrDefault();
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

        private void ApplyVisibility() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff) {
                bool constellationIsDiscovered = false;
                foreach (string moon in constellation.constelMoons) {
                    if (UnlockManager.Instance.Unlocks.Any(unlock => unlock.Discovered && unlock.Name == moon)) {
                        constellationIsDiscovered = true;
                        break;
                    }
                }
                if (constellationIsDiscovered) {
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

        private void ApplyDefaultMoons() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff) {
                if (constellation.isHidden) continue;
                List<string> constellationMoons = constellation.constelMoons;
                var constellationUnlocks = UnlockManager.Instance.Unlocks.Where(unlock => unlock.Discovered && constellationMoons.Any(moon => unlock.Name == moon)).OrderBy(unlock => unlock.ExtendedLevel.RoutePrice).ToList();
                constellation.defaultMoon = constellationUnlocks.First().Name;
                Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: set default moon to {constellationUnlocks.First().Name} ({constellation.defaultMoon})");
            }
        }

        private void AddDiscoveryCount() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff) {
                if (constellation.isHidden) continue;
                List<string> constellationMoons = constellation.constelMoons;
                var constellationUnlocks = UnlockManager.Instance.Unlocks.Where(unlock => unlock.Discovered && constellationMoons.Any(moon => unlock.Name == moon)).ToList();
                constellation.optionalParams = $"\nMoons discovered: {constellationUnlocks.Count}";
                if (constellationUnlocks.Count == constellation.constelMoons.Count) {
                    constellation.optionalParams = $"\nAll moons discovered!";
                }
            }
        }

        private void ApplyPrices() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff) {
                if (constellation.isHidden) continue;
                string constellationDefaultMoon = constellation.defaultMoon;
                var constellationDefaultMoonUnlock = UnlockManager.Instance.Unlocks.Where(unlock => unlock.Name == constellationDefaultMoon).FirstOrDefault();
                if (constellationDefaultMoonUnlock != null) {
                    constellation.constelPrice = constellationDefaultMoonUnlock.ExtendedLevel.RoutePrice;
                    Plugin.Instance.Mls.LogDebug($"Constellation {constellation.consName}: set constellation price to {constellation.constelPrice}");
                }
            }
        }

        private void HideUnlocksNotInCurrentConstellation() {
            ClassMapper currentConstellation = null;
            foreach (var constellation in Collections.ConstellationStuff) {
                if (constellation.consName == Collections.CurrentConstellation) {
                    currentConstellation = constellation;
                    break;
                }
            }
            if (currentConstellation == null) return;
            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                if (currentConstellation.constelMoons.All(moon => moon != unlock.Name)) {
                    unlock.ExtendedLevel.IsRouteHidden = true;
                    unlock.ExtendedLevel.IsRouteLocked = true;
                }
            }
        }

        private void ShowUnlocksInCurrentConstellation() {
            ClassMapper currentConstellation = null;
            foreach (var constellation in Collections.ConstellationStuff) {
                if (constellation.consName == Collections.CurrentConstellation) {
                    currentConstellation = constellation;
                    break;
                }
            }
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
