using Media_Status;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MediaControlApp
{
    public partial class OptionsForm : Form
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<float, string> OnFontSettingsChanged { get; set; }

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private readonly VideoSettings _videoSettings;
        private UserSettings _userSettings;
        private bool _darkMode;

        /// <summary>
        /// Callback executed when dark mode is toggled, allowing MainForm to update its UI live.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<bool> OnDarkModeChanged { get; set; }

        public OptionsForm(VideoSettings videoSettings, bool currentDarkMode)
        {
            InitializeComponent();
            _videoSettings = videoSettings;
            _darkMode = currentDarkMode;
        }

        private void OptionsForm_Load(object sender, EventArgs e)
        {
            _userSettings = AppConfig.Load();

            // 1. Expanded standard font size range (8pt to 24pt)
            cmbFontSize.Items.Clear();
            cmbFontSize.Items.AddRange(new object[] {
                "8", "9", "10", "11", "12", "13", "14", "15", "16", "18", "20", "22", "24"
            });

            string savedSize = _userSettings.UIFontSize.ToString("0.#");
            cmbFontSize.SelectedItem = cmbFontSize.Items.Contains(savedSize) ? savedSize : "11";

            // 2. Automatically load all installed system fonts
            cmbFontFamily.Items.Clear();
            using (InstalledFontCollection fonts = new InstalledFontCollection())
            {
                foreach (FontFamily family in fonts.Families)
                {
                    // Only add fonts that support Regular style to avoid runtime exceptions
                    if (family.IsStyleAvailable(FontStyle.Regular))
                    {
                        cmbFontFamily.Items.Add(family.Name);
                    }
                }
            }

            // Set saved font or fall back to Segoe UI
            string savedFont = _userSettings.UIFontFamily;
            if (!string.IsNullOrEmpty(savedFont) && cmbFontFamily.Items.Contains(savedFont))
            {
                cmbFontFamily.SelectedItem = savedFont;
            }
            else
            {
                cmbFontFamily.SelectedItem = "Segoe UI";
            }

            txtPlayerPath.Text = _userSettings.CustomPlayerPath ?? string.Empty;
            chkRunOnStartup.Checked = _userSettings.RunOnStartup;

            RefreshExtensionList();
            ApplyDarkMode(_darkMode);
        }

        private void cmbFontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            PreviewFontChange();
        }

        private void cmbFontFamily_SelectedIndexChanged(object sender, EventArgs e)
        {
            PreviewFontChange();
        }

        private void PreviewFontChange()
        {
            if (float.TryParse(cmbFontSize.SelectedItem?.ToString(), out float size))
            {
                string selectedFont = cmbFontFamily.SelectedItem?.ToString() ?? "Segoe UI";

                // 1. Notify MainForm to preview BOTH font family and size live
                OnFontSettingsChanged?.Invoke(size, selectedFont);

                // 2. Apply ONLY size scaling to OptionsForm (keeping standard font family)
                FontHelper.ApplyFontToForm(this, size);
            }
        }

        public static class FontHelper
        {
            // Default standard font family for OptionsForm controls
            private const string DefaultFormFontFamily = "Segoe UI";

            // 1. Two-parameter signature (uses default Segoe UI font)
            public static void ApplyFontToForm(Form form, float fontSize)
            {
                ApplyFontToForm(form, fontSize, DefaultFormFontFamily);
            }

            // 2. Three-parameter signature (resolves CS1501 calls from Media_Status.cs)
            public static void ApplyFontToForm(Form form, float fontSize, string fontFamilyName)
            {
                if (form == null || form.IsDisposed) return;

                if (string.IsNullOrWhiteSpace(fontFamilyName))
                {
                    fontFamilyName = DefaultFormFontFamily;
                }

                form.SuspendLayout();
                ApplyFontRecursive(form.Controls, fontFamilyName, fontSize);
                form.ResumeLayout(true);
            }

            private static void ApplyFontRecursive(Control.ControlCollection controls, string fontFamilyName, float fontSize)
            {
                foreach (Control c in controls)
                {
                    // Skip target font preview selectors to keep dropdown text readable
                    if (c.Name == "cmbFontFamily" || c.Name == "cmbFontSize")
                    {
                        continue;
                    }

                    try
                    {
                        // Scale size using specified font family while preserving bold/regular style
                        FontStyle currentStyle = c.Font != null ? c.Font.Style : FontStyle.Regular;
                        c.Font = new Font(fontFamilyName, fontSize, currentStyle);
                    }
                    catch
                    {
                        c.Font = new Font(DefaultFormFontFamily, fontSize, FontStyle.Regular);
                    }

                    // Keep ListBox rows visually clean when scaling font size
                    if (c is ListBox lb)
                    {
                        lb.ItemHeight = (int)(fontSize * 1.8f);
                    }

                    // Recurse into child controls (GroupBoxes, Panels, TableLayoutPanels)
                    if (c.HasChildren)
                    {
                        ApplyFontRecursive(c.Controls, fontFamilyName, fontSize);
                    }
                }
            }
        }

        private void chkRunOnStartup_CheckedChanged(object sender, EventArgs e)
        {
            _userSettings.RunOnStartup = chkRunOnStartup.Checked;
            AppConfig.Save(_userSettings);
        }

        private void BtnDarkMode_Click(object sender, EventArgs e)
        {
            _darkMode = !_darkMode;

            // Save preference to disk
            _userSettings.DarkMode = _darkMode;
            AppConfig.Save(_userSettings);

            // Apply locally to OptionsForm
            ApplyDarkMode(_darkMode);

            // Notify MainForm to update instantly
            OnDarkModeChanged?.Invoke(_darkMode);
        }

        private void ApplyDarkMode(bool enabled)
        {
            Color backColor = enabled ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            Color textColor = enabled ? Color.White : Color.Black;
            Color buttonBackColor = enabled ? Color.FromArgb(50, 50, 50) : SystemColors.ControlLight;
            Color buttonBorderColor = enabled ? Color.FromArgb(70, 70, 70) : Color.Gray;

            this.BackColor = backColor;

            // Apply native dark title bar for Windows 10/11
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10)
            {
                int darkModeEnabled = enabled ? 1 : 0;
                if (DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeEnabled, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkModeEnabled, sizeof(int));
                }
            }

            // Apply theme recursively across all child controls (including inside GroupBoxes and Panels)
            ApplyDarkModeRecursive(this.Controls, enabled, backColor, textColor, buttonBackColor, buttonBorderColor);

            // Specific control override for ListBox & TextBox
            if (lstVideoExtensions != null)
            {
                lstVideoExtensions.BackColor = enabled ? Color.FromArgb(45, 45, 45) : Color.White;
                lstVideoExtensions.ForeColor = textColor;
            }

            if (txtNewExtension != null)
            {
                txtNewExtension.BackColor = enabled ? Color.FromArgb(45, 45, 45) : Color.White;
                txtNewExtension.ForeColor = textColor;
            }

            if (txtPlayerPath != null)
            {
                txtPlayerPath.BackColor = enabled ? Color.FromArgb(45, 45, 45) : Color.White;
                txtPlayerPath.ForeColor = textColor;
            }
        }

        private void ApplyDarkModeRecursive(Control.ControlCollection controls, bool enabled, Color backColor, Color textColor, Color buttonBackColor, Color buttonBorderColor)
        {
            foreach (Control c in controls)
            {
                c.BackColor = backColor;
                c.ForeColor = textColor;

                if (c is Button btn)
                {
                    btn.FlatStyle = enabled ? FlatStyle.Flat : FlatStyle.Standard;
                    if (enabled)
                    {
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatAppearance.BorderColor = buttonBorderColor;
                        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 65, 65);
                        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 80, 80);
                        btn.BackColor = buttonBackColor;
                        btn.ForeColor = Color.White;
                    }
                    else
                    {
                        btn.UseVisualStyleBackColor = true;
                        btn.ForeColor = SystemColors.ControlText;
                    }
                }
                else if (c is ComboBox cmb)
                {
                    cmb.BackColor = enabled ? Color.FromArgb(45, 45, 45) : Color.White;
                    cmb.ForeColor = textColor;
                }
                else if (c is CheckBox chk)
                {
                    chk.ForeColor = textColor;
                }

                if (c.HasChildren)
                {
                    ApplyDarkModeRecursive(c.Controls, enabled, backColor, textColor, buttonBackColor, buttonBorderColor);
                }
            }
        }

        private void RefreshExtensionList()
        {
            lstVideoExtensions.DataSource = null;
            lstVideoExtensions.DataSource = _videoSettings.GetExtensionsList();
        }

        private void btnAddExtension_Click(object sender, EventArgs e)
        {
            string newExt = txtNewExtension.Text.Trim();

            if (string.IsNullOrWhiteSpace(newExt))
            {
                DarkMessageBox.Show(this, "Please enter an extension (e.g., .flv or flv).", "Invalid Input", MessageBoxButtons.OK, _darkMode);
                return;
            }

            if (_videoSettings.AddExtension(newExt))
            {
                txtNewExtension.Clear();
                RefreshExtensionList();
            }
            else
            {
                DarkMessageBox.Show(this, $"'{newExt}' is already in the list.", "Duplicate", MessageBoxButtons.OK, _darkMode);
            }
        }

        private void btnRemoveExtension_Click(object sender, EventArgs e)
        {
            if (lstVideoExtensions.SelectedItem == null)
            {
                DarkMessageBox.Show(this, "Select an extension to remove.", "Selection Required", MessageBoxButtons.OK, _darkMode);
                return;
            }

            string selectedExt = lstVideoExtensions.SelectedItem.ToString();

            if (DarkMessageBox.Show(this, $"Remove '{selectedExt}'?", "Confirm", MessageBoxButtons.YesNo, _darkMode) == DialogResult.Yes)
            {
                if (_videoSettings.RemoveExtension(selectedExt))
                {
                    RefreshExtensionList();
                }
            }
        }

        private void btnVLCpath_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                openFileDialog.Title = "Select Custom Player Executable (e.g. vlc.exe)";

                if (!string.IsNullOrWhiteSpace(txtPlayerPath.Text) && File.Exists(txtPlayerPath.Text))
                {
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(txtPlayerPath.Text);
                }

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtPlayerPath.Text = openFileDialog.FileName;
                }
            }
        }

        private void OptionsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            string newPath = txtPlayerPath.Text.Trim();
            string existingPath = _userSettings.CustomPlayerPath ?? string.Empty;

            if (!string.Equals(newPath, existingPath, StringComparison.OrdinalIgnoreCase))
            {
                DialogResult result = DarkMessageBox.Show(
                    this,
                    "Are you sure you want to save changes to the VLC path?",
                    "Save Changes",
                    MessageBoxButtons.YesNo,
                    _darkMode
                );

                if (result == DialogResult.Yes)
                {
                    _userSettings.CustomPlayerPath = newPath;
                }
            }

            // Save Font Settings on Form Close
            if (float.TryParse(cmbFontSize.SelectedItem?.ToString(), out float size))
            {
                _userSettings.UIFontSize = size;
            }
            _userSettings.UIFontFamily = cmbFontFamily.SelectedItem?.ToString() ?? "Segoe UI";

            // Save all settings to disk once when form closes
            AppConfig.Save(_userSettings);
        }
    }
}