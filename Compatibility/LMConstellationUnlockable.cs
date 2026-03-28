using LethalConstellations.PluginCore;
using LethalMoonUnlocks.Util;
using System;
using Dawn;
using UnityEngine;

namespace LethalMoonUnlocks.Compatibility {
    [Serializable]
    [ES3Serializable]
    internal class LMConstellationUnlockable {
        [ES3Serializable] internal string Name { get; set; }

        [ES3Serializable] internal bool StoryIsUnlocked { get; set; }
        [ES3Serializable] internal bool Discovered { get; set; }
        [ES3Serializable] internal bool DiscoveredOnce { get; set; }
        [ES3Serializable] internal bool NewDiscovery { get; set; }

        [ES3Serializable] internal int BuyCount { get; set; }
        [ES3Serializable] internal int VisitCount { get; set; }
        [ES3Serializable] internal int FreeVisitCount { get; set; }
        [ES3NonSerializable] internal int RoutePrice { get; set; }
        [ES3NonSerializable] internal int OriginalPrice { get; set; }
        [ES3NonSerializable] internal bool HasOriginalPriceSnapshot { get; set; }
        [ES3Serializable] internal bool OnSale { get; set; }
        [ES3Serializable] internal int SalesRate { get; set; }

        public LMConstellationUnlockable() {
        }

        internal LMConstellationUnlockable(string constellationName) {
            Name = constellationName;
        }

        internal LMConstellationUnlockable(ClassMapper constellation) {
            UpdateFromConstellation(constellation);
        }

        internal void UpdateFromConstellation(ClassMapper constellation) {
            if (constellation == null) {
                return;
            }

            Name = constellation.consName;
            if (!HasOriginalPriceSnapshot) {
                OriginalPrice = constellation.constelPrice;
                HasOriginalPriceSnapshot = true;
            }
        }

        internal void OverrideData(LMConstellationUnlockable other) {
            if (other == null) {
                return;
            }

            Name = other.Name;
            StoryIsUnlocked = other.StoryIsUnlocked;
            Discovered = other.Discovered;
            DiscoveredOnce = other.DiscoveredOnce;
            NewDiscovery = other.NewDiscovery;
            BuyCount = other.BuyCount;
            VisitCount = other.VisitCount;
            FreeVisitCount = other.FreeVisitCount;
            RoutePrice = other.RoutePrice;
            OriginalPrice = other.OriginalPrice;
            HasOriginalPriceSnapshot = other.HasOriginalPriceSnapshot;
            OnSale = other.OnSale;
            SalesRate = other.SalesRate;
        }

        internal LMConstellationUnlockable Clone() {
            var copy = new LMConstellationUnlockable();
            copy.OverrideData(this);
            return copy;
        }

        internal bool HasData() {
            return StoryIsUnlocked
                || Discovered
                || DiscoveredOnce
                || NewDiscovery
                || BuyCount > 0
                || VisitCount > 0
                || FreeVisitCount > 0
                || OnSale
                || SalesRate > 0;
        }

        internal void IterateState(int? originalPriceOverride = null) {
            CalculatePrice(originalPriceOverride ?? OriginalPrice);
        }

        internal void AdvanceBuyProgression() {
            if (ConfigManager.DiscountMode) {
                if (BuyCount < ConfigManager.DiscountsCount) {
                    BuyCount++;
                }

                return;
            }

            BuyCount++;
        }

        internal void VisitRoute(int? originalPriceOverride = null) {
            VisitCount++;

            int effectiveOriginalPrice = originalPriceOverride ?? OriginalPrice;
            bool usedFreeRoute = (RoutePrice == 0 || (ConfigManager.DiscountMode && BuyCount == ConfigManager.DiscountsCount))
                && effectiveOriginalPrice != RoutePrice;
            if (!usedFreeRoute) {
                return;
            }

            FreeVisitCount++;
            if (ConfigManager.UnlockMode && !ConfigManager.DiscountMode && ConfigManager.UnlocksResetAfterVisits > 0 && FreeVisitCount > ConfigManager.UnlocksResetAfterVisits) {
                BuyCount = 0;
                FreeVisitCount = 0;
            } else if (ConfigManager.DiscountMode && ConfigManager.DiscountsResetAfterVisits > 0 && FreeVisitCount > ConfigManager.DiscountsResetAfterVisits) {
                BuyCount = 0;
                FreeVisitCount = 0;
            }
        }

        internal int CalculatePrice(bool includeSale = true) {
            return CalculatePrice(OriginalPrice, includeSale);
        }

        internal int GetCalculatedPrice(int originalPrice, bool includeSale = true) {
            int price = originalPrice;
            if (BuyCount > 0) {
                if (ConfigManager.DiscountMode) {
                    float discountRate = Plugin.GetDiscountRate(BuyCount);
                    price = (int)(price * discountRate);
                    if (price <= 0 && discountRate > 0) {
                        price = 1;
                    }
                } else if (ConfigManager.UnlockMode) {
                    price = 0;
                }
            }

            if (includeSale && OnSale && price > 0) {
                price = (int)(price * (100 - SalesRate) / 100f);
                if (price <= 0) {
                    price = 1;
                }
            }

            return price;
        }

        internal int CalculatePrice(int originalPrice, bool includeSale = true) {
            RoutePrice = GetCalculatedPrice(originalPrice, includeSale);
            return RoutePrice;
        }

        internal void RefreshSale(int? originalPriceOverride = null) {
            int effectiveOriginalPrice = originalPriceOverride ?? OriginalPrice;
            if (RandomHelper.Chance(ConfigManager.SalesChance) && CalculatePrice(effectiveOriginalPrice, includeSale: false) > 0) {
                OnSale = true;
                SalesRate = ConfigManager.SalesRate;
            } else {
                OnSale = false;
                SalesRate = 0;
            }
        }

        internal string BuildAdditionalInfoString() {
            if (!ConfigManager.DisplayTerminalTags) {
                return string.Empty;
            }

            string tags = "Info: ";
            if (NewDiscovery && ConfigManager.DiscoveryMode && ConfigManager.ShowTagNewDiscovery) {
                tags = AddTag("[NEW]", tags);
            }
            if (VisitCount > 0 && ConfigManager.ShowTagExplored) {
                tags = AddTag($"[VISITS:{VisitCount}]", tags);
            }

            if (FreeVisitCount > 0 && ConfigManager.UnlockMode && !ConfigManager.DiscountMode && ConfigManager.UnlocksResetAfterVisits > 0 && ConfigManager.ShowTagUnlockDiscount) {
                tags = AddTag($"[UNLOCK EXPIRES:{ConfigManager.UnlocksResetAfterVisits - FreeVisitCount + 1}]", tags);
            } else if (FreeVisitCount > 0 && ConfigManager.DiscountMode && ConfigManager.DiscountsResetAfterVisits > 0 && ConfigManager.ShowTagUnlockDiscount) {
                tags = AddTag($"[DISCOUNT EXPIRES:{ConfigManager.DiscountsResetAfterVisits - FreeVisitCount + 1}]", tags);
            } else if (ConfigManager.UnlockMode && !ConfigManager.DiscountMode && BuyCount > 0 && ConfigManager.ShowTagUnlockDiscount) {
                tags = AddTag("[UNLOCKED]", tags);
            } else if (ConfigManager.DiscountMode && BuyCount > 0 && ConfigManager.ShowTagUnlockDiscount) {
                int discountRate = 100 - (int)(Plugin.GetDiscountRate(BuyCount) * 100);
                tags = AddTag(discountRate != 100 ? $"[DISCOUNT {discountRate}%]" : "[FULL DISCOUNT]", tags);
            }

            if (OnSale && SalesRate > 0 && RoutePrice > 0 && ConfigManager.Sales && ConfigManager.ShowTagSale) {
                tags = AddTag($"[SALE {SalesRate}%]", tags);
            }

            return string.IsNullOrEmpty(tags) ? string.Empty : $"\n{tags}";
        }

        private static string AddTag(string tag, string tags) {
            if (string.IsNullOrWhiteSpace(tag)) {
                return tags;
            }

            if (string.IsNullOrWhiteSpace(tags)) {
                return tag;
            }

            return $"{tags} {tag}";
        }
    }
}
