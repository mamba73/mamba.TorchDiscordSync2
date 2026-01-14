using System;
using System.Linq;
using System.Threading.Tasks;
using mamba.TorchDiscordSync.Config;
using mamba.TorchDiscordSync.Models;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Services
{
    public class DeathLogService
    {
        private readonly DatabaseService _db;
        private readonly EventLoggingService _eventLog;
        private readonly DeathMessagesConfig _deathMessages;
        private const int RetaliateWindow = 3600; // 1 sat u sekundama
        private const int RetaliateOldWindow = 86400; // 24 sata u sekundama

        public DeathLogService(DatabaseService db, EventLoggingService eventLog)
        {
            _db = db;
            _eventLog = eventLog;
            _deathMessages = DeathMessagesConfig.Load();
        }

        public async Task LogPlayerDeathAsync(string killerName, string victimName,
            string weapon, long killerSteamID, long victimSteamID, string location = "Unknown")
        {
            try
            {
                string deathType = "Accident";
                string message = "";

                // Ako je isti igrač - suicide
                if (killerSteamID == victimSteamID)
                {
                    deathType = "Suicide";
                    message = string.Format(
                        _deathMessages.GetRandomMessage("Suicide"),
                        victimName
                    );
                }
                else if (killerSteamID > 0) // Kill - neko je ubio
                {
                    // Provjera retalijacije
                    var lastKill = _db.GetLastKill(killerSteamID, victimSteamID);

                    if (lastKill != null)
                    {
                        var timeSinceLastKill = (DateTime.UtcNow - lastKill.DeathTime).TotalSeconds;

                        if (timeSinceLastKill < RetaliateWindow)
                        {
                            // Brza retalijacija (1 sat)
                            deathType = "Retaliate";
                            message = string.Format(
                                _deathMessages.GetRandomMessage("Retaliate"),
                                killerName,
                                victimName
                            );
                        }
                        else if (timeSinceLastKill < RetaliateOldWindow)
                        {
                            // Duža retalijacija (24 sata)
                            deathType = "RetaliateOld";
                            message = string.Format(
                                _deathMessages.GetRandomMessage("RetaliateOld"),
                                killerName,
                                victimName
                            );
                        }
                        else
                        {
                            // Prvi kill nakon dugo vremena
                            deathType = "FirstKill";
                            message = string.Format(
                                _deathMessages.GetRandomMessage("FirstKill"),
                                killerName,
                                victimName,
                                weapon,
                                location
                            );
                        }
                    }
                    else
                    {
                        // Prvi kill ikada protiv ove osobe
                        deathType = "FirstKill";
                        message = string.Format(
                            _deathMessages.GetRandomMessage("FirstKill"),
                            killerName,
                            victimName,
                            weapon,
                            location
                        );
                    }
                }
                else
                {
                    // Accident (asteroid, radiation, itd.)
                    deathType = "Accident";
                    message = string.Format(
                        _deathMessages.GetRandomMessage("Accident"),
                        victimName,
                        location
                    );
                }

                // Spremi u bazu
                _db.LogDeath(killerSteamID, victimSteamID, deathType);

                // Pošalji na Discord i u chat
                await _eventLog.LogDeathAsync(message);

                LoggerUtil.LogInfo($"[DEATH] {message}");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Death log error: {ex.Message}");
            }
        }
    }
}