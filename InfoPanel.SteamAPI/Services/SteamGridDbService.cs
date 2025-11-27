using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using craftersmine.SteamGridDBNet;
using InfoPanel.SteamAPI.Models;

namespace InfoPanel.SteamAPI.Services
{
    public class SteamGridDbService : IDisposable
    {
        private readonly ConfigurationService _configService;
        private readonly EnhancedLoggingService? _enhancedLogger;
        private SteamGridDb? _client;
        private bool _isInitialized;

        public SteamGridDbService(ConfigurationService configService, EnhancedLoggingService? enhancedLogger = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _enhancedLogger = enhancedLogger;
            InitializeClient();
        }

        private void InitializeClient()
        {
            try
            {
                if (!_configService.EnableSteamGridDb)
                {
                    _enhancedLogger?.LogInfo("SteamGridDbService", "Integration disabled in configuration");
                    return;
                }

                var apiKey = _configService.SteamGridDbApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _enhancedLogger?.LogWarning("SteamGridDbService", "API Key is missing");
                    return;
                }

                _client = new SteamGridDb(apiKey);
                _isInitialized = true;
                _enhancedLogger?.LogInfo("SteamGridDbService", "Client initialized successfully");
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("SteamGridDbService", "Failed to initialize client", ex);
            }
        }

        public bool IsAvailable => _isInitialized && _client != null;

        public async Task<string?> GetGridUrlAsync(int appId)
        {
            if (!IsAvailable) return null;

            try
            {
                var styles = ParseEnumFlags<SteamGridDbStyles>(_configService.SteamGridDbPreferredStyles) ?? SteamGridDbStyles.AllGrids;
                var formats = ParseEnumFlags<SteamGridDbFormats>(_configService.SteamGridDbPreferredMimeTypes) ?? SteamGridDbFormats.All;
                var types = ParseEnumFlags<SteamGridDbTypes>(_configService.SteamGridDbPreferredTypes) ?? SteamGridDbTypes.All;

                var grids = await _client!.GetGridsByPlatformGameIdAsync(
                    SteamGridDbGamePlatform.Steam,
                    appId,
                    nsfw: _configService.SteamGridDbIncludeNSFW,
                    humorous: _configService.SteamGridDbIncludeHumor,
                    epilepsy: _configService.SteamGridDbIncludeEpilepsy,
                    styles: styles,
                    dimensions: SteamGridDbDimensions.AllGrids,
                    formats: formats,
                    types: types
                );

                // Retry with relaxed constraints if no results found
                if ((grids == null || grids.Length == 0) && styles != SteamGridDbStyles.AllGrids)
                {
                    _enhancedLogger?.LogInfo("SteamGridDbService", $"No grids found with preferred styles for AppID {appId}. Retrying with all styles.");
                    grids = await _client!.GetGridsByPlatformGameIdAsync(
                        SteamGridDbGamePlatform.Steam,
                        appId,
                        nsfw: _configService.SteamGridDbIncludeNSFW,
                        humorous: _configService.SteamGridDbIncludeHumor,
                        epilepsy: _configService.SteamGridDbIncludeEpilepsy,
                        styles: SteamGridDbStyles.AllGrids,
                        dimensions: SteamGridDbDimensions.AllGrids,
                        formats: formats,
                        types: types
                    );
                }

                if (grids == null || grids.Length == 0) return null;

                // Prefer standard vertical box art (600x900) or standard horizontal (920x430, 460x215)
                // Sort by preference: 600x900 > 920x430 > 460x215 > Any other
                var preferredGrid = grids
                    .OrderByDescending(g => g.Width == 600 && g.Height == 900) // 1. Vertical Box Art
                    .ThenByDescending(g => g.Width == 920 && g.Height == 430)  // 2. Large Capsule
                    .ThenByDescending(g => g.Width == 460 && g.Height == 215)  // 3. Small Capsule
                    .FirstOrDefault();

                return preferredGrid?.FullImageUrl ?? grids.First().FullImageUrl;
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("SteamGridDbService", $"Failed to fetch grid for AppID {appId}", ex);
                return null;
            }
        }

        public async Task<string?> GetHeroUrlAsync(int appId)
        {
            if (!IsAvailable) return null;

            try
            {
                var styles = ParseEnumFlags<SteamGridDbStyles>(_configService.SteamGridDbPreferredStyles) ?? SteamGridDbStyles.AllHeroes;
                var formats = ParseEnumFlags<SteamGridDbFormats>(_configService.SteamGridDbPreferredMimeTypes) ?? SteamGridDbFormats.All;
                var types = ParseEnumFlags<SteamGridDbTypes>(_configService.SteamGridDbPreferredTypes) ?? SteamGridDbTypes.All;

                var heroes = await _client!.GetHeroesByPlatformGameIdAsync(
                    SteamGridDbGamePlatform.Steam,
                    appId,
                    nsfw: _configService.SteamGridDbIncludeNSFW,
                    humorous: _configService.SteamGridDbIncludeHumor,
                    epilepsy: _configService.SteamGridDbIncludeEpilepsy,
                    styles: styles,
                    dimensions: SteamGridDbDimensions.AllHeroes,
                    formats: formats,
                    types: types
                );

                // Retry with relaxed constraints if no results found
                if ((heroes == null || heroes.Length == 0) && styles != SteamGridDbStyles.AllHeroes)
                {
                    _enhancedLogger?.LogInfo("SteamGridDbService", $"No heroes found with preferred styles for AppID {appId}. Retrying with all styles.");
                    heroes = await _client!.GetHeroesByPlatformGameIdAsync(
                        SteamGridDbGamePlatform.Steam,
                        appId,
                        nsfw: _configService.SteamGridDbIncludeNSFW,
                        humorous: _configService.SteamGridDbIncludeHumor,
                        epilepsy: _configService.SteamGridDbIncludeEpilepsy,
                        styles: SteamGridDbStyles.AllHeroes,
                        dimensions: SteamGridDbDimensions.AllHeroes,
                        formats: formats,
                        types: types
                    );
                }

                if (heroes == null || heroes.Length == 0) return null;

                // Prefer standard hero dimensions (1920x620) as 4K is often overkill for dashboards
                var preferredHero = heroes
                    .OrderByDescending(h => h.Width == 1920 && h.Height == 620)   // 1. Standard Hero (Preferred)
                    .ThenByDescending(h => h.Width == 3840 && h.Height == 1240) // 2. 4K Hero (Fallback)
                    .FirstOrDefault();

                return preferredHero?.FullImageUrl ?? heroes.First().FullImageUrl;
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("SteamGridDbService", $"Failed to fetch hero for AppID {appId}", ex);
                return null;
            }
        }

        public async Task<string?> GetLogoUrlAsync(int appId)
        {
            if (!IsAvailable) return null;

            try
            {
                // Logos have specific styles (Official, White, Black, Custom) that differ from Grids
                // We ignore the shared preference to ensure we find a logo
                var styles = SteamGridDbStyles.AllLogos;
                var formats = SteamGridDbFormats.All;
                var types = ParseEnumFlags<SteamGridDbTypes>(_configService.SteamGridDbPreferredTypes) ?? SteamGridDbTypes.All;

                var logos = await _client!.GetLogosByPlatformGameIdAsync(
                    SteamGridDbGamePlatform.Steam,
                    appId,
                    nsfw: _configService.SteamGridDbIncludeNSFW,
                    humorous: _configService.SteamGridDbIncludeHumor,
                    epilepsy: _configService.SteamGridDbIncludeEpilepsy,
                    styles: styles,
                    formats: formats,
                    types: types
                );

                // Retry with relaxed constraints if no results found
                if ((logos == null || logos.Length == 0) && types != SteamGridDbTypes.All)
                {
                    _enhancedLogger?.LogInfo("SteamGridDbService", $"No logos found with preferred types for AppID {appId}. Retrying with all types.");
                    logos = await _client!.GetLogosByPlatformGameIdAsync(
                        SteamGridDbGamePlatform.Steam,
                        appId,
                        nsfw: _configService.SteamGridDbIncludeNSFW,
                        humorous: _configService.SteamGridDbIncludeHumor,
                        epilepsy: _configService.SteamGridDbIncludeEpilepsy,
                        styles: styles,
                        formats: formats,
                        types: SteamGridDbTypes.All
                    );
                }

                if (logos == null || logos.Length == 0) return null;

                // Logo Consistency Strategy:
                // 1. Prioritize "Official" logos with decent size (>200px)
                // 2. Fallback to any logo with decent size
                // 3. Fallback to any Official logo
                // 4. Prefer "Wide" logos (Aspect Ratio > 1.3)
                var preferredLogo = logos
                    .OrderByDescending(l => l.Style == SteamGridDbStyles.Official && l.Width > 200)
                    .ThenByDescending(l => l.Width > 200)
                    .ThenByDescending(l => l.Style == SteamGridDbStyles.Official)
                    .ThenByDescending(l => (double)l.Width / l.Height > 1.3)
                    .ThenByDescending(l => l.Width)
                    .FirstOrDefault();

                return preferredLogo?.FullImageUrl ?? logos.First().FullImageUrl;
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("SteamGridDbService", $"Failed to fetch logo for AppID {appId}", ex);
                return null;
            }
        }

        public async Task<string?> GetIconUrlAsync(int appId)
        {
            if (!IsAvailable) return null;

            try
            {
                // Icons have specific styles (Official, Custom) that differ from Grids
                // We ignore the shared preference to ensure we find an icon
                var styles = SteamGridDbStyles.AllIcons;
                var formats = SteamGridDbFormats.All;
                var types = ParseEnumFlags<SteamGridDbTypes>(_configService.SteamGridDbPreferredTypes) ?? SteamGridDbTypes.All;

                var icons = await _client!.GetIconsByPlatformGameIdAsync(
                    SteamGridDbGamePlatform.Steam,
                    appId,
                    nsfw: _configService.SteamGridDbIncludeNSFW,
                    humorous: _configService.SteamGridDbIncludeHumor,
                    epilepsy: _configService.SteamGridDbIncludeEpilepsy,
                    styles: styles,
                    formats: formats,
                    types: types
                );

                // Retry with relaxed constraints if no results found
                if ((icons == null || icons.Length == 0) && types != SteamGridDbTypes.All)
                {
                    _enhancedLogger?.LogInfo("SteamGridDbService", $"No icons found with preferred types for AppID {appId}. Retrying with all types.");
                    icons = await _client!.GetIconsByPlatformGameIdAsync(
                        SteamGridDbGamePlatform.Steam,
                        appId,
                        nsfw: _configService.SteamGridDbIncludeNSFW,
                        humorous: _configService.SteamGridDbIncludeHumor,
                        epilepsy: _configService.SteamGridDbIncludeEpilepsy,
                        styles: styles,
                        formats: formats,
                        types: SteamGridDbTypes.All
                    );
                }

                if (icons == null || icons.Length == 0) return null;

                // Prefer high-res square icons (1024x1024 or 512x512)
                var preferredIcon = icons
                    .OrderByDescending(i => i.Width == 1024 && i.Height == 1024) // 1. Max Res
                    .ThenByDescending(i => i.Width == 512 && i.Height == 512)    // 2. High Res
                    .ThenByDescending(i => i.Width)                              // 3. Largest available
                    .FirstOrDefault();

                return preferredIcon?.FullImageUrl ?? icons.First().FullImageUrl;
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("SteamGridDbService", $"Failed to fetch icon for AppID {appId}", ex);
                return null;
            }
        }

        private T? ParseEnumFlags<T>(string input) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(input) || input.Equals("Any", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            T result = default(T);
            bool foundAny = false;
            var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (Enum.TryParse<T>(part.Trim(), true, out var parsed))
                {
                    var currentVal = Convert.ToInt64(result);
                    var newVal = Convert.ToInt64(parsed);
                    var combined = currentVal | newVal;
                    result = (T)Enum.ToObject(typeof(T), combined);
                    foundAny = true;
                }
            }
            return foundAny ? result : null;
        }

        public void Dispose()
        {
            _client = null;
            _isInitialized = false;
        }
    }
}
