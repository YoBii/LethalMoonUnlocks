using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;

namespace LethalMoonUnlocks {
    internal static class Logger {
        private static ManualLogSource _mls;

        internal static void Initialize(ManualLogSource mls) {
            _mls = mls;
        }

        internal static void LogDebug(object data) {
            _mls?.LogDebug(data);
        }

        internal static void LogMessage(object data) {
            _mls?.LogMessage(data);
        }

        internal static void LogInfo(object data) {
            _mls?.LogInfo(data);
        }

        internal static void LogWarning(object data) {
            _mls?.LogWarning(data);
        }

        internal static void LogError(object data) {
            _mls?.LogError(data);
        }

        internal static void LogFatal(object data) {
            _mls?.LogFatal(data);
        }

        internal static void Log(LogLevel level, object data) {
            _mls?.Log(level, data);
        }



    }
}
