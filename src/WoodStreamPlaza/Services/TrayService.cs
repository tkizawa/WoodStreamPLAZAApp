using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace WoodStreamPlaza.Services;

/// <summary>
/// タスクトレイ（システムトレイ）アイコンおよび通知を管理するサービス
/// </summary>
public class TrayService : IDisposable
{
    private static readonly Lazy<TrayService> _instance = new(() => new TrayService());
    public static TrayService Instance => _instance.Value;

    private NotifyIcon? _notifyIcon;
    private Window? _mainWindow;
    private Action? _onOpenSettings;

    private TrayService() { }

    /// <summary>
    /// トレイアイコンを初期化します。
    /// </summary>
    public void Initialize(Window mainWindow, Action onOpenSettings)
    {
        _mainWindow = mainWindow;
        _onOpenSettings = onOpenSettings;

        _notifyIcon = new NotifyIcon();

        // アイコンの設定 (実行ディレクトリまたは埋め込みから)
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                _notifyIcon.Icon = new Icon(iconPath);
            }
            else
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }
        catch
        {
            _notifyIcon.Icon = SystemIcons.Application;
        }

        _notifyIcon.Text = "WoodStream PLAZA";
        _notifyIcon.Visible = true;

        // ダブルクリックでウィンドウ復元
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        // コンテキストメニュー構築
        UpdateContextMenu();
    }

    /// <summary>
    /// 言語変更時などにコンテキストメニューの表示テキストを更新します。
    /// </summary>
    public void UpdateContextMenu()
    {
        if (_notifyIcon == null) return;

        var menu = new ContextMenuStrip();

        string openText = LocalizationService.Instance.GetString("Tray_Open");
        string settingsText = LocalizationService.Instance.GetString("Tray_Settings");
        string exitText = LocalizationService.Instance.GetString("Tray_Exit");

        var openItem = new ToolStripMenuItem(openText, null, (s, e) => ShowMainWindow());
        openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);

        var settingsItem = new ToolStripMenuItem(settingsText, null, (s, e) => _onOpenSettings?.Invoke());
        var exitItem = new ToolStripMenuItem(exitText, null, (s, e) => ExitApplication());

        menu.Items.Add(openItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    /// <summary>
    /// メインウィンドウを表示して最前面にアクティブ化します。
    /// </summary>
    public void ShowMainWindow()
    {
        if (_mainWindow == null) return;

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        // 最前面に強制アクティブ化
        _mainWindow.Topmost = true;
        _mainWindow.Activate();
        _mainWindow.Focus();
        _mainWindow.Topmost = false;
    }

    /// <summary>
    /// トレイに格納された際の通知を表示します。
    /// </summary>
    public void ShowTrayNotification(string title, string message)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
    }

    /// <summary>
    /// アプリケーションを完全に終了します。
    /// </summary>
    public void ExitApplication()
    {
        Dispose();
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
