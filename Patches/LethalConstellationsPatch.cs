using HarmonyLib;
using LethalConstellations.PluginCore;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch]
    internal class LethalConstellationsPatch {
        private static readonly FieldInfo PositionalPriceModeField = AccessTools.Field(typeof(ClassMapper), "positionalPriceMode");

        private static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("LethalConstellations.PluginCore.MenuStuff");
            return AccessTools.Method(type, "MainMenuText");
        }

        private static List<ClassMapper> GetVisibleConstellations() {
            Plugin.LethalConstellationsExtension?.TryApplyPendingSaveData();
            Plugin.ConstellationManager?.ApplyConstellationState();
            if (Collections.ConstellationStuff.Count == 0) {
                return new List<ClassMapper>();
            }
            return Collections.ConstellationStuff.FindAll(x => !x.isHidden);
        }

        [HarmonyPatch]
        private static class TravelToNewConstellationPatch {
            private static MethodBase TargetMethod() {
                var type = AccessTools.TypeByName("LethalConstellations.PluginCore.MenuStuff");
                return AccessTools.Method(type, "TravelToNewConstellation");
            }

            [HarmonyPrefix]
            private static void Prefix(ref int getPrice, int indexNum) {
                if (indexNum < 0 || indexNum >= Collections.DisplayConstellations.Count) {
                    return;
                }

                var constellation = Collections.DisplayConstellations[indexNum];
                if (string.IsNullOrWhiteSpace(constellation.consName)) {
                    return;
                }

                Plugin.LethalConstellationsExtension?.TryApplyPendingSaveData();
                Plugin.LethalConstellationsExtension?.RecordPendingConstellationRoute(constellation.consName, getPrice);
            }
        }

        [HarmonyPatch]
        private static class GetConstPricePatch {
            private static MethodBase TargetMethod() {
                var type = AccessTools.TypeByName("LethalConstellations.PluginCore.LevelStuff");
                return AccessTools.Method(type, "GetConstPrice");
            }

            [HarmonyPostfix]
            private static void Postfix(ClassMapper item, ref int __result) {
                if (item == null || Plugin.LethalConstellationsExtension == null) {
                    return;
                }

                var positionalPriceMode = PositionalPriceModeField?.GetValue(item) as string;
                if (positionalPriceMode != "SetPriceByDistance") {
                    return;
                }

                Plugin.LethalConstellationsExtension.TryApplyPendingSaveData();
                if (!Plugin.LethalConstellationsExtension.TryGetConstellationState(item.consName, out var constellationState) || constellationState == null) {
                    return;
                }

                __result = constellationState.GetCalculatedPrice(__result);
            }
        }
    }
}
