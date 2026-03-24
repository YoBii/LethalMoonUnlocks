using System.Linq;
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
                    UnlockManager.Instance.Unlocks.FirstOrDefault(
                        u => u is { Name: "Galetry", StoryUnlock: true, StoryIsUnlocked: false }) is { } galetryUnlock) {
                    galetryUnlock.StoryIsUnlocked = true;
                    Logger.LogInfo($"{galetryUnlock.Name}: Releasing story lock.. {galetryUnlock.Name} now available (for discovery).");
                    NetworkManager.Instance.ServerSendAlertMessage(new Notification()
                    {
                        Header = "Art exhibition!",
                        Text = "The art museum welcomes visitors. Step inside, stare at the art and regain intellectual sustenance.",
                        Key = "LMU_GaletryProgress"
                    });
                    galetryUnlock.Discovered = true;
                    galetryUnlock.PermanentlyDiscovered = true;
                    galetryUnlock.IterateState();
                    galetryUnlock.ApplyState();
                    galetryUnlock.ApplyVisibility();
                }
            }

            return true;
        }
    }
}
