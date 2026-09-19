using System;
using System.Collections.Generic;
using WoodStreamPlaza.Models;
using WoodStreamPlaza.Services;
using Xunit;

namespace WoodStreamPlaza.Tests;

public class NotificationTests
{
    [Fact]
    public void AppSettings_DefaultEnableNotifications_ShouldBeTrue()
    {
        var settings = new AppSettings();
        Assert.True(settings.EnableNotifications);
        Assert.True(settings.MinimizeToTray);
        Assert.False(settings.CloseToTray);
    }

    [Fact]
    public void NotificationJsonParsing_ShouldParseValidList()
    {
        string json = @"[
            {
                ""id"": ""101"",
                ""type"": ""reply"",
                ""sender_name"": ""テスト太郎"",
                ""sender_provider"": ""microsoft"",
                ""body"": ""こんにちは、返信です。"",
                ""post_id"": ""50"",
                ""room"": ""lounge"",
                ""is_read"": false
            },
            {
                ""id"": ""102"",
                ""type"": ""like"",
                ""sender_name"": ""花子"",
                ""sender_provider"": ""google"",
                ""body"": """",
                ""post_id"": ""50"",
                ""room"": ""lounge"",
                ""is_read"": false
            }
        ]";

        // NotificationService の JSON 処理で例外が発生しないことを確認
        var exception = Record.Exception(() =>
        {
            NotificationService.Instance.ProcessNotificationsJson(json);
        });

        Assert.Null(exception);
    }
}
