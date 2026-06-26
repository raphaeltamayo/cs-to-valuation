using System.Windows.Controls;

namespace CStoValuation.App.Views;

/// <summary>
/// The inventory grid. It carries no logic of its own — it inherits the
/// <see cref="ViewModels.MainViewModel"/> from its parent's DataContext and binds to the
/// already-filtered, already-sorted <c>ItemsView</c>.
/// </summary>
public partial class InventoryView : UserControl
{
    public InventoryView() => InitializeComponent();
}
