using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TerminalStuff.Configs;
using TerminalStuff.MoonsTweaks;

namespace LethalMoonUnlocks.Compatibility {
    internal static class TerminalStuffCompatibility {
        internal static void OnUpdateMoonsDisplayed(List<MoonInfo> moons) {
            var levelField = AccessTools.Field(typeof(MoonInfo), "Level");

            foreach (var moon in moons) {
                try {
                    var level = (SelectableLevel) levelField.GetValue(moon);
                    var unlock =
                        UnlockManager.Instance.Unlocks.FirstOrDefault(x => x.ExtendedLevel.SelectableLevel == level);
                    if (unlock != null) {
                        moon.AdditionalInfo = unlock.BuildAdditionalInfoString();
                        Logger.LogDebug($"TerminalStuffCompatibility: Applied additional info to moon {unlock.Name} ..");
                    }
                    else if (level.PlanetName.Contains("Gordion")) {
                        moon.AdditionalInfo = string.Empty + "\n";
                        Logger.LogDebug("Skipping Gordion moon..");
                    }
                    else {
                        throw new Exception("TerminalStuffCompatibility: Moon not found in UnlockManager!");
                    }
                }
                catch (Exception ex) {
                    Logger.LogError("TerminalStuffCompatibility: Failed to get MoonInfo.Level!");
                    Logger.LogError(ex.Message);
                }
            }
        }

        [HarmonyPatch(typeof(MoonsPlus), "SetupBetterMenu")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SetupBetterMenuTranspiler(IEnumerable<CodeInstruction> instructions) {
            FieldInfo moonsPlusMenuField = AccessTools.Field(typeof(MoonsPlus), "MoonsPlusMenu");
            FieldInfo pageSizeField = moonsPlusMenuField != null
                ? AccessTools.Field(moonsPlusMenuField.FieldType, "PageSize")
                : null;
            MethodInfo replacementMethod = AccessTools.Method(typeof(TerminalStuffCompatibility), nameof(GetConfiguredMenuPageSize));

            if (moonsPlusMenuField == null || pageSizeField == null || replacementMethod == null) {
                Logger.LogError("TerminalStuffCompatibility: Failed to resolve MoonsPlus PageSize patch metadata.");
                return instructions;
            }

            var matcher = new CodeMatcher(instructions).MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, moonsPlusMenuField),
                new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)10),
                new CodeMatch(OpCodes.Stfld, pageSizeField)
            );

            if (!matcher.IsValid) {
                Logger.LogError("TerminalStuffCompatibility: Failed to find MoonsPlus PageSize assignment in SetupBetterMenu.");
                return instructions;
            }

            matcher.Advance(1);
            var replacementInstruction = new CodeInstruction(OpCodes.Call, replacementMethod);
            replacementInstruction.labels.AddRange(matcher.Instruction.labels);
            replacementInstruction.blocks.AddRange(matcher.Instruction.blocks);
            matcher.SetInstruction(replacementInstruction);

            Logger.LogDebug("TerminalStuffCompatibility: Patched MoonsPlus.SetupBetterMenu PageSize assignment.");
            return matcher.InstructionEnumeration();
        }

        private static int GetConfiguredMenuPageSize() {
            try {
                return Math.Max(1, MoonsPlusConfig.MenuPageSize.Value);
            }
            catch (Exception ex) {
                Logger.LogError("TerminalStuffCompatibility: Failed to read MoonsPlusConfig.MenuPageSize.");
                Logger.LogError(ex.Message);
                return 5;
            }
        }

    }
}
