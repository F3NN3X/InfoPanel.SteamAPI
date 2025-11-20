using InfoPanel.SteamAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Services
{
    /// <summary>
    /// Service responsible for collecting game news data
    /// Handles fetching news for specific games and aggregating news for library
    /// </summary>
    public class NewsDataService
    {
        private readonly SteamApiService _steamApiService;
        private readonly EnhancedLoggingService? _enhancedLogger;

        public NewsDataService(
            SteamApiService steamApiService,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _steamApiService = steamApiService ?? throw new ArgumentNullException(nameof(steamApiService));
            _enhancedLogger = enhancedLogger;
        }

        /// <summary>
        /// Gets the latest news for a specific game
        /// </summary>
        public async Task<NewsItem?> GetLatestNewsForGameAsync(int appId)
        {
            if (appId <= 0) return null;

            try
            {
                // Fetch just 1 item for the "latest news" check
                var response = await _steamApiService.GetGameNewsAsync(appId, count: 1, maxLength: 300);
                return response?.AppNews?.NewsItems?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("NewsDataService.GetLatestNewsForGameAsync", $"Error fetching news for app {appId}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets news for a list of games (e.g. for the library feed)
        /// </summary>
        public async Task<List<(int AppId, string GameName, NewsItem News)>> GetNewsForGamesAsync(IEnumerable<(int AppId, string GameName)> games, int countPerGame = 1)
        {
            var results = new List<(int AppId, string GameName, NewsItem News)>();

            foreach (var game in games)
            {
                if (game.AppId <= 0) continue;

                try
                {
                    var response = await _steamApiService.GetGameNewsAsync(game.AppId, count: countPerGame, maxLength: 300);
                    var newsItems = response?.AppNews?.NewsItems;

                    if (newsItems != null && newsItems.Any())
                    {
                        foreach (var item in newsItems)
                        {
                            results.Add((game.AppId, game.GameName, item));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _enhancedLogger?.LogError("NewsDataService.GetNewsForGamesAsync", $"Error fetching news for app {game.AppId}", ex);
                }
            }

            return results.OrderByDescending(x => x.News.Date).ToList();
        }
    }
}
