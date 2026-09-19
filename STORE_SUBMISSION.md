# Microsoft Store 登録・申請手順マニュアル

本ドキュメントは、**WoodStream PLAZA デスクトップクライアント** を Microsoft Partner Center（Microsoft Store）にアップロードし、審査・公開申請を完了するための詳細手順書です。

---

## 1. アプリケーション製品情報（Product Identity）

Partner Center で予約済みの本アプリの公式識別情報は以下の通りです（パッケージマニフェストに反映済み）。

| 項目 | 設定値 |
| :--- | :--- |
| **Package/Identity/Name** | `57742TomokazuKizawa.WoodStreamPLAZA` |
| **Package/Identity/Publisher** | `CN=963B8572-7B10-48CC-9F90-46F0022D6A68` |
| **PublisherDisplayName** | `Tomokazu Kizawa` |
| **Package Family Name (PFN)** | `57742TomokazuKizawa.WoodStreamPLAZA_yhp74ye327hf0` |
| **Store ID** | `9P90RHRR0KXK` |
| **Store Web URL** | [https://apps.microsoft.com/detail/9P90RHRR0KXK](https://apps.microsoft.com/detail/9P90RHRR0KXK) |
| **Store プロトコルリンク** | `ms-windows-store://pdp/?productid=9P90RHRR0KXK` |

---

## 2. 提出用パッケージファイル

ビルド成果物はプロジェクト直下の `MSIX` フォルダに出力されています。

```
MSIX/
├── WoodStreamPlaza_1.0.0.0.msixbundle  ← ★【推奨】Store提出用（x64 + arm64統合）
├── WoodStreamPlaza_1.0.0.0_x64.msix    ← x64個別パッケージ
└── WoodStreamPlaza_1.0.0.0_arm64.msix  ← arm64個別パッケージ
```

> [!TIP]
> **msixbundle の使用を推奨します**
> `WoodStreamPlaza_1.0.0.0.msixbundle` には **x64** と **arm64** の両バイナリが統合されています。
> Partner Center にこの 1 ファイルをアップロードするだけで、ストアがユーザー端末の CPU（Intel/AMD または Snapdragon等のArm64）を自動判定し、最適なバイナリを高速配信します。

---

## 3. Microsoft Partner Center での申請手順

### ステップ 1: Partner Center へログイン
1. [Microsoft Partner Center ダッシュボード](https://partner.microsoft.com/dashboard) にアクセスしてサインインします。
2. アプリ一覧から **「WoodStream PLAZAデスクトップクライアント」**（または予約したアプリ名）をクリックします。
3. **「申請を開始」**（Start your submission）をクリックします。

---

### ステップ 2: パッケージ (Packages) のアップロード
1. 申請メニューから **「パッケージ」** を選択します。
2. パッケージのドラッグ＆ドロップエリアに、以下のファイルをドラッグ＆ドロップします：
   - `MSIX\WoodStreamPlaza_1.0.0.0.msixbundle`
3. アップロード完了後、パッケージの検証が自動実行され、以下が表示されることを確認します：
   - バージョン: `1.0.0.0`
   - サポートされるアーキテクチャ: `x64, arm64`
   - 機能: `runFullTrust`
4. **「保存」** をクリックします。

---

### ステップ 3: プロパティ (Properties) の設定
1. **カテゴリ**: `ソーシャル` ＞ `コミュニティ`（または `ソーシャル ネットワーク`）
2. **プライバシー ポリシーの URL (Privacy policy URL)**:
   - **`https://github.com/tkizawa/WoodStreamPLAZAApp/blob/main/PRIVACY.md`**
3. **Web サイト**: `https://windows-podcast.com/plaza/` または GitHub リポジトリ URL
4. **サポート連絡先情報**: 問い合わせ用メールアドレスまたは Web フォーム URL (GitHub Issues 等)
5. **「保存」** をクリックします。

---

### ステップ 4: 年齢区分 (Age ratings)
1. 年齢区分のアンケート（IARC 質問票）を開始します。
2. アプリの種類（ユーティリティ/デスクトップクライアント等）を選択し、暴力や成人向けコンテンツがない旨の質問に回答します（すべて「いいえ」に該当）。
3. レーティングが自動算出されるので、確認して **「保存」** をクリックします。

---

### ステップ 5: ストア登録情報 (Store listings)
ストア上に表示される説明文やスクリーンショットを登録します。
言語ごとに登録できます（日本語 `ja-JP`、英語 `en-US`）。

#### 入力テキストの例（日本語）:
- **説明 (Description)**:
  ```
  WoodStream PLAZA デスクトップクライアントは、WoodStream PLAZA を Windows 上で快適に利用するための専用クライアントアプリケーションです。

  【主な機能】
  ・Microsoft WebView2 を採用した高速かつセキュアなブラウジング体験
  ・タスクトレイ常駐対応（ウィンドウ最小化・閉じる操作時にバックグラウンドで動作）
  ・Windows トースト通知対応（更新や新着情報を通知）
  ・多言語対応（日本語・英語の表示言語自動切り替え）
  ・ウィンドウ位置・サイズの自動保存・復元
  ・x64 および Windows on Arm (Arm64) ネイティブ対応
  ```
- **機能リスト (Product Features)**:
  - WebView2 ベースの高速ブラウジング
  - タスクトレイ常駐・クイックアクセス
  - Windows ネイティブトースト通知
  - 日本語・英語の多言語対応
  - Arm64 & x64 ネイティブ動作
- **検索キーワード (Search terms)**:
  - `WoodStream`
  - `PLAZA`
  - `クライアント`
  - `デスクトップ`
- **スクリーンショット (Screenshots)**:
  - アプリを起動した画面のキャプチャ（1366 x 768 ピクセル以上推奨、PNG形式）を1枚以上アップロードします。

---

### ステップ 6: 審査への提出 (Submit to the Store)
1. すべてのセクションに緑色のチェックマークが付いていることを確認します。
2. **「ストアに提出」**（Submit to the Store）ボタンをクリックします。
3. Microsoft による自動検証および認定審査（通常数時間〜数日）が開始されます。
4. 認定されると、[https://apps.microsoft.com/detail/9P90RHRR0KXK](https://apps.microsoft.com/detail/9P90RHRR0KXK) で一般公開されます。

---

## 4. バージョンアップ時のビルド手順

次回以降、アプリをバージョンアップして再提出する場合は、以下のコマンドを実行します。

```powershell
# 例: バージョンを 1.0.1.0 に更新してビルドする場合
pwsh -ExecutionPolicy Bypass -File .\packaging\msix\build_msix.ps1 -Version 1.0.1.0
```

生成された新しい `MSIX\WoodStreamPlaza_1.0.1.0.msixbundle` を Partner Center の「パッケージ」画面に追加し、提出を行ってください。
