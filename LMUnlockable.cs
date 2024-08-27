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
        [ES3NonSerializable]
        [NonSerialized]
        internal ExtendedLevel ExtendedLevel;
        [SerializeField]
        [ES3Serializable]
        internal string Name { get; private set; }
        public int OriginalPrice { get; private set; }
        public bool OriginallyHidden { get; private set; }
        public bool OriginallyLocked { get; private set; }

        [SerializeField]
        [ES3Serializable]
        internal int BuyCount { get; set; } = 0;
        [SerializeField]
        [ES3Serializable]
        internal int VisitCount { get; set; } = 0;
        [SerializeField]
        [ES3Serializable]
        internal int FreeVisitCount { get; set; } = 0;
        [SerializeField]
        [ES3Serializable]
        internal int LandingCount { get; set; } = 0;
        [SerializeField]
        [ES3Serializable]
        internal bool Discovered { get; set; } = false;
        [SerializeField]
        [ES3Serializable]
        internal bool NewDiscovery { get; set; } = false;
        [SerializeField]
        [ES3Serializable]
        internal bool DiscoveredOnce { get; set; } = false;
        [SerializeField]
        [ES3Serializable]
        internal bool PermanentlyDiscovered { get; set; } = false;
        [SerializeField]
        [ES3Serializable]
        internal bool OnSale { get; set; } = false;
        [SerializeField]
        [ES3Serializable]
        internal int SalesRate { get; set; } = 0;

        public LMUnlockable(string name, int originalPrice, bool originallyHidden, bool originallyLocked) {
            Name = name;
            ExtendedLevel = UnlockManager.Instance.AllLevels.Where(level => level.NumberlessPlanetName == Name).FirstOrDefault();
            OriginalPrice = originalPrice;
            OriginallyHidden = originallyHidden;
            OriginallyLocked = originallyLocked;
        }
        public void OverrideData(LMUnlockable newData) {
            if (Name != newData.Name) {
                Plugin.Instance.Mls.LogError("Name mismatch during override LMUnlockable data!");
            } else {
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
            }
        }

        public void ApplyPrice() {
            int newPrice = OriginalPrice;
            if (BuyCount > 0) {
                if (ConfigManager.DiscountMode) {
                    newPrice = (int)(newPrice * Plugin.GetDiscountRate(BuyCount));
                    Plugin.Instance.Mls.LogDebug($"{Name}: Discount applied ({newPrice})");
                } else if (ConfigManager.UnlockMode) {
                    newPrice = 0;
                    Plugin.Instance.Mls.LogDebug($"{Name}: Unlock applied ({newPrice})");
                }
            }
            if (OnSale && newPrice > 0) {
                newPrice = (int)(newPrice * (100 - SalesRate) / 100f);
                Plugin.Instance.Mls.LogDebug($"{Name}: Sales rate applied ({newPrice})");
            }
            ExtendedLevel.RoutePrice = newPrice;
        }

        public void ApplyDiscoverability() {
            // set permanently discovered if moon was bought (if config enabled)
            if (BuyCount > 0 && PermanentlyDiscovered == false && ((ConfigManager.UnlockMode && !ConfigManager.DiscountMode && ConfigManager.DiscoveryKeepUnlocks) || (ConfigManager.DiscountMode && ConfigManager.DiscoveryKeepDiscounts))) {
                PermanentlyDiscovered = true;
                Plugin.Instance.Mls.LogInfo($"{Name} set to permanently discovered because it's {(ConfigManager.DiscoveryKeepUnlocks ? "unlocked" : "discounted.")}");
            }
            // set permanently discovered if free moon was landed on x times (if config enabled)
            if (Discovered && !OriginallyHidden && !OriginallyLocked && OriginalPrice == 0 && ConfigManager.PermanentlyDiscoverFreeMoonsOnLanding > -1) {
                if (LandingCount >= ConfigManager.PermanentlyDiscoverFreeMoonsOnLanding) {
                    PermanentlyDiscovered = true;
                }
            }
            // set permanently discovered if paid moon was landed on x times (if config enabled)
            if (Discovered && !OriginallyHidden && !OriginallyLocked && OriginalPrice > 0 && ConfigManager.PermanentlyDiscoverPaidMoonsOnLanding > -1) {
                if (LandingCount >= ConfigManager.PermanentlyDiscoverPaidMoonsOnLanding) {
                    PermanentlyDiscovered = true;
                }
            }
            // make sure all by default or LLL config hidden moons are hidden
            if (OriginallyHidden) {
                Discovered = false;
                if (ConfigManager.PermanentlyDiscoverHiddenMoonsOnVisit && VisitCount > 0) {
                    PermanentlyDiscovered = true;
                    ExtendedLevel.IsRouteHidden = false;
                }
                else {
                    PermanentlyDiscovered = false;
                    ExtendedLevel.IsRouteHidden = true;
                }
            }
            // make sure all by default or LLL config locked moons are locked
            if (OriginallyLocked) {
                Discovered = false;
                PermanentlyDiscovered = false;
                ExtendedLevel.IsRouteHidden = true;
                ExtendedLevel.IsRouteLocked = true;
            }
            if (Discovered || PermanentlyDiscovered) {
                // tag new discoveries as new
                if (!DiscoveredOnce) {
                    NewDiscovery = true;
                    DiscoveredOnce = true;
                }
                ExtendedLevel.IsRouteHidden = false;
                ExtendedLevel.IsRouteLocked = false;
                Plugin.Instance.Mls.LogDebug($"{Name} is visible in terminal moon catalogue");
            } else if (!Discovered && !PermanentlyDiscovered && !OriginallyHidden && !OriginallyLocked) {
                ExtendedLevel.IsRouteHidden = true;
                ExtendedLevel.IsRouteLocked = true;
            }
        }

        public void RestoreOriginalState() {
            ExtendedLevel.RoutePrice = OriginalPrice;
            ExtendedLevel.IsRouteHidden = OriginallyHidden;
            ExtendedLevel.IsRouteLocked = OriginallyLocked;
        }

        public void RefreshSale() {
            int rnd = UnityEngine.Random.Range(0, 100);
            if (rnd < ConfigManager.SalesChance && ExtendedLevel.RoutePrice > 0) {
                OnSale = true;
                SalesRate = ConfigManager.SalesRate;
                Plugin.Instance.Mls.LogDebug($"{Name} is on SALE for {SalesRate}% OFF!");
            } else {
                OnSale = false;
                SalesRate = 0;
            }
        }

        public void VisitMoon() {
            VisitCount++;
            Plugin.Instance.Mls.LogDebug($"{Name}: Set visit count to {VisitCount}");
            if ((ExtendedLevel.RoutePrice == 0 || (ConfigManager.DiscountMode && BuyCount == ConfigManager.DiscountsCount)) && OriginalPrice != ExtendedLevel.RoutePrice) {
                FreeVisitCount++;
                Plugin.Instance.Mls.LogDebug($"{Name}: Set free visit count to {FreeVisitCount}");
                if (ConfigManager.UnlockMode && !ConfigManager.DiscountMode && ConfigManager.UnlocksResetAfterVisits > 0) {
                    if (FreeVisitCount > ConfigManager.UnlocksResetAfterVisits) {
                        Plugin.Instance.Mls.LogInfo($"{Name}: Reset unlock due to free visit count ({FreeVisitCount - 1}) reached.");
                        NotificationHelper.SendChatMessage($"Unlock expired:\n<color=red>{Name}</color>");
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Unlock expired!", Text = $"Your unlock for {Name} has been used {(FreeVisitCount - 1).NumberOfWords("time")} and expired.", IsWarning = true, Key = "LMU_UnlockExpired" });
                        BuyCount = 0;
                        FreeVisitCount = 0;
                    } else if (FreeVisitCount > 1) {
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Unlock: {Name}", Text = $"Unlock used! {(FreeVisitCount - 1).CountToText()} use.\nYou have {(ConfigManager.UnlocksResetAfterVisits - FreeVisitCount + 1).NumberOfWords("use")} left.", Key = "LMU_UnlockUsed" });
                    }
                    if (ConfigManager.UnlocksResetAfterVisitsPermDiscovery) {
                        PermanentlyDiscovered = false;
                        Plugin.Instance.Mls.LogInfo($"{Name}: Also resetting permanent discovery status.");
                    }
                }
                if (ConfigManager.DiscountMode && ConfigManager.DiscountsResetAfterVisits > 0) {
                    if (FreeVisitCount > ConfigManager.DiscountsResetAfterVisits) {
                        Plugin.Instance.Mls.LogInfo($"{Name}: Reset discount due to free visit count ({FreeVisitCount - 1}) reached.");
                        NotificationHelper.SendChatMessage($"Discount expired:\n<color=red>{Name}</color>");
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Discount expired!", Text = $"Your discount for {Name} has been used {(FreeVisitCount - 1).NumberOfWords("time")} and expired.", IsWarning = true, Key = "LMU_DiscountExpired" });
                        BuyCount = 0;
                        FreeVisitCount = 0;
                    } else if (FreeVisitCount > 1) {
                        NetworkManager.Instance.ServerSendAlertMessage(new Notification() { Header = $"Discount: {Name}", Text = $"Discount redeemed! {(FreeVisitCount - 1).CountToText()} use.\nYou have {(ConfigManager.DiscountsResetAfterVisits - FreeVisitCount + 1).NumberOfWords("use")} left.", Key = "LMU_DiscountUsed" });
                    }
                    if (ConfigManager.DiscountsResetAfterVisitsPermDiscovery) {
                        PermanentlyDiscovered = false;
                        Plugin.Instance.Mls.LogInfo($"{Name}: Also resetting permanent discovery status.");
                    }
                }
            }
            DelayHelper.Instance.ExecuteAfterDelay(NetworkManager.Instance.ServerSendAlertQueueEvent, 1);
        }

        public void Land() {
            LandingCount++;
        }

        public Dictionary<string, List<string>> GetMatchingCustomGroups() {
            var customGroups = ConfigManager.MoonGroupMatchingCustomDict;
            var matchingGroups = new Dictionary<string, List<string>>();
            foreach (var group in customGroups) {
                if (group.Value.Contains(Name))
                    matchingGroups.Add(group.Key, group.Value);
            }
            return matchingGroups;
        }
        public string GetMoonPreviewText(PreviewInfoType infoType) {
            // FIRST TERMINAL LINE (NEXT TO NAME)
            int moonNameLength = Name.Count();
            string format = "{0, -" + (18 - moonNameLength) + "} {1, -7} {2, -9} {3, -13}";
            string preview = string.Empty;
            string empty = string.Empty;
            string weather = ExtendedLevel.SelectableLevel.currentWeather.ToString();
            if (ExtendedLevel.SelectableLevel.currentWeather == LevelWeatherType.None)
                weather = string.Empty;
            //if (weather.Count() > 13) weather = weather.Substring(0, 11) + "..";
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
            if (infoType.Equals(PreviewInfoType.Weather)) {
                preview = string.Format(format, empty, empty, "$" + ExtendedLevel.RoutePrice, weather);
            } else if (infoType.Equals(PreviewInfoType.Price)) {
                preview = string.Format(format, empty, empty, "$" + ExtendedLevel.RoutePrice, empty);
            } else if (infoType.Equals(PreviewInfoType.Difficulty)) {
                preview = string.Format(format, empty, risk, empty, empty);
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
                    tags = AddTagToPreviewText($"[DISCOUNT-{discountRate}%]", tags);
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
                tags = AddTagToPreviewText($"[SALE-{SalesRate}%]", tags);
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
            } else if (Plugin.LethalConstellationsPresent && Plugin.LethalConstellationsExtension!= null && ConfigManager.MoonGroupMatchingMethod == "LethalConstellations" && ConfigManager.ShowTagGroups) {
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
            if (!string.IsNullOrEmpty(tags)) {
                preview += tags;
            }
            if (ConfigManager.TerminalFontSizeOverride) {
                UnlockManager.Instance.Terminal.screenText.textComponent.fontSize = ConfigManager.TerminalFontSize;
            }
            return preview;
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

        public override string ToString() {
            bool ignored = OriginallyHidden || OriginallyLocked;
            return string.Format(UnlockManager.LogFormatString, Name, BuyCount, VisitCount, FreeVisitCount, Discovered, NewDiscovery, DiscoveredOnce, PermanentlyDiscovered, OnSale, SalesRate, OriginalPrice, ignored);
        }
    }
}
