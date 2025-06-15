using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;
using VitalDesk.App.Services;

namespace VitalDesk.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IPatientRepository _patientRepository;
    private readonly SampleDataService _sampleDataService;
    
    [ObservableProperty]
    private ObservableCollection<Patient> _patients = new();
    
    [ObservableProperty]
    private Patient? _selectedPatient;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private bool _isLoading;
    
    public MainViewModel()
    {
        _patientRepository = new PatientRepository();
        _sampleDataService = new SampleDataService();
        _ = InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        // データベースクリーンアップを実行
        var cleanupService = new DatabaseCleanupService();
        await cleanupService.CleanupInvalidDatesAsync();
        
        // 患者データを読み込み
        await LoadPatientsAsync();
    }
    
    [RelayCommand]
    private async Task LoadPatientsAsync()
    {
        try
        {
            IsLoading = true;
            var patients = await _patientRepository.GetAllAsync();
            Patients.Clear();
            foreach (var patient in patients)
            {
                Patients.Add(patient);
            }
        }
        catch (Exception ex)
        {
            // TODO: Show error message to user
            System.Diagnostics.Debug.WriteLine($"Error loading patients: {ex.Message}");
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
            
            Patients.Clear();
            foreach (var patient in patients)
            {
                Patients.Add(patient);
            }
        }
        catch (Exception ex)
        {
            // TODO: Show error message to user
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
        
        var result = await dialog.ShowDialog<bool?>(mainWindow);
        
        if (result == true)
        {
            // Refresh the patients list
            await LoadPatientsAsync();
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
            await LoadPatientsAsync();

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
    
    [RelayCommand]
    private async Task EditPatientAsync(Patient? patient)
    {
        if (patient == null) return;
        
        var viewModel = new PatientInputViewModel();
        viewModel.SetEditMode(patient);
        var dialog = new Views.PatientInputDialog(viewModel);
        
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;
        
        var result = await dialog.ShowDialog<bool?>(mainWindow);
        
        if (result == true)
        {
            // Refresh the patients list
            await LoadPatientsAsync();
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
                $"患者「{patient.Name}」（コード: {patient.Code}）を削除しますか？\n\nこの操作は取り消せません。",
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
    
    partial void OnSearchTextChanged(string value)
    {
        // Trigger search when search text changes
        _ = SearchPatientsAsync();
    }
} 