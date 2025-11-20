# Steam Web API Implementation Guide

## Overview

This document provides comprehensive implementation details for Steam Web API integration based on the official Steam Web API documentation at <https://steamapi.xpaw.me/>. This guide covers all API endpoints needed for our InfoPanel Steam plugin, including token management for enhanced access.

## Authentication Methods

### 1. Steam Web API Key

Standard authentication method for most endpoints.

```
https://steamcommunity.com/dev/apikey
```

### 2. Access Tokens (Enhanced Access)

Some APIs work better with access tokens for more detailed data:

#### Store Token

- **Source**: `https://store.steampowered.com/pointssummary/ajaxgetasyncconfig`
- **Token Field**: `webapi_token`
- **Scope**: Store-related APIs, enhanced game data
- **Expiration**: Variable (typically 24-48 hours)

#### Community Token  

- **Source**: `https://steamcommunity.com/my/edit/info`
- **Token Field**: `data-loyalty_webapi_token` from `application_config` element
- **Script**: `JSON.parse(application_config.dataset.loyalty_webapi_token)`
- **Scope**: Community features, enhanced profile data
- **Expiration**: Variable (typically 24-48 hours)

## Core API Endpoints for Plugin Implementation

### 1. Player Profile Data

#### GetPlayerSummaries (ISteamUser)

**Purpose**: Basic player information, online status, current game

```
GET https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/
Parameters:
- key: Steam Web API key
- steamids: 64-bit Steam ID
- format: json
```

**Response Fields We Use**:

```json
{
  "response": {
    "players": [{
      "steamid": "76561198000000000",
      "personaname": "PlayerName",           // Player Name Sensor
      "personastate": 1,                     // Online Status Sensor (0-6)
      "lastlogoff": 1234567890,             // Last Seen calculation
      "gameid": "440",                       // Current Game ID
      "gameextrainfo": "Team Fortress 2",   // Current Game Sensor
      "avatar": "avatar_url",                // Profile avatar
      "avatarfull": "avatar_full_url",       // High-res avatar
      "profileurl": "steam_profile_url",     // Profile URL
      "realname": "Real Name",               // Real name if public
      "timecreated": 1234567890,            // Account creation date
      "loccountrycode": "US"                // Location if public
    }]
  }
}
```

#### GetSteamLevel (IPlayerService)

**Purpose**: Player's Steam level

```
GET https://api.steampowered.com/IPlayerService/GetSteamLevel/v1/
Parameters:
- key: Steam Web API key
- steamid: 64-bit Steam ID
```

**Response**:

```json
{
  "response": {
    "player_level": 28                      // Steam Level Sensor
  }
}
```

### 2. Game Library Data

#### GetOwnedGames (IPlayerService)

**Purpose**: Complete game library with playtime statistics

```
GET https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/
Parameters:
- key: Steam Web API key
- steamid: 64-bit Steam ID
- include_appinfo: 1
- include_played_free_games: 1
- format: json
```

**Response Fields We Use**:

```json
{
  "response": {
    "game_count": 320,                      // Total Games Sensor
    "games": [{
      "appid": 440,
      "name": "Team Fortress 2",
      "playtime_forever": 9583,             // Total playtime (minutes)
      "img_icon_url": "icon_hash",
      "img_logo_url": "logo_hash", 
      "rtime_last_played": 1234567890,      // Last played timestamp
      "playtime_windows_forever": 9583,     // Platform-specific playtime
      "playtime_mac_forever": 0,
      "playtime_linux_forever": 0
    }]
  }
}
```

**Calculated Metrics**:

- Total Games: `game_count`
- Total Playtime: `sum(playtime_forever) / 60` hours
- Most Played Game: `max(playtime_forever)`
- Platform Distribution: Windows/Mac/Linux playtime ratios

#### GetRecentlyPlayedGames (IPlayerService)

**Purpose**: Games played in last 2 weeks

```
GET https://api.steampowered.com/IPlayerService/GetRecentlyPlayedGames/v1/
Parameters:
- key: Steam Web API key
- steamid: 64-bit Steam ID
- count: 0 (all recent games)
```

**Response Fields We Use**:

```json
{
  "response": {
    "total_count": 5,                       // Recent Games Count Sensor
    "games": [{
      "appid": 730,
      "name": "Counter-Strike 2",
      "playtime_2weeks": 3487,             // Recent playtime (minutes)
      "playtime_forever": 15632,           // Total playtime
      "img_icon_url": "icon_hash"
    }]
  }
}
```

**Calculated Metrics**:

- Recent Games Count: `total_count`
- Recent Playtime: `sum(playtime_2weeks) / 60` hours
- Most Played Recent: `max(playtime_2weeks)`
- Recent Games Table: Top games with 2-week activity

### 3. Social Features

#### GetFriendList (ISteamUser)

**Purpose**: Player's friends list

```
GET https://api.steampowered.com/ISteamUser/GetFriendList/v1/
Parameters:
- key: Steam Web API key
- steamid: 64-bit Steam ID
- relationship: friend
```

**Response**:

```json
{
  "friendslist": {
    "friends": [{
      "steamid": "76561198000000001",
      "relationship": "friend",
      "friend_since": 1234567890            // Friendship start timestamp
    }]
  }
}
```

**Enhanced Friends Data** (requires multiple GetPlayerSummaries calls):

- Total Friends Count
- Online Friends Count
- Friends In-Game Count
- Popular Game Among Friends

### 4. Achievement Data

#### GetPlayerAchievements (ISteamUserStats)

**Purpose**: Achievement data for specific games

```
GET https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/
Parameters:
- key: Steam Web API key
- steamid: 64-bit Steam ID
- appid: Game App ID
- l: english
```

**Response**:

```json
{
  "playerstats": {
    "steamID": "76561198000000000",
    "gameName": "Team Fortress 2",
    "achievements": [{
      "apiname": "TF_PLAY_GAME_EVERYCLASS",
      "achieved": 1,                        // 1 = unlocked, 0 = locked
      "unlocktime": 1234567890             // Unlock timestamp
    }]
  }
}
```

#### GetSchemaForGame (ISteamUserStats)

**Purpose**: Total achievements available for a game

```
GET https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/
Parameters:
- key: Steam Web API key
- appid: Game App ID
- l: english
```

**Achievement Metrics**:

- Total Achievements Unlocked: Sum across monitored games
- Achievement Completion %: (unlocked / total) * 100
- Perfect Games: Games with 100% achievements
- Latest Achievement: Most recent unlock across all games

### 5. Game News & Updates

#### GetNewsForApp (ISteamNews)

**Purpose**: Game news and updates

```
GET https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/
Parameters:
- appid: Game App ID
- count: 5
- maxlength: 300
- format: json
```

**Response**:

```json
{
  "appnews": {
    "appid": 440,
    "newsitems": [{
      "gid": "news_id",
      "title": "Update Title",
      "url": "news_url",
      "is_external_url": false,
      "author": "author_name", 
      "contents": "news_content",
      "feedlabel": "Product Update",
      "date": 1234567890,                   // Publication timestamp
      "feedname": "steam_community_announcements"
    }]
  }
}
```

### 6. Enhanced APIs (Require Tokens)

#### GetBadges (IPlayerService) - Community Token

**Purpose**: Steam badges and XP data

```
GET https://api.steampowered.com/IPlayerService/GetBadges/v1/
Parameters:
- access_token: Community access token
- steamid: 64-bit Steam ID
```

**Enhanced Features**:

- Total Badge Count
- Total XP/Level Progress
- Rare Badge Collection
- Badge Completion Rate

#### GetCommunityBadgeProgress (IPlayerService) - Community Token

**Purpose**: Badge progress and XP details

```
GET https://api.steampowered.com/IPlayerService/GetCommunityBadgeProgress/v1/
Parameters:
- access_token: Community access token
- steamid: 64-bit Steam ID
- badgeid: Badge ID (optional)
```

### 7. Store Data (Store Token)

#### Enhanced Game Data

With store tokens, we can access:

- Game review scores
- Wishlist information
- Store page details
- Price history data
- DLC ownership status

## Token Management Implementation

### 1. Token Storage Structure

```json
{
  "community_token": {
    "token": "community_access_token_here",
    "expires": 1699459020,
    "steamid": "76561198000000000",
    "scope": "web:community"
  },
  "store_token": {
    "token": "store_access_token_here", 
    "expires": 1699459020,
    "scope": "web:store"
  },
  "last_refresh": 1699372620,
  "auto_refresh_enabled": true
}
```

### 2. Token Acquisition Methods

#### Automated Token Refresh (Preferred)

```csharp
public class SteamTokenService
{
    private readonly HttpClient _httpClient;
    private readonly string _tokenFilePath;
    
    public async Task<string> GetCommunityTokenAsync()
    {
        // Check if current token is valid and not expired
        var tokens = LoadTokens();
        if (IsTokenValid(tokens?.CommunityToken))
        {
            return tokens.CommunityToken.Token;
        }
        
        // Attempt to refresh token automatically
        var newToken = await RefreshCommunityTokenAsync();
        if (newToken != null)
        {
            await SaveTokenAsync("community", newToken);
            return newToken.Token;
        }
        
        // Fallback to manual token entry
        return null;
    }
    
    private async Task<TokenInfo> RefreshCommunityTokenAsync()
    {
        try
        {
            // This requires authenticated session - complex to implement
            // May need to use embedded browser or manual process
            var response = await _httpClient.GetStringAsync("https://steamcommunity.com/my/edit/info");
            
            // Parse HTML to extract token from application_config
            var match = Regex.Match(response, @"data-loyalty_webapi_token=""([^""]+)""");
            if (match.Success)
            {
                var tokenData = JsonSerializer.Deserialize<TokenInfo>(match.Groups[1].Value);
                return tokenData;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to refresh community token: {ex.Message}");
        }
        
        return null;
    }
}
```

#### Manual Token Entry (Fallback)

```csharp
public class TokenManagementService
{
    public async Task<bool> SetManualTokenAsync(string tokenType, string tokenValue)
    {
        try
        {
            var tokenInfo = ParseTokenString(tokenValue);
            if (tokenInfo != null)
            {
                await SaveTokenAsync(tokenType, tokenInfo);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to set manual token: {ex.Message}");
        }
        
        return false;
    }
    
    private TokenInfo ParseTokenString(string input)
    {
        // Handle full JSON paste
        if (input.TrimStart().StartsWith("{"))
        {
            try
            {
                var fullResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(input);
                if (fullResponse.ContainsKey("webapi_token"))
                {
                    return new TokenInfo
                    {
                        Token = fullResponse["webapi_token"].ToString(),
                        Expires = CalculateExpiration(),
                        Scope = "web:store"
                    };
                }
            }
            catch { }
        }
        
        // Handle direct token paste
        if (input.Length > 20 && !input.Contains(" "))
        {
            return new TokenInfo
            {
                Token = input.Trim(),
                Expires = CalculateExpiration(),
                Scope = "unknown"
            };
        }
        
        return null;
    }
}
```

### 3. Configuration Integration

Add token management to INI configuration:

```ini
[Steam API Configuration]
SteamApiKey=your_steam_api_key_here
SteamId64=76561198000000000
UpdateIntervalSeconds=30

[Token Management]
AutoRefreshTokens=true
CommunityTokenEnabled=true
StoreTokenEnabled=true
TokenRefreshIntervalHours=12
ManualTokenEntry=false

[Advanced Features]
EnableEnhancedBadgeData=true
EnableStoreIntegration=true
EnableExtendedAchievements=true
MaxMonitoredGamesForAchievements=5
```

## Implementation Priority

### Phase 1: Core API Implementation

1. Replace simulation data with real Steam Web API calls
2. Implement JSON response models
3. Add proper error handling for private profiles
4. Test with basic Steam Web API key authentication

### Phase 2: Enhanced Data Collection

1. Add achievement tracking for monitored games
2. Implement friends list analysis
3. Add game news collection
4. Optimize API call frequency and caching

### Phase 3: Token Management

1. Implement token storage system
2. Add manual token entry interface
3. Build automatic token refresh (if feasible)
4. Enable enhanced features with tokens

### Phase 4: Advanced Features

1. Store integration for enhanced game data
2. Community features with enhanced permissions
3. Advanced analytics and trend tracking
4. Performance optimization and caching strategies

## Error Handling & Edge Cases

### Profile Privacy Settings

- **Private Profile**: Limited data available, graceful degradation
- **Friends-Only**: Some data accessible, some restricted
- **Public**: Full data access

### Rate Limiting

- **Steam Web API**: 100,000 requests/day per API key
- **Store/Community APIs**: Lower limits, need careful management
- **Implementation**: Exponential backoff, request queuing

### Data Validation

- **Missing Games**: Handle games not in Steam database
- **Invalid Responses**: Robust JSON parsing with fallbacks
- **Network Issues**: Retry logic with circuit breaker pattern

This comprehensive implementation guide provides the foundation for transitioning from simulation data to real Steam Web API integration, with provisions for enhanced features through token-based authentication.
