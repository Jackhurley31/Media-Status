using MediaControlApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Media_Status
{
    public static class MediaCache
    {
        private static readonly object FileLock = new();

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };

        // Helper to get the active root directory (respecting media_state_redirect.txt)
        public static string GetActiveRootDirectory()
        {
            string redirectFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media_state_redirect.txt");
            if (File.Exists(redirectFile))
            {
                try
                {
                    string redirectedPath = File.ReadAllText(redirectFile).Trim();
                    if (!string.IsNullOrWhiteSpace(redirectedPath))
                    {
                        // If it points directly to a file path or directory, resolve accordingly
                        if (Directory.Exists(redirectedPath) || !Path.HasExtension(redirectedPath))
                        {
                            return redirectedPath;
                        }
                        else
                        {
                            string dir = Path.GetDirectoryName(redirectedPath);
                            if (!string.IsNullOrEmpty(dir)) return dir;
                        }
                    }
                }
                catch { }
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        // 1. Master file path (points to NAS via redirect file if present, otherwise local)
        public static string MasterPath
        {
            get
            {
                string redirectFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media_state_redirect.txt");
                if (File.Exists(redirectFile))
                {
                    try
                    {
                        string redirectedPath = File.ReadAllText(redirectFile).Trim();
                        if (!string.IsNullOrWhiteSpace(redirectedPath))
                        {
                            if (Path.HasExtension(redirectedPath) && redirectedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            {
                                return redirectedPath;
                            }
                            return Path.Combine(redirectedPath, "media_state.json");
                        }
                    }
                    catch { }
                }
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media_state.json");
            }
        }

        // 2. Isolated C: drive AppData cache path unique to the active root folder hash
        public static string LocalCachePath => GetLocalCachePath();

        private static string GetLocalCachePath()
        {
            string baseDir = GetActiveRootDirectory().TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );

            string folderHash = ComputeHash(baseDir);
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MediaControlApp"
            );

            return Path.Combine(appDataFolder, $"media_cache_{folderHash}.json");
        }

        private static string ComputeHash(string input)
        {
            using var md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant());
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 8);
        }

        /// <summary>
        /// Loads from Master first, falls back to C: drive cache, and syncs both additively.
        /// </summary>
        public static List<MediaItem> LoadMasterAndSync()
        {
            lock (FileLock)
            {
                var masterList = Load(MasterPath);
                var localList = Load(LocalCachePath);

                var merged = MergeLists(masterList, localList);

                SaveInternal(MasterPath, merged);
                SaveInternal(LocalCachePath, merged);

                return merged;
            }
        }

        public static void SaveAll(List<MediaItem> items)
        {
            lock (FileLock)
            {
                var masterList = Load(MasterPath);
                var localList = Load(LocalCachePath);

                var interim = MergeLists(masterList, localList);
                var finalMerged = MergeLists(interim, items);

                SaveInternal(MasterPath, finalMerged);
                SaveInternal(LocalCachePath, finalMerged);
            }
        }

        // =========================================================================
        // COMPATIBILITY WRAPPERS TO FIX CS0117 COMPILATION ERRORS
        // =========================================================================
        public static List<MediaItem> LoadLocal()
        {
            return LoadMasterAndSync();
        }

        public static void SaveLocal(List<MediaItem> items)
        {
            SaveAll(items);
        }

        public static void Save(string path, List<MediaItem> items)
        {
            lock (FileLock)
            {
                SaveInternal(path, items);
            }
        }
        // =========================================================================

        private static List<MediaItem> MergeLists(List<MediaItem> primary, List<MediaItem> secondary)
        {
            var dict = new Dictionary<string, MediaItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in primary)
            {
                if (string.IsNullOrEmpty(item.FilePath)) continue;
                string key = Path.GetFileName(item.FilePath);
                dict[key] = item;
            }

            foreach (var sec in secondary)
            {
                if (string.IsNullOrEmpty(sec.FilePath)) continue;
                string fileName = Path.GetFileName(sec.FilePath);

                if (dict.TryGetValue(fileName, out var pri))
                {
                    pri.WatchedByUser ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    pri.FavouriteByUser ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    pri.SavedPositionByUser ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                    sec.WatchedByUser ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    sec.FavouriteByUser ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    sec.SavedPositionByUser ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                    // Additive watch merge
                    foreach (var kvp in sec.WatchedByUser)
                    {
                        bool priWatched = pri.WatchedByUser.TryGetValue(kvp.Key, out var pw) && pw;
                        pri.WatchedByUser[kvp.Key] = priWatched || kvp.Value;
                    }

                    // Additive favorite merge
                    foreach (var kvp in sec.FavouriteByUser)
                    {
                        bool priFav = pri.FavouriteByUser.TryGetValue(kvp.Key, out var pf) && pf;
                        pri.FavouriteByUser[kvp.Key] = priFav || kvp.Value;
                    }

                    // Furthest playback position merge
                    foreach (var kvp in sec.SavedPositionByUser)
                    {
                        double priPos = pri.SavedPositionByUser.TryGetValue(kvp.Key, out var pp) ? pp : 0;
                        pri.SavedPositionByUser[kvp.Key] = Math.Max(priPos, kvp.Value);
                    }
                }
                else
                {
                    dict[fileName] = sec;
                }
            }

            return dict.Values.ToList();
        }

        private static void SaveInternal(string path, List<MediaItem> items)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(items, Options);
            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            File.WriteAllText(tempPath, json);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.Move(tempPath, path);
        }

        public static List<MediaItem> Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return new List<MediaItem>();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<MediaItem>>(json, Options)
                       ?? new List<MediaItem>();
            }
            catch
            {
                return new List<MediaItem>();
            }
        }
    }
}