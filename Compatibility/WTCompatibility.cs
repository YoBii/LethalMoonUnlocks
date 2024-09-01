using LethalQuantities.Json;
using LethalQuantities.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using WeatherTweaks.Definitions;
using HarmonyLib;
using System.Linq;

namespace LethalMoonUnlocks.Compatibility {
    [HarmonyPatch]
    internal static class WTCompatibility {
        internal static Dictionary<SelectableLevel, string> WTWeathers = new Dictionary<SelectableLevel, string>();
        private static MethodBase TargetMethod() {
            Assembly assembly = typeof(WeatherTweaks.Plugin).Assembly;
            Type type = assembly.GetType("WeatherTweaks.Variables");
            MethodBase mb = AccessTools.FirstMethod(type, method => method.Name.Contains("GetPlanetCurrentWeather"));
            return mb;

        }

        [HarmonyPostfix]
        private static void GetPlanetCurrentWeatherPostfix(SelectableLevel level, bool uncertain, ref string __result) {
            WTWeathers[level] = __result;
        }

        public static string GetWeatherTweaksWeather(LMUnlockable unlock) {
            string weather = WTWeathers.TryGetValue(unlock.ExtendedLevel.SelectableLevel, out string value) ? value : null;
            if (weather != null) {
                return weather;
            } else {
                return string.Empty;
            }
        }
    }
}
