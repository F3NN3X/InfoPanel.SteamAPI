using InfoPanel.Plugins;
using InfoPanel.SteamAPI.Models;
using System;

namespace InfoPanel.SteamAPI.Services
{
    /// <summary>
    /// Manages Steam sensor updates with thread safety and proper data formatting
    /// Implements thread-safe sensor update patterns for InfoPanel Steam monitoring
    /// </summary>
    public class SensorManagementService
    {
        #region Fields
        
        private readonly ConfigurationService _configService;
        private readonly object _sensorLock = new();
        
        #endregion

        #region Constructor
        
        public SensorManagementService(ConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }
        
        #endregion

        #region Steam Sensor Updates
        
        /// <summary>
        /// Updates all Steam sensors with new data in a thread-safe manner
        /// </summary>
        public void UpdateSteamSensors(
            PluginText playerNameSensor,
            PluginText onlineStatusSensor,
            PluginSensor steamLevelSensor,
            PluginText currentGameSensor,
            PluginSensor currentGamePlaytimeSensor,
            PluginSensor totalGamesSensor,
            PluginSensor totalPlaytimeSensor,
            PluginSensor recentPlaytimeSensor,
            PluginText statusSensor,
            PluginText detailsSensor,
            SteamData data)
        {
            if (data == null) return;
            
            lock (_sensorLock)
            {
                try
                {
                    if (data.HasError)
                    {
                        SetErrorState(playerNameSensor, onlineStatusSensor, steamLevelSensor,
                            currentGameSensor, currentGamePlaytimeSensor, totalGamesSensor,
                            totalPlaytimeSensor, recentPlaytimeSensor, statusSensor, detailsSensor,
                            data.ErrorMessage ?? "Unknown error");
                        return;
                    }
                    
                    // Update profile sensors
                    UpdateProfileSensors(playerNameSensor, onlineStatusSensor, steamLevelSensor, data);
                    
                    // Update current game sensors
                    UpdateCurrentGameSensors(currentGameSensor, currentGamePlaytimeSensor, data);
                    
                    // Update library statistics sensors
                    UpdateLibrarySensors(totalGamesSensor, totalPlaytimeSensor, recentPlaytimeSensor, data);
                    
                    // Update status sensors
                    UpdateStatusSensors(statusSensor, detailsSensor, data);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SensorManagementService] Error updating Steam sensors: {ex.Message}");
                    
                    // Set error state for all sensors
                    SetErrorState(playerNameSensor, onlineStatusSensor, steamLevelSensor,
                        currentGameSensor, currentGamePlaytimeSensor, totalGamesSensor,
                        totalPlaytimeSensor, recentPlaytimeSensor, statusSensor, detailsSensor,
                        ex.Message);
                }
            }
        }
        
        /// <summary>
        /// Updates Steam profile-related sensors
        /// </summary>
        private void UpdateProfileSensors(
            PluginText playerNameSensor,
            PluginText onlineStatusSensor,
            PluginSensor steamLevelSensor,
            SteamData data)
        {
            // Update player name
            playerNameSensor.Value = !string.IsNullOrEmpty(data.PlayerName) ? data.PlayerName : "Unknown Player";
            
            // Update online status
            onlineStatusSensor.Value = data.GetDisplayStatus();
            
            // Update Steam level
            steamLevelSensor.Value = data.SteamLevel;
        }
        
        /// <summary>
        /// Updates current game-related sensors
        /// </summary>
        private void UpdateCurrentGameSensors(
            PluginText currentGameSensor,
            PluginSensor currentGamePlaytimeSensor,
            SteamData data)
        {
            // Update current game
            if (data.IsInGame() && !string.IsNullOrEmpty(data.CurrentGameName))
            {
                currentGameSensor.Value = data.CurrentGameName;
                currentGamePlaytimeSensor.Value = (float)Math.Round(data.TotalPlaytimeHours, 1);
            }
            else
            {
                currentGameSensor.Value = "Not Playing";
                currentGamePlaytimeSensor.Value = 0;
            }
        }
        
        /// <summary>
        /// Updates library statistics sensors
        /// </summary>
        private void UpdateLibrarySensors(
            PluginSensor totalGamesSensor,
            PluginSensor totalPlaytimeSensor,
            PluginSensor recentPlaytimeSensor,
            SteamData data)
        {
            // Update total games owned
            totalGamesSensor.Value = (float)data.TotalGamesOwned;
            
            // Update total playtime
            totalPlaytimeSensor.Value = (float)Math.Round(data.TotalLibraryPlaytimeHours, 1);
            
            // Update recent playtime
            recentPlaytimeSensor.Value = (float)Math.Round(data.RecentPlaytimeHours, 1);
        }
        
        /// <summary>
        /// Updates status and details sensors
        /// </summary>
        private void UpdateStatusSensors(
            PluginText statusSensor,
            PluginText detailsSensor,
            SteamData data)
        {
            // Update status
            statusSensor.Value = FormatSteamStatus(data);
            
            // Update details
            detailsSensor.Value = FormatSteamDetails(data);
        }
        
        #endregion

        #region Steam-Specific Formatting
        
        /// <summary>
        /// Formats the Steam status text based on data state
        /// </summary>
        private string FormatSteamStatus(SteamData data)
        {
            if (data.HasError)
            {
                return "Error";
            }
            
            if (data.IsInGame())
            {
                return $"Playing {data.CurrentGameName}";
            }
            
            if (data.IsOnline())
            {
                return data.OnlineState ?? "Online";
            }
            
            return "Offline";
        }
        
        /// <summary>
        /// Formats detailed Steam information text
        /// </summary>
        private string FormatSteamDetails(SteamData data)
        {
            if (data.HasError)
            {
                return $"Error: {data.Details}";
            }
            
            var details = $"Level {data.SteamLevel}";
            
            if (data.TotalGamesOwned > 0)
            {
                details += $" • {data.TotalGamesOwned:F0} games";
            }
            
            if (data.TotalLibraryPlaytimeHours > 0)
            {
                details += $" • {data.TotalLibraryPlaytimeHours:F0}h total";
            }
            
            if (data.RecentPlaytimeHours > 0)
            {
                details += $" • {data.RecentPlaytimeHours:F1}h recent";
            }
            
            details += $" • Updated: {data.Timestamp:HH:mm:ss}";
            
            return details;
        }
        
        #endregion

        #region Error Handling
        
        /// <summary>
        /// Sets error state for all Steam sensors when data collection fails
        /// </summary>
        private void SetErrorState(
            PluginText playerNameSensor,
            PluginText onlineStatusSensor,
            PluginSensor steamLevelSensor,
            PluginText currentGameSensor,
            PluginSensor currentGamePlaytimeSensor,
            PluginSensor totalGamesSensor,
            PluginSensor totalPlaytimeSensor,
            PluginSensor recentPlaytimeSensor,
            PluginText statusSensor,
            PluginText detailsSensor,
            string errorMessage)
        {
            try
            {
                playerNameSensor.Value = "Error";
                onlineStatusSensor.Value = "Offline";
                steamLevelSensor.Value = 0;
                currentGameSensor.Value = "Not Playing";
                currentGamePlaytimeSensor.Value = 0;
                totalGamesSensor.Value = 0;
                totalPlaytimeSensor.Value = 0;
                recentPlaytimeSensor.Value = 0;
                statusSensor.Value = "Error";
                detailsSensor.Value = $"Steam data collection failed: {errorMessage}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SensorManagementService] Error setting error state: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Resets all Steam sensors to default/empty state
        /// </summary>
        public void ResetSteamSensors(
            PluginText playerNameSensor,
            PluginText onlineStatusSensor,
            PluginSensor steamLevelSensor,
            PluginText currentGameSensor,
            PluginSensor currentGamePlaytimeSensor,
            PluginSensor totalGamesSensor,
            PluginSensor totalPlaytimeSensor,
            PluginSensor recentPlaytimeSensor,
            PluginText statusSensor,
            PluginText detailsSensor)
        {
            lock (_sensorLock)
            {
                try
                {
                    playerNameSensor.Value = "Loading...";
                    onlineStatusSensor.Value = "Offline";
                    steamLevelSensor.Value = 0;
                    currentGameSensor.Value = "Not Playing";
                    currentGamePlaytimeSensor.Value = 0;
                    totalGamesSensor.Value = 0;
                    totalPlaytimeSensor.Value = 0;
                    recentPlaytimeSensor.Value = 0;
                    statusSensor.Value = "Initializing...";
                    detailsSensor.Value = "Loading Steam data...";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SensorManagementService] Error resetting Steam sensors: {ex.Message}");
                }
            }
        }
        
        #endregion
    }
}