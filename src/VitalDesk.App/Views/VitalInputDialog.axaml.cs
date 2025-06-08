using System;
using Avalonia.Controls;
using VitalDesk.App.ViewModels;

namespace VitalDesk.App.Views;

public partial class VitalInputDialog : Window
{
    public VitalInputDialog()
    {
        InitializeComponent();
    }
    
    public VitalInputDialog(VitalInputViewModel viewModel) : this()
    {
        DataContext = viewModel;
        Title = viewModel.IsEditMode ? "バイタルサイン編集" : "バイタルサイン入力";
        viewModel.RequestClose += OnRequestClose;
        
        // プロパティ変更を監視してタイトルを更新
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsEditMode))
            {
                Title = viewModel.IsEditMode ? "バイタルサイン編集" : "バイタルサイン入力";
            }
        };
    }
    
    private void OnRequestClose(object? sender, bool saved)
    {
        Close(saved);
    }
    
    private void OnSetTodayClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is VitalInputViewModel viewModel)
        {
            viewModel.MeasuredAt = DateTimeOffset.Now;
        }
    }
    
    private void OnSetYesterdayClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is VitalInputViewModel viewModel)
        {
            viewModel.MeasuredAt = DateTimeOffset.Now.AddDays(-1);
        }
    }
    
    private void OnSetTomorrowClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is VitalInputViewModel viewModel)
        {
            viewModel.MeasuredAt = DateTimeOffset.Now.AddDays(1);
        }
    }
} 