# Domain Architecture Refactoring Plan

**Branch:** `refactor-domain-architecture`  
**Started:** November 12, 2025  
**Status:** 🚧 In Progress

---

## 📊 Executive Summary

Refactoring the monolithic `MonitoringService` (1082 lines) and `SensorManagementService` (1037 lines) into domain-driven architecture with separate monitoring and sensor services for Player, Social, and Library domains.

### Goals
- ✅ Improve maintainability (smaller, focused files)
- ✅ Enhance testability (independent domain testing)
- ✅ Simplify troubleshooting (clear domain boundaries)
- ✅ Enable scalability (easy to add new domains)

### Metrics
- **Before:** 2119 lines in 2 monolithic files
- **After:** ~1850 lines in 8 focused files (~231 lines avg)
- **Net Reduction:** 269 lines (removing duplication)

---

## 🎯 Current Architecture (Before)

### 2-Layer Monolithic Structure
```
MonitoringService (1082 lines)
├── Coordinates 3 timers (Player, Social, Library)
├── Calls specialized data services
├── Caches session data
├── Fires DataUpdated events
└── Handles all orchestration (MIXED CONCERNS)

SensorManagementService (1037 lines)
├── Updates ALL sensor types
├── Mixed responsibilities (player, social, library, enhanced gaming)
└── 1000+ lines of mixed update logic (HARD TO MAINTAIN)
```

### Data Collection Services (Already Separated) ✅
- `PlayerDataService.cs` - Player profile & game state
- `SocialDataService.cs` - Friends data
- `LibraryDataService.cs` - Game library
- `GameStatsService.cs` - Game statistics

**Problem:** Monitoring and sensor management are still monolithic, making it hard to maintain, test, and debug domain-specific logic.

---

## 🎯 Target Architecture (After)

### 3-Layer Domain-Driven Design
```
┌─────────────────────────────────────────────────────────┐
│              MAIN PLUGIN (Orchestrator)                  │
│              InfoPanel.SteamAPI.cs                       │
│  - Coordinates domain services                           │
│  - Manages shared session cache                          │
│  - Subscribes to domain events                           │
└─────────────────────────────────────────────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        │                 │                 │
┌───────▼────────┐ ┌──────▼──────┐ ┌──────▼──────┐
│ PLAYER DOMAIN  │ │SOCIAL DOMAIN│ │LIBRARY DOMAIN│
│                │ │             │ │              │
│ [MONITORING]   │ │[MONITORING] │ │[MONITORING]  │
│ PlayerMonitor  │ │SocialMonitor│ │LibraryMonitor│
│ Service        │ │Service      │ │Service       │
│ (~350 lines)   │ │(~250 lines) │ │(~300 lines)  │
│                │ │             │ │              │
│ ├─Timer (1s)   │ │├─Timer (15s)│ │├─Timer (45s) │
│ ├─Data fetch   │ │├─Data fetch │ │├─Data fetch  │
│ ├─Cache mgmt   │ │├─Cache mgmt │ │├─Cache mgmt  │
│ └─Fire events  │ │└─Fire events│ │└─Fire events │
│                │ │             │ │              │
│ [SENSORS]      │ │[SENSORS]    │ │[SENSORS]     │
│ PlayerSensor   │ │SocialSensor │ │LibrarySensor │
│ Service        │ │Service      │ │Service       │
│ (~300 lines)   │ │(~200 lines) │ │(~250 lines)  │
│                │ │             │ │              │
│ ├─Profile      │ │├─Friends    │ │├─Library     │
│ ├─Current game │ │├─Activity   │ │├─Recent games│
│ ├─Session time │ │├─Tables     │ │├─Tables      │
│ └─Images       │ │└─Formatting │ │└─Formatting  │
└────────────────┘ └─────────────┘ └──────────────┘
```

---

## 📋 Refactoring Phases

### ✅ Phase 0: Preparation
- [x] Create `refactor-domain-architecture` branch
- [x] Document refactoring plan
- [ ] Create progress tracking todos

---

### 🚧 Phase 1: Create Domain Monitor Services

#### 1.1 PlayerMonitoringService.cs (~350 lines)
**Location:** `Services/Monitoring/PlayerMonitoringService.cs`

**Responsibilities:**
- Player timer (1 second interval)
- Call `PlayerDataService` for data collection
- Manage player sensors via `PlayerSensorService`
- Manage session sensors (current session, avg session)
- Session data caching (for sharing with other domains)
- Fire `PlayerDataUpdated` event

**Key Methods:**
```csharp
public class PlayerMonitoringService : IDisposable
{
    // Public API
    public event EventHandler<PlayerDataEventArgs>? PlayerDataUpdated;
    public SessionDataCache GetSessionCache();
    public void StartMonitoring();
    public void StopMonitoring();
    
    // Internal
    private void OnPlayerTimerElapsed(object? state);
    private void CacheSessionData(PlayerData data);
    private async Task CollectAndUpdatePlayerDataAsync();
}
```

**Status:** ⬜ Not Started

---

#### 1.2 SocialMonitoringService.cs (~250 lines)
**Location:** `Services/Monitoring/SocialMonitoringService.cs`

**Responsibilities:**
- Social timer (15 second interval)
- Call `SocialDataService` for data collection
- Manage social sensors via `SocialSensorService`
- Preserve cached session data from player domain
- Fire `SocialDataUpdated` event

**Key Methods:**
```csharp
public class SocialMonitoringService : IDisposable
{
    // Public API
    public event EventHandler<SocialDataEventArgs>? SocialDataUpdated;
    public void SetSessionCache(SessionDataCache cache);
    public void StartMonitoring();
    public void StopMonitoring();
    
    // Internal
    private void OnSocialTimerElapsed(object? state);
    private async Task CollectAndUpdateSocialDataAsync();
}
```

**Status:** ⬜ Not Started

---

#### 1.3 LibraryMonitoringService.cs (~300 lines)
**Location:** `Services/Monitoring/LibraryMonitoringService.cs`

**Responsibilities:**
- Library timer (45 second interval)
- Call `LibraryDataService` for data collection
- Manage library sensors via `LibrarySensorService`
- Preserve cached session data from player domain
- Fire `LibraryDataUpdated` event

**Key Methods:**
```csharp
public class LibraryMonitoringService : IDisposable
{
    // Public API
    public event EventHandler<LibraryDataEventArgs>? LibraryDataUpdated;
    public void SetSessionCache(SessionDataCache cache);
    public void StartMonitoring();
    public void StopMonitoring();
    
    // Internal
    private void OnLibraryTimerElapsed(object? state);
    private async Task CollectAndUpdateLibraryDataAsync();
}
```

**Status:** ⬜ Not Started

---

### 🚧 Phase 2: Create Domain Sensor Services

#### 2.1 PlayerSensorService.cs (~300 lines)
**Location:** `Services/Sensors/PlayerSensorService.cs`

**Responsibilities:**
- Update player profile sensors (name, level, online status)
- Update current game sensors (name, playtime)
- Update session time sensors (current, start time, average)
- Update image URL sensors (profile, game banner)
- Format player-specific data

**Key Methods:**
```csharp
public class PlayerSensorService
{
    public void UpdateProfileSensors(PlayerProfileSensors sensors, PlayerData data);
    public void UpdateCurrentGameSensors(CurrentGameSensors sensors, PlayerData data);
    public void UpdateSessionTimeSensors(SessionTimeSensors sensors, PlayerData data);
    public void UpdateImageUrlSensors(ImageUrlSensors sensors, PlayerData data);
    
    private string FormatSessionTime(int minutes);
    private string FormatGameStatus(PlayerData data);
}
```

**Status:** ⬜ Not Started

---

#### 2.2 SocialSensorService.cs (~200 lines)
**Location:** `Services/Sensors/SocialSensorService.cs`

**Responsibilities:**
- Update friends sensors (total, online, in game)
- Update social activity sensors
- Build friends activity table
- Format social-specific data

**Key Methods:**
```csharp
public class SocialSensorService
{
    public void UpdateFriendsSensors(FriendsSensors sensors, SocialData data);
    public void UpdateSocialActivitySensors(SocialActivitySensors sensors, SocialData data);
    public DataTable BuildFriendsActivityTable(SocialData data);
    
    private string FormatFriendActivity(FriendActivity activity);
}
```

**Status:** ⬜ Not Started

---

#### 2.3 LibrarySensorService.cs (~250 lines)
**Location:** `Services/Sensors/LibrarySensorService.cs`

**Responsibilities:**
- Update library statistics sensors (total games, playtime)
- Update recent games sensors
- Build recent games table
- Build game statistics table
- Format library-specific data

**Key Methods:**
```csharp
public class LibrarySensorService
{
    public void UpdateLibraryStatsSensors(LibraryStatsSensors sensors, LibraryData data);
    public void UpdateRecentGamesSensors(RecentGamesSensors sensors, LibraryData data);
    public DataTable BuildRecentGamesTable(LibraryData data);
    public DataTable BuildGameStatisticsTable(LibraryData data);
    
    private string FormatPlaytime(double hours);
    private string FormatGameCount(int count);
}
```

**Status:** ⬜ Not Started

---

### 🚧 Phase 3: Create Shared Infrastructure

#### 3.1 SessionDataCache.cs (~50 lines)
**Location:** `Models/SessionDataCache.cs`

**Purpose:** Shared cache object passed between domains to preserve session data

**Structure:**
```csharp
public class SessionDataCache
{
    // Current session data
    public int CurrentSessionMinutes { get; set; }
    public DateTime? SessionStartTime { get; set; }
    
    // Historical session data
    public double AverageSessionMinutes { get; set; }
    
    // Last played game data
    public string? LastPlayedGameName { get; set; }
    public int LastPlayedGameAppId { get; set; }
    public string? LastPlayedGameBannerUrl { get; set; }
    
    // Metadata
    public DateTime LastUpdated { get; set; }
    
    // Thread safety
    public object Lock { get; } = new object();
}
```

**Status:** ⬜ Not Started

---

#### 3.2 DomainMonitorBase.cs (Optional - ~150 lines)
**Location:** `Services/Monitoring/DomainMonitorBase.cs`

**Purpose:** Shared base class for all monitoring services to reduce duplication

**Structure:**
```csharp
public abstract class DomainMonitorBase : IDisposable
{
    // Shared fields
    protected System.Threading.Timer _timer;
    protected volatile bool _isMonitoring;
    protected volatile int _cycleCount;
    protected readonly SemaphoreSlim _apiSemaphore;
    protected readonly EnhancedLoggingService? _logger;
    protected readonly ConfigurationService _configService;
    
    // Abstract methods (domain-specific)
    protected abstract Task OnTimerElapsedAsync();
    protected abstract string GetDomainName();
    protected abstract int GetTimerInterval();
    
    // Shared methods
    public virtual void StartMonitoring();
    public virtual void StopMonitoring();
    public virtual void Dispose();
    
    // Logging helpers
    protected void LogOperationStart(string operation);
    protected void LogOperationEnd(string operation, TimeSpan duration, bool success);
}
```

**Status:** ⬜ Not Started (Optional - can skip if unnecessary)

---

### 🚧 Phase 4: Update Main Plugin Orchestrator

#### 4.1 InfoPanel.SteamAPI.cs (Modified)
**Changes Required:**

**Before (Current):**
```csharp
private MonitoringService? _monitoringService;
private SensorManagementService? _sensorService;

// Single DataUpdated event handler
private void OnDataUpdated(object? sender, DataUpdatedEventArgs e)
{
    // Mixed logic for all domains
}
```

**After (New):**
```csharp
// Domain-specific monitor services
private PlayerMonitoringService? _playerMonitor;
private SocialMonitoringService? _socialMonitor;
private LibraryMonitoringService? _libraryMonitor;

// Shared session cache
private SessionDataCache? _sessionCache;

// Domain-specific event handlers
private void OnPlayerDataUpdated(object? sender, PlayerDataEventArgs e)
{
    // Update session cache
    _sessionCache = _playerMonitor.GetSessionCache();
    
    // Share cache with other domains
    _socialMonitor?.SetSessionCache(_sessionCache);
    _libraryMonitor?.SetSessionCache(_sessionCache);
    
    // Update player sensors (already done in PlayerMonitoringService)
}

private void OnSocialDataUpdated(object? sender, SocialDataEventArgs e)
{
    // Update social sensors (already done in SocialMonitoringService)
}

private void OnLibraryDataUpdated(object? sender, LibraryDataEventArgs e)
{
    // Update library sensors (already done in LibraryMonitoringService)
}
```

**Initialization Changes:**
```csharp
private void InitializeMonitoring()
{
    // Create shared cache
    _sessionCache = new SessionDataCache();
    
    // Initialize domain monitors
    _playerMonitor = new PlayerMonitoringService(
        _configService, _steamApiService, _sessionTracker, 
        _playerDataService, _enhancedLogger);
    
    _socialMonitor = new SocialMonitoringService(
        _configService, _steamApiService, 
        _socialDataService, _enhancedLogger);
    
    _libraryMonitor = new LibraryMonitoringService(
        _configService, _steamApiService, 
        _libraryDataService, _enhancedLogger);
    
    // Subscribe to domain events
    _playerMonitor.PlayerDataUpdated += OnPlayerDataUpdated;
    _socialMonitor.SocialDataUpdated += OnSocialDataUpdated;
    _libraryMonitor.LibraryDataUpdated += OnLibraryDataUpdated;
    
    // Start monitoring
    _playerMonitor.StartMonitoring();
    _socialMonitor.StartMonitoring();
    _libraryMonitor.StartMonitoring();
}
```

**Status:** ⬜ Not Started

---

### 🚧 Phase 5: Testing & Migration

#### 5.1 Create Feature Flag (Optional)
Add config option to switch between old and new architecture:
```ini
[Experimental]
UseDomainArchitecture=true
```

**Status:** ⬜ Not Started (Optional)

---

#### 5.2 Testing Checklist
- [ ] **Player Domain**
  - [ ] Profile sensors update correctly (1s interval)
  - [ ] Current game sensors update when playing
  - [ ] Session sensors track time correctly
  - [ ] Banner URL updates for current/last played game
  - [ ] Session cache populates correctly

- [ ] **Social Domain**
  - [ ] Friends sensors update correctly (15s interval)
  - [ ] Friends activity table builds correctly
  - [ ] Session data preserved from player cache
  - [ ] No interference with player sensors

- [ ] **Library Domain**
  - [ ] Library stats sensors update correctly (45s interval)
  - [ ] Recent games table builds correctly
  - [ ] Game statistics table builds correctly
  - [ ] Session data preserved from player cache
  - [ ] No interference with player/social sensors

- [ ] **Integration**
  - [ ] All timers fire at correct intervals
  - [ ] Session cache shared correctly between domains
  - [ ] No sensor overwrites or conflicts
  - [ ] Memory usage acceptable
  - [ ] CPU usage acceptable

- [ ] **Edge Cases**
  - [ ] Cold start (no game running)
  - [ ] Game launch during monitoring
  - [ ] Game quit during monitoring
  - [ ] Game focus/unfocus
  - [ ] InfoPanel restart with game running
  - [ ] Steam API errors handled gracefully

**Status:** ⬜ Not Started

---

#### 5.3 Remove Old Services
Once new architecture is validated:
- [ ] Remove `MonitoringService.cs` (1082 lines)
- [ ] Remove `SensorManagementService.cs` (1037 lines)
- [ ] Remove any unused helper methods
- [ ] Update documentation
- [ ] Remove feature flag (if used)

**Status:** ⬜ Not Started

---

## 📁 Final File Structure

```
InfoPanel.SteamAPI/
├── Models/
│   ├── SteamData.cs (existing)
│   ├── PlayerData.cs (existing)
│   ├── SocialData.cs (existing)
│   ├── LibraryData.cs (existing)
│   └── SessionDataCache.cs (NEW) ⬜
│
├── Services/
│   ├── Configuration/
│   │   └── ConfigurationService.cs (existing)
│   │
│   ├── DataCollection/ (existing - no changes)
│   │   ├── PlayerDataService.cs ✅
│   │   ├── SocialDataService.cs ✅
│   │   ├── LibraryDataService.cs ✅
│   │   ├── GameStatsService.cs ✅
│   │   └── SteamApiService.cs ✅
│   │
│   ├── Monitoring/ (NEW folder) ⬜
│   │   ├── DomainMonitorBase.cs (NEW - optional) ⬜
│   │   ├── PlayerMonitoringService.cs (NEW) ⬜
│   │   ├── SocialMonitoringService.cs (NEW) ⬜
│   │   └── LibraryMonitoringService.cs (NEW) ⬜
│   │
│   ├── Sensors/ (NEW folder) ⬜
│   │   ├── PlayerSensorService.cs (NEW) ⬜
│   │   ├── SocialSensorService.cs (NEW) ⬜
│   │   └── LibrarySensorService.cs (NEW) ⬜
│   │
│   ├── Logging/
│   │   ├── EnhancedLoggingService.cs (existing)
│   │   └── FileLoggingService.cs (existing)
│   │
│   ├── Session/
│   │   └── SessionTrackingService.cs (existing)
│   │
│   └── TO REMOVE (after migration):
│       ├── MonitoringService.cs (1082 lines) ⬜ DELETE AFTER PHASE 5
│       └── SensorManagementService.cs (1037 lines) ⬜ DELETE AFTER PHASE 5
│
└── InfoPanel.SteamAPI.cs (MODIFIED in Phase 4) ⬜
```

---

## 📊 Progress Tracking

### Overall Progress: 0% Complete

| Phase | Status | Progress | Files |
|-------|--------|----------|-------|
| Phase 0: Preparation | 🟢 In Progress | 50% | 1/2 |
| Phase 1: Monitor Services | ⬜ Not Started | 0% | 0/3 |
| Phase 2: Sensor Services | ⬜ Not Started | 0% | 0/3 |
| Phase 3: Infrastructure | ⬜ Not Started | 0% | 0/2 |
| Phase 4: Plugin Update | ⬜ Not Started | 0% | 0/1 |
| Phase 5: Testing & Migration | ⬜ Not Started | 0% | 0/1 |

### Estimated Time Remaining: 4-6 hours

---

## 🎯 Benefits Realized (Post-Refactoring)

### Maintainability
- [ ] ✅ Smaller files (avg 231 lines vs 1000+ lines)
- [ ] ✅ Clear domain boundaries (player/social/library)
- [ ] ✅ Easy to locate domain-specific code
- [ ] ✅ Reduced cognitive load when making changes

### Testability
- [ ] ✅ Unit test each domain independently
- [ ] ✅ Mock one domain while testing another
- [ ] ✅ Integration tests easier to write
- [ ] ✅ Test coverage increased

### Troubleshooting
- [ ] ✅ Logs clearly show which domain has issues
- [ ] ✅ Can disable one domain without breaking others
- [ ] ✅ Stack traces point to specific domain code
- [ ] ✅ Debugging time reduced

### Scalability
- [ ] ✅ Easy to add new domains (e.g., AchievementMonitor)
- [ ] ✅ Can optimize each domain's timing independently
- [ ] ✅ Domain-specific caching strategies possible
- [ ] ✅ Parallel development on different domains

---

## 📝 Notes & Decisions

### Design Decisions

#### 1. Why Domain-Driven Design?
- Clear separation of concerns (player, social, library)
- Each domain owns its data, timing, and sensors
- Natural alignment with InfoPanel's plugin architecture
- Easier to reason about and maintain

#### 2. Why Separate Monitoring and Sensor Services?
- **Monitoring Services**: Handle timers, data collection, orchestration
- **Sensor Services**: Handle UI updates, formatting, display logic
- Clear separation of concerns (business logic vs presentation)
- Easier to test each layer independently

#### 3. Why Keep Data Services Separate?
- Data services (PlayerDataService, etc.) are already well-separated
- Focus on API calls and data transformation
- No need to change what's already working well

#### 4. Session Cache Sharing Strategy
- Player domain owns the session cache (it's player-specific data)
- Player updates cache every cycle
- Social/Library read cache when needed
- Immutable cache objects prevent race conditions

#### 5. Base Class vs Interface?
- `DomainMonitorBase` is optional but recommended
- Reduces duplication across monitor services
- Provides consistent logging and error handling
- Can be skipped if keeping services fully independent is preferred

### Technical Considerations

#### Thread Safety
- Each domain has its own timer (no shared state)
- Session cache uses lock for thread-safe access
- API semaphore shared across domains (rate limiting)

#### Performance
- No performance regression expected
- Potential improvement from better cache locality
- Domain-specific optimizations easier to implement

#### Backward Compatibility
- Plugin API unchanged (sensors remain the same)
- InfoPanel won't notice the internal refactoring
- Configuration file unchanged

---

## 🔧 Development Commands

### Branch Management
```bash
# Switch to refactor branch
git checkout refactor-domain-architecture

# View current status
git status

# Commit progress
git add .
git commit -m "Phase X: [description]"

# Push to remote
git push -u origin refactor-domain-architecture
```

### Build & Test
```bash
# Build solution
dotnet build -c Release

# Run tests (if available)
dotnet test

# Deploy to InfoPanel
# Copy from: InfoPanel.SteamAPI\bin\Release\net8.0-windows\InfoPanel.SteamAPI-v1.2.0\InfoPanel.SteamAPI\
# To: c:\ProgramData\InfoPanel\plugins\InfoPanel.SteamAPI\
```

---

## 📚 References

- [InfoPanel Plugin Documentation](../InfoPanel_PluginDocumentation.md)
- [Domain-Driven Design Principles](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Separation of Concerns](https://en.wikipedia.org/wiki/Separation_of_concerns)

---

## ✅ Completion Checklist

- [ ] All new files created
- [ ] Old services removed
- [ ] All tests passing
- [ ] Documentation updated
- [ ] Code review completed
- [ ] Merged to master
- [ ] Feature flag removed (if used)
- [ ] Performance validated
- [ ] Memory usage validated
- [ ] Production deployment successful

---

**Last Updated:** November 12, 2025  
**Branch:** `refactor-domain-architecture`  
**Next Step:** Phase 1 - Create PlayerMonitoringService.cs
