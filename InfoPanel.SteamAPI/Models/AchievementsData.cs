using System;

namespace InfoPanel.SteamAPI.Models
{
    /// <summary>
    /// Data model for Achievements domain
    /// </summary>
    public class AchievementsData
    {
        #region Current Game Achievements

        public int CurrentGameAppId { get; set; }
        public string? CurrentGameName { get; set; }
        public int CurrentGameAchievementCount { get; set; }
        public int CurrentGameUnlockedCount { get; set; }
        public double CurrentGameCompletionPercent { get; set; }
        public string? LatestAchievementName { get; set; }
        public string? LatestAchievementIcon { get; set; }
        public bool IsLive { get; set; }

        #endregion

        #region Metadata

        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        #endregion
    }
}
