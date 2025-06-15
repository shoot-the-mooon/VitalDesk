using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VitalDesk.App.Views;

public partial class MessageDialog : Window
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    public MessageDialog(string title, string message) : this()
    {
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
} 