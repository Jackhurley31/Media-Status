using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MediaControlApp
{
    public class VideoSettings
    {
        private static readonly string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaControlApp"
        );


        private static readonly string FilePath = Path.Combine(FolderPath, "video_extensions.json");

        private HashSet<string> _extensions;

        public VideoSettings()
        {
            _extensions = LoadSettings();
        }

        public string[] ExtensionsArray => _extensions.ToArray();


        public List<string> GetExtensionsList()
        {
            return _extensions.OrderBy(x => x).ToList();
        }



        public bool IsVideoFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            string ext = Path.GetExtension(filePath);
            return !string.IsNullOrEmpty(ext) && _extensions.Contains(ext);
        }

        public bool AddExtension(string extension)
        {
            string formatted = FormatExtension(extension);
            if (string.IsNullOrEmpty(formatted)) return false;

            if (_extensions.Add(formatted))
            {
                SaveSettings();
                return true;
            }
            return false;
        }

        public bool RemoveExtension(string extension)
        {
            string formatted = FormatExtension(extension);
            if (string.IsNullOrEmpty(formatted)) return false;

            if (_extensions.Remove(formatted))
            {
                SaveSettings();
                return true;
            }
            return false;
        }

        private void SaveSettings()
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_extensions, options);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save video settings: {ex.Message}");
            }
        }

        private HashSet<string> LoadSettings()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    var loaded = JsonSerializer.Deserialize<List<string>>(json);
                    if (loaded != null)
                    {
                        return new HashSet<string>(loaded, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load settings, using defaults: {ex.Message}");
                }
            }

            var defaults = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".ts", ".m4v"
            };

            try
            {
                if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
                string json = JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }

            return defaults;
        }

        private string FormatExtension(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext)) return string.Empty;

            ext = ext.Trim();
            return ext.StartsWith(".") ? ext : "." + ext;
        }
    }
}