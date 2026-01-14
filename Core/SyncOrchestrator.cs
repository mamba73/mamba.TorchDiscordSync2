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

        public async Task ExecuteFullSyncAsync(List<FactionModel> factions)
        {
            try
            {
                LoggerUtil.LogInfo("[ORCHESTRATOR] Starting full synchronization");

                // 1. Sinkronizacija baze podataka
                LoggerUtil.LogDebug("Syncing database...", _config.Debug);
                foreach (var faction in factions)
                {
                    if (faction.Tag.Length == 3) // Samo player fakcije
                    {
                        _db.SaveFaction(faction);
                    }
                }

                // 2. Sinkronizacija Discorda
                LoggerUtil.LogDebug("Syncing Discord...", _config.Debug);
                var playerFactions = factions.FindAll(f => f.Tag.Length == 3);
                await _factionSync.SyncFactionsAsync(playerFactions);

                // 3. Logiranje
                var playerCount = playerFactions.Sum(f => f.Players.Count);
                await _eventLog.LogSyncCompleteAsync(playerFactions.Count, playerCount);

                LoggerUtil.LogInfo("[ORCHESTRATOR] Synchronization complete");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Orchestrator error: {ex.Message}");
                await _eventLog.LogEventAsync("SyncError", ex.Message);
            }
        }

        public async Task CheckServerStatusAsync(float currentSimSpeed)
        {
            try
            {
                // Provjera SimSpeed-a
                if (currentSimSpeed < _config.SimSpeedThreshold)
                {
                    await _eventLog.LogSimSpeedWarningAsync(currentSimSpeed);
                }

                // Log server status
                await _eventLog.LogServerStatusAsync("UP", currentSimSpeed);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Status check failed: {ex.Message}");
            }
        }
    }
}