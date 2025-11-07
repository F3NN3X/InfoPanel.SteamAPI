# InfoPanel Steam API Plugin Configuration

## Quick Setup

1. **Get Your Steam Web API Key**
   - Visit: https://steamcommunity.com/dev/apikey
   - You'll need to provide a domain name (you can use `localhost` for personal use)
   - Copy the generated API key

2. **Find Your Steam ID**
   - Visit: https://steamid.io/
   - Enter your Steam profile URL or username
   - Copy the **steamID64** value

3. **Configure the Plugin**
   - Edit the `InfoPanel.SteamAPI.ini` file (created automatically when plugin starts)
   - Replace `<your-steam-api-key-here>` with your actual API key
   - Replace `<your-steam-id-here>` with your actual Steam ID

## Example Configuration

```ini
[Steam Settings]
ApiKey=ABCDEF1234567890ABCDEF1234567890ABCDEF12
SteamId=76561198012345678
UpdateIntervalSeconds=30
EnableProfileMonitoring=true
EnableLibraryMonitoring=true
EnableCurrentGameMonitoring=true
```

## Configuration Settings Explained

### Steam Settings

| Setting | Description | Default | Notes |
|---------|-------------|---------|-------|
| `ApiKey` | Steam Web API key | Required | Get from steamcommunity.com/dev/apikey |
| `SteamId` | Your Steam ID (64-bit) | Required | Find at steamid.io |
| `UpdateIntervalSeconds` | Data refresh rate | 30 | Minimum 10 seconds (API rate limits) |
| `EnableProfileMonitoring` | Monitor profile info | true | Name, level, online status |
| `EnableLibraryMonitoring` | Monitor game library | true | Total games, playtime stats |
| `EnableCurrentGameMonitoring` | Monitor current game | true | Currently playing game |
| `EnableAchievementMonitoring` | Monitor achievements | false | Not implemented in Phase 1 |
| `MaxRecentGames` | Recent games to track | 5 | For recent activity monitoring |

### Debug Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `EnableDebugLogging` | Enable debug output | false |
| `DebugLogLevel` | Log verbosity | Info |

### Monitoring Settings

| Setting | Description | Default | Notes |
|---------|-------------|---------|-------|
| `MonitoringIntervalMs` | Internal update rate | 1000 | Minimum 1000ms |
| `EnableAutoReconnect` | Auto-reconnect on failure | true | |
| `ConnectionTimeoutMs` | API timeout | 5000 | |

## Sensors Provided

The plugin creates the following sensors in InfoPanel:

### Profile Sensors
- **Player Name**: Your Steam display name
- **Status**: Online status (Online, Offline, Away, etc.)
- **Steam Level**: Your current Steam level

### Game Sensors
- **Current Game**: Currently playing game name
- **Game Playtime**: Playtime for current game (hours)

### Library Sensors
- **Games Owned**: Total number of games in library
- **Total Playtime**: Total playtime across all games (hours)
- **Recent Playtime**: Playtime in last 2 weeks (hours)

### Status Sensors
- **Plugin Status**: Overall plugin status
- **Details**: Detailed information and last update time

## Troubleshooting

### Common Issues

1. **"Steam API Key is required but not set"**
   - Ensure you've replaced `<your-steam-api-key-here>` with your actual API key
   - Verify the API key is correct (32 character hex string)

2. **"Steam ID is required but not set"**
   - Ensure you've replaced `<your-steam-id-here>` with your actual Steam ID
   - Use the 64-bit Steam ID (17 digit number starting with 7656119...)

3. **"Failed to connect to Steam API"**
   - Check your internet connection
   - Verify your Steam profile is public (required for API access)
   - Ensure you're not exceeding API rate limits

4. **No data showing**
   - Wait for the update interval to pass (default 30 seconds)
   - Check the plugin status sensor for error messages
   - Enable debug logging for more detailed information

### Making Your Profile Public

For the Steam API to work, your Steam profile must be public:

1. Open Steam and go to your profile
2. Click "Edit Profile"
3. Go to "Privacy Settings"
4. Set "My profile" to "Public"
5. Set "Game details" to "Public"

## Rate Limits

The Steam Web API has rate limits:
- Maximum ~1 request per second
- The plugin automatically handles rate limiting
- Minimum update interval is 10 seconds to ensure compliance

## Privacy Note

This plugin only reads publicly available Steam data using the official Steam Web API. No private information is accessed or stored.