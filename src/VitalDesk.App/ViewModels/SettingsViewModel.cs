using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalDesk.App.Services;
using VitalDesk.Core.Repositories;

namespace VitalDesk.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly BackupService _backupService;
    private readonly CsvExportService _csvExportService;
    private readonly IPatientRepository _patientRepository;
    private readonly IVitalRepository _vitalRepository;
    
    [ObservableProperty]
    private bool _isProcessing;
    
    [ObservableProperty]
    private string _statusMessage = string.Empty;
    
    [ObservableProperty]
    private bool _autoBackupEnabled = true;
    
    [ObservableProperty]
    private int _backupRetentionDays = 30;
    
    public SettingsViewModel()
    {
        _backupService = new BackupService();
        _csvExportService = new CsvExportService();
        _patientRepository = new PatientRepository();
        _vitalRepository = new VitalRepository();
    }
    
    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Creating backup...";
            
            var success = await _backupService.CreateBackupAsync();
            
            if (success)
            {
                StatusMessage = "Backup created successfully.";
            }
            else
            {
                StatusMessage = "Failed to create backup. Check logs for details.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating backup: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    [RelayCommand]
    private async Task ExportPatientsAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Exporting patients...";
            
            var patients = await _patientRepository.GetAllAsync();
            var fileName = $"Patients_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            
            var success = await _csvExportService.ExportPatientsAsync(patients, filePath);
            
            if (success)
            {
                StatusMessage = $"Patients exported to: {filePath}";
            }
            else
            {
                StatusMessage = "Failed to export patients.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting patients: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    [RelayCommand]
    private async Task ExportVitalsAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Exporting vital signs...";
            
            var vitals = await _vitalRepository.GetAllAsync();
            var fileName = $"VitalSigns_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            
            var success = await _csvExportService.ExportVitalsAsync(vitals, filePath);
            
            if (success)
            {
                StatusMessage = $"Vital signs exported to: {filePath}";
            }
            else
            {
                StatusMessage = "Failed to export vital signs.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting vital signs: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    [RelayCommand]
    private Task CleanupOldBackupsAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Cleaning up old backups...";
            
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var backupDirectory = Path.Combine(baseDirectory, "Backup");
            
            if (!Directory.Exists(backupDirectory))
            {
                StatusMessage = "No backup directory found.";
                return Task.CompletedTask;
            }
            
            var cutoffDate = DateTime.Now.AddDays(-BackupRetentionDays);
            var backupFiles = Directory.GetFiles(backupDirectory, "*.zip");
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
            
            StatusMessage = $"Deleted {deletedCount} old backup files.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error cleaning up backups: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
        
        return Task.CompletedTask;
    }
    
    [RelayCommand]
    private void OpenBackupFolder()
    {
        try
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var backupDirectory = Path.Combine(baseDirectory, "Backup");
            
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }
            
            // Open folder in file explorer
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = backupDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening backup folder: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void OpenLogsFolder()
    {
        try
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var logsDirectory = Path.Combine(baseDirectory, "Logs");
            
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            
            // Open folder in file explorer
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = logsDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening logs folder: {ex.Message}";
        }
    }
} 