using System;
using System.Collections.Generic;
using System.Linq;

namespace mamba.TorchDiscordSync.Utils
{
    public static class SecurityUtil
    {
        public static bool IsPlayerAdmin(long playerSteamID, List<string> adminSteamIDs)
        {
            if (adminSteamIDs == null || adminSteamIDs.Count == 0)
                return false;

            return adminSteamIDs.Contains(playerSteamID.ToString());
        }

        public static string SanitizeMessage(string input, int maxLength = 2000)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            // Uklanja opasne znakove za Discord
            var sanitized = input
                .Replace("@", "ᴀ")
                .Replace("<", "[")
                .Replace(">", "]")
                .Replace("```", "''");

            // Skrati ako je predugo
            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength - 3) + "...";

            return sanitized;
        }

        public static bool IsValidSteamID(long steamID)
        {
            // Steam ID bi trebao biti između 76561198000000000 i 76561202255233023
            return steamID >= 76561198000000000 && steamID <= 76561202255233023;
        }

        public static bool ValidateCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            // Dozvoljeni znakovi: a-z, 0-9, space, /
            return command.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '/');
        }
    }
}