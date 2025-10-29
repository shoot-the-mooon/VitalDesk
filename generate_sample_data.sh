#!/bin/bash

# VitalDesk サンプルデータ生成スクリプト
# 使用方法: ./generate_sample_data.sh [患者数] [バイタル日数]
# 例: ./generate_sample_data.sh 50 90

PATIENT_COUNT=${1:-50}
VITAL_DAYS=${2:-90}

echo "========================================"
echo "VitalDesk サンプルデータ生成"
echo "========================================"
echo "患者数: ${PATIENT_COUNT}人"
echo "バイタル期間: ${VITAL_DAYS}日"
echo "========================================"
echo ""

# スクリプトのディレクトリに移動
cd "$(dirname "$0")"

# ビルド
echo "アプリケーションをビルド中..."
cd src
dotnet build VitalDesk.sln -c Release > /dev/null 2>&1

if [ $? -ne 0 ]; then
    echo "エラー: ビルドに失敗しました"
    exit 1
fi

echo "ビルド完了"
echo ""

# サンプルデータ生成用の一時的なC#スクリプトを作成
SCRIPT_PATH="VitalDesk.App/bin/Release/net9.0/GenerateSampleData.cs"
cat > "$SCRIPT_PATH" << 'EOF'
using System;
using System.Threading.Tasks;
using VitalDesk.App.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var patientCount = args.Length > 0 ? int.Parse(args[0]) : 50;
        var vitalDays = args.Length > 1 ? int.Parse(args[1]) : 90;
        
        var service = new SampleDataService();
        await service.GenerateSamplePatientsAsync(patientCount, vitalDays);
        
        Console.WriteLine("");
        Console.WriteLine("サンプルデータの生成が完了しました！");
        Console.WriteLine("アプリケーションを起動してご確認ください。");
    }
}
EOF

# サンプルデータ生成を実行
echo "サンプルデータを生成中..."
cd VitalDesk.App
dotnet run --no-build -c Release -- generate-sample ${PATIENT_COUNT} ${VITAL_DAYS}

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================"
    echo "✓ サンプルデータ生成完了"
    echo "========================================"
    echo ""
    echo "アプリケーションを起動するには:"
    echo "  cd src/VitalDesk.App && dotnet run"
else
    echo ""
    echo "エラー: サンプルデータの生成に失敗しました"
    exit 1
fi

