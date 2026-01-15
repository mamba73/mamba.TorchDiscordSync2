using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using mamba.TorchDiscordSync.Config;
using mamba.TorchDiscordSync.Core;
using mamba.TorchDiscordSync.Models;
using mamba.TorchDiscordSync.Services;
using mamba.TorchDiscordSync.Utils;

namespace mamba.TorchDiscordSync.Plugin
{
    /// <summary>
    /// mamba.TorchDiscordSync v2.0.0
    /// 
    /// Advanced Space Engineers faction/Discord sync plugin with death logging,
    /// chat sync, server monitoring, and admin commands.
    /// 
    /// Features:
    /// - Faction ↔ Discord synchronization
    /// - Death/kill logging with retaliation detection
    /// - Bidirectional chat synchronization
    /// - Server SimSpeed monitoring
    /// - Admin command support
    /// - XML-based persistence (no SQLite)
    /// - Configurable death messages
    /// </summary>
    public class MambaTorchDiscordSyncPlugin : TorchPluginBase
    {
        private DatabaseService _db;
        private FactionSyncService _factionSync;
        private DiscordService _discord;
        private EventLoggingService _eventLog;
        private ChatSyncService _chatSync;
        private DeathLogService _deathLog;
        private CommandHandler _commandHandler;
        private SyncOrchestrator _orchestrator;
        private PluginConfig _config;

        private Timer _syncTimer;
        private ITorchSession _currentSession;
        private bool _isInitialized = false;
        private bool _serverStartupLogged = false;

        // ADD verification:
        private VerificationService _verification;
        private VerificationCommandHandler _verificationCommandHandler;


        /// <summary>
        /// Plugin initialization - called when Torch loads the plugin
        /// </summary>
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            try
            {
                PrintBanner("INITIALIZING");

                // Load configuration
                _config = PluginConfig.Load();
                LoggerUtil.LogInfo($"Configuration loaded - Debug mode: {_config.Debug}");

                // Initialize database service (XML-based)
                _db = new DatabaseService();
                LoggerUtil.LogSuccess("Database service initialized (XML-based)");

                // Initialize Discord service
                _discord = new DiscordService(_config.DiscordToken, _config.GuildID);
                Task.Run(async () => await _discord.ConnectAsync());
                LoggerUtil.LogInfo("Discord service initialized");

                // Initialize remaining services
                _eventLog = new EventLoggingService(_db, _discord, _config);
                _factionSync = new FactionSyncService(_db, _discord);
                _chatSync = new ChatSyncService(_discord, _config, _db);
                _deathLog = new DeathLogService(_db, _eventLog);
                _commandHandler = new CommandHandler(_config, _factionSync, _db, _eventLog);
                _orchestrator = new SyncOrchestrator(_db, _discord, _factionSync, _eventLog, _config);

                // ADD verification:
                _verification = new VerificationService(_db);
                _verificationCommandHandler = new VerificationCommandHandler(_verification, _eventLog, _config);

                LoggerUtil.LogSuccess("All services initialized");

                // Register session event handler
                var sessionManager = torch.Managers.GetManager<ITorchSessionManager>();
                if (sessionManager != null)
                {
                    sessionManager.SessionStateChanged += OnSessionStateChanged;
                    LoggerUtil.LogSuccess("Session manager hooked");
                }
                else
                {
                    LoggerUtil.LogError("Session manager not available!");
                }

                // Initialize sync timer
                _syncTimer = new Timer(_config.SyncIntervalSeconds * 1000);
                _syncTimer.Elapsed += OnSyncTimerElapsed;
                _syncTimer.AutoReset = true;

                _isInitialized = true;
                PrintBanner("INITIALIZATION COMPLETE");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Plugin initialization failed: {ex.Message}");
                LoggerUtil.LogError($"Stack trace: {ex.StackTrace}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Called when server session state changes
        /// </summary>
        private void OnSessionStateChanged(ITorchSession session, TorchSessionState state)
        {
            try
            {
                _currentSession = session;

                switch (state)
                {
                    case TorchSessionState.Loading:
                        LoggerUtil.LogInfo("═══ Server session LOADING ═══");
                        _serverStartupLogged = false;
                        break;

                    case TorchSessionState.Loaded:
                        LoggerUtil.LogSuccess("═══ Server session LOADED ═══");
                        _serverStartupLogged = false;

                        // Run startup routines
                        if (_isInitialized)
                        {
                            Task.Run(async () => await OnServerLoadedAsync(session));
                        }
                        break;

                    case TorchSessionState.Unloading:
                        LoggerUtil.LogInfo("═══ Server session UNLOADING ═══");
                        if (_syncTimer != null && _syncTimer.Enabled)
                            _syncTimer.Stop();
                        break;

                    case TorchSessionState.Unloaded:
                        LoggerUtil.LogWarning("═══ Server session UNLOADED ═══");
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Session state change error: {ex.Message}");
            }
        }

        /// <summary>
        /// Runs after server is fully loaded
        /// </summary>
        private async Task OnServerLoadedAsync(ITorchSession session)
        {
            try
            {
                if (_serverStartupLogged)
                    return; // Prevent duplicate logging

                _serverStartupLogged = true;

                LoggerUtil.LogInfo("[STARTUP] Initializing server sync...");

                // Get current SimSpeed
                float currentSimSpeed = 1.0f; // Placeholder
                if (session != null)
                {
                    try
                    {
                        // TODO: Get real SimSpeed from session
                        // currentSimSpeed = GetSimSpeedFromSession(session);
                    }
                    catch { }
                }

                // Check server status
                await _orchestrator.CheckServerStatusAsync(currentSimSpeed);

                // Load factions from save
                var factions = LoadFactionsFromSession(session);
                if (factions.Count > 0)
                {
                    LoggerUtil.LogInfo($"[STARTUP] Found {factions.Count} player factions");
                    await _orchestrator.ExecuteFullSyncAsync(factions);
                }
                else
                {
                    LoggerUtil.LogWarning("[STARTUP] No player factions found (tag length != 3)");
                }

                // Start periodic sync timer
                if (_syncTimer != null && !_syncTimer.Enabled)
                {
                    _syncTimer.Start();
                    LoggerUtil.LogSuccess($"[STARTUP] Sync timer started (interval: {_config.SyncIntervalSeconds}s)");
                }

                LoggerUtil.LogSuccess("[STARTUP] Server startup sync complete!");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[STARTUP] Error: {ex.Message}");
                await _eventLog.LogEventAsync("StartupError", ex.Message);
            }
        }

        /// <summary>
        /// Periodic sync - called every N seconds
        /// </summary>
        private void OnSyncTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_currentSession == null)
                    return;

                if (_config.Debug)
                    LoggerUtil.LogDebug("[TIMER] Periodic sync triggered", true);

                var factions = LoadFactionsFromSession(_currentSession);
                if (factions.Count > 0)
                {
                    Task.Run(async () => await _orchestrator.ExecuteFullSyncAsync(factions));
                }
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[TIMER] Sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles /tds commands from chat
        /// NOTE: This is a placeholder - needs real chat event integration
        /// </summary>
        public void HandleChatCommand(string command, long playerSteamID, string playerName)
        {
            try
            {
                var parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    HandleHelpCommand(playerSteamID);
                    return;
                }

                var subcommand = parts[1].ToLower();

                // Validate command and authorization
                var cmdModel = CommandAuthorizationUtil.ValidateCommand(subcommand, playerSteamID, _config);

                if (cmdModel == null)
                {
                    // Command doesn't exist OR user is not authorized
                    bool isAdmin = SecurityUtil.IsPlayerAdmin(playerSteamID, _config.AdminSteamIDs);
                    var allCmds = CommandAuthorizationUtil.GetAllCommands();
                    var fullCmd = allCmds.FirstOrDefault(c => c.Name.Equals(subcommand, StringComparison.OrdinalIgnoreCase));

                    if (fullCmd != null && fullCmd.RequiresAdmin && !isAdmin)
                    {
                        // Command exists but user is not authorized
                        LoggerUtil.LogWarning($"[SECURITY] Unauthorized command attempt by {playerName} ({playerSteamID}): /{subcommand}");
                        ChatUtils.SendError($"Access denied. Command '{subcommand}' requires admin privileges.");
                        return;
                    }
                    else
                    {
                        // Command doesn't exist
                        ChatUtils.SendError($"Unknown command: /tds {subcommand}. Type /tds help for available commands.");
                        return;
                    }
                }

                // Execute authorized command
                switch (subcommand)
                {
                    case "verify":
                        HandleVerifyCommand(playerSteamID, playerName, parts);
                        break;

                    case "status":
                        Task.Run(async () => await HandleStatusCommand(playerName));
                        break;

                    case "sync":
                        Task.Run(async () => await HandleSyncCommand(playerName));
                        break;

                    case "reset":
                        Task.Run(async () => await HandleResetCommand(playerName));
                        break;

                    case "unverify":
                        HandleUnverifyCommand(playerSteamID, playerName, parts);
                        break;

                    case "help":
                        HandleHelpCommand(playerSteamID);
                        break;

                    default:
                        ChatUtils.SendError($"Unknown command: /tds {subcommand}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[COMMAND] Error: {ex.Message}");
                ChatUtils.SendError($"Command error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle /tds verify @DiscordName command
        /// Available to all users
        /// </summary>
        private void HandleVerifyCommand(long playerSteamID, string playerName, string[] args)
        {
            try
            {
                if (args.Length < 3)
                {
                    ChatUtils.SendError("Usage: /tds verify @DiscordName");
                    return;
                }

                string discordUsername = args[2];

                Task.Run(async () =>
                {
                    var result = await _verificationCommandHandler.HandleVerifyCommandAsync(
                        playerSteamID, playerName, discordUsername);
                    ChatUtils.SendServerMessage(result);
                });

                LoggerUtil.LogInfo($"[COMMAND] {playerName}: verify requested for {discordUsername}");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[VERIFY_CMD] Error: {ex.Message}");
                ChatUtils.SendError($"Verification error: {ex.Message}");
            }
        }

        /// Handle /tds sync command
        /// Available only to admins
        /// </summary>
        private async Task HandleSyncCommand(string playerName)
        {
            try
            {
                LoggerUtil.LogInfo($"[COMMAND] {playerName} executed: /tds sync");
                ChatUtils.SendSuccess("Starting faction synchronization...");
                await _eventLog.LogEventAsync("Command", $"Manual sync by {playerName}");

                if (_currentSession != null)
                {
                    var factions = LoadFactionsFromSession(_currentSession);
                    await _orchestrator.ExecuteFullSyncAsync(factions);
                    ChatUtils.SendSuccess("✅ Synchronization complete!");
                }
                else
                {
                    ChatUtils.SendError("No active session found");
                }
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[SYNC_CMD] Error: {ex.Message}");
                ChatUtils.SendError($"Sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle /tds reset command
        /// Available only to admins
        /// WARNING: This is destructive
        /// </summary>
        private async Task HandleResetCommand(string playerName)
        {
            try
            {
                LoggerUtil.LogWarning($"[COMMAND] {playerName} executed: /tds reset (DESTRUCTIVE)");
                ChatUtils.SendWarning("⚠️ Clearing Discord roles and channels...");

                await _factionSync.ResetDiscordAsync();
                await _eventLog.LogEventAsync("Command", $"Discord reset executed by {playerName}");

                ChatUtils.SendSuccess("✅ Discord reset complete! User roles updated.");
                LoggerUtil.LogSuccess($"[RESET] Completed by {playerName}");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[RESET_CMD] Error: {ex.Message}");
                ChatUtils.SendError($"Reset error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle /tds unverify STEAMID [reason] command
        /// Available only to admins
        /// </summary>
        private void HandleUnverifyCommand(long adminSteamID, string adminName, string[] args)
        {
            try
            {
                if (args.Length < 3)
                {
                    ChatUtils.SendError("Usage: /tds unverify STEAMID [reason]");
                    return;
                }

                if (!long.TryParse(args[2], out long targetSteamID))
                {
                    ChatUtils.SendError("Invalid Steam ID format");
                    return;
                }

                string reason = args.Length > 3 ? string.Join(" ", args.Skip(3)) : "Admin removal";

                Task.Run(async () =>
                {
                    var result = await _verificationCommandHandler.HandleUnverifyCommandAsync(targetSteamID, reason);
                    ChatUtils.SendServerMessage(result);
                    await _eventLog.LogEventAsync("UnverifyCommand",
                        $"{adminName} unverified SteamID {targetSteamID}: {reason}");
                });

                LoggerUtil.LogInfo($"[COMMAND] {adminName} executed: /tds unverify {targetSteamID} ({reason})");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[UNVERIFY_CMD] Error: {ex.Message}");
                ChatUtils.SendError($"Unverify error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle /tds status command
        /// Available to all users
        /// </summary>
        private async Task HandleStatusCommand(string playerName)
        {
            try
            {
                var factions = _db.GetAllFactions();
                var totalPlayers = factions.Sum(f => f.Players.Count);
                var verifications = _db.GetAllVerifications();
                var verifiedCount = verifications.Count(v => v.IsVerified);

                var statusLines = new List<string>();
                statusLines.Add("");
                statusLines.Add("┌─ Plugin Status ─────────────────────────────┐");
                statusLines.Add($"│ Factions: {factions.Count,-40}│");
                statusLines.Add($"│ Players: {totalPlayers,-41}│");
                statusLines.Add($"│ Verified Accounts: {verifiedCount,-32}│");
                statusLines.Add($"│ Debug Mode: {(_config.Debug ? "ON" : "OFF"),-37}│");
                statusLines.Add("└─────────────────────────────────────────────┘");
                statusLines.Add("");

                foreach (var line in statusLines)
                {
                    ChatUtils.SendServerMessage(line);
                }

                await _eventLog.LogEventAsync("StatusCommand", $"Status requested by {playerName}");
                LoggerUtil.LogInfo($"[COMMAND] {playerName} executed: /tds status");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[STATUS_CMD] Error: {ex.Message}");
                ChatUtils.SendError($"Status error: {ex.Message}");
            }
        }

        /// <summary>
        /// Display help text based on user authorization level
        /// Admins see all commands, users see only public commands
        /// </summary>
        private void HandleHelpCommand(long playerSteamID)
        {
            try
            {
                string helpText = CommandAuthorizationUtil.GenerateHelpText(playerSteamID, _config);

                // Split into lines and send each
                var lines = helpText.Split('\n');
                foreach (var line in lines)
                {
                    ChatUtils.SendServerMessage(line);
                }

                bool isAdmin = SecurityUtil.IsPlayerAdmin(playerSteamID, _config.AdminSteamIDs);
                LoggerUtil.LogInfo($"[COMMAND] {(isAdmin ? "ADMIN" : "USER")} help displayed");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"[HELP] Error: {ex.Message}");
                ChatUtils.SendError("Error displaying help");
            }
        }

        /// <summary>
        /// Loads factions from Space Engineers save
        /// TODO: Integrate with real Space Engineers API
        /// </summary>
        private List<FactionModel> LoadFactionsFromSession(ITorchSession session)
        {
            var factions = new List<FactionModel>();

            try
            {
                if (session == null)
                    return factions;

                // TODO: Implement real loading from Sandbox.sbc
                // For now returns empty list - should:
                // 1. Get Factions from session
                // 2. Filter only players (tag.Length == 3)
                // 3. For each faction get members (PlayerId → SteamId)
                // 4. Map to FactionModel

                if (_config.Debug)
                    LoggerUtil.LogDebug("LoadFactionsFromSession: Loading factions from session", true);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Error loading factions: {ex.Message}");
            }

            return factions;
        }

        /// <summary>
        /// Called every frame (if needed)
        /// </summary>
        public override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// Called when plugin is unloaded
        /// </summary>
        public override void Dispose()
        {
            try
            {
                PrintBanner("SHUTTING DOWN");

                // Stop timer
                if (_syncTimer != null)
                {
                    _syncTimer.Stop();
                    _syncTimer.Dispose();
                    LoggerUtil.LogInfo("Sync timer stopped");
                }

                // Save data
                if (_db != null)
                {
                    _db.SaveToXml();
                    LoggerUtil.LogSuccess("Data saved to XML");
                }

                LoggerUtil.LogSuccess("Plugin shutdown complete");
                PrintBanner("GOODBYE");
            }
            catch (Exception ex)
            {
                LoggerUtil.LogError($"Shutdown error: {ex.Message}");
            }

            base.Dispose();
        }

        /// <summary>
        /// Helper - prints banner to console
        /// </summary>
        private void PrintBanner(string title)
        {
            Console.WriteLine("");
            Console.WriteLine("╔════════════════════════════════════════════════════╗");
            Console.WriteLine($"║ mamba.TorchDiscordSync v2.0.0 - {title,-28}║");
            Console.WriteLine("╚════════════════════════════════════════════════════╝");
            Console.WriteLine("");
        }
    }
}