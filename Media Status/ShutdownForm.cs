using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Media_Status
{
    public partial class ShutdownForm : Form
    {
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private bool darkMode = false;

        public int SelectedActionIndex => cmbAction.SelectedIndex;
        public int SelectedTriggerIndex => cmbTrigger.SelectedIndex;
        public bool EnableCountdown => chkEnableTimer.Checked;

        // Helper property to check if an actual post-playback action is armed
        public bool IsActionActive => cmbAction.SelectedIndex > 0;

        public ShutdownForm(int currentAction = 0, int currentTrigger = 0, bool enableCountdown = true, bool isDarkMode = false)
        {
            InitializeComponent();

            this.darkMode = isDarkMode;

            // Load saved Font preferences
            UserSettings settings = AppConfig.Load();
            string fontFamily = string.IsNullOrEmpty(settings.UIFontFamily) ? "Segoe UI" : settings.UIFontFamily;
            float fontSize = settings.UIFontSize > 0 ? settings.UIFontSize : 11.0f;

            // Set Form font so WinForms auto-scales child controls cleanly
            this.Font = new Font(fontFamily, fontSize, FontStyle.Regular);

            ApplyDarkMode(darkMode);

            cmbAction.SelectedIndex = currentAction >= 0 && currentAction < cmbAction.Items.Count ? currentAction : 0;
            cmbTrigger.SelectedIndex = currentTrigger >= 0 && currentTrigger < cmbTrigger.Items.Count ? currentTrigger : 0;
            chkEnableTimer.Checked = enableCountdown;
        }

        private void ApplyDarkMode(bool enabled)
        {
            Color back = enabled ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            Color text = enabled ? Color.White : Color.Black;

            this.BackColor = back;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10)
            {
                int darkModeEnabled = enabled ? 1 : 0;
                if (DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeEnabled, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkModeEnabled, sizeof(int));
                }
            }

            ApplyControlTheme(this.Controls, back, text, enabled);
        }

        private void ApplyControlTheme(Control.ControlCollection controls, Color back, Color text, bool enabled)
        {
            foreach (Control c in controls)
            {
                if (c is GroupBox gb)
                {
                    gb.ForeColor = text;
                    gb.BackColor = Color.Transparent;

                    if (gb.HasChildren)
                    {
                        ApplyControlTheme(gb.Controls, back, text, enabled);
                    }
                }
                else if (c is TextBox || c is ListBox || c is ComboBox)
                {
                    c.BackColor = enabled ? Color.FromArgb(45, 45, 45) : Color.White;
                    c.ForeColor = text;
                }
                else if (c is Label || c is CheckBox)
                {
                    c.BackColor = Color.Transparent;
                    c.ForeColor = text;
                }
                else
                {
                    c.BackColor = back;
                    c.ForeColor = text;

                    if (c.HasChildren)
                    {
                        ApplyControlTheme(c.Controls, back, text, enabled);
                    }
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            // Reset options to disabled state
            cmbAction.SelectedIndex = 0; // "Disabled / Do Nothing"
            cmbTrigger.SelectedIndex = 0;
            chkEnableTimer.Checked = true;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}