# mamba.TorchDiscordSync v2.0

**Space Engineers Torch Server Plugin** - Faction sync + death logging + chat bridge + server monitoring

**Author**: mamba  
**Version**: 2.0.0  
**Torch**: 1.3.1+  
**Space Engineers**: 1.208+  
**C#**: 4.6+ / .NET Framework 4.8

## Features

✨ **Faction Management**
- Auto-sync player factions (3-char tags) to Discord
- Create roles & channels per faction
- Update player nicknames: `[TAG] OriginalNick`

💀 **Death & Kill Logging**
- Suicide detection with random messages
- First kill tracking
- Retaliation detection (within 1 hour)
- Old revenge detection (within 24 hours)
- Public game chat announcements
- Discord event channel logging

💬 **Chat Synchronization**
- Game chat → Discord faction channels
- Discord messages → In-game chat
- Player name preservation

📊 **Server Monitoring**
- SimSpeed tracking
- Server up/down notifications
- Staff-only alerts on startup

🔒 **Security**
- SteamID whitelist for admin commands
- `/tds sync` - Force faction sync
- `/tds reset` - Clear all Discord objects
- `/tds status` - Show plugin status

## Installation

1. Download latest release from GitHub
2. Extract `.zip` into `Torch/Plugins/` folder
3. Start server (auto-creates config)
4. Edit `Instance/mambaTorchDiscordSync/MambaTorchDiscordSync.cfg`
5. Restart server

## Configuration

### MambaTorchDiscordSync.cfg

```xml
<DiscordToken>YOUR_BOT_TOKEN</DiscordToken>
<GuildID>000000000000</GuildID>
<CategoryID>000000000000</CategoryID>
<StaffChannelLog>000000000000</StaffChannelLog>
<AdminSteamIDs>
  <SteamID>76561198000000001</SteamID>
  <SteamID>76561198000000002</SteamID>
</AdminSteamIDs>
```

### DeathMessages.xml

Customize death announcements with templates:

```xml
<FirstKill>
  <Message>{0} obliterated {1} with {2}.</Message>
</FirstKill>
```

Variables:
- `{0}` = Killer name
- `{1}` = Victim name
- `{2}` = Weapon/cause
- `{3}` = Location

## Data Storage

All data stored in `Instance/mambaTorchDiscordSync/`:
- `MambaTorchDiscordSyncData.xml` - Factions, players, events
- `DeathMessages.xml` - Message templates
- `MambaTorchDiscordSync.cfg` - Settings

**No database files** - All XML for easy backup/restore.

## Commands

**In-game chat** (admin with SteamID approved only):

```
/tds sync              # Force full synchronization
/tds reset             # Clear all Discord roles/channels
/tds status            # Show current status
```

## Troubleshooting

**Plugin doesn't load?**
- Check manifest.xml GUID
- Verify Discord.Net NuGet packages installed
- Check Torch logs for exceptions

**Discord roles not created?**
- Verify bot has role creation permissions
- Check GuildID in config
- Ensure bot token is valid

**Death messages not showing?**
- Check DeathMessages.xml syntax
- Verify EventChannelDeathJoinLeave ID
- Enable Debug mode in config

## License

MIT - See LICENSE file

## Contributing

Pull requests welcome! Please follow C# 4.6 compatibility.

## Support

will be added later...
```

---

## Notes on C# 4.6 Compatibility

✅ Safe to use:
- `List<T>`, `Dictionary<K,V>`
- `async/await` (C# 5.0+)
- LINQ
- `DateTime.UtcNow`
- XML Serialization
- `lock` statements
- String interpolation (C# 6.0 - check if supported, else use `string.Format()`)

❌ Avoid:
- `using` declarations (C# 8.0)
- Records (C# 9.0)
- Pattern matching advanced (C# 7.0+)
- Nullable reference types (C# 8.0)
- Default interface implementations

---

## Summary of Changes

| Feature | Status | Notes |
|---------|--------|-------|
| XML Database | ✅ | Replaces SQLite, auto-creates |
| Death Logging | ✅ | Retaliation detection included |
| Chat Sync | ✅ | Bidirectional game ↔ Discord |
| Server Monitor | ✅ | SimSpeed + up/down alerts |
| Security | ✅ | SteamID whitelist + commands |
| Auto Config | ✅ | Creates directories on startup |
| Discord Integration | ✅ | Full Discord.Net library |

---