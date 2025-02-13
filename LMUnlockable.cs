using LethalLevelLoader;
using LethalMoonUnlocks.Compatibility;
using LethalMoonUnlocks.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalMoonUnlocks {
    [Serializable]
    [ES3Serializable]
    internal class LMUnlockable {
        [ES3NonSerializable] [NonSerialized] internal ExtendedLevel ExtendedLevel;
        [SerializeField] [ES3Serializable] internal string Name { get; private set; }
        [ES3NonSerializable] public int OriginalPrice { get; private set; }
        internal bool OriginallyLocked { get {
                if (ConfigManager.OverrideLocked) return ConfigManager.OverrideLockedListMoons.Contains(Name);
                else return _originallyLocked; 
            } private set {
                _originallyLocked = value;
            }
        }
        internal bool OriginallyHidden { get {
                if (ConfigManager.OverrideHidden) return ConfigManager.OverrideHiddenListMoons.Contains(Name);
                else return _originallyHidden;
            } private set {
                _originallyHidden = value;
            }
        }
        [SerializeField] [ES3Serializable] private bool _originallyLocked; 
        [SerializeField] [ES3Serializable] private bool _originallyHidden; 

        [SerializeField] [ES3Serializable] internal bool RemainingHidden { get; set; }
        [SerializeField] [ES3Serializable] internal bool StoryUnlock { get; private set; }
        [SerializeField] [ES3Serializable] internal bool StoryIsUnlocked { get; set; }
        [SerializeField] [ES3Serializable] internal int BuyCount { get; set; }
        [SerializeField] [ES3Serializable] internal int VisitCount { get; set; }
        [SerializeField] [ES3Serializable] internal int FreeVisitCount { get; set; }
        [SerializeField] [ES3Serializable] internal int LandingCount { get; set; }
        [SerializeField] [ES3Serializable] internal bool Discovered { get; set; }
        [SerializeField] [ES3Serializable] internal bool NewDiscovery { get; set; }
        [SerializeField] [ES3Serializable] internal bool DiscoveredOnce { get; set; }
        [SerializeField] [ES3Serializable] internal bool PermanentlyDiscovered { get; set; }
        [SerializeField] [ES3Serializable] internal bool OnSale { get; set; }
        [SerializeField] [ES3Serializable] internal int SalesRate { get; set; }
        
        [SerializeField] [ES3NonSerializable] private int RoutePrice{ get; set; }

        internal LMUnlockable(ExtendedLevel extendedLevel) {
            Name = extendedLevel.NumberlessPlanetName;
            ExtendedLevel = extendedLevel;
            OriginalPrice = extendedLevel.RoutePrice;
            OriginallyHidden = extendedLevel.IsRouteHidden;
            OriginallyLocked = extendedLevel.IsRouteLocked;
            if (OriginallyHidden && !OriginallyLocked) RemainingHidden = true;
        }

        internal void OverrideData(LMUnlockable newData) {
            if (Name != newData.Name) {
                Logger.LogError("Name mismatch during override LMUnlockable data!");
            } else {
                OriginallyHidden = newData.OriginallyHidden;
                OriginallyLocked = newData.OriginallyLocked;
                RemainingHidden = newData.RemainingHidden;
                StoryUnlock = newData.StoryUnlock;
                StoryIsUnlocked = newData.StoryIsUnlocked;
                BuyCount = newData.BuyCount;
                VisitCount = newData.VisitCount;
                FreeVisitCount = newData.FreeVisitCount;
                LandingCount = newData.LandingCount;
                Discovered = newData.Discovered;
                NewDiscovery = newData.NewDiscovery;
                DiscoveredOnce = newData.DiscoveredOnce;
                PermanentlyDiscovered = newData.PermanentlyDiscovered;
                OnSale = newData.OnSale;
                SalesRate = newData.SalesRate;
                RoutePrice = newData.RoutePrice;
            }
        }

        internal void Unlock() {
            ExtendedLevel.IsRouteLocked = false;
            if (RemainingHidden) {
                ExtendedLevel.IsRouteHidden = true;
            } else {
                ExtendedLevel.IsRouteHidden = false;
            }
        }

        internal void LockAndHide() {
            ExtendedLevel.IsRouteHidden = true;
            ExtendedLevel.IsRouteLocked = true;
        }

        internal void ApplyPrice() {
            // only apply price if we have to for compatibility with LQ
            if (RoutePrice != OriginalPrice) {
                ExtendedLevel.RoutePrice = RoutePrice;
            }
        }

        internal void RefreshSale() {
            int rnd = UnityEngine.Random.Range(0, 100);
            if (rnd < ConfigManager.SalesChance && ExtendedLevel.RoutePrice > 0) {
                OnSale = true;
                SalesRate = ConfigManager.SalesRate;
                Logger.LogDebug($"{Name} is on SALE for {SalesRate}% OFF!");
            } else {
                OnSale = false;
                SalesRate = 0;
            }
        }

        internal void IterateState() {
            RoutePrice = CalculatePrice();

            // set permanently discovered if moon was bought (if config enabled)
            if (BuyCount > 0 && ((ConfigManager.UnlockMode && !ConfigManager.DiscountMode && ConfigManager.DiscoveryKeepUnlocks) || (ConfigManager.DiscountMode && ConfigManager.DiscoveryKeepDiscounts))) {
                if (!PermanentlyDiscovered) {   
                    PermanentlyDiscovered = true;
                    Logger.LogInfo($"{Name} set to permanently discovered because it's {(ConfigManager.UnlockMode ? "unlocked" : "discounted.")}");
                }
            }

            // set permanently discovered if free moon was landed on x times (if config enabled)
            if (Discovered && !OriginallyHidden && !OriginallyLocked && OriginalPrice == 0 && ConfigManager.PermanentlyDiscoverFreeMoonsOnLanding >= 0) {
                if (LandingCount >= ConfigManager.PermanentlyDiscoverFreeMoonsOnLanding) {
                    if (!PermanentlyDiscovered) {
                        PermanentlyDiscovered = true;
                        Logger.LogInfo($"{Name} set to permanently discovered because it's been landed on {LandingCount} times.");
                    }
                }
            }

            // set permanently discovered if paid moon was landed on x times (if config enabled)
            if (Discovered && !OriginallyHidden && !OriginallyLocked && OriginalPrice > 0 && ConfigManager.PermanentlyDiscoverPaidMoonsOnLanding >= 0) {
                if (LandingCount >= ConfigManager.PermanentlyDiscoverPaidMoonsOnLanding) {
                    if (!PermanentlyDiscovered) {
                        PermanentlyDiscovered = true;
                        Logger.LogInfo($"{Name} set to permanently discovered because it's been landed on {LandingCount} times.");
                    }
                }
            }
            
            // un-hide after # visits (if config enabled)
            if (OriginallyHidden && !OriginallyLocked) {
                if (ConfigManager.PermanentlyDiscoverHiddenMoonsOnVisit && VisitCount > 0) {
                    RemainingHidden = false;
                    PermanentlyDiscovered = true;
                }
                RemainingHidden = true;
                PermanentlyDiscovered = false;
            } else {
                RemainingHidden = false;
            }

            // tag new discoveries as new
            if (!DiscoveredOnce && Discovered) {
                NewDiscovery = true;
                DiscoveredOnce = true;
            }

            // LMU Story
            // Artifice condition
            if (Name == "Adamance" && LandingCount > 2) {
                var art = UnlockManager.Instance.Unlocks.FirstOrDefault(u => u.Name == "Artifice");
                if (art != null && art.StoryUnlock && art.StoryIsUnlocked == false) {
                    UnlockManager.TryReleaseStoryLock(art.Name);
                }
            }
            // Embrion condition (old bird id = 18)
            if (Name == "Embrion" && UnlockManager.Instance.Terminal.scannedEnemyIDs.Contains(18)) {
                    if (StoryUnlock && StoryIsUnlocked == false) {
                    UnlockManager.TryReleaseStoryLock(this.Name);
                }
            }
        }

        internal void ApplyVisibility() {
            // special case: story locked moons
            if (StoryUnlock) {
                if (StoryIsUnlocked) {
                    if (!ConfigManager.DiscoveryMode) {
                        Unlock();
                        return;
                    }
                    if (ConfigManager.DiscoveryMode && (Discovered || PermanentlyDiscovered)) {
                        Unlock();
                        return;
                    }
                }
                LockAndHide();
                return;
            }

            // special case originally locked
            if (OriginallyLocked) {
                LockAndHide();
                return;
            }

            // special clase originally hidden
            if (OriginallyHidden) {
                Unlock();
                return;
            }

            // special case discovery mode disabled
            if (!ConfigManager.DiscoveryMode) {
                Unlock();
                return;
            }

            if (ConfigManager.DiscoveryMode) {
                if (Discovered || PermanentlyDiscovered) {
                    Unlock();
                } else {
                    LockAndHide();
                }
            }
        }

        internal void RestoreOriginalState() {
            ExtendedLevel.RoutePrice = OriginalPrice;
            ExtendedLevel.IsRouteHidden = _originallyHidden;
            ExtendedLevel.IsRouteLocked = _originallyLocked;
        }

        internal void DesignateAsStoryLocked() {
            StoryUnlock = true;
            OriginallyHidden = true;
            OriginallyLocked = true;
        }

        internal void VisitMoon() {
            VisitCount++;
            Logger.LogDebug($"{Name}: Set visit count to {VisitCount}");
            if ((ExtendedLevel.RoutePrice == 0 || (ConfigManager.DiscountMode && BuyCount == ConfigManager.DiscountsCount)) && OriginalPrice != ExtendedLevel.RoutePrice) {
                FreeVisitCount++;
                Logger.LogDebug($"{Name}: Set free visit count to {FreeVisitCount}");
                if (ConfigManager.UnlockMode && !ConfigManager.DiscountMode && ConfigManager.UnlocksResetAfterVisits > 0) {
                    if (FreeVisitCount > ConfigManager.UnlocksResetAfterVisits) {
                        Logger.LogInfo($"{Name}: Reset unlock due to free visit count ({FreeVisitCount - 1}) reached.");
                        NotificationHelper.SendChatMessage($"Unlock expired:\n<color=red>{Name}</color>");
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Unlock expired!", Text = $"Your unlock for {Name} has been used {(FreeVisitCount - 1).NumberOfWords("time")} and expired.", IsWarning = true, Key = "LMU_UnlockExpired" });
                        BuyCount = 0;
                        FreeVisitCount = 0;
                        if (ConfigManager.UnlocksResetAfterVisitsPermDiscovery) {
                            Logger.LogInfo($"{Name}: Also resetting permanent discovery status.");
                            PermanentlyDiscovered = false;
                        }
                    } else if (FreeVisitCount > 1) {
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Unlock: {Name}", Text = $"Unlock used! {(FreeVisitCount - 1).CountToText()} use.\nYou have {(ConfigManager.UnlocksResetAfterVisits - FreeVisitCount + 1).NumberOfWords("use")} left.", Key = "LMU_UnlockUsed" });
                    }
                }
                if (ConfigManager.DiscountMode && ConfigManager.DiscountsResetAfterVisits > 0) {
                    if (FreeVisitCount > ConfigManager.DiscountsResetAfterVisits) {
                        Logger.LogInfo($"{Name}: Reset discount due to free visit count ({FreeVisitCount - 1}) reached.");
                        NotificationHelper.SendChatMessage($"Discount expired:\n<color=red>{Name}</color>");
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Discount expired!", Text = $"Your discount for {Name} has been used {(FreeVisitCount - 1).NumberOfWords("time")} and expired.", IsWarning = true, Key = "LMU_DiscountExpired" });
                        BuyCount = 0;
                        FreeVisitCount = 0;
                        if (ConfigManager.DiscountsResetAfterVisitsPermDiscovery) {
                            Logger.LogInfo($"{Name}: Also resetting permanent discovery status.");
                            PermanentlyDiscovered = false;
                        }
                    } else if (FreeVisitCount > 1) {
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Discount: {Name}", Text = $"Discount redeemed! {(FreeVisitCount - 1).CountToText()} use.\nYou have {(ConfigManager.DiscountsResetAfterVisits - FreeVisitCount + 1).NumberOfWords("use")} left.", Key = "LMU_DiscountUsed" });
                    }
                }
            }
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 1);
        }

        internal void Land() {
            LandingCount++;
        }

        internal Dictionary<string, List<string>> GetMatchingCustomGroups() {
            var customGroups = ConfigManager.MoonGroupMatchingCustomDict;
            var matchingGroups = new Dictionary<string, List<string>>();
            foreach (var group in customGroups) {
                if (group.Value.Contains(Name))
                    matchingGroups.Add(group.Key, group.Value);
            }
            return matchingGroups;
        }
        internal string GetMoonPreviewText(PreviewInfoType infoType) {
            // FIRST TERMINAL LINE (NEXT TO NAME)
            int moonNameLength = Name.Count();
            string format = "{0, -" + Math.Clamp(18 - moonNameLength, 0, 18) + "} {1, -7} {2, -9} {3, -13}";
            string preview = string.Empty;
            string empty = string.Empty;

            // Gather preview components
            string weather = string.Empty;
            if (Plugin.WeatherTweaksPresent) {
                weather = WTCompatibility.GetWeatherTweaksWeather(this);
            } else {
                weather = ExtendedLevel.SelectableLevel.currentWeather.ToString();
            }
            if (weather.Trim().Equals("None")) {
                weather = string.Empty;
            }
            if (weather.Count() > 13) weather = weather.Substring(0, 11) + "..";

            string risk = string.Empty;
            if (Plugin.LQPresent && ConfigManager.PreferLQRisk) {
                risk = LQCompatibility.GetLQRiskLevel(this);
            }
            if (string.IsNullOrEmpty(risk)) {
                risk = ExtendedLevel.SelectableLevel.riskLevel;
            }
            if (risk.Count() > 7) {
                risk = risk.Substring(0, 5) + "..";
            }

            // Build preview according to PreviewInfoType
            if (infoType.Equals(PreviewInfoType.Weather)) {
                preview = string.Format(format, empty, empty, "$" + ExtendedLevel.RoutePrice, weather);
            } else if (infoType.Equals(PreviewInfoType.Price)) {
                preview = string.Format(format, empty, empty, "$" + ExtendedLevel.RoutePrice, empty);
            } else if (infoType.Equals(PreviewInfoType.Difficulty)) {
                if (ConfigManager.TerminalShowRiskWeather) {
                    preview = string.Format(format, empty, risk, empty, weather);
                } else {
                    preview = string.Format(format, empty, risk, empty, empty);
                }
            } else if (infoType.Equals(PreviewInfoType.History)) {
                preview = string.Format(format, empty, empty, empty, empty);
            } else if (infoType.Equals(PreviewInfoType.All)) {
                preview = string.Format(format, empty, risk, "$" + ExtendedLevel.RoutePrice, weather);
            } else if (infoType.Equals(PreviewInfoType.Vanilla)) {
                preview = string.Format(format, empty, empty, empty, empty);
            } else if (infoType.Equals(PreviewInfoType.Override)) {
                preview = string.Format(format, empty, empty, empty, empty);
            }
            if (ExtendedLevel.IsRouteLocked) {
                preview += "\n  * (Locked)";
            }

            if (!ConfigManager.DisplayTerminalTags) {
                return preview;
            }

            string tags = BuildTagString();
            
            if (!string.IsNullOrEmpty(tags)) {
                preview += tags;
            }
            return preview;
        }

        private string BuildTagString() {
            // LMU Tags
            string tags = string.Empty;
            if (ExtendedLevel == LevelManager.CurrentExtendedLevel && ConfigManager.ShowTagInOrbit) {
                tags = AddTagToPreviewText($"[IN ORBIT]", tags);
            }
            if (NewDiscovery && ConfigManager.DiscoveryMode && ConfigManager.ShowTagNewDiscovery) {
                tags = AddTagToPreviewText($"[NEW]", tags);
            }
            //if (VisitCount > 0) {
            //    tags = AddTagToPreviewText($"[VISITS:{VisitCount}]", tags);
            //}
            if (LandingCount > 0 && ConfigManager.ShowTagExplored) {
                tags = AddTagToPreviewText($"[EXPLORED:{LandingCount}]", tags);
            } else if (LandingCount == 0 && ConfigManager.ShowTagExplored) {
                tags = AddTagToPreviewText($"[UNEXPLORED]", tags);
            }
            if (FreeVisitCount > 0 && ConfigManager.UnlockMode && !ConfigManager.DiscountMode && ConfigManager.UnlocksResetAfterVisits > 0 && ConfigManager.ShowTagUnlockDiscount) {
                tags = AddTagToPreviewText($"[UNLOCK EXPIRES:{ConfigManager.UnlocksResetAfterVisits - FreeVisitCount + 1}]", tags);
            } else if (FreeVisitCount > 0 && ConfigManager.DiscountMode && ConfigManager.DiscountsResetAfterVisits > 0 && ConfigManager.ShowTagUnlockDiscount) {
                tags = AddTagToPreviewText($"[DISCOUNT EXPIRES:{ConfigManager.DiscountsResetAfterVisits - FreeVisitCount + 1}]", tags);
            } else if (ConfigManager.UnlockMode && !ConfigManager.DiscountMode && BuyCount > 0 && ConfigManager.ShowTagUnlockDiscount) {
                tags = AddTagToPreviewText("[UNLOCKED]", tags);
            } else if (ConfigManager.DiscountMode && BuyCount > 0 && ConfigManager.ShowTagUnlockDiscount) {
                int discountRate = 100 - (int)(Plugin.GetDiscountRate(BuyCount) * 100);
                if (discountRate != 100) {
                    tags = AddTagToPreviewText($"[DISCOUNT {discountRate}%]", tags);
                } else {
                    tags = AddTagToPreviewText($"[FULL DISCOUNT]", tags);
                }
            }
            if (PermanentlyDiscovered && !ConfigManager.DiscoveryNeverShuffle && ConfigManager.DiscoveryMode && ConfigManager.ShowTagPermanentDiscovery) {
                if (OriginalPrice == 0 && ConfigManager.PermanentlyDiscoverFreeMoonsOnLanding != 0 || OriginalPrice > 0 && ConfigManager.PermanentlyDiscoverPaidMoonsOnLanding != 0) {
                    tags = AddTagToPreviewText("[PINNED]", tags);
                }
            }
            if (OnSale && SalesRate > 0 && ExtendedLevel.RoutePrice > 0 && ConfigManager.Sales && ConfigManager.ShowTagSale) {
                tags = AddTagToPreviewText($"[SALE {SalesRate}%]", tags);
            }

            // OPTIONAL GROUP TAG
            var customGroupsDict = GetMatchingCustomGroups();
            if (ConfigManager.MoonGroupMatchingMethod == "Custom" && customGroupsDict.Count > 0 && ConfigManager.ShowTagGroups) {
                string groupTag = string.Empty;
                if (customGroupsDict.Count > 1) {
                    groupTag = string.Join("/", customGroupsDict.Keys);
                } else if (customGroupsDict.Count == 1) {
                    groupTag = customGroupsDict.Keys.First();
                }
                tags = AddTagToPreviewText($"[{groupTag.Trim().ToUpper()}]", tags);
            } else if (Plugin.LethalConstellationsPresent && Plugin.LethalConstellationsExtension != null && ConfigManager.MoonGroupMatchingMethod == "LethalConstellations" && ConfigManager.ShowTagGroups) {
                tags = AddTagToPreviewText($"[{Plugin.LethalConstellationsExtension.GetConstellationName(this).ToUpper()}]", tags);
            } else if (ConfigManager.MoonGroupMatchingMethod == "Tag") {
                var contentTags = ExtendedLevel.ContentTags;
                string tagsTag = string.Empty;
                if (contentTags.Count > 1) {
                    tagsTag = string.Join("/", contentTags.Select(tag => tag.contentTagName.ToUpper()));
                } else if (contentTags.Count == 1) {
                    tagsTag = contentTags.FirstOrDefault().ToString();
                }
                if (!string.IsNullOrEmpty(tagsTag)) {
                    tags = AddTagToPreviewText($"[{tagsTag}]", tags);
                }
            }
            return tags;
        }
        internal string BuildShortTagString() {
            // LMU Tags
            string tags = string.Empty;
            if (NewDiscovery && ConfigManager.DiscoveryMode && ConfigManager.ShowTagNewDiscovery) {
                tags = AddTagToPreviewText($"[!]", tags);
            }
            if (LandingCount > 0 && ConfigManager.ShowTagExplored) {
                tags = AddTagToPreviewText($"[EXPLORED:{LandingCount}]", tags);
            } else if (LandingCount == 0 && ConfigManager.ShowTagExplored) {
                tags = AddTagToPreviewText($"[UNEXPLORED]", tags);
            }
            if (FreeVisitCount > 0 && ConfigManager.UnlockMode && !ConfigManager.DiscountMode && ConfigManager.UnlocksResetAfterVisits > 0 && ConfigManager.ShowTagUnlockDiscount) {
                tags = AddTagToPreviewText($"[{ConfigManager.UnlocksResetAfterVisits - FreeVisitCount + 1}]", tags);
            } else if (FreeVisitCount > 0 && ConfigManager.DiscountMode && ConfigManager.DiscountsResetAfterVisits > 0 && ConfigManager.ShowTagUnlockDiscount) {
                tags = AddTagToPreviewText($"[{ConfigManager.DiscountsResetAfterVisits - FreeVisitCount + 1}]", tags);
            } else if (ConfigManager.UnlockMode && !ConfigManager.DiscountMode && BuyCount > 0 && ConfigManager.ShowTagUnlockDiscount) {
                tags = AddTagToPreviewText("[U]", tags);
            } else if (ConfigManager.DiscountMode && BuyCount > 0 && ConfigManager.ShowTagUnlockDiscount) {
                int discountRate = 100 - (int)(Plugin.GetDiscountRate(BuyCount) * 100);
                if (discountRate != 100) {
                    tags = AddTagToPreviewText($"[{discountRate}%]", tags);
                } else {
                    tags = AddTagToPreviewText($"[U]", tags);
                }
            }
            if (PermanentlyDiscovered && !ConfigManager.DiscoveryNeverShuffle && ConfigManager.DiscoveryMode && ConfigManager.ShowTagPermanentDiscovery) {
                if (OriginalPrice == 0 && ConfigManager.PermanentlyDiscoverFreeMoonsOnLanding != 0 || OriginalPrice > 0 && ConfigManager.PermanentlyDiscoverPaidMoonsOnLanding != 0) {
                    tags = AddTagToPreviewText("[P]", tags);
                }
            }
            if (OnSale && SalesRate > 0 && ExtendedLevel.RoutePrice > 0 && ConfigManager.Sales && ConfigManager.ShowTagSale) {
                tags = AddTagToPreviewText($"[{SalesRate}%]", tags);
            }
            return tags;
        }

        private string AddTagToPreviewText(string newTag, string previewText) {
            if (previewText == string.Empty) {
                previewText = "\n  *";
            }
            string[] lines = previewText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines[lines.Length - 1].Length + newTag.Length < ConfigManager.TerminalTagLineWidth || lines[lines.Length - 1] == "  *") {
                previewText += " " + newTag;
            } else {
                previewText += "\n  * " + newTag; 
            }
            return previewText;
        }

        private int CalculatePrice() {
            string log = $"{Name}: Calculating price.. ";
            int price = OriginalPrice;
            log += $"Original price -> {OriginalPrice}";
            if (BuyCount > 0) {
                if (ConfigManager.DiscountMode) {
                    float discountRate = Plugin.GetDiscountRate(BuyCount);
                    price = (int)(price * discountRate);
                    if (price <= 0 && discountRate > 0) price = 1; 
                    log += $", Discount -> {price}";
                } else if (ConfigManager.UnlockMode) {
                    price = 0;
                    log += $", Unlock -> {price}, ";
                }
            }
            if (OnSale && price > 0) {
                price = (int)(price * (100 - SalesRate) / 100f);
                if (price <= 0) price = 1;
                log += $", Sales rate ({SalesRate}%) -> {price}";
            }
            Logger.LogDebug(log);
            return price;
        }

        public override string ToString() {
            int visits = VisitCount;
            if (FreeVisitCount > VisitCount) visits = FreeVisitCount;

            string state = "\u2713";
            if (ExtendedLevel.IsRouteHidden && ExtendedLevel.IsRouteLocked) state = string.Empty;
            else if (ExtendedLevel.IsRouteHidden) state = "Hidden";
            else if (ExtendedLevel.IsRouteLocked) state = "Locked";

            string discovered = string.Empty;
            if (Discovered) discovered = "\u2713";
            if (NewDiscovery) discovered = "New";
            if (!DiscoveredOnce) discovered = "Never";
            if (PermanentlyDiscovered) discovered = "Permanent";

            string sale = "-";
            if (SalesRate > 0) sale = SalesRate.ToString() + "%";

            string originalState = "\u2713";
            if (OriginallyHidden && !OriginallyLocked) originalState = "Hidden";
            else if (!OriginallyHidden && OriginallyLocked) originalState = "Locked";
            else if (OriginallyHidden && OriginallyLocked) originalState = string.Empty;

            string storyLock = "-";
            if (StoryUnlock && !StoryIsUnlocked) storyLock = "Active";
            else if (StoryUnlock && StoryIsUnlocked) storyLock = "Released";

            return string.Format(UnlockManager.LogFormatString, [ Name, RoutePrice, BuyCount, visits, state, discovered, sale, OriginalPrice, originalState, storyLock ]);
        }
    }
}
