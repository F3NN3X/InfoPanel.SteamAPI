# InfoPanel.SteamAPI

**Version:** 1.0.0  
**Author:** F3NN3X  
**Website:** https://myurl.com

## Description

Get data from SteamAPI

## Documentation

This plugin includes comprehensive InfoPanel plugin development documentation:

- **[InfoPanel Plugin Development Guide](docs/InfoPanel_PluginDocumentation.md)** - Complete guide to InfoPanel plugin development, including:
  - Plugin architecture overview
  - Component descriptions and lifecycle
  - Code examples and best practices
  - Debugging and deployment instructions
  - API reference and data types

## Features

- Service-based architecture for clean separation of concerns
- Event-driven data updates with thread-safe sensor management
- INI-based configuration following InfoPanel standards
- Comprehensive error handling and logging
- Async monitoring with proper cancellation support
- Professional build system with automatic versioning

## Installation

1. Build the plugin in Release mode:
   ```powershell
   dotnet build -c Release
   ```

2. The plugin will be built to:
   ```
   bin\Release\net8.0-windows\SteamAPI-v1.0.0\SteamAPI\
   ```

3. A distribution ZIP file will also be created:
   ```
   bin\Release\net8.0-windows\SteamAPI-v1.0.0.zip
   ```

4. Extract the ZIP file to your InfoPanel plugins directory, or copy the plugin folder manually

5. Restart InfoPanel to load the plugin

## Configuration

After first run, the plugin creates a configuration file:
```
SteamAPI.dll.ini
```

You can edit this file to customize plugin behavior. The configuration file includes:

- **Debug Settings**: Enable logging and set log levels
- **Monitoring Settings**: Adjust monitoring intervals and connection settings
- **Display Settings**: Control how information is displayed
- **Plugin-Specific Settings**: Custom settings for this plugin

Use InfoPanel's "Open Config" button to easily access and edit the configuration file.

## Building from Source

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or JetBrains Rider (optional)
- InfoPanel installed with InfoPanel.Plugins.dll available

### Build Commands

**Debug Build:**
```powershell
dotnet build -c Debug
```

**Release Build:**
```powershell
dotnet build -c Release
```

**Clean Build:**
```powershell
dotnet clean
dotnet build -c Release
```

## Development

### Architecture

The plugin follows a service-based architecture:

```
SteamAPI.cs              # Main plugin class
├── Services/
│   ├── MonitoringService.cs         # Data collection orchestration
│   ├── SensorManagementService.cs   # Thread-safe sensor updates
│   ├── ConfigurationService.cs      # INI configuration management
│   └── FileLoggingService.cs        # Debug logging
├── Models/
│   └── TemplateData.cs              # Data structure
└── PluginInfo.ini                   # Plugin metadata
```

### Key Components

- **Main Plugin Class**: Coordinates services and manages plugin lifecycle
- **Monitoring Service**: Handles data collection and raises events when data updates
- **Sensor Management Service**: Thread-safe updates to InfoPanel sensors
- **Configuration Service**: Manages INI file configuration with section-based organization
- **File Logging Service**: Provides debug logging with rotation and multiple log levels

### Adding New Features

1. **New Sensors**: Define in main class, add to container, update in sensor service
2. **New Configuration**: Add settings to ConfigurationService with section-based accessors
3. **New Data Properties**: Extend TemplateData model and update validation

## Troubleshooting

### Enable Debug Logging

Edit the configuration file and set:
```ini
[Debug Settings]
EnableDebugLogging=true
DebugLogLevel=DEBUG
```

Check the log file: `SteamAPI-debug.log`

### Common Issues

**Plugin Not Loading:**
- Ensure InfoPanel.Plugins.dll reference is correct
- Verify all dependencies are in the plugin directory
- Check that .NET 8.0 runtime is installed

**No Data Appearing:**
- Enable debug logging and check for errors
- Verify monitoring service is starting correctly
- Check data source connectivity

**Configuration Not Saving:**
- Verify file permissions in plugin directory
- Check INI file syntax (section headers and key=value format)
- Review debug logs for file access errors

## Version History

See [CHANGELOG.md](CHANGELOG.md) for detailed version history.

## License

See [LICENSE](LICENSE) for license information.

## Support

For issues, questions, or contributions:
- Check debug logs first
- Review InfoPanel documentation
- Contact: F3NN3X (https://myurl.com)

## Acknowledgments

Built using the InfoPanel Plugin Template v1.0.
