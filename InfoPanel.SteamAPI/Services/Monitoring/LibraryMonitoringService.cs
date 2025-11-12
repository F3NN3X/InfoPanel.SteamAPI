using System;
using System.Threading;
using InfoPanel.SteamAPI.Models;

namespace InfoPanel.SteamAPI.Services.Monitoring
{
    /// <summary>
    /// Event arguments for library data updates
    /// </summary>
    public class LibraryDataEventArgs : EventArgs
    {
        public LibraryData? LibraryData { get; }
        public SessionDataCache SessionCache { get; }

        public LibraryDataEventArgs(LibraryData? libraryData, SessionDataCache sessionCache)
        {
            LibraryData = libraryData; // Can be null if no library data available
            SessionCache = sessionCache ?? throw new ArgumentNullException(nameof(sessionCache));
        }
    }

    /// <summary>
    /// Library Domain Monitoring Service
    /// Handles timer, data collection for games library and playtime stats
    /// Updates occur every 45 seconds with 5-second start delay
    /// READS session cache from player domain to preserve session sensors
    /// </summary>
    public class LibraryMonitoringService : IDisposable
    {
        // Constants
        private const int TIMER_INTERVAL_SECONDS = 45;
        private const double TIMER_DEVIATION_TOLERANCE_SECONDS = 2.0;  // 2 second tolerance for 45s timer
        private const int START_DELAY_SECONDS = 5;  // Delay before first library update
        private const string DOMAIN_NAME = "LIBRARY";

        // Events
        public event EventHandler<LibraryDataEventArgs>? LibraryDataUpdated;

        // Configuration and services
        private readonly ConfigurationService _configService;
        private readonly LibraryDataService _libraryDataService;
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
        public LibraryMonitoringService(
            ConfigurationService configService,
            LibraryDataService libraryDataService,
            SemaphoreSlim apiSemaphore,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _libraryDataService = libraryDataService ?? throw new ArgumentNullException(nameof(libraryDataService));
            _apiSemaphore = apiSemaphore ?? throw new ArgumentNullException(nameof(apiSemaphore));
            _enhancedLogger = enhancedLogger;

            // Initialize empty session cache (will be set by main plugin)
            _sessionCache = new SessionDataCache();

            Console.WriteLine($"[{DOMAIN_NAME}] LibraryMonitoringService initialized");
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
        /// Start library monitoring with 45-second timer
        /// Starts with 5-second delay to allow player/social domains to initialize
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Library monitoring already running");
                return;
            }

            _isMonitoring = true;
            _cycleCount = 0;
            _lastTimerRun = DateTime.MinValue;

            // Start timer with initial delay
            var initialDelay = TimeSpan.FromSeconds(START_DELAY_SECONDS);
            var interval = TimeSpan.FromSeconds(TIMER_INTERVAL_SECONDS);
            _timer = new System.Threading.Timer(OnTimerElapsed, null, initialDelay, interval);

            Console.WriteLine($"[{DOMAIN_NAME}] Library monitoring started (interval: {TIMER_INTERVAL_SECONDS}s, delay: {START_DELAY_SECONDS}s)");
            
            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.StartMonitoring", "Library monitoring started", new
            {
                TimerInterval = TIMER_INTERVAL_SECONDS,
                StartDelay = START_DELAY_SECONDS
            });
        }

        /// <summary>
        /// Stop library monitoring
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Library monitoring not running");
                return;
            }

            _isMonitoring = false;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);

            Console.WriteLine($"[{DOMAIN_NAME}] Library monitoring stopped");
            
            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.StopMonitoring", "Library monitoring stopped", new
            {
                TotalCycles = _cycleCount
            });
        }

        /// <summary>
        /// Timer callback - fires every 45 seconds
        /// </summary>
        private void OnTimerElapsed(object? state)
        {
            if (!_isMonitoring || _libraryDataService == null)
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
                        await CollectAndUpdateLibraryDataAsync(startTime, correlationId);
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
        /// Collect library data and fire update event
        /// </summary>
        private async System.Threading.Tasks.Task CollectAndUpdateLibraryDataAsync(DateTime startTime, string? correlationId)
        {
            try
            {
                // Collect library data from Steam API
                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.CollectData", "Collecting library data", new
                {
                    CycleCount = _cycleCount
                });

                var libraryData = await _libraryDataService.CollectLibraryDataAsync();

                if (libraryData != null)
                {
                    _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.CollectData", "Library data collected", new
                    {
                        TotalGamesOwned = libraryData.TotalGamesOwned,
                        TotalLibraryPlaytimeHours = libraryData.TotalLibraryPlaytimeHours,
                        RecentGamesCount = libraryData.RecentGamesCount,
                        RecentGames = libraryData.RecentGames?.Count ?? 0
                    });
                }
                else
                {
                    _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.CollectData", "No library data available", new
                    {
                        CycleCount = _cycleCount
                    });
                }

                // Fire update event with library data + session cache snapshot
                FireLibraryDataUpdatedEvent(libraryData);

                // Log completion
                if (_enhancedLogger != null && correlationId != null)
                {
                    var duration = DateTime.UtcNow - startTime;
                    _enhancedLogger.LogOperationEnd("TIMER", $"{DOMAIN_NAME}_UPDATE", correlationId, duration, true, new
                    {
                        TotalGamesOwned = libraryData?.TotalGamesOwned ?? 0,
                        TotalLibraryPlaytimeHours = libraryData?.TotalLibraryPlaytimeHours ?? 0,
                        RecentGamesCount = libraryData?.RecentGamesCount ?? 0,
                        CycleCount = _cycleCount
                    });
                }
                else
                {
                    Console.WriteLine($"[{DOMAIN_NAME}] Cycle {_cycleCount}: {libraryData?.TotalGamesOwned ?? 0} games, {libraryData?.TotalLibraryPlaytimeHours ?? 0:F1}h total");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Error collecting library data: {ex.Message}");
                
                _enhancedLogger?.LogError($"{DOMAIN_NAME}.CollectData", "Failed to collect library data", ex, new
                {
                    CycleCount = _cycleCount
                });

                throw; // Re-throw to be caught by timer callback
            }
        }

        /// <summary>
        /// Fire LibraryDataUpdated event with library data and session cache snapshot
        /// </summary>
        private void FireLibraryDataUpdatedEvent(LibraryData? libraryData)
        {
            try
            {
                // Get thread-safe snapshot of session cache
                SessionDataCache cacheSnapshot;
                lock (_sessionCache.Lock)
                {
                    cacheSnapshot = _sessionCache.Clone();
                }

                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.FireEvent", "Preparing to fire LibraryDataUpdated event", new
                {
                    CycleCount = _cycleCount,
                    SessionCacheLastUpdated = cacheSnapshot.LastUpdated,
                    SessionCacheCurrentMinutes = cacheSnapshot.CurrentSessionMinutes,
                    SessionCacheAvgMinutes = Math.Round(cacheSnapshot.AverageSessionMinutes, 1),
                    HasLibraryData = libraryData != null
                });

                // Fire event
                var eventArgs = new LibraryDataEventArgs(libraryData, cacheSnapshot);
                LibraryDataUpdated?.Invoke(this, eventArgs);

                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.FireEvent", "LibraryDataUpdated event fired", new
                {
                    CycleCount = _cycleCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Error firing LibraryDataUpdated event: {ex.Message}");
                
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

                Console.WriteLine($"[{DOMAIN_NAME}] LibraryMonitoringService disposed");
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
