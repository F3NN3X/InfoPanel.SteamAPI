using InfoPanel.SteamAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Services
{
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
    /// Core monitoring service for Steam data collection
    /// Manages Steam API calls and data aggregation
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
        private readonly Timer _monitoringTimer;
        private SteamApiService? _steamApiService;
        private volatile bool _isMonitoring;
        private readonly object _lockObject = new();
        
        #endregion

        #region Constructor
        
        public MonitoringService(ConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            
            // Initialize timer (but don't start it yet)
            _monitoringTimer = new System.Threading.Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            
            Console.WriteLine("[MonitoringService] Steam monitoring service initialized");
        }
        
        #endregion

        #region Monitoring Control
        
        /// <summary>
        /// Starts the Steam monitoring process
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
                
                // Start the monitoring timer using Steam update interval
                var intervalSeconds = _configService.UpdateIntervalSeconds;
                var intervalMs = intervalSeconds * 1000;
                
                _monitoringTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(intervalMs));
                
                Console.WriteLine($"[MonitoringService] Steam monitoring started (interval: {intervalSeconds}s)");
                
                // Keep the task alive while monitoring
                while (_isMonitoring && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[MonitoringService] Steam monitoring cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error during Steam monitoring: {ex.Message}");
                throw;
            }
            finally
            {
                await StopMonitoringAsync();
            }
        }
        
        /// <summary>
        /// Stops the Steam monitoring process
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring)
                {
                    return;
                }
                
                _isMonitoring = false;
            }
            
            // Stop the timer
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Dispose Steam API service
            _steamApiService?.Dispose();
            _steamApiService = null;
            
            Console.WriteLine("[MonitoringService] Steam monitoring stopped");
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
                var apiKey = _configService.SteamApiKey;
                var steamId = _configService.SteamId;
                
                if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "<your-steam-api-key-here>")
                {
                    throw new InvalidOperationException("Steam API Key is not configured. Please update the configuration file.");
                }
                
                if (string.IsNullOrWhiteSpace(steamId) || steamId == "<your-steam-id-here>")
                {
                    throw new InvalidOperationException("Steam ID is not configured. Please update the configuration file.");
                }
                
                _steamApiService = new SteamApiService(apiKey, steamId);
                
                // Test the connection
                var isValid = await _steamApiService.TestConnectionAsync();
                if (!isValid)
                {
                    throw new InvalidOperationException("Failed to connect to Steam API. Check your API key and Steam ID.");
                }
                
                Console.WriteLine("[MonitoringService] Steam API connection established");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Failed to initialize Steam API: {ex.Message}");
                throw;
            }
        }
        
        #endregion

        #region Data Collection
        
        private void OnTimerElapsed(object? state)
        {
            if (!_isMonitoring || _steamApiService == null)
                return;
                
            try
            {
                // Collect Steam data asynchronously
                _ = Task.Run(async () =>
                {
                    var data = await CollectSteamDataAsync();
                    OnDataUpdated(data);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error in timer callback: {ex.Message}");
                
                // Create error data
                var errorData = new SteamData($"Timer error: {ex.Message}");
                OnDataUpdated(errorData);
            }
        }
        
        /// <summary>
        /// Collects data from Steam API
        /// </summary>
        private async Task<SteamData> CollectSteamDataAsync()
        {
            if (_steamApiService == null)
            {
                return new SteamData("Steam API service not initialized");
            }
            
            try
            {
                var data = new SteamData
                {
                    Timestamp = DateTime.Now,
                    Status = "Collecting data..."
                };
                
                // Collect player summary if profile monitoring is enabled
                if (_configService.EnableProfileMonitoring)
                {
                    var playerSummary = await _steamApiService.GetPlayerSummaryAsync();
                    if (playerSummary != null)
                    {
                        data.PlayerName = playerSummary.PersonaName;
                        data.ProfileUrl = playerSummary.ProfileUrl;
                        data.AvatarUrl = playerSummary.Avatar;
                        data.OnlineState = SteamApiService.GetPersonaStateString(playerSummary.PersonaState);
                        data.LastLogOff = playerSummary.LastLogOff;
                        data.CurrentGameName = playerSummary.GameExtraInfo;
                        data.CurrentGameServerIp = playerSummary.GameServerIp;
                        
                        // Try to parse game ID
                        if (int.TryParse(playerSummary.GameId, out var gameId))
                        {
                            data.CurrentGameAppId = gameId;
                        }
                    }
                    
                    // Get Steam level
                    data.SteamLevel = await _steamApiService.GetSteamLevelAsync();
                }
                
                // Collect library data if library monitoring is enabled
                if (_configService.EnableLibraryMonitoring)
                {
                    var ownedGames = await _steamApiService.GetOwnedGamesAsync();
                    if (ownedGames != null && ownedGames.Count > 0)
                    {
                        data.TotalGamesOwned = ownedGames.Count;
                        data.TotalLibraryPlaytimeHours = ownedGames.Sum(g => g.PlaytimeForever) / 60.0; // Convert minutes to hours
                        
                        // Find most played game
                        var mostPlayed = ownedGames.OrderByDescending(g => g.PlaytimeForever).FirstOrDefault();
                        if (mostPlayed != null)
                        {
                            data.MostPlayedGameName = mostPlayed.Name;
                            data.MostPlayedGameHours = mostPlayed.PlaytimeForever / 60.0;
                        }
                        
                        // Set current game playtime if currently playing
                        if (!string.IsNullOrEmpty(data.CurrentGameName))
                        {
                            var currentGame = ownedGames.FirstOrDefault(g => 
                                g.AppId == data.CurrentGameAppId || 
                                g.Name.Equals(data.CurrentGameName, StringComparison.OrdinalIgnoreCase));
                            
                            if (currentGame != null)
                            {
                                data.TotalPlaytimeHours = currentGame.PlaytimeForever / 60.0;
                            }
                        }
                    }
                }
                
                // Collect recent activity if current game monitoring is enabled
                if (_configService.EnableCurrentGameMonitoring)
                {
                    var recentGames = await _steamApiService.GetRecentlyPlayedGamesAsync();
                    if (recentGames != null && recentGames.Count > 0)
                    {
                        data.RecentPlaytimeHours = recentGames.Sum(g => g.Playtime2Weeks) / 60.0;
                        data.RecentGamesCount = recentGames.Count;
                    }
                }
                
                // Set status and details based on collected data
                data.Status = data.IsOnline() ? "Online" : "Offline";
                data.Details = $"Updated at {DateTime.Now:HH:mm:ss}";
                
                Console.WriteLine($"[MonitoringService] Steam data collected for {data.PlayerName}");
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error collecting Steam data: {ex.Message}");
                return new SteamData($"Collection error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Triggers the DataUpdated event
        /// </summary>
        private void OnDataUpdated(SteamData data)
        {
            try
            {
                DataUpdated?.Invoke(this, new DataUpdatedEventArgs(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error in DataUpdated event: {ex.Message}");
            }
        }
        
        #endregion

        #region Disposal
        
        public void Dispose()
        {
            try
            {
                _isMonitoring = false;
                _monitoringTimer?.Dispose();
                _steamApiService?.Dispose();
                
                Console.WriteLine("[MonitoringService] Steam monitoring service disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringService] Error during disposal: {ex.Message}");
            }
        }
        
        #endregion
    }
}