using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using WoodStreamPlaza.Models;
using WoodStreamPlaza.Services;
using Xunit;

namespace WoodStreamPlaza.Tests;

public class SettingsTests
{
    [Fact]
    public void SettingsFile_ShouldBeSavedIn_LocalAppData_WoodStreamPlaza()
    {
        // AppData\Local\WoodStreamPlaza\settings.json のパス確認
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string expectedPath = Path.Combine(localAppData, "WoodStreamPlaza", "settings.json");

        var service = SettingsService.Instance;
        service.CurrentSettings.StartUrl = "https://windows-podcast.com/plaza/";
        service.Save();

        Assert.True(File.Exists(expectedPath), $"設定ファイルが存在しません: {expectedPath}");
    }

    [Fact]
    public void SettingsFile_JapaneseText_ShouldNotBeUnicodeEscaped()
    {
        // グローバル規約: 設定ファイル内の日本語はUnicodeエスケープせず、そのまま可視テキスト（UTF-8）として保存
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string settingsPath = Path.Combine(localAppData, "WoodStreamPlaza", "settings.json");

        var service = SettingsService.Instance;
        service.CurrentSettings.Language = "ja-JP";
        service.Save();

        string jsonContent = File.ReadAllText(settingsPath);

        // \uXXXX 形式のエスケープが含まれていないことを確認
        bool hasUnicodeEscape = Regex.IsMatch(jsonContent, @"\\u[0-9a-fA-F]{4}");
        Assert.False(hasUnicodeEscape, "設定ファイル内のテキストが \\uXXXX にUnicodeエスケープされています。可視テキストとして保存される必要があります。");
    }

    [Fact]
    public void WindowBounds_ShouldBeSavedAndRestoredProperly()
    {
        // ウィンドウ位置・サイズの保存と読み込みテスト
        var service = SettingsService.Instance;
        service.CurrentSettings.WindowLeft = 150;
        service.CurrentSettings.WindowTop = 120;
        service.CurrentSettings.WindowWidth = 1024;
        service.CurrentSettings.WindowHeight = 768;
        service.CurrentSettings.WindowState = WindowState.Normal;
        service.Save();

        var reloaded = service.Load();
        Assert.Equal(150, reloaded.WindowLeft);
        Assert.Equal(120, reloaded.WindowTop);
        Assert.Equal(1024, reloaded.WindowWidth);
        Assert.Equal(768, reloaded.WindowHeight);
        Assert.Equal(WindowState.Normal, reloaded.WindowState);
    }
}
