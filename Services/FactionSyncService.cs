using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mamba.TorchDiscordSync.Models;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Services
{
    public class FactionSyncService
    {
        private readonly DatabaseService _db;
        private readonly DiscordService _discord;

        public FactionSyncService(DatabaseService db, DiscordService discord)
        {
            _db = db;
            _discord = discord;
        }

        public async Task SyncFactionsAsync(List<FactionModel> factions)
        {
            try
            {
                LoggerUtil.LogInfo("[FACTION_SYNC] Starting faction synchronization");

                foreach (var faction in factions)
                {
                    if (faction.Tag.Length != 3) continue; // Samo player fakcije

                    var existing = _db.GetFaction(faction.FactionID);

                    if (existing == null)
                    {
                        faction.DiscordRoleID = await _discord.CreateRoleAsync(faction.Tag);
                        faction.DiscordChannelID = await _discord.CreateChannelAsync(faction.Name.ToLower());
                        _db.SaveFaction(faction);
                        LoggerUtil.LogInfo($"New faction created: {faction.Tag} - {faction.Name}");
                    }
                    else
                    {
                        faction.DiscordRoleID = existing.DiscordRoleID;
                        faction.DiscordChannelID = existing.DiscordChannelID;
                        _db.SaveFaction(faction);
                    }

                    // Sync igrače
                    foreach (var player in faction.Players)
                    {
                        player.SyncedNick = $"[{faction.Tag}] {player.OriginalNick}";
                        var playerModel = new PlayerModel
                        {
                            PlayerID = player.PlayerID,
                            SteamID = player.SteamID,
                            OriginalNick = player.OriginalNick,
                            SyncedNick = player.SyncedNick,
                            FactionID = faction.FactionID,
                            DiscordUserID = player.DiscordUserID
                        };
                        _db.SavePlayer(playerModel);
                    }
                }

                LoggerUtil.LogInfo("[FACTION_SYNC] Synchronization complete");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Faction sync error: {ex.Message}");
            }
        }

        public async Task ResetDiscordAsync()
        {
            try
            {
                LoggerUtil.LogInfo("[FACTION_SYNC] Starting Discord reset");

                var factions = _db.GetAllFactions();
                foreach (var faction in factions)
                {
                    if (faction.DiscordRoleID != 0)
                        await _discord.DeleteRoleAsync(faction.DiscordRoleID);
                    if (faction.DiscordChannelID != 0)
                        await _discord.DeleteChannelAsync(faction.DiscordChannelID);
                }

                _db.ClearAllData();
                LoggerUtil.LogInfo("[FACTION_SYNC] Discord reset complete");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Discord reset error: {ex.Message}");
            }
        }
    }
}