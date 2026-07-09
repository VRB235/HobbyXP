using System.Windows;
using System.Windows.Controls;

namespace HobbyXP.Helpers;

/// <summary>
/// Reparte el ancho de las columnas de un <see cref="ListView"/> con <see cref="GridView"/>
/// de forma proporcional al ancho inicial definido en XAML.
/// </summary>
public static class GridViewStretchHelper
{
    public static readonly DependencyProperty StretchColumnsProperty =
        DependencyProperty.RegisterAttached(
            "StretchColumns",
            typeof(bool),
            typeof(GridViewStretchHelper),
            new PropertyMetadata(false, OnStretchColumnsChanged));

    private static readonly DependencyProperty ColumnWeightRatiosProperty =
        DependencyProperty.RegisterAttached(
            "ColumnWeightRatios",
            typeof(double[]),
            typeof(GridViewStretchHelper));

    public static bool GetStretchColumns(DependencyObject obj) => (bool)obj.GetValue(StretchColumnsProperty);

    public static void SetStretchColumns(DependencyObject obj, bool value) => obj.SetValue(StretchColumnsProperty, value);

    private static void OnStretchColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListView listView)
            return;

        if ((bool)e.NewValue)
        {
            listView.Loaded += OnListViewLoaded;
            listView.SizeChanged += OnListViewSizeChanged;

            if (listView.IsLoaded)
                ApplyStretch(listView);
        }
        else
        {
            listView.Loaded -= OnListViewLoaded;
            listView.SizeChanged -= OnListViewSizeChanged;
            listView.ClearValue(ColumnWeightRatiosProperty);
        }
    }

    private static void OnListViewLoaded(object sender, RoutedEventArgs e) =>
        ApplyStretch((ListView)sender);

    private static void OnListViewSizeChanged(object sender, SizeChangedEventArgs e) =>
        ApplyStretch((ListView)sender);

    private static void ApplyStretch(ListView listView)
    {
        if (listView.View is not GridView gridView || gridView.Columns.Count == 0)
            return;

        var ratios = EnsureColumnRatios(listView, gridView);
        var availableWidth = listView.ActualWidth - 2;

        if (availableWidth <= 0)
            return;

        var assignedWidth = 0d;

        for (var i = 0; i < gridView.Columns.Count; i++)
        {
            var targetWidth = i == gridView.Columns.Count - 1
                ? Math.Max(48, availableWidth - assignedWidth)
                : Math.Max(48, Math.Floor(availableWidth * ratios[i]));

            if (Math.Abs(gridView.Columns[i].Width - targetWidth) > 0.5)
                gridView.Columns[i].Width = targetWidth;

            assignedWidth += gridView.Columns[i].Width;
        }
    }

    private static double[] EnsureColumnRatios(ListView listView, GridView gridView)
    {
        if (listView.GetValue(ColumnWeightRatiosProperty) is double[] cachedRatios &&
            cachedRatios.Length == gridView.Columns.Count)
        {
            return cachedRatios;
        }

        var ratios = new double[gridView.Columns.Count];
        var totalWeight = 0d;

        for (var i = 0; i < gridView.Columns.Count; i++)
        {
            var weight = gridView.Columns[i].Width;
            if (double.IsNaN(weight) || weight <= 0)
                weight = 1;

            ratios[i] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0)
            totalWeight = gridView.Columns.Count;

        for (var i = 0; i < ratios.Length; i++)
            ratios[i] /= totalWeight;

        listView.SetValue(ColumnWeightRatiosProperty, ratios);
        return ratios;
    }
}
