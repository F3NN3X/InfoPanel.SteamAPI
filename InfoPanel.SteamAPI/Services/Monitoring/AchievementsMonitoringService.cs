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
        private readonly System.Threading.SemaphoreSlim _apiSemaphore;
        private readonly EnhancedLoggingService? _enhancedLogger;

        private System.Threading.Timer? _timer;
        private bool _isMonitoring;
        private int _currentGameAppId;

        public AchievementsMonitoringService(
            ConfigurationService configService,
            AchievementsDataService dataService,
            System.Threading.SemaphoreSlim apiSemaphore,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _apiSemaphore = apiSemaphore ?? throw new ArgumentNullException(nameof(apiSemaphore));
            _enhancedLogger = enhancedLogger;
        }

        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
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
                _currentGameAppId = appId;
                // Trigger immediate update on game change
                Task.Run(() => UpdateLoopAsync());
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
                    var data = await _dataService.CollectAchievementsDataAsync(_currentGameAppId);
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
