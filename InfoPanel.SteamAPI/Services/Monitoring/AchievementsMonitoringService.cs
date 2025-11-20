using InfoPanel.SteamAPI.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Services.Monitoring
{
    public class AchievementsDataEventArgs : EventArgs
    {
        public AchievementsData Data { get; }

        public AchievementsDataEventArgs(AchievementsData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Achievements domain monitoring service
    /// Updates less frequently (e.g. 60s) or when game state changes
    /// </summary>
    public class AchievementsMonitoringService : IDisposable
    {
        private const int TIMER_INTERVAL_SECONDS = 60;
        private const string DOMAIN_NAME = "ACHIEVEMENTS";

        public event EventHandler<AchievementsDataEventArgs>? AchievementsDataUpdated;

        private readonly ConfigurationService _configService;
        private readonly AchievementsDataService _dataService;
        private readonly SessionTrackingService? _sessionTrackingService;
        private readonly System.Threading.SemaphoreSlim _apiSemaphore;
        private readonly EnhancedLoggingService? _enhancedLogger;

        private System.Threading.Timer? _timer;
        private bool _isMonitoring;
        private int _currentGameAppId;
        private int _lastPlayedAppId;

        public AchievementsMonitoringService(
            ConfigurationService configService,
            AchievementsDataService dataService,
            SessionTrackingService? sessionTrackingService,
            System.Threading.SemaphoreSlim apiSemaphore,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _sessionTrackingService = sessionTrackingService;
            _apiSemaphore = apiSemaphore ?? throw new ArgumentNullException(nameof(apiSemaphore));
            _enhancedLogger = enhancedLogger;
        }

        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;

            // Initialize last played game from session history if available
            // This avoids an API call on startup and provides immediate data
            if (_sessionTrackingService != null)
            {
                var lastGame = _sessionTrackingService.GetLastPlayedGame();
                if (lastGame.appId > 0)
                {
                    _lastPlayedAppId = lastGame.appId;
                    _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.StartMonitoring", $"Initialized last played game from session history: {_lastPlayedAppId}");
                }
            }

            _timer = new System.Threading.Timer(async _ => await UpdateLoopAsync(), null, 0, TIMER_INTERVAL_SECONDS * 1000);

            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.StartMonitoring", "Achievements monitoring started");
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.StopMonitoring", "Achievements monitoring stopped");
        }

        /// <summary>
        /// Updates the current game AppID. Called by PlayerMonitoringService or Main.
        /// Triggers an immediate update if the game has changed.
        /// </summary>
        public void UpdateCurrentGame(int appId)
        {
            if (_currentGameAppId != appId)
            {
                _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.UpdateCurrentGame", $"Game changed from {_currentGameAppId} to {appId}. Triggering immediate update.");

                _currentGameAppId = appId;

                if (appId > 0)
                {
                    _lastPlayedAppId = appId;
                }

                // Trigger immediate update on game change by resetting the timer
                // This ensures we don't have concurrent updates and realigns the interval
                // We use a small delay (250ms) to allow PlayerMonitoringService to release the API semaphore
                _timer?.Change(250, TIMER_INTERVAL_SECONDS * 1000);
            }
        }

        private async Task UpdateLoopAsync()
        {
            if (!_isMonitoring) return;

            try
            {
                await _apiSemaphore.WaitAsync();
                try
                {
                    // If we don't have a current game, and haven't found a last played game yet, try to find one
                    if (_currentGameAppId <= 0 && _lastPlayedAppId <= 0)
                    {
                        try
                        {
                            var recentGames = await _dataService.GetRecentlyPlayedGamesAsync();
                            if (recentGames?.Response?.Games?.Any() == true)
                            {
                                _lastPlayedAppId = recentGames.Response.Games.First().AppId;
                                _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.UpdateLoopAsync", $"Found last played game: {_lastPlayedAppId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _enhancedLogger?.LogWarning($"{DOMAIN_NAME}.UpdateLoopAsync", "Failed to fetch recent games", ex);
                        }
                    }

                    int targetAppId = _currentGameAppId > 0 ? _currentGameAppId : _lastPlayedAppId;
                    var data = await _dataService.CollectAchievementsDataAsync(targetAppId);

                    // Mark if this is live data or historical
                    data.IsLive = _currentGameAppId > 0;

                    AchievementsDataUpdated?.Invoke(this, new AchievementsDataEventArgs(data));
                }
                finally
                {
                    _apiSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError($"{DOMAIN_NAME}.UpdateLoopAsync", "Error in monitoring loop", ex);
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            _timer?.Dispose();
        }
    }
}
