using FiendFriend.Properties;
using FiendFriend.Configuration;
using FiendFriend.Services.Core;
using FiendFriend.Services.Communication;
using FiendFriend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FiendFriend
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private readonly Random _random = new();
        private DispatcherTimer? _changeTimer;
        private readonly IConfiguration _configuration;
        private readonly string _spritePath;
        private readonly int _imageChangeIntervalMinutes;
        private readonly bool _enableDoubleClickToChangeImage;
        private readonly bool _flipImagesHorizontally;
        private MessageChannelManager? _messageChannelManager;
        private ImageService? _imageService;

        public MainWindow(string? configFile = null)
        {
            InitializeComponent();

            var configFileName = configFile ?? "appsettings.json";

            _configuration = new ConfigurationBuilder()
                .AddJsonFile(configFileName, optional: false, reloadOnChange: true)
                .Build();

            _spritePath = _configuration["FiendFriend:SpritePath"] ??
                @"E:\source\extrpact\src\bin\Debug\net8.0\4\sprites\Cosmos";
            _imageChangeIntervalMinutes = _configuration.GetValue<int>("FiendFriend:ImageChangeIntervalMinutes", 5);
            _enableDoubleClickToChangeImage = _configuration.GetValue<bool>("FiendFriend:EnableDoubleClickToChangeImage", true);
            _flipImagesHorizontally = _configuration.GetValue<bool>("FiendFriend:FlipImagesHorizontally", false);

            InitializeSystemTray();
            LoadWindowSettings();
            LoadRandomImages();
            SetupImageChangeTimer();
            SetupDoubleClickHandler();
            SetupImageFlipping();
            PinToDesktop();
            SetupResizeHandling();
            SetupPositionSaving();
            _ = InitializeCommunicationServicesAsync();
        }

        private void InitializeSystemTray()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateIcon(),
                Visible = true,
                Text = "FiendFriend - Desktop Companion"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Communication Status", null, OnShowCommunicationStatusClick);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Reset Position Settings", null, OnResetPositionClick);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, OnExitClick);
            _notifyIcon.ContextMenuStrip = contextMenu;

            _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
        }

        private System.Drawing.Icon CreateIcon()
        {
            var bitmap = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.FillEllipse(System.Drawing.Brushes.Purple, 2, 2, 12, 12);
                g.FillEllipse(System.Drawing.Brushes.Yellow, 4, 4, 8, 8);
            }
            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }

        private async Task InitializeCommunicationServicesAsync()
        {
            try
            {
                _imageService = new ImageService(this, _spritePath);
                _messageChannelManager = new MessageChannelManager(_imageService);

                var commSettings = new CommunicationSettings();
                _configuration.GetSection("Communication").Bind(commSettings);

                await _messageChannelManager.InitializeAsync(commSettings);
            }
            catch (Exception ex)
            {
                _notifyIcon?.ShowBalloonTip(5000, "Communication Error",
                    $"Failed to initialize communication services: {ex.Message}",
                    ToolTipIcon.Error);
            }
        }

        public void LoadRandomImages()
        {
            try
            {
                var basesPath = Path.Combine(_spritePath, "bases");
                var facesPath = Path.Combine(_spritePath, "faces");

                if (!Directory.Exists(basesPath) || !Directory.Exists(facesPath))
                {
                    ShowErrorMessage($"Sprite directories not found:\nBases: {basesPath}\nFaces: {facesPath}\n\nPlease check the SpritePath setting in appsettings.json");
                    return;
                }

                var baseFiles = Directory.GetFiles(basesPath, "*.png");
                var faceFiles = Directory.GetFiles(facesPath, "*.png");

                if (baseFiles.Length == 0 || faceFiles.Length == 0)
                {
                    ShowErrorMessage("No PNG files found in bases or faces directories.");
                    return;
                }

                var randomBase = baseFiles[_random.Next(baseFiles.Length)];
                BaseImage.Source = new BitmapImage(new Uri(randomBase));

                var randomFace = faceFiles[_random.Next(faceFiles.Length)];
                FaceImage.Source = new BitmapImage(new Uri(randomFace));

                if (Settings.Default.FirstRun && BaseImage.Source is BitmapImage baseImg)
                {
                    Width = baseImg.PixelWidth;
                    Height = baseImg.PixelHeight;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error loading images: {ex.Message}");
            }
        }
        private void SetupImageChangeTimer()
        {
            _changeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(_imageChangeIntervalMinutes)
            };
            _changeTimer.Tick += (s, e) => LoadRandomImages();
            _changeTimer.Start();
        }

        private void SetupDoubleClickHandler()
        {
            if (_enableDoubleClickToChangeImage)
            {
                BaseImage.MouseLeftButtonDown += OnImageDoubleClick;
                FaceImage.MouseLeftButtonDown += OnImageDoubleClick;
            }
        }

        private void OnImageDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                LoadRandomImages();
                e.Handled = true;
            }
        }

        private void SetupImageFlipping()
        {
            if (_flipImagesHorizontally)
            {
                ApplyHorizontalFlip();
            }
        }

        private void ApplyHorizontalFlip()
        {
            var flipTransform = new System.Windows.Media.ScaleTransform(-1, 1);

            BaseImage.RenderTransform = flipTransform;
            BaseImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

            FaceImage.RenderTransform = flipTransform;
            FaceImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        }

        private void PinToDesktop()
        {
            MouseLeftButtonDown += (s, e) =>
            {
                DragMove();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PinToDesktopLevel();
                }), DispatcherPriority.ApplicationIdle);
            };
        }

        private void ShowErrorMessage(string message)
        {
            _notifyIcon?.ShowBalloonTip(5000, "FiendFriend Error", message, ToolTipIcon.Error);
        }

        private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
        {
            Visibility = Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void OnShowCommunicationStatusClick(object? sender, EventArgs e)
        {
            if (_messageChannelManager == null)
            {
                _notifyIcon?.ShowBalloonTip(3000, "Communication Status",
                    "Communication services not initialized", ToolTipIcon.Warning);
                return;
            }

            var channels = _messageChannelManager.GetChannelStatus().ToList();
            if (!channels.Any())
            {
                _notifyIcon?.ShowBalloonTip(3000, "Communication Status",
                    "No communication channels configured", ToolTipIcon.Info);
                return;
            }

            var statusText = string.Join("\n", channels.Select(c => $"{c.Name}: {(c.IsActive ? "Active" : "Inactive")}"));
            _notifyIcon?.ShowBalloonTip(5000, "Communication Status", statusText, ToolTipIcon.Info);
        }

        private void OnResetPositionClick(object? sender, EventArgs e)
        {
            try
            {
                Settings.Default.WindowLeft = 100;
                Settings.Default.WindowTop = 100;
                Settings.Default.WindowWidth = 200;
                Settings.Default.WindowHeight = 200;
                Settings.Default.FirstRun = true;
                Settings.Default.Save();

                _notifyIcon?.ShowBalloonTip(3000, "Settings Reset",
                    "Position settings have been reset. Restart the app to see the default position.",
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _notifyIcon?.ShowBalloonTip(3000, "Reset Failed",
                    $"Failed to reset settings: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            try
            {
                if (_changeTimer != null)
                {
                    _changeTimer.Stop();
                    _changeTimer = null;
                }
                
                if (_messageChannelManager != null)
                {
                    _messageChannelManager.Dispose();
                    _messageChannelManager = null;
                }
                
                if (_notifyIcon != null)
                {
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
            
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                SaveWindowSettings();
                
                if (_changeTimer != null)
                {
                    _changeTimer.Stop();
                    _changeTimer = null;
                }
                
                if (_messageChannelManager != null)
                {
                    _messageChannelManager.Dispose();
                    _messageChannelManager = null;
                }
                
                if (_notifyIcon != null)
                {
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during window cleanup: {ex.Message}");
            }
            
            base.OnClosed(e);
        }

        private void SetupResizeHandling()
        {
            SizeChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PinToDesktopLevel();
                }), DispatcherPriority.ApplicationIdle);
            };
        }

        private void LoadWindowSettings()
        {
            try
            {
                if (Settings.Default.FirstRun)
                {
                    SetDefaultPosition();
                    Settings.Default.FirstRun = false;
                    Settings.Default.Save();

                    _notifyIcon?.ShowBalloonTip(2000, "First Run", "Using default position for first run", ToolTipIcon.Info);
                }
                else
                {
                    var savedLeft = Settings.Default.WindowLeft;
                    var savedTop = Settings.Default.WindowTop;
                    var savedWidth = Settings.Default.WindowWidth;
                    var savedHeight = Settings.Default.WindowHeight;

                    if (IsWindowOnScreen(savedLeft, savedTop, savedWidth, savedHeight))
                    {
                        Left = savedLeft;
                        Top = savedTop;
                        Width = savedWidth;
                        Height = savedHeight;
                    }
                    else
                    {
                        SetDefaultPosition();
                        _notifyIcon?.ShowBalloonTip(3000, "Position Reset", $"Saved position was invalid: {savedLeft:F0},{savedTop:F0} {savedWidth:F0}x{savedHeight:F0}\nUsing default location", ToolTipIcon.Warning);
                    }
                }
            }
            catch
            {
                SetDefaultPosition();
            }
        }

        private void SetDefaultPosition()
        {
            var workingArea = SystemParameters.WorkArea;
            Left = workingArea.Right - 220;
            Top = workingArea.Bottom - 220;
            Width = 200;
            Height = 200;
        }

        private bool IsWindowOnScreen(double left, double top, double width, double height)
        {
            var screens = Screen.AllScreens;

            foreach (var screen in screens)
            {
                var screenBounds = screen.WorkingArea;

                var intersectLeft = Math.Max(left, screenBounds.Left);
                var intersectTop = Math.Max(top, screenBounds.Top);
                var intersectRight = Math.Min(left + width, screenBounds.Right);
                var intersectBottom = Math.Min(top + height, screenBounds.Bottom);

                var intersectWidth = Math.Max(0, intersectRight - intersectLeft);
                var intersectHeight = Math.Max(0, intersectBottom - intersectTop);

                var titleBarTop = Math.Max(top, screenBounds.Top);
                var titleBarBottom = Math.Min(top + 30, screenBounds.Bottom);
                var titleBarHeight = Math.Max(0, titleBarBottom - titleBarTop);

                if (intersectWidth > 0 && intersectHeight > 0 && titleBarHeight > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetupPositionSaving()
        {
            LocationChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SaveWindowSettings();
                }), DispatcherPriority.ApplicationIdle);
            };

            SizeChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SaveWindowSettings();
                }), DispatcherPriority.ApplicationIdle);
            };
        }

        private void SaveWindowSettings()
        {
            try
            {
                Settings.Default.WindowLeft = Left;
                Settings.Default.WindowTop = Top;
                Settings.Default.WindowWidth = Width;
                Settings.Default.WindowHeight = Height;
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                _notifyIcon?.ShowBalloonTip(3000, "Settings Save Error", $"Failed to save window settings: {ex.Message}", ToolTipIcon.Warning);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            PinToDesktopLevel();
            HideFromAltTab();
        }

        private void PinToDesktopLevel()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var desktopHandle = FindWindow("Progman", "Program Manager");
            SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        private void HideFromAltTab()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            int currentStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            int newStyle = currentStyle | WS_EX_TOOLWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
        }
    }
}
