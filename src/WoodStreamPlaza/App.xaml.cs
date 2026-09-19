using System;
using System.Threading;
using System.Windows;
using WoodStreamPlaza.Services;
using WpfApplication = System.Windows.Application;

namespace WoodStreamPlaza;

/// <summary>
/// アプリケーションのエントリおよびライフサイクル管理（多重起動防止＆前面復帰対応）
/// </summary>
public partial class App : WpfApplication
{
    private const string AppUniqueId = "WoodStreamPlaza_Unique_App_Mutex_2026";
    private const string WakeupEventName = "WoodStreamPlaza_Wakeup_Event_2026";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _wakeupEvent;
    private Thread? _wakeupThread;
    private bool _isStopping = false;

    protected override void OnStartup(StartupEventArgs e)
    {
        Logger.Info("Application OnStartup started.");

        // 多重起動の検知
        _singleInstanceMutex = new Mutex(true, AppUniqueId, out bool isNewInstance);

        if (!isNewInstance)
        {
            Logger.Info("Another instance is already running. Signaling wakeup event and exiting.");
            try
            {
                // 既存のインスタンスに起床シグナルを送信
                using var existingEvent = EventWaitHandle.OpenExisting(WakeupEventName);
                existingEvent.Set();
            }
            catch (Exception ex)
            {
                Logger.Info($"Failed to signal wakeup event: {ex.Message}");
            }

            // 二重起動したプロセスは即座に終了
            Shutdown();
            return;
        }

        // 新規インスタンス：起床イベントを作成して待機スレッドを開始
        try
        {
            _wakeupEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WakeupEventName);
            _wakeupThread = new Thread(ListenForWakeup)
            {
                IsBackground = true
            };
            _wakeupThread.Start();
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to create wakeup event: {ex.Message}");
        }

        base.OnStartup(e);

        // 未処理例外ハンドリング
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Logger.Info($"AppDomain UnhandledException: {args.ExceptionObject}");
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            Logger.Info($"DispatcherUnhandledException: {args.Exception.Message}\n{args.Exception.StackTrace}");
            args.Handled = true;
        };

        // 設定のロードおよび言語の適用
        var settings = SettingsService.Instance.CurrentSettings;
        Logger.Info($"Settings loaded. StartUrl: {settings.StartUrl}, Language: {settings.Language}");
        LocalizationService.Instance.ApplyLanguage(settings.Language);
        Logger.Info("Language applied.");
    }

    /// <summary>
    /// 別プロセスからの起動シグナルを受信した際にメインウィンドウを画面前面に復帰させます
    /// </summary>
    private void ListenForWakeup()
    {
        while (!_isStopping && _wakeupEvent != null)
        {
            try
            {
                if (_wakeupEvent.WaitOne())
                {
                    if (_isStopping) break;

                    Logger.Info("Wakeup signal received! Restoring main window to foreground.");
                    Dispatcher.Invoke(() =>
                    {
                        TrayService.Instance.ShowMainWindow();
                    });
                }
            }
            catch
            {
                break;
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isStopping = true;
        try
        {
            _wakeupEvent?.Set();
            _wakeupEvent?.Dispose();
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
        }
        catch { }

        base.OnExit(e);
    }
}
