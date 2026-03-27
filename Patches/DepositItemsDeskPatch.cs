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

            bool paintingSold = false;
            
            foreach (var item in __instance.itemsOnCounter) {
                if (item == null || item.itemProperties == null) {
                    continue;
                }

                if (item.itemProperties.itemName.Contains("Painting")) {
                    ProgressionManager.Instance.PaintingsSold++;
                    paintingSold = true;
                }
            }

            if (paintingSold) {
                NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                    Header = "Company Report",
                    Text = $"Paintings sold: {ProgressionManager.Instance.PaintingsSold}/{ConfigManager.GaletryStoryLockPaintingsAmount}",
                    IsWarning = ProgressionManager.Instance.PaintingsSold >= ConfigManager.GaletryStoryLockPaintingsAmount,
                    Key = "LMU_GaletryProgress"
                });
                NetworkManager.Instance.ServerSendAlertQueueEvent();
                if (ProgressionManager.Instance.PaintingsSold >= ConfigManager.GaletryStoryLockPaintingsAmount &&
                    UnlockManager.TryReleaseStoryLock("Galetry")) {
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification()
                    {
                        Header = "Art exhibition!",
                        Text = "The art museum welcomes visitors. Step inside, stare at the art and regain intellectual sustenance.",
                        Key = "LMU_GaletryProgress"
                    });
                    NetworkManager.Instance.ServerSendAlertQueueEvent();
                }
            }

            return true;
        }
    }
}
