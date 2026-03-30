using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch]
    internal class DawnLibSaveDataPatch {
        private static MethodBase TargetMethod() {
            if (!Plugin.DawnLibPresent) {
                return null;
            }

            var type = AccessTools.TypeByName("Dawn.Internal.DawnNetworker");
            return AccessTools.Method(type, "SaveData");
        }

        private static void Prefix() {
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsServer() || UnlockManager.Instance == null) {
                return;
            }

            UnlockManager.Instance.Unlocks.Do(unlock => unlock.RestoreOriginalState());
        }

        private static void Postfix() {
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsServer() || UnlockManager.Instance == null) {
                return;
            }

            foreach (var unlock in UnlockManager.Instance.Unlocks) {
                unlock.ApplyState();
                unlock.ApplyVisibility();
            }
        }
    }
}
