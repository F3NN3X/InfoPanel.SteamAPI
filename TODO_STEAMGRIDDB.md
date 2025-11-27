# SteamGridDB Integration Plan

## Phase 1: Configuration & Dependencies

- [x] **Add NuGet Package**: Install `craftersmine.SteamGridDB.Net` to the project.
- [x] **Update Configuration Template**: Add `[SteamGridDB Settings]` section to `InfoPanel.SteamAPI.dll.ini` with the following fields:
  - `ApiKey`: The SteamGridDB API Key.
  - `Enabled`: Master switch to enable/disable this integration.
  - `PreferredStyles`: Comma-separated list (e.g., `Alternate,Blurred,Material`).
  - `PreferredMimeTypes`: Comma-separated list (e.g., `Png,Webp`).
  - `PreferredTypes`: `Static`, `Animated`, or `Any`.
  - `IncludeNSFW`: `true`/`false`.
  - `IncludeHumor`: `true`/`false`.
  - `IncludeEpilepsy`: `true`/`false` (for animated).
  - `MinimumScore`: Integer (e.g., `5`).
- [x] **Update ConfigurationService**:
  - Add properties to `ConfigurationService.cs` to read and expose these settings.
  - Implement parsing logic for Enums (Styles, MimeTypes).

## Phase 2: Service Implementation

- [x] **Create SteamGridDbService**:
  - Create `Services/SteamGridDbService.cs`.
  - Implement constructor taking `ConfigurationService` and `EnhancedLoggingService`.
  - Initialize `SteamGridDb` client.
- [x] **Implement Fetch Methods**:
  - `GetGameImagesAsync(uint appId)`: Orchestrate fetching of Grid, Hero, Logo, and Icon.
  - `GetGridAsync(uint appId)`: Fetch box art with configured filters.
  - `GetHeroAsync(uint appId)`: Fetch banner/hero with configured filters.
  - `GetLogoAsync(uint appId)`: Fetch logo with configured filters.
  - `GetIconAsync(uint appId)`: Fetch icon with configured filters.
- [x] **Implement Filtering Logic**:
  - Map configuration settings to `SteamGridDB.Net` filter parameters.
  - Implement sorting/selection logic (e.g., pick top score from filtered results).

## Phase 3: Integration

- [x] **Update SteamAPIMain**:
  - Initialize `SteamGridDbService` in `Load()`.
  - Pass the service to `PlayerDataService`.
- [x] **Update PlayerDataService**:
  - Modify `GetGameIconUrlAsync`, `GetGameLogoUrlAsync`, `GetGameBannerUrlAsync`.
  - Implement the priority logic:
        1. Check SteamGridDB (if enabled and configured).
        2. Fallback to Official Steam API (ClientIcon, etc.).
        3. Fallback to Steam Community/CDN (existing logic).
- [x] **Update Models**:
  - Ensure `SteamGame` model can hold URLs from different sources if needed (currently just strings, so should be fine).

## Phase 4: Testing & Refinement

- [ ] **Verify Configuration**: Ensure settings are read correctly from INI.
- [ ] **Test Image Retrieval**:
  - Test with a popular game (e.g., Witcher 3 `292030`) to verify high-quality assets.
  - Test with a game known to have missing official assets.
- [ ] **Verify Fallback**: Disable SteamGridDB in config and ensure original logic still works.
- [ ] **Check Performance**: Ensure API calls don't block the UI or monitoring loop (use async properly).
