using System;
using System.Windows;
using WoodStreamPlaza.Services;
using WpfApplication = System.Windows.Application;

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
