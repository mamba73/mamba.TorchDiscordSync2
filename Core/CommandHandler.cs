using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mamba.TorchDiscordSync.Config;
using mamba.TorchDiscordSync.Services;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Core
{
    public class CommandHandler
    {
        private readonly PluginConfig _config;
        private readonly FactionSyncService _syncService;
        private readonly DatabaseService _db;
        private readonly EventLoggingService _eventLog;

        public CommandHandler(PluginConfig config, FactionSyncService syncService,
            DatabaseService db, EventLoggingService eventLog)
        {
            _config = config;
            _syncService = syncService;
            _db = db;
            _eventLog = eventLog;
        }

        public async Task<bool> ExecuteCommandAsync(string command, long playerSteamID,
            string playerName)
        {
            // Provjera je li igrač admin
            if (!_config.AdminSteamIDs.Contains(playerSteamID.ToString()))
            {
                LoggerUtil.LogWarning($"Denied command from {playerName} ({playerSteamID})");
                return false;
            }

            var parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return false;

            var cmd = parts[0].ToLower();

            try
            {
                switch (cmd)
                {
                    case "/tds":
                    case "tds":
                        if (parts.Length < 2)
                            return false;

                        var subcommand = parts[1].ToLower();

                        switch (subcommand)
                        {
                            case "sync":
                                await HandleSyncCommand(playerName);
                                return true;

                            case "reset":
                                await HandleResetCommand(playerName);
                                return true;

                            case "status":
                                await HandleStatusCommand(playerName);
                                return true;

                            default:
                                return false;
                        }

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Command execution failed: {ex.Message}");
                await _eventLog.LogEventAsync("CommandError", $"{playerName}: {ex.Message}");
                return false;
            }
        }

        private async Task HandleSyncCommand(string playerName)
        {
            LoggerUtil.LogInfo($"{playerName} initiated manual sync");
            await _eventLog.LogEventAsync("Command", $"Manual sync triggered by {playerName}");
            Console.WriteLine("[SYNC] Manual sync command queued");
        }

        private async Task HandleResetCommand(string playerName)
        {
            LoggerUtil.LogInfo($"{playerName} initiated Discord reset");

            await _syncService.ResetDiscordAsync();
            await _eventLog.LogEventAsync("Command", $"Discord reset executed by {playerName}");

            LoggerUtil.LogInfo($"Reset completed by {playerName}");
        }

        private async Task HandleStatusCommand(string playerName)
        {
            var factions = _db.GetAllFactions();
            var totalPlayers = factions.Sum(f => f.Players.Count);

            var status = $"Plugin Status - Factions: {factions.Count}, Players: {totalPlayers}, Debug: {_config.Debug}";

            LoggerUtil.LogInfo(status);
            await _eventLog.LogEventAsync("Status", status);
        }
    }
}