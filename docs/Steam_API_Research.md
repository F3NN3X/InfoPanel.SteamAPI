# Steam Web API Research for InfoPanel Plugin

## Overview

This document outlines the research findings for integrating the Steam Web API into an InfoPanel plugin. The Steam Web API provides extensive access to Steam platform data, enabling rich monitoring capabilities for gaming activity, social interactions, and game statistics.

## Steam Web API Fundamentals

### Authentication & Access
- **API Key Required**: Free registration at https://steamcommunity.com/dev/apikey
- **Steam ID Required**: 64-bit Steam ID to identify users
- **Base URL**: `https://api.steampowered.com/`
- **Format**: JSON responses (XML also available)
- **Rate Limiting**: Approximately 1 request per second recommended

### Key Concepts
- **Steam ID**: Unique 64-bit identifier for Steam accounts
- **App ID**: Unique identifier for Steam games/applications
- **API Versions**: Most endpoints use version v1 or v2
- **Privacy**: Some data requires public Steam profiles

## Available API Endpoints & Data Sources

### 1. Player Service (`IPlayerService`)

#### GetOwnedGames
- **Endpoint**: `/IPlayerService/GetOwnedGames/v0001/`
- **Data Available**:
  - Game library with App IDs and names
  - Playtime forever (in minutes)
  - Playtime in last 2 weeks
  - Last played timestamp
  - Game icons and logos
- **Privacy**: Requires public game details or friendship

#### GetRecentlyPlayedGames
- **Endpoint**: `/IPlayerService/GetRecentlyPlayedGames/v0001/`
- **Data Available**:
  - Games played in last 2 weeks
  - Session playtime
  - Individual game playtime totals
  - Game metadata (name, icons)

#### GetSteamLevel
- **Endpoint**: `/IPlayerService/GetSteamLevel/v1/`
- **Data Available**:
  - Current Steam level
  - XP progression information

#### GetBadges
- **Endpoint**: `/IPlayerService/GetBadges/v1/`
- **Data Available**:
  - All earned badges
  - Badge levels and XP
  - Completion timestamps
  - Next badge progress

### 2. Steam User (`ISteamUser`)

#### GetPlayerSummaries
- **Endpoint**: `/ISteamUser/GetPlayerSummaries/v0002/`
- **Data Available**:
  - Profile information (username, real name)
  - Avatar URLs (small, medium, full)
  - Profile URL and Steam ID
  - Account creation date
  - Last logoff time
  - Profile visibility state
  - Current game information (if playing)
  - Primary clan ID

#### GetFriendList
- **Endpoint**: `/ISteamUser/GetFriendList/v0001/`
- **Data Available**:
  - Friends list with Steam IDs
  - Friendship timestamps
  - Friend relationship types
- **Privacy**: Requires public friends list

### 3. Steam User Stats (`ISteamUserStats`)

#### GetPlayerAchievements
- **Endpoint**: `/ISteamUserStats/GetPlayerAchievements/v0001/`
- **Parameters**: Requires specific App ID
- **Data Available**:
  - Achievement unlock status
  - Unlock timestamps
  - Achievement metadata (names, descriptions)

#### GetUserStatsForGame
- **Endpoint**: `/ISteamUserStats/GetUserStatsForGame/v0002/`
- **Parameters**: Requires specific App ID
- **Data Available**:
  - Game-specific statistics
  - Numerical stats (kills, deaths, scores, etc.)
  - Achievement progress

#### GetGlobalStatsForGame
- **Endpoint**: `/ISteamUserStats/GetGlobalStatsForGame/v0001/`
- **Data Available**:
  - Global game statistics
  - Community-wide metrics
  - Popular statistics across all players

### 4. Steam Apps (`ISteamApps`)

#### GetAppList
- **Endpoint**: `/ISteamApps/GetAppList/v0002/`
- **Data Available**:
  - Complete list of Steam applications
  - App IDs and names
  - Useful for App ID lookups

### 5. Steam News (`ISteamNews`)

#### GetNewsForApp
- **Endpoint**: `/ISteamNews/GetNewsForApp/v0002/`
- **Parameters**: Requires specific App ID
- **Data Available**:
  - Game-specific news articles
  - Update announcements
  - Community posts
  - Timestamps and URLs

### 6. Steam Store API (Unofficial)

#### Store Page Data
- **Endpoint**: `https://store.steampowered.com/api/appdetails`
- **Data Available**:
  - Game pricing information
  - Review scores and counts
  - System requirements
  - Release dates
  - Developer/publisher info
  - Current player counts (for some games)

## InfoPanel Plugin Implementation Possibilities

### Core Monitoring Metrics

#### Primary Sensors
```csharp
// Gaming Activity
private readonly PluginSensor _totalGamesSensor = new("total-games", "Games Owned", 0, "games");
private readonly PluginSensor _totalPlaytimeSensor = new("total-playtime", "Total Playtime", 0, "hours");
private readonly PluginSensor _twoWeekPlaytimeSensor = new("recent-playtime", "Recent Playtime", 0, "hours");
private readonly PluginText _currentGameSensor = new("current-game", "Currently Playing", "Not playing");
private readonly PluginText _lastPlayedSensor = new("last-played", "Last Played", "Unknown");

// Profile & Social
private readonly PluginSensor _steamLevelSensor = new("steam-level", "Steam Level", 0, "level");
private readonly PluginSensor _totalBadgesSensor = new("total-badges", "Total Badges", 0, "badges");
private readonly PluginSensor _onlineFriendsSensor = new("online-friends", "Friends Online", 0, "friends");
private readonly PluginText _profileStatusSensor = new("profile-status", "Profile Status", "Offline");
private readonly PluginText _profileNameSensor = new("profile-name", "Steam Name", "Unknown");

// Achievement Tracking (for specific games)
private readonly PluginSensor _achievementPercentageSensor = new("achievement-completion", "Achievement %", 0, "%");
private readonly PluginSensor _unlockedAchievementsSensor = new("unlocked-achievements", "Achievements", 0, "unlocked");
```

#### Advanced Metrics
```csharp
// Game-Specific Monitoring (configurable)
private readonly PluginSensor _currentGamePlaytimeSensor = new("current-game-playtime", "Session Time", 0, "minutes");
private readonly PluginSensor _currentGameTotalSensor = new("current-game-total", "Game Total", 0, "hours");

// Library Statistics
private readonly PluginSensor _unplayedGamesSensor = new("unplayed-games", "Unplayed Games", 0, "games");
private readonly PluginSensor _completedGamesSensor = new("completed-games", "100% Games", 0, "games");
private readonly PluginSensor _averageGameRatingSensor = new("avg-game-rating", "Avg Game Rating", 0, "★");

// Social & Community
private readonly PluginSensor _totalFriendsSensor = new("total-friends", "Total Friends", 0, "friends");
private readonly PluginText _mostPlayedGameSensor = new("most-played-game", "Most Played", "Unknown");
private readonly PluginText _recentAchievementSensor = new("recent-achievement", "Latest Achievement", "None");
```

### Configuration Requirements

#### Essential Configuration
```ini
[Steam API Settings]
SteamApiKey=<user-steam-api-key>
SteamId=<user-64bit-steam-id>
RefreshIntervalMinutes=5
EnableCaching=true
CacheExpiryMinutes=10

[Privacy Settings]
RequirePublicProfile=true
FallbackToFriendsOnly=false
SkipPrivateGames=true

[Display Settings]
ShowCurrentGame=true
ShowSessionTime=true
ShowRecentGames=true
MaxRecentGames=5
ShowFriendsActivity=true
ShowAchievements=true

[Game Specific Monitoring]
MonitorSpecificGame=false
SpecificGameAppId=0
SpecificGameName=""
ShowGameSpecificStats=false

[Advanced Features]
EnableNewsMonitoring=false
NewsRefreshHours=6
ShowGlobalStats=false
TrackCompletionRates=true
```

### Data Processing & Caching Strategy

#### Rate Limiting Considerations
- **Steam API Limit**: ~1 request/second
- **Caching Strategy**: Cache responses for 5-10 minutes
- **Priority Requests**: Current game status > profile info > detailed stats
- **Fallback Handling**: Graceful degradation when rate limited

#### Cache Implementation
```csharp
public class SteamApiCache
{
    private readonly Dictionary<string, CachedResponse> _cache = new();
    private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(10);
    
    public class CachedResponse
    {
        public object Data { get; set; }
        public DateTime CachedAt { get; set; }
        public TimeSpan Expiry { get; set; }
        
        public bool IsExpired => DateTime.Now > CachedAt.Add(Expiry);
    }
}
```

## API Response Examples

### Player Summary Response
```json
{
  "response": {
    "players": [
      {
        "steamid": "76561197960435530",
        "communityvisibilitystate": 3,
        "profilestate": 1,
        "personaname": "Robin",
        "commentpermission": 1,
        "profileurl": "https://steamcommunity.com/id/robinwalker/",
        "avatar": "https://avatars.akamai.steamstatic.com/avatar.jpg",
        "avatarmedium": "https://avatars.akamai.steamstatic.com/avatarmedium.jpg",
        "avatarfull": "https://avatars.akamai.steamstatic.com/avatarfull.jpg",
        "avatarhash": "9e7d923aca9487d2af33b2b74ff5e1ff",
        "lastlogoff": 1690906789,
        "personastate": 1,
        "realname": "Robin Walker",
        "primaryclanid": "103582791429521408",
        "timecreated": 1063407589,
        "personastateflags": 0,
        "gameextrainfo": "Team Fortress 2",
        "gameid": "440"
      }
    ]
  }
}
```

### Owned Games Response
```json
{
  "response": {
    "game_count": 123,
    "games": [
      {
        "appid": 440,
        "name": "Team Fortress 2",
        "playtime_forever": 8374,
        "img_icon_url": "e3f595a92552da3d664ad00277fad2107345f743",
        "img_logo_url": "07385eb55b5ba974aebbe74d3c99626bda7920b8",
        "playtime_windows_forever": 8374,
        "playtime_mac_forever": 0,
        "playtime_linux_forever": 0,
        "rtime_last_played": 1690906789,
        "playtime_2weeks": 42
      }
    ]
  }
}
```

## Implementation Phases

### Phase 1: Basic Profile Monitoring
- Steam profile information (name, level, status)
- Currently playing game
- Basic library statistics (total games, total playtime)
- Simple configuration management

### Phase 2: Enhanced Gaming Metrics
- Recent gaming activity (2-week stats)
- Session time tracking for current game
- Friends online monitoring
- Achievement tracking for current game

### Phase 3: Advanced Features
- Detailed game-specific statistics
- Multiple game monitoring
- Achievement completion tracking
- News and update monitoring

### Phase 4: Social & Community Features
- Friends activity monitoring
- Popular games in friend network
- Community badge tracking
- Global statistics comparison

## Technical Considerations

### Error Handling
- **API Key Validation**: Check key validity on startup
- **Privacy Restrictions**: Handle private profile gracefully
- **Rate Limiting**: Implement exponential backoff
- **Network Issues**: Robust retry logic with circuit breaker

### Performance Optimization
- **Async Operations**: All API calls should be asynchronous
- **Batch Requests**: Combine related API calls where possible
- **Memory Management**: Proper disposal of HTTP clients
- **CPU Usage**: Minimize JSON parsing overhead

### Security Considerations
- **API Key Storage**: Secure configuration file handling
- **Data Privacy**: Respect user privacy settings
- **HTTPS Only**: All API communication over HTTPS
- **Input Validation**: Validate Steam IDs and App IDs

## Potential Challenges & Limitations

### API Limitations
1. **Rate Limiting**: Strict request limits may impact real-time monitoring
2. **Privacy Dependencies**: Many features require public Steam profiles
3. **Data Availability**: Some statistics only available for owned games
4. **API Stability**: Unofficial endpoints may change without notice

### User Experience Challenges
1. **Setup Complexity**: Users need to obtain API keys and Steam IDs
2. **Configuration**: Multiple privacy and display settings to manage
3. **Data Freshness**: Caching requirements may show stale data
4. **Error States**: Need clear feedback when data is unavailable

### Technical Challenges
1. **Steam ID Resolution**: Converting vanity URLs to Steam IDs
2. **Game Name Resolution**: Mapping App IDs to readable names
3. **Timezone Handling**: Converting Unix timestamps to local time
4. **Large Datasets**: Efficiently processing large game libraries

## Success Metrics for InfoPanel Integration

### Primary Metrics
- **Profile Activity**: Online status, current game, session duration
- **Library Overview**: Total games, playtime, recent activity
- **Achievement Progress**: Current game achievements, completion rates
- **Social Activity**: Friends online, popular games

### Secondary Metrics  
- **Game-Specific Stats**: Detailed statistics for monitored games
- **Completion Tracking**: 100% completion rates, unplayed games
- **Community Engagement**: Badge progress, group activities
- **Market Activity**: Inventory values, trading statistics (if API available)

## Additional Research Findings

### Comprehensive Steam Web API Documentation

The **steamwebapi.azurewebsites.net** provides the most comprehensive documentation of Steam Web API endpoints, automatically updated every 24 hours. Key findings:

#### Enhanced API Coverage
- **600+ documented endpoints** across 100+ interfaces
- **Partner vs Public API distinction**: Only public API (`api.steampowered.com`) is accessible without publisher credentials
- **Automatic documentation updates** ensure current endpoint information
- **Parameter documentation** with types, descriptions, and requirements

#### Notable Additional Interfaces
```csharp
// Game-specific APIs
ICSGOServers_730            // CS:GO server status and map playtime
IDOTA2Match_570            // Dota 2 match details and statistics  
IEconItems_*               // Game item inventories (TF2, CS:GO, Dota 2)
IPortal2Leaderboards_620   // Portal 2 leaderboard data

// Store and Publishing
IPublishedFileService      // Steam Workshop content
IStoreService             // Store browsing and recommendations
IInventoryService         // Item management and trading
IWishlistService          // User wishlist data

// Community Features  
IAuthenticationService    // Session and login management
IBroadcastService         // Steam broadcasting data
IGameNotificationsService // Game session notifications
```

### Critical Implementation Insights from StackOverflow Research

#### Playtime Data Availability Evolution
**Historical Challenges (2013-2017)**:
- Playtime data was scattered across multiple stat fields
- Required summing individual class playtime (TF2: Scout.accum.iPlayTime + Soldier.accum.iPlayTime, etc.)
- Community XML endpoints were deprecated

**Current Solutions (2022-2024)**:
```csharp
// GetOwnedGames now includes comprehensive playtime data
{
  "playtime_forever": 7167,              // Total playtime in minutes
  "playtime_2weeks": 89,                 // Recent playtime
  "playtime_current_session": 10,        // Current session time
  "playtime_windows_forever": 7108,      // Platform-specific totals
  "playtime_mac_forever": 0,
  "playtime_linux_forever": 0,
  "rtime_last_played": 1677310875        // Unix timestamp - last played
}
```

#### Last Played Data Evolution
**2023 Update**: Steam API now officially supports `rtime_last_played` in `GetOwnedGames` response:
```javascript
// Previously required web scraping from Steam profile pages
// rgGames.filter(g => g.last_played > 0).sort((a,b) => b.last_played - a.last_played)[0]

// Now available via official API
GET /IPlayerService/GetOwnedGames/v1/?access_token={YOUR_TOKEN}&steamid={STEAMID}
```

#### Game Rating and Review Data
**Steam Store Reviews API** (undocumented but functional):
```javascript
// Get game reviews and ratings
GET https://store.steampowered.com/appreviews/{appid}?json=1&language=all

{
  "query_summary": {
    "review_score": 9,
    "review_score_desc": "Overwhelmingly Positive", 
    "total_positive": 25703,
    "total_negative": 898,
    "total_reviews": 26601
  }
}
```

### SteamWebAPI2 C# Library Implementation Patterns

#### Professional Library Architecture
The **SteamWebAPI2** library provides proven patterns for Steam API integration:

```csharp
// Factory pattern for interface creation
var webInterfaceFactory = new SteamWebInterfaceFactory(apiKey);
var steamUser = webInterfaceFactory.CreateSteamWebInterface<SteamUser>();

// Structured response handling
public interface ISteamWebResponse<T>
{
    T Data { get; set; }                    // Actual API data
    long? ContentLength { get; set; }       // HTTP metadata
    string ContentType { get; set; }
    DateTimeOffset? LastModified { get; set; }
}

// Clean async patterns
var playerSummary = await steamUser.GetPlayerSummaryAsync(steamId);
var friendsList = await steamUser.GetFriendsListAsync(steamId);
```

#### Advanced Features Implemented
- **SteamId Class**: Handles legacy, modern, and 64-bit Steam ID conversions
- **AutoMapper Integration**: Clean JSON-to-object mapping  
- **HTTP Client Management**: Proper lifecycle management with dependency injection
- **Error Handling**: Structured exception handling for API failures
- **Caching Support**: Built-in response caching capabilities

#### Endpoint Coverage Examples
```csharp
// Player data
ISteamUser.GetPlayerSummariesAsync()     // Profile information
ISteamUser.GetFriendsListAsync()         // Social connections
ISteamUser.GetPlayerBansAsync()          // Account status

// Game data  
IPlayerService.GetOwnedGamesAsync()      // Game library with playtime
IPlayerService.GetRecentlyPlayedGames()  // Recent activity
IPlayerService.GetSteamLevelAsync()      // Account level

// Store integration
SteamStore.GetStoreAppDetailsAsync()     // Game metadata and pricing
```

## Updated Implementation Strategy

### Phase 1: Enhanced Basic Implementation
Based on research findings, start with these proven endpoints:

```csharp
// Core monitoring sensors with full playtime support
private readonly PluginSensor _totalGamesSensor = new("total-games", "Games Owned", 0, "games");
private readonly PluginSensor _totalPlaytimeMinutesSensor = new("total-playtime", "Total Playtime", 0, "minutes");
private readonly PluginSensor _recentPlaytimeMinutesSensor = new("recent-playtime", "Recent Playtime", 0, "minutes");
private readonly PluginSensor _currentSessionMinutesSensor = new("session-time", "Session Time", 0, "minutes");

// Enhanced status tracking
private readonly PluginText _currentGameSensor = new("current-game", "Currently Playing", "Not playing");
private readonly PluginText _lastPlayedGameSensor = new("last-played-game", "Last Played", "Unknown");
private readonly PluginText _lastPlayedTimeSensor = new("last-played-time", "Last Played Time", "Never");

// Profile information
private readonly PluginSensor _steamLevelSensor = new("steam-level", "Steam Level", 0, "level");
private readonly PluginText _profileNameSensor = new("profile-name", "Steam Name", "Unknown");
private readonly PluginText _profileStatusSensor = new("profile-status", "Status", "Offline");
```

### Phase 2: Advanced Data Sources
Implement additional endpoints discovered through research:

```csharp
// Game-specific monitoring (configurable per user)
private readonly PluginSensor _gameAchievementsSensor = new("game-achievements", "Achievements", 0, "%");
private readonly PluginSensor _gameRatingsSensor = new("game-rating", "Game Rating", 0, "★");

// Social features
private readonly PluginSensor _onlineFriendsSensor = new("friends-online", "Friends Online", 0, "friends");
private readonly PluginSensor _totalFriendsSensor = new("friends-total", "Total Friends", 0, "friends");

// Workshop/Community
private readonly PluginText _workshopItemsSensor = new("workshop-items", "Workshop Items", "0");
```

### Recommended C# Implementation Approach

Based on SteamWebAPI2 patterns, implement using:
- **HttpClient with proper lifecycle management**
- **Async/await patterns throughout**
- **Structured JSON deserialization with error handling**
- **Factory pattern for different Steam API interfaces**
- **Built-in caching to respect rate limits**

### Enhanced Configuration Schema
```ini
[Steam API Settings]
SteamApiKey=<user-steam-api-key>
SteamId=<user-64bit-steam-id>
RefreshIntervalMinutes=5
EnableDetailedGameStats=false
TargetGameAppId=0

[Advanced Features]
EnableFriendsMonitoring=true
EnableReviewsData=false
EnableWorkshopData=false
CacheExpiryMinutes=10

[Rate Limiting]
RequestsPerSecond=1
EnableBackoff=true
MaxRetries=3
```

This research provides a comprehensive foundation for implementing a Steam Web API plugin for InfoPanel, with clear phases for development and detailed consideration of technical challenges. The additional research reveals significant improvements in data availability and provides proven implementation patterns through the SteamWebAPI2 library.