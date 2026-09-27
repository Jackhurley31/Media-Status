using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaControlApp.Models;

namespace MediaControlApp.Helpers
{
    public static class EpisodeRenamer
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static EpisodeRenamer()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MediaControlApp/1.0");
        }

        public static async Task<Dictionary<(int Season, int Episode), string>> FetchEpisodeTitlesTvMazeAsync(string showName)
        {
            var episodeMap = new Dictionary<(int Season, int Episode), string>();

            try
            {
                string searchUrl = $"https://api.tvmaze.com/singlesearch/shows?q={Uri.EscapeDataString(showName)}&embed=episodes";
                HttpResponseMessage response = await _httpClient.GetAsync(searchUrl);

                if (!response.IsSuccessStatusCode) return episodeMap;

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("_embedded", out var embedded) &&
                    embedded.TryGetProperty("episodes", out var episodes))
                {
                    foreach (var ep in episodes.EnumerateArray())
                    {
                        if (ep.TryGetProperty("season", out var sProp) && sProp.TryGetInt32(out int season) &&
                            ep.TryGetProperty("number", out var eProp) && eProp.TryGetInt32(out int episode) &&
                            ep.TryGetProperty("name", out var nameProp) && nameProp.GetString() is string title)
                        {
                            if (!string.IsNullOrWhiteSpace(title))
                                episodeMap[(season, episode)] = CleanFileName(title);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching TVmaze titles: {ex.Message}");
            }

            return episodeMap;
        }

        public static async Task<int> AutoRenameEpisodesAsync(
            List<MediaItem> episodes,
            string showName,
            Func<string, string> buildFullPathFunc,
            Action<MediaItem, string, string> updateStateAction = null)
        {
            if (episodes == null || episodes.Count == 0 || string.IsNullOrWhiteSpace(showName))
                return 0;

            var officialTitles = await FetchEpisodeTitlesTvMazeAsync(showName);
            int renamedCount = 0;

            foreach (var ep in episodes)
            {
                int season = ep.CachedSeason;
                int episode = ep.CachedEpisode;

                string oldFullPath = buildFullPathFunc(ep.FilePath);
                if (!File.Exists(oldFullPath)) continue;

                string folder = Path.GetDirectoryName(oldFullPath);
                string ext = Path.GetExtension(oldFullPath);

                string title = officialTitles.TryGetValue((season, episode), out var t) ? t : $"Episode {episode}";
                string newFileName = $"{showName} - S{season:D2}E{episode:D2} - {title}{ext}";
                string newFullPath = Path.Combine(folder, newFileName);

                if (string.Equals(oldFullPath, newFullPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    if (!File.Exists(newFullPath))
                    {
                        File.Move(oldFullPath, newFullPath);

                        // Transfer state mapping (re-keys dictionaries and updates MediaItem path)
                        updateStateAction?.Invoke(ep, oldFullPath, newFullPath);

                        renamedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to rename {oldFullPath}: {ex.Message}");
                }
            }

            return renamedCount;
        }

        public static int RenameToSequentialOrder(
            List<MediaItem> episodes,
            string showName,
            Func<string, string> buildFullPathFunc,
            Action<MediaItem, string, string> updateStateAction = null)
        {
            if (episodes == null || episodes.Count == 0 || string.IsNullOrWhiteSpace(showName))
                return 0;

            int renamedCount = 0;

            foreach (var ep in episodes)
            {
                int season = ep.CachedSeason;
                int episode = ep.CachedEpisode;

                string oldFullPath = buildFullPathFunc(ep.FilePath);
                if (!File.Exists(oldFullPath)) continue;

                string folder = Path.GetDirectoryName(oldFullPath);
                string ext = Path.GetExtension(oldFullPath);

                string newFileName = $"{showName} - S{season:D2}E{episode:D2}{ext}";
                string newFullPath = Path.Combine(folder, newFileName);

                if (string.Equals(oldFullPath, newFullPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    if (!File.Exists(newFullPath))
                    {
                        File.Move(oldFullPath, newFullPath);

                        // Transfer state mapping (re-keys dictionaries and updates MediaItem path)
                        updateStateAction?.Invoke(ep, oldFullPath, newFullPath);

                        renamedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Sequential rename failed: {ex.Message}");
                }
            }

            return renamedCount;
        }

        private static string CleanFileName(string title)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(title, invalidRegStr, "").Trim();
        }
    }
}