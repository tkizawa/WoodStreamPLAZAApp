using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Toolkit.Uwp.Notifications;
using WoodStreamPlaza.Services;

namespace WoodStreamPlaza.Services;

/// <summary>
/// WoodStream PLAZA の通知アイテム構造体
/// </summary>
public class PlazaNotificationItem
{
    public string? id { get; set; }
    public string? type { get; set; }
    public string? sender_name { get; set; }
    public string? sender_provider { get; set; }
    public string? body { get; set; }
    public string? post_id { get; set; }
    public string? room { get; set; }
    public bool is_read { get; set; }
}

/// <summary>
/// Windowsネイティブトースト通知の生成および通知クリックの連携を行うサービス
/// </summary>
public class NotificationService
{
    private static NotificationService? _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();

    private readonly HashSet<string> _knownNotificationIds = new();
    private bool _isFirstRun = true;

    /// <summary>
    /// トースト通知がユーザーによってクリックされたときのイベント
    /// </summary>
    public event Action<string?>? NotificationClicked;

    private NotificationService()
    {
        try
        {
            // トースト通知のアクティブ化ハンドラを登録
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // トーストクリック時: UIスレッドでイベントを発火
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    Logger.Info($"ToastNotification activated. Argument: {toastArgs.Argument}");
                    NotificationClicked?.Invoke(toastArgs.Argument);
                });
            };
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to subscribe to ToastNotificationManagerCompat.OnActivated: {ex.Message}");
        }
    }

    /// <summary>
    /// Webサイトから取得した未読通知リストを処理し、新着があればトースト通知を表示します。
    /// </summary>
    /// <param name="notificationsJson">JSON文字列</param>
    public void ProcessNotificationsJson(string notificationsJson)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<PlazaNotificationItem>>(notificationsJson, options);
            if (list != null)
            {
                ProcessNotifications(list);
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"ProcessNotificationsJson parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// 未読通知リストを処理し、新着に対してトースト通知を発行します。
    /// </summary>
    public void ProcessNotifications(IEnumerable<PlazaNotificationItem> notifications)
    {
        var settings = SettingsService.Instance.CurrentSettings;

        // 初回実行時は、既存の通知を既知としてマークし大量のトースト通知の同時ポップアップを防止
        if (_isFirstRun)
        {
            foreach (var item in notifications)
            {
                if (!string.IsNullOrEmpty(item.id))
                {
                    _knownNotificationIds.Add(item.id);
                }
            }
            _isFirstRun = false;
            Logger.Info($"NotificationService initialized. Initial known items: {_knownNotificationIds.Count}");
            return;
        }

        // 新着通知のチェック
        foreach (var item in notifications)
        {
            if (string.IsNullOrEmpty(item.id) || item.is_read) continue;

            if (!_knownNotificationIds.Contains(item.id))
            {
                _knownNotificationIds.Add(item.id);

                // 設定で通知が有効化されている場合のみトースト通知を表示
                if (settings.EnableNotifications)
                {
                    ShowToastNotification(item);
                }
            }
        }
    }

    /// <summary>
    /// 単一の通知アイテムを元に Windows トースト通知を表示します。
    /// </summary>
    public void ShowToastNotification(PlazaNotificationItem item)
    {
        try
        {
            string sender = !string.IsNullOrWhiteSpace(item.sender_name) ? item.sender_name : "ユーザー";
            string actionText;

            switch (item.type)
            {
                case "reply":
                    actionText = LocalizationService.Instance.GetString("Notif_Reply");
                    break;
                case "like":
                    actionText = LocalizationService.Instance.GetString("Notif_Like");
                    break;
                case "mention":
                default:
                    actionText = LocalizationService.Instance.GetString("Notif_Mention");
                    break;
            }

            string title = $"{sender}{actionText}";
            string body = !string.IsNullOrWhiteSpace(item.body) ? item.body : "WoodStream PLAZA";

            // アプリケーションアイコンの絶対URIを取得 (app.ico または icon.png)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconFile = Path.Combine(baseDir, "app.ico");
            Uri? iconUri = File.Exists(iconFile) ? new Uri(iconFile) : null;

            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .AddArgument("postId", item.post_id ?? "")
                .AddArgument("room", item.room ?? "");

            if (iconUri != null)
            {
                builder.AddAppLogoOverride(iconUri, ToastGenericAppLogoCrop.Circle);
            }

            builder.Show();
            Logger.Info($"Toast notification shown for item id={item.id}, type={item.type}, sender={sender}");
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to show toast notification: {ex.Message}");
        }
    }

    /// <summary>
    /// 汎用の手動トースト通知（HTML5 Notification インターセプト等用）を表示します。
    /// </summary>
    public void ShowGenericNotification(string title, string? body)
    {
        try
        {
            var settings = SettingsService.Instance.CurrentSettings;
            if (!settings.EnableNotifications) return;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconFile = Path.Combine(baseDir, "app.ico");
            Uri? iconUri = File.Exists(iconFile) ? new Uri(iconFile) : null;

            var builder = new ToastContentBuilder()
                .AddText(title);

            if (!string.IsNullOrWhiteSpace(body))
            {
                builder.AddText(body);
            }

            if (iconUri != null)
            {
                builder.AddAppLogoOverride(iconUri, ToastGenericAppLogoCrop.Circle);
            }

            builder.Show();
            Logger.Info($"Generic toast notification shown: {title}");
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to show generic toast notification: {ex.Message}");
        }
    }
}
