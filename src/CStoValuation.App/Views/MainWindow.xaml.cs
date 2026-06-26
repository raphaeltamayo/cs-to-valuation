using System.Windows;
using CStoValuation.App.ViewModels;

namespace CStoValuation.App.Views;

/// <summary>
/// The shell window. Its view-model is supplied by the DI container (constructor injection),
/// which is why <see cref="App"/> resolves the window from the host rather than letting XAML
/// new it up via StartupUri.
/// </summary>
internal partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
