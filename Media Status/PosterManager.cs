using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;

namespace MediaControlApp
{
    public static class PosterManager
    {
        private static readonly string[] PosterFileNames = {
            "poster.jpg", "poster.jpeg", "poster.png",
            "folder.jpg", "folder.jpeg", "folder.png",
            "cover.jpg", "cover.jpeg", "cover.png"
        };

        /// <summary>
        /// Gets the master poster cache directory inside the tool's root/master location.
        /// </summary>
        public static string GetMasterCacheDir(string rootFolder)
        {
            string cacheDir = Path.Combine(rootFolder, "PosterCache");
            Directory.CreateDirectory(cacheDir);
            return cacheDir;
        }

        /// <summary>
        /// Cleans a raw title or filename for consistent cache naming and web searching.
        /// </summary>
        public static string CleanTitle(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string cleaned = input.Replace("★", string.Empty)
                                  .Replace("⭐", string.Empty)
                                  .Replace('.', ' ')
                                  .Replace('_', ' ')
                                  .Replace('-', ' ');

            cleaned = Regex.Replace(cleaned, @"\b(19|20)\d{2}\b", string.Empty);
            cleaned = Regex.Replace(cleaned, @"(?i)(1080p|720p|4k|2160p|bluray|web-dl|x264|x265|hevc|hdtv)", string.Empty);
            cleaned = Regex.Replace(cleaned, @"[\(\[].*?[\)\]]", string.Empty);
            return Regex.Replace(cleaned, @"\s+", " ").Trim();
        }

        /// <summary>
        /// Searches for a local poster file walking up from the target directory or file path.
        /// </summary>
        public static string FindLocalPosterFile(string targetDirOrFile)
        {
            if (string.IsNullOrEmpty(targetDirOrFile)) return null;

            string targetDir = targetDirOrFile;
            try
            {
                if (File.Exists(targetDirOrFile))
                {
                    targetDir = Path.GetDirectoryName(targetDirOrFile);
                }
                else if (!Directory.Exists(targetDirOrFile))
                {
                    string dir = Path.GetDirectoryName(targetDirOrFile);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        targetDir = dir;
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return null;

            DirectoryInfo currentDir = new DirectoryInfo(targetDir);
            for (int i = 0; i < 6 && currentDir != null; i++)
            {
                try
                {
                    foreach (var name in PosterFileNames)
                    {
                        string candidate = Path.Combine(currentDir.FullName, name);
                        if (File.Exists(candidate)) return candidate;
                    }
                    currentDir = currentDir.Parent;
                }
                catch { break; }
            }
            return null;
        }

        /// <summary>
        /// Loads an image safely into memory without locking the underlying file stream.
        /// </summary>
        public static Image LoadImageSafely(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var img = Image.FromStream(fs))
                {
                    return new Bitmap(img);
                }
            }
        }

        /// <summary>
        /// Downloads and caches a TV show poster via TVMaze.
        /// </summary>
        public static async Task<Image> ResolveOrDownloadTvPosterAsync(
            string targetDir, string rawTitle, string rootFolder, CancellationToken cancellationToken)
        {
            // 1. Check local folder artwork first
            string localFile = FindLocalPosterFile(targetDir);
            if (!string.IsNullOrEmpty(localFile))
            {
                try { return LoadImageSafely(localFile); } catch { }
            }

            // 2. Check master tool location cache next
            string cleanTitle = CleanTitle(rawTitle);
            if (string.IsNullOrEmpty(cleanTitle)) return null;

            string masterCacheDir = GetMasterCacheDir(rootFolder);
            string safeFileName = string.Join("_", cleanTitle.Split(Path.GetInvalidFileNameChars()));
            string masterCachedPath = Path.Combine(masterCacheDir, safeFileName + ".jpg");

            if (File.Exists(masterCachedPath))
            {
                try { return LoadImageSafely(masterCachedPath); } catch { File.Delete(masterCachedPath); }
            }

            // 3. Fallback: Download via TVMaze API
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                string searchUrl = $"https://api.tvmaze.com/singlesearch/shows?q={Uri.EscapeDataString(cleanTitle)}";

                HttpResponseMessage response = await client.GetAsync(searchUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        if (doc.RootElement.TryGetProperty("image", out var imgObj) &&
                            imgObj.TryGetProperty("medium", out var medProp))
                        {
                            string imageUrl = medProp.GetString();
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                byte[] imageBytes = await client.GetByteArrayAsync(imageUrl, cancellationToken);
                                await File.WriteAllBytesAsync(masterCachedPath, imageBytes, cancellationToken);

                                using (var ms = new MemoryStream(imageBytes))
                                {
                                    using (var img = Image.FromStream(ms))
                                    {
                                        return new Bitmap(img);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Downloads and caches a Film poster via IMDb.
        /// </summary>
        public static async Task<Image> ResolveOrDownloadFilmPosterAsync(
            string targetDir, string movieFileName, string rootFolder, CancellationToken cancellationToken)
        {
            string localFile = FindLocalPosterFile(targetDir);
            if (!string.IsNullOrEmpty(localFile))
            {
                try { return LoadImageSafely(localFile); } catch { }
            }

            string cleanTitle = CleanTitle(movieFileName);
            if (string.IsNullOrEmpty(cleanTitle)) return null;

            string masterCacheDir = GetMasterCacheDir(rootFolder);
            string safeFileName = string.Join("_", cleanTitle.Split(Path.GetInvalidFileNameChars()));
            string masterCachedPath = Path.Combine(masterCacheDir, safeFileName + ".jpg");

            if (File.Exists(masterCachedPath))
            {
                try { return LoadImageSafely(masterCachedPath); } catch { File.Delete(masterCachedPath); }
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                string searchUrl = $"https://v2.sg.media-imdb.com/suggestion/x/{Uri.EscapeDataString(cleanTitle)}.json";

                HttpResponseMessage response = await client.GetAsync(searchUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        if (doc.RootElement.TryGetProperty("d", out var dArr) && dArr.GetArrayLength() > 0)
                        {
                            var firstMatch = dArr[0];
                            if (firstMatch.TryGetProperty("i", out var imgObj) && imgObj.TryGetProperty("imageUrl", out var urlProp))
                            {
                                string imageUrl = urlProp.GetString();
                                if (!string.IsNullOrEmpty(imageUrl))
                                {
                                    byte[] imageBytes = await client.GetByteArrayAsync(imageUrl, cancellationToken);
                                    await File.WriteAllBytesAsync(masterCachedPath, imageBytes, cancellationToken);

                                    using (var ms = new MemoryStream(imageBytes))
                                    {
                                        using (var img = Image.FromStream(ms))
                                        {
                                            return new Bitmap(img);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}