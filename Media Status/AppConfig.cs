using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32; // Added for Registry access

namespace Media_Status
{
    public class UserSettings
    {
        public bool DarkMode { get; set; } = true;
        public string CustomPlayerPath { get; set; } = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
        public bool RunOnStartup { get; set; } = false;
        public bool EnableSubtitles { get; set; } = true;

        // Font Accessibility Settings
        public float UIFontSize { get; set; } = 11.0f; // Default base size (e.g. 11pt - 16pt)
        public string UIFontFamily { get; set; } = "Arial";
    }

    public static class AppConfig
    {
        public static string CurrentUser => Environment.UserName;

        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaControlApp"
        );

        public static readonly string ConfigFilePath = Path.Combine(AppDataFolder, "app_settings.json");
        private static readonly string DefaultVlcPath = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "MediaStatusApp";

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
        }


        public static void SyncStartupPath(UserSettings settings)
        {
            if (settings.RunOnStartup)
            {
                // Re-applies the path of whichever executable is currently running
                SetStartupRegistry(true);
            }
        }



        public static UserSettings Load()
        {
            UserSettings settings = null;
            bool forceSave = false;

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string rawJson = File.ReadAllText(ConfigFilePath);

                    if (!rawJson.Contains("CustomPlayerPath") || !rawJson.Contains("RunOnStartup"))
                    {
                        forceSave = true;
                    }

                    settings = JsonSerializer.Deserialize<UserSettings>(rawJson);
                }
                else
                {
                    forceSave = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
                forceSave = true;
            }

            if (settings == null)
            {
                settings = new UserSettings();
                forceSave = true;
            }

            if (string.IsNullOrWhiteSpace(settings.CustomPlayerPath))
            {
                settings.CustomPlayerPath = DefaultVlcPath;
                forceSave = true;
            }

            if (forceSave)
            {
                Save(settings);
            }

            return settings;
        }

        public static void Save(UserSettings settings)
        {
            try
            {
                EnsureDirectoryExists();

                if (string.IsNullOrWhiteSpace(settings.CustomPlayerPath))
                {
                    settings.CustomPlayerPath = DefaultVlcPath;
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);

                // Update registry startup entry
                SetStartupRegistry(settings.RunOnStartup);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public static void SetStartupRegistry(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string executablePath = System.Windows.Forms.Application.ExecutablePath;
                            key.SetValue(AppName, $"\"{executablePath}\"");
                        }
                        else
                        {
                            if (key.GetValue(AppName) != null)
                            {
                                key.DeleteValue(AppName, false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update registry startup key: {ex.Message}");
            }
        }
    }
}