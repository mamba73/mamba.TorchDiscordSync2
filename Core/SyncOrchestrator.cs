using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mamba.TorchDiscordSync.Config;
using mamba.TorchDiscordSync.Models;
using mamba.TorchDiscordSync.Services;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Core
{
    /// <summary>
    /// Orchestrates all synchronization operations
    /// Coordinates between multiple services to ensure proper sync flow
    /// </summary>
    public class SyncOrchestrator
    {
        private readonly DatabaseService _db;
        private readonly DiscordService _discord;
        private readonly FactionSyncService _factionSync;
        private readonly EventLoggingService _eventLog;
        private readonly PluginConfig _config;

        public SyncOrchestrator(DatabaseService db, DiscordService discord,
            FactionSyncService factionSync, EventLoggingService eventLog, PluginConfig config)
        {
            _db = db;
            _discord = discord;
            _factionSync = factionSync;
            _eventLog = eventLog;
            _config = config;
        }

        /// <summary>
        /// Execute full synchronization of factions, players, and Discord objects
        /// </summary>
        public async Task ExecuteFullSyncAsync(List<FactionModel> factions)
        {
            try
            {
                LoggerUtil.LogInfo("[ORCHESTRATOR] Starting full synchronization");

                // 1. Synchronize database
                LoggerUtil.LogDebug("Syncing database...", _config.Debug);
                foreach (var faction in factions)
                {
                    if (faction.Tag.Length == 3) // Only player factions
                    {
                        _db.SaveFaction(faction);
                    }
                }

                // 2. Synchronize Discord
                LoggerUtil.LogDebug("Syncing Discord...", _config.Debug);
                var playerFactions = factions.FindAll(f => f.Tag.Length == 3);
                await _factionSync.SyncFactionsAsync(playerFactions);

                // 3. Log completion
                var playerCount = playerFactions.Sum(f => f.Players.Count);
                await _eventLog.LogSyncCompleteAsync(playerFactions.Count, playerCount);

                LoggerUtil.LogInfo("[ORCHESTRATOR] Synchronization complete");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[ORCHESTRATOR] Sync error: {ex.Message}");
                await _eventLog.LogEventAsync("SyncError", ex.Message);
            }
        }

        /// <summary>
        /// Check and report server status
        /// </summary>
        public async Task CheckServerStatusAsync(float currentSimSpeed)
        {
            try
            {
                // Check SimSpeed threshold
                if (currentSimSpeed < _config.SimSpeedThreshold)
                {
                    await _eventLog.LogSimSpeedWarningAsync(currentSimSpeed);
                }

                // Log server status
                await _eventLog.LogServerStatusAsync("UP", currentSimSpeed);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[ORCHESTRATOR] Status check error: {ex.Message}");
            }
        }
    }
}