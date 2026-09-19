using System.Windows;

namespace WoodStreamPlaza.Models;

/// <summary>
/// アプリケーションの設定情報を保持するデータモデル
/// </summary>
public class AppSettings
{
    /// <summary>
    /// メインウィンドウの表示位置（X座標）
    /// </summary>
    public double? WindowLeft { get; set; } = null;

    /// <summary>
    /// メインウィンドウの表示位置（Y座標）
    /// </summary>
    public double? WindowTop { get; set; } = null;

    /// <summary>
    /// メインウィンドウの幅（初期値: 1100）
    /// </summary>
    public double WindowWidth { get; set; } = 1100;

    /// <summary>
    /// メインウィンドウの高さ（初期値: 800）
    /// </summary>
    public double WindowHeight { get; set; } = 800;

    /// <summary>
    /// メインウィンドウの表示状態（通常、最大化など）
    /// </summary>
    public WindowState WindowState { get; set; } = WindowState.Normal;

    /// <summary>
    /// 起動時に表示するURL
    /// </summary>
    public string StartUrl { get; set; } = "https://windows-podcast.com/plaza/";

    /// <summary>
    /// 最小化時にシステムトレイ（タスクトレイ）に格納するかどうか
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// ウィンドウの「閉じる」ボタン押下時に終了せずシステムトレイに格納するかどうか
    /// </summary>
    public bool CloseToTray { get; set; } = false;

    /// <summary>
    /// アプリケーションの表示言語設定（\"auto\": OSに合わせる, \"ja-JP\": 日本語, \"en-US\": 英語）
    /// </summary>
    public string Language { get; set; } = "auto";

    /// <summary>
    /// アプリ上部のクイックナビゲーションバーを表示するかどうか
    /// </summary>
    public bool ShowNavigationBar { get; set; } = true;
}
