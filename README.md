# InfoPanel.SteamAPI

![Version](https://img.shields.io/badge/version-1.3.5-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Platform](https://img.shields.io/badge/platform-InfoPanel-orange.svg)

**Comprehensive Steam integration for InfoPanel.**  
Monitor your real-time gaming activity, session stats, friends, and library data directly from your dashboard.

## ✨ Features

- **⚡ Real-Time Monitoring**: Instant game state detection (1s updates) and live profile status.
- **⏱️ Session Tracking**: Reliable tracking of gaming sessions, duration, and history.
- **👥 Social Integration**: Monitor friends online, see what they're playing.
- **🏆 Achievements & Stats**: Track achievement progress, game completion, and library statistics.
- **📰 Game News**: Get the latest updates and news for your currently played and monitored games.
- **📊 Interactive Tables**:
  - **Recent Games**: Top 5 recently played games with stats.
  - **Friends Activity**: Live status of your friends list.
  - **Game Statistics**: Detailed metrics for your monitored games.

## 🚀 Installation

1. **Download** the latest release or build from source.
2. **Extract** the `InfoPanel.SteamAPI` folder into your InfoPanel plugins directory:

   ```
   C:\ProgramData\InfoPanel\plugins\
   ```

3. **Restart** InfoPanel to load the plugin.

## ⚙️ Configuration

On the first run, the plugin creates `InfoPanel.SteamAPI.dll.ini`. You must configure your Steam credentials:

```ini
[Steam Settings]
# Get your key at: https://steamcommunity.com/dev/apikey
SteamApiKey=YOUR_STEAM_WEB_API_KEY

# Find your ID at: https://steamid.io/
SteamId64=YOUR_64BIT_STEAM_ID
```

### Optional Settings

You can customize update intervals, display text, and privacy settings in the config file. Use the **"Open Config"** button in InfoPanel to edit easily.

## 📊 Data Containers

The plugin organizes data into 4 main containers:

### 1. Basic Steam Data

*Core profile info and current status.*

- **Profile**: Name, Online Status, Steam Level.
- **Current Game**: Name, Playtime.
- **Library**: Total Games, Total Playtime, Recent Playtime.

### 2. Enhanced Gaming Data

*Session tracking and recent activity.*

- **Session**: Current Duration, Start Time, Average Length.
- **Recent**: Games Played (2w), Top Recent Game.
- **Achievements**: Current game progress (Unlocked/Total).

### 3. Advanced Steam Features

*Detailed stats and news.*

- **Monitored Games**: Detailed stats for your top games.
- **News**: Latest headlines and unread counts.
- **Completion**: Overall account achievement % and perfect games.

### 4. Social & Community

*Friends and global stats.*

- **Friends**: Online count, In-Game count, Most Popular Game.
- **Badges**: Total Badges, XP, Latest Badge.
- **Global**: Playtime Percentile, User Category.

## ❓ Troubleshooting

**No Data Showing?**

- Verify your **API Key** and **Steam ID**.
- Ensure your Steam Profile Privacy settings are set to **Public**.
- Check `InfoPanel.SteamAPI-debug.log` in the plugin folder if issues persist.

**Session Tracking Not Working?**

- Session tracking only activates when you are running a game.
- Ensure `FastUpdateIntervalSeconds` is set to a low value (default: 5).

## 🛠️ Development

For developers interested in building from source, architecture details, or contributing, please read our **[Development Guide](docs/DEVELOPMENT.md)**.

## 📄 License

See [LICENSE](LICENSE) for details.

## 📞 Support

- **Website**: [GitHub Repository](https://github.com/F3NN3X/InfoPanel.SteamAPI)
- **Issues**: [Report a Bug](https://github.com/F3NN3X/InfoPanel.SteamAPI/issues)
