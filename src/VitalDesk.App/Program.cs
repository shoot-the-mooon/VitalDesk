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
        if (args.Length > 0 && args[0] == "generate-sample")
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
        
        if (args.Length > 1 && int.TryParse(args[1], out var count))
            patientCount = count;
        
        if (args.Length > 2 && int.TryParse(args[2], out var days))
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
