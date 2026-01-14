using System;
using System.Linq;
using System.Threading.Tasks;
using mamba.TorchDiscordSync.Config;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Services
{
    public class ChatSyncService
    {
        private readonly DiscordService _discord;
        private readonly PluginConfig _config;
        private readonly DatabaseService _db;

        public ChatSyncService(DiscordService discord, PluginConfig config, DatabaseService db)
        {
            _discord = discord;
            _config = config;
            _db = db;
        }

        public async Task SendGameChatToDiscordAsync(long playerSteamID, string playerName, string message)
        {
            try
            {
                // Pronađi faktciju igrača
                var factions = _db.GetAllFactions();
                var playerFaction = factions.FirstOrDefault(f =>
                    f.Players.Any(p => p.SteamID == playerSteamID));

                if (playerFaction == null)
                    return;

                var sanitizedMsg = SecurityUtil.SanitizeMessage(message);
                var formattedMsg = $"**{playerName}**: {sanitizedMsg}";

                if (playerFaction.DiscordChannelID != 0)
                {
                    await _discord.SendLogAsync(playerFaction.DiscordChannelID, formattedMsg);
                }

                if (_config.Debug)
                    LoggerUtil.LogDebug($"Game -> Discord ({playerFaction.Tag}): {formattedMsg}", true);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"ChatSync error: {ex.Message}");
            }
        }

        public async Task SendDiscordChatToGameAsync(string discordUsername, string message,
            ulong channelId)
        {
            try
            {
                // Pronađi koji je kanal
                var factions = _db.GetAllFactions();
                var faction = factions.FirstOrDefault(f => f.DiscordChannelID == channelId);

                if (faction == null)
                    return;

                var sanitizedMsg = SecurityUtil.SanitizeMessage(message);
                var formattedMsg = $"[{faction.Tag} - Discord] {discordUsername}: {sanitizedMsg}";

                // Ovo bi trebalo biti poslano u Space Engineers server chat preko Torch API
                Console.WriteLine($"[CHAT_SYNC] {formattedMsg}");

                if (_config.Debug)
                    LoggerUtil.LogDebug($"Discord -> Game ({faction.Tag}): {formattedMsg}", true);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"ChatSync error: {ex.Message}");
            }
        }
    }
}