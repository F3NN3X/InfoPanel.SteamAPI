# InfoPanel Plugin Development Instructions

## Project Overview

This is an **InfoPanel plugin** that extends InfoPanel's monitoring capabilities with custom data sources. InfoPanel uses a service-based plugin architecture where plugins inherit from `BasePlugin` and provide sensors/text data through containers.

**Current Version:** 1.2.1 (Domain-Driven Architecture)

## Architecture Patterns

### Core Structure
- **Main Plugin Class**: `InfoPanel.SteamAPI.cs` - Entry point inheriting `BasePlugin`
- **Domain Services**: `/Services/Monitoring/` & `/Services/Sensors/` - Domain-driven logic
- **Data Services**: `/Services/` - Data collection and API interaction
- **Data Models**: `/Models/` - Data transfer objects
- **Configuration**: INI-based config following InfoPanel standards

### Domain-Driven Architecture
The plugin uses a domain-driven architecture with 3 distinct domains:
1. **Player Domain**: Profile, status, current game (1s update)
2. **Social Domain**: Friends, activity, groups (15s update)
3. **Library Domain**: Games owned, playtime, recent games (45s update)

### Initialization Pattern
```csharp
// 1. Initialize Core Services
_configService = new ConfigurationService(_configFilePath);
var steamApiService = new SteamApiService(...);
_sessionTrackingService = new SessionTrackingService(...);

// 2. Initialize Data Services
_playerDataService = new PlayerDataService(...);
_socialDataService = new SocialDataService(...);
_libraryDataService = new LibraryDataService(...);

// 3. Initialize Monitoring Services (Domain Logic)
_playerMonitoring = new PlayerMonitoringService(..., _playerDataService, ...);
_socialMonitoring = new SocialMonitoringService(..., _socialDataService, ...);
_libraryMonitoring = new LibraryMonitoringService(..., _libraryDataService, ...);

// 4. Initialize Sensor Services (UI/Output)
_playerSensors = new PlayerSensorService(..., _playerNameSensor, ...);
_socialSensors = new SocialSensorService(..., _friendsOnlineSensor, ...);
_librarySensors = new LibrarySensorService(..., _totalGamesSensor, ...);

// 5. Subscribe & Link
_playerSensors.SubscribeToMonitoring(_playerMonitoring);
_socialSensors.SubscribeToMonitoring(_socialMonitoring);
_librarySensors.SubscribeToMonitoring(_libraryMonitoring);

// 6. Share Session State
var sessionCache = _playerMonitoring.GetSessionCache();
_socialMonitoring.SetSessionCache(sessionCache);
_libraryMonitoring.SetSessionCache(sessionCache);
```

## Critical Implementation Rules

### Table Implementation (CRITICAL)
InfoPanel tables **MUST** use `PluginText` objects for all columns. Never use `string` or other types.

```csharp
// Correct Table Implementation
dataTable.Columns.Add("Friend", typeof(PluginText)); // MUST be PluginText
dataTable.Columns.Add("Status", typeof(PluginText));

var row = dataTable.NewRow();
// Create unique ID for each cell: "column_id_value"
row["Friend"] = new PluginText($"friend_{friendName}", friendName);
row["Status"] = new PluginText($"status_{friendName}", status);
dataTable.Rows.Add(row);
```

### Sensor Management Pattern
Sensors are defined as private fields in the main class and passed to Sensor Services:

```csharp
// Define sensors as fields in Main Class
private readonly PluginSensor _mainMetricSensor = new("main-metric", "Main Metric", 0, "units");

// Pass to Sensor Service in Initialize()
_playerSensors = new PlayerSensorService(..., _mainMetricSensor, ...);
```

## Build System & Project Structure

### MSBuild Configuration
Key `.csproj` patterns:
- **Target**: `net8.0-windows` with `UseWindowsForms=true`
- **Versioned Output**: `bin\$(Configuration)\$(TargetFramework)\{PluginName}-v$(Version)\{PluginName}\`
- **Plugin Settings**: `GenerateRuntimeConfigurationFiles=false`, `CopyLocalLockFileAssemblies=true`

### Build Commands
```powershell
# Standard build process
dotnet build -c Release

# Output location pattern
bin\Release\net8.0-windows\InfoPanel.SteamAPI-v1.2.1\InfoPanel.SteamAPI\
```

## Threading & Async Patterns

### Cancellation Token Usage
```csharp
// Standard pattern for async monitoring
_cancellationTokenSource = new CancellationTokenSource();
_ = StartMonitoringAsync(_cancellationTokenSource.Token);

// In StartMonitoringAsync
_playerMonitoring?.StartMonitoring();
_socialMonitoring?.StartMonitoring();
_libraryMonitoring?.StartMonitoring();

// Proper cleanup in Dispose()
_cancellationTokenSource?.Cancel();
_playerMonitoring?.StopMonitoring();
```

### Thread-Safe Sensor Updates
Sensor updates are handled within the `SensorService` classes, which should use proper locking or UI thread marshalling if required (InfoPanel handles basic thread safety for sensor values).

## Service Implementation Patterns

### Monitoring Services
- Inherit from `IMonitoringService` (or similar pattern)
- Manage their own `System.Threading.Timer`
- Expose `DataUpdated` events
- Use `SessionDataCache` for shared state

### Sensor Services
- Subscribe to Monitoring Service events
- Handle `DataUpdated` events to update sensor values
- Format data for display (e.g., `FormatMinutesToHourMin`)
- Handle error states (set sensors to "Error" or "-")

## Required Files & Metadata

### PluginInfo.ini
```ini
[PluginInfo]
Name=InfoPanel SteamAPI
Description=Get data from SteamAPI
Author=F3NN3X
Version=1.2.1
Website=https://github.com/F3NN3X/InfoPanel.SteamAPI
```

### Assembly Metadata
Version info must be consistent across:
- `.csproj` `<Version>` property
- `PluginInfo.ini` Version field
- Assembly attributes in `.csproj`

## Development Workflow

1. **Modify Data**: Update `*DataService.cs` to fetch new data from Steam API.
2. **Update Model**: Add properties to `PlayerData`, `SocialData`, or `LibraryData` in `/Models/`.
3. **Update Monitoring**: Ensure `*MonitoringService.cs` captures the new data.
4. **Update Sensors**: 
   - Add new `PluginSensor` field in `InfoPanel.SteamAPI.cs`.
   - Pass it to the appropriate `*SensorService` constructor.
   - Update `*SensorService.cs` to assign values in `Update*Sensors()` methods.
5. **Test Build**: Use `dotnet build -c Release` to verify output structure.

## File Deployment Guidelines

**CRITICAL**: Do not use tools like `copy-item` or similar commands to automatically deploy plugin files to InfoPanel directories. The user will manually copy files when ready for testing.

- **Build Only**: Use `dotnet build -c Release` to compile and verify
- **No Auto-Deploy**: Do not copy built files to `c:\ProgramData\InfoPanel\plugins\` automatically  
- **User Managed**: Let user decide when and how to deploy updated plugin files
- **Manual Testing**: User will restart InfoPanel manually after deploying changes

## Common Patterns to Follow

- **Error Handling**: Always set sensor error states on exceptions
- **Resource Disposal**: Implement proper cleanup in `Dispose()` method for ALL services
- **Event Unsubscription**: Unsubscribe from events before disposal
- **Console Logging**: Use `_loggingService` or `_enhancedLoggingService` instead of `Console.WriteLine`
- **Configuration Validation**: Auto-create missing config with sensible defaults

## InfoPanel Integration Points

- **Config File Access**: Expose via `ConfigFilePath` property for "Open Config" button
- **Container Registration**: Use `AddContainer()` to register sensor groups
- **Lifecycle Management**: Handle `Initialize()` and `Dispose()` properly
- **Update Intervals**: Managed by individual Monitoring Services (1s, 15s, 45s)