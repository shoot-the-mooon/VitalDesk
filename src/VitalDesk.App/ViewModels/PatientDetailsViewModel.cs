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
        
        CalculateAge();
        _ = LoadRecentVitalsAsync();
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
            await LoadRecentVitalsAsync();
            await ChartsViewModel.LoadVitalDataCommand.ExecuteAsync(null);
        }
    }
} 