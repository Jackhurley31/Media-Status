using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Media_Status.Properties;

namespace MediaStatus
{
    public partial class AutoRenameSettingsForm : Form
    {
        public string CustomFormatPattern => txtCustomFormat.Text;
        public bool IncludeShow => itemIncludeShow.Checked;
        public bool PadNumbers => itemPadNumbers.Checked;
        public bool ReplaceWithSpaces => itemReplaceWithSpaces.Checked;
        public bool ReplaceDots => itemReplaceDots.Checked;
        public bool RemoveSpecial => itemRemoveSpecial.Checked;

        public AutoRenameSettingsForm(bool darkMode = false)
        {
            InitializeComponent();

            if (darkMode)
            {
                ApplyDarkModeToSettingsForm(this);
            }

            AttachEvents();
            UpdatePreview();
        }

        private void AttachEvents()
        {
            btnTagShow.Click += (s, e) => InsertTag("{Show}");
            btnTagSeason.Click += (s, e) => InsertTag("{Season}");
            btnTagEpisode.Click += (s, e) => InsertTag("{Episode}");
            btnTagTitle.Click += (s, e) => InsertTag("{Title}");

            txtCustomFormat.TextChanged += (s, e) => UpdatePreview();
            itemIncludeShow.CheckedChanged += (s, e) => UpdatePreview();
            itemPadNumbers.CheckedChanged += (s, e) => UpdatePreview();
            itemReplaceWithSpaces.CheckedChanged += (s, e) => UpdatePreview();
            itemReplaceDots.CheckedChanged += (s, e) => UpdatePreview();
            itemRemoveSpecial.CheckedChanged += (s, e) => UpdatePreview();
        }

        public void InsertTag(string tag)
        {
            int selectionStart = txtCustomFormat.SelectionStart;
            txtCustomFormat.Text = txtCustomFormat.Text.Insert(selectionStart, tag);
            txtCustomFormat.SelectionStart = selectionStart + tag.Length;
            txtCustomFormat.Focus();
        }

        private void UpdatePreview()
        {
            string show = itemIncludeShow.Checked ? "Breaking Bad" : string.Empty;
            int season = 1;
            int episode = 3;
            string title = "And_the_Bag's_in_the_River";
            string ext = ".mkv";

            string seasonStr = itemPadNumbers.Checked ? season.ToString("D2") : season.ToString();
            string episodeStr = itemPadNumbers.Checked ? episode.ToString("D2") : episode.ToString();

            string format = string.IsNullOrWhiteSpace(txtCustomFormat.Text)
                ? "{Show} - S{Season}E{Episode} - {Title}"
                : txtCustomFormat.Text;

            string formattedName = format
                .Replace("{Show}", show)
                .Replace("{Season}", seasonStr)
                .Replace("{Episode}", episodeStr)
                .Replace("{Title}", title);

            if (itemReplaceWithSpaces.Checked)
            {
                formattedName = formattedName.Replace('_', ' ');
            }

            if (itemReplaceDots.Checked)
            {
                formattedName = formattedName.Replace('.', ' ');
            }

            if (itemRemoveSpecial.Checked)
            {
                foreach (char invalidChar in Path.GetInvalidFileNameChars())
                {
                    formattedName = formattedName.Replace(invalidChar.ToString(), string.Empty);
                }
            }

            formattedName = Regex.Replace(formattedName, @"\s+", " ").Trim();
            lblPreviewOutput.Text = $"Result:   {formattedName}{ext}";
        }

        private void ApplyDarkModeToSettingsForm(Control parent)
        {
            Color darkBg = Color.FromArgb(32, 32, 32);      // Main background
            Color textFg = Color.FromArgb(240, 240, 240);   // Primary text
            Color buttonBg = Color.FromArgb(60, 60, 65);    // Button background
            Color buttonHover = Color.FromArgb(80, 80, 85); // Button hover background
            Color borderColor = Color.FromArgb(90, 90, 90); // Subtle borders

            this.BackColor = darkBg;
            this.ForeColor = textFg;

            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case GroupBox gb:
                        gb.ForeColor = textFg;
                        gb.BackColor = darkBg;
                        ApplyDarkModeToSettingsForm(gb);
                        break;

                    case Button btn:
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.BackColor = buttonBg;
                        btn.ForeColor = textFg;
                        btn.FlatAppearance.BorderColor = borderColor;
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatAppearance.MouseOverBackColor = buttonHover;
                        break;

                    case TextBox txt:
                        txt.BackColor = Color.FromArgb(24, 24, 24);
                        txt.ForeColor = textFg;
                        txt.BorderStyle = BorderStyle.FixedSingle;
                        break;

                    case CheckBox chk:
                        chk.ForeColor = textFg;
                        chk.BackColor = Color.Transparent;
                        break;

                    case Label lbl:
                        if (lbl.ForeColor == Color.Black || lbl.ForeColor == SystemColors.ControlText)
                        {
                            lbl.ForeColor = textFg;
                        }
                        break;

                    case Panel pnl:
                        pnl.BackColor = darkBg;
                        ApplyDarkModeToSettingsForm(pnl);
                        break;
                }

                if (control.HasChildren && !(control is GroupBox) && !(control is Panel))
                {
                    ApplyDarkModeToSettingsForm(control);
                }
            }
        }

        private void AutoRenameSettingsForm_Load(object? sender, EventArgs e)
        {
            string defaultPattern = Settings.Default.RenameFormatPattern;
            txtCustomFormat.Text = string.IsNullOrWhiteSpace(defaultPattern)
                ? "{Show} - S{Season}E{Episode} - {Title}"
                : defaultPattern;

            // Default all formatting options to true on first run
            bool isFirstRun = string.IsNullOrEmpty(Settings.Default.RenameFormatPattern);

            itemIncludeShow.Checked = isFirstRun ? true : Settings.Default.RenameIncludeShow;
            itemPadNumbers.Checked = isFirstRun ? true : Settings.Default.RenamePadNumbers;
            itemReplaceWithSpaces.Checked = isFirstRun ? true : Settings.Default.RenameReplaceWithSpaces;
            itemReplaceDots.Checked = isFirstRun ? true : Settings.Default.RenameReplaceDots;
            itemRemoveSpecial.Checked = isFirstRun ? true : Settings.Default.RenameRemoveSpecial;

            UpdatePreview();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Settings.Default.RenameFormatPattern = txtCustomFormat.Text;
            Settings.Default.RenameIncludeShow = itemIncludeShow.Checked;
            Settings.Default.RenamePadNumbers = itemPadNumbers.Checked;
            Settings.Default.RenameReplaceWithSpaces = itemReplaceWithSpaces.Checked;
            Settings.Default.RenameReplaceDots = itemReplaceDots.Checked;
            Settings.Default.RenameRemoveSpecial = itemRemoveSpecial.Checked;

            Settings.Default.Save(); // Save settings persistently
        }
    }
}