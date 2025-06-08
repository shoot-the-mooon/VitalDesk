using System;
using Avalonia.Controls;
using VitalDesk.App.ViewModels;

namespace VitalDesk.App.Views;

public partial class PatientInputDialog : Window
{
    public PatientInputDialog()
    {
        InitializeComponent();
    }
    
    public PatientInputDialog(PatientInputViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
    }
    
    private void OnRequestClose(object? sender, bool saved)
    {
        Close(saved);
    }
    
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is PatientInputViewModel viewModel)
        {
            viewModel.RequestClose -= OnRequestClose;
        }
        base.OnClosed(e);
    }
} 