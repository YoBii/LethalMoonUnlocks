using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;

namespace LethalMoonUnlocks {
    internal static class Logger {
        private static ManualLogSource _mls;

        public static void Initialize(ManualLogSource mls) {
            _mls = mls;
        }

        public static void LogDebug(object data) {
            _mls?.LogDebug(data);
        }

        public static void LogMessage(object data) {
            _mls?.LogMessage(data);
        }

        public static void LogInfo(object data) {
            _mls?.LogInfo(data);
        }

        public static void LogWarning(object data) {
            _mls?.LogWarning(data);
        }

        public static void LogError(object data) {
            _mls?.LogError(data);
        }

        public static void LogFatal(object data) {
            _mls?.LogFatal(data);
        }

        public static void Log(LogLevel level, object data) {
            _mls?.Log(level, data);
        }



    }
}
