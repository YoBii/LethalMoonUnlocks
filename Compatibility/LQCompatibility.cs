using LethalQuantities.Json;
using LethalQuantities.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LethalMoonUnlocks.Compatibility {
    internal static class LQCompatibility {
        public static string GetLQRiskLevel(LMUnlockable unlock) {
            Type type = LethalQuantities.Plugin.INSTANCE.GetType();
            FieldInfo field = type.GetField("presets", BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<Guid, LevelPreset> presets = (Dictionary<Guid, LevelPreset>)field.GetValue(LethalQuantities.Plugin.INSTANCE);
            LevelPreset preset = presets.TryGetValue(unlock.ExtendedLevel.SelectableLevel.getGuid(), out LevelPreset value) ? value : null;

            if (preset != null) {
                return preset.riskLevel.value;
            } else {
                return string.Empty;
            }
        }
    }
}
