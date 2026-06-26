using System.Windows;
using CStoValuation.App.ViewModels;

namespace CStoValuation.App.Views;

internal partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
