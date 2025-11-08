using InfoPanel.SteamAPI.Services;
using InfoPanel.SteamAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Tests
{
    /// <summary>
    /// Simple test class to verify Steam API integration
    /// This would normally be run as part of InfoPanel, but we can test the core logic
    /// </summary>
    internal class SteamApiTest
    {
        public static async Task TestConfigurationService()
        {
            Console.WriteLine("=== Testing ConfigurationService ===");
            
            try
            {
                var configService = new ConfigurationService(@"e:\GitHub\MyRepos\InfoPanel.SteamAPI\test-config.ini");
                
                Console.WriteLine($"Steam API Key: {(string.IsNullOrEmpty(configService.SteamApiKey) ? "Not set" : "Set")}");
                Console.WriteLine($"Steam ID64: {configService.SteamId64}");
                Console.WriteLine($"Update Interval: {configService.UpdateIntervalSeconds}s");
                Console.WriteLine($"Profile Monitoring: {configService.EnableProfileMonitoring}");
                Console.WriteLine($"Library Monitoring: {configService.EnableLibraryMonitoring}");
                Console.WriteLine($"Configuration Valid: {configService.ValidateConfiguration()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Configuration test failed: {ex.Message}");
            }
        }
        
        public static async Task TestSteamApiService(string apiKey, string steamId64)
        {
            Console.WriteLine("=== Testing SteamApiService ===");
            
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "<your-steam-api-key-here>")
            {
                Console.WriteLine("Steam API Key not configured - skipping API tests");
                return;
            }
            
            try
            {
                using var steamApiService = new SteamApiService(apiKey, steamId64);
                
                // Test connection
                Console.WriteLine("Testing connection...");
                var isValid = await steamApiService.TestConnectionAsync();
                Console.WriteLine($"Connection test: {(isValid ? "SUCCESS" : "FAILED")}");
                
                if (!isValid)
                {
                    Console.WriteLine("Cannot proceed with API tests - connection failed");
                    return;
                }
                
                // Test player summary
                Console.WriteLine("Getting player summary...");
                var playerSummary = await steamApiService.GetPlayerSummaryAsync();
                if (playerSummary?.Response?.Players?.Any() == true)
                {
                    var player = playerSummary.Response.Players.First();
                    Console.WriteLine($"Player: {player.PersonaName}");
                    Console.WriteLine($"Status: {SteamApiService.GetPersonaStateString(player.PersonaState)}");
                    if (!string.IsNullOrEmpty(player.GameExtraInfo))
                    {
                        Console.WriteLine($"Current Game: {player.GameExtraInfo}");
                    }
                }
                
                // Test owned games
                Console.WriteLine("Getting owned games...");
                var ownedGames = await steamApiService.GetOwnedGamesAsync();
                if (ownedGames?.Response?.Games?.Any() == true)
                {
                    var games = ownedGames.Response.Games;
                    Console.WriteLine($"Total Games: {games.Count}");
                    
                    var totalPlaytime = games.Sum(g => g.PlaytimeForever) / 60.0;
                    Console.WriteLine($"Total Playtime: {totalPlaytime:F1} hours");
                    
                    var mostPlayed = games.OrderByDescending(g => g.PlaytimeForever).First();
                    Console.WriteLine($"Most Played: {mostPlayed.Name} ({mostPlayed.PlaytimeForever / 60.0:F1}h)");
                }
                else
                {
                    Console.WriteLine("No owned games found or API error");
                }
                
                // Test Steam level
                Console.WriteLine("Getting Steam level...");
                var steamLevel = await steamApiService.GetSteamLevelAsync();
                Console.WriteLine($"Steam Level: {steamLevel?.Response?.PlayerLevel ?? 0}");
                
                // Test recent games
                Console.WriteLine("Getting recent games...");
                var recentGames = await steamApiService.GetRecentlyPlayedGamesAsync();
                var recentGameCount = recentGames?.Response?.Games?.Count ?? 0;
                Console.WriteLine($"Recent Games: {recentGameCount}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Steam API test failed: {ex.Message}");
            }
        }
        
        public static void TestSteamDataModel()
        {
            Console.WriteLine("=== Testing SteamData Model ===");
            
            try
            {
                // Test normal data
                var steamData = new SteamData
                {
                    PlayerName = "TestPlayer",
                    SteamLevel = 42,
                    OnlineState = "Online",
                    CurrentGameName = "Test Game",
                    CurrentGameAppId = 12345,
                    TotalGamesOwned = 150,
                    TotalLibraryPlaytimeHours = 500.5,
                    RecentPlaytimeHours = 25.3
                };
                
                Console.WriteLine($"Display Status: {steamData.GetDisplayStatus()}");
                Console.WriteLine($"Is Online: {steamData.IsOnline()}");
                Console.WriteLine($"Is In Game: {steamData.IsInGame()}");
                Console.WriteLine($"Activity Level: {steamData.GetActivityLevel()}");
                Console.WriteLine($"Valid: {steamData.IsValid()}");
                Console.WriteLine($"ToString: {steamData}");
                
                // Test error data
                var errorData = new SteamData("Test error message");
                Console.WriteLine($"Error Data Valid: {errorData.IsValid()}");
                Console.WriteLine($"Error ToString: {errorData}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SteamData test failed: {ex.Message}");
            }
        }
    }
}