using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using WoodStreamPlaza.Services;
using WpfApplication = System.Windows.Application;

namespace WoodStreamPlaza;

/// <summary>
/// アプリケーションのエントリおよびライフサイクル管理
/// </summary>
public partial class App : WpfApplication
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    protected override void OnStartup(StartupEventArgs e)
    {
        Logger.Info("Application OnStartup started.");

        // 既存プロセスの安全な多重起動チェック
        var currentProcess = Process.GetCurrentProcess();
        var runningProcesses = Process.GetProcessesByName(currentProcess.ProcessName);

        Process? otherProcess = null;
        foreach (var p in runningProcesses)
        {
            if (p.Id != currentProcess.Id)
            {
                otherProcess = p;
                break;
            }
        }

        if (otherProcess != null)
        {
            Logger.Info($"Existing process found (PID: {otherProcess.Id}). Bringing it to foreground and exiting.");
            try
            {
                IntPtr hWnd = otherProcess.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                }
            }
            catch (Exception ex)
            {
                Logger.Info($"Error bringing window to foreground: {ex.Message}");
            }

            Shutdown();
            return;
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
}
