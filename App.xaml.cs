using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using WoodStreamPlaza.Services;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

namespace WoodStreamPlaza;

/// <summary>
/// アプリケーションのエントリおよびライフサイクル管理
/// </summary>
public partial class App : WpfApplication
{
    private const string MutexName = @"Local\WoodStreamPlaza_SingleInstance_Mutex_8F7D9364";
    private const string ActivateEventName = @"Local\WoodStreamPlaza_SingleInstance_ActivateEvent_8F7D9364";

    private Mutex? _mutex;
    private EventWaitHandle? _activateEvent;
    private RegisteredWaitHandle? _registeredWait;
    private bool _hasHandle = false;

    protected override void OnStartup(StartupEventArgs e)
    {
        Logger.Info("Application OnStartup started.");

        // 重複起動（多重起動）防止チェック
        try
        {
            _mutex = new Mutex(true, MutexName, out _hasHandle);
        }
        catch (AbandonedMutexException)
        {
            // 前のインスタンスが異常終了してミューテックスを解放しなかった場合、所有権を取得可能
            _hasHandle = true;
        }
        catch (Exception ex)
        {
            Logger.Info($"Mutex creation failed: {ex.Message}");
        }

        // すでにインスタンスが存在する場合は前面表示を通知して終了
        if (!_hasHandle)
        {
            Logger.Info("Another instance is already running. Notifying existing instance and exiting.");
            try
            {
                if (EventWaitHandle.TryOpenExisting(ActivateEventName, out var existingEvent))
                {
                    existingEvent.Set();
                    existingEvent.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Info($"Failed to signal existing instance: {ex.Message}");
            }

            Shutdown();
            return;
        }

        // 最初のインスタンス：多重起動通知を受け取った際に前面化するイベントリスナーを登録
        try
        {
            _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
            _registeredWait = ThreadPool.RegisterWaitForSingleObject(
                _activateEvent,
                (state, timedOut) =>
                {
                    if (!timedOut)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            Logger.Info("Activation request received from another instance.");
                            if (MainWindow is MainWindow mainWindow)
                            {
                                mainWindow.RestoreAndActivate();
                            }
                        });
                    }
                },
                null,
                -1,
                false);
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to setup activate event listener: {ex.Message}");
        }

        base.OnStartup(e);

        // GPU/DirectXドライバやマルチモニターの競合によるウィンドウ透明化・非表示バグを回避
        System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

        // 未処理例外ハンドリング
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Logger.Info($"AppDomain UnhandledException: {args.ExceptionObject}");
            WpfMessageBox.Show($"致命的なエラーが発生しました: {args.ExceptionObject}", "WoodStream PLAZA", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            Logger.Info($"DispatcherUnhandledException: {args.Exception.Message}\n{args.Exception.StackTrace}");
            WpfMessageBox.Show($"エラーが発生しました: {args.Exception.Message}", "WoodStream PLAZA", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // 設定のロードおよび言語の適用
        var settings = SettingsService.Instance.CurrentSettings;
        Logger.Info($"Settings loaded. StartUrl: {settings.StartUrl}, Language: {settings.Language}");
        LocalizationService.Instance.ApplyLanguage(settings.Language);
        Logger.Info("Language applied.");

        // 起動時最小化（タスクトレイ格納）の判定（コマンドライン引数または設定値）
        bool startMinimized = settings.StartMinimized;
        if (e.Args != null && e.Args.Length > 0)
        {
            foreach (var arg in e.Args)
            {
                if (string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-minimized", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "/minimized", StringComparison.OrdinalIgnoreCase))
                {
                    startMinimized = true;
                    Logger.Info("Started with --minimized command line argument.");
                    break;
                }
            }
        }

        // メインウィンドウの明示的生成と表示
        try
        {
            var mainWindow = new MainWindow(startMinimized);
            MainWindow = mainWindow;

            if (startMinimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.Show();
                mainWindow.Hide();
                Logger.Info("MainWindow initialized and running in minimized (tray) mode.");
            }
            else
            {
                mainWindow.Show();
                mainWindow.Activate();
                Logger.Info("MainWindow shown and activated successfully.");
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to create or show MainWindow: {ex.Message}\n{ex.StackTrace}");
            WpfMessageBox.Show($"メインウィンドウの表示に失敗しました: {ex.Message}", "WoodStream PLAZA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application OnExit started.");

        // イベントリスナーの登録解除
        if (_registeredWait != null)
        {
            _registeredWait.Unregister(null);
            _registeredWait = null;
        }

        if (_activateEvent != null)
        {
            _activateEvent.Dispose();
            _activateEvent = null;
        }

        // ミューテックスの解放
        if (_mutex != null)
        {
            if (_hasHandle)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    Logger.Info($"Error releasing mutex: {ex.Message}");
                }
            }
            _mutex.Dispose();
            _mutex = null;
        }

        base.OnExit(e);
        Logger.Info("Application OnExit completed.");
    }
}

