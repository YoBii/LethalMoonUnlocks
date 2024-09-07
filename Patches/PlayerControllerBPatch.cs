using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LethalMoonUnlocks.Patches {
    // Thanks @pacoito123 for this patch. I adapted it sligtly
    // Original: https://github.com/pacoito123/LC_StoreRotationConfig/blob/main/StoreRotationConfig/Patches/TerminalScrollMousePatch.cs

    [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed", typeof(InputAction.CallbackContext))]
    internal class PlayerControllerBPatch {
        public static string CurrentText { get; internal set; } = "";
        private static float ScrollAmount = 1 / 4f;
        private static void ScrollMouse_performed(Scrollbar scrollbar, float scrollDirection) {
            // Perform vanilla scroll if the 'relativeScroll' setting is disabled.
            if (UnlockManager.Instance.Terminal == null || ConfigManager.TerminalScrollAmount < 1) {
                // Increment scrollbar value by vanilla scroll amount (a third of the page).
                scrollbar.value += scrollDirection / 4f;
                return;
            }

            // Check if text currently shown in the terminal has changed, to avoid calculating the scroll amount more than once.
            if (string.CompareOrdinal(UnlockManager.Instance.Terminal.currentText, CurrentText) != 0) {
                // Cache text currently shown in the terminal.
                CurrentText = UnlockManager.Instance.Terminal.currentText;

                // Calculate relative scroll amount using the number of lines in the current terminal page.
                int NumberOfLines = CurrentText.Count(c => c.Equals('\n')) + 1;
                ScrollAmount = ConfigManager.TerminalScrollAmount / (float)NumberOfLines;

                Plugin.Instance.Mls.LogDebug($"Setting terminal scroll amount to '{ScrollAmount}'!");
            }

            // Increment terminal scrollbar value by the relative scroll amount, in the direction given by the mouse wheel input.
            scrollbar.value += scrollDirection * ScrollAmount;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            Plugin.Instance.Mls.LogError($"Patching PlayerControllerB");
            return new CodeMatcher(instructions).MatchForward(false,
               new(OpCodes.Ldarg_0),
               new(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.terminalScrollVertical))))
           .Insert(
               new(OpCodes.Ldarg_0),
               new(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.terminalScrollVertical))),
               new(OpCodes.Ldloc_0),
               new(OpCodes.Call, AccessTools.Method(typeof(PlayerControllerBPatch), nameof(ScrollMouse_performed))),
               new(OpCodes.Ret))
           .InstructionEnumeration();
        }
    }
}
