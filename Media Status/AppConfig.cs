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
    }

    public static class AppConfig
    {
        private static readonly string AppDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MediaControlApp"
                );

        // users.json will live in the folder where the tool is running
        public static readonly string UsersFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "users.json");

        // Settings files will stay in Local AppData
        public static string ConfigFilePath => Path.Combine(AppDataFolder, $"app_settings_{CurrentUser.ToLowerInvariant()}.json");

        public static string CurrentUser { get; private set; } = Environment.UserName;

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

            // Match the profile name to the current Windows computer user
            string windowsUser = Environment.UserName;

            // Check if this Windows user exists in the shared NAS users.json file
            bool userExists = db.Users.Exists(u => u.Username.Equals(windowsUser, StringComparison.OrdinalIgnoreCase));

            if (!userExists)
            {
                // If this computer's user isn't registered yet, prompt to create/set up their profile
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
                // Automatically lock/select the profile matching this Windows computer user
                CurrentUser = windowsUser;

                // Optionally update active user in db so it reflects accurately
                db.ActiveUser = windowsUser;
                SaveUserDatabase(db);
            }

            return true;
        }

        public static UserDatabase LoadUserDatabase()
        {
            try
            {
                if (File.Exists(UsersFilePath))
                {
                    string json = File.ReadAllText(UsersFilePath);
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
                EnsureDirectoryExists();
                string json = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(UsersFilePath, json);
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
                // Re-applies the path of whichever executable is currently running
                SetStartupRegistry(true);
            }
        }


        /// <summary>
        /// Switches the active user, saves the database, and returns the updated username.
        /// </summary>
        public static void SwitchUser(string username)
        {
            UserDatabase db = LoadUserDatabase();

            // If the user doesn't exist in the list yet, add them
            if (!db.Users.Exists(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                db.Users.Add(new UserProfile { Username = username, IsDefault = false });
            }

            db.ActiveUser = username;
            SaveUserDatabase(db);

            // Update the runtime property
            CurrentUser = username;
        }

        /// <summary>
        /// Gets a list of all registered usernames.
        /// </summary>
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