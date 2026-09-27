using System.Drawing;
using System.Windows.Forms;

namespace MediaControlApp.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private SplitContainer splitMain;
        private TreeView treeShows;
        private ListView listEpisodes;

        private ColumnHeader colName;
        private ColumnHeader colSeason;
        private ColumnHeader colEpisode;
        private ColumnHeader colDuration;
        private ColumnHeader colWatched;
        private ColumnHeader colDateModified;

        private TableLayoutPanel layoutBottomPanel;
        private FlowLayoutPanel flowLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel2;
        private FlowLayoutPanel flowLayoutPanel3;
        private GroupBox gbActions;
        private FlowLayoutPanel flowActionsInner;

        private Button btnRefresh;
        private Button OpenFileLocation;
        private Label label1;
        private TextBox txtSearch;
        private Button btnCollapseAll;
        private Button BtnFavourite;
        private Button btnMarkWatched;
        private Button btnArchive;
        private Button btnDelete;
        private Button btnPlay;
        private Label lblStatus;
        private Button btnStop;
        private Button btnOrganiz;
        private Button btnResume;
        private Button btnoptions;
        private Button btnShutdownOptions;
        private CheckBox chkSubtitles;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose(); base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            splitMain = new SplitContainer();
            splitContainer1 = new SplitContainer();
            picPoster = new PictureBox();
            treeShows = new TreeView();
            listEpisodes = new ListView();
            colName = new ColumnHeader();
            colSeason = new ColumnHeader();
            colEpisode = new ColumnHeader();
            colDuration = new ColumnHeader();
            colWatched = new ColumnHeader();
            colDateModified = new ColumnHeader();
            layoutBottomPanel = new TableLayoutPanel();
            flowLayoutPanel2 = new FlowLayoutPanel();
            btnCollapseAll = new Button();
            label1 = new Label();
            txtSearch = new TextBox();
            OpenFileLocation = new Button();
            BtnCheckIMDb = new Button();
            flowLayoutPanel3 = new FlowLayoutPanel();
            btnDelete = new Button();
            btnArchive = new Button();
            btnMarkWatched = new Button();
            btnResume = new Button();
            btnPlay = new Button();
            btnStop = new Button();
            flowLayoutPanel1 = new FlowLayoutPanel();
            btnoptions = new Button();
            BtnFavourite = new Button();
            btnRefresh = new Button();
            btnOrganiz = new Button();
            lblStatus = new Label();
            BtnAutoRename = new Button();
            gbActions = new GroupBox();
            flowActionsInner = new FlowLayoutPanel();
            btnShutdownOptions = new Button();
            chkSubtitles = new CheckBox();
            cmbScreens = new ComboBox();
            chkFullscreen = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPoster).BeginInit();
            layoutBottomPanel.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            flowLayoutPanel3.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            gbActions.SuspendLayout();
            flowActionsInner.SuspendLayout();
            SuspendLayout();
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(0, 0);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(splitContainer1);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(listEpisodes);
            splitMain.Size = new Size(1231, 566);
            splitMain.SplitterDistance = 354;
            splitMain.TabIndex = 0;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(picPoster);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(treeShows);
            splitContainer1.Size = new Size(354, 566);
            splitContainer1.SplitterDistance = 211;
            splitContainer1.TabIndex = 0;
            // 
            // picPoster
            // 
            picPoster.BackColor = SystemColors.Control;
            picPoster.Dock = DockStyle.Fill;
            picPoster.Location = new Point(0, 0);
            picPoster.Margin = new Padding(0);
            picPoster.Name = "picPoster";
            picPoster.Size = new Size(354, 211);
            picPoster.SizeMode = PictureBoxSizeMode.Zoom;
            picPoster.TabIndex = 1;
            picPoster.TabStop = false;
            // 
            // treeShows
            // 
            treeShows.Dock = DockStyle.Fill;
            treeShows.Font = new Font("Arial", 9F, FontStyle.Bold);
            treeShows.HideSelection = false;
            treeShows.Location = new Point(0, 0);
            treeShows.Name = "treeShows";
            treeShows.Size = new Size(354, 351);
            treeShows.TabIndex = 0;
            treeShows.AfterSelect += treeShows_AfterSelect;
            // 
            // listEpisodes
            // 
            listEpisodes.Columns.AddRange(new ColumnHeader[] { colName, colSeason, colEpisode, colDuration, colWatched, colDateModified });
            listEpisodes.Dock = DockStyle.Fill;
            listEpisodes.Font = new Font("Arial", 9F, FontStyle.Bold);
            listEpisodes.FullRowSelect = true;
            listEpisodes.GridLines = true;
            listEpisodes.Location = new Point(0, 0);
            listEpisodes.Name = "listEpisodes";
            listEpisodes.Size = new Size(873, 566);
            listEpisodes.TabIndex = 0;
            listEpisodes.UseCompatibleStateImageBehavior = false;
            listEpisodes.View = View.Details;
            listEpisodes.SelectedIndexChanged += listEpisodes_SelectedIndexChanged;
            // 
            // colName
            // 
            colName.Text = "Episode";
            colName.Width = 260;
            // 
            // colSeason
            // 
            colSeason.Text = "Season";
            colSeason.Width = 70;
            // 
            // colEpisode
            // 
            colEpisode.Text = "Ep";
            colEpisode.Width = 50;
            // 
            // colDuration
            // 
            colDuration.Text = "Duration";
            colDuration.Width = 80;
            // 
            // colWatched
            // 
            colWatched.Text = "Watched";
            colWatched.Width = 80;
            // 
            // colDateModified
            // 
            colDateModified.Text = "Date Modified";
            colDateModified.Width = 120;
            // 
            // layoutBottomPanel
            // 
            layoutBottomPanel.AutoSize = true;
            layoutBottomPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layoutBottomPanel.ColumnCount = 2;
            layoutBottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutBottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutBottomPanel.Controls.Add(flowLayoutPanel2, 0, 0);
            layoutBottomPanel.Controls.Add(flowLayoutPanel3, 1, 0);
            layoutBottomPanel.Controls.Add(flowLayoutPanel1, 0, 1);
            layoutBottomPanel.Controls.Add(gbActions, 1, 1);
            layoutBottomPanel.Dock = DockStyle.Bottom;
            layoutBottomPanel.Font = new Font("Arial", 9F, FontStyle.Bold);
            layoutBottomPanel.Location = new Point(0, 566);
            layoutBottomPanel.Name = "layoutBottomPanel";
            layoutBottomPanel.RowCount = 2;
            layoutBottomPanel.RowStyles.Add(new RowStyle());
            layoutBottomPanel.RowStyles.Add(new RowStyle());
            layoutBottomPanel.Size = new Size(1231, 94);
            layoutBottomPanel.TabIndex = 1;
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.AutoSize = true;
            flowLayoutPanel2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel2.Controls.Add(btnCollapseAll);
            flowLayoutPanel2.Controls.Add(label1);
            flowLayoutPanel2.Controls.Add(txtSearch);
            flowLayoutPanel2.Controls.Add(OpenFileLocation);
            flowLayoutPanel2.Controls.Add(BtnCheckIMDb);
            flowLayoutPanel2.Dock = DockStyle.Fill;
            flowLayoutPanel2.Location = new Point(3, 3);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new Size(609, 31);
            flowLayoutPanel2.TabIndex = 0;
            // 
            // btnCollapseAll
            // 
            btnCollapseAll.AutoSize = true;
            btnCollapseAll.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCollapseAll.Location = new Point(3, 3);
            btnCollapseAll.Name = "btnCollapseAll";
            btnCollapseAll.Size = new Size(83, 25);
            btnCollapseAll.TabIndex = 0;
            btnCollapseAll.Text = "Collapse All";
            btnCollapseAll.Click += btnCollapseAll_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.FlatStyle = FlatStyle.Popup;
            label1.Location = new Point(92, 8);
            label1.Margin = new Padding(3, 8, 0, 0);
            label1.Name = "label1";
            label1.Size = new Size(51, 15);
            label1.TabIndex = 1;
            label1.Text = "Search:";
            // 
            // txtSearch
            // 
            txtSearch.Location = new Point(146, 3);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(130, 21);
            txtSearch.TabIndex = 2;
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // OpenFileLocation
            // 
            OpenFileLocation.AutoSize = true;
            OpenFileLocation.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            OpenFileLocation.Location = new Point(282, 3);
            OpenFileLocation.Name = "OpenFileLocation";
            OpenFileLocation.Size = new Size(121, 25);
            OpenFileLocation.TabIndex = 3;
            OpenFileLocation.Text = "Open File Location";
            OpenFileLocation.Visible = false;
            OpenFileLocation.Click += OpenFileLocation_Click;
            // 
            // BtnCheckIMDb
            // 
            BtnCheckIMDb.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BtnCheckIMDb.BackgroundImageLayout = ImageLayout.Center;
            BtnCheckIMDb.Font = new Font("Arial", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            BtnCheckIMDb.Image = Media_Status.Properties.Resources.IMDb_Logo;
            BtnCheckIMDb.Location = new Point(409, 3);
            BtnCheckIMDb.Name = "BtnCheckIMDb";
            BtnCheckIMDb.Size = new Size(100, 20);
            BtnCheckIMDb.TabIndex = 4;
            BtnCheckIMDb.Click += BtnCheckIMDb_Click;
            // 
            // flowLayoutPanel3
            // 
            flowLayoutPanel3.AutoSize = true;
            flowLayoutPanel3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel3.Controls.Add(btnDelete);
            flowLayoutPanel3.Controls.Add(btnArchive);
            flowLayoutPanel3.Controls.Add(btnMarkWatched);
            flowLayoutPanel3.Controls.Add(btnResume);
            flowLayoutPanel3.Controls.Add(btnPlay);
            flowLayoutPanel3.Controls.Add(btnStop);
            flowLayoutPanel3.Dock = DockStyle.Fill;
            flowLayoutPanel3.Location = new Point(618, 3);
            flowLayoutPanel3.Name = "flowLayoutPanel3";
            flowLayoutPanel3.Size = new Size(610, 31);
            flowLayoutPanel3.TabIndex = 1;
            // 
            // btnDelete
            // 
            btnDelete.AutoSize = true;
            btnDelete.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnDelete.Location = new Point(3, 3);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(53, 25);
            btnDelete.TabIndex = 0;
            btnDelete.Text = "Delete";
            btnDelete.Click += btnDelete_Click;
            // 
            // btnArchive
            // 
            btnArchive.AutoSize = true;
            btnArchive.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnArchive.Location = new Point(62, 3);
            btnArchive.Name = "btnArchive";
            btnArchive.Size = new Size(60, 25);
            btnArchive.TabIndex = 1;
            btnArchive.Text = "Archive";
            btnArchive.Click += btnArchive_Click;
            // 
            // btnMarkWatched
            // 
            btnMarkWatched.AutoSize = true;
            btnMarkWatched.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnMarkWatched.Location = new Point(128, 3);
            btnMarkWatched.Name = "btnMarkWatched";
            btnMarkWatched.Size = new Size(46, 25);
            btnMarkWatched.TabIndex = 2;
            btnMarkWatched.Text = "Mark";
            btnMarkWatched.Click += btnMarkWatched_Click;
            // 
            // btnResume
            // 
            btnResume.AutoSize = true;
            btnResume.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnResume.Location = new Point(180, 3);
            btnResume.Name = "btnResume";
            btnResume.Size = new Size(64, 25);
            btnResume.TabIndex = 3;
            btnResume.Text = "Resume";
            btnResume.Visible = false;
            btnResume.Click += btnResume_Click;
            btnResume.MouseDown += btnResume_MouseDown;
            btnResume.MouseUp += btnResume_MouseUp;
            // 
            // btnPlay
            // 
            btnPlay.AutoSize = true;
            btnPlay.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnPlay.Location = new Point(250, 3);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(41, 25);
            btnPlay.TabIndex = 4;
            btnPlay.Text = "Play";
            btnPlay.Click += btnPlay_Click;
            // 
            // btnStop
            // 
            btnStop.AutoSize = true;
            btnStop.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnStop.Location = new Point(297, 3);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(43, 25);
            btnStop.TabIndex = 5;
            btnStop.Text = "Stop";
            btnStop.Click += btnStop_Click;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel1.Controls.Add(btnoptions);
            flowLayoutPanel1.Controls.Add(BtnFavourite);
            flowLayoutPanel1.Controls.Add(btnRefresh);
            flowLayoutPanel1.Controls.Add(BtnAutoRename);
            flowLayoutPanel1.Controls.Add(btnOrganiz);
            flowLayoutPanel1.Controls.Add(lblStatus);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(3, 40);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(609, 51);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // btnoptions
            // 
            btnoptions.AutoSize = true;
            btnoptions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnoptions.Location = new Point(3, 3);
            btnoptions.Name = "btnoptions";
            btnoptions.Size = new Size(61, 25);
            btnoptions.TabIndex = 0;
            btnoptions.Text = "Options";
            btnoptions.Click += btnoptions_Click;
            // 
            // BtnFavourite
            // 
            BtnFavourite.AutoSize = true;
            BtnFavourite.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BtnFavourite.Location = new Point(70, 3);
            BtnFavourite.Name = "BtnFavourite";
            BtnFavourite.Size = new Size(69, 25);
            BtnFavourite.TabIndex = 1;
            BtnFavourite.Text = "Favourite";
            BtnFavourite.Click += BtnFavourite_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.AutoSize = true;
            btnRefresh.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRefresh.Location = new Point(145, 3);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(62, 25);
            btnRefresh.TabIndex = 2;
            btnRefresh.Text = "Refresh";
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnOrganiz
            // 
            btnOrganiz.AutoSize = true;
            btnOrganiz.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnOrganiz.Location = new Point(321, 3);
            btnOrganiz.Name = "btnOrganiz";
            btnOrganiz.Size = new Size(97, 25);
            btnOrganiz.TabIndex = 3;
            btnOrganiz.Text = "Organize Files";
            btnOrganiz.Click += btnOrganiz_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(424, 8);
            lblStatus.Margin = new Padding(3, 8, 3, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(19, 15);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "---";
            lblStatus.Click += lblStatus_Click;
            // 
            // BtnAutoRename
            // 
            BtnAutoRename.Location = new Point(213, 3);
            BtnAutoRename.Name = "BtnAutoRename";
            BtnAutoRename.Size = new Size(102, 23);
            BtnAutoRename.TabIndex = 5;
            BtnAutoRename.Text = "Auto Rename";
            BtnAutoRename.UseVisualStyleBackColor = true;
            BtnAutoRename.Click += BtnAutoRename_Click;
            // 
            // gbActions
            // 
            gbActions.AutoSize = true;
            gbActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            gbActions.Controls.Add(flowActionsInner);
            gbActions.Dock = DockStyle.Fill;
            gbActions.Location = new Point(618, 40);
            gbActions.Name = "gbActions";
            gbActions.Size = new Size(610, 51);
            gbActions.TabIndex = 3;
            gbActions.TabStop = false;
            gbActions.Text = "Actions";
            // 
            // flowActionsInner
            // 
            flowActionsInner.AutoSize = true;
            flowActionsInner.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowActionsInner.Controls.Add(btnShutdownOptions);
            flowActionsInner.Controls.Add(chkSubtitles);
            flowActionsInner.Controls.Add(cmbScreens);
            flowActionsInner.Controls.Add(chkFullscreen);
            flowActionsInner.Dock = DockStyle.Fill;
            flowActionsInner.Location = new Point(3, 17);
            flowActionsInner.Name = "flowActionsInner";
            flowActionsInner.Size = new Size(604, 31);
            flowActionsInner.TabIndex = 0;
            // 
            // btnShutdownOptions
            // 
            btnShutdownOptions.AutoSize = true;
            btnShutdownOptions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnShutdownOptions.Location = new Point(3, 3);
            btnShutdownOptions.Name = "btnShutdownOptions";
            btnShutdownOptions.Size = new Size(90, 25);
            btnShutdownOptions.TabIndex = 0;
            btnShutdownOptions.Text = "Power Timer";
            btnShutdownOptions.Click += btnShutdownOptions_Click;
            // 
            // chkSubtitles
            // 
            chkSubtitles.AutoSize = true;
            chkSubtitles.Location = new Point(104, 6);
            chkSubtitles.Margin = new Padding(8, 6, 3, 3);
            chkSubtitles.Name = "chkSubtitles";
            chkSubtitles.Size = new Size(76, 19);
            chkSubtitles.TabIndex = 1;
            chkSubtitles.Text = "Subtitles";
            chkSubtitles.UseVisualStyleBackColor = true;
            chkSubtitles.CheckedChanged += chkSubtitles_CheckedChanged;
            // 
            // cmbScreens
            // 
            cmbScreens.FormattingEnabled = true;
            cmbScreens.Location = new Point(186, 3);
            cmbScreens.Name = "cmbScreens";
            cmbScreens.Size = new Size(151, 23);
            cmbScreens.TabIndex = 2;
            cmbScreens.SelectedIndexChanged += cmbScreens_SelectedIndexChanged;
            // 
            // chkFullscreen
            // 
            chkFullscreen.AutoSize = true;
            chkFullscreen.Checked = true;
            chkFullscreen.CheckState = CheckState.Checked;
            chkFullscreen.Location = new Point(348, 6);
            chkFullscreen.Margin = new Padding(8, 6, 3, 3);
            chkFullscreen.Name = "chkFullscreen";
            chkFullscreen.Size = new Size(85, 19);
            chkFullscreen.TabIndex = 3;
            chkFullscreen.Text = "Fullscreen";
            chkFullscreen.UseVisualStyleBackColor = true;
            chkFullscreen.CheckedChanged += chkFullscreen_CheckedChanged;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1231, 660);
            Controls.Add(splitMain);
            Controls.Add(layoutBottomPanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(980, 500);
            Name = "MainForm";
            Text = "Media Control";
            WindowState = FormWindowState.Maximized;
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picPoster).EndInit();
            layoutBottomPanel.ResumeLayout(false);
            layoutBottomPanel.PerformLayout();
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            flowLayoutPanel3.ResumeLayout(false);
            flowLayoutPanel3.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            gbActions.ResumeLayout(false);
            gbActions.PerformLayout();
            flowActionsInner.ResumeLayout(false);
            flowActionsInner.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private Button BtnCheckIMDb;
        private ComboBox cmbScreens;
        private CheckBox chkFullscreen;
        private PictureBox picPoster;
        private SplitContainer splitContainer1;
        private Button BtnAutoRename;
    }
}