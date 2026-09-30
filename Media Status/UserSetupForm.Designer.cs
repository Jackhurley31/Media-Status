namespace Media_Status
{
    partial class UserSetupForm
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
            grpUserSelection = new GroupBox();
            lblWelcomeInfo = new Label();
            btnSave = new Button();
            grpUserSelection.SuspendLayout();
            SuspendLayout();
            // 
            // grpUserSelection
            // 
            grpUserSelection.Controls.Add(lblWelcomeInfo);
            grpUserSelection.Location = new Point(14, 14);
            grpUserSelection.Margin = new Padding(4, 3, 4, 3);
            grpUserSelection.Name = "grpUserSelection";
            grpUserSelection.Padding = new Padding(4, 3, 4, 3);
            grpUserSelection.Size = new Size(390, 115);
            grpUserSelection.TabIndex = 0;
            grpUserSelection.TabStop = false;
            grpUserSelection.Text = "Automatic Profile Registration";
            // 
            // lblWelcomeInfo
            // 
            lblWelcomeInfo.Dock = DockStyle.Fill;
            lblWelcomeInfo.Location = new Point(4, 19);
            lblWelcomeInfo.Margin = new Padding(4, 0, 4, 0);
            lblWelcomeInfo.Name = "lblWelcomeInfo";
            lblWelcomeInfo.Size = new Size(382, 93);
            lblWelcomeInfo.TabIndex = 0;
            lblWelcomeInfo.Text = "Welcome Media Control App create user press continue ";
            lblWelcomeInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnSave
            // 
            btnSave.DialogResult = DialogResult.OK;
            btnSave.Location = new Point(150, 143);
            btnSave.Margin = new Padding(4, 3, 4, 3);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(110, 29);
            btnSave.TabIndex = 1;
            btnSave.Text = "Continue";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // UserSetupForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(418, 184);
            ControlBox = false;
            Controls.Add(btnSave);
            Controls.Add(grpUserSelection);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UserSetupForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Initial User Setup";
            grpUserSelection.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpUserSelection;
        private System.Windows.Forms.Label lblWelcomeInfo;
        private System.Windows.Forms.Button btnSave;
    }
}