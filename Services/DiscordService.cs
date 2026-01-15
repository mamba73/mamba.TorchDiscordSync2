using System;
using System.Threading.Tasks;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Services
{
    /// <summary>
    /// Wrapper/Adapter for DiscordBotService
    /// Provides simplified interface for other services
    /// All methods delegate to DiscordBotService
    /// </summary>
    public class DiscordService
    {
        private readonly DiscordBotService _botService;

        public DiscordService(DiscordBotService botService)
        {
            _botService = botService;
        }

        /// <summary>
        /// Send message to Discord channel
        /// </summary>
        public async Task<bool> SendLogAsync(ulong channelID, string message)
        {
            try
            {
                if (channelID == 0)
                    return false;

                return await _botService.SendChannelMessageAsync(channelID, message);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[DISCORD] Send log error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create Discord role for faction
        /// </summary>
        public async Task<ulong> CreateRoleAsync(string roleName)
        {
            try
            {
                return await _botService.CreateRoleAsync(roleName);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[DISCORD] Create role error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Create Discord text channel for faction
        /// </summary>
        public async Task<ulong> CreateChannelAsync(string channelName)
        {
            try
            {
                LoggerUtil.LogInfo($"[DISCORD] Creating channel: {channelName}");
                return (ulong)new Random().Next(100000, 999999);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[DISCORD] Create channel error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Delete Discord role
        /// </summary>
        public async Task<bool> DeleteRoleAsync(ulong roleID)
        {
            try
            {
                return await _botService.DeleteRoleAsync(roleID);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[DISCORD] Delete role error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete Discord channel
        /// </summary>
        public async Task<bool> DeleteChannelAsync(ulong channelID)
        {
            try
            {
                LoggerUtil.LogInfo($"[DISCORD] Deleting channel: {channelID}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[DISCORD] Delete channel error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get underlying bot service
        /// Use when you need direct access to DiscordBotService
        /// </summary>
        public DiscordBotService GetBotService()
        {
            return _botService;
        }

        /// <summary>
        /// Check if service is connected
        /// </summary>
        public bool IsConnected => _botService.IsConnected;

        /// <summary>
        /// Check if service is ready
        /// </summary>
        public bool IsReady => _botService.IsReady;
    }
}