using System;
using System.Drawing;
using System.Windows.Forms;

namespace Media_Status
{
    public partial class SwitchUserForm : Form
    {
        public string SelectedUser { get; private set; } = string.Empty;

        public SwitchUserForm(bool darkMode = true)
        {
            InitializeComponent();

            if (darkMode)
            {
                ApplyDarkModeToSettingsForm(this);
            }

            LoadUserProfiles();
            AttachEvents();
        }

        private void LoadUserProfiles()
        {
            cmbUsers.Items.Clear();
            foreach (var name in AppConfig.GetAllUsernames())
            {
                cmbUsers.Items.Add(name);
            }

            // Select the current active user by default
            if (cmbUsers.Items.Contains(AppConfig.CurrentUser))
            {
                cmbUsers.SelectedItem = AppConfig.CurrentUser;
            }
            else if (cmbUsers.Items.Count > 0)
            {
                cmbUsers.SelectedIndex = 0;
            }
        }

        private void AttachEvents()
        {
            btnSwitch.Click += (s, e) => {
                if (cmbUsers.SelectedItem != null)
                {
                    SelectedUser = cmbUsers.SelectedItem.ToString();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Please select a user profile.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }

        private void ApplyDarkModeToSettingsForm(Control parent)
        {
            Color darkBg = Color.FromArgb(32, 32, 32);      // Main background[cite: 7]
            Color textFg = Color.FromArgb(240, 240, 240);   // Primary text[cite: 7]
            Color buttonBg = Color.FromArgb(60, 60, 65);    // Button background[cite: 7]
            Color buttonHover = Color.FromArgb(80, 80, 85); // Button hover background[cite: 7]
            Color borderColor = Color.FromArgb(90, 90, 90); // Subtle borders[cite: 7]

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

                    case ComboBox cmb:
                        cmb.BackColor = Color.FromArgb(24, 24, 24);
                        cmb.ForeColor = textFg;
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
    }
}