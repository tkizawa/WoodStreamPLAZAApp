using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WoodStreamPlaza.Services;

namespace WoodStreamPlaza.Services;

/// <summary>
/// タスクトレイアイコン（NotifyIcon）のライフサイクルおよび右クリックメニューを管理するサービス
/// </summary>
public class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ToolStripMenuItem? _openMenuItem;
    private ToolStripMenuItem? _reloadMenuItem;
    private ToolStripMenuItem? _settingsMenuItem;
    private ToolStripMenuItem? _exitMenuItem;
    private bool _isDisposed;

    /// <summary>
    /// メインウィンドウを表示・復元する要求イベント
    /// </summary>
    public event Action? OpenRequested;

    /// <summary>
    /// ページ再読み込みの要求イベント
    /// </summary>
    public event Action? ReloadRequested;

    /// <summary>
    /// 設定画面表示の要求イベント
    /// </summary>
    public event Action? SettingsRequested;

    /// <summary>
    /// アプリケーション完全終了の要求イベント
    /// </summary>
    public event Action? ExitRequested;

    /// <summary>
    /// トレイアイコンを初期化します。
    /// </summary>
    public void Initialize()
    {
        if (_notifyIcon != null) return;

        _notifyIcon = new NotifyIcon();

        // アプリアイコンのロード (app.ico)
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconPath = Path.Combine(baseDir, "app.ico");
            if (File.Exists(iconPath))
            {
                _notifyIcon.Icon = new Icon(iconPath);
            }
            else
            {
                // フォールバック: システムアプリケーションアイコン
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to load app.ico for tray icon: {ex.Message}");
            _notifyIcon.Icon = SystemIcons.Application;
        }

        // ツールチップ初期設定
        _notifyIcon.Text = "WoodStream PLAZA";

        // コンテキストメニュー（右クリックメニュー）の構築
        var contextMenu = new ContextMenuStrip();

        _openMenuItem = new ToolStripMenuItem(LocalizationService.Instance.GetString("Tray_Open"), null, (s, e) => OpenRequested?.Invoke())
        {
            Font = new Font(contextMenu.Font, System.Drawing.FontStyle.Bold)
        };
        _reloadMenuItem = new ToolStripMenuItem(LocalizationService.Instance.GetString("Tray_Reload"), null, (s, e) => ReloadRequested?.Invoke());
        _settingsMenuItem = new ToolStripMenuItem(LocalizationService.Instance.GetString("Tray_Settings"), null, (s, e) => SettingsRequested?.Invoke());
        _exitMenuItem = new ToolStripMenuItem(LocalizationService.Instance.GetString("Tray_Exit"), null, (s, e) => ExitRequested?.Invoke());

        contextMenu.Items.Add(_openMenuItem);
        contextMenu.Items.Add(_reloadMenuItem);
        contextMenu.Items.Add(_settingsMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_exitMenuItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // アイコンのクリック/ダブルクリックでウィンドウ復元
        _notifyIcon.DoubleClick += (s, e) => OpenRequested?.Invoke();
        _notifyIcon.Click += (s, e) =>
        {
            // 左クリック時のみ復帰（右クリック時はメニューが開く）
            if (e is MouseEventArgs mouseArgs && mouseArgs.Button == MouseButtons.Left)
            {
                OpenRequested?.Invoke();
            }
        };

        _notifyIcon.Visible = true;
        Logger.Info("TrayIconService initialized and visible.");
    }

    /// <summary>
    /// 言語変更時にトレイメニューの表示文言を更新します。
    /// </summary>
    public void UpdateMenuLanguage()
    {
        if (_openMenuItem != null) _openMenuItem.Text = LocalizationService.Instance.GetString("Tray_Open");
        if (_reloadMenuItem != null) _reloadMenuItem.Text = LocalizationService.Instance.GetString("Tray_Reload");
        if (_settingsMenuItem != null) _settingsMenuItem.Text = LocalizationService.Instance.GetString("Tray_Settings");
        if (_exitMenuItem != null) _exitMenuItem.Text = LocalizationService.Instance.GetString("Tray_Exit");
    }

    /// <summary>
    /// 未読件数に応じてトレイアイコンのツールチップテキストを更新します。
    /// （NotifyIcon.Text は最大63文字制限があるため切り詰め処理を含む）
    /// </summary>
    /// <param name="unreadCount">未読件数</param>
    public void UpdateUnreadCount(int unreadCount)
    {
        if (_notifyIcon == null) return;

        string text;
        if (unreadCount > 0)
        {
            string format = LocalizationService.Instance.GetString("Tray_Tooltip_Unread");
            text = string.Format(format, unreadCount);
        }
        else
        {
            text = "WoodStream PLAZA";
        }

        // WindowsのNotifyIcon.Textは最大63文字制限
        if (text.Length > 63)
        {
            text = text.Substring(0, 60) + "...";
        }

        _notifyIcon.Text = text;
    }

    /// <summary>
    /// トレイアイコンの表示/非表示を切り替えます。
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = visible;
        }
    }

    /// <summary>
    /// リソースを破棄し、トレイアイコンを削除します。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        Logger.Info("TrayIconService disposed.");
    }
}
