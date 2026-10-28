using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Media_Status
{
    public class OrganizerResult
    {
        public string StatusText { get; set; } = string.Empty;
        public Cursor StatusCursor { get; set; } = Cursors.Default;
        public List<string> MovedLog { get; set; } = new();
    }

    public class FileOrganizer
    {
        private static readonly Regex TvPattern = new(
            @"\b(S(\d{1,2})E\d{1,2}|(\d{1,2})x\d{1,2}|Season\s*(\d{1,2})|Episode\s*\d{1,2})\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SeasonExtractor = new(
            @"(?:S(\d{1,2})E\d{1,2}|(\d{1,2})x\d{1,2}|Season\s*(\d{1,2}))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ReleaseTagsPattern = new(
            @"\b(1080p|720p|4k|2160p|x264|x265|hevc|bluray|webrip|web-dl|h264|h265|aac)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static OrganizerResult OrganizeLooseFiles(string rootFolder, IEnumerable<string> videoExtensions)
        {
            var result = new OrganizerResult();

            if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
            {
                result.StatusText = "Invalid root directory.";
                return result;
            }

            try
            {
                var looseFiles = Directory.GetFiles(rootFolder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.Contains("\\Archive\\", StringComparison.OrdinalIgnoreCase) &&
                                !f.Contains("\\Films\\", StringComparison.OrdinalIgnoreCase) &&
                                videoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();

                if (looseFiles.Count == 0)
                {
                    result.StatusText = "No loose media files found in root.";
                    return result;
                }

                var existingDirectories = Directory.GetDirectories(rootFolder)
                    .Where(d => !Path.GetFileName(d).Equals("Archive", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(d).Equals("Films", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                int untouchedCount = 0;

                foreach (var file in looseFiles)
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                    string extension = Path.GetExtension(file);
                    string fileName = Path.GetFileName(file);

                    bool isTvShow = TvPattern.IsMatch(fileNameWithoutExt);

                    if (isTvShow)
                    {
                        // 1. Extract Season Number
                        int seasonNumber = 1; // Default fallback
                        var seasonMatch = SeasonExtractor.Match(fileNameWithoutExt);
                        if (seasonMatch.Success)
                        {
                            if (seasonMatch.Groups[1].Success) int.TryParse(seasonMatch.Groups[1].Value, out seasonNumber);
                            else if (seasonMatch.Groups[2].Success) int.TryParse(seasonMatch.Groups[2].Value, out seasonNumber);
                            else if (seasonMatch.Groups[3].Success) int.TryParse(seasonMatch.Groups[3].Value, out seasonNumber);
                        }

                        // 2. Extract Show Name by splitting from the season/episode tag onwards
                        string showName = fileNameWithoutExt;
                        int tvPatternIdx = fileNameWithoutExt.IndexOf(seasonMatch.Value, StringComparison.OrdinalIgnoreCase);
                        if (tvPatternIdx > 0)
                        {
                            showName = fileNameWithoutExt.Substring(0, tvPatternIdx);
                        }

                        // Clean up show name formatting
                        showName = Regex.Replace(showName, @"[\._\-]+", " ").Trim();
                        showName = ReleaseTagsPattern.Replace(showName, "").Trim();
                        showName = Regex.Replace(showName, @"\s+", " ").Trim();

                        if (string.IsNullOrWhiteSpace(showName))
                        {
                            untouchedCount++;
                            continue;
                        }

                        // 3. Find or Create Show Folder
                        string showDir = existingDirectories.FirstOrDefault(d =>
                        {
                            string dirName = Path.GetFileName(d);
                            string cleanDirName = Regex.Replace(dirName, @"[\._\-]+", " ").Trim();
                            return string.Equals(cleanDirName, showName, StringComparison.OrdinalIgnoreCase) ||
                                   showName.Contains(cleanDirName, StringComparison.OrdinalIgnoreCase) ||
                                   cleanDirName.Contains(showName, StringComparison.OrdinalIgnoreCase);
                        });

                        if (showDir == null)
                        {
                            showDir = Path.Combine(rootFolder, showName);
                            Directory.CreateDirectory(showDir);
                            if (!existingDirectories.Contains(showDir, StringComparer.OrdinalIgnoreCase))
                            {
                                existingDirectories.Add(showDir);
                            }
                        }

                        // 4. Find or Create Season Folder inside Show Folder (e.g., "Season 1")
                        string seasonFolderName = $"Season {seasonNumber}";
                        string seasonDir = Directory.GetDirectories(showDir)
                            .FirstOrDefault(d => Regex.IsMatch(Path.GetFileName(d), $@"^(Season\s*0*{seasonNumber}|S0*{seasonNumber})\b", RegexOptions.IgnoreCase));

                        if (seasonDir == null)
                        {
                            seasonDir = Path.Combine(showDir, seasonFolderName);
                            Directory.CreateDirectory(seasonDir);
                        }

                        // 5. Move file to the correct season subfolder
                        string destPath = Path.Combine(seasonDir, fileName);
                        if (!File.Exists(destPath) && !string.Equals(file, destPath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Move(file, destPath);
                            result.MovedLog.Add($"• {fileName}  ➔  {Path.GetFileName(showDir)}\\{Path.GetFileName(seasonDir)}");
                        }
                    }
                    else
                    {
                        // Treat as a Film
                        string cleanFileName = Regex.Replace(fileNameWithoutExt, @"[\._\-]+", " ").Trim();
                        cleanFileName = ReleaseTagsPattern.Replace(cleanFileName, "").Trim();
                        cleanFileName = Regex.Replace(cleanFileName, @"\s+", " ");

                        string filmsBaseDir = Path.Combine(rootFolder, "Films");
                        string movieFolder = Path.Combine(filmsBaseDir, cleanFileName);

                        if (!Directory.Exists(movieFolder))
                        {
                            Directory.CreateDirectory(movieFolder);
                        }

                        string destPath = Path.Combine(movieFolder, cleanFileName + extension);
                        if (!File.Exists(destPath) && !string.Equals(file, destPath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Move(file, destPath);
                            result.MovedLog.Add($"• {fileName}  ➔  Films\\{cleanFileName}");
                        }
                    }
                }

                if (result.MovedLog.Count > 0)
                {
                    result.StatusText = $"✨ Organized {result.MovedLog.Count} file(s) (Click for details)";
                    result.StatusCursor = Cursors.Hand;
                }
                else if (untouchedCount > 0)
                {
                    result.StatusText = $"Loose files checked: {untouchedCount} left in root.";
                }
            }
            catch (Exception ex)
            {
                result.StatusText = $"Error organizing files: {ex.Message}";
            }

            return result;
        }
    }
}