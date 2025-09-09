using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalDesk.App.Services;
using VitalDesk.Core.Repositories;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Avalonia.Controls.ApplicationLifetimes;

namespace VitalDesk.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IPatientRepository _patientRepository;
    private readonly IVitalRepository _vitalRepository;
    private readonly SampleDataService _sampleDataService;
    
    [ObservableProperty]
    private bool _isProcessing;
    
    [ObservableProperty]
    private string _processingMessage = string.Empty;
    
    [ObservableProperty]
    private bool _autoBackupEnabled = true;
    
    [ObservableProperty]
    private int _backupRetentionDays = 30;
    
    [ObservableProperty]
    private int _patientCount;
    
    [ObservableProperty]
    private int _vitalCount;
    
    public SettingsViewModel()
    {
        _patientRepository = new PatientRepository();
        _vitalRepository = new VitalRepository();
        _sampleDataService = new SampleDataService();
        
        _ = LoadStatisticsAsync();
    }
    
    [RelayCommand]
    private async Task GenerateSampleDataAsync()
    {
        try
        {
            IsProcessing = true;
            ProcessingMessage = "サンプルデータを生成しています...";

            // 既にサンプルデータがあるかチェック
            if (await _sampleDataService.HasSampleDataAsync())
            {
                var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (mainWindow != null)
                {
                    var confirmDialog = new Views.ConfirmationDialog(
                        "サンプルデータ生成の確認",
                        "既に多くの患者データが存在します。追加でサンプルデータを生成しますか？",
                        "生成する",
                        "キャンセル"
                    );
                    
                    var result = await confirmDialog.ShowDialog<bool?>(mainWindow);
                    if (result != true)
                    {
                        return;
                    }
                }
            }

            await _sampleDataService.GenerateSamplePatientsAsync(100);
            await LoadStatisticsAsync();

            var mainWindow2 = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow2 != null)
            {
                var successDialog = new Views.MessageDialog(
                    "サンプルデータ生成完了",
                    "100人の患者データとバイタルサインデータを生成しました。"
                );
                await successDialog.ShowDialog(mainWindow2);
            }
        }
        catch (Exception ex)
        {
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var errorDialog = new Views.MessageDialog(
                    "エラー",
                    $"サンプルデータの生成中にエラーが発生しました: {ex.Message}"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
        finally
        {
            IsProcessing = false;
            ProcessingMessage = string.Empty;
        }
    }
    
    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            IsProcessing = true;
            ProcessingMessage = "バックアップを作成しています...";

            var backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VitalDesk", "Backups");
            Directory.CreateDirectory(backupFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"VitalDesk_Backup_{timestamp}.db";
            var backupPath = Path.Combine(backupFolder, backupFileName);

            // データベースファイルをコピー
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VitalDesk", "vitaldesk.db");
            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, backupPath, true);

                var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (mainWindow != null)
                {
                    var successDialog = new Views.MessageDialog(
                        "バックアップ完了",
                        $"バックアップが正常に作成されました。\n\n保存場所: {backupPath}"
                    );
                    await successDialog.ShowDialog(mainWindow);
                }
            }
            else
            {
                throw new FileNotFoundException("データベースファイルが見つかりません。");
            }
        }
        catch (Exception ex)
        {
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var errorDialog = new Views.MessageDialog(
                    "バックアップエラー",
                    $"バックアップの作成中にエラーが発生しました: {ex.Message}"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
        finally
        {
            IsProcessing = false;
            ProcessingMessage = string.Empty;
        }
    }
    
    [RelayCommand]
    private async Task CleanupOldBackupsAsync()
    {
        try
        {
            IsProcessing = true;
            ProcessingMessage = "古いバックアップファイルを削除しています...";

            var backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VitalDesk", "Backups");
            
            if (!Directory.Exists(backupFolder))
            {
                var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (mainWindow != null)
                {
                    var infoDialog = new Views.MessageDialog(
                        "情報",
                        "バックアップフォルダが存在しません。"
                    );
                    await infoDialog.ShowDialog(mainWindow);
                }
                return;
            }

            var cutoffDate = DateTime.Now.AddDays(-BackupRetentionDays);
            var backupFiles = Directory.GetFiles(backupFolder, "VitalDesk_Backup_*.db");
            var deletedCount = 0;

            foreach (var file in backupFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(file);
                    deletedCount++;
                }
            }

            var mainWindow2 = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow2 != null)
            {
                var successDialog = new Views.MessageDialog(
                    "クリーンアップ完了",
                    $"{deletedCount}個の古いバックアップファイルを削除しました。"
                );
                await successDialog.ShowDialog(mainWindow2);
            }
        }
        catch (Exception ex)
        {
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var errorDialog = new Views.MessageDialog(
                    "クリーンアップエラー",
                    $"バックアップファイルの削除中にエラーが発生しました: {ex.Message}"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
        finally
        {
            IsProcessing = false;
            ProcessingMessage = string.Empty;
        }
    }
    
    [RelayCommand]
    private async Task ExportPatientsAsync()
    {
        try
        {
            IsProcessing = true;
            ProcessingMessage = "患者データをエクスポートしています...";

            var patients = await _patientRepository.GetAllAsync();
            var exportFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VitalDesk", "Exports");
            Directory.CreateDirectory(exportFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"Patients_Export_{timestamp}.csv";
            var filePath = Path.Combine(exportFolder, fileName);

            var csv = new StringBuilder();
            csv.AppendLine("国保,記号,番号,保険者名,患者名,フリガナ,生年月日,年齢,初診日,入院日,退院日");

            foreach (var patient in patients)
            {
                csv.AppendLine($"{patient.NationalHealthInsurance},{patient.Symbol},{patient.Number},{patient.InsurerName},{patient.Name},{patient.Furigana},{patient.BirthDate:yyyy/MM/dd},{patient.Age},{patient.FirstVisit:yyyy/MM/dd},{patient.Admission:yyyy/MM/dd},{patient.Discharge:yyyy/MM/dd}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);

            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var successDialog = new Views.MessageDialog(
                    "エクスポート完了",
                    $"患者データをCSVファイルにエクスポートしました。\n\n保存場所: {filePath}"
                );
                await successDialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var errorDialog = new Views.MessageDialog(
                    "エクスポートエラー",
                    $"患者データのエクスポート中にエラーが発生しました: {ex.Message}"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
        finally
        {
            IsProcessing = false;
            ProcessingMessage = string.Empty;
        }
    }
    
    [RelayCommand]
    private async Task ExportVitalsAsync()
    {
        try
        {
            IsProcessing = true;
            ProcessingMessage = "バイタルサインデータをエクスポートしています...";

            var vitals = await _vitalRepository.GetAllAsync();
            var exportFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VitalDesk", "Exports");
            Directory.CreateDirectory(exportFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"Vitals_Export_{timestamp}.csv";
            var filePath = Path.Combine(exportFolder, fileName);

            var csv = new StringBuilder();
            csv.AppendLine("患者ID,測定日時,体温,脈拍,収縮期血圧,拡張期血圧,体重");

            foreach (var vital in vitals)
            {
                csv.AppendLine($"{vital.PatientId},{vital.MeasuredAt:yyyy/MM/dd HH:mm},{vital.Temperature:F1},{vital.Pulse},{vital.Systolic},{vital.Diastolic},{vital.Weight:F1}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);

            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var successDialog = new Views.MessageDialog(
                    "エクスポート完了",
                    $"バイタルサインデータをCSVファイルにエクスポートしました。\n\n保存場所: {filePath}"
                );
                await successDialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var errorDialog = new Views.MessageDialog(
                    "エクスポートエラー",
                    $"バイタルサインデータのエクスポート中にエラーが発生しました: {ex.Message}"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
        finally
        {
            IsProcessing = false;
            ProcessingMessage = string.Empty;
        }
    }

    [RelayCommand]
    private void OpenBackupFolder()
    {
        var backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VitalDesk", "Backups");
        Directory.CreateDirectory(backupFolder);
        OpenFolder(backupFolder);
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        var logsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VitalDesk", "Logs");
        Directory.CreateDirectory(logsFolder);
        OpenFolder(logsFolder);
    }

    [RelayCommand]
    private void OpenDatabaseFolder()
    {
        var dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VitalDesk");
        Directory.CreateDirectory(dbFolder);
        OpenFolder(dbFolder);
    }

    private void OpenFolder(string folderPath)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", folderPath);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", folderPath);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", folderPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening folder: {ex.Message}");
        }
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            var patients = await _patientRepository.GetAllAsync();
            var vitals = await _vitalRepository.GetAllAsync();
            
            PatientCount = patients.Count();
            VitalCount = vitals.Count();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading statistics: {ex.Message}");
        }
    }
} 