namespace Media_Status
{
    partial class RestoreBackupForm
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
            this.lblInfo = new System.Windows.Forms.Label();
            this.lstBackups = new System.Windows.Forms.ListBox();
            this.chkUseAsMaster = new System.Windows.Forms.CheckBox();
            this.btnRestore = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(15, 15);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(434, 15);
            this.lblInfo.TabIndex = 0;
            this.lblInfo.Text = "Select a backup file to restore your watched history, favorites, and settings:";
            // 
            // lstBackups
            // 
            this.lstBackups.FormattingEnabled = true;
            this.lstBackups.ItemHeight = 15;
            this.lstBackups.Location = new System.Drawing.Point(15, 40);
            this.lstBackups.Name = "lstBackups";
            this.lstBackups.Size = new System.Drawing.Size(475, 244);
            this.lstBackups.TabIndex = 1;
            // 
            // chkUseAsMaster
            // 
            this.chkUseAsMaster.AutoSize = true;
            this.chkUseAsMaster.Checked = true;
            this.chkUseAsMaster.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUseAsMaster.Location = new System.Drawing.Point(15, 295);
            this.chkUseAsMaster.Name = "chkUseAsMaster";
            this.chkUseAsMaster.Size = new System.Drawing.Size(306, 19);
            this.chkUseAsMaster.TabIndex = 2;
            this.chkUseAsMaster.Text = "Overwrite & set this backup as active Master database";
            this.chkUseAsMaster.UseVisualStyleBackColor = true;
            // 
            // btnRestore
            // 
            this.btnRestore.Location = new System.Drawing.Point(305, 335);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(90, 30);
            this.btnRestore.TabIndex = 3;
            this.btnRestore.Text = "Restore";
            this.btnRestore.UseVisualStyleBackColor = true;
            this.btnRestore.Click += new System.EventHandler(this.btnRestore_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(400, 335);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 30);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // RestoreBackupForm
            // 
            this.AcceptButton = this.btnRestore;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(510, 385);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnRestore);
            this.Controls.Add(this.chkUseAsMaster);
            this.Controls.Add(this.lstBackups);
            this.Controls.Add(this.lblInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RestoreBackupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Restore Database Backup";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.ListBox lstBackups;
        private System.Windows.Forms.CheckBox chkUseAsMaster;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Button btnCancel;
    }
}