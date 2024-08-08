using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace LethalMoonUnlocks {
    internal static class RandomSelector {
        public static List<T> Get<T>(List<T> objects, int amount) {
            if (objects.Count < amount || objects.Count == 0 || objects == null)
                return objects;
            List<T> input = new List<T>(objects);
            List<T> selection = new List<T>();
            while (selection.Count < amount) {
                selection.Add(input[UnityEngine.Random.Range(0, input.Count)]);
                input.Remove(selection.Last());
            }
            CheckResult(selection, amount);
            return selection;
        }

        public static List<T> GetWeighted<T>(Dictionary<T, int> objects, int amount) {
            if (objects.Count < amount || objects.Count == 0 || objects == null)
                return new List<T>(objects.Keys);
            Dictionary<T, int> input = new Dictionary<T, int>(objects);
            List<T> selection = new List<T>();

            while (selection.Count < amount) {
                int totalWeight = input.Sum(entry => entry.Value);
                int random = UnityEngine.Random.Range(0, totalWeight);
                T result = default;
                foreach (var i in input) {
                    if (i.Value == 0)
                        continue;
                    if (random < i.Value) {
                        result = i.Key;
                        break;
                    }
                    random -= i.Value;
                }
                if (result == null)
                    break;
                selection.Add(result);
                input.Remove(result);
            }
            CheckResult(selection, amount);
            return selection;
        }

        public static Dictionary<LMUnlockable, int> CalculateBiasedWeights(List<LMUnlockable> unlocks) {
            var weights = new Dictionary<LMUnlockable, int>();
            if (unlocks.Count < 1) {  return weights; }
            var sumAllPrices = unlocks.Sum(unlock => unlock.OriginalPrice);
            foreach (var unlock in unlocks) {
                int price = Math.Clamp(unlock.OriginalPrice, 1, int.MaxValue);
                weights[unlock] = (int) Math.Clamp(Math.Pow(sumAllPrices / Math.Clamp(unlock.ExtendedLevel.RoutePrice, 50, int.MaxValue), ConfigManager.CheapMoonBias), 1, int.MaxValue);
            }
            Plugin.Instance.Mls.LogDebug($"Cheap moon bias: Assigned the following weights: [ {string.Join(", ", weights.Select(weight => weight.Key.Name + ":" + weight.Value ))} ]");
            return weights;
        }

        private static bool CheckResult<T>(List<T> result, int goal) {
            if (result.Count < goal) {
                Plugin.Instance.Mls.LogWarning("Couldn't select the desired amount of elements!");
                return false;
            }
            return true;
        }
    }
}
