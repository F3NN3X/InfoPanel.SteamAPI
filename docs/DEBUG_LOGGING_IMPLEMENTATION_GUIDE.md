# Debug Logging Implementation Guide

## 📋 **Overview**

This document provides a comprehensive guide on how the debug logging feature is implemented in InfoPanel.RTSS and how to adapt it for other InfoPanel plugins. The system provides intelligent file-based logging with throttling, batching, log rotation, and user-configurable debug modes.

---

## 🏗️ **Architecture Overview**

### **Core Components**

1. **`FileLoggingService`** - Main logging service with advanced features
2. **`ConfigurationService`** - Reads debug configuration from INI file  
3. **INI Configuration** - User-controllable debug toggle
4. **Service Integration** - Dependency injection pattern throughout plugin

### **Key Features**

✅ **User-Controllable**: Toggle via INI file (`debug=true/false`)  
✅ **Intelligent Throttling**: Prevents log spam while preserving important messages  
✅ **Batched Writing**: Performance-optimized with 500ms flush intervals  
✅ **Log Rotation**: Automatic file rotation at 5MB with 3 backup files  
✅ **Log Levels**: Debug, Info, Warning, Error with filtering  
✅ **Thread-Safe**: Lock synchronization for concurrent access  
✅ **Memory Management**: Buffer limits and disposal patterns  

---

## 📁 **File Structure & Integration**

### **Required Files**

```
YourPlugin/
├── Services/
│   ├── FileLoggingService.cs     ← Core logging implementation
│   ├── ConfigurationService.cs   ← INI file reading (debug toggle)
│   └── YourMainService.cs         ← Services that need logging
├── YourPlugin.cs                  ← Main plugin class
├── YourPlugin.ini                 ← Configuration template
└── PluginInfo.ini                 ← Plugin metadata
```

### **Service Dependencies**

```
ConfigurationService (reads INI) 
        ↓
FileLoggingService (logging engine)
        ↓  
YourMainServices (business logic with logging)
        ↓
YourPlugin.cs (main plugin class)
```

---

## 🔧 **Implementation Guide**

### **Step 1: Copy Core Services**

#### **FileLoggingService.cs**

Copy the complete `FileLoggingService.cs` from InfoPanel.RTSS with these adaptations:

**Required Changes:**

```csharp
namespace YourPlugin.Services  // Change namespace
{
    public class FileLoggingService : IDisposable
    {
        // ... (copy complete implementation) ...
        
        private void AddLogEntry(LogLevel level, string message)
        {
            // Change debug check to your plugin's config
            if (!_configService.IsDebugEnabled || string.IsNullOrEmpty(_logFilePath) || _disposed || level < _minimumLogLevel)
                return;
            
            // ... rest unchanged ...
            
            string logLine = $"[{timestamp}] [YourPlugin] [{levelStr}] {message}";  // Add plugin identifier
        }
        
        public FileLoggingService(ConfigurationService configService)
        {
            _configService = configService;
            
            // ... initialization code unchanged ...
            
            // Change log file name
            _logFilePath = Path.Combine(pluginDirectory, "your-plugin-debug.log");
            
            if (_configService.IsDebugEnabled)
            {
                AddLogEntry(LogLevel.Info, "=== YourPlugin Debug Log Started ===");  // Update startup message
            }
        }
    }
}
```

#### **ConfigurationService.cs (Debug Section)**

Add debug configuration support to your existing ConfigurationService:

```csharp
namespace YourPlugin.Services
{
    public class ConfigurationService
    {
        // ... your existing config code ...
        
        /// <summary>
        /// Gets whether debug logging is enabled from [Debug] section
        /// </summary>
        public bool IsDebugEnabled => GetBoolValue("Debug", "debug", false);
        
        private bool GetBoolValue(string section, string key, bool defaultValue)
        {
            if (_configData.TryGetValue(section, out var sectionData) &&
                sectionData.TryGetValue(key, out var value))
            {
                return bool.TryParse(value, out var result) && result;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Logs current configuration settings for debugging
        /// </summary>
        public void LogCurrentSettings()
        {
            Console.WriteLine($"[YourPlugin] Debug Mode: {IsDebugEnabled}");
            Console.WriteLine($"[YourPlugin] Config File: {_configFilePath}");
            // Add other settings as needed
        }
    }
}
```

### **Step 2: Update INI Template**

#### **YourPlugin.ini**

```ini
[Debug]
# Enable/disable debug logging to your-plugin-debug.log file
# Set to true to enable detailed logging for troubleshooting
# Set to false to disable debug logging for production use  
debug=false

[YourSection]
# Your plugin-specific settings
yourSetting=value

# Other sections...
```

### **Step 3: Integrate into Main Plugin Class**

#### **YourPlugin.cs**

```csharp
using YourPlugin.Services;

namespace YourPlugin
{
    public class YourPlugin : BasePlugin
    {
        #region Private Fields
        
        private string? _configFilePath;
        private ConfigurationService? _configService;
        private FileLoggingService? _fileLogger;
        private YourMainService? _yourMainService;
        
        #endregion
        
        #region Constructor & Initialization
        
        public YourPlugin() : base("YourPlugin", "Your Plugin Display Name", "Plugin description")
        {
            // Set config file path (InfoPanel integration pattern)
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string assemblyPath = assembly.ManifestModule.FullyQualifiedName;
            _configFilePath = assemblyPath.Replace(".dll", ".ini");
        }
        
        public override void Initialize()
        {
            try
            {
                // Create services fresh (for plugin reload support)
                _configService = new ConfigurationService(_configFilePath);
                _configService.LogCurrentSettings();
                
                // Create file logger AFTER config service
                _fileLogger = new FileLoggingService(_configService);
                
                _fileLogger?.LogInfo("=== YourPlugin Initialize() ===");
                _fileLogger?.LogInfo($"Config file: {_configFilePath}");
                
                // Create other services with logger dependency
                _yourMainService = new YourMainService(_configService, _fileLogger);
                
                // Start your plugin's main functionality
                // ... your initialization code ...
                
                _fileLogger?.LogInfo("YourPlugin initialization completed successfully");
            }
            catch (Exception ex)
            {
                _fileLogger?.LogError("Error during plugin initialization", ex);
            }
        }
        
        #endregion
        
        #region Service Integration Example
        
        private void SomePluginMethod()
        {
            try
            {
                _fileLogger?.LogInfo("Starting some operation...");
                
                // Your business logic here
                
                _fileLogger?.LogInfo("Operation completed successfully");
            }
            catch (Exception ex)
            {
                _fileLogger?.LogError("Error in SomePluginMethod", ex);
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void CleanupServices()
        {
            try
            {
                _fileLogger?.LogInfo("Cleaning up services...");
                
                // Clean up your services
                _yourMainService?.Dispose();
                
                // Clean up logger last
                _fileLogger?.LogInfo("Service cleanup completed");
                _fileLogger?.Dispose();
                
                // Clear references
                _yourMainService = null;
                _fileLogger = null;
                _configService = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YourPlugin] Cleanup error: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            CleanupServices();
            GC.SuppressFinalize(this);
        }
        
        #endregion
    }
}
```

### **Step 4: Integrate into Business Logic Services**

#### **YourMainService.cs**

```csharp
namespace YourPlugin.Services
{
    public class YourMainService : IDisposable
    {
        private readonly ConfigurationService _configService;
        private readonly FileLoggingService? _fileLogger;
        private bool _disposed = false;
        
        public YourMainService(ConfigurationService configService, FileLoggingService? fileLogger = null)
        {
            _configService = configService;
            _fileLogger = fileLogger;
            
            _fileLogger?.LogInfo("YourMainService initialized");
        }
        
        public void DoSomething()
        {
            try
            {
                _fileLogger?.LogDebug("DoSomething() called");
                
                // Your business logic
                var result = PerformOperation();
                
                _fileLogger?.LogInfo($"Operation result: {result}");
            }
            catch (Exception ex)
            {
                _fileLogger?.LogError("Error in DoSomething", ex);
                throw; // Re-throw if appropriate
            }
        }
        
        public void StartBackgroundTask()
        {
            Task.Run(async () =>
            {
                try
                {
                    _fileLogger?.LogInfo("Background task started");
                    
                    while (!_disposed)
                    {
                        // Your background work
                        await Task.Delay(1000);
                        
                        // Use throttled logging for frequent operations
                        _fileLogger?.LogDebugThrottled("Background task iteration");
                    }
                }
                catch (Exception ex)
                {
                    _fileLogger?.LogError("Background task error", ex);
                }
                finally
                {
                    _fileLogger?.LogInfo("Background task ended");
                }
            });
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _fileLogger?.LogInfo("Disposing YourMainService");
                _disposed = true;
            }
        }
    }
}
```

---

## 📊 **Logging Methods Reference**

### **Basic Logging**

```csharp
_fileLogger?.LogDebug("Detailed debug information");    // Only when debug=true
_fileLogger?.LogInfo("General information");            // Always logged when debug=true  
_fileLogger?.LogWarning("Warning message");             // Always logged
_fileLogger?.LogError("Error message");                 // Always logged
_fileLogger?.LogError("Error with exception", ex);      // Logs message + stack trace
```

### **Advanced Logging**

```csharp
_fileLogger?.LogImportant("Critical status update");           // Bypasses throttling
_fileLogger?.LogDebugThrottled("Frequent debug message");     // Automatic throttling
_fileLogger?.LogStateChange("Setting", "old", "new");         // Only logs actual changes
_fileLogger?.LogPeriodicStatus("Current Status", data);       // Status summaries
```

### **Domain-Specific Methods**

```csharp
// Adapt these patterns for your plugin's domain
_fileLogger?.LogSystemInfo("Component", "System information");
_fileLogger?.LogMonitoringState("Started", pid, "Context");
_fileLogger?.LogSensorUpdate("SensorType", "value", "context");
```

---

## ⚙️ **Configuration Options**

### **User Configuration (INI)**

```ini
[Debug]  
# Main debug toggle
debug=true                    # Enable debug logging
debug=false                   # Disable debug logging (production)
```

### **Developer Configuration (Code)**

```csharp
public class FileLoggingService
{
    // Batching settings
    private readonly TimeSpan _flushInterval = TimeSpan.FromMilliseconds(500);  // Write frequency
    private const int MAX_BUFFER_SIZE = 20;                                     // Buffer size
    
    // Log rotation settings
    private const long MAX_LOG_SIZE_BYTES = 5 * 1024 * 1024;  // 5MB max file size
    private const int MAX_BACKUP_FILES = 3;                    // Keep 3 backups
    
    // Throttling settings
    private const int DEFAULT_BURST_ALLOWANCE = 5;             // Allow first 5 quickly
    
    // Log level filtering
    private readonly LogLevel _minimumLogLevel;                // Info when debug=true, Warning when debug=false
}
```

### **Throttling Intervals (Customizable)**

```csharp
private void InitializeThrottleIntervals()
{
    if (_configService.IsDebugEnabled)
    {
        // Debug mode - more frequent logging
        _throttleIntervals["YOUR_OPERATION"] = TimeSpan.FromSeconds(10);
        _throttleIntervals["FREQUENT_EVENT"] = TimeSpan.FromSeconds(5);
    }
    else
    {
        // Production mode - minimal logging
        _throttleIntervals["YOUR_OPERATION"] = TimeSpan.FromMinutes(2);
        _throttleIntervals["FREQUENT_EVENT"] = TimeSpan.FromMinutes(1);
    }
}
```

---

## 🧪 **Testing & Validation**

### **Test Debug Toggle**

1. **Enable Logging:**
   ```ini
   [Debug]
   debug=true
   ```
   - Restart plugin or click "Reload Plugin"
   - Check `your-plugin-debug.log` file is created
   - Verify startup messages appear

2. **Disable Logging:**
   ```ini
   [Debug]
   debug=false
   ```
   - Reload plugin
   - Verify no new log entries (file stops growing)

### **Test Log Rotation**

```csharp
// Generate enough log entries to trigger rotation
for (int i = 0; i < 50000; i++)
{
    _fileLogger?.LogInfo($"Test message {i}");
}
// Should create: debug.log, debug.log.1, debug.log.2, debug.log.3
```

### **Test Throttling**

```csharp
// Generate rapid messages
for (int i = 0; i < 100; i++)
{
    _fileLogger?.LogDebug("Rapid message");  
}
// Should see: First 5 + periodic summaries with suppression counts
```

---

## 📂 **File Locations**

### **Log Files**

```
C:\ProgramData\InfoPanel\plugins\YourPlugin\
├── YourPlugin.dll              ← Plugin assembly
├── YourPlugin.ini              ← Configuration file  
├── your-plugin-debug.log       ← Current log file
├── your-plugin-debug.log.1     ← Backup 1 (most recent)
├── your-plugin-debug.log.2     ← Backup 2
└── your-plugin-debug.log.3     ← Backup 3 (oldest)
```

### **Development Files**

```
YourPluginSource/
├── Services/
│   ├── FileLoggingService.cs   ← Copy from InfoPanel.RTSS
│   ├── ConfigurationService.cs ← Add IsDebugEnabled property
│   └── YourMainService.cs       ← Your business logic
├── YourPlugin.cs                ← Main plugin class
└── YourPlugin.ini               ← Template with [Debug] section
```

---

## 🚀 **Advanced Features**

### **Custom Throttle Categories**

```csharp
private string CreateThrottleKey(string message)
{
    // Adapt these patterns for your plugin
    if (message.Contains("Network") || message.Contains("HTTP"))
        return "NETWORK_OPERATIONS";
    if (message.Contains("Database") || message.Contains("SQL"))
        return "DATABASE_OPERATIONS";
    if (message.Contains("File") || message.Contains("IO"))
        return "FILE_OPERATIONS";
    
    // Create category from first two words
    var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (words.Length >= 2)
        return $"{words[0]}_{words[1]}".ToUpper();
    
    return "DEFAULT";
}
```

### **Performance Monitoring**

```csharp
public void LogPerformanceMetric(string operation, TimeSpan duration, bool success = true)
{
    string status = success ? "SUCCESS" : "FAILED";
    _fileLogger?.LogInfo($"Performance - {operation}: {duration.TotalMilliseconds:F1}ms [{status}]");
}
```

### **Memory Usage Tracking**

```csharp
public void LogMemoryUsage(string context = "")
{
    var process = Process.GetCurrentProcess();
    var workingSet = process.WorkingSet64 / (1024 * 1024); // MB
    var privateMemory = process.PrivateMemorySize64 / (1024 * 1024); // MB
    
    string contextInfo = string.IsNullOrEmpty(context) ? "" : $" [{context}]";
    _fileLogger?.LogDebug($"Memory Usage{contextInfo}: Working Set: {workingSet}MB, Private: {privateMemory}MB");
}
```

---

## 🛡️ **Best Practices**

### **1. Service Lifecycle**

✅ **Create ConfigurationService first**  
✅ **Create FileLoggingService second**  
✅ **Pass FileLoggingService to other services**  
✅ **Dispose in reverse order**  

### **2. Null Safety**

```csharp
// Always use null conditional operator
_fileLogger?.LogInfo("Message");

// Check for disposal
if (_fileLogger != null && !_disposed)
{
    _fileLogger.LogInfo("Message");
}
```

### **3. Exception Handling**

```csharp
try
{
    // Risky operation
    _fileLogger?.LogDebug("Starting risky operation");
    DoRiskyOperation();
    _fileLogger?.LogInfo("Operation succeeded");
}
catch (Exception ex)
{
    _fileLogger?.LogError("Operation failed", ex);
    throw; // Re-throw if appropriate
}
```

### **4. Performance Considerations**

```csharp
// Use throttled logging for frequent events
_fileLogger?.LogDebugThrottled("Frequent event occurred");

// Use appropriate log levels
_fileLogger?.LogDebug("Detailed info only needed for debugging");
_fileLogger?.LogInfo("Important status information");
_fileLogger?.LogWarning("Something unusual but not critical");
_fileLogger?.LogError("Critical error that needs attention");
```

### **5. Threading Safety**

```csharp
// FileLoggingService is thread-safe, but your services should be careful
private readonly object _lock = new object();

public void ThreadSafeMethod()
{
    lock (_lock)
    {
        _fileLogger?.LogDebug("Thread-safe operation");
        // Your thread-sensitive code
    }
}
```

---

## 🔍 **Troubleshooting**

### **Common Issues**

#### **1. No Log File Created**
- Check `debug=true` in INI file
- Verify ConfigurationService.IsDebugEnabled returns true
- Check file permissions in plugin directory
- Verify FileLoggingService initialization didn't throw exception

#### **2. Empty Log File**
- Check minimum log level filtering
- Verify messages meet throttling criteria
- Check if buffering is working (wait 500ms for flush)

#### **3. Missing Log Entries**
- Check if messages are being throttled
- Look for suppression count indicators: `[+5 similar suppressed]`
- Verify log level meets minimum threshold

#### **4. Performance Issues**
- Adjust flush interval: `_flushInterval = TimeSpan.FromSeconds(1)`
- Reduce buffer size: `MAX_BUFFER_SIZE = 10`
- Increase throttling intervals for noisy messages

### **Debug Commands**

```csharp
// Force immediate flush for testing
_fileLogger?.FlushLogBuffer(null);

// Check if debug is enabled
Console.WriteLine($"Debug enabled: {_configService.IsDebugEnabled}");

// Test message categories
_fileLogger?.LogInfo("Test message to verify logging works");
```

---

## 📋 **Implementation Checklist**

### **Phase 1: Basic Setup**
- [ ] Copy `FileLoggingService.cs` and adapt namespace
- [ ] Update `ConfigurationService.cs` with `IsDebugEnabled` property  
- [ ] Add `[Debug]` section to INI template
- [ ] Update main plugin class constructor for config path
- [ ] Test basic logging works

### **Phase 2: Service Integration**
- [ ] Add FileLoggingService to service constructors
- [ ] Update Initialize() method to create logger after config
- [ ] Add logging to main business logic methods
- [ ] Test debug toggle works (INI → reload → logging starts/stops)

### **Phase 3: Advanced Features**
- [ ] Customize throttle categories for your domain
- [ ] Add domain-specific logging methods
- [ ] Implement performance/memory logging if needed
- [ ] Test log rotation with large files
- [ ] Test throttling with rapid messages

### **Phase 4: Production Readiness**
- [ ] Set `debug=false` in default INI template
- [ ] Add error handling around all logging calls
- [ ] Test plugin works correctly when logging is disabled
- [ ] Document debug feature for users
- [ ] Test plugin reload functionality

---

## 🎯 **Summary**

The InfoPanel.RTSS debug logging system provides:

✅ **User Control**: Simple INI toggle (`debug=true/false`)  
✅ **Performance**: Batched writes, intelligent throttling, minimal overhead when disabled  
✅ **Maintainability**: Structured log files with rotation, categorized messages  
✅ **Developer Experience**: Rich logging methods, null-safe, thread-safe  
✅ **Production Ready**: Automatic throttling prevents log spam, graceful degradation  

**Key Implementation Steps:**
1. Copy and adapt `FileLoggingService.cs`
2. Add `IsDebugEnabled` to `ConfigurationService.cs` 
3. Update INI template with `[Debug]` section
4. Integrate into plugin lifecycle (config → logger → services)
5. Add logging calls throughout business logic
6. Test debug toggle and log functionality

This system has been battle-tested in InfoPanel.RTSS with complex real-time monitoring and provides a solid foundation for any InfoPanel plugin requiring debug logging capabilities.

---

**Reference Implementation**: InfoPanel.RTSS v1.2.0  
**License**: Adapt freely for your InfoPanel plugins  
**Support**: See InfoPanel.RTSS source code for complete working example