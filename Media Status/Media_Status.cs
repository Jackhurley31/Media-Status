using Media_Status;
using Media_Status.Properties;
using MediaControlApp.Helpers;
using MediaControlApp.Models;
using MediaStatus;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using static MediaControlApp.OptionsForm;


namespace MediaControlApp.Forms
{


    public partial class MainForm : Form
    {
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private const int WM_QUERYENDSESSION = 0x0011;

        private List<MediaItem> currentEpisodes = new();

        private List<MediaItem> items = new();

        private bool darkMode = false;

        private UserSettings _userSettings;

        private static readonly VideoSettings _videoSettings = new VideoSettings();

        private static string[] VideoExtensions => _videoSettings.ExtensionsArray;


        private bool _isRefreshing = false;

        private readonly object _saveLock = new object();
        private Task _backgroundSaveTask = Task.CompletedTask;

        private Process currentPlayingProcess;

        private CancellationTokenSource _playbackCts;

        private CancellationTokenSource _posterCancellationTokenSource;

        private bool _isFavouriting = false;

        private bool _isPlaying = false;

        private bool _userStoppedPlayback = false;


        private System.Threading.Timer _autoSaveTimer;
        private readonly object _dirtyLock = new object();
        private bool _isDirty = false;




        public string RootFolder
        {
            get
            {
                // Directly use the folder where the application executable is stored
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        public string JsonPath => Path.Combine(RootFolder, "media_state.json");




        public MainForm()
        {
            InitializeComponent();


            // Runs every 3 minutes after an initial 3-minute delay
            _autoSaveTimer = new System.Threading.Timer(AutoSaveCallback, null, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3));


            // Allows the form to catch key events before focused controls capture them
            this.KeyPreview = true;


            _userSettings = AppConfig.Load();
            chkSubtitles.Checked = _userSettings.EnableSubtitles;
            AppConfig.SyncStartupPath(_userSettings);



            // Update current user label on start
            UpdateCurrentUserLabel();



            listEpisodes.MouseDoubleClick += listEpisodes_MouseDoubleClick;


            // Enable double buffering smoothly without invalid flags
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();

            btnStop.Enabled = false;


            // Load saved settings
            LoadSettings();
            SetPlaybackState(false);
            ApplyDarkMode(darkMode);
            UserSettings userSettings = AppConfig.Load();



            ApplyButtonStyle(btnResume, Color.FromArgb(0, 122, 204), Color.White);   // Accent Blue (Continue/Resume)
            ApplyButtonStyle(btnPlay, Color.FromArgb(20, 50, 25), Color.White);    // Action Green (Start Playback)
            ApplyButtonStyle(btnStop, Color.FromArgb(40, 25, 25), Color.White);    // Alert Red (Stop Playback)



            var menu = new ContextMenuStrip();
            menu.Opening += ContextMenu_Opening; // Wire up the Opening event
            menu.Items.Add("Open File Location", null, OpenFileLocation_Click);
            menu.Items.Add("Archive Show/Season", null, TreeArchive_Click); // Wire up TreeArchive_Click
            menu.Items.Add("Favourite Season", null, BtnFavourite_Click);
            menu.Items.Add("Delete Show/Season", null, btnDelete_Click);   // Wire up btnDelete_Click (or populate TreeDelete_Click)
            treeShows.ContextMenuStrip = menu;

            // Inside MainForm() constructor:
            menu.Items.Add("Auto-Fix Episode Names (Web API)", null, BtnAutoRename_Click);


            treeShows.NodeMouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    treeShows.SelectedNode = e.Node;
                    listEpisodes.SelectedItems.Clear(); // Clear list selection so right-click targets tree

                    // Dynamic menu label based on node level
                    var favMenuItem = treeShows.ContextMenuStrip?.Items.OfType<ToolStripMenuItem>()
                        .FirstOrDefault(i => i.Text.Contains("Favourite"));
                    if (favMenuItem != null)
                    {
                        favMenuItem.Text = e.Node.Parent == null ? "Favourite Entire Show" : "Favourite Season";
                    }
                }
            };


            treeShows.AllowDrop = true;
            treeShows.DragEnter += TreeShows_DragEnter;
            treeShows.DragDrop += TreeShows_DragDrop;
            treeShows.AfterSelect += TreeShows_AfterSelect;



            // 1. Load instantly from the local JSON cache first so the UI opens immediately
            items = MediaCache.Load(JsonPath);
            foreach (var it in items)
            {
                if (string.IsNullOrWhiteSpace(it.CachedSeriesName))
                    it.CacheParsed();
            }
            BuildShowTree();

            // 2. Run the slow network scan asynchronously in the background after startup
            _ = InitializeBackgroundScanAsync();
        }

        private async Task InitializeBackgroundScanAsync()
        {
            await Task.Run(() =>
            {

                ScanDiskAndMerge();
            });

            this.Invoke((MethodInvoker)delegate
            {
                BuildShowTree();
            });
        }




        private void ApplyButtonStyle(Button btn, Color backColor, Color foreColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.BorderColor = Color.White;

            btn.AutoSize = true;
            btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btn.Padding = new Padding(6, 3, 6, 3);
        }


        private UserSettings userSettings = new UserSettings();

        private void LoadSettings()
        {
            userSettings = AppConfig.Load();
            darkMode = userSettings.DarkMode;

            // Wrap the font application to prevent poster and UI layout bugs
            this.SuspendLayout();
            try
            {
                // Only apply custom font settings if they have actually been saved/configured
                if (!string.IsNullOrEmpty(userSettings.UIFontFamily) && userSettings.UIFontSize > 0)
                {
                    FontHelper.ApplyFontToForm(this, userSettings.UIFontSize, userSettings.UIFontFamily);
                }

                // Ensure poster scaling remains stable if applicable
                if (picPoster != null)
                {
                    picPoster.SizeMode = PictureBoxSizeMode.Zoom;
                }
                this.PerformLayout();
            }
            finally
            {
                this.ResumeLayout(true);
            }

            // Load saved window size, state, and position
            LoadScreenSettings();
        }

        private void LoadScreenSettings()
        {
            cmbScreens.Items.Clear();
            cmbScreens.Items.Add("Default / Unlocked (Follow Windows)");

            var classifiedScreens = ScreenLayoutClassifier.GetClassifiedScreens();

            foreach (var info in classifiedScreens)
            {
                cmbScreens.Items.Add(info.Label);
            }

            cmbScreens.DropDownWidth = 340;

            int targetIndex = _userSettings.TargetScreenIndex;
            if (targetIndex >= 0 && targetIndex < classifiedScreens.Count)
            {
                cmbScreens.SelectedIndex = targetIndex + 1;
            }
            else
            {
                cmbScreens.SelectedIndex = 0;
            }

            chkFullscreen.Checked = _userSettings.EnableFullscreen;



            splitContainer1.FixedPanel = FixedPanel.Panel1;

            if (_userSettings != null)
            {
                // Set outer split first
                if (_userSettings.SplitMainDistance > 0 && _userSettings.SplitMainDistance < splitMain.Width)
                {
                    splitMain.SplitterDistance = _userSettings.SplitMainDistance;
                }

                // Then set inner split
                if (_userSettings.SplitContainer1Distance > 0 && _userSettings.SplitContainer1Distance < splitContainer1.Height)
                {
                    splitContainer1.SplitterDistance = _userSettings.SplitContainer1Distance;
                }
            }




        }



        private void cmbScreens_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbScreens.SelectedIndex >= 0)
            {
                // Dropdown index 0 -> TargetScreenIndex = -1 (Unlocked)
                // Dropdown index 1 -> TargetScreenIndex = 0 (Display 1)
                // Dropdown index 2 -> TargetScreenIndex = 1 (Display 2)
                _userSettings.TargetScreenIndex = cmbScreens.SelectedIndex - 1;
                SaveScreenSettings();
            }
        }

        private void chkFullscreen_CheckedChanged(object sender, EventArgs e)
        {
            SaveScreenSettings();
        }

        private void SaveScreenSettings()
        {
            _userSettings.TargetScreenIndex = cmbScreens.SelectedIndex - 1;
            _userSettings.EnableFullscreen = chkFullscreen.Checked;
            AppConfig.Save(_userSettings);
        }


        private string BuildFullPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return string.Empty;

            // Fix single-slash UNC corruption immediately (\ShareDrive -> \\ShareDrive)
            if (filePath.StartsWith("\\") && !filePath.StartsWith("\\\\"))
            {
                filePath = "\\" + filePath;
            }

            // If the path has a duplicated root bug, fix it
            string doubleRoot = RootFolder + RootFolder;
            while (filePath.Contains(doubleRoot, StringComparison.OrdinalIgnoreCase))
            {
                filePath = filePath.Replace(doubleRoot, RootFolder, StringComparison.OrdinalIgnoreCase);
            }

            // If it already starts with the RootFolder, return it directly
            if (filePath.StartsWith(RootFolder, StringComparison.OrdinalIgnoreCase))
            {
                return filePath;
            }

            // If it's a rooted path (UNC or absolute drive like C:\), return it as-is
            if (Path.IsPathRooted(filePath))
            {
                return filePath;
            }

            // Otherwise, it's a relative path inside RootFolder
            string cleanRelativePath = filePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(RootFolder, cleanRelativePath);
        }




        private async void RefreshAll()
        {
            // 1. Load JSON and perform the heavy disk scan entirely in the background
            await Task.Run(() =>
            {
                items = MediaCache.Load(JsonPath);

                foreach (var it in items)
                {
                    if (string.IsNullOrWhiteSpace(it.CachedSeriesName))
                        it.CacheParsed();
                }

                ScanDiskAndMerge();
            });

            // 2. Safely rebuild the UI on the main thread once background work finishes
            BuildShowTree();
        }



        private async void listEpisodes_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Prevent starting new playback if something is already playing
            if (_isPlaying)
            {
                return;
            }

            // Ensure the user double-clicked an actual item, not empty space
            ListViewHitTestInfo info = listEpisodes.HitTest(e.Location);
            if (info.Item != null && info.Item.Tag is MediaItem clickedEpisode)
            {
                int clickedIndex = info.Item.Index;

                // Check if there are episodes following this one in the current list
                bool hasNextEpisodes = clickedIndex < listEpisodes.Items.Count - 1;

                List<MediaItem> playbackQueue = new();

                if (hasNextEpisodes)
                {
                    // Ask user if they want to auto-play subsequent episodes in this list
                    DialogResult autoPlayResult = DarkMessageBox.Show(
                        this,
                        "Would you like to auto-play subsequent episodes in this list?",
                        "Auto Play",
                        MessageBoxButtons.YesNo,
                        darkMode // Passes true for dark, false for light
                    );

                    // If the user closed the window with X, stop playback execution
                    if (autoPlayResult == DialogResult.Cancel)
                    {
                        return;
                    }

                    _isPlaying = true;

                    if (autoPlayResult == DialogResult.Yes)
                    {
                        // Enqueue starting from double-clicked episode through the end of season
                        playbackQueue = listEpisodes.Items
                            .Cast<ListViewItem>()
                            .Skip(clickedIndex)
                            .Select(li => li.Tag as MediaItem)
                            .Where(m => m != null)
                            .ToList();
                    }
                    else
                    {
                        // User selected 'No': Play only the double-clicked episode
                        playbackQueue = new List<MediaItem> { clickedEpisode };
                    }
                }
                else
                {
                    // Last episode in season: play single item without prompting
                    _isPlaying = true;
                    playbackQueue = new List<MediaItem> { clickedEpisode };
                }

                await PlayEpisodesQueueAsync(playbackQueue);
            }
        }


        private ProcessStartInfo GetVlcStartInfo(string filePath, double startSeconds = 0)
        {
            var settings = _userSettings ?? AppConfig.Load();

            if (!string.IsNullOrWhiteSpace(settings.CustomPlayerPath) && File.Exists(settings.CustomPlayerPath))
            {
                string safePath = filePath.Replace("\"", "\\\"");
                string startTimeArg = startSeconds > 5 ? $" --start-time={Math.Floor(startSeconds)}" : "";
                string httpArgs = " --extraintf=http --http-password=mediacontrol --play-and-exit";

                string subArgs = settings.EnableSubtitles ? " --sub-track=0" : " --no-sub-autodetect --sub-track=-1";
                string darkPaletteArg = settings.DarkMode ? " --qt-dark-palette" : "";
                string fullscreenArg = settings.EnableFullscreen ? " --fullscreen" : "";
                string screenArg = "";

                // DO NOT SORT. Match native Screen.AllScreens exactly as the UI does.
                if (settings.TargetScreenIndex >= 0 && settings.TargetScreenIndex < Screen.AllScreens.Length)
                {
                    var targetScreen = Screen.AllScreens[settings.TargetScreenIndex];
                    int winDispNumber = GetDisplayNumber(targetScreen.DeviceName);

                    screenArg = $" --video-x={targetScreen.Bounds.X} --video-y={targetScreen.Bounds.Y}" +
                                $" --qt-fullscreen-screennumber={winDispNumber}" +
                                $" --directx-device=\\\\.\\DISPLAY{winDispNumber}";
                }

                return new ProcessStartInfo
                {
                    FileName = settings.CustomPlayerPath,
                    Arguments = $"\"{safePath}\"{fullscreenArg}{screenArg}{startTimeArg}{httpArgs}{subArgs}{darkPaletteArg}",
                    UseShellExecute = false,
                    CreateNoWindow = false
                };
            }
            return null;
        }


        private int GetDisplayNumber(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) return 1;
            var match = System.Text.RegularExpressions.Regex.Match(deviceName, @"\d+");
            return match.Success && int.TryParse(match.Value, out int num) ? num : 1;
        }



        private async Task<double> GetVlcCurrentTimeSecondsAsync()
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(800);

                    // Basic HTTP Auth credentials (username is empty, password is 'mediacontrol')
                    var authBytes = System.Text.Encoding.ASCII.GetBytes(":mediacontrol");
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

                    string json = await client.GetStringAsync("http://localhost:8080/requests/status.json");

                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("time", out var timeProp) && timeProp.TryGetDouble(out double seconds))
                        {
                            return seconds;
                        }
                    }
                }
            }
            catch
            {
                // Fail quietly if VLC is closing or not responding
            }

            return -1;
        }




        private List<string> _lastMovedLog = new();

        private void BuildShowTree(string filter = "")
        {
            NormalizeSeriesNames();

            treeShows.BeginUpdate();
            try
            {
                // =========================================================================
                // SECTION 1: Capture expanded states and selected node path before clearing
                // =========================================================================
                var expandedShows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (TreeNode node in treeShows.Nodes)
                {
                    if (node.IsExpanded)
                    {
                        expandedShows.Add(node.Text.Replace("★ ", "").Trim());
                    }
                    foreach (TreeNode child in node.Nodes)
                    {
                        if (child.IsExpanded)
                        {
                            expandedShows.Add(child.Text.Replace("★ ", "").Trim());
                        }
                    }
                }

                string selectedShowName = null;
                string selectedSeasonText = null;

                if (treeShows.SelectedNode != null)
                {
                    if (treeShows.SelectedNode.Parent != null)
                    {
                        selectedShowName = treeShows.SelectedNode.Parent.Text.Replace("★ ", "").Trim();
                        selectedSeasonText = treeShows.SelectedNode.Text.Replace("★ ", "").Trim();
                    }
                    else
                    {
                        selectedShowName = treeShows.SelectedNode.Text.Replace("★ ", "").Trim();
                    }
                }

                treeShows.Nodes.Clear();

                // =========================================================================
                // SECTION 2: Filter media items
                // =========================================================================
                string cleanFilter = filter.Trim();

                var itemsToDisplay = string.IsNullOrWhiteSpace(cleanFilter)
                    ? items
                    : items.Where(i =>
                        (i.CachedSeriesName != null && i.CachedSeriesName.Contains(cleanFilter, StringComparison.OrdinalIgnoreCase)) ||
                        (i.FileName != null && i.FileName.Contains(cleanFilter, StringComparison.OrdinalIgnoreCase))).ToList();

                TreeNode nodeToSelect = null;
                // =========================================================================
                // SECTION 3: Root Nodes setup ("Continue", "Recently Added" & "Watched")
                // =========================================================================

                // 1. Define helper to identify film items vs TV show episodes
                bool IsFilmItem(MediaItem item)
                {
                    if (!string.IsNullOrEmpty(item.CachedCategory) &&
                        item.CachedCategory.Contains("Film", StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (!string.IsNullOrEmpty(item.FilePath) &&
                        (item.FilePath.Contains(@"\Films\", StringComparison.OrdinalIgnoreCase) ||
                         item.FilePath.Contains(@"\Film\", StringComparison.OrdinalIgnoreCase)))
                        return true;

                    return false;
                }

                // 2. Separate Films from TV Items first
                var filmItems = itemsToDisplay.Where(IsFilmItem).ToList();
                var tvItems = itemsToDisplay.Except(filmItems).ToList();

                // 3. Create "Continue" node at Index 0 if any show has progress, markup data, or an unfinished first episode
                TreeNode topContinueNode = null;
                var hasContinueItems = tvItems
                    .GroupBy(i => i.CachedSeriesName)
                    .Any(g =>
                    {
                        var showList = g.ToList();
                        bool hasProgressOrMarkup = showList.Any(ep => ep.IsWatched(AppConfig.CurrentUser) || ep.GetSavedPosition(AppConfig.CurrentUser) > 0);

                        bool firstEpNeedsFinishing = showList
                            .FirstOrDefault(ep => ep.CachedSeason == 1 && ep.CachedEpisode == 1) is MediaItem firstEp &&
                            firstEp.GetSavedPosition(AppConfig.CurrentUser) > 0 &&
                            !firstEp.IsWatched(AppConfig.CurrentUser);

                        return (hasProgressOrMarkup || firstEpNeedsFinishing) && !showList.All(ep => ep.IsWatched(AppConfig.CurrentUser));
                    });

                if (hasContinueItems)
                {
                    topContinueNode = new TreeNode("Continue");
                    treeShows.Nodes.Add(topContinueNode); // Index 0
                }

                // 4. Create "Recently Added" node directly BELOW "Continue" (Checking current calendar month)
                TreeNode topRecentlyAddedNode = null;
                DateTime now = DateTime.Now;

                var recentTvItems = tvItems
                    .Where(i =>
                    {
                        // Use cached date directly if available, fallback safely without hitting File.GetLastWriteTime on UI thread
                        DateTime modDate = i.CachedDateModified;
                        if (modDate == DateTime.MinValue)
                        {
                            try
                            {
                                string fullPath = BuildFullPath(i.FilePath);
                                if (File.Exists(fullPath))
                                {
                                    modDate = File.GetLastWriteTime(fullPath);
                                    i.CachedDateModified = modDate; // Cache it so it never lags again
                                }
                            }
                            catch { }
                        }

                        // Check if the item's modification date falls within the current year and month
                        return modDate.Year == now.Year && modDate.Month == now.Month;
                    })
                    .ToList();

                if (recentTvItems.Any())
                {
                    topRecentlyAddedNode = new TreeNode("Recently Added");
                    treeShows.Nodes.Add(topRecentlyAddedNode); // Placed right below Continue

                    // Group recently added items by TV Show and populate child nodes
                    var recentShows = recentTvItems
                        .GroupBy(i => i.CachedSeriesName)
                        .OrderBy(g => g.Key);

                    foreach (var show in recentShows)
                    {
                        if (string.IsNullOrWhiteSpace(show.Key)) continue;

                        string showNameClean = show.Key;
                        var showNode = topRecentlyAddedNode.Nodes.Add(showNameClean);

                        var seasons = show
                            .GroupBy(i => i.CachedSeason)
                            .OrderBy(g => g.Key);

                        foreach (var season in seasons)
                        {
                            string seasonLabel = "Season " + season.Key;
                            var seasonNode = showNode.Nodes.Add(seasonLabel);
                            seasonNode.Tag = season.ToList();

                            if (string.Equals(selectedShowName, showNameClean, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(selectedSeasonText, seasonLabel, StringComparison.OrdinalIgnoreCase))
                            {
                                nodeToSelect = seasonNode;
                            }
                        }
                    }

                    if (expandedShows.Contains("Recently Added") || !string.IsNullOrWhiteSpace(cleanFilter))
                    {
                        topRecentlyAddedNode.Expand();
                    }
                }

                // 5. Create "Watched" node directly BELOW "Recently Added"
                TreeNode topWatchedNode = null;
                var hasWatchedItems = itemsToDisplay.Any(i => i.IsWatched(AppConfig.CurrentUser));
                if (hasWatchedItems)
                {
                    topWatchedNode = new TreeNode("Watched");
                    treeShows.Nodes.Add(topWatchedNode); // Placed right after Recently Added
                }

                // =========================================================================
                // SECTION 4: Process Films (Grouped by Release Year)
                // =========================================================================
                if (filmItems.Any())
                {
                    TreeNode filmsNode = new TreeNode("Films");

                    // Calculate index so Films sits below Continue and Watched
                    int filmsIndex = treeShows.Nodes.Count;
                    treeShows.Nodes.Insert(filmsIndex, filmsNode);

                    // Group films by Release Year (extracted from title/filename, fallback to "Other")
                    var filmsByYear = filmItems
                        .GroupBy(f =>
                        {
                            string name = f.CachedSeriesName;
                            if (string.IsNullOrWhiteSpace(name) ||
                                name.Equals("Films", StringComparison.OrdinalIgnoreCase) ||
                                name.Equals("Film", StringComparison.OrdinalIgnoreCase))
                            {
                                name = Path.GetFileNameWithoutExtension(f.FilePath);
                            }

                            var match = System.Text.RegularExpressions.Regex.Match(name, @"\b(19\d{2}|20\d{2})\b");
                            return match.Success ? match.Value : "Other";
                        })
                        .OrderByDescending(g => g.Key);

                    foreach (var yearGroup in filmsByYear)
                    {
                        string yearLabel = yearGroup.Key;
                        TreeNode yearNode = filmsNode.Nodes.Add(yearLabel);

                        // Group films by title, but clean out generic folder names like "Films" or "Film"
                        var filmGroups = yearGroup
                            .GroupBy(f =>
                            {
                                string name = f.CachedSeriesName;
                                if (string.IsNullOrWhiteSpace(name) ||
                                    name.Equals("Films", StringComparison.OrdinalIgnoreCase) ||
                                    name.Equals("Film", StringComparison.OrdinalIgnoreCase))
                                {
                                    return Path.GetFileNameWithoutExtension(f.FilePath);
                                }
                                return name;
                            })
                            .OrderBy(g => g.Key);

                        foreach (var filmGroup in filmGroups)
                        {
                            var groupList = filmGroup.ToList();
                            bool isFilmWatched = groupList.All(f => f.IsWatched(AppConfig.CurrentUser));
                            string filmTitle = filmGroup.Key;

                            // Skip if title is still "Films"
                            if (filmTitle.Equals("Films", StringComparison.OrdinalIgnoreCase) ||
                                filmTitle.Equals("Film", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            bool isFav = groupList.Any(f => f.FavouriteByUser.GetValueOrDefault(AppConfig.CurrentUser));
                            string nodeText = isFav ? "★ " + filmTitle : filmTitle;

                            if (isFilmWatched && topWatchedNode != null)
                            {
                                var watchedFilmNode = topWatchedNode.Nodes.Add(nodeText);
                                watchedFilmNode.Tag = groupList;
                                if (string.Equals(selectedShowName, filmTitle, StringComparison.OrdinalIgnoreCase))
                                    nodeToSelect = watchedFilmNode;
                            }
                            else
                            {
                                var filmNode = yearNode.Nodes.Add(nodeText);
                                filmNode.Tag = groupList;
                                if (string.Equals(selectedShowName, filmTitle, StringComparison.OrdinalIgnoreCase))
                                    nodeToSelect = filmNode;
                            }
                        }

                        // Expand year node if it was previously expanded or filtering is active
                        if (expandedShows.Contains(yearLabel) || !string.IsNullOrWhiteSpace(cleanFilter))
                        {
                            yearNode.Expand();
                        }
                    }

                    if (expandedShows.Contains("Films") || !string.IsNullOrWhiteSpace(cleanFilter))
                    {
                        filmsNode.Expand();
                    }
                }

                // =========================================================================
                // SECTION 5: Process TV Shows (Strictly TV/Series items with Seasons)
                // =========================================================================
                var shows = tvItems
                    .GroupBy(i => i.CachedSeriesName)
                    .OrderByDescending(g => g.Any(ep => ep.FavouriteByUser.GetValueOrDefault(AppConfig.CurrentUser)))
                    .ThenBy(g => g.Key);

                foreach (var show in shows)
                {
                    if (string.IsNullOrWhiteSpace(show.Key)) continue;

                    var allShowItems = show.ToList();
                    bool isShowFav = allShowItems.Any(ep => ep.FavouriteByUser.GetValueOrDefault(AppConfig.CurrentUser));
                    string showNameClean = show.Key;
                    string showNodeText = isShowFav ? "★ " + showNameClean : showNameClean;

                    bool isEntireShowWatched = allShowItems.Count > 0 && allShowItems.All(ep => ep.IsWatched(AppConfig.CurrentUser));
                    bool hasAnyWatchedOrProgress = allShowItems.Any(ep => ep.IsWatched(AppConfig.CurrentUser) || ep.GetSavedPosition(AppConfig.CurrentUser) > 0);

                    bool firstEpNeedsFinishing = allShowItems
                        .FirstOrDefault(ep => ep.CachedSeason == 1 && ep.CachedEpisode == 1) is MediaItem firstEp &&
                        firstEp.GetSavedPosition(AppConfig.CurrentUser) > 0 &&
                        !firstEp.IsWatched(AppConfig.CurrentUser);

                    TreeNode showNode;

                    if (isEntireShowWatched)
                    {
                        if (topWatchedNode == null)
                        {
                            topWatchedNode = new TreeNode("Watched");
                            // Insert topWatchedNode at index 1 (or 0 if topContinueNode is null)
                            int insertIndex = topContinueNode != null ? 1 : 0;
                            treeShows.Nodes.Insert(insertIndex, topWatchedNode);
                        }

                        showNode = topWatchedNode.Nodes.Add(showNodeText);
                    }
                    else if ((hasAnyWatchedOrProgress || firstEpNeedsFinishing) && topContinueNode != null)
                    {
                        // Route partially watched or started shows (including Episode 1 with ToFinish markup) to "Continue" at top
                        showNode = topContinueNode.Nodes.Add(showNodeText);
                    }
                    else
                    {
                        // Unwatched shows stay at the main root level
                        showNode = treeShows.Nodes.Add(showNodeText);
                    }

                    var seasons = show
                        .GroupBy(i => i.CachedSeason)
                        .OrderBy(g => g.Key);

                    foreach (var season in seasons)
                    {
                        var seasonList = season.ToList();
                        bool isSeasonFav = seasonList.Any(ep => ep.FavouriteByUser.GetValueOrDefault(AppConfig.CurrentUser));
                        bool isSeasonWatched = seasonList.Count > 0 && seasonList.All(ep => ep.IsWatched(AppConfig.CurrentUser));

                        string seasonLabel = "Season " + season.Key;
                        string nodeText = isSeasonFav ? "★ " + seasonLabel : seasonLabel;

                        var seasonNode = showNode.Nodes.Add(nodeText);
                        seasonNode.Tag = seasonList;

                        // Apply color when all episodes in the season are watched
                        if (isSeasonWatched)
                        {
                            // DarkGreen for Light Mode, Dim gray or LightGreen for Dark Mode
                            seasonNode.ForeColor = darkMode ? Color.LimeGreen : Color.DarkGreen;
                        }

                        if (string.Equals(selectedShowName, showNameClean, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(selectedSeasonText, seasonLabel, StringComparison.OrdinalIgnoreCase))
                        {
                            nodeToSelect = seasonNode;
                        }
                    }

                    bool shouldExpandShow = expandedShows.Contains(showNameClean) ||
                                           string.Equals(selectedShowName, showNameClean, StringComparison.OrdinalIgnoreCase) ||
                                           !string.IsNullOrWhiteSpace(cleanFilter);

                    if (shouldExpandShow)
                    {
                        showNode.Expand();
                    }

                    if (string.Equals(selectedShowName, showNameClean, StringComparison.OrdinalIgnoreCase) && selectedSeasonText == null)
                    {
                        nodeToSelect = showNode;
                    }
                }

                // =========================================================================
                // SECTION 6: Restore expanded folder states & selected node
                // =========================================================================
                if (topContinueNode != null && (expandedShows.Contains("Continue") || !string.IsNullOrWhiteSpace(cleanFilter)))
                {
                    topContinueNode.Expand();
                }

                if (topWatchedNode != null && (expandedShows.Contains("Watched") || !string.IsNullOrWhiteSpace(cleanFilter)))
                {
                    topWatchedNode.Expand();
                }

                if (nodeToSelect != null)
                {
                    treeShows.SelectedNode = nodeToSelect;
                }
            }
            finally
            {
                treeShows.EndUpdate();

                UpdateResumeButtonVisibility();
            }
        }


        private void TreeShows_AfterSelect(object sender, TreeViewEventArgs e)
        {


            if (e.Node.Tag is List<MediaItem> seasonItems)
            {
                currentEpisodes = seasonItems;
                LoadEpisodes(currentEpisodes);

                // Auto-resize columns to fit both the column header AND the longest item text
                foreach (ColumnHeader column in listEpisodes.Columns)
                {
                    column.Width = -2; // -2 automatically sizes to fit the header and contents perfectly
                }


                UpdateButtonStates();

            }
            else
            {
                listEpisodes.Items.Clear();
                currentEpisodes = new List<MediaItem>();

                // Reset title back to original
                this.Text = "Media Control";
            }
        }


        private async void treeShows_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;

            // 1. Walk up to the root node to check the top-level section
            TreeNode showNode = e.Node;
            while (showNode.Parent != null)
            {
                showNode = showNode.Parent;
            }

            string rootName = showNode.Text.Trim();

            // 2. Ignore the main "Films" category entirely
            // CHECK IF CLICKED NODE IS UNDER "Films"
            if (rootName.Equals("Films", StringComparison.OrdinalIgnoreCase))
            {
                string nodeText = e.Node.Text.Replace("★ ", "").Trim();

                // If the user clicked the main "Films" header, a Year folder, or the "Other" folder, 
                // they don't have media items in their Tag. Clear the poster and exit!
                if (e.Node.Tag == null || nodeText.Equals("Other", StringComparison.OrdinalIgnoreCase))
                {
                    picPoster.Image = null;
                    return;
                }

                // If it's an actual individual film node (Tag contains the media list), load the poster
                if (e.Node.Tag is List<MediaItem> filmList && filmList.Count > 0)
                {
                    string filmTitle = nodeText;
                    await LoadAutoFilmPosterAsync(filmTitle);
                    return;
                }
            }

            // 3. Extract the clean show title
            string showTitle = rootName.Replace("★ ", "").Trim();

            // 4. Handle smart/dynamic folders (Continue, Watched, Recently added)
            if (rootName.Equals("Continue", StringComparison.OrdinalIgnoreCase) ||
                rootName.Equals("Watched", StringComparison.OrdinalIgnoreCase) ||
                rootName.Equals("Recently added", StringComparison.OrdinalIgnoreCase))
            {
                var mediaItem = e.Node.Tag as MediaItem;

                // If it has a cached series name, use that (means it's a TV show episode)
                if (mediaItem != null && !string.IsNullOrEmpty(mediaItem.CachedSeriesName))
                {
                    showTitle = mediaItem.CachedSeriesName;
                }
                else if (e.Node.Parent != null)
                {
                    // Otherwise, walk up the node hierarchy inside the smart list to find the grouping
                    TreeNode current = e.Node;
                    while (current.Parent != null && current.Parent.Parent != null)
                    {
                        current = current.Parent;
                    }
                    showTitle = current.Text.Replace("★ ", "").Trim();
                }
                else
                {
                    // Clicked the category header itself
                    picPoster.Image = null;
                    return;
                }
            }

            // 5. Final safety check: if the title still points to a category header or empty string, abort
            string[] ignoredCategories = { "Top", "Continue", "Watched", "Recently added", "Films" };
            if (Array.Exists(ignoredCategories, cat => cat.Equals(showTitle, StringComparison.OrdinalIgnoreCase)))
            {
                picPoster.Image = null;
                return;
            }

            await LoadAutoOnlinePosterAsync(showTitle);
        }



        private void listEpisodes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProcessingStop) return;

            if (listEpisodes.SelectedItems.Count > 0)
            {
                var selectedMedia = listEpisodes.SelectedItems[0].Tag as MediaItem;
                UpdatePlayButtonText(selectedMedia);
            }
            else
            {
                btnPlay.Text = "Play";
            }
        }


        private void TreeShows_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }



        // 1. Add this small helper class to MainForm.cs
        private class DragState
        {
            public bool SkipAllExisting { get; set; }
        }

        // 2. Updated DragDrop handler
        private async void TreeShows_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (droppedPaths == null || droppedPaths.Length == 0) return;

            Point pt = treeShows.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeShows.GetNodeAt(pt);

            string targetDir = RootFolder;

            if (targetNode != null)
            {
                var itemsFromNode = GetItemsFromNode(targetNode);
                var firstItem = itemsFromNode.FirstOrDefault();

                if (firstItem != null)
                {
                    string fullPath = BuildFullPath(firstItem.FilePath);
                    string folder = Path.GetDirectoryName(fullPath);

                    if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                    {
                        targetDir = folder;
                    }
                }
            }

            var state = new DragState();

            try
            {
                await Task.Run(() =>
                {
                    foreach (string path in droppedPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            string destFolder = Path.Combine(targetDir, Path.GetFileName(path));
                            CopyDirectoryWithVideos(path, destFolder, state);
                        }
                        else if (File.Exists(path))
                        {
                            string ext = Path.GetExtension(path).ToLower();
                            if (VideoExtensions.Contains(ext))
                            {
                                string destPath = Path.Combine(targetDir, Path.GetFileName(path));
                                CopyFileSafely(path, destPath, state);
                            }
                        }
                    }
                });

                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy dropped items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 3. Updated CopyFileSafely method using DragState
        private void CopyFileSafely(string sourceFile, string destFile, DragState state)
        {
            if (File.Exists(destFile))
            {
                if (state.SkipAllExisting) return;

                bool shouldOverwrite = false;

                this.Invoke((MethodInvoker)delegate
                {
                    var result = DarkMessageBox.Show(
                        this,
                        $"The file '{Path.GetFileName(destFile)}' already exists in the target folder.\n\nDo you want to overwrite it?",
                        "File Conflict",
                        MessageBoxButtons.YesNoCancel
                    );

                    if (result == DialogResult.Yes)
                    {
                        shouldOverwrite = true;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        state.SkipAllExisting = true;
                    }
                });

                if (state.SkipAllExisting || !shouldOverwrite) return;
            }

            File.Copy(sourceFile, destFile, true);
        }

        // 4. Updated CopyDirectoryWithVideos method using DragState
        private void CopyDirectoryWithVideos(string sourceDir, string destDir, DragState state)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string ext = Path.GetExtension(file).ToLower();
                if (VideoExtensions.Contains(ext))
                {
                    string destFile = Path.Combine(destDir, Path.GetFileName(file));
                    CopyFileSafely(file, destFile, state);
                }
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string newDestDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectoryWithVideos(subDir, newDestDir, state);
            }
        }


        private async Task LoadAutoOnlinePosterAsync(string mediaPathOrTitle)
        {
            if (string.IsNullOrWhiteSpace(mediaPathOrTitle))
            {
                picPoster.Image = null;
                return;
            }

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string showsDir = Path.Combine(baseDir, "Shows");

                // 1. Resolve path using your app's BuildFullPath or direct checks
                string resolvedPath = null;
                try
                {
                    resolvedPath = BuildFullPath(mediaPathOrTitle);
                }
                catch
                {
                    resolvedPath = mediaPathOrTitle;
                }

                string targetDir = null;
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    if (Directory.Exists(resolvedPath))
                    {
                        targetDir = resolvedPath;
                    }
                    else if (File.Exists(resolvedPath))
                    {
                        targetDir = Path.GetDirectoryName(resolvedPath);
                    }
                }

                // Fallback lookup if targetDir is empty
                if (string.IsNullOrEmpty(targetDir))
                {
                    string directCheck = Path.Combine(baseDir, mediaPathOrTitle);
                    if (Directory.Exists(directCheck)) targetDir = directCheck;
                    else if (File.Exists(directCheck)) targetDir = Path.GetDirectoryName(directCheck);
                    else if (Directory.Exists(showsDir))
                    {
                        foreach (var showDir in Directory.GetDirectories(showsDir))
                        {
                            if (Path.GetFileName(showDir).Equals(mediaPathOrTitle, StringComparison.OrdinalIgnoreCase))
                            {
                                targetDir = showDir;
                                break;
                            }
                            foreach (var seasonDir in Directory.GetDirectories(showDir))
                            {
                                if (Path.GetFileName(seasonDir).Equals(mediaPathOrTitle, StringComparison.OrdinalIgnoreCase) ||
                                    File.Exists(Path.Combine(seasonDir, mediaPathOrTitle)))
                                {
                                    targetDir = seasonDir;
                                    break;
                                }
                            }
                            if (targetDir != null) break;
                        }
                    }
                }

                // 2. Search for local poster files by walking UP through nested folders (Supports deep subfolders & category roots)
                string localPosterFile = null;
                string[] posterNames = { "poster.jpg", "poster.jpeg", "poster.png", "folder.jpg", "folder.jpeg", "folder.png", "cover.jpg", "cover.jpeg", "cover.png" };

                if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
                {
                    DirectoryInfo currentDir = new DirectoryInfo(targetDir);

                    // Walk up safely through multiple folder levels (up to 6 levels deep)
                    for (int i = 0; i < 6 && currentDir != null; i++)
                    {
                        try
                        {
                            foreach (var name in posterNames)
                            {
                                string candidate = Path.Combine(currentDir.FullName, name);
                                if (File.Exists(candidate))
                                {
                                    localPosterFile = candidate;
                                    break;
                                }
                            }
                            if (localPosterFile != null) break;

                            currentDir = currentDir.Parent;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }

                // 3. LOAD LOCAL POSTER IMMEDIATELY (MemoryStream prevents file locking)
                if (!string.IsNullOrEmpty(localPosterFile) && File.Exists(localPosterFile))
                {
                    _posterCancellationTokenSource?.Cancel();
                    try
                    {
                        using (var fs = new FileStream(localPosterFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (var img = Image.FromStream(fs))
                            {
                                picPoster.Image = new Bitmap(img);
                            }
                        }
                        return; // Local poster found and loaded! Done instantly.
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading local poster: {ex.Message}");
                    }
                }

                // 4. Extract the CORRECT Show Title for Web Search (Climbs out of Season folders automatically)
                string sourceNameToClean = string.Empty;
                if (!string.IsNullOrEmpty(targetDir))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(targetDir);
                    // If we are inside a Season folder (e.g. "Season 1", "S01"), use the Parent (Show) name
                    if (System.Text.RegularExpressions.Regex.IsMatch(dirInfo.Name, @"^(season\s*\d+|s\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) && dirInfo.Parent != null)
                    {
                        sourceNameToClean = dirInfo.Parent.Name;
                    }
                    else
                    {
                        sourceNameToClean = dirInfo.Name;
                    }
                }
                else
                {
                    sourceNameToClean = Path.GetFileNameWithoutExtension(resolvedPath);
                }

                string cleanTitle = System.Text.RegularExpressions.Regex.Replace(sourceNameToClean, @"\b(19|20)\d{2}\b", "");
                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"(?i)(1080p|720p|hrhdtv|web-dl|bluray|x264|x265|miniseries)", "");
                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"[\(\[].*?[\)\]]", "");
                cleanTitle = cleanTitle.Replace(".", " ").Replace("_", " ").Trim();

                if (string.IsNullOrWhiteSpace(cleanTitle)) return;

                string posterCacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MediaControlApp", "PosterCache");
                Directory.CreateDirectory(posterCacheDir);

                string safeFileName = string.Join("_", cleanTitle.Split(Path.GetInvalidFileNameChars()));
                string localPosterPath = Path.Combine(posterCacheDir, safeFileName + ".jpg");

                if (File.Exists(localPosterPath))
                {
                    _posterCancellationTokenSource?.Cancel();
                    try
                    {
                        using (var fs = new FileStream(localPosterPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (var img = Image.FromStream(fs))
                            {
                                picPoster.Image = new Bitmap(img);
                            }
                        }
                        return;
                    }
                    catch
                    {
                        File.Delete(localPosterPath);
                    }
                }

                _posterCancellationTokenSource?.Cancel();
                _posterCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _posterCancellationTokenSource.Token;

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    string imageUrl = null;

                    string searchUrl = $"https://api.tvmaze.com/singlesearch/shows?q={Uri.EscapeDataString(cleanTitle)}";
                    HttpResponseMessage response = await client.GetAsync(searchUrl, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                        using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                        {
                            if (doc.RootElement.TryGetProperty("image", out var imgObj) &&
                                imgObj.TryGetProperty("medium", out var medProp))
                            {
                                imageUrl = medProp.GetString();
                            }
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        byte[] imageBytes = await client.GetByteArrayAsync(imageUrl, cancellationToken);
                        await File.WriteAllBytesAsync(localPosterPath, imageBytes, cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            using (var ms = new MemoryStream(imageBytes))
                            {
                                using (var img = Image.FromStream(ms))
                                {
                                    picPoster.Image = new Bitmap(img);
                                }
                            }
                        }
                        return;
                    }
                }

                if (!_posterCancellationTokenSource.Token.IsCancellationRequested)
                {
                    picPoster.Image = null;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-poster error: {ex.Message}");
                picPoster.Image = null;
            }
        }




        private async Task LoadAutoFilmPosterAsync(string mediaPathOrTitle)
        {
            if (string.IsNullOrWhiteSpace(mediaPathOrTitle))
            {
                picPoster.Image = null;
                return;
            }

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string resolvedPath = null;
                try { resolvedPath = BuildFullPath(mediaPathOrTitle); }
                catch { resolvedPath = mediaPathOrTitle; }

                string targetDir = null;
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    if (Directory.Exists(resolvedPath)) targetDir = resolvedPath;
                    else if (File.Exists(resolvedPath)) targetDir = Path.GetDirectoryName(resolvedPath);
                }

                // 1. Check local poster files first (poster.jpg, folder.jpg, cover.jpg, etc.)
                string localPosterFile = null;
                string[] posterNames = { "poster.jpg", "poster.jpeg", "poster.png", "folder.jpg", "folder.jpeg", "folder.png", "cover.jpg", "cover.jpeg", "cover.png" };

                if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
                {
                    DirectoryInfo currentDir = new DirectoryInfo(targetDir);
                    for (int i = 0; i < 4 && currentDir != null; i++)
                    {
                        try
                        {
                            foreach (var name in posterNames)
                            {
                                string candidate = Path.Combine(currentDir.FullName, name);
                                if (File.Exists(candidate))
                                {
                                    localPosterFile = candidate;
                                    break;
                                }
                            }
                            if (localPosterFile != null) break;
                            currentDir = currentDir.Parent;
                        }
                        catch { break; }
                    }
                }

                if (!string.IsNullOrEmpty(localPosterFile) && File.Exists(localPosterFile))
                {
                    _posterCancellationTokenSource?.Cancel();
                    try
                    {
                        using (var fs = new FileStream(localPosterFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (var img = Image.FromStream(fs))
                            {
                                picPoster.Image = new Bitmap(img);
                            }
                        }
                        return;
                    }
                    catch { }
                }

                // 2. Clean Film Title
                string sourceNameToClean = !string.IsNullOrEmpty(targetDir) ? new DirectoryInfo(targetDir).Name : Path.GetFileNameWithoutExtension(resolvedPath);
                string cleanTitle = System.Text.RegularExpressions.Regex.Replace(sourceNameToClean, @"\b(19|20)\d{2}\b", "");
                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"(?i)(1080p|720p|hrhdtv|web-dl|bluray|x264|x265)", "");
                cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, @"[\(\[].*?[\)\]]", "");
                cleanTitle = cleanTitle.Replace(".", " ").Replace("_", " ").Trim();

                if (string.IsNullOrWhiteSpace(cleanTitle)) return;

                string posterCacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MediaControlApp", "PosterCache");
                Directory.CreateDirectory(posterCacheDir);

                string safeFileName = string.Join("_", cleanTitle.Split(Path.GetInvalidFileNameChars()));
                string localPosterPath = Path.Combine(posterCacheDir, safeFileName + ".jpg");

                if (File.Exists(localPosterPath))
                {
                    _posterCancellationTokenSource?.Cancel();
                    try
                    {
                        using (var fs = new FileStream(localPosterPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (var img = Image.FromStream(fs))
                            {
                                picPoster.Image = new Bitmap(img);
                            }
                        }
                        return;
                    }
                    catch { File.Delete(localPosterPath); }
                }

                _posterCancellationTokenSource?.Cancel();
                _posterCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _posterCancellationTokenSource.Token;

                // 3. Keyless Open Search Query for Movie Posters
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    string imageUrl = null;

                    // Using the open IMDb suggestion mirror (No API key needed)
                    string searchUrl = $"https://v2.sg.media-imdb.com/suggestion/x/{Uri.EscapeDataString(cleanTitle)}.json";
                    HttpResponseMessage response = await client.GetAsync(searchUrl, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                        using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                        {
                            if (doc.RootElement.TryGetProperty("d", out var dArr) && dArr.GetArrayLength() > 0)
                            {
                                var firstMatch = dArr[0];
                                if (firstMatch.TryGetProperty("i", out var imgObj) && imgObj.TryGetProperty("imageUrl", out var urlProp))
                                {
                                    imageUrl = urlProp.GetString();
                                }
                            }
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        byte[] imageBytes = await client.GetByteArrayAsync(imageUrl, cancellationToken);
                        await File.WriteAllBytesAsync(localPosterPath, imageBytes, cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            using (var ms = new MemoryStream(imageBytes))
                            {
                                using (var img = Image.FromStream(ms))
                                {
                                    picPoster.Image = new Bitmap(img);
                                }
                            }
                        }
                        return;
                    }
                }

                if (!_posterCancellationTokenSource.Token.IsCancellationRequested)
                {
                    picPoster.Image = null;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Film auto-poster error: {ex.Message}");
                picPoster.Image = null;
            }
        }














        private async Task SaveAndMergeState()
        {
            // Snapshot state variables on UI thread before hopping to background thread
            var localItemsSnapshot = items.ToList();
            var currentUser = AppConfig.CurrentUser ?? "Default";
            var jsonPath = JsonPath;

            await Task.Run(() =>
            {
                int retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        // 1. Create backup on background thread[cite: 1]
                        CreateJsonBackup();

                        // 2. Read disk state on background thread to preserve OTHER users' progress
                        if (File.Exists(jsonPath))
                        {
                            var diskItems = MediaCache.Load(jsonPath);

                            var localDict = localItemsSnapshot.ToDictionary(
                                i => i.FilePath,
                                i => i,
                                StringComparer.OrdinalIgnoreCase
                            );

                            foreach (var diskItem in diskItems)
                            {
                                if (!string.IsNullOrEmpty(diskItem.FilePath) &&
                                    localDict.TryGetValue(diskItem.FilePath, out var localItem))
                                {
                                    localItem.WatchedByUser ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                                    localItem.FavouriteByUser ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                                    localItem.SavedPositionByUser ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                                    // Capture and protect the CURRENT user's live in-memory state 
                                    // so a disk read can NEVER wipe out what was just watched/marked!
                                    bool hasInMemWatched = localItem.WatchedByUser.TryGetValue(currentUser, out var memWatched) && memWatched;
                                    bool hasInMemFav = localItem.FavouriteByUser.TryGetValue(currentUser, out var memFav) && memFav;
                                    double inMemPos = localItem.SavedPositionByUser.TryGetValue(currentUser, out var memPos) ? memPos : 0;

                                    // Merge other users' data from disk
                                    if (diskItem.WatchedByUser != null)
                                    {
                                        foreach (var kvp in diskItem.WatchedByUser)
                                        {
                                            if (!string.Equals(kvp.Key, currentUser, StringComparison.OrdinalIgnoreCase))
                                            {
                                                localItem.WatchedByUser[kvp.Key] = kvp.Value;
                                            }
                                        }
                                    }

                                    if (diskItem.FavouriteByUser != null)
                                    {
                                        foreach (var kvp in diskItem.FavouriteByUser)
                                        {
                                            if (!string.Equals(kvp.Key, currentUser, StringComparison.OrdinalIgnoreCase))
                                            {
                                                localItem.FavouriteByUser[kvp.Key] = kvp.Value;
                                            }
                                        }
                                    }

                                    if (diskItem.SavedPositionByUser != null)
                                    {
                                        foreach (var kvp in diskItem.SavedPositionByUser)
                                        {
                                            if (!string.Equals(kvp.Key, currentUser, StringComparison.OrdinalIgnoreCase))
                                            {
                                                localItem.SavedPositionByUser[kvp.Key] = kvp.Value;
                                            }
                                        }
                                    }

                                    // RESTORE current user's live in-memory state explicitly
                                    localItem.WatchedByUser[currentUser] = hasInMemWatched || (diskItem.WatchedByUser?.TryGetValue(currentUser, out var dWatch) == true && dWatch);
                                    localItem.FavouriteByUser[currentUser] = hasInMemFav || (diskItem.FavouriteByUser?.TryGetValue(currentUser, out var dFav) == true && dFav);

                                    // If memory position is 0 (finished) or has a value, honor memory first, else fallback to disk
                                    localItem.SavedPositionByUser[currentUser] = inMemPos > 0 ? inMemPos : (diskItem.SavedPositionByUser?.TryGetValue(currentUser, out var dPos) == true ? dPos : 0);
                                }
                            }
                        }

                        // 3. Save merged items to master file & local fast cache[cite: 1]
                        MediaCache.Save(jsonPath, localItemsSnapshot);
                        MediaCache.SaveLocal(localItemsSnapshot);

                        break; // Success
                    }
                    catch (IOException ex) when (retries > 1)
                    {
                        retries--;
                        System.Diagnostics.Debug.WriteLine($"File locked by another instance. Retrying... ({retries} left)[cite: 1]");
                        Thread.Sleep(150);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SaveAndMergeState trace: {ex.Message}[cite: 1]");
                        break;
                    }
                }
            });
        }



        private void CreateJsonBackup()
        {
            try
            {
                if (!File.Exists(JsonPath)) return;

                string baseDir = Path.GetDirectoryName(JsonPath);
                if (string.IsNullOrEmpty(baseDir))
                {
                    baseDir = AppDomain.CurrentDomain.BaseDirectory;
                }

                string backupDir = Path.Combine(baseDir, "media_state_backups");
                Directory.CreateDirectory(backupDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string backupFile = Path.Combine(backupDir, $"media_state_{timestamp}.json");

                File.Copy(JsonPath, backupFile, overwrite: true);

                // Keep the 5 most recent backups
                int maxBackups = 5;
                var backupFiles = new DirectoryInfo(backupDir)
                    .GetFiles("media_state_*.json")
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(maxBackups);

                foreach (var file in backupFiles)
                {
                    file.Delete();
                }
            }
            catch
            {
                // Fail silently so backup errors don't interrupt saving
            }
        }



        private async void LoadEpisodes(List<MediaItem> eps)
        {
            listEpisodes.Items.Clear();

            TimeSpan totalDuration = TimeSpan.Zero;
            TimeSpan remainingDuration = TimeSpan.Zero;

            var itemsNeedingDuration = new List<(ListViewItem Item, MediaItem Ep, string FullPath)>();

            foreach (var ep in eps
                .OrderByDescending(i => i.FavouriteByUser != null && i.FavouriteByUser.TryGetValue(AppConfig.CurrentUser, out bool f) && f)
                .ThenBy(i => i.CachedEpisode))
            {
                bool isFav = ep.FavouriteByUser != null && ep.FavouriteByUser.TryGetValue(AppConfig.CurrentUser, out bool f) && f;
                string displayName = isFav ? "★ " + ep.FileName : ep.FileName;
                string fullPath = BuildFullPath(ep.FilePath);

                totalDuration += ep.Duration;
                if (!ep.IsWatched(AppConfig.CurrentUser))
                {
                    remainingDuration += ep.Duration;
                }

                var li = new ListViewItem(displayName);                       // 0: colName
                li.SubItems.Add(ep.CachedSeason.ToString());                   // 1: colSeason
                li.SubItems.Add(ep.CachedEpisode.ToString());                  // 2: colEpisode

                // 3: colDuration
                // Ensures we handle TimeSpan or raw seconds safely, with padded minutes (e.g. 05:20 instead of 5:20)
                TimeSpan ts = ep.Duration is TimeSpan t ? t : TimeSpan.FromSeconds(Convert.ToDouble(ep.Duration));

                string durationDisplay = ts > TimeSpan.Zero
                    ? (ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss"))
                    : "--:--";

                li.SubItems.Add(durationDisplay);

                // 4: colWatched
                bool isPartial = ep.GetSavedPosition(AppConfig.CurrentUser) > 0 && !ep.IsWatched(AppConfig.CurrentUser);
                li.SubItems.Add(GetWatchedDisplay(ep, isPartial));

                // 5: colDateModified (UK dd/MM/yyyy)
                DateTime dt = ep.GetDateModified(fullPath);
                string dateUkDisplay = dt != DateTime.MinValue ? dt.ToString("dd/MM/yyyy") : "N/A";
                li.SubItems.Add(dateUkDisplay);

                li.Tag = ep;
                listEpisodes.Items.Add(li);

                if (ep.Duration == TimeSpan.Zero && File.Exists(fullPath))
                {
                    itemsNeedingDuration.Add((li, ep, fullPath));
                }
            }

            // 1. Hydrate missing durations immediately using the local C-drive cache
            List<MediaItem> cachedItems = MediaCache.LoadLocal();
            var cacheDict = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in cachedItems)
            {
                string key = !string.IsNullOrEmpty(item.FilePath) ? BuildFullPath(item.FilePath) : "";
                if (!string.IsNullOrEmpty(key))
                {
                    cacheDict[key] = item.Duration;
                }
            }

            foreach (var triple in itemsNeedingDuration.ToList())
            {
                if (cacheDict.TryGetValue(triple.FullPath, out TimeSpan cachedDur) && cachedDur > TimeSpan.Zero)
                {
                    triple.Ep.Duration = cachedDur;

                    string display = triple.Ep.Duration.Hours > 0
                        ? triple.Ep.Duration.ToString(@"h\:mm\:ss")
                        : triple.Ep.Duration.ToString(@"m\:ss");
                    triple.Item.SubItems[3].Text = display;

                    totalDuration += cachedDur;
                    if (!triple.Ep.IsWatched(AppConfig.CurrentUser))
                    {
                        remainingDuration += cachedDur;
                    }

                    itemsNeedingDuration.Remove(triple);
                }
            }

            UpdateWatchTimeSummary(totalDuration, remainingDuration);

            // 2. Fetch any remaining missing durations asynchronously in background
            if (itemsNeedingDuration.Count > 0)
            {
                await Task.Run(() =>
                {
                    bool updatedAny = false;

                    foreach (var (item, ep, fullPath) in itemsNeedingDuration)
                    {
                        TimeSpan dur = GetVideoDuration(fullPath);
                        if (dur > TimeSpan.Zero)
                        {
                            ep.Duration = dur;
                            updatedAny = true;

                            // Push ListView and summary updates back to UI thread safely
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                if (listEpisodes.Items.Contains(item))
                                {
                                    string display = ep.Duration.Hours > 0
                                        ? ep.Duration.ToString(@"h\:mm\:ss")
                                        : ep.Duration.ToString(@"m\:ss");
                                    item.SubItems[3].Text = display; // Index 3 = colDuration
                                }

                                TimeSpan newTotal = eps.Aggregate(TimeSpan.Zero, (acc, x) => acc + x.Duration);
                                TimeSpan newRemaining = eps.Where(x => !x.IsWatched(AppConfig.CurrentUser)).Aggregate(TimeSpan.Zero, (acc, x) => acc + x.Duration);
                                UpdateWatchTimeSummary(newTotal, newRemaining);
                            });
                        }
                    }

                    // 3. Merge updated durations into the master C-drive cache and save
                    if (updatedAny)
                    {
                        var masterCache = MediaCache.LoadLocal();
                        var masterDict = new Dictionary<string, MediaItem>(StringComparer.OrdinalIgnoreCase);

                        foreach (var item in masterCache)
                        {
                            if (!string.IsNullOrEmpty(item.FilePath))
                            {
                                masterDict[item.FilePath] = item;
                            }
                        }

                        foreach (var ep in eps)
                        {
                            if (!string.IsNullOrEmpty(ep.FilePath))
                            {
                                masterDict[ep.FilePath] = ep;
                            }
                        }

                        MediaCache.SaveLocal(masterDict.Values.ToList());
                    }
                });
            }
        }



        private string GetWatchedDisplay(MediaItem ep, bool isPartial = false)
        {
            if (isPartial)
            {
                return "⏳ (ToFinish)";
            }

            var watchedUsers = ep.WatchedByUser?
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList() ?? new List<string>();

            return watchedUsers.Count > 0 ? "✓ (" + string.Join(", ", watchedUsers) + ")" : "";
        }





        private static int? _cachedDurationIndex = null;

        // Helper 1: Reads video duration natively using Windows Shell metadata (Optimized with Caching)
        private TimeSpan GetVideoDuration(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath)) return TimeSpan.Zero;

                string folder = Path.GetDirectoryName(fullPath);
                string filename = Path.GetFileName(fullPath);

                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return TimeSpan.Zero;

                dynamic shell = Activator.CreateInstance(shellType);
                var shellFolder = shell.NameSpace(folder);
                var folderItem = shellFolder?.ParseName(filename);

                if (folderItem != null)
                {
                    string val = null;

                    // 1. Try standard Windows Shell property indices first
                    int[] commonIndices = new int[] { 27, 32, 19 };
                    foreach (int index in commonIndices)
                    {
                        try
                        {
                            string rawVal = shellFolder.GetDetailsOf(folderItem, index);
                            if (!string.IsNullOrWhiteSpace(rawVal) && rawVal.Contains(":"))
                            {
                                val = rawVal;
                                break;
                            }
                        }
                        catch { }
                    }

                    // 2. Try the cached index if standard indices missed
                    if (string.IsNullOrWhiteSpace(val) && _cachedDurationIndex.HasValue)
                    {
                        try
                        {
                            val = shellFolder.GetDetailsOf(folderItem, _cachedDurationIndex.Value);
                        }
                        catch { }
                    }

                    // 3. Fallback: Search column names once, then cache the result
                    if (string.IsNullOrWhiteSpace(val))
                    {
                        for (int i = 0; i < 350; i++)
                        {
                            try
                            {
                                string header = shellFolder.GetDetailsOf(null, i);
                                if (string.Equals(header, "Duration", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(header, "Length", StringComparison.OrdinalIgnoreCase))
                                {
                                    _cachedDurationIndex = i; // Cache for subsequent calls[cite: 1]
                                    val = shellFolder.GetDetailsOf(folderItem, i);
                                    break;
                                }
                            }
                            catch { }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        // Strip invisible Unicode formatting characters inserted by Windows
                        val = System.Text.RegularExpressions.Regex.Replace(val, @"[^\d:]", "").Trim();

                        var parts = val.Split(':');

                        // Parse MM:SS
                        if (parts.Length == 2 &&
                            int.TryParse(parts[0], out int mins) &&
                            int.TryParse(parts[1], out int secs))
                        {
                            return new TimeSpan(0, mins, secs);
                        }
                        // Parse HH:MM:SS
                        else if (parts.Length == 3 &&
                                 int.TryParse(parts[0], out int hrs) &&
                                 int.TryParse(parts[1], out int m) &&
                                 int.TryParse(parts[2], out int s))
                        {
                            return new TimeSpan(hrs, m, s);
                        }
                        else if (TimeSpan.TryParse(val, out TimeSpan ts))
                        {
                            return ts;
                        }
                    }
                }
            }
            catch { }

            return TimeSpan.Zero;
        }

        // Helper 2: Displays total unwatched time in the window title bar
        private void UpdateWatchTimeSummary(TimeSpan total, TimeSpan remaining)
        {
            string remainingText = remaining.Hours > 0
                ? $"{remaining.Hours}h {remaining.Minutes}m"
                : $"{remaining.Minutes}m";

            string totalText = total.Hours > 0
                ? $"{total.Hours}h {total.Minutes}m"
                : $"{total.Minutes}m";

            this.Text = $"Media Control - Season Time Remaining: {remainingText} (Total: {totalText})";
        }



        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            string originalText = btnRefresh.Text;

            // Active accent color (Cyan for Dark Mode, Dark Blue for Light Mode)
            bool isDarkMode = btnRefresh.BackColor.GetBrightness() < 0.5f;
            btnRefresh.ForeColor = isDarkMode ? Color.LightCyan : Color.DarkBlue;

            var sw = Stopwatch.StartNew();

            var progress = new Progress<string>(status =>
            {
                btnRefresh.Text = status;
            });

            try
            {
                await Task.Run(() => ScanDiskAndMerge(progress));

                btnRefresh.Text = "Building Tree...";
                BuildShowTree();

                sw.Stop();

                string durationStr = sw.Elapsed.TotalSeconds < 1
                    ? $"{sw.ElapsedMilliseconds}ms"
                    : $"{sw.Elapsed.TotalSeconds:F1}s";

                // Show a self-closing notification that automatically disappears after 5 seconds
                ShowAutoClosingBox(
                    this,
                    $"Scan completed in {durationStr}.\nTotal files found: {items.Count}",
                    "Refresh Complete",
                    5000, // 5 seconds
                    darkMode
                );
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(
                    this,
                    $"Refresh failed: {ex.Message}",
                    "Scan Error",
                    MessageBoxButtons.OK,
                    darkMode
                );
            }
            finally
            {
                sw.Stop();
                btnRefresh.Text = originalText;

                // Restore readable text color based on theme when finished
                isDarkMode = btnRefresh.BackColor.GetBrightness() < 0.5f;
                btnRefresh.ForeColor = isDarkMode ? Color.White : Color.Black;

                _isRefreshing = false;
            }
        }



        private void ShowAutoClosingBox(Form owner, string message, string title, int timeoutMillis, bool isDarkMode)
        {
            using (var box = new Form())
            {
                box.Text = title;
                box.StartPosition = FormStartPosition.CenterParent;
                box.FormBorderStyle = FormBorderStyle.FixedDialog;
                box.MaximizeBox = false;
                box.MinimizeBox = false;
                box.ClientSize = new Size(320, 140);

                box.BackColor = isDarkMode ? Color.FromArgb(35, 35, 35) : SystemColors.Control;
                box.ForeColor = isDarkMode ? Color.White : Color.Black;

                var lbl = new Label
                {
                    Text = message,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular)
                };

                var timer = new System.Windows.Forms.Timer { Interval = timeoutMillis };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    box.Close();
                };
                timer.Start();

                box.Controls.Add(lbl);
                box.ShowDialog(owner);
            }
        }





        private void AutoSaveCallback(object state)
        {
            bool shouldSave = false;

            lock (_dirtyLock)
            {
                if (_isDirty)
                {
                    shouldSave = true;
                    _isDirty = false; // Reset the flag so it doesn't repeatedly save if idle
                }
            }

            if (shouldSave)
            {
                // Invoke your existing save method safely in the background
                SaveAndMergeState();
            }
        }

        // Call this method whenever a user action changes data (e.g., favorite toggled, progress updated)
        public void MarkStateDirty()
        {
            lock (_dirtyLock)
            {
                _isDirty = true;
            }
        }






        // Merge that normalizes only for comparison and reuses existing items
        private void ScanDiskAndMerge(IProgress<string> progress = null)
        {
            progress?.Report("Saving Current State...");
            // Ensure any unsaved in-memory changes are written to disk before reloading
            SaveAndMergeState();

            progress?.Report("Loading Cache...");

            items = MediaCache.Load(JsonPath) ?? new List<MediaItem>();
            foreach (var it in items)
            {
                if (string.IsNullOrWhiteSpace(it.CachedSeriesName))
                    it.CacheParsed();
            }

            static string NormalizeForCompare(string p)
            {
                if (string.IsNullOrEmpty(p)) return string.Empty;
                while (p.Contains("\\\\"))
                    p = p.Replace("\\\\", "\\");
                return p.Trim();
            }

            if (string.IsNullOrWhiteSpace(RootFolder) || !Directory.Exists(RootFolder))
                return;

            // Index existing items by Exact File Path
            var pathDict = items
                .Where(i => !string.IsNullOrEmpty(i.FilePath))
                .GroupBy(i => NormalizeForCompare(i.FilePath), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // Backup lookup by Filename (e.g., "S01E01 - Pilot.mp4") so moved folders retain history
            var fileNameDict = items
                .Where(i => !string.IsNullOrEmpty(i.FilePath))
                .GroupBy(i => Path.GetFileName(i.FilePath), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var videoExtSet = new HashSet<string>(VideoExtensions, StringComparer.OrdinalIgnoreCase);
            var merged = new List<MediaItem>();

            progress?.Report("Scanning Disk...");

            var foundFiles = Directory.EnumerateFiles(RootFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\Archive\\", StringComparison.OrdinalIgnoreCase) &&
                            videoExtSet.Contains(Path.GetExtension(f)))
                .Select(NormalizeForCompare)
                .ToList();

            int totalFiles = foundFiles.Count;
            int processed = 0;

            foreach (var file in foundFiles)
            {
                processed++;

                if (processed % 25 == 0 || processed == totalFiles)
                {
                    progress?.Report($"Scanning ({processed}/{totalFiles})...");
                }

                // 1. Direct path match
                if (pathDict.TryGetValue(file, out var existing))
                {
                    if (string.IsNullOrWhiteSpace(existing.CachedSeriesName))
                        existing.CacheParsed();

                    merged.Add(existing);
                }
                else
                {
                    // 2. File moved/reordered: Check if filename matches an existing cached item
                    string fileName = Path.GetFileName(file);

                    if (fileNameDict.TryGetValue(fileName, out var movedItem))
                    {
                        // Transfer watched/favourite history to the new location
                        movedItem.FilePath = file;
                        if (string.IsNullOrWhiteSpace(movedItem.CachedSeriesName))
                            movedItem.CacheParsed();

                        merged.Add(movedItem);

                        // Optional enhancement: remove from dictionary so duplicate filenames 
                        // don't incorrectly re-assign the same historical object to multiple distinct files
                        fileNameDict.Remove(fileName);
                    }
                    else
                    {
                        // 3. Brand new file
                        var mi = new MediaItem
                        {
                            FilePath = file,
                            WatchedByUser = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase),
                            FavouriteByUser = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                        };
                        mi.CacheParsed();
                        merged.Add(mi);
                    }
                }
            }

            progress?.Report("Saving State...");
            items = merged;
            SaveAndMergeState();
        }

        private async void TreeArchive_Click(object sender, EventArgs e)
        {
            var node = treeShows.SelectedNode;
            if (node == null) return;

            List<MediaItem> itemsToProcess = GetItemsFromNode(node);
            if (itemsToProcess.Count == 0) return;

            bool isSeriesNode = (node.Tag == null);

            // Use the central helper to get the exact file path safely
            string firstPath = BuildFullPath(itemsToProcess.First().FilePath);

            if (!File.Exists(firstPath))
            {
                MessageBox.Show($"File not found at resolved path:\n{firstPath}", "Path Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string seasonDir = Path.GetDirectoryName(firstPath);
            string targetFolderToMove = isSeriesNode ? Path.GetDirectoryName(seasonDir) : seasonDir;

            var confirm = MessageBox.Show(
                $"Are you sure you want to archive this entire {(isSeriesNode ? "show" : "season")} folder?",
                "Confirm Archive",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes) return;

            treeShows.Enabled = true;
            RefreshAll();

            try
            {
                await Task.Run(() =>
                {
                    string archiveRoot = Path.Combine(RootFolder, "Archive");
                    Directory.CreateDirectory(archiveRoot);

                    if (!string.IsNullOrEmpty(targetFolderToMove) && Directory.Exists(targetFolderToMove))
                    {
                        string folderName = Path.GetFileName(targetFolderToMove);
                        string destDir = "";

                        if (isSeriesNode)
                        {
                            destDir = Path.Combine(archiveRoot, folderName);
                        }
                        else
                        {
                            string seriesName = node.Parent?.Text.Replace("★ ", "").Trim() ?? "Unknown";
                            string destSeriesDir = Path.Combine(archiveRoot, seriesName);
                            Directory.CreateDirectory(destSeriesDir);
                            destDir = Path.Combine(destSeriesDir, folderName);
                        }

                        if (Directory.Exists(destDir))
                        {
                            Directory.Delete(destDir, true);
                        }

                        Directory.Move(targetFolderToMove, destDir);
                    }

                    foreach (var ep in itemsToProcess)
                    {
                        items.Remove(ep);
                    }

                    SaveAndMergeState(); ;
                });

                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Archive failed: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                treeShows.Enabled = true;
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }




        private void GroupBox_Paint(object sender, PaintEventArgs e)
        {
            if (sender is not GroupBox box) return;

            // Determine target colors
            Color textColor = darkMode ? Color.White : Color.Black;
            Color borderColor = darkMode ? Color.FromArgb(70, 70, 70) : SystemColors.ControlDark;

            // Measure text bounds
            Size textSize = TextRenderer.MeasureText(box.Text, box.Font);
            Rectangle borderRect = box.ClientRectangle;
            borderRect.Y += textSize.Height / 2;
            borderRect.Height -= textSize.Height / 2;

            // Draw custom border ignoring default black OS line
            e.Graphics.Clear(box.BackColor);
            using (Pen pen = new Pen(borderColor))
            {
                e.Graphics.DrawRectangle(pen, borderRect.X, borderRect.Y, borderRect.Width - 1, borderRect.Height - 1);
            }

            // Draw custom text label with readable foreground color
            Rectangle textRect = new Rectangle(box.Padding.Left, 0, textSize.Width, textSize.Height);
            e.Graphics.FillRectangle(new SolidBrush(box.BackColor), textRect);
            TextRenderer.DrawText(e.Graphics, box.Text, box.Font, textRect, textColor, TextFormatFlags.Left);
        }



        private void ApplyDarkMode(bool enabled)
        {
            Color back = enabled ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            Color text = enabled ? Color.White : Color.Black;

            this.BackColor = back;

            // Apply native dark mode to the main form title bar (Windows 10/11)
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10)
            {
                int darkModeEnabled = enabled ? 1 : 0;
                if (DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeEnabled, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkModeEnabled, sizeof(int));
                }
            }

            // Apply theme recursively across top-level controls and nested controls (like GroupBoxes)
            ApplyControlTheme(this.Controls, back, text, enabled);

            // Keep explicit overrides for key controls
            if (listEpisodes != null)
            {
                listEpisodes.BackColor = enabled ? Color.FromArgb(45, 45, 45) : Color.White;
                listEpisodes.ForeColor = text;
            }

            if (treeShows != null)
            {
                treeShows.BackColor = enabled ? Color.FromArgb(45, 45, 45) : Color.White;
                treeShows.ForeColor = text;
            }


            ApplyButtonStyle(OpenFileLocation, Color.FromArgb(60, 64, 67), Color.White); // Dark Charcoal (Blends nicely with dark forms)
            ApplyButtonStyle(btnCollapseAll, Color.FromArgb(60, 64, 67), Color.White); // Dark Charcoal (Blends nicely with dark forms)
            ApplyButtonStyle(btnoptions, Color.FromArgb(60, 64, 67), Color.White); // Dark Charcoal (Blends nicely with dark forms)
            ApplyButtonStyle(BtnFavourite, Color.FromArgb(60, 64, 67), Color.White); // Dark Charcoal (Blends nicely with dark forms)
            ApplyButtonStyle(btnRefresh, Color.FromArgb(60, 64, 67), Color.White); // Dark Charcoal (Blends nicely with dark forms)
            ApplyButtonStyle(btnOrganiz, Color.FromArgb(60, 64, 67), Color.White); // Dark Charcoal (Blends nicely with dark forms)
            ApplyButtonStyle(BtnAutoRename, Color.FromArgb(60, 64, 67), Color.White); // Dark Charcoal (Blends nicely with dark forms)
            ApplyButtonStyle(btnAutoRenameOptions, Color.FromArgb(60, 64, 67), Color.White); // Dark Charcoal (Blends nicely with dark forms)
            ApplyButtonStyle(btnSupport, Color.FromArgb(60, 64, 67), Color.White); // Dark Charcoal (Blends nicely with dark forms)



            ApplyButtonStyle(btnArchive, Color.FromArgb(60, 64, 67), Color.White);
            ApplyButtonStyle(btnDelete, Color.FromArgb(60, 64, 67), Color.White); // Delete: Vibrant Coral Red (High-visibility destructive action)
            ApplyButtonStyle(btnMarkWatched, Color.FromArgb(225, 212, 187), Color.FromArgb(30, 30, 30));
            ApplyButtonStyle(btnResume, Color.FromArgb(0, 122, 204), Color.White);   // Accent Blue (Continue/Resume)
            ApplyButtonStyle(btnPlay, Color.FromArgb(20, 50, 25), Color.White);    // Action Green (Start Playback)
            ApplyButtonStyle(btnStop, Color.FromArgb(40, 25, 25), Color.White);    // Alert Red (Stop Playback)




        }

        private void ApplyControlTheme(Control.ControlCollection controls, Color back, Color text, bool enabled)
        {
            foreach (Control c in controls)
            {
                if (c is GroupBox gb)
                {
                    // Set GroupBox text color and transparent background to avoid double-click/rendering glitches
                    gb.ForeColor = text;
                    gb.BackColor = Color.Transparent;

                    // Recursively process controls inside the GroupBox
                    if (gb.HasChildren)
                    {
                        ApplyControlTheme(gb.Controls, back, text, enabled);
                    }
                }
                else if (c is TextBox || c is ListBox || c is ComboBox)
                {
                    c.BackColor = enabled ? Color.FromArgb(45, 45, 45) : Color.White;
                    c.ForeColor = text;
                }
                else
                {
                    c.BackColor = back;
                    c.ForeColor = text;

                    // Recurse down into any other container controls (Panels, UserControls, etc.)
                    if (c.HasChildren)
                    {
                        ApplyControlTheme(c.Controls, back, text, enabled);
                    }
                }
            }
        }



        private void UpdateButtonStates()
        {
            bool hasValidSelection = HasValidSelectionForExplorer();

            // Hide or show the standalone button directly
            OpenFileLocation.Visible = hasValidSelection;

            // Optional: If you prefer disabling it rather than hiding it completely:
            // btnOpenFileLocation.Enabled = hasValidSelection;
        }

        private void OpenFileLocation_Click(object sender, EventArgs e)
        {
            // Handle ListView selection
            if (listEpisodes.SelectedItems.Count > 0)
            {
                foreach (ListViewItem li in listEpisodes.SelectedItems)
                {
                    if (li.Tag is MediaItem ep)
                    {
                        string full = BuildFullPath(ep.FilePath);
                        if (File.Exists(full))
                        {
                            Process.Start("explorer.exe", $"/select,\"{full}\"");
                        }
                    }
                }
                return;
            }

            // Handle TreeView selection
            var node = treeShows.SelectedNode;
            if (node == null) return;

            if (node.Tag is List<MediaItem> seasonItems)
            {
                var firstEp = seasonItems.FirstOrDefault();
                if (firstEp != null)
                {
                    string folder = Path.GetDirectoryName(BuildFullPath(firstEp.FilePath));
                    if (Directory.Exists(folder)) Process.Start("explorer.exe", $"\"{folder}\"");
                }
            }
            else if (node.Tag is MediaItem episode)
            {
                string full = BuildFullPath(episode.FilePath);
                if (File.Exists(full)) Process.Start("explorer.exe", $"/select,\"{full}\"");
            }
            else if (node.Tag == null)
            {
                string seriesName = node.Text.Replace("★ ", "").Trim();
                var seriesEp = items.FirstOrDefault(i => string.Equals(i.CachedSeriesName, seriesName, StringComparison.OrdinalIgnoreCase));
                if (seriesEp != null)
                {
                    string folder = Path.GetDirectoryName(BuildFullPath(seriesEp.FilePath));
                    if (Directory.Exists(folder)) Process.Start("explorer.exe", $"\"{folder}\"");
                }
            }
        }

        // Wire this handler to treeShows.ContextMenuStrip.Opening or ContextMenuStrip.Opening
        private void ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            bool hasValidSelection = HasValidSelectionForExplorer();

            // Toggle item visibility on Context Menu
            var openMenuItem = treeShows.ContextMenuStrip?.Items
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(i => i.Text.Contains("Open File Location"));

            if (openMenuItem != null)
            {
                openMenuItem.Visible = hasValidSelection;
            }
        }

        private bool HasValidSelectionForExplorer()
        {
            // 1. Check ListView Selection
            if (listEpisodes.SelectedItems.Count > 0)
            {
                return listEpisodes.SelectedItems
                    .Cast<ListViewItem>()
                    .Any(li => li.Tag is MediaItem ep && File.Exists(BuildFullPath(ep.FilePath)));
            }

            // 2. Check TreeView Selection
            var node = treeShows.SelectedNode;
            if (node == null) return false;

            if (node.Tag is List<MediaItem> seasonItems)
            {
                var firstEp = seasonItems.FirstOrDefault();
                return firstEp != null && Directory.Exists(Path.GetDirectoryName(BuildFullPath(firstEp.FilePath)));
            }

            if (node.Tag is MediaItem episode)
            {
                return File.Exists(BuildFullPath(episode.FilePath));
            }

            if (node.Tag == null)
            {
                string seriesName = node.Text.Replace("★ ", "").Trim();
                var seriesEp = items.FirstOrDefault(i => string.Equals(i.CachedSeriesName, seriesName, StringComparison.OrdinalIgnoreCase));
                return seriesEp != null && Directory.Exists(Path.GetDirectoryName(BuildFullPath(seriesEp.FilePath)));
            }

            return false;
        }


        // Helper method to collect items whether a Season or Show node is selected
        private List<MediaItem> GetItemsFromNode(TreeNode node)
        {
            if (node == null) return new List<MediaItem>();

            // Case 1: Season Node (Tag contains the list of episodes)
            if (node.Tag is List<MediaItem> seasonItems)
            {
                return seasonItems;
            }

            // Case 2: Show/Series Node (Tag is null, extract name from text)
            if (node.Tag == null)
            {
                string seriesName = node.Text.Replace("★ ", "").Trim();
                return items.Where(i => string.Equals(i.CachedSeriesName, seriesName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return new List<MediaItem>();
        }



        private void NormalizeSeriesNames()
        {
            var knownSeries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.CachedSeriesName))
                    item.CacheParsed();

                // Pass current show name into StringMatcher to check for typos against known shows
                string matchedName = MediaControlApp.Helpers.StringMatcher.MatchOrCreateSeriesName(item.CachedSeriesName, knownSeries);

                item.OverrideSeriesName(matchedName);
                knownSeries.Add(matchedName);
            }
        }



        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            BuildShowTree(txtSearch.Text);

            // Automatically collapse all tree nodes when search is cleared
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                treeShows.CollapseAll();
            }
        }

        private void btnCollapseAll_Click(object sender, EventArgs e)
        {
            treeShows.CollapseAll();
        }




        private async void BtnFavourite_Click(object sender, EventArgs e)
        {
            if (_isFavouriting) return;

            List<MediaItem> itemsToToggle = new();
            bool isEpisodeSelection = false;

            bool isFromTreeContextMenu = sender is ToolStripMenuItem menuItem && menuItem.Owner == treeShows.ContextMenuStrip;

            if (!isFromTreeContextMenu && listEpisodes.SelectedItems.Count > 0)
            {
                isEpisodeSelection = true;
                foreach (ListViewItem li in listEpisodes.SelectedItems)
                {
                    if (li.Tag is MediaItem ep) itemsToToggle.Add(ep);
                }
            }
            else if (treeShows.SelectedNode != null)
            {
                itemsToToggle = GetItemsFromNode(treeShows.SelectedNode);
            }

            if (itemsToToggle.Count == 0) return;

            _isFavouriting = true;

            string originalText = BtnFavourite.Text;

            // Active accent color (Cyan for Dark Mode, Dark Blue for Light Mode)
            bool isDarkMode = BtnFavourite.BackColor.GetBrightness() < 0.5f;

            this.Cursor = Cursors.WaitCursor;
            BtnFavourite.Text = "Updating...";
            BtnFavourite.ForeColor = isDarkMode ? Color.LightCyan : Color.DarkBlue;

            try
            {
                foreach (var ep in itemsToToggle)
                {
                    ep.FavouriteByUser ??= new Dictionary<string, bool>();
                }

                bool hasAnyFav = itemsToToggle.Any(i =>
                    i.FavouriteByUser.TryGetValue(AppConfig.CurrentUser, out bool isFav) && isFav);

                bool newFavState = !hasAnyFav;

                foreach (var ep in itemsToToggle)
                {
                    ep.FavouriteByUser[AppConfig.CurrentUser] = newFavState;
                }

                await Task.Run(() => SaveAndMergeState());

                BuildShowTree();

                if (treeShows.SelectedNode?.Tag is List<MediaItem> activeSeasonItems)
                {
                    currentEpisodes = activeSeasonItems;
                    LoadEpisodes(currentEpisodes);
                }
                else if (isEpisodeSelection)
                {
                    LoadEpisodes(currentEpisodes);
                }

                if (isEpisodeSelection)
                {
                    foreach (ListViewItem li in listEpisodes.Items)
                    {
                        if (li.Tag is MediaItem ep && itemsToToggle.Contains(ep))
                        {
                            li.Selected = true;
                            li.Focused = true;
                        }
                    }
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
                BtnFavourite.Text = originalText;

                // Restore readable text color based on theme when finished
                isDarkMode = BtnFavourite.BackColor.GetBrightness() < 0.5f;
                BtnFavourite.ForeColor = isDarkMode ? Color.White : Color.Black;

                _isFavouriting = false;
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            List<MediaItem> itemsToProcess = new();

            if (listEpisodes.SelectedItems.Count > 0)
            {
                foreach (ListViewItem li in listEpisodes.SelectedItems)
                {
                    if (li.Tag is MediaItem ep) itemsToProcess.Add(ep);
                }
            }
            else
            {
                var node = treeShows.SelectedNode;
                if (node == null) return;
                itemsToProcess = GetItemsFromNode(node);
            }

            if (itemsToProcess.Count == 0) return;

            var confirm = DarkMessageBox.Show(
                this,
                $"Are you sure you want to PERMANENTLY delete {itemsToProcess.Count} media file(s)?\n\nEmpty parent folders will also be cleaned up safely.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                darkMode
            );

            if (confirm != DialogResult.Yes) return;

            foreach (var ep in itemsToProcess) items.Remove(ep);

            listEpisodes.Items.Clear();
            picPoster.Image = null;

            this.Cursor = Cursors.WaitCursor;

            try
            {
                await Task.Run(() =>
                {
                    var affectedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var ep in itemsToProcess)
                    {
                        string fullPath = BuildFullPath(ep.FilePath);
                        if (File.Exists(fullPath))
                        {
                            string dir = Path.GetDirectoryName(fullPath);
                            if (!string.IsNullOrEmpty(dir))
                            {
                                affectedDirectories.Add(dir);
                            }

                            File.Delete(fullPath);
                        }
                    }

                    foreach (var dir in affectedDirectories)
                    {
                        CleanupEmptyDirectoriesUpwards(dir, RootFolder);
                    }

                    SaveAndMergeState();
                });

                // Trigger the full refresh button workflow
                btnRefresh_Click(sender, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(this, $"Delete failed: {ex.Message}", "Error", MessageBoxButtons.OK, darkMode);
                btnRefresh_Click(sender, EventArgs.Empty);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        // Safe helper method to recursively delete empty parent folders without deleting loose root files or crossing boundaries
        private void CleanupEmptyDirectoriesUpwards(string targetDir, string rootBoundary)
        {
            try
            {
                // Ensure we never delete the root folder itself or folders outside of it
                while (!string.IsNullOrEmpty(targetDir) &&
                       !string.Equals(targetDir, rootBoundary, StringComparison.OrdinalIgnoreCase) &&
                       Directory.Exists(targetDir))
                {
                    // Only delete the directory if it is completely empty (no files and no subdirectories)
                    if (!Directory.EnumerateFileSystemEntries(targetDir).Any())
                    {
                        Directory.Delete(targetDir, false);
                        targetDir = Path.GetDirectoryName(targetDir); // Move up to parent folder
                    }
                    else
                    {
                        break; // Stop if the folder still contains other files or sibling seasons
                    }
                }
            }
            catch
            {
                // Fail silently so file deletion isn't interrupted by locked handles or permission hitches
            }
        }

        private async void btnArchive_Click(object sender, EventArgs e)
        {
            List<MediaItem> itemsToProcess = new();

            if (listEpisodes.SelectedItems.Count > 0)
            {
                foreach (ListViewItem li in listEpisodes.SelectedItems)
                {
                    if (li.Tag is MediaItem ep) itemsToProcess.Add(ep);
                }
            }
            else
            {
                var node = treeShows.SelectedNode;
                if (node == null) return;
                itemsToProcess = GetItemsFromNode(node);
            }

            if (itemsToProcess.Count == 0) return;

            var confirm = DarkMessageBox.Show(
                this,
                $"Are you sure you want to archive {itemsToProcess.Count} selected item(s)?\n\nEmpty parent folders will also be cleaned up safely.",
                "Confirm Archive",
                MessageBoxButtons.YesNo,
                darkMode
            );

            if (confirm != DialogResult.Yes) return;

            foreach (var ep in itemsToProcess) items.Remove(ep);

            listEpisodes.Items.Clear();
            picPoster.Image = null;

            this.Cursor = Cursors.WaitCursor;

            try
            {
                await Task.Run(() =>
                {
                    string archiveRoot = Path.Combine(RootFolder, "Archive");
                    Directory.CreateDirectory(archiveRoot);

                    var affectedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var ep in itemsToProcess)
                    {
                        string fullPath = BuildFullPath(ep.FilePath);
                        if (File.Exists(fullPath))
                        {
                            string dir = Path.GetDirectoryName(fullPath);
                            if (!string.IsNullOrEmpty(dir))
                            {
                                affectedDirectories.Add(dir);
                            }

                            string relPath = Path.GetRelativePath(RootFolder, fullPath);
                            string destPath = Path.Combine(archiveRoot, relPath);
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            File.Move(fullPath, destPath, true);
                        }
                    }

                    foreach (var dir in affectedDirectories)
                    {
                        CleanupEmptyDirectoriesUpwards(dir, RootFolder);
                    }

                    SaveAndMergeState();
                });

                // Trigger the full refresh button workflow
                btnRefresh_Click(sender, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(this, $"Archive failed: {ex.Message}", "Error", MessageBoxButtons.OK, darkMode);
                btnRefresh_Click(sender, EventArgs.Empty);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }


        private async void btnMarkWatched_Click(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                DarkMessageBox.Show(
                    this,
                    "Cannot modify watch status while media is currently playing. Please stop playback first.",
                    "Action Blocked",
                    MessageBoxButtons.OK,
                    darkMode
                );
                return;
            }

            if (listEpisodes.SelectedItems.Count == 0)
                return;

            string currentUser = AppConfig.CurrentUser;
            if (string.IsNullOrWhiteSpace(currentUser)) currentUser = "Default";

            foreach (ListViewItem li in listEpisodes.SelectedItems)
            {
                if (li.Tag is MediaItem ep)
                {
                    // Ensure dictionary is initialized
                    ep.WatchedByUser ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                    // Check current watched status
                    bool currentlyWatched = ep.IsWatched(currentUser);

                    // Explicitly toggle the watched state (true -> false, or false -> true)
                    ep.WatchedByUser[currentUser] = !currentlyWatched;

                    // Reset saved playback progress to 0 when toggling watched state
                    ep.SetSavedPosition(currentUser, 0);

                    // Also update the master items list reference directly
                    var masterItem = items.FirstOrDefault(i => string.Equals(i.FilePath, ep.FilePath, StringComparison.OrdinalIgnoreCase));
                    if (masterItem != null)
                    {
                        masterItem.WatchedByUser ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                        masterItem.WatchedByUser[currentUser] = !currentlyWatched;
                        masterItem.SetSavedPosition(currentUser, 0);
                    }

                    // Update UI list view row immediately
                    bool isPartial = ep.GetSavedPosition(currentUser) > 0 && !ep.IsWatched(currentUser);
                    li.SubItems[4].Text = GetWatchedDisplay(ep, isPartial);
                }
            }

            // Force an immediate synchronous write to bypass background merge delays
            await Task.Run(() =>
            {
                lock (_saveLock)
                {
                    try
                    {
                        MediaCache.Save(JsonPath, items);
                        MediaCache.SaveLocal(items);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to save mark state: {ex.Message}");
                    }
                }
            });

            // Instantly refresh tree structure and Resume button state
            BuildShowTree();
        }




        private void UpdatePlayButtonText(MediaItem selectedItem)
        {
            // 1. Check if item is null
            if (selectedItem == null)
            {
                btnPlay.Text = "Play";
                return;
            }

            // 2. Check if watched by current user or flagged FullyWatched
            bool isWatched = selectedItem.FullyWatched ||
                            (selectedItem.WatchedByUser != null &&
                             selectedItem.WatchedByUser.TryGetValue(AppConfig.CurrentUser, out bool watched) &&
                             watched);

            // 3. Check if there is actual saved playback progress
            bool hasSavedProgress = selectedItem.GetSavedPosition(AppConfig.CurrentUser) > 0;

            // 4. Set Continue only if watched or partially played
            btnPlay.Text = (isWatched || hasSavedProgress) ? "Continue" : "Play";
        }




        private async void btnPlay_Click(object sender, EventArgs e)
        {
            if (_isPlaying || listEpisodes.SelectedItems.Count == 0)
                return;

            _isPlaying = true;

            // Get selected items ordered by their position in the list
            var selectedListItems = listEpisodes.SelectedItems
                .Cast<ListViewItem>()
                .OrderBy(x => x.Index)
                .ToList();

            List<MediaItem> playbackQueue = new();

            if (selectedListItems.Count == 1)
            {
                // Ask user if they want to auto-play remaining episodes in the list
                DialogResult autoPlayResult = DarkMessageBox.Show(
                    this,
                    "Would you like to auto-play subsequent episodes in this list?",
                    "Auto Play",
                    MessageBoxButtons.YesNo,
                    darkMode // Passes true for dark, false for light
                );

                // If the user closed the window with X, stop playback execution
                if (autoPlayResult == DialogResult.Cancel)
                {
                    _isPlaying = false;
                    return;
                }


                if (autoPlayResult == DialogResult.Yes)
                {
                    // Auto-play enabled: Start from selected item and chain remaining items
                    int startIndex = selectedListItems[0].Index;
                    playbackQueue = listEpisodes.Items
                        .Cast<ListViewItem>()
                        .Skip(startIndex)
                        .Select(li => li.Tag as MediaItem)
                        .Where(m => m != null)
                        .ToList();
                }
                else
                {
                    // Auto-play disabled: Play only the single selected item
                    playbackQueue = selectedListItems
                        .Select(li => li.Tag as MediaItem)
                        .Where(m => m != null)
                        .ToList();
                }
            }
            else
            {
                // Multiple selection (e.g. 1 to 5): Play only the exact selected queue snapshot
                playbackQueue = selectedListItems
                    .Select(li => li.Tag as MediaItem)
                    .Where(m => m != null)
                    .ToList();
            }

            if (playbackQueue.Count == 0)
            {
                _isPlaying = false;
                return;
            }

            string originalText = btnPlay.Text;

            try
            {
                await PlayEpisodesQueueAsync(playbackQueue);
            }
            finally
            {
                btnPlay.Text = originalText;
                _isPlaying = false;
            }
        }



        private async Task PlayEpisodesQueueAsync(List<MediaItem> playlist)
        {
            if (playlist == null || playlist.Count == 0) return;

            _userStoppedPlayback = false;
            _playbackCts?.Cancel();
            _playbackCts = new CancellationTokenSource();

            SetPlaybackState(true);

            try
            {
                foreach (var ep in playlist)
                {
                    if (_userStoppedPlayback) break;
                    if (ep == null) continue;

                    _currentPlayingItem = ep; // Track globally for stop handler

                    // Update UI highlight safely if item is currently present in the list view
                    this.Invoke((MethodInvoker)delegate
                    {
                        foreach (ListViewItem item in listEpisodes.Items)
                        {
                            if (item.Tag is MediaItem m && string.Equals(m.FilePath, ep.FilePath, StringComparison.OrdinalIgnoreCase))
                            {
                                listEpisodes.SelectedItems.Clear();
                                item.Selected = true;
                                item.Focused = true;
                                listEpisodes.EnsureVisible(item.Index);
                                break;
                            }
                        }
                    });

                    string fullPath = BuildFullPath(ep.FilePath);
                    if (!File.Exists(fullPath)) continue;

                    string currentUser = AppConfig.CurrentUser;
                    if (string.IsNullOrWhiteSpace(currentUser)) currentUser = "Default";

                    double userSavedPos = ep.GetSavedPosition(currentUser);
                    _currentVlcTime = userSavedPos;

                    var startInfo = GetVlcStartInfo(fullPath, userSavedPos);
                    if (startInfo == null) break;

                    try
                    {
                        var sw = Stopwatch.StartNew();
                        currentPlayingProcess = Process.Start(startInfo);
                        if (currentPlayingProcess == null) continue;

                        bool finishedNaturally = false;

                        // Monitor VLC process and poll for exact playback position
                        while (!currentPlayingProcess.HasExited && !_userStoppedPlayback)
                        {
                            try
                            {
                                await Task.Delay(200, _playbackCts.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                break; // Exit loop instantly when token is canceled!
                            }

                            double exactTime = await GetVlcCurrentTimeSecondsAsync();
                            if (exactTime >= 0)
                            {
                                _currentVlcTime = exactTime;
                            }
                        }

                        sw.Stop();

                        if (_userStoppedPlayback)
                        {
                            break; // Handled by btnStop_Click logic
                        }

                        // Check how VLC exited
                        if (currentPlayingProcess != null && currentPlayingProcess.HasExited)
                        {
                            // 1. If we know the duration, check if it reached near the end OR if it played for most of the video track
                            if (ep.Duration > TimeSpan.Zero)
                            {
                                // If it got within 15 seconds of the end, or played past 85% of its duration before closing
                                if (_currentVlcTime >= (ep.Duration.TotalSeconds - 15) || sw.Elapsed.TotalSeconds >= (ep.Duration.TotalSeconds * 0.85))
                                {
                                    finishedNaturally = true;
                                }
                            }

                            // 2. If duration wasn't cached/known, check elapsed playback time vs reasonable threshold
                            if (!finishedNaturally && ep.Duration == TimeSpan.Zero)
                            {
                                // If it played for more than 20 seconds and closed on its own (not instant abort)
                                if (sw.Elapsed.TotalSeconds > 20)
                                {
                                    finishedNaturally = true;
                                }
                            }

                            // 3. Fallback for short clips or edge cases where VLC exited cleanly via --play-and-exit
                            // If the user didn't hit stop, and it ran for a decent amount of time relative to the last known position
                            if (!finishedNaturally && sw.Elapsed.TotalSeconds > 10 && _currentVlcTime > userSavedPos)
                            {
                                // If it got very close to the end based on local tracking
                                if (ep.Duration > TimeSpan.Zero && _currentVlcTime >= (ep.Duration.TotalSeconds * 0.8))
                                {
                                    finishedNaturally = true;
                                }
                            }
                        }

                        if (finishedNaturally)
                        {
                            // Episode finished completely: reset position and mark watched
                            ep.SetSavedPosition(currentUser, 0);
                            ep.MarkWatched(currentUser);

                            var masterItem = items.FirstOrDefault(i => string.Equals(i.FilePath, ep.FilePath, StringComparison.OrdinalIgnoreCase));
                            if (masterItem != null)
                            {
                                masterItem.SetSavedPosition(currentUser, 0);
                                masterItem.MarkWatched(currentUser);
                            }

                            SaveAndMergeState();

                            // Force UI and Title Bar to refresh on the UI thread
                            this.Invoke((MethodInvoker)delegate
                            {
                                // 1. Refresh Checkmark in list view
                                foreach (ListViewItem item in listEpisodes.Items)
                                {
                                    if (item.Tag is MediaItem m && string.Equals(m.FilePath, ep.FilePath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        item.SubItems[4].Text = GetWatchedDisplay(ep);
                                        listEpisodes.Refresh();
                                        break;
                                    }
                                }

                                // 2. Recalculate remaining time for active season
                                if (currentEpisodes != null && currentEpisodes.Count > 0)
                                {
                                    TimeSpan newTotal = currentEpisodes.Aggregate(TimeSpan.Zero, (acc, x) => acc + x.Duration);
                                    TimeSpan newRemaining = currentEpisodes
                                        .Where(x => !x.IsWatched(AppConfig.CurrentUser))
                                        .Aggregate(TimeSpan.Zero, (acc, x) => acc + x.Duration);

                                    UpdateWatchTimeSummary(newTotal, newRemaining);
                                }
                            });

                            // Trigger 0: After current episode finishes
                            if (_isPowerActionActive && _activePowerTrigger == 0)
                            {
                                await ExecutePowerActionAsync();
                                break; // Stop playing remaining episodes in the list
                            }
                        }
                        else
                        {
                            // User closed VLC manually (clicked X) before the video finished:
                            ep.SetSavedPosition(currentUser, _currentVlcTime);

                            var masterItem = items.FirstOrDefault(i => string.Equals(i.FilePath, ep.FilePath, StringComparison.OrdinalIgnoreCase));
                            if (masterItem != null)
                            {
                                masterItem.SetSavedPosition(currentUser, _currentVlcTime);
                            }

                            SaveAndMergeState();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!_userStoppedPlayback)
                        {
                            MessageBox.Show($"Playback error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        break;
                    }
                    finally
                    {
                        currentPlayingProcess?.Dispose();
                        currentPlayingProcess = null;
                    }
                }

                // Trigger 1: After complete playback queue finishes naturally
                if (_isPowerActionActive && _activePowerTrigger == 1 && !_userStoppedPlayback)
                {
                    await ExecutePowerActionAsync();
                }
            }
            finally
            {
                _isPlaying = false;
                SetPlaybackState(false);
                _userStoppedPlayback = false;
                _currentPlayingItem = null;

                // Refresh tree and restore selected season view cleanly
                this.Invoke((MethodInvoker)delegate
                {
                    BuildShowTree();

                    if (treeShows.SelectedNode?.Tag is List<MediaItem> activeSeasonItems)
                    {
                        currentEpisodes = activeSeasonItems;
                        LoadEpisodes(currentEpisodes);
                    }
                });
            }
        }



        private void SetPlaybackState(bool isPlaying)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)(() => SetPlaybackState(isPlaying)));
                return;
            }

            btnPlay.Enabled = !isPlaying;
            btnStop.Enabled = isPlaying;

            if (darkMode)
            {
                // Dark Mode Play button styling (Green accent)
                if (btnPlay.Enabled)
                {
                    btnPlay.BackColor = Color.FromArgb(34, 139, 34); // Forest Green
                    btnPlay.ForeColor = Color.White;
                }
                else
                {
                    btnPlay.BackColor = Color.FromArgb(20, 50, 25); // Muted Dark Green
                    btnPlay.ForeColor = Color.FromArgb(90, 130, 90); // Muted text
                }

                // Dark Mode Stop button styling (Red accent)
                if (btnStop.Enabled)
                {
                    btnStop.BackColor = Color.FromArgb(170, 40, 40); // Vibrant Red
                    btnStop.ForeColor = Color.White;
                }
                else
                {
                    btnStop.BackColor = Color.FromArgb(40, 25, 25); // Muted Dark Red
                    btnStop.ForeColor = Color.FromArgb(110, 80, 80);
                }
            }
            else
            {
                // Light Mode Play button styling (Green accent)
                if (btnPlay.Enabled)
                {
                    btnPlay.BackColor = Color.FromArgb(40, 167, 69); // Bright Green
                    btnPlay.ForeColor = Color.White;
                }
                else
                {
                    btnPlay.BackColor = Color.FromArgb(200, 230, 205); // Light Muted Green
                    btnPlay.ForeColor = Color.DarkGray;
                }

                // Light Mode Stop button styling (Red accent)
                if (btnStop.Enabled)
                {
                    btnStop.BackColor = Color.Crimson;
                    btnStop.ForeColor = Color.White;
                }
                else
                {
                    btnStop.BackColor = Color.FromArgb(230, 230, 230);
                    btnStop.ForeColor = Color.Gray;
                }
            }
        }






        private bool _isProcessingStop = false;
        private MediaItem _currentPlayingItem = null;
        private double _currentVlcTime = 0;

        private async void btnStop_Click(object sender, EventArgs e)
        {
            if (_isProcessingStop) return;
            _isProcessingStop = true;

            // 1. Signal cancellation instantly
            _userStoppedPlayback = true;
            _playbackCts?.Cancel();

            // Grab the currently playing item from our active tracker or list selection
            MediaItem targetEp = _currentPlayingItem;
            if (targetEp == null && listEpisodes.SelectedItems.Count > 0 &&
                listEpisodes.SelectedItems[0].Tag is MediaItem selectedMedia)
            {
                targetEp = selectedMedia;
            }

            // Check if stopped within 30 seconds of the video end
            if (targetEp != null && targetEp.Duration > TimeSpan.Zero && _currentVlcTime >= (targetEp.Duration.TotalSeconds - 30))
            {
                string currentUser = AppConfig.CurrentUser;
                if (string.IsNullOrWhiteSpace(currentUser)) currentUser = "Default";

                targetEp.MarkWatched(currentUser);
                targetEp.SetSavedPosition(currentUser, 0);

                var masterItem = items.FirstOrDefault(i => string.Equals(i.FilePath, targetEp.FilePath, StringComparison.OrdinalIgnoreCase));
                if (masterItem != null)
                {
                    masterItem.MarkWatched(currentUser);
                    masterItem.SetSavedPosition(currentUser, 0);
                }
            }
            else if (targetEp != null)
            {
                string currentUser = AppConfig.CurrentUser;
                if (string.IsNullOrWhiteSpace(currentUser)) currentUser = "Default";

                targetEp.SetSavedPosition(currentUser, _currentVlcTime);

                var masterItem = items.FirstOrDefault(i => string.Equals(i.FilePath, targetEp.FilePath, StringComparison.OrdinalIgnoreCase));
                if (masterItem != null)
                {
                    masterItem.SetSavedPosition(currentUser, _currentVlcTime);
                }
            }

            SetPlaybackState(false);
            _isPlaying = false;

            var procToKill = currentPlayingProcess;
            currentPlayingProcess = null;
            _currentPlayingItem = null;
            _currentVlcTime = 0;

            // 2. Offload process killing and WaitForExit to a background thread
            await Task.Run(() =>
            {
                if (procToKill != null)
                {
                    try
                    {
                        if (!procToKill.HasExited)
                        {
                            procToKill.Kill(entireProcessTree: true);
                            procToKill.WaitForExit(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to kill tracked VLC process: {ex.Message}");
                    }
                }

                // Fallback: Force kill ANY vlc process running if still open
                try
                {
                    foreach (var proc in Process.GetProcessesByName("vlc"))
                    {
                        try
                        {
                            if (!proc.HasExited)
                            {
                                proc.Kill(entireProcessTree: true);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            });

            // 3. Save state in background and refresh tree safely preserving selection view
            _backgroundSaveTask = Task.Run(() =>
            {
                lock (_saveLock)
                {
                    SaveAndMergeState();
                }
            }).ContinueWith(_ =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _isProcessingStop = false;

                        string previouslySelectedShow = treeShows.SelectedNode?.Text;
                        BuildShowTree(); // Refresh UI to reflect watched status

                        // Restore selection so it doesn't go blank
                        if (!string.IsNullOrEmpty(previouslySelectedShow))
                        {
                            foreach (TreeNode node in treeShows.Nodes)
                            {
                                if (node.Text.Equals(previouslySelectedShow, StringComparison.OrdinalIgnoreCase))
                                {
                                    treeShows.SelectedNode = node;
                                    break;
                                }
                            }
                        }
                    }));
                }
                else
                {
                    _isProcessingStop = false;
                }
            });
        }






        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                lock (_saveLock)
                {
                    // Check if the hashed local cache file exists on the C: drive
                    if (!File.Exists(MediaCache.LocalCachePath))
                    {
                        // If it doesn't exist
                        if (File.Exists(JsonPath))
                        {
                            CreateJsonBackup();
                        }
                        MediaCache.Save(JsonPath, items);
                        MediaCache.SaveLocal(items);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during startup check: {ex.Message}");
            }

            // Automatically trigger a full refresh scan on startup after the form renders
            this.BeginInvoke((MethodInvoker)(() =>
            {
                btnRefresh_Click(sender, EventArgs.Empty);
            }));
        }




        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 1. Load the absolute latest settings from disk first so we don't 
            // overwrite font changes made in OptionsForm, then update the splitters.
            var latestSettings = AppConfig.Load() ?? new UserSettings();
            latestSettings.SplitMainDistance = splitMain.SplitterDistance;
            latestSettings.SplitContainer1Distance = splitContainer1.SplitterDistance;

            AppConfig.Save(latestSettings);
            _userSettings = latestSettings; // Keep local reference synced

            // 2. Check if VLC process is currently running/playing
            bool isPlaying = currentPlayingProcess != null && !currentPlayingProcess.HasExited;

            if (isPlaying)
            {
                DialogResult autoPlayResult = DarkMessageBox.Show(
                    this,
                    "Please click the Stop button to save your playback progress before closing the application.",
                    "Playback Active",
                    "Use Stop button to save progress",
                    MessageBoxButtons.OK,
                    darkMode // Passes true for dark, false for light
                );

                // Cancel the close operation and keep the form open
                e.Cancel = true;
                return;
            }

            // 3. Standard exit save when media is already stopped
            try
            {
                if (_backgroundSaveTask != null && !_backgroundSaveTask.IsCompleted)
                {
                    _backgroundSaveTask.Wait(1000);
                }

                lock (_saveLock)
                {
                    if (File.Exists(JsonPath))
                    {
                        CreateJsonBackup();
                    }
                    MediaCache.Save(JsonPath, items);
                    MediaCache.SaveLocal(items);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during exit save: {ex.Message}");
            }


            _autoSaveTimer?.Dispose();

        }

        private async void btnOrganiz_Click(object sender, EventArgs e)
        {
            // Execute worker logic on a background thread
            var result = await Task.Run(() => FileOrganizer.OrganizeLooseFiles(RootFolder, VideoExtensions));

            // Update Form UI Labels
            _lastMovedLog = result.MovedLog;
            lblStatus.Text = result.StatusText;
            lblStatus.Cursor = result.StatusCursor;

            //full scan and UI rebuild so Auto-Rename can pick up the new paths immediately
            RefreshAll();
        }

        private void lblStatus_Click(object sender, EventArgs e)
        {
            if (_lastMovedLog.Count > 0)
            {
                string details = string.Join("\n", _lastMovedLog);
                MessageBox.Show(
                    $"{_lastMovedLog.Count} file(s) successfully organized:\n\n{details}",
                    "Organization Summary",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }




        private bool CheckNodesForToFinish(TreeNode node)
        {
            if (node == null) return false;

            // Check single MediaItem tag
            if (node.Tag is MediaItem ep && ep.GetSavedPosition(AppConfig.CurrentUser) > 0 && !ep.IsWatched(AppConfig.CurrentUser))
            {
                return true;
            }

            // Check List<MediaItem> tag
            if (node.Tag is List<MediaItem> mediaList &&
                mediaList.Any(m => m.GetSavedPosition(AppConfig.CurrentUser) > 0 && !m.IsWatched(AppConfig.CurrentUser)))
            {
                return true;
            }

            // Recursively check all child nodes
            foreach (TreeNode child in node.Nodes)
            {
                if (CheckNodesForToFinish(child)) return true;
            }

            return false;
        }

        private void UpdateResumeButtonVisibility()
        {
            string user = AppConfig.CurrentUser;

            // 1. Check tree nodes
            bool hasToFinish = treeShows.Nodes.Cast<TreeNode>().Any(CheckNodesForToFinish);

            // 2. Fallback check on master items list in case tree tags are unpopulated
            if (!hasToFinish && items != null)
            {
                hasToFinish = items.Any(m => m.GetSavedPosition(user) > 0 && !m.IsWatched(user));
            }

            flowLayoutPanel3.SuspendLayout();

            btnResume.Visible = hasToFinish;

            if (hasToFinish)
            {
                btnResume.FlatStyle = FlatStyle.Flat;
                btnResume.FlatAppearance.BorderSize = 2;
                btnResume.FlatAppearance.BorderColor = Color.White;
                btnResume.BackColor = Color.FromArgb(0, 122, 204);
                btnResume.ForeColor = Color.White;
                btnResume.Font = new Font(btnResume.Font, FontStyle.Bold);

                // Position index 3 places Resume right between Mark (index 2) and Play (index 4)
                flowLayoutPanel3.Controls.SetChildIndex(btnResume, 3);
            }

            flowLayoutPanel3.ResumeLayout(true);
            flowLayoutPanel3.PerformLayout();
        }


        private void btnResume_Click(object sender, EventArgs e)
        {
            if (_isPlaying) return;

            // 1. Gather all in-progress nodes across the entire tree
            List<TreeNode> pendingNodes = new List<TreeNode>();
            FindAllToFinishNodes(treeShows.Nodes, pendingNodes);

            if (pendingNodes.Count == 0) return;

            TreeNode targetNode = pendingNodes[0];

            // 2. If multiple shows/seasons have saved progress, let the user pick which one to resume
            if (pendingNodes.Count > 1)
            {
                targetNode = ShowResumeSelectionDialog(pendingNodes);
                if (targetNode == null) return; // User canceled the dialog
            }

            // 3. Select and reveal the target node in treeShows
            treeShows.SelectedNode = targetNode;
            targetNode.EnsureVisible();
            treeShows.Focus();

            // 4. Prompt for auto-play preference
            DialogResult autoPlayResult = DarkMessageBox.Show(
                this,
                "Would you like to auto-play subsequent episodes in this list?",
                "Auto Play",
                MessageBoxButtons.YesNo,
                darkMode
            );

            if (autoPlayResult == DialogResult.Cancel) return;

            _isPlaying = true;
            bool autoPlay = (autoPlayResult == DialogResult.Yes);

            this.BeginInvoke((MethodInvoker)delegate
            {
                PlayFirstUnwatchedEpisode(autoPlay);
            });
        }



        private void FindAllToFinishNodes(TreeNodeCollection nodes, List<TreeNode> resultList)
        {
            string currentUser = AppConfig.CurrentUser;

            foreach (TreeNode node in nodes)
            {
                bool hasPendingItem = false;

                if (node.Tag is List<MediaItem> mediaList)
                {
                    hasPendingItem = mediaList.Any(m => m.GetSavedPosition(currentUser) > 0 && !m.IsWatched(currentUser));
                }
                else if (node.Tag is MediaItem item)
                {
                    hasPendingItem = item.GetSavedPosition(currentUser) > 0 && !item.IsWatched(currentUser);
                }

                if (hasPendingItem)
                {
                    // Get the clean show title to ensure we don't add the same show twice (e.g. once in Continue, once in Main tree)
                    string showTitle = node.Parent != null ? node.Parent.Text : node.Text;
                    showTitle = showTitle.Replace("★ ", "").Trim();

                    bool alreadyExists = resultList.Any(existing =>
                    {
                        string existingTitle = existing.Parent != null ? existing.Parent.Text : existing.Text;
                        return existingTitle.Replace("★ ", "").Trim().Equals(showTitle, StringComparison.OrdinalIgnoreCase);
                    });

                    if (!alreadyExists)
                    {
                        resultList.Add(node);
                    }
                }

                // Recursively check child nodes
                FindAllToFinishNodes(node.Nodes, resultList);
            }
        }



        private TreeNode ShowResumeSelectionDialog(List<TreeNode> choices)
        {
            string currentUser = AppConfig.CurrentUser;

            using (Form selectForm = new Form())
            {
                selectForm.Text = "Select Show to Resume";
                selectForm.Size = new Size(480, 320);
                selectForm.StartPosition = FormStartPosition.CenterParent;
                selectForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                selectForm.MaximizeBox = false;
                selectForm.MinimizeBox = false;

                // Apply theme styling
                Color backColor = darkMode ? Color.FromArgb(35, 35, 35) : SystemColors.Control;
                Color textColor = darkMode ? Color.White : Color.Black;
                Color controlBack = darkMode ? Color.FromArgb(45, 45, 45) : Color.White;

                selectForm.BackColor = backColor;
                selectForm.ForeColor = textColor;

                Label lbl = new Label
                {
                    Text = "Multiple shows have saved progress. Choose which one to continue:",
                    Dock = DockStyle.Top,
                    Padding = new Padding(12, 12, 12, 6),
                    AutoSize = true
                };

                ListBox lstChoices = new ListBox
                {
                    Dock = DockStyle.Fill,
                    BackColor = controlBack,
                    ForeColor = textColor,
                    Font = new Font("Segoe UI", 9.75f, FontStyle.Regular),
                    IntegralHeight = false
                };

                // Populate options with Show Name / Season / Episode details
                Dictionary<int, TreeNode> choiceMapping = new Dictionary<int, TreeNode>();

                for (int i = 0; i < choices.Count; i++)
                {
                    TreeNode node = choices[i];
                    string labelText = node.Text.Replace("★ ", "").Trim();

                    // Build hierarchical display name (e.g. "Stargate Atlantis -> Season 5")
                    if (node.Parent != null)
                    {
                        labelText = $"{node.Parent.Text.Replace("★ ", "").Trim()} — {labelText}";
                    }

                    // Get first pending episode details
                    MediaItem pendingItem = null;
                    if (node.Tag is List<MediaItem> mediaList)
                    {
                        pendingItem = mediaList.FirstOrDefault(m => m.GetSavedPosition(currentUser) > 0 && !m.IsWatched(currentUser));
                    }
                    else if (node.Tag is MediaItem item)
                    {
                        pendingItem = item;
                    }

                    if (pendingItem != null)
                    {
                        TimeSpan savedTime = TimeSpan.FromSeconds(pendingItem.GetSavedPosition(currentUser));
                        string timeDisplay = savedTime.Hours > 0 ? savedTime.ToString(@"h\:mm\:ss") : savedTime.ToString(@"mm\:ss");
                        labelText += $" (Resume at {timeDisplay} - S{pendingItem.CachedSeason:D2}E{pendingItem.CachedEpisode:D2})";
                    }

                    int addedIdx = lstChoices.Items.Add(labelText);
                    choiceMapping[addedIdx] = node;
                }

                lstChoices.SelectedIndex = 0; // Default selection

                Panel btnPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 45,
                    Padding = new Padding(8)
                };

                Button btnOk = new Button
                {
                    Text = "Resume Selected",
                    DialogResult = DialogResult.OK,
                    Width = 130,
                    Height = 28,
                    Dock = DockStyle.Right,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White
                };

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Width = 80,
                    Height = 28,
                    Dock = DockStyle.Right,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = darkMode ? Color.FromArgb(60, 64, 67) : SystemColors.Control,
                    ForeColor = textColor
                };

                // Double-clicking an item in the list immediately resumes it
                lstChoices.DoubleClick += (s, e) =>
                {
                    if (lstChoices.SelectedIndex >= 0)
                    {
                        selectForm.DialogResult = DialogResult.OK;
                        selectForm.Close();
                    }
                };

                btnPanel.Controls.Add(btnOk);
                btnPanel.Controls.Add(btnCancel);

                selectForm.Controls.Add(lstChoices);
                selectForm.Controls.Add(lbl);
                selectForm.Controls.Add(btnPanel);

                if (selectForm.ShowDialog(this) == DialogResult.OK && lstChoices.SelectedIndex >= 0)
                {
                    return choiceMapping[lstChoices.SelectedIndex];
                }

                return null;
            }
        }


        private void PlayFirstUnwatchedEpisode(bool autoPlay = true)
        {
            if (listEpisodes.Items.Count == 0) return;

            // Locate the first in-progress episode in the list
            var firstItem = listEpisodes.Items
                .Cast<ListViewItem>()
                .FirstOrDefault(li => li.Tag is MediaItem m &&
                                      m.GetSavedPosition(AppConfig.CurrentUser) > 0 &&
                                      !m.IsWatched(AppConfig.CurrentUser));

            if (firstItem == null || firstItem.Tag is not MediaItem firstMedia) return;

            // Highlight and focus the item in the ListView
            listEpisodes.Focus();
            listEpisodes.SelectedItems.Clear();
            firstItem.Selected = true;
            firstItem.Focused = true;
            listEpisodes.EnsureVisible(firstItem.Index);

            List<MediaItem> playbackQueue;

            if (autoPlay)
            {
                // Enqueue from the in-progress episode through the end of the season list
                playbackQueue = listEpisodes.Items
                    .Cast<ListViewItem>()
                    .Skip(firstItem.Index)
                    .Select(li => li.Tag as MediaItem)
                    .Where(m => m != null)
                    .ToList();
            }
            else
            {
                // Enqueue only the single target episode
                playbackQueue = new List<MediaItem> { firstMedia };
            }

            _ = PlayEpisodesQueueAsync(playbackQueue);
        }

       

        private void btnResume_MouseDown(object sender, MouseEventArgs e)
        {
            // Darken to Dark Blue while pressed down
            btnResume.BackColor = Color.FromArgb(0, 80, 150);
        }

        private void btnResume_MouseUp(object sender, MouseEventArgs e)
        {
            // Return to standard Accent Blue upon release
            btnResume.BackColor = Color.FromArgb(0, 122, 204);
        }

        private void btnoptions_Click(object sender, EventArgs e)
        {
            using (var optionsForm = new OptionsForm(_videoSettings, darkMode))
            {
                optionsForm.OnDarkModeChanged = (newDarkModeState) =>
                {
                    darkMode = newDarkModeState;
                    ApplyDarkMode(darkMode);
                };

                // Connect the font callback to update MainForm live
                optionsForm.OnFontSettingsChanged = (newFontSize, newFontFamily) =>
                {
                    FontHelper.ApplyFontToForm(this, newFontSize, newFontFamily);
                };

                optionsForm.ShowDialog(this);
            }
        }



        // Store selected options in main Form state
        private int _activePowerAction = 0;   // 0: Shutdown, 1: Sleep, 2: Hibernate, 3: Exit App
        private int _activePowerTrigger = 0;  // 0: Current video, 1: End of queue
        private bool _usePowerCountdown = true;
        private bool _isPowerActionActive = false;

        private void btnShutdownOptions_Click(object sender, EventArgs e)
        {
            // Pass darkMode as the 4th argument
            using (var form = new ShutdownForm(_activePowerAction, _activePowerTrigger, _usePowerCountdown, darkMode))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _activePowerAction = form.SelectedActionIndex;
                    _activePowerTrigger = form.SelectedTriggerIndex;
                    _usePowerCountdown = form.EnableCountdown;

                    // Only mark active if an action other than "Disabled / Do Nothing" (Index 0) was chosen
                    _isPowerActionActive = _activePowerAction > 0;

                    if (_isPowerActionActive)
                    {
                        btnShutdownOptions.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
                        btnShutdownOptions.ForeColor = System.Drawing.Color.White;
                    }
                    else
                    {
                        // Reset button back to standard theme colors when set to "Disabled / Do Nothing"
                        btnShutdownOptions.BackColor = darkMode ? System.Drawing.Color.FromArgb(45, 45, 48) : System.Drawing.SystemColors.Control;
                        btnShutdownOptions.ForeColor = darkMode ? System.Drawing.Color.White : System.Drawing.Color.Black;
                    }
                }
            }
        }


        private async Task ExecutePowerActionAsync()
        {
            if (!_isPowerActionActive) return;

            bool proceedWithAction = true;

            if (_usePowerCountdown)
            {
                DialogResult result = DarkMessageBox.ShowCountdown(
                    this,
                    "Power action will execute in {0} seconds.\n\nDo you want to proceed now?",
                    "Power Action Warning",
                    30,
                    darkMode
                );

                proceedWithAction = (result == DialogResult.Yes);
            }

            if (proceedWithAction)
            {
                try
                {
                    switch (_activePowerAction)
                    {
                        case 0: // Disabled / Do Nothing
                            break;

                        case 1: // Shutdown PC (Matches Index 1 in cmbAction)
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "shutdown.exe",
                                Arguments = "/s /f /t 0 /c \"Media Status: Shutting down...\"",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            break;

                        case 2: // Sleep (Matches Index 2 in cmbAction)
                            Application.SetSuspendState(PowerState.Suspend, true, true);
                            break;

                        case 3: // Hibernate (Matches Index 3 in cmbAction)
                            Application.SetSuspendState(PowerState.Hibernate, true, true);
                            break;

                        case 4: // Exit Application (Matches Index 4 in cmbAction)
                            Application.Exit();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DarkMessageBox.Show(this, $"Failed to execute power action: {ex.Message}", "Error", MessageBoxButtons.OK, darkMode);
                }
            }

            _isPowerActionActive = false;
            btnShutdownOptions.BackColor = SystemColors.Control;
        }

        private void chkSubtitles_CheckedChanged(object sender, EventArgs e)
        {
            _userSettings.EnableSubtitles = chkSubtitles.Checked;
            AppConfig.Save(_userSettings);
        }

        private void BtnCheckIMDb_Click(object sender, EventArgs e)
        {
            string targetToClean = null;

            if (treeShows != null && treeShows.SelectedNode != null)
            {
                TreeNode node = treeShows.SelectedNode;

                if (node.Tag is List<MediaItem> items && items.Count > 0)
                {
                    // Use the CachedSeriesName if available, or pass the full file path
                    targetToClean = !string.IsNullOrWhiteSpace(items[0].CachedSeriesName)
                        ? items[0].CachedSeriesName
                        : BuildFullPath(items[0].FilePath);
                }
                else if (node.Tag is MediaItem item)
                {
                    targetToClean = !string.IsNullOrWhiteSpace(item.CachedSeriesName)
                        ? item.CachedSeriesName
                        : BuildFullPath(item.FilePath);
                }
                else if (!string.IsNullOrWhiteSpace(node.Text))
                {
                    targetToClean = node.Text;
                }
            }

            if (string.IsNullOrWhiteSpace(targetToClean))
            {
                DarkMessageBox.Show(this, "Please select a media item or node first.", "No Selection", MessageBoxButtons.OK, darkMode);
                return;
            }

            string cleanTitle = CleanMediaTitle(targetToClean);

            if (string.IsNullOrWhiteSpace(cleanTitle))
            {
                DarkMessageBox.Show(this, "Could not determine a valid title from the selection.", "Parsing Error", MessageBoxButtons.OK, darkMode);
                return;
            }

            string searchUrl = $"https://www.imdb.com/find/?q={Uri.EscapeDataString(cleanTitle)}";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = searchUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(this, $"Failed to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, darkMode);
            }
        }



        private void BtnCheckTVMAZE_Click(object sender, EventArgs e)
        {
            string targetToClean = null;

            if (treeShows != null && treeShows.SelectedNode != null)
            {
                TreeNode node = treeShows.SelectedNode;

                if (node.Tag is List<MediaItem> items && items.Count > 0)
                {
                    // Use the CachedSeriesName if available, or pass the full file path
                    targetToClean = !string.IsNullOrWhiteSpace(items[0].CachedSeriesName)
                        ? items[0].CachedSeriesName
                        : BuildFullPath(items[0].FilePath);
                }
                else if (node.Tag is MediaItem item)
                {
                    targetToClean = !string.IsNullOrWhiteSpace(item.CachedSeriesName)
                        ? item.CachedSeriesName
                        : BuildFullPath(item.FilePath);
                }
                else if (!string.IsNullOrWhiteSpace(node.Text))
                {
                    targetToClean = node.Text;
                }
            }

            if (string.IsNullOrWhiteSpace(targetToClean))
            {
                DarkMessageBox.Show(this, "Please select a media item or node first.", "No Selection", MessageBoxButtons.OK, darkMode);
                return;
            }

            string cleanTitle = CleanMediaTitle(targetToClean);

            if (string.IsNullOrWhiteSpace(cleanTitle))
            {
                DarkMessageBox.Show(this, "Could not determine a valid title from the selection.", "Parsing Error", MessageBoxButtons.OK, darkMode);
                return;
            }

            // TVMaze web search query URL
            string searchUrl = $"https://www.tvmaze.com/search?q={Uri.EscapeDataString(cleanTitle)}";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = searchUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(this, $"Failed to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, darkMode);
            }
        }





        private string CleanMediaTitle(string inputPathOrName)
        {
            if (string.IsNullOrWhiteSpace(inputPathOrName)) return string.Empty;

            string targetName = inputPathOrName;

            // 1. Extract top-level show folder name if a full path is provided
            if (Path.IsPathRooted(inputPathOrName) || inputPathOrName.Contains(Path.DirectorySeparatorChar))
            {
                string dirPath = Path.GetDirectoryName(inputPathOrName);
                if (!string.IsNullOrEmpty(dirPath))
                {
                    var dirInfo = new DirectoryInfo(dirPath);

                    // Navigate up one level if inside a Season subfolder
                    if (dirInfo.Name.StartsWith("Season", StringComparison.OrdinalIgnoreCase) ||
                        Regex.IsMatch(dirInfo.Name, @"(?i)^S\d+$"))
                    {
                        targetName = dirInfo.Parent?.Name ?? dirInfo.Name;
                    }
                    else
                    {
                        targetName = dirInfo.Name;
                    }
                }
            }
            else
            {
                // 2. Strip extension if given a single filename
                targetName = Path.GetFileNameWithoutExtension(inputPathOrName);
            }

            // Strip favorited star symbols and delimiters
            string cleaned = targetName.Replace("★", string.Empty)
                                       .Replace("⭐", string.Empty)
                                       .Replace('.', ' ')
                                       .Replace('_', ' ')
                                       .Replace('-', ' ');

            // Strip quality, encoding, and release tags
            string pattern = @"(?i)\b(1080p|720p|4k|2160p|h264|x264|h265|x265|hevc|web-dl|webrip|bluray|hdtv|xvid|aac|dts)\b";
            cleaned = Regex.Replace(cleaned, pattern, string.Empty);

            // Clean up trailing brackets and whitespace
            cleaned = Regex.Replace(cleaned, @"[\(\[].*?[\)\]]", string.Empty);
            return Regex.Replace(cleaned, @"\s+", " ").Trim();
        }





        /// <summary>
        /// Fallback local renamer: formats files into clean SXXEXX ordering without external API calls.
        /// </summary>
        public static int RenameToSequentialOrder(List<MediaItem> episodes, string showName, Func<string, string> buildFullPathFunc)
        {
            if (episodes == null || episodes.Count == 0 || string.IsNullOrWhiteSpace(showName))
                return 0;

            int renamedCount = 0;

            foreach (var ep in episodes)
            {
                int season = ep.CachedSeason;
                int episode = ep.CachedEpisode;

                string oldFullPath = buildFullPathFunc(ep.FilePath);
                if (!File.Exists(oldFullPath)) continue;

                string folder = Path.GetDirectoryName(oldFullPath);
                string ext = Path.GetExtension(oldFullPath);

                // Format into standard clean pattern: "Show Name - S01E01.ext"
                string newFileName = $"{showName} - S{season:D2}E{episode:D2}{ext}";
                string newFullPath = Path.Combine(folder, newFileName);

                if (string.Equals(oldFullPath, newFullPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    if (!File.Exists(newFullPath))
                    {
                        File.Move(oldFullPath, newFullPath);
                        ep.FilePath = newFullPath;
                        ep.CacheParsed();
                        renamedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Sequential rename failed: {ex.Message}");
                }
            }

            return renamedCount;
        }



        private void TransferMediaItemState(MediaItem item, string oldPath, string newPath)
        {
            if (item == null || string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                return;

            string oldFileName = Path.GetFileName(oldPath);
            string newFileName = Path.GetFileName(newPath);

            // 1. Remap in-memory dictionaries (WatchedStates, PlaybackPositions, etc.)
            var fields = this.GetType().GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.GetValue(this) is System.Collections.IDictionary dict)
                {
                    if (dict.Contains(oldPath))
                    {
                        var val = dict[oldPath];
                        dict.Remove(oldPath);
                        dict[newPath] = val;
                    }
                    if (dict.Contains(oldFileName))
                    {
                        var val = dict[oldFileName];
                        dict.Remove(oldFileName);
                        dict[newFileName] = val;
                    }
                }
            }

            // 2. Update MediaItem properties directly
            item.FilePath = newPath;
            item.CacheParsed();

            // 3. Directly patch media_state.json on disk
            UpdateMediaStateJsonFile(oldPath, newPath, oldFileName, newFileName);
        }

        /// <summary>
        /// Directly updates keys in media_state.json so watched state, progress, and favorites persist across renames.
        /// </summary>
        private void UpdateMediaStateJsonFile(string oldPath, string newPath, string oldFileName, string newFileName)
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media_state.json");
                if (!File.Exists(jsonPath)) return;

                string jsonContent = File.ReadAllText(jsonPath);
                if (string.IsNullOrWhiteSpace(jsonContent)) return;

                using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                // Replace old path/filename strings with new path/filename strings across the entire state structure
                string updatedJson = jsonContent
                    .Replace(System.Text.Json.JsonSerializer.Serialize(oldPath).Trim('"'), System.Text.Json.JsonSerializer.Serialize(newPath).Trim('"'))
                    .Replace(System.Text.Json.JsonSerializer.Serialize(oldFileName).Trim('"'), System.Text.Json.JsonSerializer.Serialize(newFileName).Trim('"'));

                File.WriteAllText(jsonPath, updatedJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update media_state.json: {ex.Message}");
            }
        }




        private async void BtnAutoRename_Click(object sender, EventArgs e)
        {
            await EpisodeRenamer.ExecuteAutoRenameAsync(
                this,
                listEpisodes,
                treeShows,
                darkMode,
                node => GetItemsFromNode(node),
                path => BuildFullPath(path),
                (item, oldPath, newPath) => TransferMediaItemState(item, oldPath, newPath),
                async () => await SaveAndMergeState(),
                () => RefreshAll(),
                () => { if (treeShows.SelectedNode != null) TreeShows_AfterSelect(treeShows, new TreeViewEventArgs(treeShows.SelectedNode)); }
            );
        }



        private void btnAutoRenameOptions_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new AutoRenameSettingsForm(darkMode))
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    // Persist values to Properties.Settings
                    Settings.Default.RenameFormatPattern = settingsForm.CustomFormatPattern;
                    Settings.Default.RenameIncludeShow = settingsForm.IncludeShow;
                    Settings.Default.RenamePadNumbers = settingsForm.PadNumbers;
                    Settings.Default.RenameReplaceWithSpaces = settingsForm.ReplaceWithSpaces;
                    Settings.Default.RenameReplaceDots = settingsForm.ReplaceDots;
                    Settings.Default.RenameRemoveSpecial = settingsForm.RemoveSpecial;
                    Settings.Default.Save();
                }
            }
        }







        private void UpdateCurrentUserLabel()
        {
            if (lblCurrentUser != null)
            {
                lblCurrentUser.Text = $"Current User: {AppConfig.CurrentUser}";
            }
        }

        private void BtnSwitchUser_Click(object sender, EventArgs e)
        {
            // Check if media is currently playing before proceeding
            if (_isPlaying)
            {
                DarkMessageBox.Show(
                    this,
                    "Cannot switch users while media is currently playing.",
                    "Action Blocked",
                    MessageBoxButtons.OK,
                    darkMode
                );
                return;
            }

            using (var switchForm = new SwitchUserForm(darkMode))
            {
                if (switchForm.ShowDialog(this) == DialogResult.OK)
                {
                    string newProfile = switchForm.SelectedUser;

                    if (!string.IsNullOrWhiteSpace(newProfile) &&
                        !string.Equals(newProfile, AppConfig.CurrentUser, StringComparison.OrdinalIgnoreCase))
                    {
                        // 1. Switch profile context
                        AppConfig.SwitchUser(newProfile);

                        // 2. Reload the user's settings and media cache
                        _userSettings = AppConfig.Load();
                        items = MediaCache.Load(JsonPath);
                        foreach (var it in items)
                        {
                            if (string.IsNullOrWhiteSpace(it.CachedSeriesName))
                                it.CacheParsed();
                        }

                        // 3. Update local dark mode state and apply MainForm's full dark mode styling
                        darkMode = _userSettings.DarkMode;
                        ApplyDarkMode(darkMode); // Replace with your MainForm's actual dark mode application method if named differently

                        // 4. Apply the user's saved font family and font size
                        float fontSize = _userSettings.UIFontSize > 0 ? _userSettings.UIFontSize : 11f;
                        string fontFamily = !string.IsNullOrEmpty(_userSettings.UIFontFamily) ? _userSettings.UIFontFamily : "Segoe UI";
                        OptionsForm.FontHelper.ApplyFontToForm(this, fontSize, fontFamily);

                        // 5. Update the user interface label to reflect the new user
                        UpdateCurrentUserLabel();

                        // 6. Rebuild the show tree with the new user's watched/favourite state
                        BuildShowTree();

                        if (treeShows.SelectedNode?.Tag is List<MediaItem> activeSeasonItems)
                        {
                            currentEpisodes = activeSeasonItems;
                            LoadEpisodes(currentEpisodes);
                        }
                        else
                        {
                            listEpisodes.Items.Clear();
                            currentEpisodes = new List<MediaItem>();
                            picPoster.Image = null;
                        }
                    }
                }
            }
        }



        private void ApplyUserSettingsToUI()
        {
            // 1. Update your local dark mode tracking variable from the loaded settings
            darkMode = _userSettings.DarkMode;

            // 2. Apply Dark Mode colors to MainForm components
            Color backColor = darkMode ? Color.FromArgb(30, 30, 30) : Color.White;
            Color foreColor = darkMode ? Color.White : Color.Black;

            this.BackColor = backColor;
            this.ForeColor = foreColor;

            listEpisodes.BackColor = backColor;
            listEpisodes.ForeColor = foreColor;
            treeShows.BackColor = backColor;
            treeShows.ForeColor = foreColor;

            // 3. Apply the user's saved font family and font size using your FontHelper
            float fontSize = _userSettings.UIFontSize > 0 ? _userSettings.UIFontSize : 11f;
            string fontFamily = !string.IsNullOrEmpty(_userSettings.UIFontFamily) ? _userSettings.UIFontFamily : "Segoe UI";

            OptionsForm.FontHelper.ApplyFontToForm(this, fontSize, fontFamily);
        }



        private void btnSupport_Click(object sender, EventArgs e)
        {
            //Ko-fi username link
            string url = "https://ko-fi.com/jackhurley";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(
                    this,
                    $"Could not open browser: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    darkMode
                );
            }
        }

     
    }

}
