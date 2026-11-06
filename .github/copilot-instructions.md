# WoodStream PLAZA 開発ガイド

## プロジェクト概要
- Windows 向けの WPF デスクトップクライアントです。既存の .NET 10、WPF、WebView2 の構成と実装パターンを維持してください。
- 主な UI は `MainWindow.xaml` / `.xaml.cs`、`SettingsWindow`、`HelpWindow`、設定モデルは `Models/`、アプリ機能は `Services/` にあります。
- `MainWindow` は WebView2 とナビゲーション、通知、タスクトレイを連携します。WebView2 のユーザーデータは `%LOCALAPPDATA%\WoodStreamPlaza\WebView2Data` を使い、セッション維持と既存のキャッシュ管理を壊さないでください。
- 新しい依存関係を追加する前に、.NET 標準 API と既存パッケージで対応できないか確認してください。

## 実装規約
- UI は日本語・英語に対応します。画面に表示する文字列は `Resources/Strings.ja-JP.xaml` と `Resources/Strings.en-US.xaml` の両方に追加し、既存の `LocalizationService` とリソースキーのパターンを使ってください。「自動」は Windows の表示言語に従う動作を維持してください。
- 設定は `SettingsService` を通じて `%LOCALAPPDATA%\WoodStreamPlaza\settings.json` に保存します。設定モデルは `Models/AppSettings.cs` に集約し、日本語を含む JSON は UTF-8 の可読テキストとして保存してください。
- ウィンドウの位置・サイズ・状態は設定経由で保存・復元してください。画面構成の変更時も、モニター範囲外の位置から安全に復元できる既存の挙動を維持してください。
- ログは既存の `Logger` を使い、設定・起動・通知などの処理を独自のログ方式に分散させないでください。
- 既存の命名、書式、非同期処理、エラー通知のパターンに従ってください。必要な理由がないコメントや、画面文字列の直書きは避けてください。

## バージョンとドキュメント
- アプリケーション、アセンブリ、ファイルのバージョンは `WoodStreamPlaza.csproj` で同期してください。機能追加・改善・不具合修正ではビルド番号 `d` を 1 増やし、明示的なバージョン更新では `a.b.c.d` の `c` を 1 増やして `d` を 0 にします。
- バージョンに関係する変更では、必要に応じて `packaging/msix/build_msix.ps1` などの既定値も同期してください。ユーザー向けの操作・仕様が変わる場合は `MANUAL.md`、開発・配布手順が変わる場合は `README.md` または `STORE_SUBMISSION.md` も更新してください。
- コミットする場合は、日本語のコミットメッセージを使用してください。

## ビルド・テスト・配布
- ビルド: `dotnet build WoodStreamPlaza.slnx`
- テスト: `dotnet test WoodStreamPlaza.slnx`。テストは `tests/WoodStreamPlaza.Tests/` の xUnit に追加し、関連するテストを実行してください。
- 配布物の発行では対象を明示し、現在のサポート対象である `win-x64` と `win-arm64` を維持してください。インストーラーは `packaging/installer/build_installers.ps1`、MSIX は `packaging/msix/build_msix.ps1` を使います。
- `Installer/`、`MSIX/`、`publish/` の生成物はコミットしないでください。
- 詳細な利用・開発手順は `README.md` と `MANUAL.md`、Store 提出手順は `STORE_SUBMISSION.md` を参照してください。
