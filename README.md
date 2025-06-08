# VitalDesk

患者のバイタルサイン（体温、脈拍、血圧、体重）を管理するWindows デスクトップアプリケーション

## 📋 概要

VitalDeskは医療機関向けの患者バイタルサイン管理システムです。日本の医療現場での使用を想定し、オフライン環境での動作とシンプルな操作性を重視して開発されています。

## ✨ 主な機能

### 👥 患者管理
- 患者の基本情報登録・編集・検索
- 患者コード、氏名、生年月日、保険番号の管理
- 初診日、入院日、退院日の記録

### 🩺 バイタルサイン管理
- **体温** (必須): 30.0-45.0°C
- **脈拍**: 30-200 bpm
- **血圧**: 収縮期/拡張期 (mmHg)
- **体重**: 1.0-300.0 kg
- 測定日時の記録（重複防止機能付き）

### 📊 データ可視化
- 体温・脈拍の時系列グラフ表示（統一軸スケール）
- 期間フィルタリング（1週間、1ヶ月、全期間）
- バイタルサイン一覧テーブル表示

### 💾 データ管理
- SQLite3データベース（WALモード）
- 自動的な重複データ検出と編集モード切り替え
- データバックアップ機能

## 🛠️ 技術仕様

### プラットフォーム
- **対象OS**: Windows 10/11 64-bit
- **動作環境**: オフライン対応
- **配布形式**: 単一実行ファイル（43MB）

### 開発技術
- **フレームワーク**: Avalonia 11 (.NET 9.0)
- **アーキテクチャ**: MVVM パターン
- **UI**: FluentAvalonia UI
- **グラフ**: LiveChartsCore.SkiaSharpView.Avalonia
- **データベース**: SQLite3 (WALモード)
- **ORM**: Dapper
- **テスト**: NUnit

### プロジェクト構成
```
VitalDesk/
├── src/
│   ├── VitalDesk.App/          # Avalonia UI アプリケーション
│   ├── VitalDesk.Core/         # ビジネスロジック・データアクセス
│   └── VitalDesk.Tests/        # 単体テスト
├── .github/workflows/          # CI/CDパイプライン
├── backup.ps1                 # データバックアップスクリプト
└── VitalDesk.sln              # ソリューションファイル
```

## 🗄️ データベーススキーマ

### Patient テーブル
| カラム | 型 | 説明 |
|--------|-----|------|
| Id | INTEGER | 主キー |
| Code | TEXT | 患者コード（ユニーク） |
| Name | TEXT | 患者名 |
| BirthDate | TEXT | 生年月日 |
| InsuranceNo | TEXT | 保険番号 |
| FirstVisit | TEXT | 初診日 |
| Admission | TEXT | 入院日 |
| Discharge | TEXT | 退院日 |

### Vital テーブル
| カラム | 型 | 説明 |
|--------|-----|------|
| Id | INTEGER | 主キー |
| PatientId | INTEGER | 患者ID（外部キー） |
| MeasuredAt | TEXT | 測定日時 |
| Temperature | REAL | 体温（必須） |
| Pulse | INTEGER | 脈拍 |
| Systolic | INTEGER | 収縮期血圧 |
| Diastolic | INTEGER | 拡張期血圧 |
| Weight | REAL | 体重 |

## 🚀 セットアップ

### 開発環境
1. [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) をインストール
2. リポジトリをクローン
```bash
git clone https://github.com/[username]/VitalDesk.git
cd VitalDesk
```
3. プロジェクトを復元・ビルド
```bash
dotnet restore
dotnet build
```
4. アプリケーションを実行
```bash
dotnet run --project src/VitalDesk.App
```

### Windows配布用ビルド
```bash
dotnet publish src/VitalDesk.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true -p:TrimMode=link -o ./publish/windows-opt
```
**Note:** The application is published with trimming enabled. To ensure that
Dapper can map data correctly in the trimmed executable, the `Patient` and
`Vital` model classes preserve their public properties using the
`[DynamicallyAccessedMembers]` attribute.

### テスト実行
```bash
dotnet test
```

## 📱 使用方法

### 患者登録
1. メイン画面で「患者追加」ボタンをクリック
2. 患者情報を入力（患者コードと患者名は必須）
3. 「保存」ボタンで登録完了

### バイタルサイン記録
1. 患者一覧から対象患者を選択
2. 「詳細表示」ボタンをクリック
3. 「バイタル追加」ボタンをクリック
4. カレンダーで測定日を選択
5. バイタルサイン値を入力（体温は必須）
6. 「保存」または「更新」ボタンで記録

### データ表示・分析
- 患者詳細画面でグラフと一覧表を確認
- 期間フィルタで表示範囲を調整
- グラフは体温（34-40°C）と脈拍（40-160bpm）を統一スケールで表示

## 🔧 機能詳細

### データ検証
- 体温: 30.0-45.0°C の範囲チェック
- 脈拍: 30-200 bpm の範囲チェック
- 血圧: 収縮期50-250、拡張期30-150 mmHg
- 体重: 1.0-300.0 kg の範囲チェック
- 患者コード: 重複チェック
- 測定日時: 同一患者の重複防止

### グラフ機能
- 時系列データの可視化
- 体温・脈拍の統一軸表示（医療適正範囲）
- インタラクティブな操作
- 期間フィルタリング対応

## 🔄 CI/CD

GitHub Actionsによる自動ビルド・テストパイプライン:
- プルリクエスト時の自動テスト実行
- .NET 9.0 環境での検証
- Windows実行ファイルの自動生成

## 📋 今後の機能拡張

- [ ] データエクスポート機能（CSV、Excel）
- [ ] レポート生成機能
- [ ] 検索・フィルタリング機能の強化
- [ ] 多言語対応
- [ ] ネットワーク連携機能

## 🤝 コントリビューション

1. このリポジトリをフォーク
2. 機能ブランチを作成 (`git checkout -b feature/amazing-feature`)
3. 変更をコミット (`git commit -m 'Add amazing feature'`)
4. ブランチにプッシュ (`git push origin feature/amazing-feature`)
5. プルリクエストを作成

## 📄 ライセンス

このプロジェクトはMITライセンスの下で公開されています。詳細は[LICENSE](LICENSE)ファイルを参照してください。

## 📞 サポート

問題やご質問がございましたら、[Issues](https://github.com/[username]/VitalDesk/issues)ページでお知らせください。

---

**VitalDesk** - 患者バイタルサイン管理システム  
開発: 2024年 