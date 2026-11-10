using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Media_Status
{
    public class UserSettings
    {
        public DateTime? NextUpdateReminder { get; set; }

        public bool DarkMode { get; set; } = true;
        public string CustomPlayerPath { get; set; } = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
        public bool RunOnStartup { get; set; } = false;
        public bool EnableSubtitles { get; set; } = true;

        // Font Accessibility Settings
        public float UIFontSize { get; set; } = 11.0f; // Default base size (e.g. 11pt - 16pt)
        public string UIFontFamily { get; set; } = "Arial";

        public bool EnableFullscreen { get; set; } = true;
        public int TargetScreenIndex { get; set; } = 0; // 0 = Primary / Default


        public int SplitMainDistance { get; set; } = 354;
        public int SplitContainer1Distance { get; set; } = 207;


        // Auto-Renamer Local Settings
        public string RenameFormatPattern { get; set; } = "{Show} - S{Season}E{Episode} - {Title}";

        public bool RenameIncludeShow { get; set; } = true;
        public bool RenamePadNumbers { get; set; } = true;
        public bool RenameReplaceWithSpaces { get; set; } = true;
        public bool RenameReplaceDots { get; set; } = true;
        public bool RenameRemoveSpecial { get; set; } = true;
    }

    public static class AppConfig
    {
        private static readonly string LocalAppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaControlApp"
        );

        // Dynamically routes users.json to the active NAS directory (via redirect file) or falls back locally
        public static string UsersFilePath => Path.Combine(Media_Status.MediaCache.GetActiveRootDirectory(), "users.json");

        // Dynamically routes app settings to the active NAS directory (via redirect file) or falls back locally
        public static string ConfigFilePath => Path.Combine(LocalAppDataFolder, $"app_settings_{CurrentUser.ToLowerInvariant()}.json");

        public static string CurrentUser { get; private set; } = Environment.UserName;

        private static readonly string DefaultVlcPath = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "MediaStatusApp";

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(LocalAppDataFolder))
            {
                Directory.CreateDirectory(LocalAppDataFolder);
            }
        }

        /// <summary>
        /// Checks if any users exist. If not, prompts the setup dialog.
        /// Returns true if a user session is active, false if cancelled.
        /// </summary>
        public static bool InitializeUserSession()
        {
            EnsureDirectoryExists();
            UserDatabase db = LoadUserDatabase();

            if (db.Users == null)
            {
                db.Users = new List<UserProfile>();
            }

            string windowsUser = Environment.UserName;
            bool userExists = db.Users.Exists(u => u.Username.Equals(windowsUser, StringComparison.OrdinalIgnoreCase));

            if (!userExists)
            {
                using (var setupForm = new UserSetupForm())
                {
                    if (setupForm.ShowDialog() == DialogResult.OK)
                    {
                        string chosenUser = string.IsNullOrWhiteSpace(setupForm.SelectedUsername) ? windowsUser : setupForm.SelectedUsername.Trim();

                        if (!db.Users.Exists(u => u.Username.Equals(chosenUser, StringComparison.OrdinalIgnoreCase)))
                        {
                            db.Users.Add(new UserProfile { Username = chosenUser, IsDefault = false });
                        }

                        db.ActiveUser = chosenUser;
                        SaveUserDatabase(db);
                        CurrentUser = chosenUser;
                    }
                    else
                    {
                        return false; // User cancelled setup
                    }
                }
            }
            else
            {
                CurrentUser = windowsUser;
                db.ActiveUser = windowsUser;
                SaveUserDatabase(db);
            }

            return true;
        }

        public static UserDatabase LoadUserDatabase()
        {
            try
            {
                string path = UsersFilePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<UserDatabase>(json) ?? new UserDatabase();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load users: {ex.Message}");
            }

            return new UserDatabase();
        }

        public static void SaveUserDatabase(UserDatabase db)
        {
            try
            {
                string path = UsersFilePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save users: {ex.Message}");
            }
        }

        public static void SyncStartupPath(UserSettings settings)
        {
            if (settings.RunOnStartup)
            {
                SetStartupRegistry(true);
            }
        }

        public static void SwitchUser(string username)
        {
            UserDatabase db = LoadUserDatabase();

            if (!db.Users.Exists(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                db.Users.Add(new UserProfile { Username = username, IsDefault = false });
            }

            db.ActiveUser = username;
            SaveUserDatabase(db);
            CurrentUser = username;
        }

        public static List<string> GetAllUsernames()
        {
            UserDatabase db = LoadUserDatabase();
            List<string> names = new List<string>();
            foreach (var user in db.Users)
            {
                names.Add(user.Username);
            }
            return names;
        }

        public static UserSettings Load()
        {
            UserSettings settings = null;
            bool forceSave = false;

            string dynamicConfigPath = ConfigFilePath;

            try
            {
                if (File.Exists(dynamicConfigPath))
                {
                    string rawJson = File.ReadAllText(dynamicConfigPath);

                    if (!rawJson.Contains("CustomPlayerPath") || !rawJson.Contains("RunOnStartup") || !rawJson.Contains("UIFontSize"))
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
                string path = ConfigFilePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (string.IsNullOrWhiteSpace(settings.CustomPlayerPath))
                {
                    settings.CustomPlayerPath = DefaultVlcPath;
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);

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
                            string executablePath = Application.ExecutablePath;
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