using System.Windows;
using CStoValuation.App.ViewModels;

namespace CStoValuation.App.Views;

internal partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
