# InfoPanel.SteamAPI Development Guide

This document contains technical details, architecture overview, and build instructions for the InfoPanel.SteamAPI plugin.

## 🏗️ Architecture

### Service-Oriented Design

The plugin follows a clean service-based architecture:

- **PlayerDataService**: Real-time player status and game state (1s updates)
- **SocialDataService**: Friends and community features (15s updates)
- **LibraryDataService**: Game library and playtime statistics (45s updates)
- **GameStatsService**: Detailed achievements and game analytics (45s updates)
- **NewsDataService**: Game news and updates (60s updates)
- **SessionTrackingService**: Reliable session tracking with immediate game state updates

### Project Structure

```
InfoPanel.SteamAPI.cs    # Main plugin class
├── Services/
│   ├── Monitoring/      # Domain-specific monitoring services
│   │   ├── PlayerMonitoringService.cs
│   │   ├── SocialMonitoringService.cs
│   │   └── ...
│   ├── Sensors/         # Domain-specific sensor services
│   │   ├── PlayerSensorService.cs
│   │   ├── SocialSensorService.cs
│   │   └── ...
│   ├── ConfigurationService.cs      # INI configuration management
│   └── EnhancedLoggingService.cs    # Structured logging
├── Models/
│   └── SteamData.cs                 # Steam data structure
└── PluginInfo.ini                   # Plugin metadata
```

### Key Components

- **Main Plugin Class**: Manages 4 containers with 48 sensors and 3 tables total.
- **Monitoring Services**: Handle Steam Web API calls and data collection for specific domains.
- **Sensor Services**: Manage thread-safe updates for InfoPanel sensors.
- **Configuration Service**: Manages Steam API key, Steam ID, and settings.
- **Enhanced Logging Service**: Provides structured, rotated, and performance-aware logging.

## 🛠️ Building from Source

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or JetBrains Rider (optional)
- InfoPanel installed with `InfoPanel.Plugins.dll` available

### Build Commands

**Debug Build:**
```powershell
dotnet build -c Debug
```

**Release Build:**
```powershell
dotnet build -c Release
```

**Clean Build:**
```powershell
dotnet clean
dotnet build -c Release
```

The build output will be located in:
`bin\Release\net8.0-windows\InfoPanel.SteamAPI-v{Version}\InfoPanel.SteamAPI\`

## 🔌 Steam Web API Integration

The plugin utilizes the following Steam Web API endpoints:

- **ISteamUser/GetPlayerSummaries**: Basic profile data and online status
- **IPlayerService/GetOwnedGames**: Game library statistics and playtime data
- **IPlayerService/GetRecentlyPlayedGames**: Recent gaming activity tracking
- **ISteamUserStats/GetPlayerAchievements**: Achievement data
- **ISteamUser/GetFriendList**: Friends monitoring
- **ISteamUserStats/GetUserStatsForGame**: Detailed game statistics
- **IStorefrontService/GetNewsForApp**: Game news monitoring
- **ISteamUser/GetPlayerBadges**: Community badge tracking
- **ISteamCommunity**: Global statistics

## 📝 Adding New Features

1. **New Sensors**: Define in `InfoPanel.SteamAPI.cs`, add to appropriate container, pass to Sensor Service.
2. **New Configuration**: Add settings to `ConfigurationService.cs`.
3. **New Data Properties**: Extend `SteamData` model.
4. **New API Calls**: Add endpoint calls to the appropriate `*DataService.cs`.

## 📚 Documentation

- **[InfoPanel Plugin Development Guide](InfoPanel_PluginDocumentation.md)**: Complete guide to InfoPanel plugin development.
