using System;
using System.Globalization;
using System.Windows;
using WpfApplication = System.Windows.Application;

namespace WoodStreamPlaza.Services;

/// <summary>
/// アプリケーションの多言語表示（日本語・英語）を管理するサービス
/// </summary>
public class LocalizationService
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Instance => _instance.Value;

    private LocalizationService() { }

    /// <summary>
    /// 指定された言語コード（または"auto"）に基づいてリソース辞書を適用します。
    /// </summary>
    /// <param name="languageCode">"auto", "ja-JP", または "en-US"</param>
    public void ApplyLanguage(string languageCode)
    {
        string targetCulture = languageCode;

        if (string.IsNullOrEmpty(targetCulture) || targetCulture.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            // Windowsの表示言語モード（CurrentUICulture）に従う
            var currentUi = CultureInfo.CurrentUICulture.Name;
            if (currentUi.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            {
                targetCulture = "ja-JP";
            }
            else
            {
                targetCulture = "en-US";
            }
        }

        string resourcePath = targetCulture.StartsWith("ja", StringComparison.OrdinalIgnoreCase)
            ? "Resources/Strings.ja-JP.xaml"
            : "Resources/Strings.en-US.xaml";

        try
        {
            var dictUri = new Uri(resourcePath, UriKind.Relative);
            var resourceDict = new ResourceDictionary { Source = dictUri };

            // 既存の言語辞書を置き換え
            var appResources = WpfApplication.Current.Resources;
            var mergedDicts = appResources.MergedDictionaries;

            ResourceDictionary? existingLangDict = null;
            foreach (var dict in mergedDicts)
            {
                if (dict.Source != null && dict.Source.OriginalString.Contains("Strings."))
                {
                    existingLangDict = dict;
                    break;
                }
            }

            if (existingLangDict != null)
            {
                mergedDicts.Remove(existingLangDict);
            }

            mergedDicts.Add(resourceDict);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"言語リソースの適用に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 現在適用されているリソースから指定されたキーの文字列を取得します。
    /// </summary>
    public string GetString(string key)
    {
        if (WpfApplication.Current.Resources.Contains(key))
        {
            return WpfApplication.Current.Resources[key]?.ToString() ?? key;
        }
        return key;
    }
}
