namespace MediaControlApp.Forms
{
    partial class AutoRenamePreviewForm
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

        private void InitializeComponent()
        {
            this.lblHeader = new System.Windows.Forms.Label();
            this.listViewPreview = new System.Windows.Forms.ListView();
            this.columnHeaderOriginal = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderNew = new System.Windows.Forms.ColumnHeader();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblHeader
            // 
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(804, 35);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "Review the proposed auto-rename changes below. Click \'Apply Changes\' to confirm:";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // listViewPreview
            // 
            this.listViewPreview.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderOriginal,
            this.columnHeaderNew});
            this.listViewPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewPreview.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.listViewPreview.FullRowSelect = true;
            this.listViewPreview.GridLines = true;
            this.listViewPreview.Location = new System.Drawing.Point(0, 35);
            this.listViewPreview.Name = "listViewPreview";
            this.listViewPreview.Size = new System.Drawing.Size(804, 346);
            this.listViewPreview.TabIndex = 1;
            this.listViewPreview.UseCompatibleStateImageBehavior = false;
            this.listViewPreview.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderOriginal
            // 
            this.columnHeaderOriginal.Text = "Original Filename";
            this.columnHeaderOriginal.Width = 390;
            // 
            // columnHeaderNew
            // 
            this.columnHeaderNew.Text = "Proposed New Filename";
            this.columnHeaderNew.Width = 390;
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.btnApply);
            this.panelBottom.Controls.Add(this.btnCancel);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 381);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Padding = new System.Windows.Forms.Padding(12);
            this.panelBottom.Size = new System.Drawing.Size(804, 55);
            this.panelBottom.TabIndex = 2;
            // 
            // btnApply
            // 
            this.btnApply.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnApply.Location = new System.Drawing.Point(620, 12);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(172, 31);
            this.btnApply.TabIndex = 0;
            this.btnApply.Text = "Apply Changes (Yes)";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnCancel.Location = new System.Drawing.Point(12, 12);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(110, 31);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel (No)";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // AutoRenamePreviewForm
            // 
            this.AcceptButton = this.btnApply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(804, 436);
            this.Controls.Add(this.listViewPreview);
            this.Controls.Add(this.lblHeader);
            this.Controls.Add(this.panelBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Auto-Rename Preview";
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.ListView listViewPreview;
        private System.Windows.Forms.ColumnHeader columnHeaderOriginal;
        private System.Windows.Forms.ColumnHeader columnHeaderNew;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
    }
}