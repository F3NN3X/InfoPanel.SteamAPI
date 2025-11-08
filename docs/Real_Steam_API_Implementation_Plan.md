# Real Steam API Implementation Plan

## Overview

This document outlines the step-by-step implementation plan to replace our current simulation data with real Steam Web API integration. This implementation will provide accurate, live Steam data for our InfoPanel plugin.

## Phase 1: JSON Response Models (PRIORITY 1)

### Create Steam API Response Models

First, we need to create proper C# models to deserialize Steam API JSON responses.

#### 1.1 Player Summary Models
```csharp
// File: Models/SteamApiModels.cs

public class PlayerSummariesResponse
{
    [JsonPropertyName("response")]
    public PlayerSummariesResult Response { get; set; } = new();
}

public class PlayerSummariesResult
{
    [JsonPropertyName("players")]
    public List<SteamPlayer> Players { get; set; } = new();
}

public class SteamPlayer
{
    [JsonPropertyName("steamid")]
    public string SteamId { get; set; } = string.Empty;
    
    [JsonPropertyName("communityvisibilitystate")]
    public int CommunityVisibilityState { get; set; }
    
    [JsonPropertyName("profilestate")]
    public int ProfileState { get; set; }
    
    [JsonPropertyName("personaname")]
    public string PersonaName { get; set; } = string.Empty;
    
    [JsonPropertyName("profileurl")]
    public string ProfileUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;
    
    [JsonPropertyName("avatarmedium")]
    public string AvatarMedium { get; set; } = string.Empty;
    
    [JsonPropertyName("avatarfull")]
    public string AvatarFull { get; set; } = string.Empty;
    
    [JsonPropertyName("lastlogoff")]
    public long LastLogoff { get; set; }
    
    [JsonPropertyName("personastate")]
    public int PersonaState { get; set; }
    
    [JsonPropertyName("realname")]
    public string? RealName { get; set; }
    
    [JsonPropertyName("primaryclanid")]
    public string? PrimaryClanId { get; set; }
    
    [JsonPropertyName("timecreated")]
    public long? TimeCreated { get; set; }
    
    [JsonPropertyName("gameid")]
    public string? GameId { get; set; }
    
    [JsonPropertyName("gameextrainfo")]
    public string? GameExtraInfo { get; set; }
    
    [JsonPropertyName("gameserverip")]
    public string? GameServerIp { get; set; }
    
    [JsonPropertyName("loccountrycode")]
    public string? LocCountryCode { get; set; }
    
    [JsonPropertyName("locstatecode")]
    public string? LocStateCode { get; set; }
}
```

#### 1.2 Steam Level Models
```csharp
public class SteamLevelResponse
{
    [JsonPropertyName("response")]
    public SteamLevelResult Response { get; set; } = new();
}

public class SteamLevelResult
{
    [JsonPropertyName("player_level")]
    public int PlayerLevel { get; set; }
}
```

#### 1.3 Owned Games Models
```csharp
public class OwnedGamesResponse
{
    [JsonPropertyName("response")]
    public OwnedGamesResult Response { get; set; } = new();
}

public class OwnedGamesResult
{
    [JsonPropertyName("game_count")]
    public int GameCount { get; set; }
    
    [JsonPropertyName("games")]
    public List<SteamGame> Games { get; set; } = new();
}

public class SteamGame
{
    [JsonPropertyName("appid")]
    public int AppId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("playtime_forever")]
    public int PlaytimeForever { get; set; }
    
    [JsonPropertyName("img_icon_url")]
    public string ImgIconUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("img_logo_url")]
    public string ImgLogoUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("playtime_windows_forever")]
    public int PlaytimeWindowsForever { get; set; }
    
    [JsonPropertyName("playtime_mac_forever")]
    public int PlaytimeMacForever { get; set; }
    
    [JsonPropertyName("playtime_linux_forever")]
    public int PlaytimeLinuxForever { get; set; }
    
    [JsonPropertyName("rtime_last_played")]
    public long RtimeLastPlayed { get; set; }
    
    [JsonPropertyName("playtime_disconnected")]
    public int PlaytimeDisconnected { get; set; }
    
    [JsonPropertyName("playtime_2weeks")]
    public int? Playtime2weeks { get; set; }
}
```

#### 1.4 Friends List Models
```csharp
public class FriendsListResponse
{
    [JsonPropertyName("friendslist")]
    public FriendsListResult FriendsList { get; set; } = new();
}

public class FriendsListResult
{
    [JsonPropertyName("friends")]
    public List<SteamFriend> Friends { get; set; } = new();
}

public class SteamFriend
{
    [JsonPropertyName("steamid")]
    public string SteamId { get; set; } = string.Empty;
    
    [JsonPropertyName("relationship")]
    public string Relationship { get; set; } = string.Empty;
    
    [JsonPropertyName("friend_since")]
    public long FriendSince { get; set; }
}
```

## Phase 2: Update SteamApiService (PRIORITY 1)

### Replace Simulation Methods with Real API Calls

#### 2.1 Update Core Methods
```csharp
public async Task<PlayerSummariesResponse?> GetPlayerSummaryAsync()
{
    try
    {
        var endpoint = $"ISteamUser/GetPlayerSummaries/v2/?key={_apiKey}&steamids={_steamId64}&format=json";
        var jsonResponse = await CallSteamApiAsync(endpoint);
        
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<PlayerSummariesResponse>(jsonResponse, options);
        }
        
        return null;
    }
    catch (Exception ex)
    {
        _logger?.LogError($"Error getting player summary: {ex.Message}");
        return null;
    }
}

public async Task<SteamLevelResponse?> GetSteamLevelAsync()
{
    try
    {
        var endpoint = $"IPlayerService/GetSteamLevel/v1/?key={_apiKey}&steamid={_steamId64}&format=json";
        var jsonResponse = await CallSteamApiAsync(endpoint);
        
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<SteamLevelResponse>(jsonResponse, options);
        }
        
        return null;
    }
    catch (Exception ex)
    {
        _logger?.LogError($"Error getting Steam level: {ex.Message}");
        return null;
    }
}

public async Task<OwnedGamesResponse?> GetOwnedGamesAsync()
{
    try
    {
        var endpoint = $"IPlayerService/GetOwnedGames/v1/?key={_apiKey}&steamid={_steamId64}&include_appinfo=1&include_played_free_games=1&format=json";
        var jsonResponse = await CallSteamApiAsync(endpoint);
        
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<OwnedGamesResponse>(jsonResponse, options);
        }
        
        return null;
    }
    catch (Exception ex)
    {
        _logger?.LogError($"Error getting owned games: {ex.Message}");
        return null;
    }
}

public async Task<OwnedGamesResponse?> GetRecentlyPlayedGamesAsync()
{
    try
    {
        var endpoint = $"IPlayerService/GetRecentlyPlayedGames/v1/?key={_apiKey}&steamid={_steamId64}&format=json";
        var jsonResponse = await CallSteamApiAsync(endpoint);
        
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<OwnedGamesResponse>(jsonResponse, options);
        }
        
        return null;
    }
    catch (Exception ex)
    {
        _logger?.LogError($"Error getting recently played games: {ex.Message}");
        return null;
    }
}

public async Task<FriendsListResponse?> GetFriendsListAsync()
{
    try
    {
        var endpoint = $"ISteamUser/GetFriendList/v1/?key={_apiKey}&steamid={_steamId64}&relationship=friend&format=json";
        var jsonResponse = await CallSteamApiAsync(endpoint);
        
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<FriendsListResponse>(jsonResponse, options);
        }
        
        return null;
    }
    catch (Exception ex)
    {
        _logger?.LogError($"Error getting friends list: {ex.Message}");
        return null;
    }
}
```

## Phase 3: Update MonitoringService (PRIORITY 1)

### Replace Data Collection with Real API Parsing

#### 3.1 Update CollectSteamDataAsync Method
```csharp
private async Task<SteamData> CollectSteamDataAsync()
{
    var data = new SteamData();
    
    try
    {
        _logger?.LogDebug("Starting real Steam data collection...");
        
        // Get player summary (profile, status, current game)
        var playerSummary = await _apiService.GetPlayerSummaryAsync();
        if (playerSummary?.Response?.Players?.FirstOrDefault() is SteamPlayer player)
        {
            data.PlayerName = player.PersonaName;
            data.OnlineStatus = GetPersonaStateString(player.PersonaState);
            data.IsOnline = player.PersonaState > 0;
            data.CurrentGame = player.GameExtraInfo ?? "Not Playing";
            data.CurrentGameId = player.GameId ?? "0";
            data.LastSeen = player.PersonaState == 0 ? DateTimeOffset.FromUnixTimeSeconds(player.LastLogoff) : null;
            data.ProfileUrl = player.ProfileUrl;
            data.AvatarUrl = player.AvatarFull;
            data.RealName = player.RealName;
            data.CountryCode = player.LocCountryCode;
            
            _logger?.LogInfo($"Player Summary - Name: {data.PlayerName}, Status: {data.OnlineStatus}, Current Game: {data.CurrentGame}");
        }
        
        // Get Steam level
        var levelResponse = await _apiService.GetSteamLevelAsync();
        if (levelResponse?.Response != null)
        {
            data.SteamLevel = levelResponse.Response.PlayerLevel;
            _logger?.LogDebug($"Steam Level: {data.SteamLevel}");
        }
        
        // Get owned games (for total games and playtime)
        var ownedGames = await _apiService.GetOwnedGamesAsync();
        if (ownedGames?.Response is OwnedGamesResult gamesResult)
        {
            data.TotalGames = gamesResult.GameCount;
            data.TotalPlaytimeHours = gamesResult.Games.Sum(g => g.PlaytimeForever) / 60.0;
            
            // Find most played game
            var mostPlayed = gamesResult.Games.OrderByDescending(g => g.PlaytimeForever).FirstOrDefault();
            if (mostPlayed != null)
            {
                data.MostPlayedGame = mostPlayed.Name;
                data.MostPlayedHours = mostPlayed.PlaytimeForever / 60.0;
            }
            
            // Calculate platform distribution
            var totalWindows = gamesResult.Games.Sum(g => g.PlaytimeWindowsForever);
            var totalMac = gamesResult.Games.Sum(g => g.PlaytimeMacForever);
            var totalLinux = gamesResult.Games.Sum(g => g.PlaytimeLinuxForever);
            var total = totalWindows + totalMac + totalLinux;
            
            if (total > 0)
            {
                data.WindowsPlaytimePercentage = (totalWindows / (double)total) * 100;
                data.MacPlaytimePercentage = (totalMac / (double)total) * 100;
                data.LinuxPlaytimePercentage = (totalLinux / (double)total) * 100;
            }
            
            _logger?.LogInfo($"Library Data - Games Owned: {data.TotalGames}, Total Playtime: {data.TotalPlaytimeHours:F1} hours");
        }
        
        // Get recently played games
        var recentGames = await _apiService.GetRecentlyPlayedGamesAsync();
        if (recentGames?.Response is OwnedGamesResult recentResult)
        {
            data.RecentGamesCount = recentResult.GameCount;
            data.RecentPlaytimeHours = recentResult.Games.Sum(g => g.Playtime2weeks ?? 0) / 60.0;
            
            // Find most played recent game
            var mostPlayedRecent = recentResult.Games.OrderByDescending(g => g.Playtime2weeks ?? 0).FirstOrDefault();
            if (mostPlayedRecent != null)
            {
                data.MostPlayedRecentGame = mostPlayedRecent.Name;
                data.MostPlayedRecentHours = (mostPlayedRecent.Playtime2weeks ?? 0) / 60.0;
            }
            
            // Convert to our RecentlyPlayedGame format
            data.RecentGames = recentResult.Games.Select(g => new RecentlyPlayedGame
            {
                Name = g.Name,
                AppId = g.AppId.ToString(),
                PlaytimeForever = g.PlaytimeForever / 60.0,
                Playtime2weeks = (g.Playtime2weeks ?? 0) / 60.0,
                LastPlayed = DateTimeOffset.FromUnixTimeSeconds(g.RtimeLastPlayed),
                IconUrl = $"https://media.steampowered.com/steamcommunity/public/images/apps/{g.AppId}/{g.ImgIconUrl}.jpg"
            }).ToList();
            
            _logger?.LogInfo($"Recent Activity - Games: {data.RecentGamesCount}, Playtime (2w): {data.RecentPlaytimeHours:F1} hours");
        }
        
        // Get friends list (for social features)
        if (_configService.EnableSocialFeatures)
        {
            var friendsList = await _apiService.GetFriendsListAsync();
            if (friendsList?.FriendsList?.Friends != null)
            {
                data.TotalFriends = friendsList.FriendsList.Friends.Count;
                
                // Get online friends status (requires additional API calls)
                await CollectFriendsDataAsync(data, friendsList.FriendsList.Friends);
            }
        }
        
        data.LastUpdated = DateTimeOffset.Now;
        _logger?.LogInfo("Real Steam data collection completed successfully");
        
        return data;
    }
    catch (Exception ex)
    {
        _logger?.LogError("Failed to collect real Steam data", ex);
        return GetErrorStateData();
    }
}

private string GetPersonaStateString(int personaState)
{
    return personaState switch
    {
        0 => "Offline",
        1 => "Online", 
        2 => "Busy",
        3 => "Away",
        4 => "Snooze",
        5 => "Looking to Trade",
        6 => "Looking to Play",
        _ => "Unknown"
    };
}
```

## Phase 4: Enhanced Features with Token Integration

### 4.1 Badge and Achievement Data
```csharp
public async Task<BadgesResponse?> GetPlayerBadgesAsync(string? communityToken = null)
{
    try
    {
        string endpoint;
        if (!string.IsNullOrEmpty(communityToken))
        {
            // Use community token for enhanced data
            endpoint = $"IPlayerService/GetBadges/v1/?access_token={communityToken}&steamid={_steamId64}&format=json";
        }
        else
        {
            // Fallback to API key (limited data)
            endpoint = $"IPlayerService/GetBadges/v1/?key={_apiKey}&steamid={_steamId64}&format=json";
        }
        
        var jsonResponse = await CallSteamApiAsync(endpoint);
        
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            return JsonSerializer.Deserialize<BadgesResponse>(jsonResponse);
        }
        
        return null;
    }
    catch (Exception ex)
    {
        _logger?.LogError($"Error getting player badges: {ex.Message}");
        return null;
    }
}
```

## Phase 5: Error Handling and Privacy Support

### 5.1 Profile Privacy Detection
```csharp
private bool IsProfilePublic(SteamPlayer player)
{
    // communityvisibilitystate: 1 = Private, 3 = Public
    return player.CommunityVisibilityState == 3;
}

private SteamData HandlePrivateProfile(SteamPlayer player)
{
    var data = new SteamData
    {
        PlayerName = player.PersonaName,
        OnlineStatus = GetPersonaStateString(player.PersonaState),
        IsOnline = player.PersonaState > 0,
        ProfileUrl = player.ProfileUrl,
        AvatarUrl = player.AvatarFull,
        IsPrivateProfile = true,
        ErrorMessage = "Profile is private - limited data available"
    };
    
    _logger?.LogWarning($"Profile is private for {player.PersonaName} - limited data available");
    return data;
}
```

### 5.2 Rate Limiting and Retry Logic
```csharp
private async Task<string?> CallSteamApiAsync(string endpoint)
{
    var maxRetries = 3;
    var baseDelay = TimeSpan.FromSeconds(1);
    
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}{endpoint}");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) + baseDelay;
                _logger?.LogWarning($"Rate limited, waiting {delay.TotalSeconds} seconds before retry {attempt + 1}/{maxRetries}");
                await Task.Delay(delay);
                continue;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger?.LogError("API key is invalid or lacks permissions");
                return null;
            }
            
            _logger?.LogError($"API call failed with status {response.StatusCode}: {response.ReasonPhrase}");
            return null;
        }
        catch (HttpRequestException ex)
        {
            if (attempt == maxRetries - 1)
            {
                _logger?.LogError($"API call failed after {maxRetries} attempts", ex);
                return null;
            }
            
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) + baseDelay;
            _logger?.LogWarning($"Network error, retrying in {delay.TotalSeconds} seconds: {ex.Message}");
            await Task.Delay(delay);
        }
    }
    
    return null;
}
```

## Phase 6: Configuration and Testing

### 6.1 Add Required NuGet Package
```xml
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

### 6.2 Configuration Validation
```csharp
public bool ValidateRealApiConfiguration()
{
    if (string.IsNullOrWhiteSpace(SteamApiKey) || SteamApiKey.Contains("your-steam-api-key"))
    {
        _logger?.LogError("Steam Web API key is required for real data collection");
        return false;
    }
    
    if (!IsValidSteamId64(SteamId64))
    {
        _logger?.LogError($"Invalid SteamID64 format: {SteamId64}");
        return false;
    }
    
    return true;
}
```

## Implementation Priority Order

1. **Phase 1**: Create JSON response models ⭐
2. **Phase 2**: Update SteamApiService with real API calls ⭐
3. **Phase 3**: Update MonitoringService to parse real data ⭐
4. **Test with Basic API Key**: Verify core functionality works
5. **Phase 4**: Implement token management for enhanced features
6. **Phase 5**: Add comprehensive error handling
7. **Phase 6**: Optimize and add advanced features

## Testing Strategy

### 6.3 Test Plan
1. **Unit Testing**: Test individual API methods with mock responses
2. **Integration Testing**: Test with real Steam API key and public profile
3. **Error Testing**: Test with private profiles, invalid keys, rate limiting
4. **Performance Testing**: Monitor API call frequency and response times

This implementation plan will transition our plugin from simulation data to real Steam Web API integration while maintaining robustness and handling edge cases like private profiles and rate limiting.