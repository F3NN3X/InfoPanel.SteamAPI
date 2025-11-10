using InfoPanel.SteamAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Services
{
    /// <summary>
    /// Constants for monitoring service timing and configuration
    /// </summary>
    public static class MonitoringConstants
    {
        #region Timer Configuration
        /// <summary>Initial cycle count for timer tracking</summary>
        public const int INITIAL_CYCLE_COUNT = 0;
        
        /// <summary>Expected player timer interval in seconds</summary>
        public const int PLAYER_TIMER_INTERVAL_SECONDS = 1;
        
        /// <summary>Expected social timer interval in seconds</summary>
        public const int SOCIAL_TIMER_INTERVAL_SECONDS = 15;
        
        /// <summary>Expected library timer interval in seconds</summary>
        public const int LIBRARY_TIMER_INTERVAL_SECONDS = 45;
        #endregion
        
        #region Logging Messages
        /// <summary>Service name prefix for consistent logging</summary>
        public const string SERVICE_NAME = "MonitoringService";
        
        /// <summary>Log message for already monitoring state</summary>
        public const string MSG_ALREADY_MONITORING = "Already monitoring";
        
        /// <summary>Log message for monitoring cancelled</summary>
        public const string MSG_MONITORING_CANCELLED = "Steam monitoring cancelled";
        
        /// <summary>Log message for monitoring stopped</summary>
        public const string MSG_MONITORING_STOPPED = "Multi-timer monitoring stopped";
        
        /// <summary>Log message for service disposal</summary>
        public const string MSG_SERVICE_DISPOSED = "Multi-timer monitoring service disposed";
        #endregion
    }

    /// <summary>
    /// Event arguments for Steam data update events
    /// </summary>
    public class DataUpdatedEventArgs : EventArgs
    {
        public SteamData Data { get; }
        
        public DataUpdatedEventArgs(SteamData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Multi-timer monitoring service for Steam data collection
    /// NO DATA MERGING - Each timer updates only its own sensors
    /// </summary>
    public class MonitoringService : IDisposable
    {
        #region Events
        
        /// <summary>
        /// Triggered when new Steam data is available
        /// </summary>
        public event EventHandler<DataUpdatedEventArgs>? DataUpdated;
        
        #endregion

        #region Fields
        
        private readonly ConfigurationService _configService;
        private readonly FileLoggingService? _logger;
        private readonly SensorManagementService? _sensorService;
        
        // Multi-timer architecture - each timer owns specific data types
        private readonly System.Threading.Timer _playerTimer;   // 1 second - Game state, sessions, profile
        private readonly System.Threading.Timer _socialTimer;   // 15 seconds - Friends status
        private readonly System.Threading.Timer _libraryTimer;  // 45 seconds - Games owned, playtime
        
        // API rate limiting - only one Steam API call at a time
        private readonly SemaphoreSlim _apiSemaphore = new(1, 1);
        
        // Core Steam API service
        private SteamApiService? _steamApiService;
        private SessionTrackingService? _sessionTracker;
        
        // Specialized data collection services
        private PlayerDataService? _playerDataService;
        private SocialDataService? _socialDataService;
        private LibraryDataService? _libraryDataService;
        private GameStatsService? _gameStatsService;
        
        private volatile bool _isMonitoring;
        private readonly object _lockObject = new();
        
        // Timer timing verification
        private DateTime _playerTimerLastRun = DateTime.MinValue;
        private DateTime _socialTimerLastRun = DateTime.MinValue;
        private DateTime _libraryTimerLastRun = DateTime.MinValue;
        
        // Cycle tracking for each timer
        private volatile int _playerCycleCount = MonitoringConstants.INITIAL_CYCLE_COUNT;
        private volatile int _socialCycleCount = MonitoringConstants.INITIAL_CYCLE_COUNT;
        private volatile int _libraryCycleCount = MonitoringConstants.INITIAL_CYCLE_COUNT;
        
        #endregion

        #region Constructor
        
        public MonitoringService(ConfigurationService configService, SensorManagementService? sensorService = null, FileLoggingService? logger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _sensorService = sensorService;
            _logger = logger;
            
            // Initialize multi-timer architecture (but don't start them yet)
            _playerTimer = new System.Threading.Timer(OnPlayerTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _socialTimer = new System.Threading.Timer(OnSocialTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _libraryTimer = new System.Threading.Timer(OnLibraryTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            
            // Initialize session tracking service
            _sessionTracker = new SessionTrackingService(_logger);
            
            _logger?.LogInfo("MonitoringService initialized with multi-timer architecture (1s/15s/45s)");
            Console.WriteLine("[MonitoringService] Multi-timer architecture initialized - Player:1s, Social:15s, Library:45s");
        }
        
        #endregion

        #region Monitoring Control
        
        /// <summary>
        /// Starts the Steam monitoring process with multi-timer architecture
        /// </summary>
        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                if (_isMonitoring)
                {
                    Console.WriteLine("[MonitoringService] Already monitoring");
                    return;
                }
                _isMonitoring = true;
            }
            
            try
            {
                // Initialize Steam API service
                await InitializeSteamApiAsync();
                
                // STEP 1: Collect initial data to populate all sensors immediately
                Console.WriteLine("[MonitoringService] Collecting initial data to populate sensors...");
                await CollectInitialDataAsync();
                
                // STEP 2: Start multi-timer architecture for ongoing updates
                Console.WriteLine("[MonitoringService] Starting multi-timer monitoring: Player=1s, Social=15s, Library=45s");
                _logger?.LogInfo("Starting multi-timer monitoring: Player=1s, Social=15s, Library=45s");
                
                // Start timers with staggered offsets to spread API load
                _playerTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));        // Start immediately, repeat every 1s
                _socialTimer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(15));   // Start after 2s, repeat every 15s  
                _libraryTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(45));  // Start after 5s, repeat every 45s
                
                // Keep task alive while monitoring
                while (_isMonitoring && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[MonitoringService] Monitoring cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error during monitoring: {ex.Message}");
                throw;
            }
            finally
            {
                StopMonitoring();
            }
        }
        
        /// <summary>
        /// Stops the Steam monitoring process
        /// </summary>
        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring)
                {
                    return;
                }
                
                _isMonitoring = false;
            }
            
            // Stop all timers
            _playerTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _socialTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _libraryTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Dispose Steam API service
            _steamApiService?.Dispose();
            _steamApiService = null;
            
            Console.WriteLine("[MonitoringService] Multi-timer monitoring stopped");
        }
        
        #endregion

        #region Steam API Management
        
        /// <summary>
        /// Initializes the Steam API service with configuration
        /// </summary>
        private async Task InitializeSteamApiAsync()
        {
            try
            {
                // Initialize Steam API service with configuration
                var steamId = _configService.SteamId64;
                var apiKey = _configService.SteamApiKey;
                
                if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("Steam ID and API Key must be configured");
                }
                
                _steamApiService = new SteamApiService(apiKey, steamId, _logger);
                
                Console.WriteLine("[MonitoringService] Testing Steam API connection...");
                await _steamApiService.TestConnectionAsync();
                Console.WriteLine("[MonitoringService] Steam API connection successful");
                
                // Initialize specialized data collection services
                InitializeSpecializedServices();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Failed to initialize Steam API: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Initializes the specialized data collection services
        /// </summary>
        private void InitializeSpecializedServices()
        {
            try
            {
                if (_steamApiService == null)
                {
                    throw new InvalidOperationException("Steam API service must be initialized first");
                }
                
                // Initialize specialized services
                _playerDataService = new PlayerDataService(_configService, _steamApiService, _sessionTracker, _logger);
                _socialDataService = new SocialDataService(_configService, _steamApiService, _logger);
                _libraryDataService = new LibraryDataService(_configService, _steamApiService, _logger);
                _gameStatsService = new GameStatsService(_configService, _steamApiService, _logger);
                
                Console.WriteLine("[MonitoringService] Specialized services initialized");
                _logger?.LogInfo("Specialized data collection services initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Failed to initialize specialized services: {ex.Message}");
                _logger?.LogError("Failed to initialize specialized services", ex);
                throw;
            }
        }
        
        #endregion

        #region Initial Data Collection

        /// <summary>
        /// Collects initial data from all services to populate sensors immediately on startup
        /// Uses API semaphore for proper rate limiting instead of simple delays
        /// </summary>
        private async Task CollectInitialDataAsync()
        {
            try
            {
                Console.WriteLine("[MonitoringService] Collecting initial data sequentially with API rate limiting...");

                // 1. Player data first (most important for game detection and session tracking)
                if (_playerDataService != null)
                {
                    await _apiSemaphore.WaitAsync();
                    try
                    {
                        Console.WriteLine("[MonitoringService] Collecting initial player data...");
                        var playerData = await _playerDataService.CollectPlayerDataAsync();
                        UpdatePlayerSensors(playerData);
                        
                        // IMMEDIATE session tracking on startup
                        _sessionTracker?.UpdateSessionTracking(new SteamData
                        {
                            CurrentGameName = playerData.CurrentGameName,
                            CurrentGameAppId = playerData.CurrentGameAppId,
                            PlayerName = playerData.PlayerName,
                            Status = "Initial player data loaded"
                        });
                        
                        Console.WriteLine($"[MonitoringService] Initial player data: {playerData.PlayerName}, Game: '{playerData.CurrentGameName ?? "None"}'");
                    }
                    finally
                    {
                        _apiSemaphore.Release();
                    }
                }

                // Small delay between API calls for Steam rate limiting  
                await Task.Delay(1100);

                // 2. Social data second
                if (_socialDataService != null)
                {
                    await _apiSemaphore.WaitAsync();
                    try
                    {
                        Console.WriteLine("[MonitoringService] Collecting initial social data...");
                        var socialData = await _socialDataService.CollectSocialDataAsync();
                        UpdateSocialSensors(socialData);
                        Console.WriteLine($"[MonitoringService] Initial social data: {socialData.FriendsOnline} friends online");
                    }
                    finally
                    {
                        _apiSemaphore.Release();
                    }
                }

                // Small delay between API calls
                await Task.Delay(1100);

                // 3. Library data last (least critical for immediate display)
                if (_libraryDataService != null)
                {
                    await _apiSemaphore.WaitAsync();
                    try
                    {
                        Console.WriteLine("[MonitoringService] Collecting initial library data...");
                        var libraryData = await _libraryDataService.CollectLibraryDataAsync();
                        UpdateLibrarySensors(libraryData);
                        Console.WriteLine($"[MonitoringService] Initial library data: {libraryData?.TotalGamesOwned ?? 0} games owned");
                    }
                    finally
                    {
                        _apiSemaphore.Release();
                    }
                }

                Console.WriteLine("[MonitoringService] Initial data collection completed - all sensors populated");
                _logger?.LogInfo("Initial data collection completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error during initial data collection: {ex.Message}");
                _logger?.LogError("Error during initial data collection", ex);
            }
        }

        #endregion

        #region Multi-Timer Callbacks

        /// <summary>
        /// Player timer callback (1 second) - Game state, sessions, profile
        /// MOST IMPORTANT: Handles game detection and session tracking
        /// </summary>
        private void OnPlayerTimerElapsed(object? state)
        {
            if (!_isMonitoring || _playerDataService == null)
                return;
                
            try
            {
                _playerCycleCount++;
                
                // Timer interval verification
                var now = DateTime.Now;
                if (_playerTimerLastRun != DateTime.MinValue)
                {
                    var actualInterval = (now - _playerTimerLastRun).TotalSeconds;
                    var expectedInterval = MonitoringConstants.PLAYER_TIMER_INTERVAL_SECONDS;
                    var deviation = Math.Abs(actualInterval - expectedInterval);
                    
                    if (deviation > 0.5) // Allow 0.5 second tolerance
                    {
                        Console.WriteLine($"[TIMER-TEST] Player timer deviation: Expected {expectedInterval}s, Actual {actualInterval:F2}s, Deviation {deviation:F2}s");
                    }
                }
                _playerTimerLastRun = now;
                
                Console.WriteLine($"[TIMER-TEST] Player timer cycle {_playerCycleCount} at {now:HH:mm:ss.fff}");
                
                // Run with API rate limiting
                _ = Task.Run(async () =>
                {
                    await _apiSemaphore.WaitAsync();
                    try
                    {
                        var playerData = await _playerDataService.CollectPlayerDataAsync();
                        
                        // IMMEDIATE session tracking - most critical functionality
                        _sessionTracker?.UpdateSessionTracking(new SteamData
                        {
                            CurrentGameName = playerData.CurrentGameName,
                            CurrentGameAppId = playerData.CurrentGameAppId,
                            PlayerName = playerData.PlayerName,
                            Status = $"Player cycle {_playerCycleCount}"
                        });
                        
                        // Update player sensors directly
                        UpdatePlayerSensors(playerData);
                        
                        // Fire data event for any other listeners
                        var steamData = new SteamData
                        {
                            Status = $"Player data updated - cycle {_playerCycleCount}",
                            Timestamp = DateTime.Now,
                            PlayerName = playerData.PlayerName,
                            SteamLevel = playerData.SteamLevel,
                            ProfileImageUrl = playerData.ProfileImageUrl,
                            OnlineState = playerData.OnlineState,
                            CurrentGameName = playerData.CurrentGameName,
                            CurrentGameAppId = playerData.CurrentGameAppId,
                            CurrentGameBannerUrl = playerData.CurrentGameBannerUrl
                        };
                        
                        DataUpdated?.Invoke(this, new DataUpdatedEventArgs(steamData));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MonitoringService] Error in player timer cycle {_playerCycleCount}: {ex.Message}");
                    }
                    finally
                    {
                        _apiSemaphore.Release();
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error in player timer: {ex.Message}");
            }
        }

        /// <summary>
        /// Social timer callback (15 seconds) - Friends status
        /// Does NOT interfere with game data
        /// </summary>
        private void OnSocialTimerElapsed(object? state)
        {
            if (!_isMonitoring || _socialDataService == null)
                return;
                
            try
            {
                _socialCycleCount++;
                
                // Timer interval verification
                var now = DateTime.Now;
                if (_socialTimerLastRun != DateTime.MinValue)
                {
                    var actualInterval = (now - _socialTimerLastRun).TotalSeconds;
                    var expectedInterval = MonitoringConstants.SOCIAL_TIMER_INTERVAL_SECONDS;
                    var deviation = Math.Abs(actualInterval - expectedInterval);
                    
                    if (deviation > 1.0) // Allow 1 second tolerance for 15s timer
                    {
                        Console.WriteLine($"[TIMER-TEST] Social timer deviation: Expected {expectedInterval}s, Actual {actualInterval:F2}s, Deviation {deviation:F2}s");
                    }
                }
                _socialTimerLastRun = now;
                
                Console.WriteLine($"[TIMER-TEST] Social timer cycle {_socialCycleCount} at {now:HH:mm:ss.fff}");
                
                _ = Task.Run(async () =>
                {
                    await _apiSemaphore.WaitAsync();
                    try
                    {
                        var socialData = await _socialDataService.CollectSocialDataAsync();
                        
                        // Update social sensors directly - NO main plugin interference
                        UpdateSocialSensors(socialData);
                        
                        // NO DataUpdated event - prevents overwriting player data
                        Console.WriteLine($"[MonitoringService] Social timer cycle {_socialCycleCount}: {socialData.FriendsOnline} friends online, {socialData.FriendsInGame} in game");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MonitoringService] Error in social timer cycle {_socialCycleCount}: {ex.Message}");
                    }
                    finally
                    {
                        _apiSemaphore.Release();
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error in social timer: {ex.Message}");
            }
        }

        /// <summary>
        /// Library timer callback (45 seconds) - Games owned, playtime stats
        /// Does NOT interfere with game data
        /// </summary>
        private void OnLibraryTimerElapsed(object? state)
        {
            if (!_isMonitoring || _libraryDataService == null)
                return;
                
            try
            {
                _libraryCycleCount++;
                
                _ = Task.Run(async () =>
                {
                    await _apiSemaphore.WaitAsync();
                    try
                    {
                        var libraryData = await _libraryDataService.CollectLibraryDataAsync();
                        
                        // Update library sensors directly - NO main plugin interference
                        UpdateLibrarySensors(libraryData);
                        
                        // NO DataUpdated event - prevents overwriting player data
                        Console.WriteLine($"[MonitoringService] Library timer cycle {_libraryCycleCount}: {libraryData?.TotalGamesOwned ?? 0} games, {libraryData?.TotalLibraryPlaytimeHours ?? 0:F1}h total");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MonitoringService] Error in library timer cycle {_libraryCycleCount}: {ex.Message}");
                    }
                    finally
                    {
                        _apiSemaphore.Release();
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error in library timer: {ex.Message}");
            }
        }

        #endregion

        #region Direct Sensor Update Methods

        /// <summary>
        /// Updates player-specific sensors directly from PlayerData
        /// Covers: Profile, online status, current game, session tracking
        /// </summary>
        private void UpdatePlayerSensors(PlayerData playerData)
        {
            try
            {
                // Create SteamData focused on player information for the main plugin to process
                var steamData = new SteamData
                {
                    // Core player information
                    PlayerName = playerData.PlayerName,
                    SteamLevel = playerData.SteamLevel,
                    OnlineState = playerData.OnlineState,
                    
                    // Profile image URL - critical for image sensors
                    ProfileImageUrl = !string.IsNullOrEmpty(playerData.ProfileImageUrl) 
                        ? playerData.ProfileImageUrl 
                        : "-",
                    
                    // Current game information - critical for session tracking
                    CurrentGameName = playerData.CurrentGameName,
                    CurrentGameAppId = playerData.CurrentGameAppId,
                    
                    // Game banner URL - critical for image sensors  
                    CurrentGameBannerUrl = !string.IsNullOrEmpty(playerData.CurrentGameBannerUrl)
                        ? playerData.CurrentGameBannerUrl
                        : "-",
                    
                    // Status and metadata
                    Status = $"Player data updated - cycle {_playerCycleCount}",
                    Details = $"Player: {playerData.PlayerName ?? "Unknown"}, Game: {playerData.CurrentGameName ?? "Not Playing"}",
                    Timestamp = DateTime.Now
                };
                
                // Fire event for main plugin to update sensors
                DataUpdated?.Invoke(this, new DataUpdatedEventArgs(steamData));
                
                _logger?.LogDebug($"Player sensors updated - Game: '{playerData.CurrentGameName}', Profile URL: {playerData.ProfileImageUrl != null}, Banner URL: {playerData.CurrentGameBannerUrl != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error updating player sensors: {ex.Message}");
                _logger?.LogError("Error updating player sensors", ex);
            }
        }

        /// <summary>
        /// Updates social-specific sensors directly from SocialData
        /// Covers: Friends online, friends in game, social activity
        /// </summary>
        private void UpdateSocialSensors(SocialData socialData)
        {
            try
            {
                // Create a MINIMAL SteamData object with ONLY social fields populated
                // This prevents overwriting player data while still updating friend sensors
                var socialSteamData = new SteamData
                {
                    // ONLY populate social/friends fields - leave player fields as null/default
                    TotalFriendsCount = socialData.TotalFriends,
                    FriendsOnline = socialData.FriendsOnline,
                    FriendsInGame = socialData.FriendsInGame,
                    FriendsPopularGame = socialData.FriendsPopularGame,
                    
                    // Status for debugging - does NOT overwrite main status
                    Details = $"Social update: {socialData.FriendsOnline} friends online, {socialData.FriendsInGame} in game",
                    Timestamp = DateTime.Now
                };
                
                // Fire TARGETED DataUpdated event with only social data
                DataUpdated?.Invoke(this, new DataUpdatedEventArgs(socialSteamData));
                
                _logger?.LogDebug($"Social sensors updated - {socialData.FriendsOnline} friends online, {socialData.FriendsInGame} in game");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error updating social sensors: {ex.Message}");
                _logger?.LogError("Error updating social sensors", ex);
            }
        }

        /// <summary>
        /// Updates library-specific sensors directly from LibraryData  
        /// Covers: Total games, total playtime, library statistics
        /// </summary>
        private void UpdateLibrarySensors(LibraryData? libraryData)
        {
            try
            {
                if (libraryData == null)
                {
                    _logger?.LogDebug("No library data available for sensor updates");
                    return;
                }
                
                // Create a MINIMAL SteamData object with ONLY library fields populated
                // This prevents overwriting player data while still updating library sensors
                var librarySteamData = new SteamData
                {
                    // ONLY populate library fields - leave player fields as null/default
                    TotalGamesOwned = libraryData.TotalGamesOwned,
                    TotalLibraryPlaytimeHours = libraryData.TotalLibraryPlaytimeHours,
                    RecentPlaytimeHours = libraryData.RecentPlaytimeHours,
                    RecentGamesCount = libraryData.RecentGamesCount,
                    MostPlayedGameName = libraryData.MostPlayedGameName,
                    MostPlayedGameHours = libraryData.MostPlayedGameHours,
                    
                    // Status for debugging - does NOT overwrite main status  
                    Details = $"Library update: {libraryData.TotalGamesOwned} games, {libraryData.TotalLibraryPlaytimeHours:F1}h total",
                    Timestamp = DateTime.Now
                };
                
                // Fire TARGETED DataUpdated event with only library data
                DataUpdated?.Invoke(this, new DataUpdatedEventArgs(librarySteamData));
                
                _logger?.LogDebug($"Library sensors updated - {libraryData.TotalGamesOwned} games, {libraryData.TotalLibraryPlaytimeHours:F1} hours");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error updating library sensors: {ex.Message}");
                _logger?.LogError("Error updating library sensors", ex);
            }
        }

        #endregion

        #region Disposal
        
        public void Dispose()
        {
            try
            {
                _isMonitoring = false;
                _playerTimer?.Dispose();
                _socialTimer?.Dispose();
                _libraryTimer?.Dispose();
                _apiSemaphore?.Dispose();
                _steamApiService?.Dispose();
                _sessionTracker?.Dispose();
                
                Console.WriteLine($"[{MonitoringConstants.SERVICE_NAME}] {MonitoringConstants.MSG_SERVICE_DISPOSED}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{MonitoringConstants.SERVICE_NAME}] Error during disposal: {ex.Message}");
            }
        }
        
        #endregion
    }
}