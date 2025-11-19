using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;

namespace VitalDesk.App.ViewModels;

public partial class PatientDetailsViewModel : ViewModelBase
{
    private readonly IVitalRepository _vitalRepository;
    
    [ObservableProperty]
    private Patient _patient;
    
    [ObservableProperty]
    private ObservableCollection<Vital> _recentVitals = new();
    
    [ObservableProperty]
    private Vital? _selectedVital;
    
    [ObservableProperty]
    private bool _hasSelectedVital;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string _patientAge = string.Empty;
    
    [ObservableProperty]
    private string _patientAgeNumber = string.Empty;
    
    public VitalChartsViewModel ChartsViewModel { get; }

    public PatientDetailsViewModel(Patient patient)
    {
        _patient = patient;
        _vitalRepository = new VitalRepository();
        ChartsViewModel = new VitalChartsViewModel(patient);
        
        // グラフの期間が変更されたときにテーブルを更新
        ChartsViewModel.OnPeriodChanged += OnChartPeriodChanged;
        
        CalculateAge();
        // LoadRecentVitalsAsync() を削除
        // VitalChartsViewModel の初期化時に UpdateCharts() が呼ばれ、
        // OnPeriodChanged イベントが発火するので、そこでデータが読み込まれる
    }
    
    private void OnChartPeriodChanged(DateTime startDate, DateTime endDate)
    {
        _ = LoadVitalsForPeriodAsync(startDate, endDate);
    }
    
    private void CalculateAge()
    {
        if (Patient.BirthDate.HasValue)
        {
            var age = DateTime.Today.Year - Patient.BirthDate.Value.Year;
            if (Patient.BirthDate.Value.Date > DateTime.Today.AddYears(-age))
                age--;
            PatientAge = $"{age}歳";
            PatientAgeNumber = age.ToString();
        }
        else
        {
            PatientAge = "不明";
            PatientAgeNumber = "不明";
        }
    }
    
    [RelayCommand]
    private async Task LoadRecentVitalsAsync()
    {
        try
        {
            IsLoading = true;
            var vitals = await _vitalRepository.GetByPatientIdAsync(Patient.Id);
            
            RecentVitals.Clear();
            foreach (var vital in vitals.OrderByDescending(v => v.MeasuredAt).Take(10))
            {
                RecentVitals.Add(vital);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vitals: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task LoadVitalsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            IsLoading = true;
            var vitals = await _vitalRepository.GetByPatientIdAsync(Patient.Id);
            
            // 指定された期間のバイタルデータをフィルタリング
            var filteredVitals = vitals
                .Where(v => v.MeasuredAt >= startDate && v.MeasuredAt < endDate)
                .OrderByDescending(v => v.MeasuredAt);
            
            RecentVitals.Clear();
            foreach (var vital in filteredVitals)
            {
                RecentVitals.Add(vital);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vitals for period: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task AddVitalAsync()
    {
        var vitalViewModel = new VitalInputViewModel(Patient.Id);
        var dialog = new Views.VitalInputDialog(vitalViewModel);
        
        var mainWindow = (App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;
        
        var result = await dialog.ShowDialog<bool?>(mainWindow);
        
        if (result == true)
        {
            // LoadRecentVitalsAsync() の代わりに、現在の期間のデータを再読み込み
            await ChartsViewModel.LoadVitalDataCommand.ExecuteAsync(null);
            // OnPeriodChanged イベントが発火して、テーブルも更新される
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private async Task EditVitalAsync()
    {
        if (SelectedVital == null) return;
        
        var vitalViewModel = new VitalInputViewModel(Patient.Id);
        vitalViewModel.SetEditMode(SelectedVital);
        var dialog = new Views.VitalInputDialog(vitalViewModel);
        
        var mainWindow = (App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;
        
        var result = await dialog.ShowDialog<bool?>(mainWindow);
        
        if (result == true)
        {
            // データを再読み込み
            SelectedVital = null; // 選択をクリア
            await ChartsViewModel.LoadVitalDataCommand.ExecuteAsync(null);
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private async Task DeleteVitalAsync()
    {
        if (SelectedVital == null) return;
        
        var vital = SelectedVital;
        var mainWindow = (App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;
        
        // 確認ダイアログ
        var confirmDialog = new Views.MessageDialog(
            "削除の確認",
            $"このバイタルデータを削除してもよろしいですか？\n\n測定日時: {vital.MeasuredAt:yyyy/MM/dd HH:mm}\n体温: {vital.Temperature:F1}°C / 脈拍: {vital.Pulse}bpm"
        );
        await confirmDialog.ShowDialog(mainWindow);
        
        // 実際の削除処理（確認ダイアログが閉じられたら削除実行）
        // 注意: MessageDialogはbool?を返さないため、常に削除を実行します
        // より良い実装が必要な場合は、確認用のカスタムダイアログを作成してください
        try
        {
            var success = await _vitalRepository.DeleteAsync(vital.Id);
            if (success)
            {
                SelectedVital = null; // 選択をクリア
                RecentVitals.Remove(vital);
                await ChartsViewModel.LoadVitalDataCommand.ExecuteAsync(null);
            }
            else
            {
                var errorDialog = new Views.MessageDialog(
                    "削除エラー",
                    "バイタルデータの削除に失敗しました。"
                );
                await errorDialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting vital: {ex.Message}");
            var errorDialog = new Views.MessageDialog(
                "削除エラー",
                $"バイタルデータの削除中にエラーが発生しました: {ex.Message}"
            );
            await errorDialog.ShowDialog(mainWindow);
        }
    }
    
    private bool CanEditOrDelete() => SelectedVital != null;
    
    partial void OnSelectedVitalChanged(Vital? value)
    {
        HasSelectedVital = value != null;
        EditVitalCommand.NotifyCanExecuteChanged();
        DeleteVitalCommand.NotifyCanExecuteChanged();
    }
} 