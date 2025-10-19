using LethalConstellations.PluginCore;
using LethalMoonUnlocks;
using LethalMoonUnlocks.Util;
using System.Collections.Generic;
using System.Linq;

namespace LethalMoonUnlocks.Compatibility {
    public class LethalConstellationsExtension : ILethalConstellationsExtension {
        public LethalConstellationsExtension() {
            LethalConstellations.EventStuff.NewEvents.RouteConstellationSuccess.AddListener(OnConstellationBought);
        }

        public void ApplyUnlocks() {
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

        public string GetConstellationName(LMUnlockable unlock) {
            ClassMapper constellation = Collections.ConstellationStuff.Where(constellation => constellation.constelMoons.Any(moon => moon == unlock.Name)).FirstOrDefault();
            if (constellation != null) {
                return constellation.consName;
            }
            return string.Empty;
        }

        public List<LMUnlockable> GetConstellationMatchesForMoon(LMUnlockable matchingUnlock, List<LMUnlockable> unlocksToMatch) {
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

        public LMGroup GetCheapestUndiscoveredConstellation() {
            LMGroup group = new LMGroup();
            foreach (var constellation in Collections.ConstellationStuff.OrderBy(c => c.constelPrice)) {
                Logger.LogInfo($"Got cheapest constellation: {constellation.consName}");
                if (constellation.isHidden) {
                    List<LMUnlockable> constellationUnlockables = new List<LMUnlockable>();
                    foreach (string moon in constellation.constelMoons) {
                        foreach (var unlock in UnlockManager.Instance.Unlocks) {
                            if (unlock.Name == moon) {
                                constellationUnlockables.Add(unlock);
                            }
                        }
                    }
                    group = new LMGroup() { Members = constellationUnlockables, Name = constellation.consName };
                    break;
                } else {
                    Logger.LogInfo($"Constellation already discovered. Try next..");
                }
            }
            return group;
        }

        private void OnConstellationBought() {
            //ClassMapper currentConstellation = Collections.ConstellationStuff.Where(constellation => constellation.consName == Collections.CurrentConstellation).FirstOrDefault();
            //TIL: Linq generates 'DisplayClasses' from lambda expressions. If those classes are of or contain (idk) a referenced type that's not present and they are picked up via reflection at runtime.. TypeLoadException
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
                Logger.LogInfo($"Routing to constellation {currentConstellation.consName} -> default moon {unlock.Name} with ID {unlock.ExtendedLevel.SelectableLevel.levelID}!");
                if (unlock.ExtendedLevel.RoutePrice > 0) {
                    if (ConfigManager.LethalConstellationsOverridePrice) {
                        Logger.LogInfo($"Route to {unlock.Name} was paid ({unlock.ExtendedLevel.RoutePrice} credits).");
                    }
                    if (NetworkManager.Instance.IsServer()) {
                        UnlockManager.Instance.BuyMoon(unlock.Name);
                    } else {
                        NetworkManager.Instance.ClientBuyMoon(unlock.Name);
                    }
                } else {
                    Logger.LogInfo($"Route to {unlock.Name} was free.");
                }
            }
        }

        private bool AllAvailableConstellationsBought() {
            if (Collections.ConstellationStuff.Any(c => !c.isHidden && !c.isLocked && c.buyOnce && !c.oneTimePurchase))
                return false;
            else
                return true;
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
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = "New Discovery!", Text = $"{LethalConstellations.ConfigManager.Configuration.ConstellationWord.Value} <color=yellow>{constellation.consName}</color> available for routing.", IsWarning = true, Key = "LMU_ConstellationDiscovered", ExceptWhenKey = "LMU_NewQuotaDiscoveryGroup" });
                    }
                    constellation.isHidden = false;
                    constellation.isLocked = false;
                    Logger.LogDebug($"Constellation {constellation.consName}: There are discovered moons in this constellation. Constellation is also discovered.");
                } else {
                    constellation.isHidden = true;
                    constellation.isLocked = true;
                    Logger.LogDebug($"Constellation {constellation.consName}: No moon in this constellation is discovered. Hiding constellation.");
                }
            }
        }

        private void ApplyDefaultMoons() {
            foreach (ClassMapper constellation in Collections.ConstellationStuff) {
                if (constellation.isHidden) continue;
                List<string> constellationMoons = constellation.constelMoons;
                var constellationUnlocks = UnlockManager.Instance.Unlocks.Where(unlock => unlock.Discovered && constellationMoons.Any(moon => unlock.Name == moon)).OrderBy(unlock => unlock.ExtendedLevel.RoutePrice).ToList();
                if (constellationUnlocks.Count > 0) {
                    constellation.defaultMoon = constellationUnlocks.First().Name;
                    constellation.defaultMoonLevel = constellationUnlocks.First().ExtendedLevel;
                    Logger.LogDebug($"Constellation {constellation.consName}: set default moon to {constellation.defaultMoon}");
                }
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
                    Logger.LogDebug($"Constellation {constellation.consName}: set constellation price to {constellation.constelPrice}");
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
                    unlock.LockAndHide();
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
                    unlock.Unlock();
                    Logger.LogDebug($"Making moon {unlock.Name} routable as part of the current constellation! (May be hidden)");
                }
            }
        }
    }
}
