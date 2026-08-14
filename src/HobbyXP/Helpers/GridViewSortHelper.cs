using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace HobbyXP.Helpers;

/// <summary>
/// Ordenación por clic en cabeceras de un <see cref="ListView"/> con <see cref="GridView"/>.
/// Usa el Path de <see cref="GridViewColumn.DisplayMemberBinding"/> o
/// <see cref="SortPropertyProperty"/> cuando el valor mostrado es una etiqueta.
/// </summary>
public static class GridViewSortHelper
{
    private const string AscendingGlyph = " ▲";
    private const string DescendingGlyph = " ▼";

    public static readonly DependencyProperty EnableSortProperty =
        DependencyProperty.RegisterAttached(
            "EnableSort",
            typeof(bool),
            typeof(GridViewSortHelper),
            new PropertyMetadata(false, OnEnableSortChanged));

    public static readonly DependencyProperty SortPropertyProperty =
        DependencyProperty.RegisterAttached(
            "SortProperty",
            typeof(string),
            typeof(GridViewSortHelper),
            new PropertyMetadata(null));

    private static readonly DependencyProperty BaseHeaderProperty =
        DependencyProperty.RegisterAttached(
            "BaseHeader",
            typeof(object),
            typeof(GridViewSortHelper));

    private static readonly DependencyProperty CurrentSortColumnProperty =
        DependencyProperty.RegisterAttached(
            "CurrentSortColumn",
            typeof(GridViewColumn),
            typeof(GridViewSortHelper));

    private static readonly DependencyProperty CurrentSortDirectionProperty =
        DependencyProperty.RegisterAttached(
            "CurrentSortDirection",
            typeof(ListSortDirection),
            typeof(GridViewSortHelper),
            new PropertyMetadata(ListSortDirection.Ascending));

    public static bool GetEnableSort(DependencyObject obj) =>
        (bool)obj.GetValue(EnableSortProperty);

    public static void SetEnableSort(DependencyObject obj, bool value) =>
        obj.SetValue(EnableSortProperty, value);

    public static string? GetSortProperty(DependencyObject obj) =>
        (string?)obj.GetValue(SortPropertyProperty);

    public static void SetSortProperty(DependencyObject obj, string? value) =>
        obj.SetValue(SortPropertyProperty, value);

    private static void OnEnableSortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListView listView)
            return;

        if ((bool)e.NewValue)
        {
            listView.AddHandler(ButtonBase.ClickEvent, (RoutedEventHandler)OnColumnHeaderClick);
            listView.Loaded += OnListViewLoaded;
            if (listView.IsLoaded)
                MarkSortableHeaderCursors(listView);
        }
        else
        {
            listView.RemoveHandler(ButtonBase.ClickEvent, (RoutedEventHandler)OnColumnHeaderClick);
            listView.Loaded -= OnListViewLoaded;
            ClearHeaderGlyphs(listView);
            listView.ClearValue(CurrentSortColumnProperty);
        }
    }

    private static void OnListViewLoaded(object sender, RoutedEventArgs e) =>
        MarkSortableHeaderCursors((ListView)sender);

    private static void OnColumnHeaderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ListView listView)
            return;

        if (e.OriginalSource is not GridViewColumnHeader { Role: GridViewColumnHeaderRole.Normal, Column: { } column })
            return;

        var sortProperty = ResolveSortProperty(column);
        if (string.IsNullOrWhiteSpace(sortProperty))
            return;

        var direction = ListSortDirection.Ascending;
        var currentColumn = listView.GetValue(CurrentSortColumnProperty) as GridViewColumn;
        if (ReferenceEquals(currentColumn, column))
        {
            var currentDirection = (ListSortDirection)listView.GetValue(CurrentSortDirectionProperty);
            direction = currentDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }

        if (!TryApplySort(listView, sortProperty, direction))
            return;

        listView.SetValue(CurrentSortColumnProperty, column);
        listView.SetValue(CurrentSortDirectionProperty, direction);
        UpdateHeaderGlyphs(listView, column, direction);
        MarkSortableHeaderCursors(listView);
        e.Handled = true;
    }

    private static bool TryApplySort(ListView listView, string sortProperty, ListSortDirection direction)
    {
        if (listView.ItemsSource is null)
            return false;

        var view = CollectionViewSource.GetDefaultView(listView.ItemsSource);
        if (view?.CanSort != true)
            return false;

        using (view.DeferRefresh())
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(sortProperty, direction));
        }

        return true;
    }

    private static string? ResolveSortProperty(GridViewColumn column)
    {
        var explicitProperty = GetSortProperty(column);
        if (!string.IsNullOrWhiteSpace(explicitProperty))
            return explicitProperty;

        if (column.DisplayMemberBinding is Binding { Path.Path: { Length: > 0 } path })
            return path;

        return null;
    }

    private static void EnsureBaseHeader(GridViewColumn column)
    {
        if (column.ReadLocalValue(BaseHeaderProperty) != DependencyProperty.UnsetValue)
            return;

        column.SetValue(BaseHeaderProperty, column.Header);
    }

    private static void UpdateHeaderGlyphs(ListView listView, GridViewColumn activeColumn, ListSortDirection direction)
    {
        if (listView.View is not GridView gridView)
            return;

        foreach (var column in gridView.Columns)
        {
            EnsureBaseHeader(column);
            var baseHeader = column.GetValue(BaseHeaderProperty);
            var baseText = baseHeader?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(ResolveSortProperty(column)))
            {
                column.Header = baseHeader;
                continue;
            }

            if (ReferenceEquals(column, activeColumn))
            {
                var glyph = direction == ListSortDirection.Ascending ? AscendingGlyph : DescendingGlyph;
                column.Header = string.IsNullOrEmpty(baseText) ? glyph.Trim() : baseText + glyph;
            }
            else
            {
                column.Header = baseHeader;
            }
        }
    }

    private static void ClearHeaderGlyphs(ListView listView)
    {
        if (listView.View is not GridView gridView)
            return;

        foreach (var column in gridView.Columns)
        {
            if (column.ReadLocalValue(BaseHeaderProperty) == DependencyProperty.UnsetValue)
                continue;

            column.Header = column.GetValue(BaseHeaderProperty);
            column.ClearValue(BaseHeaderProperty);
        }
    }

    private static void MarkSortableHeaderCursors(ListView listView)
    {
        listView.Dispatcher.BeginInvoke(() =>
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(listView))
            {
                if (header.Role != GridViewColumnHeaderRole.Normal || header.Column is null)
                    continue;

                if (string.IsNullOrWhiteSpace(ResolveSortProperty(header.Column)))
                    continue;

                header.Cursor = Cursors.Hand;
                header.ToolTip ??= "Clic para ordenar";
            }
        }, DispatcherPriority.Loaded);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;

            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
