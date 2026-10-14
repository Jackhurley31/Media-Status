using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using Media_Status;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MediaControlApp.Forms
{
    public partial class VideoPlayerForm : Form
    {
        public bool UserStoppedPlayback { get; private set; } = false;

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private LibVLCSharp.WinForms.VideoView _videoView;

        // UI Controls for custom overlay
        private Panel _controlPanel;
        private Button _btnPlayPause;
        private Button _btnMute;
        private Button _btnClose;
        private Button _btnFullscreen;
        private Button _btnSettings;

        // Custom sleek seek bar & volume slider components
        private DoubleBufferedPanel _seekPanel;
        private DoubleBufferedPanel _volumePanel;
        private float _currentProgress = 0f;
        private float _currentVolume = 1f;
        private bool _isDraggingSeek = false;
        private bool _isDraggingVolume = false;
        private bool _isHoveringSeek = false;
        private bool _isHoveringVolume = false;

        private Label _lblTime;
        private Panel _settingsPopup;

        private System.Windows.Forms.Timer _hideTimer;
        private Point _lastMousePosition;
        private bool _isFullscreen = false;
        private bool _isClosing = false;
        private int _idleCounter = 0;
        private double _finalPlaybackTime = 0;

        public string FilePath { get; }
        public double CurrentPlaybackTime => _finalPlaybackTime;

        public VideoPlayerForm(string filePath, double startSeconds, UserSettings userSettings, Form ownerForm = null)
        {
            InitializeComponent();
            FilePath = filePath;

            // 1. Initialize LibVLC with local installation path
            string vlcPath = Environment.Is64BitOperatingSystem
                ? @"C:\Program Files\VideoLAN\VLC"
                : @"C:\Program Files (x86)\VideoLAN\VLC";

            if (!Directory.Exists(vlcPath))
            {
                vlcPath = @"C:\Program Files\VideoLAN\VLC";
            }

            Core.Initialize(vlcPath);
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            // Automatically close and save progress when video reaches the end
            _mediaPlayer.EndReached += (s, e) =>
            {
                if (_isClosing || this.IsDisposed) return;
                try
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        if (_mediaPlayer != null && _mediaPlayer.Length > 0)
                        {
                            _finalPlaybackTime = (double)_mediaPlayer.Length / 1000.0;
                            _currentProgress = 1.0f;
                            _seekPanel?.Invalidate();
                        }

                        if (!this.IsDisposed)
                        {
                            this.Close();
                        }
                    });
                }
                catch { }
            };

            // 2. Setup VideoView
            _videoView = new LibVLCSharp.WinForms.VideoView
            {
                Dock = DockStyle.Fill,
                MediaPlayer = _mediaPlayer
            };
            this.Controls.Add(_videoView);

            // 3. Build UI Overlay & Modern Settings Popup
            SetupControlOverlay();
            SetupSettingsPopup();

            // 4. Configure Initial Window State
            _isFullscreen = userSettings.EnableFullscreen;
            ApplyWindowMode(_isFullscreen);

            // 5. Hook up Mouse Activity Timer
            _lastMousePosition = Cursor.Position;
            _hideTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _hideTimer.Tick += (s, e) =>
            {
                if (_isClosing || _controlPanel.IsDisposed) return;

                try
                {
                    Point currentMouse = Cursor.Position;
                    Point clientCursor = this.PointToClient(currentMouse);

                    if (currentMouse != _lastMousePosition)
                    {
                        _lastMousePosition = currentMouse;
                        _controlPanel.Visible = true;
                        Cursor.Show();
                        _idleCounter = 0;
                        return;
                    }

                    bool overControl = _controlPanel.Bounds.Contains(clientCursor);
                    bool overPopup = _settingsPopup.Visible && _settingsPopup.Bounds.Contains(clientCursor);

                    if (overControl || overPopup || _isDraggingSeek || _isDraggingVolume || _settingsPopup.Visible)
                    {
                        _idleCounter = 0;
                        return;
                    }

                    _idleCounter++;
                    if (_idleCounter >= 12) // ~3 seconds of inactivity
                    {
                        _controlPanel.Visible = false;
                        _settingsPopup.Visible = false;
                        if (_isFullscreen)
                        {
                            Cursor.Hide();
                        }
                    }
                }
                catch { }
            };
            _hideTimer.Start();

            // 6. Wire up LibVLC Progress Events safely
            _mediaPlayer.TimeChanged += (s, e) =>
            {
                if (_isDraggingSeek || _isClosing || this.IsDisposed) return;

                try
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        if (_mediaPlayer != null && _mediaPlayer.Length > 0 && !_isClosing)
                        {
                            _currentProgress = (float)_mediaPlayer.Time / _mediaPlayer.Length;
                            _finalPlaybackTime = (double)_mediaPlayer.Time / 1000.0;
                            _seekPanel.Invalidate();
                            UpdateTimeString(_mediaPlayer.Time, _mediaPlayer.Length);
                        }
                    });
                }
                catch { }
            };

            // 7. Load & Play Media
            if (File.Exists(filePath))
            {
                var media = new Media(_libVLC, filePath, FromType.FromPath);

                if (userSettings.EnableSubtitles)
                {
                    media.AddOption(":sub-track=0");
                }
                else
                {
                    media.AddOption(":no-sub-autodetect");
                }

                _mediaPlayer.Play(media);

                if (startSeconds > 5)
                {
                    _mediaPlayer.Time = (long)(startSeconds * 1000);
                }
            }

            // Keyboard & Mouse Controls
            this.KeyPreview = true;
            this.KeyDown += VideoPlayerForm_KeyDown;
            this.MouseWheel += VideoPlayerForm_MouseWheel;
            _videoView.MouseWheel += VideoPlayerForm_MouseWheel;

            _videoView.DoubleClick += (s, e) => ToggleFullscreen();
            this.DoubleClick += (s, e) => ToggleFullscreen();

            _videoView.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    ToggleSettingsPopup();
                }
                else
                {
                    if (_settingsPopup.Visible)
                    {
                        Point popupClientPoint = _settingsPopup.PointToClient(Cursor.Position);
                        if (!_settingsPopup.ClientRectangle.Contains(popupClientPoint))
                        {
                            _settingsPopup.Visible = false;
                        }
                    }
                }
            };
        }

        private void SetupControlOverlay()
        {
            _controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.FromArgb(240, 20, 20, 20)
            };

            // Play / Pause Button
            _btnPlayPause = CreateStyledButton("❚❚", 42, 12);
            _btnPlayPause.Click += (s, e) => TogglePlayPause();

            // Mute Button
            _btnMute = CreateStyledButton("🔊", 38, 58);
            _btnMute.Click += (s, e) => ToggleMute();

            // Custom Sleek Volume Slider
            _volumePanel = new DoubleBufferedPanel
            {
                Width = 85,
                Height = 35,
                Left = 100,
                Top = 14,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            _volumePanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int width = _volumePanel.Width;
                int height = _volumePanel.Height;
                int trackHeight = 5;
                int trackY = (height - trackHeight) / 2;

                using (var bgBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
                {
                    g.FillRectangle(bgBrush, 0, trackY, width, trackHeight);
                }

                int volWidth = (int)(width * _currentVolume);
                using (var volBrush = new SolidBrush(Color.FromArgb(220, 220, 220)))
                {
                    g.FillRectangle(volBrush, 0, trackY, volWidth, trackHeight);
                }

                if (_isHoveringVolume || _isDraggingVolume)
                {
                    using (var handleBrush = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(handleBrush, Math.Max(0, volWidth - 6), trackY - 3.5f, 12, 12);
                    }
                }
            };

            _volumePanel.MouseDown += (s, e) => { _isDraggingVolume = true; UpdateVolumeFromMouse(e.X); };
            _volumePanel.MouseMove += (s, e) => { if (_isDraggingVolume) UpdateVolumeFromMouse(e.X); };
            _volumePanel.MouseUp += (s, e) => { _isDraggingVolume = false; UpdateVolumeFromMouse(e.X); };
            _volumePanel.MouseEnter += (s, e) => { _isHoveringVolume = true; _volumePanel.Invalidate(); };
            _volumePanel.MouseLeave += (s, e) => { _isHoveringVolume = false; _volumePanel.Invalidate(); };

            // Custom Sleek Seek Bar
            _seekPanel = new DoubleBufferedPanel
            {
                Height = 35,
                Top = 14,
                Left = 195,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            _seekPanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int width = _seekPanel.Width;
                int height = _seekPanel.Height;
                int trackHeight = 5;
                int trackY = (height - trackHeight) / 2;

                using (var bgBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
                {
                    g.FillRectangle(bgBrush, 0, trackY, width, trackHeight);
                }

                int progressWidth = (int)(width * _currentProgress);
                using (var progressBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    g.FillRectangle(progressBrush, 0, trackY, progressWidth, trackHeight);
                }

                if (_isHoveringSeek || _isDraggingSeek)
                {
                    using (var handleBrush = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(handleBrush, Math.Max(0, progressWidth - 6), trackY - 3.5f, 12, 12);
                    }
                }
            };

            _seekPanel.MouseDown += (s, e) => { _isDraggingSeek = true; UpdateSeekFromMouse(e.X); };
            _seekPanel.MouseMove += (s, e) => { if (_isDraggingSeek) UpdateSeekFromMouse(e.X); };
            _seekPanel.MouseUp += (s, e) => { _isDraggingSeek = false; UpdateSeekFromMouse(e.X); };
            _seekPanel.MouseEnter += (s, e) => { _isHoveringSeek = true; _seekPanel.Invalidate(); };
            _seekPanel.MouseLeave += (s, e) => { _isHoveringSeek = false; _seekPanel.Invalidate(); };

            // Time Label
            _lblTime = new Label
            {
                Text = "00:00 / 00:00",
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(0, 21)
            };

            // Settings Menu Button
            _btnSettings = CreateStyledButton("⚙ Options", 84, 0);
            _btnSettings.Click += (s, e) => ToggleSettingsPopup();

            // Fullscreen Toggle Button
            _btnFullscreen = CreateStyledButton("⛶", 38, 0);
            _btnFullscreen.Click += (s, e) => ToggleFullscreen();

            // Close Button
            _btnClose = CreateStyledButton("✕", 38, 0);
            _btnClose.BackColor = Color.FromArgb(170, 45, 45);
            _btnClose.Click += (s, e) =>
            {
                UserStoppedPlayback = true;
                this.Close();
            };

            // Handle Dynamic Resizing & Spacing
            _controlPanel.Resize += (s, e) =>
            {
                _seekPanel.Width = Math.Max(50, _controlPanel.Width - 485);
                _lblTime.Left = _controlPanel.Width - 280;
                _btnSettings.Left = _controlPanel.Width - 165;
                _btnFullscreen.Left = _controlPanel.Width - 77;
                _btnClose.Left = _controlPanel.Width - 37;
            };

            _controlPanel.Controls.Add(_btnPlayPause);
            _controlPanel.Controls.Add(_btnMute);
            _controlPanel.Controls.Add(_volumePanel);
            _controlPanel.Controls.Add(_seekPanel);
            _controlPanel.Controls.Add(_lblTime);
            _controlPanel.Controls.Add(_btnSettings);
            _controlPanel.Controls.Add(_btnFullscreen);
            _controlPanel.Controls.Add(_btnClose);

            this.Controls.Add(_controlPanel);
            _controlPanel.BringToFront();
        }

        private void UpdateSeekFromMouse(int mouseX)
        {
            if (_mediaPlayer == null || _mediaPlayer.Length <= 0) return;
            float ratio = (float)mouseX / _seekPanel.Width;
            ratio = Math.Clamp(ratio, 0f, 1f);
            _currentProgress = ratio;
            _mediaPlayer.Time = (long)(ratio * _mediaPlayer.Length);
            _finalPlaybackTime = (double)_mediaPlayer.Time / 1000.0;
            _seekPanel.Invalidate();
        }

        private void UpdateVolumeFromMouse(int mouseX)
        {
            float ratio = (float)mouseX / _volumePanel.Width;
            ratio = Math.Clamp(ratio, 0f, 1f);
            _currentVolume = ratio;
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)(ratio * 100);
                _btnMute.Text = _mediaPlayer.Volume == 0 ? "🔇" : "🔊";
            }
            _volumePanel.Invalidate();
        }

        private Button CreateStyledButton(string text, int width, int left)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 36,
                Left = left,
                Top = 14,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 40),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 90, 90);
            return btn;
        }

        private void SetupSettingsPopup()
        {
            _settingsPopup = new Panel
            {
                Width = 200,
                Height = 160,
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            int yPos = 8;
            AddPopupOption("Speed: 1.0x", yPos, (s, e) => { _mediaPlayer?.SetRate(1.0f); _settingsPopup.Visible = false; });
            yPos += 35;
            AddPopupOption("Speed: 1.5x", yPos, (s, e) => { _mediaPlayer?.SetRate(1.5f); _settingsPopup.Visible = false; });
            yPos += 35;
            AddPopupOption("Aspect Ratio 16:9", yPos, (s, e) => { if (_mediaPlayer != null) _mediaPlayer.AspectRatio = "16:9"; _settingsPopup.Visible = false; });
            yPos += 35;
            AddPopupOption("Reset Aspect Ratio", yPos, (s, e) => { if (_mediaPlayer != null) _mediaPlayer.AspectRatio = null; _settingsPopup.Visible = false; });

            this.Controls.Add(_settingsPopup);
            _settingsPopup.BringToFront();
        }

        private void AddPopupOption(string text, int top, EventHandler onClick)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Width = 184,
                Height = 28,
                Left = 8,
                Top = top,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            lbl.Click += onClick;
            lbl.MouseEnter += (s, e) => lbl.BackColor = Color.FromArgb(50, 50, 50);
            lbl.MouseLeave += (s, e) => lbl.BackColor = Color.Transparent;
            _settingsPopup.Controls.Add(lbl);
        }

        private void ToggleSettingsPopup()
        {
            if (_settingsPopup.Visible)
            {
                _settingsPopup.Visible = false;
            }
            else
            {
                Point btnLoc = _btnSettings.PointToScreen(Point.Empty);
                Point formLoc = this.PointToClient(btnLoc);
                _settingsPopup.Location = new Point(formLoc.X, formLoc.Y - _settingsPopup.Height - 6);
                _settingsPopup.Visible = true;
                _settingsPopup.BringToFront();
            }
        }

        private void VideoPlayerForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isClosing) return;
            int delta = e.Delta > 0 ? 5 : -5;
            int newVol = Math.Clamp(_mediaPlayer.Volume + delta, 0, 100);
            _currentVolume = newVol / 100f;
            if (_mediaPlayer != null) _mediaPlayer.Volume = newVol;
            _volumePanel.Invalidate();
        }

        private void VideoPlayerForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isClosing) return;

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    if (_isFullscreen)
                    {
                        ToggleFullscreen();
                    }
                    else
                    {
                        UserStoppedPlayback = true;
                        this.Close();
                    }
                    break;
                case Keys.Space:
                    TogglePlayPause();
                    break;
                case Keys.F:
                    ToggleFullscreen();
                    break;
                case Keys.Left:
                    if (_mediaPlayer != null)
                    {
                        _mediaPlayer.Time = Math.Max(0, _mediaPlayer.Time - 5000);
                        _finalPlaybackTime = (double)_mediaPlayer.Time / 1000.0;
                    }
                    break;
                case Keys.Right:
                    if (_mediaPlayer != null)
                    {
                        _mediaPlayer.Time = Math.Min(_mediaPlayer.Length, _mediaPlayer.Time + 5000);
                        _finalPlaybackTime = (double)_mediaPlayer.Time / 1000.0;
                    }
                    break;
                case Keys.Up:
                    if (_mediaPlayer != null)
                    {
                        int upVol = Math.Clamp(_mediaPlayer.Volume + 5, 0, 100);
                        _mediaPlayer.Volume = upVol;
                        _currentVolume = upVol / 100f;
                        _volumePanel.Invalidate();
                    }
                    break;
                case Keys.Down:
                    if (_mediaPlayer != null)
                    {
                        int downVol = Math.Clamp(_mediaPlayer.Volume - 5, 0, 100);
                        _mediaPlayer.Volume = downVol;
                        _currentVolume = downVol / 100f;
                        _volumePanel.Invalidate();
                    }
                    break;
                case Keys.M:
                    ToggleMute();
                    break;
            }
        }

        private void TogglePlayPause()
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Pause();
                    _btnPlayPause.Text = "▶";
                }
                else
                {
                    _mediaPlayer.Play();
                    _btnPlayPause.Text = "❚❚";
                }
            }
        }

        private void ToggleMute()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Mute = !_mediaPlayer.Mute;
                _btnMute.Text = _mediaPlayer.Mute ? "🔇" : "🔊";
            }
        }

        private void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;
            ApplyWindowMode(_isFullscreen);
        }

        private void ApplyWindowMode(bool fullscreen)
        {
            if (fullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void UpdateTimeString(long currentTimeMs, long totalTimeMs)
        {
            TimeSpan current = TimeSpan.FromMilliseconds(currentTimeMs);
            TimeSpan total = TimeSpan.FromMilliseconds(totalTimeMs);

            string curStr = current.Hours > 0 ? current.ToString(@"h\:mm\:ss") : current.ToString(@"mm\:ss");
            string totStr = total.Hours > 0 ? total.ToString(@"h\:mm\:ss") : total.ToString(@"mm\:ss");

            _lblTime.Text = $"{curStr} / {totStr}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _isClosing = true;

            try
            {
                if (_mediaPlayer != null && _mediaPlayer.Time > 0)
                {
                    _finalPlaybackTime = (double)_mediaPlayer.Time / 1000.0;
                }

                _hideTimer?.Stop();
                _hideTimer?.Dispose();
                _mediaPlayer?.Stop();
                _mediaPlayer?.Dispose();
                _libVLC?.Dispose();
            }
            catch { }

            base.OnFormClosing(e);
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }
}