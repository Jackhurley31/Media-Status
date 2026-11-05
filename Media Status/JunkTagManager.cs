using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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

        public static List<string> Load()
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
                "AAC5 1"

            };
        }
    }
}