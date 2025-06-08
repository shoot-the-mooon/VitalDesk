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
            // TODO: Show confirmation dialog
            var success = await _patientRepository.DeleteAsync(patient.Id);
            if (success)
            {
                Patients.Remove(patient);
            }
        }
        catch (Exception ex)
        {
            // TODO: Show error message to user
            System.Diagnostics.Debug.WriteLine($"Error deleting patient: {ex.Message}");
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