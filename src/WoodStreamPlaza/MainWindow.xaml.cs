using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using WoodStreamPlaza.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace WoodStreamPlaza;

/// <summary>
/// メインウィンドウの相互作用ロジック
/// </summary>
public partial class MainWindow : Window
{
    private bool _isExplicitExit = false;

    public MainWindow()
    {
        InitializeComponent();

        // 終了時ウィンドウ位置およびサイズの復元 (グローバル規約)
        RestoreWindowBounds();

        // 上部ナビバー表示状態の反映
        UpdateNavBarVisibility();

        // トレイアイコンの初期化
        TrayService.Instance.Initialize(this, OpenSettingsWindow);

        // イベント購読
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
    }

    /// <summary>
    /// ウィンドウロード時：WebView2環境の初期化とページ読み込み
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebViewAsync();
    }

    /// <summary>
    /// WebView2の初期化（キャッシュフォルダ分離、Cookieセッション維持）
    /// </summary>
    private async System.Threading.Tasks.Task InitializeWebViewAsync()
    {
        try
        {
            // グローバル規約: AppData\Local\WoodStreamPlaza へのデータ配置
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string userDataFolder = Path.Combine(localAppData, "WoodStreamPlaza", "WebView2Data");

            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await MainWebView.EnsureCoreWebView2Async(env);

            // イベントハンドラ登録
            MainWebView.NavigationStarting += (s, args) => LoadingProgressBar.Visibility = Visibility.Visible;
            MainWebView.NavigationCompleted += (s, args) =>
            {
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                UpdateNavigationButtons();
            };
            MainWebView.SourceChanged += (s, args) => UpdateNavigationButtons();
            MainWebView.ZoomFactorChanged += (s, args) =>
            {
                ZoomLevelButton.Content = $"{Math.Round(MainWebView.ZoomFactor * 100)}%";
            };

            // 起動URLのロード
            var settings = SettingsService.Instance.CurrentSettings;
            string startUrl = string.IsNullOrWhiteSpace(settings.StartUrl) ? "https://windows-podcast.com/plaza/" : settings.StartUrl;
            MainWebView.Source = new Uri(startUrl);
        }
        catch (Exception ex)
        {
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
    /// </summary>
    private void RestoreWindowBounds()
    {
        var settings = SettingsService.Instance.CurrentSettings;

        // 幅・高さの復元
        if (settings.WindowWidth >= MinWidth)
        {
            Width = settings.WindowWidth;
        }
        if (settings.WindowHeight >= MinHeight)
        {
            Height = settings.WindowHeight;
        }

        // 座標の復元（画面外に配置されて見えなくなるのを防止）
        if (settings.WindowLeft.HasValue && settings.WindowTop.HasValue)
        {
            double left = settings.WindowLeft.Value;
            double top = settings.WindowTop.Value;

            // 仮想スクリーン全体（マルチモニタ含む）の範囲内にあるか検証
            double virtualLeft = SystemParameters.VirtualScreenLeft;
            double virtualTop = SystemParameters.VirtualScreenTop;
            double virtualWidth = SystemParameters.VirtualScreenWidth;
            double virtualHeight = SystemParameters.VirtualScreenHeight;

            if (left >= virtualLeft - 100 && left < virtualLeft + virtualWidth - 100 &&
                top >= virtualTop - 100 && top < virtualTop + virtualHeight - 100)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = left;
                Top = top;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        // 最大化状態の復元
        if (settings.WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
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
        else
        {
            // 最大化や最小化時の場合は RestoreBounds から通常時のサイズを取得
            Rect restoreBounds = RestoreBounds;
            if (!restoreBounds.IsEmpty)
            {
                settings.WindowLeft = restoreBounds.Left;
                settings.WindowTop = restoreBounds.Top;
                settings.WindowWidth = restoreBounds.Width;
                settings.WindowHeight = restoreBounds.Height;
            }
            settings.WindowState = WindowState == WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        SettingsService.Instance.Save();
    }

    #endregion

    #region ウィンドウライフサイクル & トレイ常駐

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        var settings = SettingsService.Instance.CurrentSettings;
        if (WindowState == WindowState.Minimized && settings.MinimizeToTray)
        {
            Hide();
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var settings = SettingsService.Instance.CurrentSettings;

        if (!_isExplicitExit && settings.CloseToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        // 終了時に位置とサイズを保存 (グローバル規約)
        SaveWindowBounds();
        TrayService.Instance.Dispose();
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

    /// <summary>
    /// Webページ内の特定ルームをクリックして切り替えるヘルパー
    /// </summary>
    private async void SwitchRoom(string roomId)
    {
        if (MainWebView.CoreWebView2 == null) return;

        // Webページ内のルーム切替ボタンを安全にクリック
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
            // まだページが読み込まれていないか、別のページにいる場合はトップページへ遷移
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
        OpenSettingsWindow();
    }

    private void OpenSettingsWindow()
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
