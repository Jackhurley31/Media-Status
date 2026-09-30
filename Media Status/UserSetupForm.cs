using System;
using System.Drawing;
using System.Windows.Forms;

namespace Media_Status
{
    public partial class UserSetupForm : Form
    {
        public string SelectedUsername { get; private set; } = string.Empty;

        public UserSetupForm(bool darkMode = true)
        {
            InitializeComponent();

            if (darkMode)
            {
                ApplyDarkModeToSettingsForm(this);
            }

            // Display the current Windows username clearly to the user
            string winUser = Environment.UserName;
            lblWelcomeInfo.Text = $"No profile found for your Windows account.\n\n" +
                                  $"Would you like to register and continue using:\n" +
                                  $"👉 {winUser}";

            SelectedUsername = winUser;
            AttachEvents();
        }

        private void AttachEvents()
        {
            btnSave.Click += (s, e) => {
                this.DialogResult = DialogResult.OK;
                this.Close();
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