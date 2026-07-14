namespace CStoValuation.App.ViewModels;

/// <summary>
/// A fixed-size row of catalog cards. WPF has no built-in virtualizing wrap panel, so the
/// catalog grid is instead built as a virtualizing (vertical) list of rows, each rendering a
/// handful of cards horizontally — the standard way to get a virtualized card grid out of
/// stock WPF controls.
/// </summary>
internal sealed record CatalogRowViewModel(IReadOnlyList<CatalogItemViewModel> Items);
