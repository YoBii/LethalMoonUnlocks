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

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> MainMenuTextTranspiler(IEnumerable<CodeInstruction> instructions) {
            var collectionsType = AccessTools.TypeByName("LethalConstellations.PluginCore.Collections");
            FieldInfo constellationStuffField = AccessTools.Field(collectionsType, "ConstellationStuff");
            FieldInfo displayConstellationsField = AccessTools.Field(collectionsType, "DisplayConstellations");
            MethodInfo replacementMethod = AccessTools.Method(typeof(LethalConstellationsPatch), nameof(GetVisibleConstellations));

            if (constellationStuffField == null || displayConstellationsField == null || replacementMethod == null) {
                Logger.LogError("LethalConstellationsPatch: Failed to resolve MenuStuff.MainMenuText patch metadata.");
                return instructions;
            }

            var matcher = new CodeMatcher(instructions).MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, constellationStuffField),
                new CodeMatch(OpCodes.Stsfld, displayConstellationsField)
            );

            if (!matcher.IsValid) {
                Logger.LogError("LethalConstellationsPatch: Failed to find DisplayConstellations assignment in MenuStuff.MainMenuText.");
                return instructions;
            }

            var replacementInstruction = new CodeInstruction(OpCodes.Call, replacementMethod);
            replacementInstruction.labels.AddRange(matcher.Instruction.labels);
            replacementInstruction.blocks.AddRange(matcher.Instruction.blocks);
            matcher.SetInstruction(replacementInstruction);

            Logger.LogDebug("LethalConstellationsPatch: Patched MenuStuff.MainMenuText to filter hidden constellations.");
            return matcher.InstructionEnumeration();
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
