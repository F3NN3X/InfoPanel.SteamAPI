using System;
using System.Data;
using System.Linq;
using InfoPanel.Plugins;
using InfoPanel.SteamAPI.Models;
using InfoPanel.SteamAPI.Services.Monitoring;

namespace InfoPanel.SteamAPI.Services.Sensors
{
    public class NewsSensorService : IDisposable
    {
        private const string DOMAIN_NAME = "NEWS_SENSORS";

        private readonly EnhancedLoggingService? _enhancedLogger;
        private readonly object _sensorLock = new();

        // Sensors
        private readonly PluginText _currentGameNewsTitleSensor;
        private readonly PluginTable _libraryNewsTableSensor;

        public NewsSensorService(
            PluginText currentGameNewsTitleSensor,
            PluginTable libraryNewsTableSensor,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _currentGameNewsTitleSensor = currentGameNewsTitleSensor ?? throw new ArgumentNullException(nameof(currentGameNewsTitleSensor));
            _libraryNewsTableSensor = libraryNewsTableSensor ?? throw new ArgumentNullException(nameof(libraryNewsTableSensor));
            _enhancedLogger = enhancedLogger;
        }

        public void SubscribeToMonitoring(NewsMonitoringService monitoringService)
        {
            if (monitoringService == null) throw new ArgumentNullException(nameof(monitoringService));
            monitoringService.NewsDataUpdated += OnNewsDataUpdated;
            _enhancedLogger?.LogInfo(DOMAIN_NAME, "Subscribed to NewsMonitoringService");
        }

        public void UnsubscribeFromMonitoring(NewsMonitoringService monitoringService)
        {
            if (monitoringService == null) return;
            monitoringService.NewsDataUpdated -= OnNewsDataUpdated;
            _enhancedLogger?.LogInfo(DOMAIN_NAME, "Unsubscribed from NewsMonitoringService");
        }

        private void OnNewsDataUpdated(object? sender, NewsDataEventArgs e)
        {
            UpdateSensors(e.Data);
        }

        private void UpdateSensors(NewsData data)
        {
            lock (_sensorLock)
            {
                try
                {
                    // 1. Current Game News Title
                    if (data.CurrentGameNews != null)
                    {
                        _currentGameNewsTitleSensor.Value = data.CurrentGameNews.Title;
                    }
                    else
                    {
                        _currentGameNewsTitleSensor.Value = "-";
                    }

                    // 2. Library News Table
                    UpdateLibraryNewsTable(data);
                }
                catch (Exception ex)
                {
                    _enhancedLogger?.LogError(DOMAIN_NAME, "Error updating sensors", ex);
                }
            }
        }

        private void UpdateLibraryNewsTable(NewsData data)
        {
            try
            {
                var table = new DataTable();
                table.Columns.Add("Game", typeof(PluginText));
                table.Columns.Add("News", typeof(PluginText));
                table.Columns.Add("Date", typeof(PluginText));

                foreach (var (appId, gameName, news) in data.LibraryNews)
                {
                    var row = table.NewRow();

                    // Use the game name we passed through
                    string displayName = !string.IsNullOrEmpty(gameName) ? gameName : $"App {appId}";

                    // Format date
                    var date = DateTimeOffset.FromUnixTimeSeconds(news.Date).ToLocalTime();
                    string dateStr = date.ToString("MMM dd");

                    row["Game"] = new PluginText($"game_{news.Gid}", displayName);
                    row["News"] = new PluginText($"title_{news.Gid}", news.Title);
                    row["Date"] = new PluginText($"date_{news.Gid}", dateStr);

                    table.Rows.Add(row);
                }

                _libraryNewsTableSensor.Value = table;
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError(DOMAIN_NAME, "Error updating library news table", ex);
            }
        }

        public void Dispose()
        {
            // Unsubscribe logic would go here if we kept reference to service
        }
    }
}
