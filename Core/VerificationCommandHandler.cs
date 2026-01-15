using System;
using System.Threading.Tasks;
using mamba.TorchDiscordSync.Config;
using mamba.TorchDiscordSync.Services;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Core
{
    /// <summary>
    /// Handles all verification-related commands from both in-game and Discord
    /// Coordinates between VerificationService and DiscordBotService
    /// </summary>
    public class VerificationCommandHandler
    {
        private readonly VerificationService _verification;
        private readonly EventLoggingService _eventLog;
        private readonly PluginConfig _config;
        private readonly DiscordBotService _discordBot;
        private readonly DiscordBotConfig _discordBotConfig;

        public VerificationCommandHandler(VerificationService verification,
           EventLoggingService eventLog, PluginConfig config, DiscordBotService discordBot,
           DiscordBotConfig discordBotConfig)  
        {
            _verification = verification;
            _eventLog = eventLog;
            _config = config;
            _discordBot = discordBot;
            _discordBotConfig = discordBotConfig;  
        }

        /// <summary>
        /// Handle in-game /tds verify @DiscordUsername command
        /// Generates verification code and sends DM to Discord user
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

                // Send DM to Discord user with verification instructions
                bool dmSent = await _discordBot.SendVerificationDMAsync(discordUsername, code);

                if (!dmSent)
                {
                    return $"❌ Error: Could not find Discord user '{discordUsername}' or send DM.\n" +
                           $"Make sure:\n" +
                           $"  • Username is correct\n" +
                           $"  • User is in the Discord server\n" +
                           $"  • Bot has DM permissions";
                }

                // Log event
                await _eventLog.LogEventAsync("VerificationRequest",
                    $"{playerName} ({playerSteamID}) requested verification as {discordUsername}. DM sent.");

                // Return instruction to player
                string message = $"✅ Verification code sent to {discordUsername} via DM!\n" +
                                $"📧 Check your Discord private messages\n" +
                                $"⏱️ Code expires in {_discordBotConfig.VerificationCodeExpirationMinutes} minutes";

                LoggerUtil.LogInfo($"[VERIFY] {playerName}: {discordUsername} → Code: {code} → DM sent");
                return message;
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[VERIFY] Command error: {ex.Message}");
                return $"❌ Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Handle verification from Discord bot (!verify CODE command)
        /// Called when user completes verification on Discord
        /// </summary>
        public async Task<string> VerifyFromDiscordAsync(string code, ulong discordUserID,
            string discordUsername)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    return "❌ Error: Verification code is required";

                bool success = _verification.VerifyCode(code, discordUserID, discordUsername);

                if (success)
                {
                    // Send success DM
                    await _discordBot.SendVerificationSuccessDMAsync(discordUsername, "Verified", 0);

                    await _eventLog.LogEventAsync("VerificationComplete",
                        $"{discordUsername} ({discordUserID}) verified with code {code}");

                    LoggerUtil.LogSuccess($"[VERIFY] Discord verification successful: {discordUsername}");
                    return "✅ Verification successful! Your account is now linked.";
                }
                else
                {
                    await _eventLog.LogEventAsync("VerificationFailed",
                        $"Invalid or expired code: {code}");

                    LoggerUtil.LogWarning($"[VERIFY] Discord verification failed: {code}");
                    return "❌ Verification failed. Code may be invalid or expired. Try again with /tds verify in-game.";
                }
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[VERIFY] Discord verification error: {ex.Message}");
                return $"❌ Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Handle /tds unverify STEAMID [reason] command (admin only)
        /// Removes verification link for a player
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

                    LoggerUtil.LogSuccess($"[VERIFY] Unverified SteamID {steamID}: {reason}");
                    return "✅ Verification removed";
                }
                else
                {
                    return "❌ Error: Verification not found for this Steam ID";
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