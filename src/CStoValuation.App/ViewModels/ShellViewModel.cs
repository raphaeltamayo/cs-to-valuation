using CommunityToolkit.Mvvm.ComponentModel;

namespace CStoValuation.App.ViewModels;

internal sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPage))]
    private NavItem _selectedNavItem;

    public ShellViewModel(
        InventoryPageViewModel inventoryPage,
        CatalogPageViewModel catalogPage,
        PerformancePageViewModel performancePage)
    {
        NavItems =
        [
            new NavItem("Inventory", inventoryPage),
            new NavItem("Catalog", catalogPage),
            new NavItem("Performance", performancePage),
        ];

        _selectedNavItem = NavItems[0];
    }

    public IReadOnlyList<NavItem> NavItems { get; }

    public object CurrentPage => SelectedNavItem.Page;
}
