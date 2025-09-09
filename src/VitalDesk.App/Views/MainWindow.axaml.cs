using Avalonia.Controls;
using VitalDesk.App.ViewModels;

namespace VitalDesk.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize main view
        var mainView = new MainView();
        mainView.DataContext = new MainViewModel();
        MainContent.Content = mainView;
    }
}