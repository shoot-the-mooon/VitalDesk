# VitalDesk サンプルデータ生成ガイド

## 📊 概要

このスクリプトは、VitalDeskアプリケーションのテスト・デモ用にリアルなサンプルデータを生成します。

### 生成されるデータ

- **患者データ**: デフォルト50人（カスタマイズ可能）
- **バイタルサイン**: 1人あたり3ヶ月分（90日、カスタマイズ可能）
- **測定頻度**: **1日1件**（午前中8-11時にランダム）
- **患者ステータス**: 入院中（70%）、退院（15%）、転棟（15%）

### データの詳細

#### 患者情報
- 国保番号: 10桁（市区町村コード6桁 + 個人番号4桁）
- 記号: アルファベット2-4文字 + 数字3-5桁
- 番号: 6-8桁の数字（**TEXT型**）
- 保険者名: 日本の主要都市の国民健康保険
- フリガナ: カタカナ
- 年齢: 20-90歳

#### バイタルサイン
- 体温: 35.8-38.2°C
- 脈拍: 55-104 bpm
- 血圧（収縮期）: 95-154 mmHg
- 血圧（拡張期）: 55-94 mmHg
- 体重: 45.0-95.0 kg

---

## 🚀 使用方法

### 方法1: コマンドラインから直接実行（推奨）

```bash
# デフォルト設定（50人、90日分）
cd src/VitalDesk.App
dotnet run -- generate-sample

# カスタム設定: 患者数を指定
dotnet run -- generate-sample 100

# カスタム設定: 患者数とバイタル期間を指定
dotnet run -- generate-sample 100 180
```

### 方法2: シェルスクリプトを使用

```bash
# デフォルト設定（50人、90日分）
./generate_sample_data.sh

# カスタム設定
./generate_sample_data.sh 100 180
```

---

## 📝 コマンドの説明

### 基本構文

```bash
cd src/VitalDesk.App
dotnet run -- generate-sample [患者数] [バイタル日数]
```

### パラメータ

| パラメータ | 説明 | デフォルト値 |
|-----------|------|-------------|
| 患者数 | 生成する患者の人数 | 50 |
| バイタル日数 | 各患者のバイタルサインを記録する日数 | 90 |

### 実行例

```bash
# 例1: 50人、90日分（デフォルト）
dotnet run -- generate-sample

# 例2: 100人、90日分
dotnet run -- generate-sample 100

# 例3: 30人、30日分（小規模テスト）
dotnet run -- generate-sample 30 30

# 例4: 200人、180日分（大規模テスト）
dotnet run -- generate-sample 200 180
```

---

## ⚠️ 注意事項

1. **既存データの削除**: サンプルデータ生成を実行すると、**既存のすべてのデータが削除されます**。

2. **生成時間**: データ量に応じて数秒〜数十秒かかります。
   - 50人 x 90日 ≈ 5-10秒
   - 100人 x 180日 ≈ 15-25秒

3. **データベースの場所**: 
   ```
   src/VitalDesk.App/bin/Debug/net9.0/Temperatures.db
   ```

---

## 🔍 生成されたデータの確認

### SQLiteで確認

```bash
# データベースに接続
sqlite3 src/VitalDesk.App/bin/Debug/net9.0/Temperatures.db

# 患者数を確認
SELECT COUNT(*) FROM Patient;

# ステータス別の患者数
SELECT Status, COUNT(*) FROM Patient GROUP BY Status;

# バイタルデータの総数
SELECT COUNT(*) FROM Vital;

# サンプルデータを表示
SELECT Name, Furigana, Status FROM Patient LIMIT 10;
```

### アプリケーションで確認

```bash
cd src/VitalDesk.App
dotnet run
```

アプリケーションを起動すると、生成されたサンプルデータが各タブに表示されます。

---

## 📊 生成結果の例

```
======================================
VitalDesk サンプルデータ生成
======================================
患者数: 50人
バイタル期間: 90日
======================================

サンプルデータ生成開始: 50人の患者、90日分のバイタル
  10/50人の患者データを生成完了
  20/50人の患者データを生成完了
  30/50人の患者データを生成完了
  40/50人の患者データを生成完了
  50/50人の患者データを生成完了
サンプルデータ生成完了: 50人

======================================
✓ サンプルデータ生成完了
======================================

データベース: src/VitalDesk.App/bin/Debug/net9.0/Temperatures.db

アプリケーションを起動するには:
  cd src/VitalDesk.App && dotnet run
```

### 統計情報

- **患者総数**: 50人
- **入院中**: 約35人（70%）
- **退院**: 約8人（15%）
- **転棟**: 約7人（15%）
- **バイタルデータ総数**: 約2,356件（50人 x 平均47日）
- **1人あたり平均**: 約47件（入院日から現在まで、1日1件）

---

## 🛠️ トラブルシューティング

### エラー: ビルドに失敗しました

```bash
cd src
dotnet build VitalDesk.sln
```

ビルドエラーを確認して修正してください。

### エラー: データベースが見つかりません

データベースは初回実行時に自動的に作成されます。パスが正しいか確認してください。

### Number列の型エラー

新しいデータベースでは`Number`列は**TEXT型**になっています。古いデータベースを削除してから再生成してください：

```bash
rm -f src/VitalDesk.App/bin/Debug/net9.0/Temperatures.db*
dotnet run -- generate-sample
```

---

## 💡 ヒント

1. **テスト用の小規模データ**: `dotnet run -- generate-sample 10 30`
2. **デモ用の中規模データ**: `dotnet run -- generate-sample 50 90`（デフォルト）
3. **パフォーマンステスト用**: `dotnet run -- generate-sample 200 180`

---

## 📚 関連ファイル

- `src/VitalDesk.App/Services/SampleDataService.cs` - サンプルデータ生成ロジック
- `src/VitalDesk.App/Program.cs` - コマンドライン処理
- `src/VitalDesk.Core/Migrations/DatabaseInitializer.cs` - データベース初期化
- `generate_sample_data.sh` - シェルスクリプト（オプション）

