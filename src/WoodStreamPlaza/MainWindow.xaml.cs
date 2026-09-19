using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using WoodStreamPlaza.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace WoodStreamPlaza;

/// <summary>
/// メインウィンドウの相互作用ロジック
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        Logger.Info("MainWindow constructor starting.");
        InitializeComponent();

        // 終了時ウィンドウ位置およびサイズの復元 (グローバル規約)
        RestoreWindowBounds();

        // 上部ナビバー表示状態の反映
        UpdateNavBarVisibility();

        // イベント購読
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;

        Logger.Info("MainWindow constructor completed.");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;

    /// <summary>
    /// ウィンドウロード時：WebView2環境の初期化とページ読み込み
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Logger.Info($"MainWindow_Loaded event fired. Left={Left}, Top={Top}, Width={ActualWidth}, Height={ActualHeight}, Visibility={Visibility}, WindowState={WindowState}");
        
        // 仮想デスクトップやバックグラウンドから強制的に現在のデスクトップの最前面へ呼び出し
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                SetForegroundWindow(hwnd);
                SwitchToThisWindow(hwnd, true);
                Logger.Info("Win32 SwitchToThisWindow and Topmost sequence applied.");
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to apply Win32 window focus: {ex.Message}");
        }

        Activate();
        Focus();

        await InitializeWebViewAsync();
    }

    /// <summary>
    /// WebView2の初期化（キャッシュフォルダ分離、Cookieセッション維持）
    /// </summary>
    private async System.Threading.Tasks.Task InitializeWebViewAsync()
    {
        try
        {
            Logger.Info("InitializeWebViewAsync started.");
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string userDataFolder = Path.Combine(localAppData, "WoodStreamPlaza", "WebView2Data");
            if (!Directory.Exists(userDataFolder))
            {
                Directory.CreateDirectory(userDataFolder);
            }

            Logger.Info($"UserDataFolder: {userDataFolder}");

            // CreationProperties による初期化設定
            MainWebView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = userDataFolder
            };

            // CoreWebView2初期化完了イベント
            MainWebView.CoreWebView2InitializationCompleted += (s, args) =>
            {
                if (args.IsSuccess)
                {
                    Logger.Info("CoreWebView2InitializationCompleted: Success!");
                    MainWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                }
                else
                {
                    Logger.Info($"CoreWebView2InitializationCompleted failed: {args.InitializationException?.Message}");
                }
            };

            // イベントハンドラ登録
            MainWebView.NavigationStarting += (s, args) =>
            {
                Logger.Info($"NavigationStarting: {args.Uri}");
                LoadingProgressBar.Visibility = Visibility.Visible;
            };
            MainWebView.NavigationCompleted += (s, args) =>
            {
                Logger.Info($"NavigationCompleted: IsSuccess={args.IsSuccess}");
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                UpdateNavigationButtons();
            };
            MainWebView.SourceChanged += (s, args) => UpdateNavigationButtons();
            MainWebView.ZoomFactorChanged += (s, args) =>
            {
                ZoomLevelButton.Content = $"{Math.Round(MainWebView.ZoomFactor * 100)}%";
            };

            Logger.Info("Calling EnsureCoreWebView2Async...");
            await MainWebView.EnsureCoreWebView2Async();
            Logger.Info("EnsureCoreWebView2Async completed.");

            // 起動URLのロード
            var settings = SettingsService.Instance.CurrentSettings;
            string startUrl = string.IsNullOrWhiteSpace(settings.StartUrl) ? "https://windows-podcast.com/plaza/" : settings.StartUrl;
            Logger.Info($"Setting Source to: {startUrl}");
            MainWebView.Source = new Uri(startUrl);
        }
        catch (Exception ex)
        {
            Logger.Info($"InitializeWebViewAsync EXCEPTION: {ex.Message}\n{ex.StackTrace}");
            WpfMessageBox.Show(this, $"WebView2の初期化に失敗しました: {ex.Message}", "WoodStream PLAZA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// [戻る][進む]ボタンの有効無効状態を更新
    /// </summary>
    private void UpdateNavigationButtons()
    {
        BackButton.IsEnabled = MainWebView.CanGoBack;
        ForwardButton.IsEnabled = MainWebView.CanGoForward;
    }

    /// <summary>
    /// ナビゲーションバーの表示/非表示を更新
    /// </summary>
    private void UpdateNavBarVisibility()
    {
        var settings = SettingsService.Instance.CurrentSettings;
        NavBarRow.Height = settings.ShowNavigationBar ? GridLength.Auto : new GridLength(0);
    }

    #region ウィンドウ位置・サイズの保存と復元 (グローバル規約)

    /// <summary>
    /// 終了時のウィンドウ位置およびサイズを復元します。
    /// マルチモニターや仮想デスクトップでの画面外表示を防ぎ、確実に画面内に配置します。
    /// </summary>
    private void RestoreWindowBounds()
    {
        var settings = SettingsService.Instance.CurrentSettings;
        Logger.Info($"Restoring window bounds. W:{settings.WindowWidth}, H:{settings.WindowHeight}, L:{settings.WindowLeft}, T:{settings.WindowTop}, State:{settings.WindowState}");

        // 幅・高さの復元
        Width = (settings.WindowWidth >= MinWidth) ? settings.WindowWidth : 1200;
        Height = (settings.WindowHeight >= MinHeight) ? settings.WindowHeight : 800;

        // マルチモニター環境（隙間やオフスクリーン）を考慮し、メインディスプレイの作業領域内に確実に収める
        double workWidth = SystemParameters.WorkArea.Width;
        double workHeight = SystemParameters.WorkArea.Height;

        if (settings.WindowLeft.HasValue && settings.WindowTop.HasValue &&
            settings.WindowLeft.Value >= 0 && settings.WindowLeft.Value + 100 < workWidth &&
            settings.WindowTop.Value >= 0 && settings.WindowTop.Value + 100 < workHeight)
        {
            Left = settings.WindowLeft.Value;
            Top = settings.WindowTop.Value;
        }
        else
        {
            // メイン画面の作業領域中央に明示的に配置
            Left = Math.Max(0, (workWidth - Width) / 2);
            Top = Math.Max(0, (workHeight - Height) / 2);
        }

        WindowState = WindowState.Normal;
        Logger.Info($"Window placed at Left={Left}, Top={Top}, Width={Width}, Height={Height}");
    }

    /// <summary>
    /// ウィンドウの位置およびサイズを設定に保存します。
    /// </summary>
    private void SaveWindowBounds()
    {
        var settings = SettingsService.Instance.CurrentSettings;

        if (WindowState == WindowState.Normal)
        {
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            settings.WindowState = WindowState.Normal;
        }
        else if (WindowState == WindowState.Maximized)
        {
            Rect restoreBounds = RestoreBounds;
            if (!restoreBounds.IsEmpty)
            {
                settings.WindowLeft = restoreBounds.Left;
                settings.WindowTop = restoreBounds.Top;
                settings.WindowWidth = restoreBounds.Width;
                settings.WindowHeight = restoreBounds.Height;
            }
            settings.WindowState = WindowState.Maximized;
        }

        SettingsService.Instance.Save();
        Logger.Info("Window bounds saved.");
    }

    #endregion

    #region ウィンドウ終了イベント

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Logger.Info("MainWindow_Closing event.");
        SaveWindowBounds();
    }

    #endregion

    #region ブラウザ操作 & ルーム切り替え

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (MainWebView.CanGoBack) MainWebView.GoBack();
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (MainWebView.CanGoForward) MainWebView.GoForward();
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        MainWebView.Reload();
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = SettingsService.Instance.CurrentSettings;
        string startUrl = string.IsNullOrWhiteSpace(settings.StartUrl) ? "https://windows-podcast.com/plaza/" : settings.StartUrl;
        MainWebView.Source = new Uri(startUrl);
    }

    private async void SwitchRoom(string roomId)
    {
        if (MainWebView.CoreWebView2 == null) return;

        string script = $@"
            (() => {{
                const targetBtn = document.querySelector('[data-room=""{roomId}""]');
                if (targetBtn) {{
                    targetBtn.click();
                    return true;
                }}
                return false;
            }})();
        ";

        try
        {
            string result = await MainWebView.ExecuteScriptAsync(script);
            if (result == "false" || result == "null")
            {
                MainWebView.Source = new Uri("https://windows-podcast.com/plaza/");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ルーム切り替えエラー: {ex.Message}");
        }
    }

    private void RoomNotice_Click(object sender, RoutedEventArgs e) => SwitchRoom("notice");
    private void RoomLounge_Click(object sender, RoutedEventArgs e) => SwitchRoom("lounge");
    private void RoomEpisodes_Click(object sender, RoutedEventArgs e) => SwitchRoom("episodes");
    private void RoomEntertainment_Click(object sender, RoutedEventArgs e) => SwitchRoom("entertainment");
    private void RoomGourmet_Click(object sender, RoutedEventArgs e) => SwitchRoom("gourmet");
    private void RoomAiArt_Click(object sender, RoutedEventArgs e) => SwitchRoom("ai_art");

    #endregion

    #region ズーム制御

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        if (MainWebView.CoreWebView2 != null)
        {
            MainWebView.ZoomFactor = Math.Min(3.0, MainWebView.ZoomFactor + 0.1);
        }
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        if (MainWebView.CoreWebView2 != null)
        {
            MainWebView.ZoomFactor = Math.Max(0.5, MainWebView.ZoomFactor - 0.1);
        }
    }

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        if (MainWebView.CoreWebView2 != null)
        {
            MainWebView.ZoomFactor = 1.0;
        }
    }

    #endregion

    #region 設定画面

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWin = new SettingsWindow(MainWebView)
        {
            Owner = this
        };

        if (settingsWin.ShowDialog() == true)
        {
            UpdateNavBarVisibility();
        }
    }

    #endregion
}
