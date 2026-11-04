using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

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

            btnAdd.Click += (s, e) => {
                string newUserName = PromptForInput("Add New User", "Enter new username:");
                if (!string.IsNullOrWhiteSpace(newUserName))
                {
                    newUserName = newUserName.Trim();
                    var usernames = AppConfig.GetAllUsernames();

                    if (usernames.Contains(newUserName, StringComparer.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("A user with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    usernames.Add(newUserName);
                    AppConfig.SaveUserDatabase(new UserDatabase { ActiveUser = AppConfig.CurrentUser, Users = GetUpdatedProfiles(usernames) });
                    LoadUserProfiles();
                    cmbUsers.SelectedItem = newUserName;
                }
            };

            btnRemove.Click += (s, e) => {
                if (cmbUsers.SelectedItem == null)
                {
                    MessageBox.Show("Please select a user profile to remove.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string userToRemove = cmbUsers.SelectedItem.ToString();
                var usernames = AppConfig.GetAllUsernames();

                if (usernames.Count <= 1)
                {
                    MessageBox.Show("You cannot remove the last remaining user profile.", "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Warning prompt before deletion
                var confirmResult = MessageBox.Show(
                    $"Are you sure you want to remove user '{userToRemove}'?\n\nThis will delete their profile and watch history association.",
                    "Confirm Profile Deletion",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.Yes)
                {
                    usernames.Remove(userToRemove);

                    // Rebuild and save database
                    var db = AppConfig.LoadUserDatabase();
                    db.Users.RemoveAll(u => u.Username.Equals(userToRemove, StringComparison.OrdinalIgnoreCase));
                    AppConfig.SaveUserDatabase(db);

                    if (AppConfig.CurrentUser.Equals(userToRemove, StringComparison.OrdinalIgnoreCase))
                    {
                        string fallbackUser = usernames.Count > 0 ? usernames[0] : "Default";
                        AppConfig.SwitchUser(fallbackUser);
                    }

                    LoadUserProfiles();
                }
            };
        }

        private List<UserProfile> GetUpdatedProfiles(List<string> usernames)
        {
            var list = new List<UserProfile>();
            foreach (var name in usernames)
            {
                list.Add(new UserProfile { Username = name, IsDefault = false });
            }
            return list;
        }

        private string PromptForInput(string title, string promptText)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 320;
                prompt.Height = 160;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = title;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Color darkBg = Color.FromArgb(32, 32, 32);
                Color textFg = Color.FromArgb(240, 240, 240);
                prompt.BackColor = darkBg;
                prompt.ForeColor = textFg;

                Label textLabel = new Label() { Left = 20, Top = 20, Text = promptText, AutoSize = true, ForeColor = textFg };
                TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 260, BackColor = Color.FromArgb(24, 24, 24), ForeColor = textFg };
                Button confirmation = new Button() { Text = "OK", Left = 120, Width = 75, Top = 90, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Cancel", Left = 205, Width = 75, Top = 90, DialogResult = DialogResult.Cancel };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(cancel);
                prompt.AcceptButton = confirmation;
                prompt.CancelButton = cancel;

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
            }
        }

        private void ApplyDarkModeToSettingsForm(Control parent)
        {
            Color darkBg = Color.FromArgb(32, 32, 32);
            Color textFg = Color.FromArgb(240, 240, 240);
            Color buttonBg = Color.FromArgb(60, 60, 65);
            Color buttonHover = Color.FromArgb(80, 80, 85);
            Color borderColor = Color.FromArgb(90, 90, 90);

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