using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MediaControlApp.Forms
{
    public partial class AutoRenamePreviewForm : Form
    {
        private bool _darkMode;
        public List<AutoRenamePreviewItem> ApprovedItems { get; private set; } = new List<AutoRenamePreviewItem>();

        public AutoRenamePreviewForm(List<AutoRenamePreviewItem> previewItems, bool darkMode)
        {
            _darkMode = darkMode;
            InitializeComponent();
            ApplyThemeAndData(previewItems);
        }

        private void ApplyThemeAndData(List<AutoRenamePreviewItem> items)
        {
            Color backColor = _darkMode ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            Color foreColor = _darkMode ? Color.White : Color.Black;
            Color controlBack = _darkMode ? Color.FromArgb(45, 45, 45) : Color.White;

            this.BackColor = backColor;
            this.ForeColor = foreColor;

            if (lblHeader != null)
            {
                lblHeader.ForeColor = foreColor;
            }

            if (listViewPreview != null)
            {
                listViewPreview.BackColor = controlBack;
                listViewPreview.ForeColor = foreColor;

                listViewPreview.Items.Clear();
                foreach (var item in items)
                {
                    var lvi = new ListViewItem(item.OriginalName);
                    lvi.SubItems.Add(item.NewName);
                    lvi.Tag = item;
                    listViewPreview.Items.Add(lvi);
                }
            }

            if (btnApply != null)
            {
                btnApply.BackColor = Color.FromArgb(40, 167, 69);
                btnApply.ForeColor = Color.White;
                btnApply.FlatStyle = FlatStyle.Flat;
            }

            if (btnCancel != null)
            {
                btnCancel.BackColor = _darkMode ? Color.FromArgb(60, 64, 67) : SystemColors.Control;
                btnCancel.ForeColor = foreColor;
                btnCancel.FlatStyle = FlatStyle.Flat;
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            ApprovedItems.Clear();
            if (listViewPreview != null)
            {
                foreach (ListViewItem lvi in listViewPreview.Items)
                {
                    if (lvi.Tag is AutoRenamePreviewItem api)
                    {
                        ApprovedItems.Add(api);
                    }
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}