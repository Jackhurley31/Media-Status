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
            @"\b(S\d{1,2}E\d{1,2}|\d{1,2}x\d{1,2}|Season\s*\d+|Episode\s*\d+)\b",
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
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string extension = Path.GetExtension(file);

                    // Clean up separators and release tags
                    string cleanFileName = Regex.Replace(fileName, @"[\._\-]+", " ").Trim();
                    cleanFileName = ReleaseTagsPattern.Replace(cleanFileName, "").Trim();
                    cleanFileName = Regex.Replace(cleanFileName, @"\s+", " ");

                    bool isTvShow = TvPattern.IsMatch(fileName);

                    // 1. Check if the file belongs to an existing TV directory
                    string matchingDir = existingDirectories.FirstOrDefault(d =>
                    {
                        string dirName = Path.GetFileName(d);
                        string cleanDirName = Regex.Replace(dirName, @"[\._\-]+", " ").Trim();
                        return cleanFileName.Contains(cleanDirName, StringComparison.OrdinalIgnoreCase);
                    });

                    if (matchingDir != null)
                    {
                        string destPath = Path.Combine(matchingDir, Path.GetFileName(file));
                        if (!File.Exists(destPath) && !string.Equals(file, destPath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Move(file, destPath);
                            result.MovedLog.Add($"• {Path.GetFileName(file)}  ➔  {Path.GetFileName(matchingDir)}");
                        }
                    }
                    // 2. If it's NOT a TV show and has no matching folder, treat as a Film
                    else if (!isTvShow)
                    {
                        string filmsBaseDir = Path.Combine(rootFolder, "Films");
                        string movieFolder = Path.Combine(filmsBaseDir, cleanFileName);

                        if (!Directory.Exists(movieFolder))
                        {
                            Directory.CreateDirectory(movieFolder);
                        }

                        string destPath = Path.Combine(movieFolder, cleanFileName + extension);
                        if (!File.Exists(destPath))
                        {
                            File.Move(file, destPath);
                            result.MovedLog.Add($"• {Path.GetFileName(file)}  ➔  Films\\{cleanFileName}");
                        }
                    }
                    else
                    {
                        untouchedCount++;
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