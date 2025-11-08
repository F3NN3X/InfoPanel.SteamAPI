# Steam API Debug Logging Enhancement Summary

## Overview
Enhanced the InfoPanel.SteamAPI plugin with comprehensive debug logging to help troubleshoot runtime issues with the Steam Web API integration. This logging provides detailed visibility into API calls, responses, data processing, and error conditions.

## Enhanced Logging Features

### 1. Core API Request/Response Logging
**Location**: `SteamApiService.cs` - `CallSteamApiAsync` method

**Features Added**:
- **Request Timing**: Tracks individual request time and total call time
- **Attempt Tracking**: Shows which retry attempt is being made (1/3, 2/3, etc.)
- **Response Size Logging**: Reports response length in characters
- **Smart Response Logging**:
  - Full response logged if under 5,000 characters
  - First 500 characters + truncation indicator for large responses
- **Enhanced Error Logging**: Captures actual Steam API error messages for 403, 401, and other HTTP errors

**Example Log Output**:
```
Making Steam API call (attempt 1/3): https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=[REDACTED]&steamids=76561198123456789&format=json
Steam API call successful - Response: 487 chars, Request time: 234ms, Total time: 1156ms
Steam API Response: {"response":{"players":[{"steamid":"76561198123456789","communityvisibilitystate":3...}]}}
```

### 2. Individual API Method Logging
**Locations**: `GetPlayerSummaryAsync`, `GetOwnedGamesAsync`, `GetRecentlyPlayedGamesAsync`

**Features Added**:
- **Method Entry Logging**: Logs when each API method starts
- **Data Validation Logging**: Reports player count, game counts, and key data points
- **Content Analysis**: Extracts and logs key information from responses
- **Enhanced Error Handling**: Separate handling for JSON deserialization vs. API errors

**Example Log Output**:
```
Requesting player summary from Steam API
Deserializing player summary response
Retrieved player summary - Players found: 1, Primary player: YourSteamName

Requesting owned games from Steam API
Retrieved owned games - Reported count: 127, Actual games in response: 127
Most played game in last 2 weeks: Counter-Strike 2 (340 minutes)

Requesting recently played games from Steam API
Retrieved recently played games - Reported count: 3, Actual games in response: 3
Top recently played game: Counter-Strike 2 (340 minutes in last 2 weeks)
Total recent playtime: 456 minutes (7.6 hours)
```

### 3. Data Collection Process Logging
**Location**: `MonitoringService.cs` - `CollectSteamDataAsync` method

**Features Added**:
- **Collection Phase Tracking**: Logs each data collection phase (player, library, recent games)
- **Configuration Awareness**: Shows which monitoring features are enabled/disabled
- **Data Aggregation Logging**: Reports calculated totals, averages, and derived metrics
- **Error Context**: Detailed error information with context about which phase failed

**Example Log Output**:
```
Starting Steam data collection...
Player Summary - Name: YourSteamName, State: Online, Current Game: Counter-Strike 2
Steam Level: 25
Library Data - Games Owned: 127, Total Playtime: 1,234.5 hours
Most Played Game: Team Fortress 2 (456.2 hours)
Recent Activity - Games: 3, Playtime (2w): 7.6 hours
Steam data collection completed successfully - Status: Online, Player: YourSteamName
```

### 4. Rate Limiting and Performance Logging
**Features**:
- **Rate Limit Tracking**: Shows when delays are applied between API calls
- **Performance Monitoring**: Reports individual request times and total operation times
- **Retry Logic Visibility**: Clear indication of retry attempts and reasons

## How to Use the Debug Logging

### 1. Enable Debug Logging
The logging uses the existing `FileLoggingService`. Make sure your configuration enables appropriate log levels:

```ini
[Logging]
LogLevel=Debug  # Set to Debug to see detailed API information
LogLevel=Info   # Set to Info for important events only
```

### 2. Monitor Log Output
- **File Location**: Check your configured log file path (typically in the plugin directory)
- **Console Output**: Debug messages also appear in VS Code debug console when debugging
- **Real-time Monitoring**: Use `tail -f` or similar tools to monitor logs in real-time

### 3. Key Information to Look For

#### API Connection Issues:
```
Steam API returned 403 Forbidden - check API key validity
Steam API returned 401 Unauthorized - invalid API key
```

#### Rate Limiting:
```
Rate limited by Steam API, waiting 2.0 seconds before retry 2/3
Rate limiting: waiting 1100ms before API call
```

#### Data Processing Issues:
```
Player summary returned null
Owned games returned null or empty
Could not find current game 'GameName' in owned games list
```

#### Performance Issues:
```
Steam API call successful - Response: 15247 chars, Request time: 3456ms, Total time: 4567ms
```

## Troubleshooting Common Issues

### 1. API Key Issues
**Symptoms**: 403/401 errors, null responses
**Debug Output to Check**:
- `Steam API returned 403 Forbidden - check API key validity`
- `Steam API connection test failed - no player data returned`

### 2. Rate Limiting
**Symptoms**: Slow responses, timeout errors
**Debug Output to Check**:
- `Rate limited by Steam API, waiting X seconds before retry`
- High request times in success messages

### 3. Data Parsing Issues
**Symptoms**: Missing sensor data, incorrect values
**Debug Output to Check**:
- `Failed to deserialize [endpoint] JSON response`
- `Players found: 0` or `Actual games in response: 0`
- Missing game names or incorrect counts

### 4. Configuration Issues
**Symptoms**: Missing data types
**Debug Output to Check**:
- `[Monitoring type] monitoring is disabled`
- `SteamApiService is null - cannot collect data`

## Next Steps for Issue Resolution

1. **Review Log Output**: Check the enhanced logs for specific error messages and timing information
2. **Verify API Key**: Ensure the Steam Web API key is valid and has proper permissions
3. **Check Network Connectivity**: Look for timeout or connection errors in the logs
4. **Monitor Performance**: Use timing information to identify slow API calls
5. **Validate Data Structure**: Check that API responses match expected JSON structure

This enhanced logging should provide comprehensive visibility into the Steam API integration and help identify the specific issues causing runtime problems.