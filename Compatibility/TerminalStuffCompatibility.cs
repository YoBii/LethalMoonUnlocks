using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TerminalStuff.Configs;
using TerminalStuff.MoonsTweaks;
using TerminalStuff.SpecialStuff;
using TerminalStuff.StoreTweaks;

namespace LethalMoonUnlocks.Compatibility {
    internal static class TerminalStuffCompatibility {
        internal static void OnUpdateMoonsDisplayed(List<MoonInfo> moons) {
            var levelField = AccessTools.Field(typeof(MoonInfo), "Level");

            foreach (var moon in moons) {
                if (!ConfigManager.DisplayTerminalTags) {
                    moon.AdditionalInfo = string.Empty;
                    continue;
                }
                try {
                    var level = (SelectableLevel) levelField.GetValue(moon);
                    var unlock =
                        UnlockManager.Instance.Unlocks.FirstOrDefault(x => x.ExtendedLevel.SelectableLevel == level);
                    if (unlock != null) {
                        moon.AdditionalInfo = unlock.BuildAdditionalInfoString();
                    }
                    else if (level.PlanetName.Contains("Gordion")) {
                        moon.AdditionalInfo = string.Empty + "\n";
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
            MethodInfo replacementMethod = AccessTools.Method(typeof(TerminalStuffCompatibility), nameof(GetConfiguredMoonsPageSize));

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

        private static int GetConfiguredMoonsPageSize() {
            try {
                return Math.Max(1, MoonsPlusConfig.MenuPageSize.Value);
            }
            catch (Exception ex) {
                Logger.LogError("TerminalStuffCompatibility: Failed to read MoonsPlusConfig.MenuPageSize.");
                Logger.LogError(ex.Message);
                return 5;
            }
        }

        [HarmonyPatch(typeof(StorePlus), "InitBetterMenu")]
        [HarmonyTranspiler] 
        private static IEnumerable<CodeInstruction> InitBetterMenuTranspiler(IEnumerable<CodeInstruction> instructions) {
            FieldInfo storePlusMenuField = AccessTools.Field(typeof(StorePlus), "StorePlusMenu");
            FieldInfo pageSizeField = storePlusMenuField != null
                ? AccessTools.Field(storePlusMenuField.FieldType, "PageSize")
                : null;
            MethodInfo replacementMethod = AccessTools.Method(typeof(TerminalStuffCompatibility), nameof(GetConfiguredStorePageSize));

            if (storePlusMenuField == null || pageSizeField == null || replacementMethod == null) {
                Logger.LogError("TerminalStuffCompatibility: Failed to resolve StorePlus PageSize patch metadata.");
                return instructions;
            }

            var matcher = new CodeMatcher(instructions).MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, storePlusMenuField),
                new CodeMatch(ci => ci.LoadsConstant(6)),
                new CodeMatch(OpCodes.Stfld, pageSizeField)
            );

            if (!matcher.IsValid) {
                Logger.LogError("TerminalStuffCompatibility: Failed to find StorePlus PageSize assignment in SetupBetterMenu.");
                return instructions;
            }

            matcher.Advance(1);
            var replacementInstruction = new CodeInstruction(OpCodes.Call, replacementMethod);
            replacementInstruction.labels.AddRange(matcher.Instruction.labels);
            replacementInstruction.blocks.AddRange(matcher.Instruction.blocks);
            matcher.SetInstruction(replacementInstruction);

            Logger.LogDebug("TerminalStuffCompatibility: Patched StorePlus.SetupBetterMenu PageSize assignment.");
            return matcher.InstructionEnumeration();
        }

        private static int GetConfiguredStorePageSize() {
            try {
                return Math.Max(1, StorePlusConfig.MenuPageSize.Value);
            }
            catch (Exception ex) {
                Logger.LogError("TerminalStuffCompatibility: Failed to read StorePlusConfig.MenuPageSize.");
                Logger.LogError(ex.Message);
                return 6;
            }
        }

        private static int _groupCredits;
        [HarmonyPatch(typeof(MoonInfo), "SelectThisMoon")]
        [HarmonyPrefix]
        private static void SelectThisMoonPrefix(MoonInfo __instance) {
            _groupCredits = UnlockManager.Instance.Terminal.groupCredits;
        }
        
        [HarmonyPatch(typeof(MoonInfo), "SelectThisMoon")]
        [HarmonyPostfix]
        private static void SelectThisMoonPostfix(MoonInfo __instance) {
            if (_groupCredits > UnlockManager.Instance.Terminal.groupCredits) {
                var pricePaid = _groupCredits - UnlockManager.Instance.Terminal.groupCredits;
                var levelField = AccessTools.Field(typeof(MoonInfo), "Level");
                var level = (SelectableLevel) levelField.GetValue(__instance);
                
                Logger.LogInfo($"Route to {level.PlanetName} was paid ({pricePaid} credits) (routed via MoonsPlus).");

                if (UnlockManager.Instance.Unlocks.FirstOrDefault(x => x.ExtendedLevel.SelectableLevel == level) is
                    { } unlock) {
                    if (NetworkManager.Instance.IsServer()) {
                        UnlockManager.Instance.BuyMoon(unlock.Name);
                    } else {
                        NetworkManager.Instance.ClientBuyMoon(unlock.Name);
                    }
                }
            }
        }
    }
}
