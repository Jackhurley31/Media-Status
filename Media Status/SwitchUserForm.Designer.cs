namespace Media_Status
{
    partial class SwitchUserForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            grpUser = new System.Windows.Forms.GroupBox();
            cmbUsers = new System.Windows.Forms.ComboBox();
            lblSelect = new System.Windows.Forms.Label();
            btnSwitch = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            grpUser.SuspendLayout();
            SuspendLayout();
            // 
            // grpUser
            // 
            grpUser.Controls.Add(cmbUsers);
            grpUser.Controls.Add(lblSelect);
            grpUser.Location = new System.Drawing.Point(14, 14);
            grpUser.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            grpUser.Name = "grpUser";
            grpUser.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            grpUser.Size = new System.Drawing.Size(330, 95);
            grpUser.TabIndex = 0;
            grpUser.TabStop = false;
            grpUser.Text = "Active Profile";
            // 
            // cmbUsers
            // 
            cmbUsers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbUsers.FormattingEnabled = true;
            cmbUsers.Location = new System.Drawing.Point(21, 50);
            cmbUsers.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cmbUsers.Name = "cmbUsers";
            cmbUsers.Size = new System.Drawing.Size(288, 23);
            cmbUsers.TabIndex = 1;
            // 
            // lblSelect
            // 
            lblSelect.AutoSize = true;
            lblSelect.Location = new System.Drawing.Point(18, 27);
            lblSelect.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblSelect.Name = "lblSelect";
            lblSelect.Size = new Size(113, 15);
            lblSelect.TabIndex = 0;
            lblSelect.Text = "Select User Account:";
            // 
            // btnSwitch
            // 
            btnSwitch.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnSwitch.Location = new System.Drawing.Point(161, 125);
            btnSwitch.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnSwitch.Name = "btnSwitch";
            btnSwitch.Size = new System.Drawing.Size(88, 29);
            btnSwitch.TabIndex = 1;
            btnSwitch.Text = "Switch";
            btnSwitch.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(256, 125);
            btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(88, 29);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // SwitchUserForm
            // 
            AcceptButton = btnSwitch;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(358, 166);
            Controls.Add(btnCancel);
            Controls.Add(btnSwitch);
            Controls.Add(grpUser);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SwitchUserForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Switch User Profile";
            grpUser.ResumeLayout(false);
            grpUser.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpUser;
        private System.Windows.Forms.Label lblSelect;
        private System.Windows.Forms.ComboBox cmbUsers;
        private System.Windows.Forms.Button btnSwitch;
        private System.Windows.Forms.Button btnCancel;
    }
}