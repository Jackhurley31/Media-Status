namespace Media_Status
{
    partial class ShutdownForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.GroupBox grpActionOptions;
        private System.Windows.Forms.Label lblAction;
        private System.Windows.Forms.ComboBox cmbAction;
        private System.Windows.Forms.Label lblTrigger;
        private System.Windows.Forms.ComboBox cmbTrigger;
        private System.Windows.Forms.CheckBox chkEnableTimer;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.TableLayoutPanel tableMainLayout;
        private System.Windows.Forms.FlowLayoutPanel flowButtonLayout;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            grpActionOptions = new GroupBox();
            tableMainLayout = new TableLayoutPanel();
            lblAction = new Label();
            cmbAction = new ComboBox();
            lblTrigger = new Label();
            cmbTrigger = new ComboBox();
            chkEnableTimer = new CheckBox();
            flowButtonLayout = new FlowLayoutPanel();
            btnClear = new Button();
            btnSave = new Button();
            btnCancel = new Button();

            grpActionOptions.SuspendLayout();
            tableMainLayout.SuspendLayout();
            flowButtonLayout.SuspendLayout();
            SuspendLayout();

            // 
            // grpActionOptions
            // 
            grpActionOptions.AutoSize = true;
            grpActionOptions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grpActionOptions.Controls.Add(tableMainLayout);
            grpActionOptions.Location = new Point(12, 12);
            grpActionOptions.Name = "grpActionOptions";
            grpActionOptions.Padding = new Padding(12);
            grpActionOptions.Size = new Size(320, 180);
            grpActionOptions.TabIndex = 0;
            grpActionOptions.TabStop = false;
            grpActionOptions.Text = "Post-Playback Actions";

            // 
            // tableMainLayout
            // 
            tableMainLayout.AutoSize = true;
            tableMainLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableMainLayout.ColumnCount = 2;
            tableMainLayout.ColumnStyles.Add(new ColumnStyle());
            tableMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableMainLayout.Controls.Add(lblAction, 0, 0);
            tableMainLayout.Controls.Add(cmbAction, 1, 0);
            tableMainLayout.Controls.Add(lblTrigger, 0, 1);
            tableMainLayout.Controls.Add(cmbTrigger, 1, 1);
            tableMainLayout.Controls.Add(chkEnableTimer, 0, 2);
            tableMainLayout.Controls.Add(flowButtonLayout, 0, 3);
            tableMainLayout.Dock = DockStyle.Fill;
            tableMainLayout.Location = new Point(12, 27);
            tableMainLayout.Name = "tableMainLayout";
            tableMainLayout.RowCount = 4;
            tableMainLayout.RowStyles.Add(new RowStyle());
            tableMainLayout.RowStyles.Add(new RowStyle());
            tableMainLayout.RowStyles.Add(new RowStyle());
            tableMainLayout.RowStyles.Add(new RowStyle());
            tableMainLayout.Size = new Size(296, 141);
            tableMainLayout.TabIndex = 0;

            // Fix: SetColumnSpan called directly on the TableLayoutPanel instance
            tableMainLayout.SetColumnSpan(chkEnableTimer, 2);
            tableMainLayout.SetColumnSpan(flowButtonLayout, 2);

            // 
            // lblAction
            // 
            lblAction.Anchor = AnchorStyles.Left;
            lblAction.AutoSize = true;
            lblAction.Margin = new Padding(3, 0, 6, 0);
            lblAction.Name = "lblAction";
            lblAction.Size = new Size(45, 15);
            lblAction.TabIndex = 0;
            lblAction.Text = "Action:";

            // 
            // cmbAction
            // 
            cmbAction.Dock = DockStyle.Fill;
            cmbAction.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAction.FormattingEnabled = true;
            cmbAction.Items.AddRange(new object[] { "Disabled / Do Nothing", "Shutdown PC", "Sleep", "Hibernate", "Exit Application" });
            cmbAction.Margin = new Padding(3, 3, 3, 6);
            cmbAction.Name = "cmbAction";
            cmbAction.Size = new Size(240, 23);
            cmbAction.TabIndex = 1;

            // 
            // lblTrigger
            // 
            lblTrigger.Anchor = AnchorStyles.Left;
            lblTrigger.AutoSize = true;
            lblTrigger.Margin = new Padding(3, 0, 6, 0);
            lblTrigger.Name = "lblTrigger";
            lblTrigger.Size = new Size(47, 15);
            lblTrigger.TabIndex = 2;
            lblTrigger.Text = "Trigger:";

            // 
            // cmbTrigger
            // 
            cmbTrigger.Dock = DockStyle.Fill;
            cmbTrigger.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTrigger.FormattingEnabled = true;
            cmbTrigger.Items.AddRange(new object[] { "After current video finishes", "After complete playback queue finishes" });
            cmbTrigger.Margin = new Padding(3, 3, 3, 8);
            cmbTrigger.Name = "cmbTrigger";
            cmbTrigger.Size = new Size(240, 23);
            cmbTrigger.TabIndex = 3;

            // 
            // chkEnableTimer
            // 
            chkEnableTimer.AutoSize = true;
            chkEnableTimer.Checked = true;
            chkEnableTimer.CheckState = CheckState.Checked;
            chkEnableTimer.Margin = new Padding(3, 4, 3, 12);
            chkEnableTimer.Name = "chkEnableTimer";
            chkEnableTimer.Size = new Size(229, 19);
            chkEnableTimer.TabIndex = 4;
            chkEnableTimer.Text = "Enable 30-second warning countdown";
            chkEnableTimer.UseVisualStyleBackColor = true;

            // 
            // flowButtonLayout
            // 
            flowButtonLayout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            flowButtonLayout.AutoSize = true;
            flowButtonLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowButtonLayout.Controls.Add(btnClear);
            flowButtonLayout.Controls.Add(btnSave);
            flowButtonLayout.Controls.Add(btnCancel);
            flowButtonLayout.Location = new Point(41, 107);
            flowButtonLayout.Margin = new Padding(0, 4, 0, 0);
            flowButtonLayout.Name = "flowButtonLayout";
            flowButtonLayout.Size = new Size(255, 31);
            flowButtonLayout.TabIndex = 5;

            // 
            // btnClear
            // 
            btnClear.AutoSize = true;
            btnClear.Margin = new Padding(3, 0, 3, 0);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(85, 27);
            btnClear.TabIndex = 0;
            btnClear.Text = "Clear Action";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;

            // 
            // btnSave
            // 
            btnSave.AutoSize = true;
            btnSave.Margin = new Padding(3, 0, 3, 0);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 27);
            btnSave.TabIndex = 1;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;

            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.Margin = new Padding(3, 0, 0, 0);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 27);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;

            // 
            // ShutdownForm
            // 
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(344, 204);
            Controls.Add(grpActionOptions);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ShutdownForm";
            Padding = new Padding(12);
            StartPosition = FormStartPosition.CenterParent;
            Text = "Power & Timeout Settings";

            grpActionOptions.ResumeLayout(false);
            grpActionOptions.PerformLayout();
            tableMainLayout.ResumeLayout(false);
            tableMainLayout.PerformLayout();
            flowButtonLayout.ResumeLayout(false);
            flowButtonLayout.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}