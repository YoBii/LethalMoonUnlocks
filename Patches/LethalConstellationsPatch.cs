using HarmonyLib;
using LethalConstellations.PluginCore;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LethalMoonUnlocks.Patches {
    internal class LethalConstellationsPatch {

        private static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("LethalConstellations.PluginCore.LevelStuff");
            MethodBase mb = AccessTools.FirstMethod(type, method => method.Name.Contains("RouteConstellation"));
            return mb;

        }

        [HarmonyPostfix]
        private static void RouteConstellationPostfix() {
            var currentConstellation = Collections.ConstellationStuff.Where(c => c.consName == Collections.CurrentConstellation).FirstOrDefault();
            var unlock = UnlockManager.Instance.Unlocks.Where(unlock => unlock.ExtendedLevel.NumberlessPlanetName == currentConstellation.defaultMoon).FirstOrDefault();
            if (unlock != null) {
                Plugin.Instance.Mls.LogInfo($"Routing to moon {unlock.Name} with ID {unlock.ExtendedLevel.SelectableLevel.levelID}!");
                if (unlock.ExtendedLevel.RoutePrice > 0) { 
                    Plugin.Instance.Mls.LogInfo($"Route to {unlock.Name} was paid ({unlock.ExtendedLevel.RoutePrice} credits).");
                    if (NetworkManager.Instance.IsServer()) {
                        UnlockManager.Instance.BuyMoon(unlock.Name);
                    }
                } else {
                    NetworkManager.Instance.ClientBuyMoon(unlock.Name);
                }
            } else {
                    Plugin.Instance.Mls.LogInfo($"Route to {unlock.Name} was free.");
            }
        }
    }
}
