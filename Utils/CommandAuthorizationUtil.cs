using System;
using System.Collections.Generic;
using System.Linq;
using mamba.TorchDiscordSync.Config;
using mamba.TorchDiscordSync.Models;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Utils
{
    /// <summary>
    /// Handles command authorization and help text generation
    /// </summary>
    public static class CommandAuthorizationUtil
    {
        /// <summary>
        /// Define all available commands
        /// </summary>
        public static List<CommandModel> GetAllCommands()
        {
            return new List<CommandModel>
            {
                // User commands (no admin required)
                new CommandModel(
                    "verify",
                    "Link your Discord account to Space Engineers character",
                    "/tds verify @DiscordName",
                    requiresAdmin: false,
                    minArgs: 1
                ),

                new CommandModel(
                    "status",
                    "Display current plugin and server status",
                    "/tds status",
                    requiresAdmin: false,
                    minArgs: 0
                ),

                new CommandModel(
                    "help",
                    "Show available commands for your authorization level",
                    "/tds help",
                    requiresAdmin: false,
                    minArgs: 0
                ),

                // Admin commands
                new CommandModel(
                    "sync",
                    "Force immediate faction and player synchronization",
                    "/tds sync",
                    requiresAdmin: true,
                    minArgs: 0
                ),

                new CommandModel(
                    "reset",
                    "Clear all Discord roles and channels (WARNING: irreversible)",
                    "/tds reset",
                    requiresAdmin: true,
                    minArgs: 0
                ),

                new CommandModel(
                    "unverify",
                    "Remove verification link for a player (by Steam ID)",
                    "/tds unverify STEAMID [reason]",
                    requiresAdmin: true,
                    minArgs: 1
                )
            };
        }

        /// <summary>
        /// Check if command exists and validate authorization
        /// Returns null if command doesn't exist or user is not authorized
        /// </summary>
        public static CommandModel ValidateCommand(string commandName, long playerSteamID,
            PluginConfig config)
        {
            var command = GetAllCommands().FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

            if (command == null)
                return null; // Command doesn't exist

            // Check authorization
            if (command.RequiresAdmin)
            {
                if (!SecurityUtil.IsPlayerAdmin(playerSteamID, config.AdminSteamIDs))
                    return null; // User not authorized
            }

            return command; // Command is valid and authorized
        }

        /// <summary>
        /// Get all available commands for a user based on authorization level
        /// </summary>
        public static List<CommandModel> GetAvailableCommands(long playerSteamID, PluginConfig config)
        {
            var allCommands = GetAllCommands();
            bool isAdmin = SecurityUtil.IsPlayerAdmin(playerSteamID, config.AdminSteamIDs);

            return allCommands
                .Where(c => !c.RequiresAdmin || isAdmin)
                .ToList();
        }

        /// <summary>
        /// Generate help text based on user authorization
        /// </summary>
        public static string GenerateHelpText(long playerSteamID, PluginConfig config)
        {
            var availableCommands = GetAvailableCommands(playerSteamID, config);
            bool isAdmin = SecurityUtil.IsPlayerAdmin(playerSteamID, config.AdminSteamIDs);

            var helpLines = new List<string>();

            helpLines.Add("");
            helpLines.Add("╔════════════════════════════════════════════════════╗");
            helpLines.Add("║   mamba.TorchDiscordSync v2.0 - Command Help       ║");
            helpLines.Add("╚════════════════════════════════════════════════════╝");
            helpLines.Add("");

            // Separate user and admin commands
            var userCommands = availableCommands.Where(c => !c.RequiresAdmin).ToList();
            var adminCommands = availableCommands.Where(c => c.RequiresAdmin).ToList();

            // Display user commands
            if (userCommands.Any())
            {
                helpLines.Add("📍 PUBLIC COMMANDS:");
                helpLines.Add("");
                foreach (var cmd in userCommands)
                {
                    helpLines.Add($"  {cmd.GetHelpText()}");
                }
                helpLines.Add("");
            }

            // Display admin commands (only if user is admin)
            if (isAdmin && adminCommands.Any())
            {
                helpLines.Add("🔐 ADMIN COMMANDS (Restricted):");
                helpLines.Add("");
                foreach (var cmd in adminCommands)
                {
                    helpLines.Add($"  {cmd.GetHelpText()}");
                }
                helpLines.Add("");
            }

            // Add footer
            if (!isAdmin)
            {
                helpLines.Add("💡 For more commands, contact a server administrator");
            }

            helpLines.Add("");

            return string.Join("\n", helpLines);
        }
    }
}
