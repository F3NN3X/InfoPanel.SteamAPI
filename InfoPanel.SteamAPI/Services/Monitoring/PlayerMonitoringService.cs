using InfoPanel.SteamAPI.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Services.Monitoring
{
    /// <summary>
    /// Event arguments for player data update events
    /// </summary>
    public class PlayerDataEventArgs : EventArgs
    {
        public PlayerData Data { get; }
        public SessionDataCache SessionCache { get; }

        public PlayerDataEventArgs(PlayerData data, SessionDataCache sessionCache)
        {
            Data = data;
            SessionCache = sessionCache;
        }
    }

    /// <summary>
    /// Player domain monitoring service - handles player timer (1 second interval).
    /// Responsibilities:
    /// - Collect player data (profile, current game, session info)
    /// - Update session tracking
    /// - Manage session data cache (shared with other domains)
    /// - Fire PlayerDataUpdated events for sensor updates
    /// </summary>
    public class PlayerMonitoringService : IDisposable
    {
        #region Constants

        private const int TIMER_INTERVAL_SECONDS = 3;
        private const double TIMER_DEVIATION_TOLERANCE_SECONDS = 0.5;
        private const string DOMAIN_NAME = "PLAYER";

        #endregion

        #region Events

        /// <summary>
        /// Fired when new player data is available
        /// </summary>
        public event EventHandler<PlayerDataEventArgs>? PlayerDataUpdated;

        #endregion

        #region Fields

        private readonly ConfigurationService _configService;
        private readonly EnhancedLoggingService? _enhancedLogger;
        private readonly SemaphoreSlim _apiSemaphore;

        // Data services
        private readonly PlayerDataService _playerDataService;
        private readonly SessionTrackingService? _sessionTracker;

        // Timer
        private readonly System.Threading.Timer _timer;
        private volatile bool _isMonitoring;
        private DateTime _lastRunTime = DateTime.MinValue;

        // Cycle tracking
        private volatile int _cycleCount = 0;

        // Session data cache (shared with other domains)
        private readonly SessionDataCache _sessionCache;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new player monitoring service
        /// </summary>
        public PlayerMonitoringService(
            ConfigurationService configService,
            PlayerDataService playerDataService,
            SessionTrackingService? sessionTracker,
            SemaphoreSlim apiSemaphore,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _playerDataService = playerDataService ?? throw new ArgumentNullException(nameof(playerDataService));
            _sessionTracker = sessionTracker;
            _apiSemaphore = apiSemaphore ?? throw new ArgumentNullException(nameof(apiSemaphore));
            _enhancedLogger = enhancedLogger;

            // Initialize session cache
            _sessionCache = new SessionDataCache();

            // Initialize timer (but don't start it yet)
            _timer = new System.Threading.Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}MonitoringService", "Service initialized", new
            {
                TimerInterval = $"{TIMER_INTERVAL_SECONDS}s",
                HasSessionTracker = _sessionTracker != null
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts player data monitoring
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
            {
                _enhancedLogger?.LogWarning($"{DOMAIN_NAME}MonitoringService", "Already monitoring");
                return;
            }

            _isMonitoring = true;
            _cycleCount = 0;
            _lastRunTime = DateTime.MinValue;

            // Start timer immediately, then repeat every second
            _timer.Change(0, TIMER_INTERVAL_SECONDS * 1000);

            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}MonitoringService", "Monitoring started", new
            {
                Interval = $"{TIMER_INTERVAL_SECONDS}s",
                StartDelay = "0s"
            });
        }

        /// <summary>
        /// Stops player data monitoring
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            _isMonitoring = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}MonitoringService", "Monitoring stopped", new
            {
                TotalCycles = _cycleCount
            });
        }

        /// <summary>
        /// Gets the current session data cache (for sharing with other domains)
        /// </summary>
        public SessionDataCache GetSessionCache()
        {
            return _sessionCache.Clone();
        }

        #endregion

        #region Timer Callback

        /// <summary>
        /// Player timer callback - fires every 1 second
        /// </summary>
        private void OnTimerElapsed(object? state)
        {
            if (!_isMonitoring || _playerDataService == null)
                return;

            string? correlationId = null;
            var startTime = DateTime.UtcNow;

            try
            {
                _cycleCount++;

                // Start operation tracking
                if (_enhancedLogger != null)
                {
                    correlationId = _enhancedLogger.LogOperationStart("TIMER", $"{DOMAIN_NAME}_UPDATE", new
                    {
                        CycleCount = _cycleCount,
                        ExpectedInterval = $"{TIMER_INTERVAL_SECONDS}s"
                    });
                }

                // Timer interval verification
                var now = DateTime.Now;
                if (_lastRunTime != DateTime.MinValue)
                {
                    var actualInterval = (now - _lastRunTime).TotalSeconds;
                    var deviation = Math.Abs(actualInterval - TIMER_INTERVAL_SECONDS);

                    if (deviation > TIMER_DEVIATION_TOLERANCE_SECONDS)
                    {
                        Console.WriteLine($"[{DOMAIN_NAME}-TIMER] Timer deviation: Expected {TIMER_INTERVAL_SECONDS}s, Actual {actualInterval:F2}s, Deviation {deviation:F2}s");
                    }
                }
                _lastRunTime = now;

                // Run async collection with API rate limiting
                _ = Task.Run(async () =>
                {
                    await _apiSemaphore.WaitAsync();
                    try
                    {
                        await CollectAndUpdatePlayerDataAsync();

                        // Log successful completion
                        if (_enhancedLogger != null && correlationId != null)
                        {
                            var duration = DateTime.UtcNow - startTime;
                            _enhancedLogger.LogOperationEnd("TIMER", $"{DOMAIN_NAME}_UPDATE", correlationId, duration, true, new
                            {
                                CycleCount = _cycleCount
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DOMAIN_NAME}MonitoringService] Error in timer cycle {_cycleCount}: {ex.Message}");
                        _enhancedLogger?.LogError($"{DOMAIN_NAME}MonitoringService", $"Timer cycle {_cycleCount} failed", ex);

                        // Log operation failure
                        if (_enhancedLogger != null && correlationId != null)
                        {
                            var duration = DateTime.UtcNow - startTime;
                            _enhancedLogger.LogOperationEnd("TIMER", $"{DOMAIN_NAME}_UPDATE", correlationId, duration, false);
                        }
                    }
                    finally
                    {
                        _apiSemaphore.Release();
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}MonitoringService] Error in timer: {ex.Message}");
                _enhancedLogger?.LogError($"{DOMAIN_NAME}MonitoringService", "Timer error", ex);
            }
        }

        #endregion

        #region Data Collection

        /// <summary>
        /// Collects player data and updates session tracking and cache
        /// </summary>
        private async Task CollectAndUpdatePlayerDataAsync()
        {
            // Collect player data from API
            var playerData = await _playerDataService.CollectPlayerDataAsync();

            if (playerData == null)
            {
                _enhancedLogger?.LogWarning($"{DOMAIN_NAME}MonitoringService", "Player data collection returned null");
                return;
            }

            // Update session tracking (if available)
            if (_sessionTracker != null)
            {
                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}MonitoringService", "Updating session tracking", new
                {
                    Cycle = _cycleCount,
                    CurrentGameName = playerData.CurrentGameName,
                    CurrentGameAppId = playerData.CurrentGameAppId,
                    CurrentGameBannerUrl = playerData.CurrentGameBannerUrl
                });

                _sessionTracker.UpdateSessionTracking(new SteamData
                {
                    CurrentGameName = playerData.CurrentGameName,
                    CurrentGameAppId = playerData.CurrentGameAppId,
                    CurrentGameBannerUrl = playerData.CurrentGameBannerUrl,
                    PlayerName = playerData.PlayerName,
                    Status = $"Player cycle {_cycleCount}"
                });
            }

            // Update session cache (for sharing with other domains)
            UpdateSessionCache(playerData);

            // Fire event for sensor updates
            FirePlayerDataUpdatedEvent(playerData);
        }

        #endregion

        #region Session Cache Management

        /// <summary>
        /// Updates the session cache from player data
        /// </summary>
        private void UpdateSessionCache(PlayerData playerData)
        {
            if (playerData == null) return;

            // Use the helper method on SessionDataCache
            _sessionCache.UpdateFromPlayerData(playerData);

            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}MonitoringService", "Session cache updated", new
            {
                CachedCurrentSessionMinutes = _sessionCache.CurrentSessionMinutes,
                CachedAverageSessionMinutes = Math.Round(_sessionCache.AverageSessionMinutes, 1),
                CachedLastPlayedGame = _sessionCache.LastPlayedGameName,
                CachedBannerUrl = _sessionCache.LastPlayedGameBannerUrl != null ? "Populated" : "Null",
                CycleCount = _cycleCount
            });
        }

        #endregion

        #region Event Firing

        /// <summary>
        /// Fires the PlayerDataUpdated event with complete data
        /// </summary>
        private void FirePlayerDataUpdatedEvent(PlayerData playerData)
        {
            if (playerData == null) return;

            try
            {
                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}MonitoringService", "Firing PlayerDataUpdated event", new
                {
                    PlayerName = playerData.PlayerName,
                    CurrentGame = playerData.CurrentGameName ?? "None",
                    HasProfileUrl = playerData.ProfileImageUrl != null,
                    HasBannerUrl = playerData.CurrentGameBannerUrl != null,
                    SessionMinutes = _sessionCache.CurrentSessionMinutes,
                    AverageSessionMinutes = Math.Round(_sessionCache.AverageSessionMinutes, 1),
                    CycleCount = _cycleCount
                });

                // Fire event with player data and session cache
                PlayerDataUpdated?.Invoke(this, new PlayerDataEventArgs(playerData, _sessionCache.Clone()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}MonitoringService] Error firing PlayerDataUpdated event: {ex.Message}");
                _enhancedLogger?.LogError($"{DOMAIN_NAME}MonitoringService", "Error firing event", ex);
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed) return;

            StopMonitoring();
            _timer?.Dispose();

            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}MonitoringService", "Service disposed", new
            {
                TotalCycles = _cycleCount
            });

            _disposed = true;
        }

        #endregion
    }
}
