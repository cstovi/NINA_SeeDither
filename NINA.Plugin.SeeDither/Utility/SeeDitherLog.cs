using System;
using NINA.Core.Utility;

namespace NINA.Plugin.SeeDither.Utility {
    internal static class SeeDitherLog {
        private const string Tag = "[SeeDither] ";

        public static void Info(string message) {
            try { Logger.Info(Tag + message); } catch { /* swallow */ }
        }

        public static void Debug(string message) {
            try { Logger.Debug(Tag + message); } catch { /* swallow */ }
        }

        public static void Warn(string message) {
            try { Logger.Warning(Tag + message); } catch { /* swallow */ }
        }

        public static void Error(string message, Exception ex = null) {
            try {
                if (ex == null)
                    Logger.Error(Tag + message);
                else
                    Logger.Error(Tag + message, ex);
            } catch { /* swallow */ }
        }
    }
}
