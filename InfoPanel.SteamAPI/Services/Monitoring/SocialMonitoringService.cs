using System;
using System.Linq;
using System.Threading;
using InfoPanel.SteamAPI.Models;

namespace InfoPanel.SteamAPI.Services.Monitoring
{
    /// <summary>
    /// Event arguments for social data updates
    /// </summary>
    public class SocialDataEventArgs : EventArgs
    {
        public SocialData SocialData { get; }
        public SessionDataCache SessionCache { get; }

        public SocialDataEventArgs(SocialData socialData, SessionDataCache sessionCache)
        {
            SocialData = socialData ?? throw new ArgumentNullException(nameof(socialData));
            SessionCache = sessionCache ?? throw new ArgumentNullException(nameof(sessionCache));
        }
    }

    /// <summary>
    /// Social Domain Monitoring Service
    /// Handles timer, data collection for friends/social data
    /// Updates occur every 15 seconds with 2-second start delay
    /// READS session cache from player domain to preserve session sensors
    /// </summary>
    public class SocialMonitoringService : IDisposable
    {
        // Constants
        private const int TIMER_INTERVAL_SECONDS = 15;
        private const double TIMER_DEVIATION_TOLERANCE_SECONDS = 1.0;  // 1 second tolerance for 15s timer
        private const int START_DELAY_SECONDS = 2;  // Delay before first social update
        private const string DOMAIN_NAME = "SOCIAL";

        // Events
        public event EventHandler<SocialDataEventArgs>? SocialDataUpdated;

        // Configuration and services
        private readonly ConfigurationService _configService;
        private readonly SocialDataService _socialDataService;
        private readonly SemaphoreSlim _apiSemaphore;  // Shared semaphore for API rate limiting
        private readonly EnhancedLoggingService? _enhancedLogger;

        // Session cache (READ-ONLY - owned by player domain)
        private SessionDataCache _sessionCache;

        // Timer management
        private System.Threading.Timer? _timer;
        private DateTime _lastTimerRun = DateTime.MinValue;
        private int _cycleCount = 0;

        // State management
        private bool _isMonitoring = false;
        private bool _disposed = false;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public SocialMonitoringService(
            ConfigurationService configService,
            SocialDataService socialDataService,
            SemaphoreSlim apiSemaphore,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _socialDataService = socialDataService ?? throw new ArgumentNullException(nameof(socialDataService));
            _apiSemaphore = apiSemaphore ?? throw new ArgumentNullException(nameof(apiSemaphore));
            _enhancedLogger = enhancedLogger;

            // Initialize empty session cache (will be set by main plugin)
            _sessionCache = new SessionDataCache();

            Console.WriteLine($"[{DOMAIN_NAME}] SocialMonitoringService initialized");
        }

        /// <summary>
        /// Set the session cache reference (called by main plugin after player domain creates it)
        /// </summary>
        public void SetSessionCache(SessionDataCache sessionCache)
        {
            if (sessionCache == null)
                throw new ArgumentNullException(nameof(sessionCache));

            _sessionCache = sessionCache;
            
            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.SetSessionCache", "Session cache reference set", new
            {
                CacheLastUpdated = _sessionCache.LastUpdated
            });
        }

        /// <summary>
        /// Start social monitoring with 15-second timer
        /// Starts with 2-second delay to allow player domain to initialize
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Social monitoring already running");
                return;
            }

            _isMonitoring = true;
            _cycleCount = 0;
            _lastTimerRun = DateTime.MinValue;

            // Start timer with initial delay
            var initialDelay = TimeSpan.FromSeconds(START_DELAY_SECONDS);
            var interval = TimeSpan.FromSeconds(TIMER_INTERVAL_SECONDS);
            _timer = new System.Threading.Timer(OnTimerElapsed, null, initialDelay, interval);

            Console.WriteLine($"[{DOMAIN_NAME}] Social monitoring started (interval: {TIMER_INTERVAL_SECONDS}s, delay: {START_DELAY_SECONDS}s)");
            
            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.StartMonitoring", "Social monitoring started", new
            {
                TimerInterval = TIMER_INTERVAL_SECONDS,
                StartDelay = START_DELAY_SECONDS
            });
        }

        /// <summary>
        /// Stop social monitoring
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Social monitoring not running");
                return;
            }

            _isMonitoring = false;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);

            Console.WriteLine($"[{DOMAIN_NAME}] Social monitoring stopped");
            
            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.StopMonitoring", "Social monitoring stopped", new
            {
                TotalCycles = _cycleCount
            });
        }

        /// <summary>
        /// Timer callback - fires every 15 seconds
        /// </summary>
        private void OnTimerElapsed(object? state)
        {
            if (!_isMonitoring || _socialDataService == null)
                return;

            var startTime = DateTime.UtcNow;
            string? correlationId = null;

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
                if (_lastTimerRun != DateTime.MinValue)
                {
                    var actualInterval = (now - _lastTimerRun).TotalSeconds;
                    var expectedInterval = TIMER_INTERVAL_SECONDS;
                    var deviation = Math.Abs(actualInterval - expectedInterval);

                    if (deviation > TIMER_DEVIATION_TOLERANCE_SECONDS)
                    {
                        Console.WriteLine($"[{DOMAIN_NAME}] Timer deviation: Expected {expectedInterval}s, Actual {actualInterval:F2}s, Deviation {deviation:F2}s");
                        
                        _enhancedLogger?.LogWarning($"{DOMAIN_NAME}.TimerElapsed", "Timer deviation detected", new
                        {
                            ExpectedInterval = expectedInterval,
                            ActualInterval = Math.Round(actualInterval, 2),
                            Deviation = Math.Round(deviation, 2)
                        });
                    }
                }
                _lastTimerRun = now;

                Console.WriteLine($"[{DOMAIN_NAME}] Timer cycle {_cycleCount} at {now:HH:mm:ss.fff}");

                // Run data collection on background thread
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await _apiSemaphore.WaitAsync();
                    try
                    {
                        await CollectAndUpdateSocialDataAsync(startTime, correlationId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DOMAIN_NAME}] Error in timer cycle {_cycleCount}: {ex.Message}");
                        
                        _enhancedLogger?.LogError($"{DOMAIN_NAME}.TimerElapsed", $"Error in cycle {_cycleCount}", ex, new
                        {
                            CycleCount = _cycleCount
                        });
                    }
                    finally
                    {
                        _apiSemaphore.Release();
                    }
                });
            }
            catch (Exception ex)
            {
                // Log operation failure
                if (_enhancedLogger != null && correlationId != null)
                {
                    var duration = DateTime.UtcNow - startTime;
                    _enhancedLogger.LogOperationEnd("TIMER", $"{DOMAIN_NAME}_UPDATE", correlationId, duration, false);
                    _enhancedLogger.LogError("TIMER", $"{DOMAIN_NAME} timer error in cycle {_cycleCount}", ex, new
                    {
                        CycleCount = _cycleCount
                    });
                }
                else
                {
                    Console.WriteLine($"[{DOMAIN_NAME}] Error in timer: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Collect social data and fire update event
        /// </summary>
        private async System.Threading.Tasks.Task CollectAndUpdateSocialDataAsync(DateTime startTime, string? correlationId)
        {
            try
            {
                // Collect social data from Steam API
                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.CollectData", "Collecting social data", new
                {
                    CycleCount = _cycleCount
                });

                var socialData = await _socialDataService.CollectSocialDataAsync();

                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.CollectData", "Social data collected", new
                {
                    FriendsOnline = socialData.FriendsOnline,
                    FriendsInGame = socialData.FriendsInGame,
                    TotalFriends = socialData.TotalFriends,
                    FriendsActivityCount = socialData.FriendsActivity?.Count ?? 0
                });

                // Fire update event with social data + session cache snapshot
                FireSocialDataUpdatedEvent(socialData);

                // Log completion
                if (_enhancedLogger != null && correlationId != null)
                {
                    var duration = DateTime.UtcNow - startTime;
                    _enhancedLogger.LogOperationEnd("TIMER", $"{DOMAIN_NAME}_UPDATE", correlationId, duration, true, new
                    {
                        FriendsOnline = socialData.FriendsOnline,
                        FriendsInGame = socialData.FriendsInGame,
                        TotalFriends = socialData.TotalFriends,
                        CycleCount = _cycleCount
                    });
                }
                else
                {
                    Console.WriteLine($"[{DOMAIN_NAME}] Cycle {_cycleCount}: {socialData.FriendsOnline} friends online, {socialData.FriendsInGame} in game");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Error collecting social data: {ex.Message}");
                
                _enhancedLogger?.LogError($"{DOMAIN_NAME}.CollectData", "Failed to collect social data", ex, new
                {
                    CycleCount = _cycleCount
                });

                throw; // Re-throw to be caught by timer callback
            }
        }

        /// <summary>
        /// Fire SocialDataUpdated event with social data and session cache snapshot
        /// </summary>
        private void FireSocialDataUpdatedEvent(SocialData socialData)
        {
            try
            {
                // Get thread-safe snapshot of session cache
                SessionDataCache cacheSnapshot;
                lock (_sessionCache.Lock)
                {
                    cacheSnapshot = _sessionCache.Clone();
                }

                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.FireEvent", "Preparing to fire SocialDataUpdated event", new
                {
                    CycleCount = _cycleCount,
                    SessionCacheLastUpdated = cacheSnapshot.LastUpdated,
                    SessionCacheCurrentMinutes = cacheSnapshot.CurrentSessionMinutes,
                    SessionCacheAvgMinutes = Math.Round(cacheSnapshot.AverageSessionMinutes, 1)
                });

                // Fire event
                var eventArgs = new SocialDataEventArgs(socialData, cacheSnapshot);
                SocialDataUpdated?.Invoke(this, eventArgs);

                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.FireEvent", "SocialDataUpdated event fired", new
                {
                    CycleCount = _cycleCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Error firing SocialDataUpdated event: {ex.Message}");
                
                _enhancedLogger?.LogError($"{DOMAIN_NAME}.FireEvent", "Failed to fire event", ex, new
                {
                    CycleCount = _cycleCount
                });
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                StopMonitoring();
                _timer?.Dispose();
                _timer = null;

                Console.WriteLine($"[{DOMAIN_NAME}] SocialMonitoringService disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Error during disposal: {ex.Message}");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
