using BepInEx;
using BepInEx.Configuration;
using LethalConstellations.PluginCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LethalMoonUnlocks.Compatibility {
    internal sealed class ConstellationUnlockConditionsConfig {
        private const string SectionPrefix = "Constellation: ";
        private readonly string _configPath;
        private readonly ConfigFile _configFile;
        private readonly Dictionary<string, ConstellationUnlockRuleDefinition> _ruleDefinitions = new(StringComparer.OrdinalIgnoreCase);

        internal ConstellationUnlockConditionsConfig() {
            _configPath = Path.Combine(Paths.ConfigPath, $"{PluginMetadata.PLUGIN_GUID}.constellations.generated.cfg");
            EnsureConfigFileExists();
            _configFile = new ConfigFile(_configPath, false);
            RefreshDefinitions();
        }

        internal IReadOnlyDictionary<string, ConstellationUnlockRuleDefinition> RuleDefinitions => _ruleDefinitions;

        internal bool TryGetRuleDefinition(string constellationName, out ConstellationUnlockRuleDefinition ruleDefinition) {
            ruleDefinition = null;
            if (string.IsNullOrWhiteSpace(constellationName)) {
                return false;
            }

            return _ruleDefinitions.TryGetValue(constellationName, out ruleDefinition) && ruleDefinition != null;
        }

        internal void RefreshDefinitions(bool saveChanges = true) {
            _ruleDefinitions.Clear();
            EnsureConfigFileExists();
            _configFile.Reload();

            foreach (var constellation in Collections.ConstellationStuff) {
                if (string.IsNullOrWhiteSpace(constellation.consName)) {
                    continue;
                }

                var ruleDefinition = BindRuleDefinition(constellation.consName);
                if (ruleDefinition != null) {
                    _ruleDefinitions[constellation.consName] = ruleDefinition;
                }
            }

            if (saveChanges) {
                _configFile.Save();
            }
        }

        private void EnsureConfigFileExists() {
            string configDirectory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrWhiteSpace(configDirectory)) {
                Directory.CreateDirectory(configDirectory);
            }

            if (!File.Exists(_configPath)) {
                File.WriteAllText(_configPath, string.Empty);
            }
        }

        private ConstellationUnlockRuleDefinition BindRuleDefinition(string constellationName) {
            string section = $"{SectionPrefix}{constellationName}";
            bool enabled = BindValue(section, "Enabled", false, "Enable custom unlock conditions for this constellation.");
            string matchModeValue = BindValue(section, "MatchMode", ConstellationUnlockMatchMode.Any.ToString(),
                "How active conditions are combined. Any requires one active condition to pass. All requires every active condition to pass.",
                new AcceptableValueList<string>([ConstellationUnlockMatchMode.Any.ToString(), ConstellationUnlockMatchMode.All.ToString()]));
            int requiredQuotaCount = BindValue(section, "RequiredQuotaCount", 0,
                "Unlock when the quota count reaches this value. Set to 0 to disable this condition.",
                new AcceptableValueRange<int>(0, 1000));
            string requiredVisitedMoons = BindValue(section, "RequiredVisitedMoons", string.Empty,
                "Unlock when every moon in this comma-separated list has been visited at least once. Leave empty to disable this condition.");
            int requiredUniqueMoonVisits = BindValue(section, "RequiredUniqueMoonVisits", 0,
                "Unlock when at least this many different moons have been visited. Set to 0 to disable this condition.",
                new AcceptableValueRange<int>(0, 1000));
            bool ignoreDefaultMoonStoryLock = BindValue(section, "IgnoreDefaultMoonStoryLock", false,
                "When enabled, the constellation may unlock even if its default moon is still story-locked by another source.");

            return new ConstellationUnlockRuleDefinition(
                constellationName,
                enabled,
                ParseMatchMode(matchModeValue),
                requiredQuotaCount,
                ParseRequiredVisitedMoons(requiredVisitedMoons),
                requiredUniqueMoonVisits,
                ignoreDefaultMoonStoryLock);
        }

        private T BindValue<T>(string section, string key, T defaultValue, string description) {
            return _configFile.Bind(section, key, defaultValue, description).Value;
        }

        private T BindValue<T>(string section, string key, T defaultValue, string description, AcceptableValueBase acceptableValues) {
            return _configFile.Bind(section, key, defaultValue, new ConfigDescription(description, acceptableValues)).Value;
        }

        private static ConstellationUnlockMatchMode ParseMatchMode(string value) {
            if (Enum.TryParse(value, ignoreCase: true, out ConstellationUnlockMatchMode matchMode)) {
                return matchMode;
            }

            return ConstellationUnlockMatchMode.Any;
        }

        private static string[] ParseRequiredVisitedMoons(string value) {
            if (string.IsNullOrWhiteSpace(value)) {
                return Array.Empty<string>();
            }

            HashSet<string> seenMoonNames = new(StringComparer.OrdinalIgnoreCase);
            List<string> moonNames = [];
            foreach (string candidate in value.Split(',')) {
                string moonName = candidate?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(moonName) || !seenMoonNames.Add(moonName)) {
                    continue;
                }

                moonNames.Add(moonName);
            }

            return moonNames.ToArray();
        }
    }

    internal enum ConstellationUnlockMatchMode {
        Any,
        All
    }

    internal sealed class ConstellationUnlockRuleDefinition {
        internal string ConstellationName { get; }
        internal bool Enabled { get; }
        internal ConstellationUnlockMatchMode MatchMode { get; }
        internal int RequiredQuotaCount { get; }
        internal IReadOnlyList<string> RequiredVisitedMoons { get; }
        internal int RequiredUniqueMoonVisits { get; }
        internal bool IgnoreDefaultMoonStoryLock { get; }
        internal bool HasActiveConditions =>
            RequiredQuotaCount > 0
            || RequiredVisitedMoons.Count > 0
            || RequiredUniqueMoonVisits > 0;
        internal bool IsEnabled => Enabled && HasActiveConditions;

        internal ConstellationUnlockRuleDefinition(
            string constellationName,
            bool enabled,
            ConstellationUnlockMatchMode matchMode,
            int requiredQuotaCount,
            IReadOnlyList<string> requiredVisitedMoons,
            int requiredUniqueMoonVisits,
            bool ignoreDefaultMoonStoryLock) {
            ConstellationName = constellationName ?? string.Empty;
            Enabled = enabled;
            MatchMode = matchMode;
            RequiredQuotaCount = requiredQuotaCount;
            RequiredVisitedMoons = requiredVisitedMoons?.ToArray() ?? Array.Empty<string>();
            RequiredUniqueMoonVisits = requiredUniqueMoonVisits;
            IgnoreDefaultMoonStoryLock = ignoreDefaultMoonStoryLock;
        }
    }
}
