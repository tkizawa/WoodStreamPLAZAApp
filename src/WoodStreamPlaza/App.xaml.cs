using System;
using System.Diagnostics;
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
    protected override void OnStartup(StartupEventArgs e)
    {
        Logger.Info("Application OnStartup started.");
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

        // メインウィンドウの明示的生成と表示（確実な表示を保証）
        try
        {
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Activate();
            Logger.Info("MainWindow shown and activated successfully.");
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to create or show MainWindow: {ex.Message}\n{ex.StackTrace}");
            WpfMessageBox.Show($"メインウィンドウの表示に失敗しました: {ex.Message}", "WoodStream PLAZA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
