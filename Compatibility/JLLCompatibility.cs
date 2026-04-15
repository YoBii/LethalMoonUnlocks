using LethalMoonUnlocks.Util;

namespace LethalMoonUnlocks.Compatibility {
    internal class JLLCompatibility {
        internal static void ReplaceJLLAlertDiscovery(string text) {
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsServer()) {
                return;
            }

            string locationName = text.Replace("Location: ", string.Empty);
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                Header = "New Coordinates!",
                Text = $"Establishing route to {locationName}...",
                IsWarning = false,
                UseSave = false,
                Key = "LMU_JLL_Discovery_1"
            });
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                Header = "Route unavailable!",
                Text = $"Adding {locationName} to backlog...",
                IsWarning = true,
                UseSave = false,
                Key = "LMU_JLL_Discovery_2"
            });
            NetworkManager.Instance.ServerSendAlertQueueEvent();
        }
        
        internal static void ReplaceJLLAlert(string text) {
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsServer()) {
                return;
            }

            string locationName = text.Replace("Location: ", string.Empty);
            NetworkManager.Instance.ServerSendAlertMessage(new Notification() {
                Header = "New Coordinates!",
                Text = $"Adding {locationName} to moons catalog...",
                IsWarning = false,
                UseSave = false,
                Key = "LMU_JLL_1"
            });
            NetworkManager.Instance.ServerSendAlertQueueEvent();
        }
    }
}
