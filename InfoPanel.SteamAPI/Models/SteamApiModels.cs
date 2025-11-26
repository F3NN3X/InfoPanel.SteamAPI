using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InfoPanel.SteamAPI.Models
{
    /// <summary>
    /// Steam API response models for JSON deserialization
    /// Based on official Steam Web API documentation from https://steamapi.xpaw.me/
    /// </summary>

    #region Player Summary Models

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

        [JsonPropertyName("loccityid")]
        public int? LocCityId { get; set; }
    }

    #endregion

    #region Steam Level Models

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

    #endregion

    #region Owned Games Models

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

        [JsonPropertyName("client_icon")]
        public string? ClientIcon { get; set; }

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

    #endregion

    #region Friends List Models

    public class FriendsListResponse
    {
        [JsonPropertyName("friendslist")]
        public FriendsListResult? FriendsList { get; set; }
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

        // Extended properties for detailed friend information
        public string PersonaName { get; set; } = string.Empty;
        public string OnlineStatus { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public long LastLogOff { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string ProfileUrl { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
    }

    #endregion

    #region Achievement Models

    public class PlayerAchievementsResponse
    {
        [JsonPropertyName("playerstats")]
        public PlayerAchievementsResult? PlayerStats { get; set; }
    }

    public class PlayerAchievementsResult
    {
        [JsonPropertyName("steamID")]
        public string SteamID { get; set; } = string.Empty;

        [JsonPropertyName("gameName")]
        public string GameName { get; set; } = string.Empty;

        [JsonPropertyName("achievements")]
        public List<SteamAchievement> Achievements { get; set; } = new();

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    public class SteamAchievement
    {
        [JsonPropertyName("apiname")]
        public string ApiName { get; set; } = string.Empty;

        [JsonPropertyName("achieved")]
        public int Achieved { get; set; }

        [JsonPropertyName("unlocktime")]
        public long UnlockTime { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class GameSchemaResponse
    {
        [JsonPropertyName("game")]
        public GameSchemaResult? Game { get; set; }
    }

    public class GameSchemaResult
    {
        [JsonPropertyName("gameName")]
        public string GameName { get; set; } = string.Empty;

        [JsonPropertyName("gameVersion")]
        public string GameVersion { get; set; } = string.Empty;

        [JsonPropertyName("availableGameStats")]
        public GameStatsInfo? AvailableGameStats { get; set; }
    }

    public class GameStatsInfo
    {
        [JsonPropertyName("achievements")]
        public List<AchievementSchema> Achievements { get; set; } = new();
    }

    public class AchievementSchema
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("defaultvalue")]
        public int DefaultValue { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("hidden")]
        public int Hidden { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonPropertyName("icongray")]
        public string IconGray { get; set; } = string.Empty;
    }

    #endregion

    #region Game News Models

    public class GameNewsResponse
    {
        [JsonPropertyName("appnews")]
        public GameNewsResult? AppNews { get; set; }
    }

    public class GameNewsResult
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }

        [JsonPropertyName("newsitems")]
        public List<NewsItem> NewsItems { get; set; } = new();

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class NewsItem
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("is_external_url")]
        public bool IsExternalUrl { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("contents")]
        public string Contents { get; set; } = string.Empty;

        [JsonPropertyName("feedlabel")]
        public string FeedLabel { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("feedname")]
        public string FeedName { get; set; } = string.Empty;
    }

    #endregion

    #region Enhanced Models (Token-based APIs)

    public class BadgesResponse
    {
        [JsonPropertyName("response")]
        public BadgesResult Response { get; set; } = new();
    }

    public class BadgesResult
    {
        [JsonPropertyName("badges")]
        public List<SteamBadge> Badges { get; set; } = new();

        [JsonPropertyName("player_xp")]
        public int PlayerXp { get; set; }

        [JsonPropertyName("player_level")]
        public int PlayerLevel { get; set; }

        [JsonPropertyName("player_xp_needed_to_level_up")]
        public int PlayerXpNeededToLevelUp { get; set; }

        [JsonPropertyName("player_xp_needed_current_level")]
        public int PlayerXpNeededCurrentLevel { get; set; }
    }

    public class SteamBadge
    {
        [JsonPropertyName("badgeid")]
        public int BadgeId { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("completion_time")]
        public long CompletionTime { get; set; }

        [JsonPropertyName("xp")]
        public int Xp { get; set; }

        [JsonPropertyName("scarcity")]
        public int Scarcity { get; set; }

        [JsonPropertyName("appid")]
        public int? AppId { get; set; }

        [JsonPropertyName("communityitemid")]
        public string? CommunityItemId { get; set; }

        [JsonPropertyName("border_color")]
        public int BorderColor { get; set; }
    }

    #endregion

    #region Error Response Models

    public class SteamApiErrorResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    #endregion

    #region Utility Classes

    /// <summary>
    /// Utilities for working with Steam API persona states
    /// </summary>
    public static class SteamPersonaState
    {
        public const int Offline = 0;
        public const int Online = 1;
        public const int Busy = 2;
        public const int Away = 3;
        public const int Snooze = 4;
        public const int LookingToTrade = 5;
        public const int LookingToPlay = 6;

        public static string GetPersonaStateString(int personaState)
        {
            return personaState switch
            {
                Offline => "Offline",
                Online => "Online",
                Busy => "Busy",
                Away => "Away",
                Snooze => "Snooze",
                LookingToTrade => "Looking to Trade",
                LookingToPlay => "Looking to Play",
                _ => "Unknown"
            };
        }

        public static bool IsOnline(int personaState)
        {
            return personaState > Offline;
        }
    }

    /// <summary>
    /// Utilities for working with Steam API visibility states
    /// </summary>
    public static class SteamVisibilityState
    {
        public const int Private = 1;
        public const int FriendsOnly = 2;
        public const int Public = 3;

        public static string GetVisibilityStateString(int visibilityState)
        {
            return visibilityState switch
            {
                Private => "Private",
                FriendsOnly => "Friends Only",
                Public => "Public",
                _ => "Unknown"
            };
        }

        public static bool IsPublic(int visibilityState)
        {
            return visibilityState == Public;
        }

        public static bool CanAccessLibraryData(int visibilityState)
        {
            return visibilityState >= FriendsOnly;
        }
    }

    #endregion
}