# Optimization and Performance Plan

## Objective

Maximize the efficiency, responsiveness, and stability of the InfoPanel.SteamAPI plugin by reducing resource usage, minimizing API calls, and optimizing data processing.

## 1. Critical Issues Identified

### A. `HttpClient` Instantiation

- **Issue**: `PlayerDataService.GetGameBannerUrlAsync` creates a new `HttpClient` instance for every call.
- **Impact**: Potential socket exhaustion and unnecessary overhead.
- **Fix**: Use a shared `HttpClient` instance (likely in `SteamApiService` or a new `HttpService`).

### B. Inefficient API Usage (`GetOwnedGames`)

- **Issue**: `GetGameTotalPlaytimeAsync` fetches the *entire* list of owned games (potentially thousands) every 1-3 seconds while in-game to find playtime for *one* game.
- **Impact**: Massive unnecessary data transfer and CPU usage for parsing JSON.
- **Fix**: Cache the `OwnedGames` list with a reasonable TTL (e.g., 5-10 minutes).

### C. Redundant API Calls (`SteamLevel`)

- **Issue**: `GetSteamLevelAsync` is called every cycle (1-3 seconds). Steam level changes very rarely.
- **Impact**: Wasted API calls.
- **Fix**: Cache Steam Level with a long TTL (e.g., 15-30 minutes).

### D. Network Latency in UI Thread (`GetGameBannerUrlAsync`)

- **Issue**: Performs a `HEAD` request to verify image existence every time.
- **Impact**: Adds network latency to the update cycle.
- **Fix**: Cache the valid URL after the first successful check.

## 2. Optimization Strategy

### Phase 1: Caching Layer

Implement a robust caching mechanism for:

- `SteamLevel` (TTL: 15 min)
- `OwnedGames` (TTL: 10 min)
- `GameBannerUrl` (TTL: Permanent for session)
- `GameSchema` (Already partially implemented, review for completeness)

### Phase 2: Resource Management

- Refactor `HttpClient` usage to be a singleton or shared service.
- Review `SemaphoreSlim` usage to ensure we aren't blocking unnecessarily.

### Phase 3: Memory & Allocations

- Review `PlayerData` and other DTOs to see if we can reduce allocations (e.g., reuse objects where safe).
- Optimize string formatting in `SensorService` classes.

## 3. Execution Plan

1. **Refactor `SteamApiService`**: Add internal caching for `GetOwnedGames` and `GetSteamLevel`.
2. **Fix `HttpClient`**: Introduce a shared client.
3. **Optimize `PlayerDataService`**: Use the cached API methods and fix the banner URL logic.
4. **Review `MonitoringServices`**: Ensure timers are efficient and handle skipped cycles correctly.

## 4. Verification

- Monitor memory usage and CPU.
- Verify API call frequency (should drop significantly).
- Ensure UI remains responsive.
