using System;
using System.Collections.Generic;

namespace InfoPanel.SteamAPI.Models
{
    /// <summary>
    /// Data model for game news information
    /// Contains news for the current game and a feed for the library
    /// </summary>
    public class NewsData
    {
        /// <summary>
        /// The latest news item for the currently played game
        /// </summary>
        public NewsItem? CurrentGameNews { get; set; }

        /// <summary>
        /// A list of news items for the user's library (e.g. recent games)
        /// Tuple: AppId, GameName, NewsItem
        /// </summary>
        public List<(int AppId, string GameName, NewsItem News)> LibraryNews { get; set; } = new();

        /// <summary>
        /// When this data was last updated
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
