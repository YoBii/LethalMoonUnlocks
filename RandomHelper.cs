using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalMoonUnlocks {
    internal static class RandomHelper {
        private static readonly object RandomLock = new();
        private static readonly Random Random = new(unchecked(Environment.TickCount ^ (int)DateTime.UtcNow.Ticks));

        internal static int Range(int minInclusive, int maxExclusive) {
            lock (RandomLock) {
                return Random.Next(minInclusive, maxExclusive);
            }
        }

        internal static bool Chance(int percent) {
            if (percent <= 0) {
                return false;
            }
            if (percent >= 100) {
                return true;
            }
            return Range(0, 100) < percent;
        }

        internal static List<T> Select<T>(List<T> objects, int amount) {
            if (objects.Count < amount || objects.Count == 0 || objects == null)
                return objects;
            List<T> input = new List<T>(objects);
            List<T> selection = new List<T>();
            while (selection.Count < amount) {
                selection.Add(input[RandomHelper.Range(0, input.Count)]);
                input.Remove(selection.Last());
            }
            if (selection.Count < amount) {
                Logger.LogWarning("Couldn't select the desired amount of elements!");
            }
            return selection;
        }

        internal static List<T> SelectWeighted<T>(Dictionary<T, int> objects, int amount) {
            if (objects.Count < amount || objects.Count == 0 || objects == null)
                return new List<T>(objects.Keys);
            Dictionary<T, int> input = new Dictionary<T, int>(objects);
            List<T> selection = new List<T>();

            while (selection.Count < amount) {
                int totalWeight = input.Sum(entry => entry.Value);
                int random = RandomHelper.Range(0, totalWeight);
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
            if (selection.Count < amount) {
                Logger.LogWarning("Couldn't select the desired amount of elements!");
            }
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
    }
}
