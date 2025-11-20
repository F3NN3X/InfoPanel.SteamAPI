using InfoPanel.SteamAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Services
{
    /// <summary>
    /// Service responsible for collecting achievements and badges data
    /// </summary>
    public class AchievementsDataService
    {
        private readonly ConfigurationService _configService;
        private readonly SteamApiService _steamApiService;
        private readonly EnhancedLoggingService? _enhancedLogger;

        public AchievementsDataService(
            ConfigurationService configService,
            SteamApiService steamApiService,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _steamApiService = steamApiService ?? throw new ArgumentNullException(nameof(steamApiService));
            _enhancedLogger = enhancedLogger;
        }

        /// <summary>
        /// Collects achievements and badges data
        /// </summary>
        /// <param name="currentGameAppId">The AppID of the game currently being played (0 if none)</param>
        public async Task<AchievementsData> CollectAchievementsDataAsync(int currentGameAppId)
        {
            var data = new AchievementsData
            {
                CurrentGameAppId = currentGameAppId
            };

            try
            {
                // 1. Get Badges (always available)
                // We can cache this or fetch less frequently in the monitoring service, 
                // but here we just fetch it.
                var badgesResponse = await _steamApiService.GetPlayerBadgesAsync();
                if (badgesResponse?.Response != null)
                {
                    data.TotalBadgesEarned = badgesResponse.Response.Badges.Count;
                    data.TotalBadgeXP = badgesResponse.Response.PlayerXp;
                    data.PlayerLevel = badgesResponse.Response.PlayerLevel;

                    // Find latest badge
                    if (badgesResponse.Response.Badges.Any())
                    {
                        var latestBadge = badgesResponse.Response.Badges
                            .OrderByDescending(b => b.CompletionTime)
                            .First();

                        data.LatestBadgeDate = SteamApiService.FromUnixTimestamp(latestBadge.CompletionTime).DateTime;
                        // Note: Badge names require extra API calls or schema knowledge. 
                        // For now we might just show ID or generic text, or try to map common ones.
                        data.LatestBadgeName = $"Badge #{latestBadge.BadgeId}";
                    }
                }

                // 2. Get Game Achievements (only if in game)
                if (currentGameAppId > 0)
                {
                    var achResponse = await _steamApiService.GetPlayerAchievementsAsync(currentGameAppId);
                    if (achResponse?.PlayerStats != null)
                    {
                        data.CurrentGameName = achResponse.PlayerStats.GameName;
                        data.CurrentGameAchievementCount = achResponse.PlayerStats.Achievements.Count;
                        data.CurrentGameUnlockedCount = achResponse.PlayerStats.Achievements.Count(a => a.Achieved == 1);

                        if (data.CurrentGameAchievementCount > 0)
                        {
                            data.CurrentGameCompletionPercent = (double)data.CurrentGameUnlockedCount / data.CurrentGameAchievementCount * 100;
                        }

                        // Find latest unlocked achievement
                        var unlocked = achResponse.PlayerStats.Achievements
                            .Where(a => a.Achieved == 1)
                            .OrderByDescending(a => a.UnlockTime)
                            .FirstOrDefault();

                        if (unlocked != null)
                        {
                            data.LatestAchievementName = unlocked.Name; // This is the API name, display name requires schema
                            // If we want display name, we need GetSchemaForGame
                        }
                    }
                }

                _enhancedLogger?.LogDebug("AchievementsDataService", "Collected achievements data", new
                {
                    Badges = data.TotalBadgesEarned,
                    InGame = currentGameAppId > 0,
                    Achievements = data.CurrentGameUnlockedCount
                });
            }
            catch (Exception ex)
            {
                data.HasError = true;
                data.ErrorMessage = ex.Message;
                _enhancedLogger?.LogError("AchievementsDataService", "Error collecting data", ex);
            }

            return data;
        }
    }
}
