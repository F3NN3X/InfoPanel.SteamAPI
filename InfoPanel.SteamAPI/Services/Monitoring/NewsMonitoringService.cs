using InfoPanel.SteamAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Services.Monitoring
{
    public class NewsDataEventArgs : EventArgs
    {
        public NewsData Data { get; }
        public NewsDataEventArgs(NewsData data) { Data = data; }
    }

    public class NewsMonitoringService : IDisposable
    {
        private const string DOMAIN_NAME = "NEWS";
        private const int LIBRARY_NEWS_UPDATE_INTERVAL_MINUTES = 15;

        public event EventHandler<NewsDataEventArgs>? NewsDataUpdated;

        private readonly NewsDataService _newsDataService;
        private readonly EnhancedLoggingService? _enhancedLogger;

        // State
        private readonly NewsData _currentData = new();
        private int _lastKnownGameId = 0;
        private List<(int AppId, string GameName)> _lastKnownRecentGames = new();

        // Timer for library news
        private readonly System.Threading.Timer _libraryNewsTimer;

        public NewsMonitoringService(
            NewsDataService newsDataService,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _newsDataService = newsDataService ?? throw new ArgumentNullException(nameof(newsDataService));
            _enhancedLogger = enhancedLogger;

            // Timer for periodic library news updates
            _libraryNewsTimer = new System.Threading.Timer(OnLibraryNewsTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartMonitoring()
        {
            // Start the library news timer
            _libraryNewsTimer.Change(0, LIBRARY_NEWS_UPDATE_INTERVAL_MINUTES * 60 * 1000);
            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}MonitoringService", "Service started");
        }

        public void StopMonitoring()
        {
            _libraryNewsTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}MonitoringService", "Service stopped");
        }

        // Event Handler for Player Data (Current Game)
        public async void OnPlayerDataUpdated(object? sender, PlayerDataEventArgs e)
        {
            try
            {
                // Determine which game to show news for:
                // 1. Current Game (if playing)
                // 2. Last Played Game (if not playing)
                int targetGameId = e.Data.CurrentGameAppId > 0
                    ? e.Data.CurrentGameAppId
                    : e.Data.LastPlayedGameAppId;

                // If the target game changed, OR if we have a target game but no news yet (initial load)
                if (targetGameId != _lastKnownGameId || (targetGameId > 0 && _currentData.CurrentGameNews == null))
                {
                    _lastKnownGameId = targetGameId;

                    if (targetGameId > 0)
                    {
                        var news = await _newsDataService.GetLatestNewsForGameAsync(targetGameId);
                        _currentData.CurrentGameNews = news;

                        _enhancedLogger?.LogDebug($"{DOMAIN_NAME}MonitoringService", "Updated current game news", new
                        {
                            AppId = targetGameId,
                            IsCurrentGame = e.Data.CurrentGameAppId > 0,
                            HasNews = news != null
                        });
                    }
                    else
                    {
                        // No current game AND no last played game? Clear it.
                        _currentData.CurrentGameNews = null;
                    }

                    _currentData.Timestamp = DateTime.Now;
                    NewsDataUpdated?.Invoke(this, new NewsDataEventArgs(_currentData));
                }
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError($"{DOMAIN_NAME}MonitoringService.OnPlayerDataUpdated", "Error processing player update", ex);
            }
        }

        // Event Handler for Library Data (Recent Games List)
        public void OnLibraryDataUpdated(object? sender, LibraryDataEventArgs e)
        {
            if (e.LibraryData?.RecentGames != null)
            {
                // Update our list of games to watch, but don't fetch immediately (let the timer do it)
                // unless we have no data yet.
                var newRecentGames = e.LibraryData.RecentGames
                    .Take(5)
                    .Select(g => (g.AppId, g.Name))
                    .ToList();

                bool isDifferent = _lastKnownRecentGames.Count != newRecentGames.Count ||
                                   !_lastKnownRecentGames.Select(x => x.AppId).SequenceEqual(newRecentGames.Select(x => x.AppId));

                _lastKnownRecentGames = newRecentGames;

                if (isDifferent && _currentData.LibraryNews.Count == 0)
                {
                    // Initial fetch
                    _ = UpdateLibraryNewsAsync();
                }
            }
        }

        private async void OnLibraryNewsTimerElapsed(object? state)
        {
            await UpdateLibraryNewsAsync();
        }

        private async Task UpdateLibraryNewsAsync()
        {
            if (_lastKnownRecentGames.Count == 0) return;

            try
            {
                var news = await _newsDataService.GetNewsForGamesAsync(_lastKnownRecentGames);
                _currentData.LibraryNews = news;
                _currentData.Timestamp = DateTime.Now;
                NewsDataUpdated?.Invoke(this, new NewsDataEventArgs(_currentData));
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError($"{DOMAIN_NAME}MonitoringService.UpdateLibraryNewsAsync", "Error updating library news", ex);
            }
        }

        public void Dispose()
        {
            _libraryNewsTimer?.Dispose();
        }
    }
}
