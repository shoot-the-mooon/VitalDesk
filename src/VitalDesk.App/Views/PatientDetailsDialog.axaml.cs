using Avalonia.Controls;
using Avalonia.Interactivity;
using VitalDesk.App.ViewModels;

namespace VitalDesk.App.Views;

public partial class PatientDetailsDialog : Window
{
    public PatientDetailsDialog()
    {
        InitializeComponent();
    }
    
    public PatientDetailsDialog(PatientDetailsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
    
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
} 