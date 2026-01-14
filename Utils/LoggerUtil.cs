using System;

namespace mamba.TorchDiscordSync.Utils
{
    public static class LoggerUtil
    {
        private const string PREFIX = "[mamba.TorchDiscordSync]";

        public static void Log(string category, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"{PREFIX} [{timestamp}] [{category}] {message}");
        }

        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogWarning(string message)
        {
            Log("WARN", message);
        }

        public static void LogError(string message)
        {
            Log("ERROR", message);
        }

        public static void LogDebug(string message, bool debugMode = false)
        {
            if (debugMode)
                Log("DEBUG", message);
        }

        public static void LogSuccess(string message)
        {
            Log("SUCCESS", message);
        }
    }
}
