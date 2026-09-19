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
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string SenderProvider { get; set; } = "";
    public string MessageExcerpt { get; set; } = "";
    public string Room { get; set; } = "";
    public string Floor { get; set; } = "";
    public string TargetPostId { get; set; } = "";
    public string TargetCommentId { get; set; } = "";
    public bool IsRead { get; set; }

    /// <summary>
    /// JsonElement から型（数値・文字列・ブール）の差異を吸収して安全にパースします。
    /// </summary>
    public static PlazaNotificationItem FromJsonElement(JsonElement el)
    {
        var item = new PlazaNotificationItem
        {
            Id = GetStringOrNumber(el, "id"),
            Type = GetString(el, "type"),
            SenderName = GetString(el, "sender_name"),
            SenderProvider = GetString(el, "sender_provider"),
            MessageExcerpt = GetString(el, "message_excerpt"),
            Room = GetString(el, "room"),
            Floor = GetString(el, "floor"),
            TargetPostId = GetStringOrNumber(el, "target_post_id"),
            TargetCommentId = GetStringOrNumber(el, "target_comment_id"),
            IsRead = GetBoolean(el, "is_read")
        };

        if (string.IsNullOrEmpty(item.MessageExcerpt))
        {
            item.MessageExcerpt = GetString(el, "body");
        }
        if (string.IsNullOrEmpty(item.TargetPostId))
        {
            item.TargetPostId = GetStringOrNumber(el, "post_id");
        }

        return item;
    }

    private static string GetString(JsonElement el, string propertyName)
    {
        if (el.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? "";
        }
        return "";
    }

    private static string GetStringOrNumber(JsonElement el, string propertyName)
    {
        if (el.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String) return prop.GetString() ?? "";
            if (prop.ValueKind == JsonValueKind.Number) return prop.GetRawText();
        }
        return "";
    }

    private static bool GetBoolean(JsonElement el, string propertyName)
    {
        if (el.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.True) return true;
            if (prop.ValueKind == JsonValueKind.False) return false;
            if (prop.ValueKind == JsonValueKind.Number) return prop.GetInt32() != 0;
            if (prop.ValueKind == JsonValueKind.String)
            {
                string s = prop.GetString() ?? "";
                return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }
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
            using var doc = JsonDocument.Parse(notificationsJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var list = new List<PlazaNotificationItem>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    list.Add(PlazaNotificationItem.FromJsonElement(el));
                }
                Logger.Info($"Parsed {list.Count} notifications from JSON.");
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

        // 初回実行時
        if (_isFirstRun)
        {
            _isFirstRun = false;

            // 既存の未読通知を抽出
            var unreadList = new List<PlazaNotificationItem>();
            foreach (var item in notifications)
            {
                if (!string.IsNullOrEmpty(item.Id))
                {
                    _knownNotificationIds.Add(item.Id);
                    if (!item.IsRead)
                    {
                        unreadList.Add(item);
                    }
                }
            }

            Logger.Info($"NotificationService initialized. Known items: {_knownNotificationIds.Count}, Initial unreads: {unreadList.Count}");

            // 起動時に未読通知がある場合、最新の通知をトースト表示
            if (settings.EnableNotifications && unreadList.Count > 0)
            {
                var latest = unreadList[0];
                Logger.Info($"Showing initial unread toast for ID={latest.Id}, Sender={latest.SenderName}");
                ShowToastNotification(latest);
            }
            return;
        }

        // 新着通知のチェック
        foreach (var item in notifications)
        {
            if (string.IsNullOrEmpty(item.Id) || item.IsRead) continue;

            if (!_knownNotificationIds.Contains(item.Id))
            {
                _knownNotificationIds.Add(item.Id);
                Logger.Info($"New unread notification detected: ID={item.Id}, Type={item.Type}, Sender={item.SenderName}");

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
            string sender = !string.IsNullOrWhiteSpace(item.SenderName) ? item.SenderName : "ユーザー";
            string actionText;

            switch (item.Type)
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
            string body = !string.IsNullOrWhiteSpace(item.MessageExcerpt) ? item.MessageExcerpt : "WoodStream PLAZA";

            // アプリケーションアイコンの絶対URIを取得 (app.ico)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconFile = Path.Combine(baseDir, "app.ico");
            Uri? iconUri = File.Exists(iconFile) ? new Uri(iconFile) : null;

            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .AddArgument("postId", item.TargetPostId)
                .AddArgument("room", item.Room);

            if (iconUri != null)
            {
                builder.AddAppLogoOverride(iconUri, ToastGenericAppLogoCrop.Circle);
            }

            builder.Show();
            Logger.Info($"Toast notification successfully sent to OS: ID={item.Id}, Type={item.Type}, Sender={sender}");
        }
        catch (Exception ex)
        {
            Logger.Info($"Failed to show toast notification: {ex.Message}\n{ex.StackTrace}");
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
