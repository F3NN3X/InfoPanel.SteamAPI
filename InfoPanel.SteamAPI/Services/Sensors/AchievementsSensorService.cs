using InfoPanel.Plugins;
using InfoPanel.SteamAPI.Models;
using InfoPanel.SteamAPI.Services.Monitoring;
using System;

namespace InfoPanel.SteamAPI.Services.Sensors
{
    public class AchievementsSensorService : IDisposable
    {
        private const string DOMAIN_NAME = "ACHIEVEMENTS_SENSORS";

        private readonly ConfigurationService _configService;
        private readonly EnhancedLoggingService? _enhancedLogger;
        private readonly object _sensorLock = new();

        // Sensors
        private readonly PluginSensor _currentGameAchievementsSensor;
        private readonly PluginSensor _currentGameAchievementsUnlockedSensor;
        private readonly PluginSensor _currentGameAchievementsTotalSensor;
        private readonly PluginText _latestAchievementSensor;
        private readonly PluginText _latestAchievementIconSensor;
        private readonly PluginSensor _totalBadgesEarnedSensor;
        private readonly PluginSensor _totalBadgeXPSensor;
        private readonly PluginText _latestBadgeSensor;

        public AchievementsSensorService(
            ConfigurationService configService,
            PluginSensor currentGameAchievementsSensor,
            PluginSensor currentGameAchievementsUnlockedSensor,
            PluginSensor currentGameAchievementsTotalSensor,
            PluginText latestAchievementSensor,
            PluginText latestAchievementIconSensor,
            PluginSensor totalBadgesEarnedSensor,
            PluginSensor totalBadgeXPSensor,
            PluginText latestBadgeSensor,
            EnhancedLoggingService? enhancedLogger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _currentGameAchievementsSensor = currentGameAchievementsSensor;
            _currentGameAchievementsUnlockedSensor = currentGameAchievementsUnlockedSensor;
            _currentGameAchievementsTotalSensor = currentGameAchievementsTotalSensor;
            _latestAchievementSensor = latestAchievementSensor;
            _latestAchievementIconSensor = latestAchievementIconSensor;
            _totalBadgesEarnedSensor = totalBadgesEarnedSensor;
            _totalBadgeXPSensor = totalBadgeXPSensor;
            _latestBadgeSensor = latestBadgeSensor;
            _enhancedLogger = enhancedLogger;
        }

        public void SubscribeToMonitoring(AchievementsMonitoringService monitoringService)
        {
            if (monitoringService == null) throw new ArgumentNullException(nameof(monitoringService));
            monitoringService.AchievementsDataUpdated += OnAchievementsDataUpdated;
        }

        public void UnsubscribeFromMonitoring(AchievementsMonitoringService monitoringService)
        {
            if (monitoringService == null) throw new ArgumentNullException(nameof(monitoringService));
            monitoringService.AchievementsDataUpdated -= OnAchievementsDataUpdated;
        }

        private void OnAchievementsDataUpdated(object? sender, AchievementsDataEventArgs e)
        {
            lock (_sensorLock)
            {
                try
                {
                    var data = e.Data;

                    // Badges
                    _totalBadgesEarnedSensor.Value = data.TotalBadgesEarned;
                    _totalBadgeXPSensor.Value = data.TotalBadgeXP;
                    _latestBadgeSensor.Value = data.LatestBadgeName ?? "None";

                    // Achievements
                    if (data.CurrentGameAppId > 0)
                    {
                        _currentGameAchievementsSensor.Value = (float)Math.Round(data.CurrentGameCompletionPercent, 1);
                        _currentGameAchievementsUnlockedSensor.Value = data.CurrentGameUnlockedCount;
                        _currentGameAchievementsTotalSensor.Value = data.CurrentGameAchievementCount;
                        _latestAchievementSensor.Value = data.LatestAchievementName ?? "None";
                        _latestAchievementIconSensor.Value = data.LatestAchievementIcon ?? "-";
                    }
                    else
                    {
                        // Reset or keep last? Usually reset if not playing
                        _currentGameAchievementsSensor.Value = 0;
                        _currentGameAchievementsUnlockedSensor.Value = 0;
                        _currentGameAchievementsTotalSensor.Value = 0;
                        _latestAchievementSensor.Value = "Not Playing";
                        _latestAchievementIconSensor.Value = "-";
                    }

                    _enhancedLogger?.LogDebug($"{DOMAIN_NAME}.OnAchievementsDataUpdated", "Sensors updated");
                }
                catch (Exception ex)
                {
                    _enhancedLogger?.LogError($"{DOMAIN_NAME}.OnAchievementsDataUpdated", "Error updating sensors", ex);
                }
            }
        }

        public void Dispose()
        {
            // Unsubscribe if needed, though usually handled by GC/Service lifecycle
        }
    }
}
