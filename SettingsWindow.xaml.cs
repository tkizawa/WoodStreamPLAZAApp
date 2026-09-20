using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using WoodStreamPlaza.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace WoodStreamPlaza;

/// <summary>
/// アプリケーション設定ウィンドウの相互作用ロジック
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly WebView2? _webView;

    public SettingsWindow(WebView2? webView)
    {
        InitializeComponent();
        _webView = webView;

        LoadCurrentSettings();
    }

    /// <summary>
    /// 現在の設定値をコントロールに反映します。
    /// </summary>
    private void LoadCurrentSettings()
    {
        var settings = SettingsService.Instance.CurrentSettings;

        // 言語コンボボックスの選択
        foreach (ComboBoxItem item in LanguageComboBox.Items)
        {
            if (item.Tag?.ToString() == settings.Language)
            {
                LanguageComboBox.SelectedItem = item;
                break;
            }
        }
        if (LanguageComboBox.SelectedItem == null)
        {
            LanguageComboBox.SelectedIndex = 0;
        }

        // 起動時URL
        StartUrlTextBox.Text = settings.StartUrl;

        // 動作チェックボックス
        MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;
        CloseToTrayCheckBox.IsChecked = settings.CloseToTray;
        AutoStartCheckBox.IsChecked = settings.AutoStart || StartupService.IsAutoStartEnabled();
        StartMinimizedCheckBox.IsChecked = settings.StartMinimized;
        EnableNotificationsCheckBox.IsChecked = settings.EnableNotifications;
        ShowNavBarCheckBox.IsChecked = settings.ShowNavigationBar;
    }

    /// <summary>
    /// 保存ボタン押下時：設定を反映して保存します。
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = SettingsService.Instance.CurrentSettings;

        if (LanguageComboBox.SelectedItem is ComboBoxItem selectedLang)
        {
            settings.Language = selectedLang.Tag?.ToString() ?? "auto";
            LocalizationService.Instance.ApplyLanguage(settings.Language);
        }

        string url = StartUrlTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(url))
        {
            settings.StartUrl = url;
        }

        settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;
        settings.CloseToTray = CloseToTrayCheckBox.IsChecked ?? false;
        settings.AutoStart = AutoStartCheckBox.IsChecked ?? false;
        settings.StartMinimized = StartMinimizedCheckBox.IsChecked ?? false;
        settings.EnableNotifications = EnableNotificationsCheckBox.IsChecked ?? true;
        settings.ShowNavigationBar = ShowNavBarCheckBox.IsChecked ?? true;

        // Windowsスタートアップ登録の同期
        StartupService.SetAutoStart(settings.AutoStart, settings.StartMinimized);

        SettingsService.Instance.Save();
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// キャンセルボタン押下時
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// ブラウザキャッシュクリアボタン押下時
    /// </summary>
    private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
    {
        if (_webView?.CoreWebView2 != null)
        {
            try
            {
                ClearCacheButton.IsEnabled = false;
                await _webView.CoreWebView2.Profile.ClearBrowsingDataAsync();

                string doneMessage = LocalizationService.Instance.GetString("Settings_ClearCache_Done");
                WpfMessageBox.Show(this, doneMessage, "WoodStream PLAZA", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(this, $"キャッシュのクリアに失敗しました: {ex.Message}", "WoodStream PLAZA", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                ClearCacheButton.IsEnabled = true;
            }
        }
        else
        {
            WpfMessageBox.Show(this, "ブラウザコンポーネントが初期化されていません。", "WoodStream PLAZA", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
