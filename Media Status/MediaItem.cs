using Media_Status;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace MediaControlApp.Models
{
    public class MediaItem
    {

        public Dictionary<string, double> SavedPositionByUser { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public double GetSavedPosition(string user)
        {
            if (string.IsNullOrWhiteSpace(user)) return 0;
            return SavedPositionByUser.TryGetValue(user, out double pos) ? pos : 0;
        }

        public void SetSavedPosition(string user, double positionSeconds)
        {
            if (string.IsNullOrWhiteSpace(user)) return;
            SavedPositionByUser ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            SavedPositionByUser[user] = positionSeconds;
        }



        public DateTime CachedDateModified { get; set; }

        public TimeSpan Duration { get; set; } = TimeSpan.Zero;

        public string FilePath { get; set; } = string.Empty;

        public Dictionary<string, bool> WatchedByUser { get; set; } = new();

        public bool IsWatched(string user)
        {
            return WatchedByUser.TryGetValue(user, out bool watched) && watched;
        }

        public Dictionary<string, bool> FavouriteByUser { get; set; } = new();



        public void MarkWatched(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;

            WatchedByUser ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            WatchedByUser[username] = true;
        }

        // Gets LastWriteTime safely or returns MinValue if missing/unreachable
        public DateTime GetDateModified(string resolvedFullPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(resolvedFullPath) && File.Exists(resolvedFullPath))
                {
                    return File.GetLastWriteTime(resolvedFullPath);
                }
            }
            catch { }
            return DateTime.MinValue;
        }


        public bool FullyWatched => IsWatched(AppConfig.CurrentUser);

        public string CachedSeriesName { get; private set; } = "";
        public int CachedSeason { get; private set; }
        public int CachedEpisode { get; private set; }
        public string CachedCategory { get; private set; } = "";

        public string FileName => Path.GetFileName(FilePath);

        public void CacheParsed()
        {
            CachedSeason = ParseSeason();
            CachedEpisode = ParseEpisode();
            CachedSeriesName = ParseSeriesName();
            CachedCategory = ParseCategory();
        }

        private string ParseSeriesName()
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(FileName);

            // 1. Try extracting series name from filename before season/episode tags
            Match match = Regex.Match(fileNameNoExt, @"^(.*?)(?=[._\s-]*(?:S\d+|S\d+E\d+|\d+x\d+|Season\s*\d+|Episode\s*\d+))", RegexOptions.IgnoreCase);

            string parsedFromFileName = "";
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                parsedFromFileName = CleanName(match.Groups[1].Value);
            }

            if (!string.IsNullOrWhiteSpace(parsedFromFileName))
            {
                return parsedFromFileName;
            }

            // 2. Extract series name from folder structure
            string folderSeriesName = ExtractSeriesFromFolder();
            if (!string.IsNullOrWhiteSpace(folderSeriesName))
            {
                return folderSeriesName;
            }

            // 3. Fallback to cleaned filename
            return CleanName(fileNameNoExt);
        }

        private string ExtractSeriesFromFolder()
        {
            if (string.IsNullOrWhiteSpace(FilePath)) return "";

            try
            {
                string dirPath = Path.GetDirectoryName(FilePath);
                if (string.IsNullOrEmpty(dirPath)) return "";

                string parentDirName = Path.GetFileName(dirPath);

                // Step up if folder is strictly a season folder (e.g. "Season 1", "S01", "Specials")
                if (Regex.IsMatch(parentDirName, @"^(Season\s*\d+|S\d+|Specials)$", RegexOptions.IgnoreCase))
                {
                    string grandParentPath = Path.GetDirectoryName(dirPath);
                    if (!string.IsNullOrEmpty(grandParentPath))
                    {
                        parentDirName = Path.GetFileName(grandParentPath);
                    }
                }

                // Ignore root/utility folders
                if (!string.IsNullOrWhiteSpace(parentDirName) &&
                    !parentDirName.Equals("Archive", StringComparison.OrdinalIgnoreCase) &&
                    !parentDirName.Equals("ToSort", StringComparison.OrdinalIgnoreCase) &&
                    !parentDirName.Contains(":"))
                {
                    return CleanName(parentDirName);
                }
            }
            catch { }

            return "";
        }

        private int ParseSeason()
        {
            string fileName = FileName;

            // 1. Check filename for S01 / 1x01 / Season 1
            Match m = Regex.Match(fileName, @"S(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int season)) return season;

            m = Regex.Match(fileName, @"(\d+)x\d+", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out season)) return season;

            m = Regex.Match(fileName, @"Season\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out season)) return season;

            // 2. Check enclosing directory names (e.g., "Fraiser Season 2" or "Season 2")
            try
            {
                string dirPath = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dirPath))
                {
                    string folderName = Path.GetFileName(dirPath);

                    m = Regex.Match(folderName, @"Season\s*(\d+)", RegexOptions.IgnoreCase);
                    if (m.Success && int.TryParse(m.Groups[1].Value, out season)) return season;

                    m = Regex.Match(folderName, @"^S(\d+)$", RegexOptions.IgnoreCase);
                    if (m.Success && int.TryParse(m.Groups[1].Value, out season)) return season;
                }
            }
            catch { }

            return 1; // Default season
        }

        private int ParseEpisode()
        {
            string fileName = FileName;

            Match m = Regex.Match(fileName, @"E(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int ep)) return ep;

            m = Regex.Match(fileName, @"\d+x(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out ep)) return ep;

            m = Regex.Match(fileName, @"Episode\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out ep)) return ep;

            m = Regex.Match(fileName, @"\b(\d{1,3})\b");
            if (m.Success && int.TryParse(m.Groups[1].Value, out ep)) return ep;

            return 0;
        }

        private string ParseCategory()
        {
            if (string.IsNullOrWhiteSpace(FilePath)) return "Show";

            try
            {
                // 1. Check directory path for explicit "Film" or "Films" folder names
                string dirPath = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dirPath))
                {
                    // Normalise separators and split path segments
                    string[] segments = dirPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                    if (segments.Any(s => s.Equals("Film", StringComparison.OrdinalIgnoreCase) ||
                                         s.Equals("Films", StringComparison.OrdinalIgnoreCase) ||
                                         s.Equals("Movies", StringComparison.OrdinalIgnoreCase)))
                    {
                        return "Film";
                    }
                }

                // 2. Check filename for standalone movie indicators or standalone files (no S01E01 / 1x01 patterns)
                string fileName = FileName;
                bool hasTvPattern = Regex.IsMatch(fileName, @"\b(S\d+|S\d+E\d+|\d+x\d+|Season\s*\d+|Episode\s*\d+)\b", RegexOptions.IgnoreCase);

                // If it resides in a root folder without TV season patterns, treat single-file entries as Film
                if (!hasTvPattern && !string.IsNullOrEmpty(dirPath))
                {
                    string parentFolder = Path.GetFileName(dirPath);

                    // If the folder matches the file name (common format: Movie Name (2023)/Movie Name.mp4)
                    if (!string.IsNullOrEmpty(parentFolder) &&
                        CleanName(parentFolder).Equals(CleanName(Path.GetFileNameWithoutExtension(fileName)), StringComparison.OrdinalIgnoreCase))
                    {
                        return "Film";
                    }
                }
            }
            catch { }

            return "Show";
        }

        private string CleanName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";

            string clean = raw;

            // Strip bracketed tags e.g. [www Movcr tv], [720p], [YTS MX]
            clean = Regex.Replace(clean, @"\[.*?\]", " ");

            // Strip multi-season/range patterns e.g. "Season 1 and 2", "Seasons 1-3", "Season 1 & 2", "S01-S02"
            clean = Regex.Replace(clean, @"(?i)\b(Seasons?|Series)\s*\d+([\s,&\-and]+\d+)*\b", " ");
            clean = Regex.Replace(clean, @"(?i)\bS\d+[\s,&\-]*S?\d+\b", " ");

            // Replace delimiters with spaces
            clean = clean.Replace('.', ' ')
                         .Replace('_', ' ')
                         .Replace('-', ' ');

            // Normalize spaces
            clean = Regex.Replace(clean, @"\s+", " ").Trim();

            // Convert to Title Case
            if (!string.IsNullOrWhiteSpace(clean))
            {
                clean = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clean.ToLower());
            }

            return clean;
        }

        public void OverrideSeriesName(string newName)
        {
            CachedSeriesName = newName;
        }


    }
}