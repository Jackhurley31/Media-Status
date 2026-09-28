namespace MediaStatus
{
    partial class AutoRenameSettingsForm
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
            grpPattern = new GroupBox();
            btnTagTitle = new Button();
            btnTagEpisode = new Button();
            btnTagSeason = new Button();
            btnTagShow = new Button();
            txtCustomFormat = new TextBox();
            lblPattern = new Label();
            grpOptions = new GroupBox();
            itemIncludeShow = new CheckBox();
            itemRemoveSpecial = new CheckBox();
            itemReplaceDots = new CheckBox();
            itemReplaceWithSpaces = new CheckBox();
            itemPadNumbers = new CheckBox();
            grpPreview = new GroupBox();
            lblPreviewOutput = new Label();
            lblPreviewInput = new Label();
            btnSave = new Button();
            btnCancel = new Button();
            grpPattern.SuspendLayout();
            grpOptions.SuspendLayout();
            grpPreview.SuspendLayout();
            SuspendLayout();
            // 
            // grpPattern
            // 
            grpPattern.Controls.Add(btnTagTitle);
            grpPattern.Controls.Add(btnTagEpisode);
            grpPattern.Controls.Add(btnTagSeason);
            grpPattern.Controls.Add(btnTagShow);
            grpPattern.Controls.Add(txtCustomFormat);
            grpPattern.Controls.Add(lblPattern);
            grpPattern.Location = new Point(14, 14);
            grpPattern.Margin = new Padding(4, 3, 4, 3);
            grpPattern.Name = "grpPattern";
            grpPattern.Padding = new Padding(4, 3, 4, 3);
            grpPattern.Size = new Size(537, 121);
            grpPattern.TabIndex = 0;
            grpPattern.TabStop = false;
            grpPattern.Text = "Pattern & Tags";
            // 
            // btnTagTitle
            // 
            btnTagTitle.Location = new Point(310, 78);
            btnTagTitle.Margin = new Padding(4, 3, 4, 3);
            btnTagTitle.Name = "btnTagTitle";
            btnTagTitle.Size = new Size(82, 27);
            btnTagTitle.TabIndex = 5;
            btnTagTitle.Text = "+ {Title}";
            btnTagTitle.UseVisualStyleBackColor = true;
            // 
            // btnTagEpisode
            // 
            btnTagEpisode.Location = new Point(210, 78);
            btnTagEpisode.Margin = new Padding(4, 3, 4, 3);
            btnTagEpisode.Name = "btnTagEpisode";
            btnTagEpisode.Size = new Size(93, 27);
            btnTagEpisode.TabIndex = 4;
            btnTagEpisode.Text = "+ {Episode}";
            btnTagEpisode.UseVisualStyleBackColor = true;
            // 
            // btnTagSeason
            // 
            btnTagSeason.Location = new Point(110, 78);
            btnTagSeason.Margin = new Padding(4, 3, 4, 3);
            btnTagSeason.Name = "btnTagSeason";
            btnTagSeason.Size = new Size(93, 27);
            btnTagSeason.TabIndex = 3;
            btnTagSeason.Text = "+ {Season}";
            btnTagSeason.UseVisualStyleBackColor = true;
            // 
            // btnTagShow
            // 
            btnTagShow.Location = new Point(21, 78);
            btnTagShow.Margin = new Padding(4, 3, 4, 3);
            btnTagShow.Name = "btnTagShow";
            btnTagShow.Size = new Size(82, 27);
            btnTagShow.TabIndex = 2;
            btnTagShow.Text = "+ {Show}";
            btnTagShow.UseVisualStyleBackColor = true;
            // 
            // txtCustomFormat
            // 
            txtCustomFormat.Location = new Point(21, 48);
            txtCustomFormat.Margin = new Padding(4, 3, 4, 3);
            txtCustomFormat.Name = "txtCustomFormat";
            txtCustomFormat.Size = new Size(494, 23);
            txtCustomFormat.TabIndex = 1;
            // 
            // lblPattern
            // 
            lblPattern.AutoSize = true;
            lblPattern.Location = new Point(18, 29);
            lblPattern.Margin = new Padding(4, 0, 4, 0);
            lblPattern.Name = "lblPattern";
            lblPattern.Size = new Size(99, 15);
            lblPattern.TabIndex = 0;
            lblPattern.Text = "Filename Pattern:";
            // 
            // grpOptions
            // 
            grpOptions.Controls.Add(itemIncludeShow);
            grpOptions.Controls.Add(itemRemoveSpecial);
            grpOptions.Controls.Add(itemReplaceDots);
            grpOptions.Controls.Add(itemReplaceWithSpaces);
            grpOptions.Controls.Add(itemPadNumbers);
            grpOptions.Location = new Point(14, 142);
            grpOptions.Margin = new Padding(4, 3, 4, 3);
            grpOptions.Name = "grpOptions";
            grpOptions.Padding = new Padding(4, 3, 4, 3);
            grpOptions.Size = new Size(537, 156);
            grpOptions.TabIndex = 1;
            grpOptions.TabStop = false;
            grpOptions.Text = "Formatting Options";
            // 
            // itemIncludeShow
            // 
            itemIncludeShow.AutoSize = true;
            itemIncludeShow.Checked = true;
            itemIncludeShow.CheckState = CheckState.Checked;
            itemIncludeShow.Location = new Point(21, 25);
            itemIncludeShow.Margin = new Padding(4, 3, 4, 3);
            itemIncludeShow.Name = "itemIncludeShow";
            itemIncludeShow.Size = new Size(191, 19);
            itemIncludeShow.TabIndex = 0;
            itemIncludeShow.Text = "Include show name in filename";
            itemIncludeShow.UseVisualStyleBackColor = true;
            // 
            // itemRemoveSpecial
            // 
            itemRemoveSpecial.AutoSize = true;
            itemRemoveSpecial.Checked = true;
            itemRemoveSpecial.CheckState = CheckState.Checked;
            itemRemoveSpecial.Location = new Point(21, 127);
            itemRemoveSpecial.Margin = new Padding(4, 3, 4, 3);
            itemRemoveSpecial.Name = "itemRemoveSpecial";
            itemRemoveSpecial.Size = new Size(285, 19);
            itemRemoveSpecial.TabIndex = 4;
            itemRemoveSpecial.Text = "Remove invalid file path characters automatically";
            itemRemoveSpecial.UseVisualStyleBackColor = true;
            // 
            // itemReplaceDots
            // 
            itemReplaceDots.AutoSize = true;
            itemReplaceDots.Checked = true;
            itemReplaceDots.CheckState = CheckState.Checked;
            itemReplaceDots.Location = new Point(21, 102);
            itemReplaceDots.Margin = new Padding(4, 3, 4, 3);
            itemReplaceDots.Name = "itemReplaceDots";
            itemReplaceDots.Size = new Size(171, 19);
            itemReplaceDots.TabIndex = 3;
            itemReplaceDots.Text = "Replace dots (.) with spaces";
            itemReplaceDots.UseVisualStyleBackColor = true;
            // 
            // itemReplaceWithSpaces
            // 
            itemReplaceWithSpaces.AutoSize = true;
            itemReplaceWithSpaces.Checked = true;
            itemReplaceWithSpaces.CheckState = CheckState.Checked;
            itemReplaceWithSpaces.Location = new Point(21, 76);
            itemReplaceWithSpaces.Margin = new Padding(4, 3, 4, 3);
            itemReplaceWithSpaces.Name = "itemReplaceWithSpaces";
            itemReplaceWithSpaces.Size = new Size(214, 19);
            itemReplaceWithSpaces.TabIndex = 2;
            itemReplaceWithSpaces.Text = "Replace underscores (_) with spaces";
            itemReplaceWithSpaces.UseVisualStyleBackColor = true;
            // 
            // itemPadNumbers
            // 
            itemPadNumbers.AutoSize = true;
            itemPadNumbers.Checked = true;
            itemPadNumbers.CheckState = CheckState.Checked;
            itemPadNumbers.Location = new Point(21, 51);
            itemPadNumbers.Margin = new Padding(4, 3, 4, 3);
            itemPadNumbers.Name = "itemPadNumbers";
            itemPadNumbers.Size = new Size(266, 19);
            itemPadNumbers.TabIndex = 1;
            itemPadNumbers.Text = "Pad numbers with leading zeros (e.g., S01E05)";
            itemPadNumbers.UseVisualStyleBackColor = true;
            // 
            // grpPreview
            // 
            grpPreview.Controls.Add(lblPreviewOutput);
            grpPreview.Controls.Add(lblPreviewInput);
            grpPreview.Location = new Point(14, 305);
            grpPreview.Margin = new Padding(4, 3, 4, 3);
            grpPreview.Name = "grpPreview";
            grpPreview.Padding = new Padding(4, 3, 4, 3);
            grpPreview.Size = new Size(537, 87);
            grpPreview.TabIndex = 2;
            grpPreview.TabStop = false;
            grpPreview.Text = "Live Preview";
            // 
            // lblPreviewOutput
            // 
            lblPreviewOutput.AutoSize = true;
            lblPreviewOutput.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPreviewOutput.Location = new Point(18, 52);
            lblPreviewOutput.Margin = new Padding(4, 0, 4, 0);
            lblPreviewOutput.Name = "lblPreviewOutput";
            lblPreviewOutput.Size = new Size(379, 13);
            lblPreviewOutput.TabIndex = 1;
            lblPreviewOutput.Text = "Result:   Breaking Bad - S01E03 - And the Bag's in the River.mkv";
            // 
            // lblPreviewInput
            // 
            lblPreviewInput.AutoSize = true;
            lblPreviewInput.Location = new Point(18, 25);
            lblPreviewInput.Margin = new Padding(4, 0, 4, 0);
            lblPreviewInput.Name = "lblPreviewInput";
            lblPreviewInput.Size = new Size(191, 15);
            lblPreviewInput.TabIndex = 0;
            lblPreviewInput.Text = "Original: Breaking_Bad.S01E03.mkv";
            // 
            // btnSave
            // 
            btnSave.DialogResult = DialogResult.OK;
            btnSave.Location = new Point(369, 404);
            btnSave.Margin = new Padding(4, 3, 4, 3);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(88, 29);
            btnSave.TabIndex = 3;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(463, 404);
            btnCancel.Margin = new Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(88, 29);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // AutoRenameSettingsForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(565, 445);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(grpPreview);
            Controls.Add(grpOptions);
            Controls.Add(grpPattern);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AutoRenameSettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Auto-Rename Settings";
            Load += AutoRenameSettingsForm_Load;
            grpPattern.ResumeLayout(false);
            grpPattern.PerformLayout();
            grpOptions.ResumeLayout(false);
            grpOptions.PerformLayout();
            grpPreview.ResumeLayout(false);
            grpPreview.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpPattern;
        private System.Windows.Forms.Label lblPattern;
        private System.Windows.Forms.TextBox txtCustomFormat;
        private System.Windows.Forms.Button btnTagShow;
        private System.Windows.Forms.Button btnTagSeason;
        private System.Windows.Forms.Button btnTagEpisode;
        private System.Windows.Forms.Button btnTagTitle;
        private System.Windows.Forms.GroupBox grpOptions;
        public System.Windows.Forms.CheckBox itemIncludeShow;
        public System.Windows.Forms.CheckBox itemPadNumbers;
        public System.Windows.Forms.CheckBox itemReplaceWithSpaces;
        public System.Windows.Forms.CheckBox itemReplaceDots;
        public System.Windows.Forms.CheckBox itemRemoveSpecial;
        private System.Windows.Forms.GroupBox grpPreview;
        private System.Windows.Forms.Label lblPreviewInput;
        private System.Windows.Forms.Label lblPreviewOutput;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}