using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using VitalDesk.App.ViewModels;

namespace VitalDesk.App.Views;

public partial class MainWindow : Window
{
    private MainView? _mainView;
    private SettingsView? _settingsView;
    
    public MainWindow()
    {
        InitializeComponent();
        
        NavigationView.SelectionChanged += OnNavigationSelectionChanged;
        
        // Initialize views
        _mainView = new MainView();
        _mainView.DataContext = new MainViewModel();
    }
    
    private void OnNavigationSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            
            switch (tag)
            {
                case "patients":
                    if (_mainView == null)
                    {
                        _mainView = new MainView();
                        _mainView.DataContext = new MainViewModel();
                    }
                    NavigationView.Content = _mainView;
                    break;
                    
                case "settings":
                    if (_settingsView == null)
                    {
                        _settingsView = new SettingsView();
                        _settingsView.DataContext = new SettingsViewModel();
                    }
                    NavigationView.Content = _settingsView;
                    break;
                    
                case "reports":
                    // TODO: Implement reports view
                    break;
                    
                default:
                    NavigationView.Content = _mainView;
                    break;
            }
        }
    }
}