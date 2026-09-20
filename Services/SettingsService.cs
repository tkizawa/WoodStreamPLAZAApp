using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using WoodStreamPlaza.Models;

namespace WoodStreamPlaza.Services;

/// <summary>
/// アプリケーション設定の読み込みおよび保存を管理するサービス
/// </summary>
public class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    public static SettingsService Instance => _instance.Value;

    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 現在ロードされている設定オブジェクト
    /// </summary>
    public AppSettings CurrentSettings { get; private set; }

    private SettingsService()
    {
        // グローバル規約: 設定ファイルの保存先は AppData\Local\(アプリ名)
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appDirectory = Path.Combine(localAppData, "WoodStreamPlaza");

        // ディレクトリが存在しない場合は自動作成
        if (!Directory.Exists(appDirectory))
        {
            Directory.CreateDirectory(appDirectory);
        }

        _settingsFilePath = Path.Combine(appDirectory, "settings.json");

        // グローバル規約: 日本語はUnicodeエスケープせず、そのまま可視テキスト(UTF-8)として保存
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        CurrentSettings = Load();
    }

    /// <summary>
    /// 設定ファイルから設定情報を読み込みます。ファイルが存在しないか破損している場合は既定値を返します。
    /// </summary>
    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (settings != null)
                {
                    CurrentSettings = settings;
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"設定ファイルの読み込みに失敗しました: {ex.Message}");
        }

        // 既定値
        CurrentSettings = new AppSettings();
        return CurrentSettings;
    }

    /// <summary>
    /// 現在の設定情報を設定ファイル(settings.json)に保存します。
    /// </summary>
    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(CurrentSettings, _jsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"設定ファイルの保存に失敗しました: {ex.Message}");
        }
    }
}
