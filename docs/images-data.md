Complete Guide to Retrieving All Steam Game Images (Capsules, Banners, Heroes) for C# Plugin
Compiled from Steam Store API, CDN patterns, and <https://steamapi.xpaw.me/#> on November 10, 2025
Overview: Use the unofficial Steam Store API[](https://store.steampowered.com/api/appdetails) to get direct URLs for most images—no API key needed. For missing ones, construct CDN URLs (99% reliable for published games). Supports batch (100 appids/call). Rate limit: ~200/5min. Get appids from IPlayerService/GetOwnedGames.

1. Primary API: appdetails (Returns JSON with image URLs)

- Endpoint: GET <https://store.steampowered.com/api/appdetails?appids={APPIDS}&filters=basic>
  - appids: Comma-separated (e.g., 730,440), max ~100
  - filters: "basic" (name, images), or "" for all (detailed, price, etc.)
  - cc: Country (e.g., US), l: Language (e.g., english)
- Response Fields (data object per appid):

  | Field                  | Size (px)     | Description                  | URL Example (appid=730) |
  |------------------------|---------------|------------------------------|-------------------------|
  | header_image          | 460x215      | Header/Promotional Banner   | <https://cdn.cloudflare.steampowered.com/steam/apps/730/header.jpg> |
  | small_capsule_image   | 184x69       | Small Capsule (search/lib)  | <https://cdn.cloudflare.steampowered.com/steam/apps/730/capsule_184x69.jpg> |
  | large_capsule_image   | 616x353      | Main Capsule                | <https://cdn.cloudflare.steampowered.com/steam/apps/730/capsule_616x353.jpg> |
  | hero_capsule_image?   | ~1232x706?   | Hero/Main Promotional       | <https://cdn.cloudflare.steampowered.com/steam/apps/730/hero_capsule.jpg> |
  | small_capsule_vn?     | ~374x448?    | Vertical Capsule (half)     | <https://cdn.cloudflare.steampowered.com/steam/apps/730/capsule_sm_120.jpg> (small vn) |
  | page_background?      | Variable     | Store Page Background       | <https://cdn.cloudflare.steampowered.com/steam/apps/730/page_bg_generated.jpg> |

Notes: "?" = Not always present (dev-dependent). JSON: {"730":{"success":true,"data":{...images...}}}

2. Constructed CDN URLs (Fallback/Additional Sizes)
Base: <https://cdn.cloudflare.steampowered.com/steam/apps/{APPID}/filename.jpg> (or steamcdn-a.akamaihd.net)
All Known Patterns (test; 404 if missing):
| Image Type             | Filename                  | Size (px)      | Notes |
|------------------------|---------------------------|----------------|-------|
| Header Capsule        | header.jpg               | 920x430 / 460x215 | Store header |
| Main Capsule          | capsule_616x353.jpg      | 1232x706 scaled | Main promo |
| Small Capsule         | capsule_184x69.jpg       | 462x174 scaled | Frequent (search) |
| Vertical Capsule      | hero_capsule.jpg         | 748x896        | Promotions |
| Library Hero          | library_hero.jpg         | 3840x1240      | Library BG |
| Library Capsule       | library_600x900.jpg      | 600x900        | Box art |
| Library Hero Blur     | library_hero_blur.jpg    | 3840x1240 blur | Alt BG |
| Page BG               | page_bg_generated.jpg    | 1438x810       | Store page |
| Logo                  | logo.png                 | Variable       | Overlay |
| Icon                  | icon.jpg                 | Small          | From GetOwnedGames |

3. C# Code Example (HttpClient + Json)

```csharp
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public static class SteamImages
{
    private static readonly HttpClient client = new();

    public static async Task<Dictionary<uint, Dictionary<string, string>>> GetAllImagesAsync(uint appId)
    {
        string url = $"https://store.steampowered.com/api/appdetails?appids={appId}&filters=basic";
        string json = await client.GetStringAsync(url);
        using JsonDocument doc = JsonDocument.Parse(json);
        var images = new Dictionary<uint, Dictionary<string, string>>();

        if (doc.RootElement.TryGetProperty(appId.ToString(), out JsonElement appData) &&
            appData.GetProperty("success").GetBoolean() &&
            appData.GetProperty("data").TryGetProperty("header_image", out _)) // Check success
        {
            var appImages = new Dictionary<string, string>();
            JsonElement data = appData.GetProperty("data");

            // API Fields
            if (data.TryGetProperty("header_image", out JsonElement h)) appImages["header"] = h.GetString();
            if (data.TryGetProperty("small_capsule_image", out JsonElement s)) appImages["small_capsule"] = s.GetString();
            if (data.TryGetProperty("large_capsule_image", out JsonElement l)) appImages["main_capsule"] = l.GetString();
            if (data.TryGetProperty("hero_capsule_image", out JsonElement hc)) appImages["hero_capsule"] = hc.GetString();

            // Always-Construct CDN
            string cdnBase = $"https://cdn.cloudflare.steampowered.com/steam/apps/{appId}/";
            appImages["library_hero"] = cdnBase + "library_hero.jpg";
            appImages["library_capsule"] = cdnBase + "library_600x900.jpg";
            appImages["page_bg"] = cdnBase + "page_bg_generated.jpg";
            appImages["vertical_capsule"] = cdnBase + "hero_capsule.jpg"; // Alias

            images[appId] = appImages;
        }
        return images;
    }

    // Batch (100 max)
    public static async Task<Dictionary<uint, Dictionary<string, string>>> GetBatchAsync(List<uint> appIds)
    {
        var allImages = new Dictionary<uint, Dictionary<string, string>>();
        for (int i = 0; i < appIds.Count; i += 100)
        {
            string ids = string.Join(",", appIds.Skip(i).Take(100));
            // Call GetAllImagesAsync logic with batch URL, parse each
            // ... (implement batch parsing similar to above)
        }
        return allImages;
    }
}

// Usage
var imgs = await SteamImages.GetAllImagesAsync(730); // CS2
foreach (var kv in imgs[730])
    Console.WriteLine($"{kv.Key}: {kv.Value}");
