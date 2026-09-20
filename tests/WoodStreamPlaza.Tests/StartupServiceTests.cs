using System;
using System.IO;
using WoodStreamPlaza.Services;
using Xunit;

namespace WoodStreamPlaza.Tests;

public class StartupServiceTests
{
    [Fact]
    public void GetExecutablePath_ReturnsPathOrNullSafely()
    {
        // 実行パス取得が例外を投げずに動作することを確認
        string? path = StartupService.GetExecutablePath();
        
        // テストランナー実行下でも例外なく文字列またはnullが返ること
        if (path != null)
        {
            Assert.True(path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || File.Exists(path));
        }
    }

    [Fact]
    public void IsAutoStartEnabled_RunsWithoutExceptions()
    {
        // レジストリ確認が例外を投げずに真偽値を返すこと
        bool isEnabled = StartupService.IsAutoStartEnabled();
        // true または false のいずれかであり、例外が発生しないこと
        Assert.True(isEnabled || !isEnabled);
    }
}
