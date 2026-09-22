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
    private readonly TrayIconService _trayIconService = new();
    private readonly bool _startMinimized;
    private bool _isExplicitExit = false;
    private System.Windows.Threading.DispatcherTimer? _backgroundPollingTimer;

    public MainWindow(bool startMinimized = false)
    {
        _startMinimized = startMinimized;
        Logger.Info($"MainWindow constructor starting. startMinimized={startMinimized}");
        InitializeComponent();

        // 終了時ウィンドウ位置およびサイズの復元 (グローバル規約)
        RestoreWindowBounds();

        // 上部ナビバー表示状態の反映
        UpdateNavBarVisibility();

        // バージョン番号の反映
        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        if (ver != null)
        {
            AppVersionTextBlock.Text = $"v{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";
        }

        // タスクトレイアイコンの初期化とイベント購読
        InitializeTrayIcon();

        // 通知クリックイベント購読
        NotificationService.Instance.NotificationClicked += OnNotificationClicked;

        // イベント購読
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;

        Logger.Info("MainWindow constructor completed.");
    }

    /// <summary>
    /// タスクトレイ常駐アイコンの初期化
    /// </summary>
    private void InitializeTrayIcon()
    {
        _trayIconService.Initialize();
        _trayIconService.OpenRequested += RestoreAndActivate;
        _trayIconService.ReloadRequested += () => Dispatcher.Invoke(() => MainWebView.Reload());
        _trayIconService.SettingsRequested += () => Dispatcher.Invoke(OpenSettingsDialog);
        _trayIconService.ExitRequested += ExitApplication;
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
        Logger.Info($"MainWindow_Loaded event fired. Left={Left}, Top={Top}, Width={ActualWidth}, Height={ActualHeight}, Visibility={Visibility}, WindowState={WindowState}, startMinimized={_startMinimized}");
        
        if (!_startMinimized)
        {
            // 最前面復元
            RestoreAndActivate();
        }
        else
        {
            // 起動時最小化：タスクトレイに格納
            WindowState = WindowState.Minimized;
            Hide();
            Logger.Info("MainWindow hidden to system tray on start.");
        }

        await InitializeWebViewAsync();
    }

    /// <summary>
    /// Webサイトとクライアントを繋ぐ通知ブリッジ用JavaScriptスクリプト
    /// </summary>
    private const string NotificationBridgeScript = @"
        (() => {
            if (window.__plazaNotificationBridgeInjected) return;
            window.__plazaNotificationBridgeInjected = true;

            // 1. window.Notification のフック / ポリフィル
            try {
                const notifyHost = (title, options) => {
                    try {
                        window.chrome?.webview?.postMessage({
                            type: 'web_notification',
                            title: title,
                            body: options?.body,
                            icon: options?.icon
                        });
                    } catch (e) {}
                };

                if (!window.Notification) {
                    window.Notification = function(title, options) {
                        notifyHost(title, options);
                    };
                } else {
                    const OrigNotif = window.Notification;
                    window.Notification = function(title, options) {
                        notifyHost(title, options);
                        return new OrigNotif(title, options);
                    };
                }
                window.Notification.permission = 'granted';
                window.Notification.requestPermission = async () => 'granted';
            } catch (e) {
                console.error('Notification hook error:', e);
            }

            // 2. window.fetch のインターセプト（api.php?action=get_notifications を監視）
            try {
                const origFetch = window.fetch;
                window.fetch = async function(...args) {
                    const res = await origFetch.apply(this, args);
                    try {
                        const url = typeof args[0] === 'string' ? args[0] : (args[0]?.url || '');
                        if (url.includes('action=get_notifications')) {
                            const clone = res.clone();
                            clone.json().then(data => {
                                if (data && data.success && Array.isArray(data.notifications)) {
                                    window.chrome?.webview?.postMessage({
                                        type: 'notifications_updated',
                                        unreadCount: data.unread_count || 0,
                                        notifications: data.notifications
                                    });
                                }
                            }).catch(() => {});
                        }
                    } catch (e) {}
                    return res;
                };
            } catch (e) {
                console.error('Fetch hook error:', e);
            }
        })();
    ";

    /// <summary>
    /// WebView2の初期化（キャッシュフォルダ分離、Cookieセッション維持、通知スクリプト注入）
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
            MainWebView.CoreWebView2InitializationCompleted += async (s, args) =>
            {
                if (args.IsSuccess)
                {
                    Logger.Info("CoreWebView2InitializationCompleted: Success!");
                    MainWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    MainWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                    // 通知のアクセス許可要求を自動許可
                    MainWebView.CoreWebView2.PermissionRequested += (sender, pArgs) =>
                    {
                        if (pArgs.PermissionKind == CoreWebView2PermissionKind.Notifications)
                        {
                            pArgs.State = CoreWebView2PermissionState.Allow;
                            pArgs.Handled = true;
                        }
                    };

                    // WebView2標準のWeb Notification受信時ハンドリング
                    MainWebView.CoreWebView2.NotificationReceived += (sender, nArgs) =>
                    {
                        NotificationService.Instance.ShowGenericNotification(nArgs.Notification.Title, nArgs.Notification.Body);
                        nArgs.Handled = true;
                    };

                    // 通知ブリッジスクリプトをドキュメントロード時に自動注入
                    await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(NotificationBridgeScript);
                }
                else
                {
                    Logger.Info($"CoreWebView2InitializationCompleted failed: {args.InitializationException?.Message}");
                }
            };

            // WebView2からのメッセージ受信（通知連携）
            MainWebView.WebMessageReceived += (s, args) =>
            {
                try
                {
                    string json = args.WebMessageAsJson;
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("type", out var typeProp))
                    {
                        string type = typeProp.GetString() ?? "";
                        if (type == "notifications_updated")
                        {
                            int unreadCount = root.TryGetProperty("unreadCount", out var uc) ? uc.GetInt32() : 0;
                            _trayIconService.UpdateUnreadCount(unreadCount);
                            UpdateTaskbarBadge(unreadCount);

                            if (root.TryGetProperty("notifications", out var notifArray))
                            {
                                NotificationService.Instance.ProcessNotificationsJson(notifArray.GetRawText());
                            }
                        }
                        else if (type == "web_notification")
                        {
                            string title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                            string? body = root.TryGetProperty("body", out var b) ? b.GetString() : null;
                            NotificationService.Instance.ShowGenericNotification(title, body);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info($"WebMessageReceived parse error: {ex.Message}");
                }
            };

            // イベントハンドラ登録
            MainWebView.NavigationStarting += (s, args) =>
            {
                Logger.Info($"NavigationStarting: {args.Uri}");
                LoadingProgressBar.Visibility = Visibility.Visible;
            };
            MainWebView.NavigationCompleted += async (s, args) =>
            {
                Logger.Info($"NavigationCompleted: IsSuccess={args.IsSuccess}");
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                UpdateNavigationButtons();

                // 念のためナビゲーション完了後にもブリッジスクリプトを直接実行
                if (args.IsSuccess && MainWebView.CoreWebView2 != null)
                {
                    try
                    {
                        await MainWebView.CoreWebView2.ExecuteScriptAsync(NotificationBridgeScript);
                    }
                    catch { }
                }
            };
            MainWebView.SourceChanged += (s, args) => UpdateNavigationButtons();
            MainWebView.ZoomFactorChanged += (s, args) =>
            {
                ZoomLevelButton.Content = $"{Math.Round(MainWebView.ZoomFactor * 100)}%";
            };

            Logger.Info("Calling EnsureCoreWebView2Async...");
            await MainWebView.EnsureCoreWebView2Async();
            Logger.Info("EnsureCoreWebView2Async completed.");

            // バックグラウンド常駐時の新着ポーリングタイマーを開始（30秒間隔）
            StartBackgroundPolling();

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
    /// バックグラウンド常駐時でも通知を取得するための定期ポーリングタイマーを開始します。
    /// </summary>
    private void StartBackgroundPolling()
    {
        if (_backgroundPollingTimer != null) return;

        _backgroundPollingTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _backgroundPollingTimer.Tick += async (s, e) =>
        {
            if (MainWebView.CoreWebView2 != null)
            {
                try
                {
                    await MainWebView.CoreWebView2.ExecuteScriptAsync("window.loadNotifications && window.loadNotifications();");
                }
                catch { }
            }
        };
        _backgroundPollingTimer.Start();
        Logger.Info("Background notification polling timer started.");
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

    #region ウィンドウ状態とタスクトレイ連携

    /// <summary>
    /// メインウィンドウをタスクトレイまたはバックグラウンドから最前面に復元・アクティブ化します。
    /// </summary>
    public void RestoreAndActivate()
    {
        Logger.Info("RestoreAndActivate called.");

        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

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
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to restore window focus: {ex.Message}");
        }

        Activate();
        Focus();
    }

    /// <summary>
    /// タスクバーアイコンの未読バッジ（赤丸数字）を更新します。
    /// </summary>
    /// <param name="unreadCount">未読件数（0件でバッジ消去）</param>
    private void UpdateTaskbarBadge(int unreadCount)
    {
        try
        {
            if (AppTaskbarItemInfo != null)
            {
                AppTaskbarItemInfo.Overlay = TaskbarBadgeHelper.CreateBadge(unreadCount);
                Logger.Info($"Taskbar badge updated. UnreadCount={unreadCount}");
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to update taskbar badge: {ex.Message}");
        }
    }

    /// <summary>
    /// ウィンドウ最小化時：設定に応じてタスクトレイに格納（タスクバーから非表示）
    /// </summary>
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        var settings = SettingsService.Instance.CurrentSettings;
        if (WindowState == WindowState.Minimized && settings.MinimizeToTray)
        {
            Logger.Info("Window minimized to system tray.");
            Hide();
        }
    }

    /// <summary>
    /// ウィンドウ終了イベント：CloseToTray設定時はタスクトレイに常駐
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Logger.Info($"MainWindow_Closing event. IsExplicitExit={_isExplicitExit}");

        var settings = SettingsService.Instance.CurrentSettings;
        if (!_isExplicitExit && settings.CloseToTray)
        {
            // アプリを終了せずタスクトレイに格納
            e.Cancel = true;
            SaveWindowBounds();
            Hide();
            Logger.Info("Window closed to system tray.");
            return;
        }

        // アプリケーション完全終了時の後始末
        SaveWindowBounds();
        _backgroundPollingTimer?.Stop();
        _trayIconService.Dispose();
        NotificationService.Instance.NotificationClicked -= OnNotificationClicked;
        Logger.Info("MainWindow closed completely.");
    }

    /// <summary>
    /// トレイメニューからの完全終了要求
    /// </summary>
    private void ExitApplication()
    {
        Logger.Info("ExitApplication requested.");
        _isExplicitExit = true;
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// トースト通知クリック時のハンドラ：ウィンドウを復元し、通知メニューを開く
    /// </summary>
    private void OnNotificationClicked(string? argument)
    {
        Logger.Info($"OnNotificationClicked invoked. Argument={argument}");
        RestoreAndActivate();

        if (MainWebView.CoreWebView2 != null)
        {
            // Web画面上の通知ベルを開く
            MainWebView.CoreWebView2.ExecuteScriptAsync("(() => { const btn = document.getElementById('notificationBellBtn'); if (btn) btn.click(); })();");
        }
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

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (MainWebView.CoreWebView2 != null)
        {
            try
            {
                // キャッシュを無視して最新のWebリソース（HTML/JS/CSS）を強制再取得（スーパーリロード / Ctrl+F5 相当）
                await MainWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.reload", "{\"ignoreCache\":true}");
            }
            catch
            {
                MainWebView.Reload();
            }
        }
        else
        {
            MainWebView.Reload();
        }
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

        string floor = (roomId == "entertainment" || roomId == "gourmet" || roomId == "ai_art") ? "annex" : "main";

        // ルーム切り替えスクリプト: ルームボタンをクリック、または親フロアタブを開いた上でクリック
        string script = $@"
            (() => {{
                const targetBtn = document.querySelector('[data-room=""{roomId}""]');
                if (targetBtn) {{
                    const inMain = targetBtn.closest('#roomsFloorMain');
                    const inAnnex = targetBtn.closest('#roomsFloorAnnex');
                    if (inMain) {{
                        const mainTab = document.getElementById('tabFloorMain');
                        if (mainTab && mainTab.getAttribute('aria-selected') !== 'true') {{
                            mainTab.click();
                        }}
                    }} else if (inAnnex) {{
                        const annexTab = document.getElementById('tabFloorAnnex');
                        if (annexTab && annexTab.getAttribute('aria-selected') !== 'true') {{
                            annexTab.click();
                        }}
                    }}
                    targetBtn.click();
                }}

                // サイト正規ハッシュ形式 (#main/links, #annex/entertainment 等) を設定して確実に同期
                const targetHash = '#{floor}/{roomId}';
                if (window.location.hash !== targetHash) {{
                    window.location.hash = targetHash;
                }}
                return true;
            }})();
        ";

        try
        {
            string result = await MainWebView.ExecuteScriptAsync(script);
            if (result == "false" || result == "null")
            {
                MainWebView.Source = new Uri($"https://windows-podcast.com/plaza/#{floor}/{roomId}");
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
    private void RoomLinks_Click(object sender, RoutedEventArgs e) => SwitchRoom("links");
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

    #region 操作説明画面

    /// <summary>
    /// 操作説明ボタン押下時：操作説明書ウィンドウを表示
    /// </summary>
    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        var helpWin = new HelpWindow
        {
            Owner = this
        };
        helpWin.ShowDialog();
    }

    #endregion

    #region 設定画面

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsDialog();
    }

    /// <summary>
    /// 設定ダイアログを表示し、変更があればナビゲーションバーやトレイメニューに反映します。
    /// </summary>
    private void OpenSettingsDialog()
    {
        RestoreAndActivate();

        var settingsWin = new SettingsWindow(MainWebView)
        {
            Owner = this
        };

        if (settingsWin.ShowDialog() == true)
        {
            UpdateNavBarVisibility();
            _trayIconService.UpdateMenuLanguage();
        }
    }

    #endregion
}
