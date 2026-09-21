using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Media_Status
{
    public class DarkMessageBox : Form
    {
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private DarkMessageBox(string text, string caption, MessageBoxButtons buttons, bool isDarkMode)
        {
            // Load saved font settings (fallback to Segoe UI / 11pt if missing)
            UserSettings settings = AppConfig.Load();
            string fontFamily = string.IsNullOrEmpty(settings.UIFontFamily) ? "Segoe UI" : settings.UIFontFamily;
            float baseSize = settings.UIFontSize > 0 ? settings.UIFontSize : 11.0f;

            // Form setup with dynamic sizing based on text scale
            this.Text = caption;
            int dynamicWidth = Math.Max(400, (int)(baseSize * 30));
            int dynamicHeight = Math.Max(180, (int)(baseSize * 14));
            this.Size = new Size(dynamicWidth, dynamicHeight);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Set Form font so all dynamic children inherit the family
            this.Font = new Font(fontFamily, baseSize, FontStyle.Regular);

            // Apply Native Dark Title Bar
            if (isDarkMode && IsWindows10OrGreater())
            {
                int darkModeEnabled = 1;
                if (DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeEnabled, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkModeEnabled, sizeof(int));
                }
            }

            // Apply Theme Colors
            Color mainBg = isDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
            Color bottomBg = isDarkMode ? Color.FromArgb(24, 24, 24) : Color.FromArgb(225, 225, 225);
            Color textColor = isDarkMode ? Color.White : Color.Black;
            Color secondaryBtnBg = isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(204, 204, 204);

            this.BackColor = mainBg;
            this.ForeColor = textColor;

            // Message Label using dynamic settings
            Label lblText = new Label()
            {
                Text = text,
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                ForeColor = textColor,
                Font = new Font(fontFamily, baseSize, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Bottom Panel for Buttons
            int buttonPanelHeight = (int)(baseSize * 3.8f);
            Panel pnlButtons = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = Math.Max(50, buttonPanelHeight),
                BackColor = bottomBg
            };

            int btnWidth = (int)(baseSize * 7.5f);
            int btnHeight = (int)(baseSize * 2.5f);
            int btnTop = (pnlButtons.Height - btnHeight) / 2;

            if (buttons == MessageBoxButtons.OK)
            {
                Button btnOk = new Button()
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Width = Math.Max(85, btnWidth),
                    Height = Math.Max(30, btnHeight),
                    Location = new Point(pnlButtons.Width - btnWidth - 20, btnTop),
                    Anchor = AnchorStyles.Right | AnchorStyles.Top,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    Font = new Font(fontFamily, baseSize, FontStyle.Bold)
                };
                btnOk.FlatAppearance.BorderSize = 0;

                pnlButtons.Controls.Add(btnOk);
                this.AcceptButton = btnOk;
                this.CancelButton = btnOk;
            }
            else if (buttons == MessageBoxButtons.YesNo)
            {
                Button btnYes = new Button()
                {
                    Text = "Yes",
                    DialogResult = DialogResult.Yes,
                    Width = Math.Max(85, btnWidth),
                    Height = Math.Max(30, btnHeight),
                    Location = new Point(pnlButtons.Width - (btnWidth * 2) - 30, btnTop),
                    Anchor = AnchorStyles.Right | AnchorStyles.Top,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    Font = new Font(fontFamily, baseSize, FontStyle.Bold)
                };
                btnYes.FlatAppearance.BorderSize = 0;

                Button btnNo = new Button()
                {
                    Text = "No",
                    DialogResult = DialogResult.No,
                    Width = Math.Max(85, btnWidth),
                    Height = Math.Max(30, btnHeight),
                    Location = new Point(pnlButtons.Width - btnWidth - 15, btnTop),
                    Anchor = AnchorStyles.Right | AnchorStyles.Top,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = secondaryBtnBg,
                    ForeColor = textColor,
                    Font = new Font(fontFamily, baseSize)
                };
                btnNo.FlatAppearance.BorderSize = 0;

                pnlButtons.Controls.Add(btnYes);
                pnlButtons.Controls.Add(btnNo);

                this.AcceptButton = btnYes;
                this.CancelButton = btnNo;
            }

            this.Controls.Add(lblText);
            this.Controls.Add(pnlButtons);
        }

        private static bool IsWindows10OrGreater()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10;
        }

        public static DialogResult Show(Form parent, string text, string caption, MessageBoxButtons buttons = MessageBoxButtons.OK, bool isDarkMode = true)
        {
            using (var msgBox = new DarkMessageBox(text, caption, buttons, isDarkMode))
            {
                return msgBox.ShowDialog(parent);
            }
        }

        public static DialogResult Show(Form parent, string text, string caption, string subText, MessageBoxButtons buttons = MessageBoxButtons.OK, bool isDarkMode = true)
        {
            string fullText = string.IsNullOrWhiteSpace(subText) ? text : $"{text}\n\n{subText}";
            return Show(parent, fullText, caption, buttons, isDarkMode);
        }

        public static DialogResult Show(Form parent, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, bool isDarkMode = true)
        {
            return Show(parent, text, caption, buttons, isDarkMode);
        }

        public static DialogResult Show(Form parent, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, bool isDarkMode = true)
        {
            return Show(parent, text, caption, buttons, isDarkMode);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons = MessageBoxButtons.OK, bool isDarkMode = true)
        {
            return Show(Form.ActiveForm, text, caption, buttons, isDarkMode);
        }


        public static DialogResult ShowCountdown(Form parent, string textFormat, string caption, int seconds = 30, bool isDarkMode = true)
        {
            using (var msgBox = new DarkMessageBox(string.Format(textFormat, seconds), caption, MessageBoxButtons.YesNo, isDarkMode))
            {
                // Locate the Label control created inside the DarkMessageBox constructor
                Label lblText = null;
                foreach (Control c in msgBox.Controls)
                {
                    if (c is Label l)
                    {
                        lblText = l;
                        break;
                    }
                }

                int remaining = seconds;
                using (var timer = new System.Windows.Forms.Timer())
                {
                    timer.Interval = 1000;
                    timer.Tick += (s, e) =>
                    {
                        remaining--;
                        if (remaining <= 0)
                        {
                            timer.Stop();
                            msgBox.DialogResult = DialogResult.Yes; // Auto-proceed on timeout
                            msgBox.Close();
                        }
                        else if (lblText != null)
                        {
                            lblText.Text = string.Format(textFormat, remaining);
                        }
                    };

                    timer.Start();
                    return msgBox.ShowDialog(parent);
                }
            }
        }



    }
}