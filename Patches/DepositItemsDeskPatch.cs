using HarmonyLib;
using LethalMoonUnlocks.Util;

namespace LethalMoonUnlocks.Patches {
    [HarmonyPatch(typeof(DepositItemsDesk))]
    internal class DepositItemsDeskPatch {
        [HarmonyPatch(nameof(DepositItemsDesk.SellItemsOnServer))]
        [HarmonyPrefix]
        private static bool SellItemsOnServerPatch(ref DepositItemsDesk __instance) {
            if (NetworkManager.Instance == null || ProgressionManager.Instance == null || !NetworkManager.Instance.IsServer()) {
                return true;
            }

            if (!ConfigManager.GaletryStoryLock || ProgressionManager.Instance.PaintingsSold >= ConfigManager.GaletryStoryLockPaintingsAmount) {
                return true;
            }

            bool queuedAlert = false;
            foreach (var item in __instance.itemsOnCounter) {
                if (item == null || item.itemProperties == null || item.itemProperties.itemName != "Painting") {
                    continue;
                }

                ProgressionManager.Instance.PaintingsSold++;
                NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                    Header = "Company Report",
                    Text = $"{item.itemProperties.itemName}s sold: {ProgressionManager.Instance.PaintingsSold}/{ConfigManager.GaletryStoryLockPaintingsAmount}",
                    IsWarning = ProgressionManager.Instance.PaintingsSold >= ConfigManager.GaletryStoryLockPaintingsAmount,
                    Key = "LMU_GaletryProgress"
                });
                queuedAlert = true;
            }

            if (queuedAlert) {
                NetworkManager.Instance.ServerSendAlertQueueEvent();
            }

            return true;
        }
    }
}
