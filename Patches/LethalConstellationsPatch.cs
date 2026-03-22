using HarmonyLib;
using LethalConstellations.PluginCore;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch]
    internal class LethalConstellationsPatch {
        private static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("LethalConstellations.PluginCore.MenuStuff");
            return AccessTools.Method(type, "MainMenuText");
        }

        private static List<ClassMapper> GetVisibleConstellations() {
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
    }
}
