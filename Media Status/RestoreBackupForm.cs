using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Media_Status
{
    public partial class RestoreBackupForm : Form
    {
        public BackupFileInfo? SelectedBackup { get; private set; }
        public bool UseAsNewMaster => chkUseAsMaster.Checked;
        private bool _darkMode;

        public static string BackupFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaControlApp",
            "media_state_backups"
        );

        public RestoreBackupForm(bool darkMode = false)
        {
            _darkMode = darkMode;
            InitializeComponent();

            if (_darkMode)
            {
                ApplyDarkMode();
            }

            LoadBackups();
        }

        private void LoadBackups()
        {
            lstBackups.Items.Clear();
            var backups = GetAvailableBackups();

            if (backups.Count == 0)
            {
                lblInfo.Text = "No backups found in AppData backup directory.";
                btnRestore.Enabled = false;
                return;
            }

            foreach (var backup in backups)
            {
                lstBackups.Items.Add(backup);
            }

            if (lstBackups.Items.Count > 0)
            {
                lstBackups.SelectedIndex = 0;
            }
        }

        public static List<BackupFileInfo> GetAvailableBackups()
        {
            var list = new List<BackupFileInfo>();

            if (!Directory.Exists(BackupFolder))
                return list;

            var files = new DirectoryInfo(BackupFolder).GetFiles("*.json")
                .OrderByDescending(f => f.LastWriteTime);

            foreach (var file in files)
            {
                list.Add(new BackupFileInfo
                {
                    FilePath = file.FullName,
                    FileName = file.Name,
                    CreatedTime = file.LastWriteTime,
                    SizeBytes = file.Length
                });
            }

            return list;
        }

        public static bool RestoreBackup(string backupFilePath, string targetMasterPath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                    return false;

                string targetDir = Path.GetDirectoryName(targetMasterPath) ?? string.Empty;
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                if (File.Exists(targetMasterPath))
                {
                    string preRestoreSafety = Path.Combine(BackupFolder, $"pre_restore_safety_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    Directory.CreateDirectory(BackupFolder);
                    File.Copy(targetMasterPath, preRestoreSafety, overwrite: true);
                }

                File.Copy(backupFilePath, targetMasterPath, overwrite: true);
                MediaCache.SaveLocal(MediaCache.Load(targetMasterPath));

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestoreBackup Error] {ex.Message}");
                return false;
            }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (lstBackups.SelectedItem is BackupFileInfo info)
            {
                SelectedBackup = info;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                DarkMessageBox.Show(this, "Please select a backup file to restore.", "Selection Required", MessageBoxButtons.OK, _darkMode);
                DialogResult = DialogResult.None;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ApplyDarkMode()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            lstBackups.BackColor = Color.FromArgb(45, 45, 48);
            lstBackups.ForeColor = Color.White;
            chkUseAsMaster.ForeColor = Color.White;

            btnRestore.FlatStyle = FlatStyle.Flat;
            btnRestore.BackColor = Color.FromArgb(60, 60, 65);
            btnRestore.ForeColor = Color.White;

            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.BackColor = Color.FromArgb(60, 60, 65);
            btnCancel.ForeColor = Color.White;
        }
    }

    // Data model container placed AFTER the Form so the Visual Studio designer is happy
    public class BackupFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public long SizeBytes { get; set; }

        public override string ToString()
        {
            return $"{CreatedTime:yyyy-MM-dd HH:mm:ss} — {FileName} ({SizeBytes / 1024.0:F1} KB)";
        }
    }
}