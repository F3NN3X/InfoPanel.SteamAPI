using System;
using System.Data;
using System.Linq;
using InfoPanel.Plugins;
using InfoPanel.SteamAPI.Models;
using InfoPanel.SteamAPI.Services.Monitoring;

namespace InfoPanel.SteamAPI.Services.Sensors
{
    /// <summary>
    /// Library Domain Sensor Service
    /// Updates InfoPanel sensors for library/games data:
    /// - Total games owned and playtime statistics
    /// - Recent games played (last 2 weeks)
    /// - Most played recent game
    /// - Recent games table
    /// </summary>
    public class LibrarySensorService : IDisposable
    {
        private const string DOMAIN_NAME = "LIBRARY_SENSORS";
        private const int MINUTES_PER_HOUR = 60;

        // Configuration and services
        private readonly ConfigurationService _configService;
        private readonly EnhancedLoggingService? _enhancedLogger;

        // Thread safety
        private readonly object _sensorLock = new();

        // Library sensors (injected via constructor)
        private readonly PluginSensor _totalGamesSensor;
        private readonly PluginSensor _totalPlaytimeSensor;
        private readonly PluginSensor _recentPlaytimeSensor;
        private readonly PluginSensor _recentGamesCountSensor;
        private readonly PluginText _mostPlayedRecentSensor;
        private readonly PluginSensor _recentSessionsSensor;
        private readonly PluginTable _recentGamesTable;

        private bool _disposed = false;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public LibrarySensorService(
            ConfigurationService configService,
            PluginSensor totalGamesSensor,
            PluginSensor totalPlaytimeSensor,
            PluginSensor recentPlaytimeSensor,
            PluginSensor recentGamesCountSensor,
            PluginText mostPlayedRecentSensor,
            PluginSensor recentSessionsSensor,
            PluginTable recentGamesTable,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _totalGamesSensor = totalGamesSensor ?? throw new ArgumentNullException(nameof(totalGamesSensor));
            _totalPlaytimeSensor = totalPlaytimeSensor ?? throw new ArgumentNullException(nameof(totalPlaytimeSensor));
            _recentPlaytimeSensor = recentPlaytimeSensor ?? throw new ArgumentNullException(nameof(recentPlaytimeSensor));
            _recentGamesCountSensor = recentGamesCountSensor ?? throw new ArgumentNullException(nameof(recentGamesCountSensor));
            _mostPlayedRecentSensor = mostPlayedRecentSensor ?? throw new ArgumentNullException(nameof(mostPlayedRecentSensor));
            _recentSessionsSensor = recentSessionsSensor ?? throw new ArgumentNullException(nameof(recentSessionsSensor));
            _recentGamesTable = recentGamesTable ?? throw new ArgumentNullException(nameof(recentGamesTable));
            _enhancedLogger = enhancedLogger;

            Console.WriteLine($"[{DOMAIN_NAME}] LibrarySensorService initialized");
        }

        /// <summary>
        /// Subscribe to library monitoring events
        /// </summary>
        public void SubscribeToMonitoring(LibraryMonitoringService libraryMonitoring)
        {
            if (libraryMonitoring == null)
                throw new ArgumentNullException(nameof(libraryMonitoring));

            libraryMonitoring.LibraryDataUpdated += OnLibraryDataUpdated;

            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.SubscribeToMonitoring", "Subscribed to library monitoring events");
        }

        /// <summary>
        /// Unsubscribe from library monitoring events
        /// </summary>
        public void UnsubscribeFromMonitoring(LibraryMonitoringService libraryMonitoring)
        {
            if (libraryMonitoring == null)
                return;

            libraryMonitoring.LibraryDataUpdated -= OnLibraryDataUpdated;

            _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.UnsubscribeFromMonitoring", "Unsubscribed from library monitoring events");
        }

        /// <summary>
        /// Event handler for library data updates
        /// </summary>
        private void OnLibraryDataUpdated(object? sender, LibraryDataEventArgs e)
        {
            // Library data can be null (e.g., private profile)
            if (e?.LibraryData == null)
            {
                _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.OnLibraryDataUpdated", "Received null library data - skipping update");
                return;
            }

            try
            {
                UpdateLibrarySensors(e.LibraryData, e.SessionCache);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Error updating library sensors: {ex.Message}");

                _enhancedLogger?.LogError($"{DOMAIN_NAME}.OnLibraryDataUpdated", "Failed to update sensors", ex);
            }
        }

        /// <summary>
        /// Update all library sensors with data from monitoring service
        /// </summary>
        private void UpdateLibrarySensors(LibraryData libraryData, SessionDataCache sessionCache)
        {
            lock (_sensorLock)
            {
                try
                {
                    _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.UpdateLibrarySensors", "Updating library sensors", new
                    {
                        TotalGamesOwned = libraryData.TotalGamesOwned,
                        RecentGamesCount = libraryData.RecentGamesCount,
                        MostPlayedRecent = libraryData.MostPlayedRecentGame
                    });

                    // Update total games and playtime sensors
                    _totalGamesSensor.Value = (float)libraryData.TotalGamesOwned;

                    var totalPlaytime = (float)Math.Round(libraryData.TotalLibraryPlaytimeHours, 1);
                    _totalPlaytimeSensor.Value = totalPlaytime;

                    var recentPlaytime = (float)Math.Round(libraryData.RecentPlaytimeHours, 1);
                    _recentPlaytimeSensor.Value = recentPlaytime;

                    // Update recent games sensors
                    _recentGamesCountSensor.Value = (float)libraryData.RecentGamesCount;
                    
                    // Update recent sessions count (from local tracking)
                    _recentSessionsSensor.Value = (float)libraryData.RecentSessionsCount;
                    
                    var mostPlayedRecent = libraryData.MostPlayedRecentGame ?? "None";
                    _mostPlayedRecentSensor.Value = mostPlayedRecent;

                    // Build recent games table
                    _recentGamesTable.Value = BuildRecentGamesTable(libraryData);

                    _enhancedLogger?.LogInfo($"{DOMAIN_NAME}.UpdateLibrarySensors", "Library sensors updated successfully", new
                    {
                        TotalGames = libraryData.TotalGamesOwned,
                        TotalPlaytimeHours = totalPlaytime,
                        RecentGamesCount = libraryData.RecentGamesCount,
                        MostPlayedRecent = mostPlayedRecent
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DOMAIN_NAME}] Error updating sensors: {ex.Message}");

                    _enhancedLogger?.LogError($"{DOMAIN_NAME}.UpdateLibrarySensors", "Sensor update failed", ex);

                    SetErrorState(ex.Message);
                }
            }
        }

        /// <summary>
        /// Build recent games table from library data
        /// </summary>
        private DataTable BuildRecentGamesTable(LibraryData libraryData)
        {
            var dataTable = new DataTable();

            try
            {
                // Initialize columns
                dataTable.Columns.Add("Game", typeof(PluginText));
                dataTable.Columns.Add("2w Hours", typeof(PluginText));
                dataTable.Columns.Add("Total Hours", typeof(PluginText));

                if (libraryData.RecentGames != null && libraryData.RecentGames.Count > 0)
                {
                    // Sort by recent playtime (2 weeks) descending
                    var sortedGames = libraryData.RecentGames
                        .OrderByDescending(g => g.Playtime2weeks ?? 0)
                        .ToList();

                    foreach (var game in sortedGames)
                    {
                        var row = dataTable.NewRow();

                        // Game name column
                        row["Game"] = new PluginText($"recent-game_{game.AppId}", game.Name ?? "Unknown Game");

                        // Recent playtime (2 weeks) in hours
                        var recentHours = (game.Playtime2weeks ?? 0) / (double)MINUTES_PER_HOUR;
                        row["2w Hours"] = new PluginText($"recent-hours_{game.AppId}", $"{recentHours:F1}h");

                        // Total playtime in hours
                        var totalHours = game.PlaytimeForever / (double)MINUTES_PER_HOUR;
                        row["Total Hours"] = new PluginText($"total-hours_{game.AppId}", $"{totalHours:F1}h");

                        dataTable.Rows.Add(row);
                    }

                    _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.BuildRecentGamesTable", "Built recent games table", new
                    {
                        GamesCount = dataTable.Rows.Count
                    });
                }
                else
                {
                    _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.BuildRecentGamesTable", "No recent games data available");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Error building recent games table: {ex.Message}");

                _enhancedLogger?.LogError($"{DOMAIN_NAME}.BuildRecentGamesTable", "Failed to build table", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// Set all library sensors to error state
        /// </summary>
        private void SetErrorState(string errorMessage)
        {
            try
            {
                _totalGamesSensor.Value = 0f;
                _totalPlaytimeSensor.Value = 0f;
                _recentPlaytimeSensor.Value = 0f;
                _recentGamesCountSensor.Value = 0f;
                _mostPlayedRecentSensor.Value = "Error";
                _recentGamesTable.Value = new DataTable();

                _enhancedLogger?.LogError($"{DOMAIN_NAME}.SetErrorState", "Library sensors set to error state", null, new
                {
                    ErrorMessage = errorMessage
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DOMAIN_NAME}] Error setting error state: {ex.Message}");
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
                Console.WriteLine($"[{DOMAIN_NAME}] LibrarySensorService disposed");
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
