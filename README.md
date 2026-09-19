# WoodStream PLAZA Desktop Client

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-0078D7)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![WebView2](https://img.shields.io/badge/Engine-WebView2-008AD7?logo=microsoftedge)](https://developer.microsoft.com/microsoft-edge/webview2/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

ポッドキャスト番組「**WoodStreamのデジタル生活**」のリスナーコミュニティ「[WoodStream PLAZA](https://windows-podcast.com/plaza/)」を、Windows デスクトップ環境で快適に利用するための専用クライアントアプリケーションです。

---

## 🌟 主な特徴

### 1. 🔔 Windows ネイティブトースト通知
- メンション（`@表示名`）、返信、いいね！などの新着アクティビティを検知し、Windows 10 / 11 のネイティブトースト通知で画面右下にポップアップ表示します。
- 通知をクリックすると、バックグラウンドやタスクトレイに格納中であってもウィンドウが最前面に復元し、該当の投稿や通知メニューを即座に開きます。

### 2. 🔴 タスクバー未読バッジ
- アプリがタスクバーに表示されている際、未読件数（1〜99+）に応じた赤いバッジ（オーバーレイ）がアイコン右下に自動表示されます。
- すべて既読にするとバッジは自動的に消去されます。

### 3. 📥 タスクトレイ常駐（Discord / Teams 風の挙動）
- **最小化時**: タスクバーから隠してタスクトレイにコンパクトに格納。
- **閉じるボタン（×）押下時**: アプリを終了せずタスクトレイに常駐し、バックグラウンドで新着通知の監視を継続（設定でオン/オフ可能）。
- **トレイ右クリックメニュー**: 「開く」「ページの再読み込み」「設定...」「終了」を素早く操作可能。未読件数もツールチップに反映されます。

### 4. 🧭 クイックナビゲーションバー
- 本館（お知らせ・ラウンジ・番組感想）および別館 木澤屋（エンタメ・食テロ/日常・AI生成画像）の各ルームへワンクリックでアクセス。
- ブラウザ操作（戻る・進む・更新・ホーム）、画面表示倍率のズームイン/アウト/リセット（50%〜300%）に対応。

### 5. 🌐 多言語対応 & 設定の永続化
- **多言語対応**: Windows の表示言語設定に従い、日本語・英語を自動切り替え（手動指定も可能）。
- **ウィンドウ状態復元**: 終了時のウィンドウサイズおよび表示位置を `AppData\Local\WoodStreamPlaza` に保存し、次回起動時に正確に復元。マルチモニターや仮想デスクトップでの画面外表示を防ぐ安全設計です。
- **キャッシュ管理**: セッション維持やCookie分離、設定画面からのワンクリックキャッシュクリアに対応。

---

## 💻 動作要件

| 項目 | 要件 |
| :--- | :--- |
| **OS** | Windows 10 (バージョン 19041 以降) / Windows 11 |
| **アーキテクチャ** | x64 / Arm64 |
| **ランタイム** | [.NET 10.0 Runtime (Windows Desktop)](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **コンポーネント** | Microsoft Edge WebView2 ランタイム (Windows 10/11 に標準搭載) |

---

## 🛠️ 開発環境とビルド手順

### 前提条件
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2026 / Visual Studio Code / JetBrains Rider

### リポジトリのクローン
```bash
git clone https://github.com/tkizawa/WoodStreamPLAZAApp.git
cd WoodStreamPLAZAApp
```

### ビルド
```bash
dotnet build
```

### 実行
```bash
dotnet run --project src/WoodStreamPlaza
```

### 単体テストの実行
```bash
dotnet test
```

---

## 📂 プロジェクト構成

```
WoodStreamPLAZAApp/
├── WoodStreamPlaza.slnx              # ソリューション定義
├── app.ico / icon.png               # アプリケーションアイコン
├── src/
│   └── WoodStreamPlaza/
│       ├── App.xaml / App.xaml.cs    # アプリケーションエントリ・ライフサイクル
│       ├── MainWindow.xaml / .cs    # メインウィンドウ・WebView2制御・トレイ連携
│       ├── SettingsWindow.xaml / .cs# 設定画面（言語・常駐・通知・キャッシュ）
│       ├── Models/
│       │   └── AppSettings.cs       # アプリ設定データモデル
│       ├── Services/
│       │   ├── NotificationService.cs # Windowsトースト通知制御
│       │   ├── TrayIconService.cs     # タスクトレイ常駐管理 (NotifyIcon)
│       │   ├── TaskbarBadgeHelper.cs  # タスクバー未読バッジ生成
│       │   ├── LocalizationService.cs # 多言語リソース適用
│       │   ├── SettingsService.cs     # 設定ファイル保存・復元 (UTF-8)
│       │   └── Logger.cs              # アプリケーションログ出力
│       └── Resources/
│           ├── Strings.ja-JP.xaml   # 日本語リソース
│           └── Strings.en-US.xaml   # 英語リソース
└── tests/
    └── WoodStreamPlaza.Tests/       # 単体テスト (設定・通知パース・バッジ生成)
```

---

## 📄 ライセンス

Copyright © 2026 WoodStream. All rights reserved.
