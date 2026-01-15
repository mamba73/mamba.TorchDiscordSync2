using System;
using System.Threading.Tasks;
using mamba.TorchDiscordSync.Config;
using mamba.TorchDiscordSync.Services;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Core
{
    public class VerificationCommandHandler
    {
        private readonly VerificationService _verification;
        private readonly EventLoggingService _eventLog;
        private readonly PluginConfig _config;

        public VerificationCommandHandler(VerificationService verification,
            EventLoggingService eventLog, PluginConfig config)
        {
            _verification = verification;
            _eventLog = eventLog;
            _config = config;
        }

        /// <summary>
        /// Handle in-game /tds verify @DiscordUsername command
        /// </summary>
        public async Task<string> HandleVerifyCommandAsync(long playerSteamID,
            string playerName, string discordUsername)
        {
            try
            {
                // Validate Discord username format
                if (string.IsNullOrWhiteSpace(discordUsername))
                    return "❌ Error: Discord username is required. Usage: /tds verify @DiscordUsername";

                // Clean up Discord username (remove @ if present)
                discordUsername = discordUsername.TrimStart('@').Trim();

                if (discordUsername.Length < 2 || discordUsername.Length > 32)
                    return "❌ Error: Invalid Discord username length (2-32 characters)";

                // Generate verification code
                string code = _verification.GenerateVerificationCode(playerSteamID, playerName, discordUsername);

                if (code == null)
                    return "❌ Error: You already have a pending verification code. It expires in 15 minutes.";

                // Log event
                await _eventLog.LogEventAsync("VerificationRequest",
                    $"{playerName} ({playerSteamID}) requested verification as {discordUsername}");

                // Return instruction to player
                string message = $"✅ Verification code generated: {code}\n" +
                                $"📍 Go to Discord and type: !verify {code}\n" +
                                $"⏱️ Code expires in 15 minutes";

                LoggerUtil.LogInfo($"[VERIFY] {playerName}: {discordUsername} → Code: {code}");
                return message;
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[VERIFY] Command error: {ex.Message}");
                return $"❌ Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Handle Discord bot !verify CODE command
        /// Called from Discord bot webhook or Discord bot integration
        /// </summary>
        public async Task<bool> VerifyFromDiscordAsync(string code, ulong discordUserID,
            string discordUsername)
        {
            try
            {
                bool success = _verification.VerifyCode(code, discordUserID, discordUsername);

                if (success)
                {
                    await _eventLog.LogEventAsync("VerificationComplete",
                        $"{discordUsername} ({discordUserID}) verified with code {code}");

                    LoggerUtil.LogSuccess($"[VERIFY] Discord verification successful: {discordUsername}");
                    return true;
                }
                else
                {
                    await _eventLog.LogEventAsync("VerificationFailed",
                        $"Invalid or expired code: {code}");

                    LoggerUtil.LogWarning($"[VERIFY] Discord verification failed: {code}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[VERIFY] Discord verification error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handle /tds unverify (admin command)
        /// </summary>
        public async Task<string> HandleUnverifyCommandAsync(long steamID, string reason = "Admin removal")
        {
            try
            {
                bool success = _verification.RemoveVerification(steamID, reason);

                if (success)
                {
                    await _eventLog.LogEventAsync("VerificationRemoved",
                        $"SteamID {steamID} unverified: {reason}");

                    return "✅ Verification removed";
                }
                else
                {
                    return "❌ Error: Verification not found";
                }
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[VERIFY] Unverify error: {ex.Message}");
                return $"❌ Error: {ex.Message}";
            }
        }
    }
}