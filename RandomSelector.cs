using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace LethalMoonUnlocks {
    internal static class RandomSelector {
        private static readonly Random Random = new(DateTime.Now.Millisecond);
        internal static List<T> Get<T>(List<T> objects, int amount) {
            if (objects.Count < amount || objects.Count == 0 || objects == null)
                return objects;
            List<T> input = new List<T>(objects);
            List<T> selection = new List<T>();
            while (selection.Count < amount) {
                selection.Add(input[Random.Next(0, input.Count)]);
                input.Remove(selection.Last());
            }
            CheckResult(selection, amount);
            return selection;
        }

        internal static List<T> GetWeighted<T>(Dictionary<T, int> objects, int amount) {
            if (objects.Count < amount || objects.Count == 0 || objects == null)
                return new List<T>(objects.Keys);
            Dictionary<T, int> input = new Dictionary<T, int>(objects);
            List<T> selection = new List<T>();

            while (selection.Count < amount) {
                int totalWeight = input.Sum(entry => entry.Value);
                int random = Random.Next(0, totalWeight);
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

        internal static Dictionary<LMUnlockable, int> CalculateBiasedWeights(List<LMUnlockable> unlocks, float bias) {
            var weights = new Dictionary<LMUnlockable, int>();
            if (unlocks.Count < 1) {  return weights; }

            const double scale = 1000d;
            var prices = unlocks.ToDictionary(unlock => unlock, unlock => ConfigManager.CheapMoonBiasIgnorePriceChanges
                ? Math.Clamp(unlock.OriginalPrice, 1, int.MaxValue)
                : Math.Clamp(unlock.ExtendedLevel.RoutePrice, 1, int.MaxValue));
            double averagePrice = prices.Values.Average();

            foreach (var unlock in unlocks) {
                double relativeCheapness = averagePrice / prices[unlock];
                long result = Math.Clamp((long)Math.Round(Math.Pow(relativeCheapness, bias) * scale), 1, int.MaxValue / (unlocks.Count + 1));
                weights[unlock] = (int)result;
            }
            Logger.LogDebug($"Cheap moon bias: Assigned the following weights: [ {string.Join(", ", weights.Select(weight => weight.Key.Name + ":" + weight.Value ))} ]");
            return weights;
        }

        private static bool CheckResult<T>(List<T> result, int goal) {
            if (result.Count < goal) {
                Logger.LogWarning("Couldn't select the desired amount of elements!");
                return false;
            }
            return true;
        }
    }
}
