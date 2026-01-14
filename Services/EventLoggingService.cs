using System;
using System.Threading.Tasks;
using mamba.TorchDiscordSync.Config;
using mamba.TorchDiscordSync.Models;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Services
{
    public class EventLoggingService
    {
        private readonly DatabaseService _db;
        private readonly DiscordService _discord;
        private readonly PluginConfig _config;

        public EventLoggingService(DatabaseService db, DiscordService discord, PluginConfig config)
        {
            _db = db;
            _discord = discord;
            _config = config;
        }

        public async Task LogEventAsync(string eventType, string details)
        {
            try
            {
                var evt = new EventLogModel
                {
                    EventType = eventType,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _db.LogEvent(evt);

                if (_config.StaffChannelLog != 0)
                {
                    await _discord.SendLogAsync(_config.StaffChannelLog,
                        $"**[{eventType}]** {details}");
                }

                if (_config.Debug)
                    LoggerUtil.LogDebug($"Event logged: {eventType} - {details}", true);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Event logging error: {ex.Message}");
            }
        }

        public async Task LogServerStatusAsync(string status, float simSpeed)
        {
            try
            {
                var message = $"🟢 **Server {status}** | SimSpeed: {simSpeed:F2}";

                if (_config.StaffChannelServerStatus != 0)
                {
                    await _discord.SendLogAsync(_config.StaffChannelServerStatus, message);
                }

                await LogEventAsync("ServerStatus", $"{status} (SimSpeed: {simSpeed:F2})");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Server status logging error: {ex.Message}");
            }
        }

        public async Task LogSimSpeedWarningAsync(float simSpeed)
        {
            try
            {
                var message = $"⚠️ **SIMSPEED ALERT** - Current: {simSpeed:F2} (Threshold: {_config.SimSpeedThreshold:F2})";

                if (_config.StaffChannelStatus != 0)
                {
                    await _discord.SendLogAsync(_config.StaffChannelStatus, message);
                }

                await LogEventAsync("SimSpeedWarning", $"SimSpeed below threshold: {simSpeed:F2}");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"SimSpeed warning error: {ex.Message}");
            }
        }

        public async Task LogDeathAsync(string deathMessage)
        {
            try
            {
                if (_config.EventChannelDeathJoinLeave != 0)
                {
                    await _discord.SendLogAsync(_config.EventChannelDeathJoinLeave, deathMessage);
                }

                await LogEventAsync("Death", deathMessage);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Death logging error: {ex.Message}");
            }
        }

        public async Task LogPlayerJoinAsync(string playerName, long steamID)
        {
            try
            {
                var message = $"➕ **{playerName}** ({steamID}) joined the server";

                if (_config.EventChannelDeathJoinLeave != 0)
                {
                    await _discord.SendLogAsync(_config.EventChannelDeathJoinLeave, message);
                }

                await LogEventAsync("PlayerJoin", $"{playerName} ({steamID})");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Player join logging error: {ex.Message}");
            }
        }

        public async Task LogPlayerLeaveAsync(string playerName, long steamID)
        {
            try
            {
                var message = $"➖ **{playerName}** ({steamID}) left the server";

                if (_config.EventChannelDeathJoinLeave != 0)
                {
                    await _discord.SendLogAsync(_config.EventChannelDeathJoinLeave, message);
                }

                await LogEventAsync("PlayerLeave", $"{playerName} ({steamID})");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Player leave logging error: {ex.Message}");
            }
        }

        public async Task LogSyncCompleteAsync(int factionsCount, int playersCount)
        {
            try
            {
                var message = $"✅ **Sync Complete** - Factions: {factionsCount}, Players: {playersCount}";

                if (_config.StaffChannelLog != 0)
                {
                    await _discord.SendLogAsync(_config.StaffChannelLog, message);
                }

                await LogEventAsync("SyncComplete", $"{factionsCount} factions, {playersCount} players");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Sync logging error: {ex.Message}");
            }
        }
    }
}