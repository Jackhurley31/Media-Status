using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace Media_Status
{
    public class JunkTagSettings
    {
        public List<string> Tags { get; set; } = new();
    }

    public static class JunkTagManager
    {
        private static readonly string FilePath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MediaControlApp",
                "junk_tags.json");

        // Direct Raw URL pointing to your GitHub repository file
        private const string GitHubRawUrl = "https://raw.githubusercontent.com/Jackhurley31/Media-Status/master/junk_tags.json";

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Attempts to download the latest tags from GitHub on startup.
        /// Falls back to the local LocalAppData cache if offline.
        /// </summary>
        public static async Task<List<string>> LoadAsync()
        {
            try
            {
                // 1. Try downloading the fresh list from GitHub
                string json = await HttpClient.GetStringAsync(GitHubRawUrl);
                var settings = JsonSerializer.Deserialize<JunkTagSettings>(json);

                if (settings?.Tags != null && settings.Tags.Count > 0)
                {
                    bool changed = false;

                    foreach (string tag in GetDefaultTags())
                    {
                        if (!settings.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                        {
                            settings.Tags.Add(tag);
                            changed = true;
                        }
                    }

                    // Save the updated list locally for offline caching
                    Save(settings.Tags);
                    return settings.Tags;
                }
            }
            catch
            {
                // Network failed or offline — fall back smoothly to local cache
            }

            return LoadLocal();
        }

        private static List<string> LoadLocal()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);

                    var settings =
                        JsonSerializer.Deserialize<JunkTagSettings>(json);

                    if (settings?.Tags != null)
                    {
                        bool changed = false;

                        foreach (string tag in GetDefaultTags())
                        {
                            if (!settings.Tags.Contains(tag,
                                StringComparer.OrdinalIgnoreCase))
                            {
                                settings.Tags.Add(tag);
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            Save(settings.Tags);
                        }

                        return settings.Tags;
                    }
                }
            }
            catch
            {
            }

            var defaults = GetDefaultTags();

            Save(defaults);

            return defaults;
        }

        public static void Save(List<string> tags)
        {
            try
            {
                string? directory = Path.GetDirectoryName(FilePath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var settings = new JunkTagSettings
                {
                    Tags = tags
                };

                string json = JsonSerializer.Serialize(
                    settings,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                File.WriteAllText(FilePath, json);
            }
            catch
            {
            }
        }

        private static List<string> GetDefaultTags()
        {
            return new List<string>
            {
                "720p",
                "1080p",
                "2160p",
                "4K",
                "HDTV",
                "WEB-DL",
                "WEBRip",
                "HDRip",
                "DVDRip",
                "BDRip",
                "BRRip",
                "BluRay",
                "HD",
                "x264",
                "x265",
                "HEVC",
                "AVC",
                "Xvid",
                "DivX",
                "10bit",
                "8bit",
                @"\d+MB",
                "HDR",
                "GalaxyRG",
                "HDR10",
                "Atmos",
                "TrueHD",
                "DDP?",
                @"DD\+",
                "AC3",
                "AAC",
                "MP3",
                "DTS",
                "CH",
                @"\d+CH",
                "PROPER",
                "REPACK",
                "INTERNAL",
                "EXTENDED",
                "UNRATED",
                "REMASTERED",
                "PSA",
                "FENIX",
                "EZTV",
                "AFG",
                "DSNP",
                "NF",
                "AMZN",
                "HULU",
                "ATVP",
                "MAX",
                "PCOK",
                "CRITERION",
                "TIFY",
                "YIFY",
                "BOKUTOX",
                "DD5 1 X 264 EVO",
                "AAC5 1",
                "[EZTVx.to]"
            };
        }
    }
}