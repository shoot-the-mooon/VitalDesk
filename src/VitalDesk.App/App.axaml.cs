using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using VitalDesk.App.ViewModels;
using VitalDesk.App.Views;
using VitalDesk.App.Services;

namespace VitalDesk.App;

public partial class App : Application
{
    private BackupService? _backupService;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            _backupService = new BackupService();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            // Handle application exit for backup
            desktop.Exit += OnApplicationExit;
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private async void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_backupService != null)
        {
            try
            {
                await _backupService.CreateBackupAsync();
            }
            catch
            {
                // Ignore backup errors during exit
            }
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}