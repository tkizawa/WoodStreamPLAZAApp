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
        base.OnStartup(e);

        // 未処理例外ハンドリング
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"AppDomain UnhandledException: {args.ExceptionObject}");
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"DispatcherUnhandledException: {args.Exception.Message}");
            args.Handled = true;
        };

        // 設定のロードおよび言語の適用
        var settings = SettingsService.Instance.CurrentSettings;
        LocalizationService.Instance.ApplyLanguage(settings.Language);
    }
}
