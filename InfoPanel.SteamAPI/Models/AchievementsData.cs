using System;

namespace InfoPanel.SteamAPI.Models
{
    /// <summary>
    /// Data model for Achievements and Badges domain
    /// </summary>
    public class AchievementsData
    {
        #region Badges Data

        public int TotalBadgesEarned { get; set; }
        public int TotalBadgeXP { get; set; }
        public int PlayerLevel { get; set; }
        public string? LatestBadgeName { get; set; }
        public DateTime? LatestBadgeDate { get; set; }

        #endregion

        #region Current Game Achievements

        public int CurrentGameAppId { get; set; }
        public string? CurrentGameName { get; set; }
        public int CurrentGameAchievementCount { get; set; }
        public int CurrentGameUnlockedCount { get; set; }
        public double CurrentGameCompletionPercent { get; set; }
        public string? LatestAchievementName { get; set; }

        #endregion

        #region Metadata

        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        #endregion
    }
}
