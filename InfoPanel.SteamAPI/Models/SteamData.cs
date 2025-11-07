using System;

namespace InfoPanel.SteamAPI.Models
{
    /// <summary>
    /// Data model for Steam API monitoring data
    /// Contains Steam profile information, game statistics, and playtime data
    /// </summary>
    public class SteamData
    {
        #region Core Properties
        
        /// <summary>
        /// Total playtime in hours for currently displayed game
        /// </summary>
        public double TotalPlaytimeHours { get; set; }
        
        /// <summary>
        /// Total number of games owned
        /// </summary>
        public double TotalGamesOwned { get; set; }
        
        /// <summary>
        /// Current status of the Steam profile
        /// </summary>
        public string? Status { get; set; }
        
        /// <summary>
        /// Detailed information about current activity
        /// </summary>
        public string? Details { get; set; }
        
        /// <summary>
        /// Timestamp when this data was collected
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Indicates if there's an error in data collection
        /// </summary>
        public bool HasError { get; set; }
        
        /// <summary>
        /// Error message if HasError is true
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        #endregion

        #region Steam Profile Properties
        
        /// <summary>
        /// Steam username/display name
        /// </summary>
        public string? PlayerName { get; set; }
        
        /// <summary>
        /// Steam profile URL
        /// </summary>
        public string? ProfileUrl { get; set; }
        
        /// <summary>
        /// Steam level (XP-based progression)
        /// </summary>
        public int SteamLevel { get; set; }
        
        /// <summary>
        /// Avatar URL (small version)
        /// </summary>
        public string? AvatarUrl { get; set; }
        
        /// <summary>
        /// Online status (Online, Offline, Away, Busy, Snooze, Looking to trade, Looking to play)
        /// </summary>
        public string? OnlineState { get; set; }
        
        /// <summary>
        /// Last logoff timestamp (Unix timestamp)
        /// </summary>
        public long LastLogOff { get; set; }
        
        #endregion

        #region Current Game Properties
        
        /// <summary>
        /// Name of currently playing game
        /// </summary>
        public string? CurrentGameName { get; set; }
        
        /// <summary>
        /// App ID of currently playing game
        /// </summary>
        public int CurrentGameAppId { get; set; }
        
        /// <summary>
        /// Extra info about current game session
        /// </summary>
        public string? CurrentGameExtraInfo { get; set; }
        
        /// <summary>
        /// Server IP for multiplayer games
        /// </summary>
        public string? CurrentGameServerIp { get; set; }
        
        #endregion

        #region Library Statistics
        
        /// <summary>
        /// Total playtime across all games in hours
        /// </summary>
        public double TotalLibraryPlaytimeHours { get; set; }
        
        /// <summary>
        /// Most played game name
        /// </summary>
        public string? MostPlayedGameName { get; set; }
        
        /// <summary>
        /// Playtime for most played game in hours
        /// </summary>
        public double MostPlayedGameHours { get; set; }
        
        /// <summary>
        /// Recent playtime (last 2 weeks) in hours
        /// </summary>
        public double RecentPlaytimeHours { get; set; }
        
        /// <summary>
        /// Number of games played recently (last 2 weeks)
        /// </summary>
        public int RecentGamesCount { get; set; }
        
        #endregion

        #region Achievement Statistics
        
        /// <summary>
        /// Total achievements unlocked across all games
        /// </summary>
        public int TotalAchievements { get; set; }
        
        /// <summary>
        /// Perfect games (100% achievements)
        /// </summary>
        public int PerfectGames { get; set; }
        
        /// <summary>
        /// Average game completion percentage
        /// </summary>
        public double AverageGameCompletion { get; set; }
        
        #endregion

        #region Phase 2: Enhanced Gaming Metrics
        
        // Recent Gaming Activity (2-week stats)
        /// <summary>
        /// Most played game in recent period
        /// </summary>
        public string? MostPlayedRecentGame { get; set; }
        
        /// <summary>
        /// Number of gaming sessions in last 2 weeks
        /// </summary>
        public int RecentGameSessions { get; set; }
        
        // Session Time Tracking
        /// <summary>
        /// Current session time in minutes
        /// </summary>
        public int CurrentSessionTimeMinutes { get; set; }
        
        /// <summary>
        /// When the current gaming session started
        /// </summary>
        public DateTime? SessionStartTime { get; set; }
        
        /// <summary>
        /// Average session length in minutes
        /// </summary>
        public double AverageSessionTimeMinutes { get; set; }
        
        // Friends Online Monitoring
        /// <summary>
        /// Number of friends currently online
        /// </summary>
        public int FriendsOnline { get; set; }
        
        /// <summary>
        /// Number of friends currently in a game
        /// </summary>
        public int FriendsInGame { get; set; }
        
        /// <summary>
        /// Most popular game among online friends
        /// </summary>
        public string? FriendsPopularGame { get; set; }
        
        // Achievement Tracking (for current game)
        /// <summary>
        /// Achievement completion percentage for current game
        /// </summary>
        public double CurrentGameAchievementPercentage { get; set; }
        
        /// <summary>
        /// Number of achievements unlocked in current game
        /// </summary>
        public int CurrentGameAchievementsUnlocked { get; set; }
        
        /// <summary>
        /// Total achievements available in current game
        /// </summary>
        public int CurrentGameAchievementsTotal { get; set; }
        
        /// <summary>
        /// Name of the most recently unlocked achievement
        /// </summary>
        public string? LatestAchievementName { get; set; }
        
        /// <summary>
        /// Date when the latest achievement was unlocked
        /// </summary>
        public DateTime? LatestAchievementDate { get; set; }
        
        #endregion

        #region Constructors
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public SteamData()
        {
        }
        
        /// <summary>
        /// Constructor with basic data
        /// </summary>
        public SteamData(double totalPlaytimeHours, double totalGamesOwned, string? status = null)
        {
            TotalPlaytimeHours = totalPlaytimeHours;
            TotalGamesOwned = totalGamesOwned;
            Status = status;
        }
        
        /// <summary>
        /// Constructor for error states
        /// </summary>
        public SteamData(string errorMessage)
        {
            HasError = true;
            ErrorMessage = errorMessage;
            Status = "Error";
            Details = errorMessage;
        }
        
        #endregion

        #region Validation
        
        /// <summary>
        /// Validates that the data is in a consistent state
        /// </summary>
        public bool IsValid()
        {
            try
            {
                // Basic validation rules
                if (HasError && string.IsNullOrWhiteSpace(ErrorMessage))
                    return false;
                
                if (!HasError && (double.IsNaN(TotalPlaytimeHours) || double.IsInfinity(TotalPlaytimeHours)))
                    return false;
                
                if (!HasError && (double.IsNaN(TotalGamesOwned) || double.IsInfinity(TotalGamesOwned)))
                    return false;
                
                // Steam-specific validation
                if (TotalPlaytimeHours < 0) return false;
                if (TotalGamesOwned < 0) return false;
                if (SteamLevel < 0) return false;
                if (CurrentGameAppId < 0) return false;
                if (RecentPlaytimeHours < 0) return false;
                if (TotalAchievements < 0) return false;
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion

        #region Data Formatting
        
        /// <summary>
        /// Returns a formatted string representation of total playtime
        /// </summary>
        public string GetFormattedPlaytime(int decimalPlaces = 1, string unit = "hrs")
        {
            if (HasError) return "Error";
            
            var formatted = Math.Round(TotalPlaytimeHours, decimalPlaces).ToString($"F{decimalPlaces}");
            return string.IsNullOrEmpty(unit) ? formatted : $"{formatted} {unit}";
        }
        
        /// <summary>
        /// Returns a formatted string representation of games owned
        /// </summary>
        public string GetFormattedGamesOwned()
        {
            if (HasError) return "Error";
            
            return TotalGamesOwned.ToString("F0");
        }
        
        /// <summary>
        /// Returns a formatted timestamp string
        /// </summary>
        public string GetFormattedTimestamp(string format = "HH:mm:ss")
        {
            return Timestamp.ToString(format);
        }
        
        /// <summary>
        /// Returns a formatted string for last logoff time
        /// </summary>
        public string GetFormattedLastLogOff()
        {
            if (LastLogOff == 0) return "Unknown";
            
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(LastLogOff).DateTime;
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalHours < 1) return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalDays < 1) return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 30) return $"{(int)timeSpan.TotalDays} days ago";
            
            return dateTime.ToString("MMM dd, yyyy");
        }
        
        #endregion

        #region Steam-Specific Methods
        
        /// <summary>
        /// Calculates average playtime per game
        /// </summary>
        public double CalculateAveragePlaytimePerGame()
        {
            if (TotalGamesOwned <= 0) return 0;
            return TotalLibraryPlaytimeHours / TotalGamesOwned;
        }
        
        /// <summary>
        /// Determines if the player is currently online
        /// </summary>
        public bool IsOnline()
        {
            return OnlineState == "Online" || !string.IsNullOrEmpty(CurrentGameName);
        }
        
        /// <summary>
        /// Determines if the player is currently in a game
        /// </summary>
        public bool IsInGame()
        {
            return !string.IsNullOrEmpty(CurrentGameName) && CurrentGameAppId > 0;
        }
        
        /// <summary>
        /// Gets a simplified status for display
        /// </summary>
        public string GetDisplayStatus()
        {
            if (HasError) return "Error";
            if (IsInGame()) return $"Playing {CurrentGameName}";
            if (IsOnline()) return OnlineState ?? "Online";
            return "Offline";
        }
        
        /// <summary>
        /// Gets a health status color based on data availability
        /// </summary>
        public string GetHealthStatusColor()
        {
            if (HasError) return "Red";
            if (string.IsNullOrEmpty(PlayerName)) return "Orange";
            if (IsOnline()) return "Green";
            return "Yellow";
        }
        
        /// <summary>
        /// Calculates gaming activity level based on recent playtime
        /// </summary>
        public string GetActivityLevel()
        {
            if (HasError) return "Unknown";
            if (RecentPlaytimeHours > 20) return "Very Active";
            if (RecentPlaytimeHours > 10) return "Active";
            if (RecentPlaytimeHours > 2) return "Casual";
            if (RecentPlaytimeHours > 0) return "Light";
            return "Inactive";
        }
        
        #endregion

        #region Equality and Comparison
        
        /// <summary>
        /// Determines if this data is significantly different from another instance
        /// </summary>
        public bool HasSignificantChange(SteamData? other, double threshold = 0.1)
        {
            if (other == null) return true;
            if (HasError != other.HasError) return true;
            if (HasError && other.HasError) return ErrorMessage != other.ErrorMessage;
            
            var playtimeDiff = Math.Abs(TotalPlaytimeHours - other.TotalPlaytimeHours);
            var gamesDiff = Math.Abs(TotalGamesOwned - other.TotalGamesOwned);
            
            return playtimeDiff > threshold || 
                   gamesDiff > threshold || 
                   Status != other.Status ||
                   CurrentGameName != other.CurrentGameName ||
                   OnlineState != other.OnlineState;
        }
        
        #endregion

        #region String Representation
        
        /// <summary>
        /// Returns a string representation of the data
        /// </summary>
        public override string ToString()
        {
            if (HasError)
            {
                return $"SteamData[Error: {ErrorMessage}]";
            }
            
            var currentGame = IsInGame() ? $", Playing: {CurrentGameName}" : "";
            return $"SteamData[{PlayerName}, Games: {TotalGamesOwned:F0}, Playtime: {TotalPlaytimeHours:F1}hrs{currentGame}, Status: {OnlineState}, Time: {Timestamp:HH:mm:ss}]";
        }
        
        #endregion
    }
}