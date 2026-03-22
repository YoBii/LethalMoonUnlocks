using System.Collections.Generic;

namespace LethalMoonUnlocks {
    internal class ProgressionManager {
        internal static ProgressionManager Instance { get; private set; } = null!;
        internal int PaintingsSold { get; set; }

        internal ProgressionManager() {
            if (Instance == null) {
                Instance = this;
            }
            Reset();
        }

        internal ProgressionSaveData GetSaveData() {
            return new ProgressionSaveData() {
                PaintingsSold = PaintingsSold
            };
        }

        internal void LoadSaveData(ProgressionSaveData progressionSaveData) {
            if (progressionSaveData == null) {
                Reset();
                return;
            }

            PaintingsSold = progressionSaveData.PaintingsSold;
        }

        internal void Reset() {
            PaintingsSold = 0;
        }

        internal static List<string> LMUStoryLocks() {
            return new List<string> { "Artifice", "Embrion" };
        }

        internal static List<string> GaletryStoryLock() {
            return new List<string> { "Galetry" };
        }
    }
}
