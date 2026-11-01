using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using MediaControlApp.Forms;
using Media_Status;

namespace MediaControlApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. Check if running in an empty folder and offer to configure redirect before anything loads
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string redirectFile = Path.Combine(baseDir, "media_state_redirect.txt");
            string localJson = Path.Combine(baseDir, "media_state.json");

            bool hasRedirect = File.Exists(redirectFile);
            bool hasLocalJson = File.Exists(localJson);
            bool hasVideos = false;

            try
            {
                var videoExts = new HashSet<string>(new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v" }, StringComparer.OrdinalIgnoreCase);
                hasVideos = Directory.EnumerateFiles(baseDir, "*.*", SearchOption.AllDirectories)
                    .Any(f => videoExts.Contains(Path.GetExtension(f)));
            }
            catch { }

            if (!hasRedirect && !hasLocalJson && !hasVideos)
            {
                using (var promptForm = new Form())
                {
                    promptForm.Text = "Empty Local Folder Setup";
                    promptForm.StartPosition = FormStartPosition.CenterScreen;
                    promptForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    promptForm.MaximizeBox = false;
                    promptForm.MinimizeBox = false;
                    promptForm.ClientSize = new Size(380, 150);

                    var lbl = new Label
                    {
                        Text = "This local folder contains no media files or configuration.\n\nWould you like to create a 'media_state_redirect.txt' file to point to your NAS share?",
                        Dock = DockStyle.Top,
                        Height = 80,
                        TextAlign = ContentAlignment.MiddleCenter
                    };

                    var btnYes = new Button { Text = "Yes", DialogResult = DialogResult.Yes, Width = 80, Height = 30, Location = new Point(100, 105) };
                    var btnNo = new Button { Text = "No", DialogResult = DialogResult.No, Width = 80, Height = 30, Location = new Point(190, 105) };

                    promptForm.Controls.Add(lbl);
                    promptForm.Controls.Add(btnYes);
                    promptForm.Controls.Add(btnNo);
                    promptForm.AcceptButton = btnYes;
                    promptForm.CancelButton = btnNo;

                    if (promptForm.ShowDialog() == DialogResult.Yes)
                    {
                        using (var fbd = new FolderBrowserDialog())
                        {
                            fbd.Description = "Select your remote NAS media folder path (over Tailscale)";
                            if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                            {
                                try
                                {
                                    File.WriteAllText(redirectFile, fbd.SelectedPath.Trim());
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            // 2. Checks for users.json; if missing, triggers your dark-themed UserSetupForm
            if (!AppConfig.InitializeUserSession())
            {
                return; // Exits the application cleanly if the user cancels setup
            }

            Application.Run(new MainForm());
        }
    }
}