using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;
using VitalDesk.App.Services;
using System.Linq;

namespace VitalDesk.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IPatientRepository _patientRepository;
    private readonly SampleDataService _sampleDataService;
    private readonly CsvBackupService _csvBackupService;
    
    [ObservableProperty]
    private ObservableCollection<Patient> _patients = new();
    
    [ObservableProperty]
    private Patient? _selectedPatient;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private int _selectedTabIndex = 0;
    
    [ObservableProperty]
    private ObservableCollection<Patient> _dischargedPatients = new();
    
    [ObservableProperty]
    private ObservableCollection<Patient> _transferredPatients = new();
    
    public MainViewModel()
    {
        _patientRepository = new PatientRepository();
        _sampleDataService = new SampleDataService();
        _csvBackupService = new CsvBackupService();
        _ = InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        // データベースを初期化
        await VitalDesk.Core.Migrations.DatabaseInitializer.InitializeAsync();
        
        // 最初は入院患者データのみを読み込み
        await LoadPatientsAsync();
    }
    
    // タブが変更された時の処理
    partial void OnSelectedTabIndexChanged(int value)
    {
        // タブが変更された時に該当タブのデータをDBから即座に読み込み
        _ = LoadTabDataAsync(value);
    }
    
    private async Task LoadTabDataAsync(int tabIndex)
    {
        try
        {
            IsLoading = true;
            
            switch (tabIndex)
            {
                case 0: // 入院患者タブ
                    await LoadPatientsAsync();
                    break;
                case 1: // 退院患者タブ
                    await LoadDischargedPatientsAsync();
                    break;
                case 2: // 転棟患者タブ
                    await LoadTransferredPatientsAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tab data for index {tabIndex}: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task LoadPatientsAsync()
    {
        try
        {
            IsLoading = true;
            var patients = await _patientRepository.GetAllAsync();
            Patients.Clear();
            
            // ふりがなでソート
            var sortedPatients = patients.OrderBy(p => p.Furigana).ToList();
            foreach (var patient in sortedPatients)
            {
                Patients.Add(patient);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading patients: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task LoadDischargedPatientsAsync()
    {
        try
        {
            IsLoading = true;
            var patients = await _patientRepository.GetDischargedPatientsAsync();
            DischargedPatients.Clear();
            foreach (var patient in patients)
            {
                DischargedPatients.Add(patient);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading discharged patients: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task LoadTransferredPatientsAsync()
    {
        try
        {
            IsLoading = true;
            var patients = await _patientRepository.GetTransferredPatientsAsync();
            TransferredPatients.Clear();
            foreach (var patient in patients)
            {
                TransferredPatients.Add(patient);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading transferred patients: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task SearchPatientsAsync()
    {
        try
        {
            IsLoading = true;
            
            IEnumerable<Patient> patients;
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                patients = await _patientRepository.GetAllAsync();
            }
            else
            {
                patients = await _patientRepository.SearchAsync(SearchText.Trim());
            }
            
            // ふりがなでソートしてから表示
            var sortedPatients = patients.OrderBy(p => p.Furigana).ToList();
            
            Patients.Clear();
            foreach (var patient in sortedPatients)
            {
                Patients.Add(patient);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching patients: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task AddPatientAsync()
    {
        var viewModel = new PatientInputViewModel();
        var dialog = new Views.PatientInputDialog(viewModel);
        
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;
        
        var result = await dialog.ShowDialog<Patient?>(mainWindow);
        
        if (result != null)
        {
            // 新規追加された患者をふりがな順で適切な位置に挿入
            var insertIndex = 0;
            for (int i = 0; i < Patients.Count; i++)
            {
                if (string.Compare(result.Furigana, Patients[i].Furigana, StringComparison.OrdinalIgnoreCase) <= 0)
                {
                    insertIndex = i;
                    break;
                }
                insertIndex = i + 1;
            }
            Patients.Insert(insertIndex, result);
        }
    }
    
    [RelayCommand]
    private async Task EditPatientAsync(Patient? patient)
    {
        if (patient == null) return;
        
        var viewModel = new PatientInputViewModel();
        viewModel.SetEditMode(patient);
        var dialog = new Views.PatientInputDialog(viewModel);
        
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;
        
        var result = await dialog.ShowDialog<Patient?>(mainWindow);
        
        if (result != null)
        {
            // 編集された患者を一旦リストから削除
            var index = Patients.IndexOf(patient);
            if (index >= 0)
            {
                Patients.RemoveAt(index);
            }
            
            // ふりがな順で適切な位置に再挿入
            var insertIndex = 0;
            for (int i = 0; i < Patients.Count; i++)
            {
                if (string.Compare(result.Furigana, Patients[i].Furigana, StringComparison.OrdinalIgnoreCase) <= 0)
                {
                    insertIndex = i;
                    break;
                }
                insertIndex = i + 1;
            }
            Patients.Insert(insertIndex, result);
        }
    }
    
    [RelayCommand]
    private async Task DeletePatientAsync(Patient? patient)
    {
        if (patient == null) return;
        
        try
        {
            // 確認ダイアログを表示
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow == null) return;
            
            var dialog = new Views.ConfirmationDialog(
                "患者削除の確認",
                $"患者「{patient.Name}」（国保: {patient.NationalHealthInsurance}）を削除しますか？\n\nこの操作は取り消せません。",
                "削除",
                "キャンセル"
            );
            
            var result = await dialog.ShowDialog<bool?>(mainWindow);
            
            if (result == true)
            {
                var success = await _patientRepository.DeleteAsync(patient.Id);
                if (success)
                {
                    Patients.Remove(patient);
                    DischargedPatients.Remove(patient);
                    TransferredPatients.Remove(patient);
                    
                    // 成功メッセージを表示
                    var successDialog = new Views.MessageDialog(
                        "削除完了",
                        $"患者「{patient.Name}」を削除しました。"
                    );
                    await successDialog.ShowDialog(mainWindow);
                }
                else
                {
                    // エラーメッセージを表示
                    var errorDialog = new Views.MessageDialog(
                        "削除エラー",
                        "患者の削除に失敗しました。"
                    );
                    await errorDialog.ShowDialog(mainWindow);
                }
            }
        }
        catch (Exception ex)
        {
            // エラーメッセージを表示
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var errorDialog = new Views.MessageDialog(
                    "削除エラー",
                    $"患者の削除中にエラーが発生しました: {ex.Message}"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
    }
    
    [RelayCommand]
    private async Task ViewPatientDetailsAsync(Patient? patient)
    {
        if (patient == null) return;
        
        var viewModel = new PatientDetailsViewModel(patient);
        var dialog = new Views.PatientDetailsDialog(viewModel);
        
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;
        
        await dialog.ShowDialog(mainWindow);
    }
    
    [RelayCommand]
    private async Task DischargePatientAsync(Patient? patient)
    {
        if (patient == null) return;
        
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;
        
        // 確認ダイアログを表示
        var confirmDialog = new Views.ConfirmationDialog(
            "退院確認",
            $"患者「{patient.Name}」を退院させますか？",
            "退院",
            "キャンセル"
        );
        
        var result = await confirmDialog.ShowDialog<bool?>(mainWindow);
        
        if (result != true) return; // キャンセルされた場合は何もしない
        
        try
        {
            // ステータスを退院に設定して更新
            patient.Status = PatientStatus.Discharged;
            var success = await _patientRepository.UpdateAsync(patient);
            
            if (success)
            {
                // 入院患者リストから削除
                Patients.Remove(patient);
                
                // 退院タブに切り替え
                SelectedTabIndex = 1;
                
                var successDialog = new Views.MessageDialog(
                    "退院処理完了",
                    $"患者「{patient.Name}」の退院処理が完了しました。"
                );
                await successDialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new Views.MessageDialog(
                "退院処理エラー",
                $"退院処理中にエラーが発生しました: {ex.Message}"
            );
            await errorDialog.ShowDialog(mainWindow);
        }
    }
    
    [RelayCommand]
    private async Task TransferPatientAsync(Patient? patient)
    {
        if (patient == null) return;
        
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;
        
        // 確認ダイアログを表示
        var confirmDialog = new Views.ConfirmationDialog(
            "転棟確認",
            $"患者「{patient.Name}」を転棟させますか？",
            "転棟",
            "キャンセル"
        );
        
        var result = await confirmDialog.ShowDialog<bool?>(mainWindow);
        
        if (result != true) return; // キャンセルされた場合は何もしない
        
        try
        {
            // ステータスを転棟に設定して更新
            patient.Status = PatientStatus.Transferred;
            var success = await _patientRepository.UpdateAsync(patient);
            
            if (success)
            {
                // 入院患者リストから削除
                Patients.Remove(patient);
                
                // 転棟タブに切り替え
                SelectedTabIndex = 2;
                
                var successDialog = new Views.MessageDialog(
                    "転棟処理完了",
                    $"患者「{patient.Name}」の転棟処理が完了しました。"
                );
                await successDialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new Views.MessageDialog(
                "転棟処理エラー",
                $"転棟処理中にエラーが発生しました: {ex.Message}"
            );
            await errorDialog.ShowDialog(mainWindow);
        }
    }
    
    [RelayCommand]
    private async Task ReadmitPatientAsync(Patient? patient)
    {
        if (patient == null) return;
        
        try
        {
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow == null) return;
            
            // 確認ダイアログを表示
            var dialog = new Views.ConfirmationDialog(
                "再入院の確認",
                $"患者「{patient.Name}」（国保: {patient.NationalHealthInsurance}）を再入院させますか？",
                "再入院",
                "キャンセル"
            );
            
            var result = await dialog.ShowDialog<bool?>(mainWindow);
            
            if (result == true)
            {
                // ステータスを入院に変更
                patient.Status = PatientStatus.Admitted;
                var success = await _patientRepository.UpdateAsync(patient);
                
                if (success)
                {
                    // 退院・転棟リストから患者を削除
                    DischargedPatients.Remove(patient);
                    TransferredPatients.Remove(patient);
                    
                    // 入院患者タブに切り替え
                    SelectedTabIndex = 0;
                    
                    var successDialog = new Views.MessageDialog(
                        "再入院処理完了",
                        $"患者「{patient.Name}」の再入院処理が完了しました。"
                    );
                    await successDialog.ShowDialog(mainWindow);
                }
                else
                {
                    var errorDialog = new Views.MessageDialog(
                        "再入院処理エラー",
                        "再入院処理に失敗しました。"
                    );
                    await errorDialog.ShowDialog(mainWindow);
                }
            }
        }
        catch (Exception ex)
        {
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var errorDialog = new Views.MessageDialog(
                    "再入院処理エラー",
                    $"再入院処理中にエラーが発生しました: {ex.Message}"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
    }
    
    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            IsLoading = true;
            
            var backupPath = await _csvBackupService.CreateBackupAsync();
            
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var successDialog = new Views.MessageDialog(
                    "バックアップ完了",
                    $"全データのバックアップが完了しました。\n\n保存場所:\n{backupPath}\n\n患者データとバイタルサインデータが全て含まれています。"
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
                    "バックアップエラー",
                    $"バックアップの作成中にエラーが発生しました:\n{ex.Message}"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportBackupAsync()
    {
        try
        {
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow == null) return;

            // 最初の確認ダイアログ
            var initialConfirmDialog = new Views.ConfirmationDialog(
                "全データを削除します",
                "職員の方は必ず'キャンセル'を押してください。\n\n",
                "続行する",
                "キャンセル"
            );

            var initialResult = await initialConfirmDialog.ShowDialog<bool?>(mainWindow);
            if (initialResult != true) return;

            // ファイル選択ダイアログを表示
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(mainWindow);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "バックアップファイルを選択",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("CSVファイル")
                    {
                        Patterns = new[] { "*.csv" },
                        MimeTypes = new[] { "text/csv" }
                    }
                }
            });

            if (files.Count == 0) return;

            var filePath = files[0].Path.LocalPath;

            // 最終確認ダイアログ
            var finalConfirmDialog = new Views.ConfirmationDialog(
                "最終確認",
                "この操作を実行すると、現在のデータは完全に削除され、選択したバックアップファイルのデータに置き換えられます。\n\n" +
                "この操作は取り消すことができません。本当に実行しますか？",
                "実行する",
                "キャンセル"
            );

            var finalResult = await finalConfirmDialog.ShowDialog<bool?>(mainWindow);
            if (finalResult != true) return;

            IsLoading = true;

            var importedPatientCount = await _csvBackupService.ImportBackupAsync(filePath);

            // 現在表示中のタブを再読み込み
            await LoadTabDataAsync(SelectedTabIndex);

            var successDialog = new Views.MessageDialog(
                "復元完了",
                $"バックアップファイルからデータを復元しました。\n\n復元された患者数: {importedPatientCount}人"
            );
            await successDialog.ShowDialog(mainWindow);
        }
        catch (Exception ex)
        {
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var errorDialog = new Views.MessageDialog(
                    "復元エラー",
                    $"データの復元中にエラーが発生しました:\n{ex.Message}"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GenerateSampleDataAsync()
    {
        try
        {
            IsLoading = true;
            
            var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow == null) return;

            // 既にサンプルデータがあるかチェック
            if (await _sampleDataService.HasSampleDataAsync())
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

            await _sampleDataService.GenerateSamplePatientsAsync(100);
            
            // 現在表示中のタブを再読み込み
            await LoadTabDataAsync(SelectedTabIndex);

            var successDialog = new Views.MessageDialog(
                "サンプルデータ生成完了",
                "100人の患者データとバイタルサインデータを生成しました。"
            );
            await successDialog.ShowDialog(mainWindow);
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
            IsLoading = false;
        }
    }
    
    partial void OnSearchTextChanged(string value)
    {
        // 検索テキストが変更されたら即座に検索を実行
        _ = SearchPatientsAsync();
    }
} 