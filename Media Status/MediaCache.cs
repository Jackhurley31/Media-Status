using MediaControlApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Gets the local cache file path on C drive, dynamically generated using a hash 
        /// of the application's base directory so multiple instances remain isolated.
        /// </summary>
        public static string LocalCachePath => GetLocalCachePath();

        private static string GetLocalCachePath()
        {
            // Normalize current application directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );

            // Compute an 8-character hex hash based on the directory path
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
        /// Convenient shortcut to save directly to the fast local C drive.
        /// </summary>
        public static void SaveLocal(List<MediaItem> items)
        {
            string cachePath = LocalCachePath;
            string dir = Path.GetDirectoryName(cachePath)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            Save(cachePath, items);
        }

        /// <summary>
        /// Convenient shortcut to load directly from the fast local C drive.
        /// </summary>
        public static List<MediaItem> LoadLocal()
        {
            return Load(LocalCachePath);
        }

        public static void Save(string path, List<MediaItem> items)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(items, Options);

            // Use a unique temp file name per save to prevent multi-threaded collision on network shares
            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            // Synchronize threads within the same process to prevent concurrent file access
            lock (FileLock)
            {
                int maxRetries = 5;
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        File.WriteAllText(tempPath, json);

                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }

                        File.Move(tempPath, path);
                        break;
                    }
                    catch (IOException)
                    {
                        if (i == maxRetries - 1) throw;
                        Thread.Sleep(300); // Slightly increased delay for network drives
                    }
                    finally
                    {
                        // Clean up the unique temp file if it somehow got left behind
                        try
                        {
                            if (File.Exists(tempPath))
                            {
                                File.Delete(tempPath);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        public static List<MediaItem> Load(string path)
        {
            lock (FileLock)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return new List<MediaItem>();

                try
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<List<MediaItem>>(json, Options)
                           ?? new List<MediaItem>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load cache from {path}: {ex.Message}");
                    return new List<MediaItem>();
                }
            }
        }
    }
}