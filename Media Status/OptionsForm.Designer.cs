namespace MediaControlApp
{
    partial class OptionsForm
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
            lstVideoExtensions = new ListBox();
            txtNewExtension = new TextBox();
            btnAddExtension = new Button();
            btnRemoveExtension = new Button();
            lblExtensionTitle = new Label();
            BtnDarkMode = new Button();
            btnVLCpath = new Button();
            openFileDialog1 = new OpenFileDialog();
            txtPlayerPath = new TextBox();
            label1 = new Label();
            groupBox1 = new GroupBox();
            tableGeneralLayout = new TableLayoutPanel();
            chkRunOnStartup = new CheckBox();
            label2 = new Label();
            cmbFontSize = new ComboBox();
            label3 = new Label();
            cmbFontFamily = new ComboBox();
            groupBox2 = new GroupBox();
            tableVideoLayout = new TableLayoutPanel();
            tableVlcPath = new TableLayoutPanel();
            flowAddExtension = new FlowLayoutPanel();
            tableFormLayout = new TableLayoutPanel();
            groupBox1.SuspendLayout();
            tableGeneralLayout.SuspendLayout();
            groupBox2.SuspendLayout();
            tableVideoLayout.SuspendLayout();
            tableVlcPath.SuspendLayout();
            flowAddExtension.SuspendLayout();
            tableFormLayout.SuspendLayout();
            SuspendLayout();
            // 
            // lstVideoExtensions
            // 
            lstVideoExtensions.Dock = DockStyle.Fill;
            lstVideoExtensions.Font = new Font("Segoe UI", 9F);
            lstVideoExtensions.FormattingEnabled = true;
            lstVideoExtensions.IntegralHeight = false;
            lstVideoExtensions.Location = new Point(3, 84);
            lstVideoExtensions.Margin = new Padding(3, 3, 3, 8);
            lstVideoExtensions.MinimumSize = new Size(0, 150);
            lstVideoExtensions.Name = "lstVideoExtensions";
            lstVideoExtensions.Size = new Size(469, 325);
            lstVideoExtensions.TabIndex = 3;
            // 
            // txtNewExtension
            // 
            txtNewExtension.Font = new Font("Segoe UI", 9F);
            txtNewExtension.Location = new Point(3, 3);
            txtNewExtension.Name = "txtNewExtension";
            txtNewExtension.Size = new Size(120, 23);
            txtNewExtension.TabIndex = 0;
            // 
            // btnAddExtension
            // 
            btnAddExtension.AutoSize = true;
            btnAddExtension.Location = new Point(129, 2);
            btnAddExtension.Margin = new Padding(3, 2, 3, 2);
            btnAddExtension.Name = "btnAddExtension";
            btnAddExtension.Size = new Size(75, 27);
            btnAddExtension.TabIndex = 1;
            btnAddExtension.Text = "Add";
            btnAddExtension.UseVisualStyleBackColor = true;
            btnAddExtension.Click += btnAddExtension_Click;
            // 
            // btnRemoveExtension
            // 
            btnRemoveExtension.AutoSize = true;
            btnRemoveExtension.Location = new Point(210, 2);
            btnRemoveExtension.Margin = new Padding(3, 2, 3, 2);
            btnRemoveExtension.Name = "btnRemoveExtension";
            btnRemoveExtension.Size = new Size(130, 27);
            btnRemoveExtension.TabIndex = 2;
            btnRemoveExtension.Text = "Remove Selected";
            btnRemoveExtension.UseVisualStyleBackColor = true;
            btnRemoveExtension.Click += btnRemoveExtension_Click;
            // 
            // lblExtensionTitle
            // 
            lblExtensionTitle.AutoSize = true;
            lblExtensionTitle.Font = new Font("Segoe UI", 9F);
            lblExtensionTitle.Location = new Point(3, 62);
            lblExtensionTitle.Margin = new Padding(3, 6, 3, 4);
            lblExtensionTitle.Name = "lblExtensionTitle";
            lblExtensionTitle.Size = new Size(144, 15);
            lblExtensionTitle.TabIndex = 2;
            lblExtensionTitle.Text = "Allowed Video Extensions:";
            // 
            // BtnDarkMode
            // 
            BtnDarkMode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            BtnDarkMode.AutoSize = true;
            BtnDarkMode.Location = new Point(3, 3);
            BtnDarkMode.Margin = new Padding(3, 3, 3, 6);
            BtnDarkMode.Name = "BtnDarkMode";
            BtnDarkMode.Size = new Size(212, 30);
            BtnDarkMode.TabIndex = 0;
            BtnDarkMode.Text = "🌙 Dark Mode";
            BtnDarkMode.UseVisualStyleBackColor = true;
            BtnDarkMode.Click += BtnDarkMode_Click;
            // 
            // btnVLCpath
            // 
            btnVLCpath.AutoSize = true;
            btnVLCpath.Location = new Point(393, 2);
            btnVLCpath.Margin = new Padding(3, 2, 3, 2);
            btnVLCpath.Name = "btnVLCpath";
            btnVLCpath.Size = new Size(79, 27);
            btnVLCpath.TabIndex = 1;
            btnVLCpath.Text = "Browse";
            btnVLCpath.UseVisualStyleBackColor = true;
            btnVLCpath.Click += btnVLCpath_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // txtPlayerPath
            // 
            txtPlayerPath.Dock = DockStyle.Fill;
            txtPlayerPath.Font = new Font("Segoe UI", 9F);
            txtPlayerPath.Location = new Point(3, 3);
            txtPlayerPath.Name = "txtPlayerPath";
            txtPlayerPath.Size = new Size(384, 23);
            txtPlayerPath.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F);
            label1.Location = new Point(3, 0);
            label1.Margin = new Padding(3, 0, 3, 2);
            label1.Name = "label1";
            label1.Size = new Size(117, 15);
            label1.TabIndex = 0;
            label1.Text = "VLC Executable Path:";
            // 
            // groupBox1
            // 
            groupBox1.AutoSize = true;
            groupBox1.Controls.Add(tableGeneralLayout);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            groupBox1.Location = new Point(3, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(8);
            groupBox1.Size = new Size(234, 480);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "General Settings";
            // 
            // tableGeneralLayout
            // 
            tableGeneralLayout.AutoSize = true;
            tableGeneralLayout.ColumnCount = 1;
            tableGeneralLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableGeneralLayout.Controls.Add(BtnDarkMode, 0, 0);
            tableGeneralLayout.Controls.Add(chkRunOnStartup, 0, 1);
            tableGeneralLayout.Controls.Add(label2, 0, 2);
            tableGeneralLayout.Controls.Add(cmbFontSize, 0, 3);
            tableGeneralLayout.Controls.Add(label3, 0, 4);
            tableGeneralLayout.Controls.Add(cmbFontFamily, 0, 5);
            tableGeneralLayout.Dock = DockStyle.Fill;
            tableGeneralLayout.Location = new Point(8, 24);
            tableGeneralLayout.Name = "tableGeneralLayout";
            tableGeneralLayout.RowCount = 7;
            tableGeneralLayout.RowStyles.Add(new RowStyle());
            tableGeneralLayout.RowStyles.Add(new RowStyle());
            tableGeneralLayout.RowStyles.Add(new RowStyle());
            tableGeneralLayout.RowStyles.Add(new RowStyle());
            tableGeneralLayout.RowStyles.Add(new RowStyle());
            tableGeneralLayout.RowStyles.Add(new RowStyle());
            tableGeneralLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableGeneralLayout.Size = new Size(218, 448);
            tableGeneralLayout.TabIndex = 0;
            // 
            // chkRunOnStartup
            // 
            chkRunOnStartup.AutoSize = true;
            chkRunOnStartup.Font = new Font("Segoe UI", 9F);
            chkRunOnStartup.Location = new Point(3, 45);
            chkRunOnStartup.Margin = new Padding(3, 6, 3, 10);
            chkRunOnStartup.Name = "chkRunOnStartup";
            chkRunOnStartup.Size = new Size(128, 19);
            chkRunOnStartup.TabIndex = 1;
            chkRunOnStartup.Text = "Start with Windows";
            chkRunOnStartup.UseVisualStyleBackColor = true;
            chkRunOnStartup.CheckedChanged += chkRunOnStartup_CheckedChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9F);
            label2.Location = new Point(3, 80);
            label2.Margin = new Padding(3, 6, 3, 2);
            label2.Name = "label2";
            label2.Size = new Size(57, 15);
            label2.TabIndex = 2;
            label2.Text = "Font Size:";
            // 
            // cmbFontSize
            // 
            cmbFontSize.Dock = DockStyle.Fill;
            cmbFontSize.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFontSize.Font = new Font("Segoe UI", 9F);
            cmbFontSize.FormattingEnabled = true;
            cmbFontSize.Location = new Point(3, 97);
            cmbFontSize.Margin = new Padding(3, 0, 3, 10);
            cmbFontSize.Name = "cmbFontSize";
            cmbFontSize.Size = new Size(212, 23);
            cmbFontSize.TabIndex = 3;
            cmbFontSize.SelectedIndexChanged += cmbFontSize_SelectedIndexChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9F);
            label3.Location = new Point(3, 136);
            label3.Margin = new Padding(3, 6, 3, 2);
            label3.Name = "label3";
            label3.Size = new Size(62, 15);
            label3.TabIndex = 4;
            label3.Text = "Font Style:";
            // 
            // cmbFontFamily
            // 
            cmbFontFamily.Dock = DockStyle.Fill;
            cmbFontFamily.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFontFamily.Font = new Font("Segoe UI", 9F);
            cmbFontFamily.FormattingEnabled = true;
            cmbFontFamily.Location = new Point(3, 153);
            cmbFontFamily.Margin = new Padding(3, 0, 3, 10);
            cmbFontFamily.Name = "cmbFontFamily";
            cmbFontFamily.Size = new Size(212, 23);
            cmbFontFamily.TabIndex = 5;
            cmbFontFamily.SelectedIndexChanged += cmbFontFamily_SelectedIndexChanged;
            // 
            // groupBox2
            // 
            groupBox2.AutoSize = true;
            groupBox2.Controls.Add(tableVideoLayout);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            groupBox2.Location = new Point(243, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(8);
            groupBox2.Size = new Size(491, 480);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Video Format Settings";
            // 
            // tableVideoLayout
            // 
            tableVideoLayout.AutoSize = true;
            tableVideoLayout.ColumnCount = 1;
            tableVideoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableVideoLayout.Controls.Add(label1, 0, 0);
            tableVideoLayout.Controls.Add(tableVlcPath, 0, 1);
            tableVideoLayout.Controls.Add(lblExtensionTitle, 0, 2);
            tableVideoLayout.Controls.Add(lstVideoExtensions, 0, 3);
            tableVideoLayout.Controls.Add(flowAddExtension, 0, 4);
            tableVideoLayout.Dock = DockStyle.Fill;
            tableVideoLayout.Location = new Point(8, 24);
            tableVideoLayout.Name = "tableVideoLayout";
            tableVideoLayout.RowCount = 5;
            tableVideoLayout.RowStyles.Add(new RowStyle());
            tableVideoLayout.RowStyles.Add(new RowStyle());
            tableVideoLayout.RowStyles.Add(new RowStyle());
            tableVideoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableVideoLayout.RowStyles.Add(new RowStyle());
            tableVideoLayout.Size = new Size(475, 448);
            tableVideoLayout.TabIndex = 0;
            // 
            // tableVlcPath
            // 
            tableVlcPath.AutoSize = true;
            tableVlcPath.ColumnCount = 2;
            tableVlcPath.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableVlcPath.ColumnStyles.Add(new ColumnStyle());
            tableVlcPath.Controls.Add(txtPlayerPath, 0, 0);
            tableVlcPath.Controls.Add(btnVLCpath, 1, 0);
            tableVlcPath.Dock = DockStyle.Fill;
            tableVlcPath.Location = new Point(0, 17);
            tableVlcPath.Margin = new Padding(0, 0, 0, 8);
            tableVlcPath.Name = "tableVlcPath";
            tableVlcPath.RowCount = 1;
            tableVlcPath.RowStyles.Add(new RowStyle());
            tableVlcPath.Size = new Size(475, 31);
            tableVlcPath.TabIndex = 1;
            // 
            // flowAddExtension
            // 
            flowAddExtension.AutoSize = true;
            flowAddExtension.Controls.Add(txtNewExtension);
            flowAddExtension.Controls.Add(btnAddExtension);
            flowAddExtension.Controls.Add(btnRemoveExtension);
            flowAddExtension.Dock = DockStyle.Fill;
            flowAddExtension.Location = new Point(0, 417);
            flowAddExtension.Margin = new Padding(0);
            flowAddExtension.Name = "flowAddExtension";
            flowAddExtension.Size = new Size(475, 31);
            flowAddExtension.TabIndex = 4;
            flowAddExtension.WrapContents = false;
            // 
            // tableFormLayout
            // 
            tableFormLayout.AutoSize = true;
            tableFormLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableFormLayout.ColumnCount = 2;
            tableFormLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
            tableFormLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableFormLayout.Controls.Add(groupBox1, 0, 0);
            tableFormLayout.Controls.Add(groupBox2, 1, 0);
            tableFormLayout.Dock = DockStyle.Fill;
            tableFormLayout.Location = new Point(12, 12);
            tableFormLayout.Name = "tableFormLayout";
            tableFormLayout.RowCount = 1;
            tableFormLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableFormLayout.Size = new Size(737, 486);
            tableFormLayout.TabIndex = 0;
            // 
            // OptionsForm
            // 
            AcceptButton = btnAddExtension;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(761, 510);
            Controls.Add(tableFormLayout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(720, 480);
            Name = "OptionsForm";
            Padding = new Padding(12);
            StartPosition = FormStartPosition.CenterParent;
            Text = "Options";
            FormClosed += OptionsForm_FormClosed;
            Load += OptionsForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            tableGeneralLayout.ResumeLayout(false);
            tableGeneralLayout.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            tableVideoLayout.ResumeLayout(false);
            tableVideoLayout.PerformLayout();
            tableVlcPath.ResumeLayout(false);
            tableVlcPath.PerformLayout();
            flowAddExtension.ResumeLayout(false);
            flowAddExtension.PerformLayout();
            tableFormLayout.ResumeLayout(false);
            tableFormLayout.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListBox lstVideoExtensions;
        private System.Windows.Forms.TextBox txtNewExtension;
        private System.Windows.Forms.Button btnAddExtension;
        private System.Windows.Forms.Button btnRemoveExtension;
        private System.Windows.Forms.Label lblExtensionTitle;
        private Button BtnDarkMode;
        private Button btnVLCpath;
        private OpenFileDialog openFileDialog1;
        private TextBox txtPlayerPath;
        private Label label1;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private CheckBox chkRunOnStartup;
        private ComboBox cmbFontFamily;
        private ComboBox cmbFontSize;
        private Label label3;
        private Label label2;
        private TableLayoutPanel tableGeneralLayout;
        private TableLayoutPanel tableVideoLayout;
        private TableLayoutPanel tableVlcPath;
        private FlowLayoutPanel flowAddExtension;
        private TableLayoutPanel tableFormLayout;
    }
}