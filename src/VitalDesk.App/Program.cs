using System;
using System.Threading.Tasks;
using Avalonia;

namespace VitalDesk.App;

public class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // コマンドライン引数でサンプルデータ生成を実行
        // --generate-sample または generate-sample の形式に対応
        if (args.Length > 0 && (args[0] == "--generate-sample" || args[0] == "generate-sample"))
        {
            GenerateSampleDataAsync(args).GetAwaiter().GetResult();
            return;
        }
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static async Task GenerateSampleDataAsync(string[] args)
    {
        var patientCount = 50;
        var vitalDays = 90;
        
        // 引数のインデックスを調整（--generate-sample の場合は1から、generate-sample の場合は1から）
        var startIndex = args[0].StartsWith("--") ? 1 : 1;
        
        if (args.Length > startIndex && int.TryParse(args[startIndex], out var count))
            patientCount = count;
        
        if (args.Length > startIndex + 1 && int.TryParse(args[startIndex + 1], out var days))
            vitalDays = days;
        
        Console.WriteLine("======================================");
        Console.WriteLine("VitalDesk サンプルデータ生成");
        Console.WriteLine("======================================");
        Console.WriteLine($"患者数: {patientCount}人");
        Console.WriteLine($"バイタル期間: {vitalDays}日");
        Console.WriteLine("======================================");
        Console.WriteLine();
        
        var service = new Services.SampleDataService();
        await service.GenerateSamplePatientsAsync(patientCount, vitalDays);
        
        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("✓ サンプルデータ生成完了");
        Console.WriteLine("======================================");
        Console.WriteLine();
        Console.WriteLine("データベース: src/VitalDesk.App/bin/Debug/net9.0/Temperatures.db");
        Console.WriteLine();
        Console.WriteLine("アプリケーションを起動するには:");
        Console.WriteLine("  cd src/VitalDesk.App && dotnet run");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
