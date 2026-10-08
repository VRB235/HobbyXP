using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace HobbyXP.Helpers;

/// <summary>
/// Al abrir el popup del <see cref="DatePicker"/>, pinta los días con actividad (verde)
/// y sin actividad (gris). El pintado es imperativo sobre cada <see cref="CalendarDayButton"/>:
/// los DataTrigger/MultiBinding del popup no son fiables.
/// </summary>
public static class DatePickerTrainingHighlightHelper
{
    public const string DayKindNone = "None";
    public const string DayKindActive = "Active";
    public const string DayKindIdle = "Idle";

    private static readonly Brush ActiveBackground = Freeze(new SolidColorBrush(Color.FromRgb(0x1B, 0x3A, 0x2E)));
    private static readonly Brush ActiveBorder = Freeze(new SolidColorBrush(Color.FromRgb(0x2F, 0x6B, 0x4F)));
    private static readonly Brush ActiveForeground = Freeze(new SolidColorBrush(Color.FromRgb(0x00, 0xE6, 0x76)));
    private static readonly Brush IdleBackground = Freeze(new SolidColorBrush(Color.FromRgb(0x16, 0x1A, 0x22)));
    private static readonly Brush IdleForeground = Freeze(new SolidColorBrush(Color.FromRgb(0x5C, 0x66, 0x7A)));

    public static readonly DependencyProperty ActiveLocalDatesProperty =
        DependencyProperty.RegisterAttached(
            "ActiveLocalDates",
            typeof(IEnumerable),
            typeof(DatePickerTrainingHighlightHelper),
            new FrameworkPropertyMetadata(null, OnActiveLocalDatesChanged));

    /// <summary>
    /// Engancha el DatePicker al Loaded/CalendarOpened (p. ej. desde el estilo global).
    /// </summary>
    public static readonly DependencyProperty WatchDropDownProperty =
        DependencyProperty.RegisterAttached(
            "WatchDropDown",
            typeof(bool),
            typeof(DatePickerTrainingHighlightHelper),
            new PropertyMetadata(false, OnWatchDropDownChanged));

    public static readonly DependencyProperty DayKindProperty =
        DependencyProperty.RegisterAttached(
            "DayKind",
            typeof(string),
            typeof(DatePickerTrainingHighlightHelper),
            new PropertyMetadata(DayKindNone));

    private static readonly DependencyProperty IsHookedProperty =
        DependencyProperty.RegisterAttached(
            "IsHooked",
            typeof(bool),
            typeof(DatePickerTrainingHighlightHelper),
            new PropertyMetadata(false));

    private static readonly DependencyProperty BoundCalendarProperty =
        DependencyProperty.RegisterAttached(
            "BoundCalendar",
            typeof(Calendar),
            typeof(DatePickerTrainingHighlightHelper),
            new PropertyMetadata(null));

    private static readonly DependencyProperty LayoutHookProperty =
        DependencyProperty.RegisterAttached(
            "LayoutHook",
            typeof(LayoutUpdateHook),
            typeof(DatePickerTrainingHighlightHelper),
            new PropertyMetadata(null));

    public static void SetActiveLocalDates(DependencyObject element, IEnumerable? value) =>
        element.SetValue(ActiveLocalDatesProperty, value);

    public static IEnumerable? GetActiveLocalDates(DependencyObject element) =>
        (IEnumerable?)element.GetValue(ActiveLocalDatesProperty);

    public static void SetWatchDropDown(DependencyObject element, bool value) =>
        element.SetValue(WatchDropDownProperty, value);

    public static bool GetWatchDropDown(DependencyObject element) =>
        (bool)element.GetValue(WatchDropDownProperty);

    public static void SetDayKind(DependencyObject element, string? value) =>
        element.SetValue(DayKindProperty, value ?? DayKindNone);

    public static string GetDayKind(DependencyObject element) =>
        (string)element.GetValue(DayKindProperty);

    private static Brush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }

    private static void OnActiveLocalDatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DatePicker picker)
            return;

        EnsureHooked(picker);
        ScheduleSync(picker);
    }

    private static void OnWatchDropDownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DatePicker picker && e.NewValue is true)
            EnsureHooked(picker);
    }

    private static void EnsureHooked(DatePicker picker)
    {
        if ((bool)picker.GetValue(IsHookedProperty))
            return;

        picker.SetValue(IsHookedProperty, true);
        picker.Loaded += OnPickerLoaded;
        picker.CalendarOpened += OnCalendarOpened;
        picker.CalendarClosed += OnCalendarClosed;
        picker.Unloaded += OnPickerUnloaded;
    }

    private static void OnPickerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker picker)
            EnsureHooked(picker);
    }

    private static void OnCalendarOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not DatePicker picker)
            return;

        ScheduleSync(picker);
    }

    private static void ScheduleSync(DatePicker picker)
    {
        void Run() => SyncToOpenCalendar(picker);

        picker.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, Run);
        picker.Dispatcher.BeginInvoke(DispatcherPriority.Input, Run);
        picker.Dispatcher.BeginInvoke(DispatcherPriority.Background, Run);
        picker.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, Run);
        picker.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, Run);
    }

    private static void OnCalendarClosed(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker picker)
            DetachCalendar(picker);
    }

    private static void OnPickerUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DatePicker picker)
            return;

        DetachCalendar(picker);
        picker.Loaded -= OnPickerLoaded;
        picker.CalendarOpened -= OnCalendarOpened;
        picker.CalendarClosed -= OnCalendarClosed;
        picker.Unloaded -= OnPickerUnloaded;
        picker.SetValue(IsHookedProperty, false);
    }

    private static void SyncToOpenCalendar(DatePicker picker)
    {
        picker.ApplyTemplate();
        var calendar = FindCalendar(picker);
        // Sin calendar visible (popup cerrado) no hay nada que pintar.
        if (calendar is null || calendar.IsVisible == false && !picker.IsDropDownOpen)
            return;

        calendar.ApplyTemplate();
        if (FindVisualChild<CalendarItem>(calendar) is { } item)
            item.ApplyTemplate();

        BindCalendar(picker, calendar);

        var dates = ResolveActiveDates(picker);
        // Copia local (el Calendar del Popup no hereda DPs del DatePicker).
        calendar.SetCurrentValue(ActiveLocalDatesProperty, dates);

        var painted = ApplyHighlights(calendar, dates);
        if (!painted)
            AttachLayoutHook(picker, calendar);
    }

    private static IEnumerable? ResolveActiveDates(DatePicker picker)
    {
        var dates = GetActiveLocalDates(picker);
        if (dates is not null)
            return dates;

        // Fallback si el attached binding aún no aplicó.
        var dc = picker.DataContext;
        if (dc is null)
            return null;

        foreach (var name in new[]
                 {
                     "SessionTrainingLocalDates",
                     "ActivityCalendarLocalDates",
                     "SeriesWatchLocalDates"
                 })
        {
            var prop = dc.GetType().GetProperty(name);
            if (prop?.GetValue(dc) is IEnumerable enumerable)
                return enumerable;
        }

        return null;
    }

    private static void BindCalendar(DatePicker picker, Calendar calendar)
    {
        var previous = picker.GetValue(BoundCalendarProperty) as Calendar;
        if (ReferenceEquals(previous, calendar))
            return;

        if (previous is not null)
        {
            previous.DisplayDateChanged -= OnDisplayDateChanged;
            DetachLayoutHook(previous);
        }

        picker.SetValue(BoundCalendarProperty, calendar);
        calendar.DisplayDateChanged += OnDisplayDateChanged;
        calendar.Tag = picker;
    }

    private static void DetachCalendar(DatePicker picker)
    {
        if (picker.GetValue(BoundCalendarProperty) is not Calendar calendar)
            return;

        calendar.DisplayDateChanged -= OnDisplayDateChanged;
        DetachLayoutHook(calendar);
        ClearHighlights(calendar);
        calendar.ClearValue(ActiveLocalDatesProperty);
        if (ReferenceEquals(calendar.Tag, picker))
            calendar.Tag = null;
        picker.SetValue(BoundCalendarProperty, null);
    }

    private static void OnDisplayDateChanged(object? sender, CalendarDateChangedEventArgs e)
    {
        if (sender is not Calendar calendar)
            return;

        calendar.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            if (calendar.Tag is DatePicker picker)
                SyncToOpenCalendar(picker);
            else
                ApplyHighlights(calendar, GetActiveLocalDates(calendar));
        });
    }

    private static void AttachLayoutHook(DatePicker picker, Calendar calendar)
    {
        if (calendar.GetValue(LayoutHookProperty) is LayoutUpdateHook)
            return;

        var hook = new LayoutUpdateHook(picker, calendar);
        calendar.SetValue(LayoutHookProperty, hook);
        calendar.LayoutUpdated += hook.OnLayoutUpdated;
    }

    private static void DetachLayoutHook(Calendar calendar)
    {
        if (calendar.GetValue(LayoutHookProperty) is not LayoutUpdateHook hook)
            return;

        calendar.LayoutUpdated -= hook.OnLayoutUpdated;
        calendar.ClearValue(LayoutHookProperty);
    }

    private static bool ApplyHighlights(Calendar calendar, IEnumerable? dates)
    {
        var buttons = FindVisualChildren<CalendarDayButton>(calendar).ToList();
        if (buttons.Count == 0)
            return false;

        if (dates is null)
        {
            ClearHighlights(calendar);
            return true;
        }

        var active = ToDateOnlySet(dates);
        foreach (var dayButton in buttons)
        {
            if (!TryGetButtonDate(dayButton, out var day) || dayButton.IsInactive)
            {
                ClearButtonHighlight(dayButton);
                continue;
            }

            if (active.Contains(DateOnly.FromDateTime(day)))
                PaintActive(dayButton);
            else
                PaintIdle(dayButton);
        }

        return true;
    }

    private static void ClearHighlights(Calendar calendar)
    {
        foreach (var dayButton in FindVisualChildren<CalendarDayButton>(calendar))
            ClearButtonHighlight(dayButton);
    }

    private static void PaintActive(CalendarDayButton dayButton)
    {
        SetDayKind(dayButton, DayKindActive);
        dayButton.Background = ActiveBackground;
        dayButton.Foreground = ActiveForeground;
        dayButton.BorderBrush = ActiveBorder;
        dayButton.FontWeight = FontWeights.SemiBold;
        dayButton.BorderThickness = new Thickness(1);
        // Pintar el Border del template: los Trigger del ControlTemplate pisan TemplateBinding.
        if (FindVisualChild<Border>(dayButton) is { } bd && !dayButton.IsSelected && !dayButton.IsMouseOver)
        {
            bd.Background = ActiveBackground;
            bd.BorderBrush = ActiveBorder;
        }
    }

    private static void PaintIdle(CalendarDayButton dayButton)
    {
        SetDayKind(dayButton, DayKindIdle);
        dayButton.Background = IdleBackground;
        dayButton.Foreground = IdleForeground;
        dayButton.BorderBrush = Brushes.Transparent;
        dayButton.FontWeight = FontWeights.Normal;
        dayButton.BorderThickness = new Thickness(1);
        if (FindVisualChild<Border>(dayButton) is { } bd && !dayButton.IsSelected && !dayButton.IsMouseOver)
        {
            bd.Background = IdleBackground;
            bd.BorderBrush = Brushes.Transparent;
        }
    }

    private static void ClearButtonHighlight(CalendarDayButton dayButton)
    {
        SetDayKind(dayButton, DayKindNone);
        dayButton.ClearValue(Control.BackgroundProperty);
        dayButton.ClearValue(Control.ForegroundProperty);
        dayButton.ClearValue(Control.BorderBrushProperty);
        dayButton.ClearValue(Control.FontWeightProperty);
        dayButton.ClearValue(Control.BorderThicknessProperty);
        if (FindVisualChild<Border>(dayButton) is { } bd)
        {
            bd.ClearValue(Border.BackgroundProperty);
            bd.ClearValue(Border.BorderBrushProperty);
        }
    }

    private static bool TryGetButtonDate(CalendarDayButton dayButton, out DateTime day)
    {
        switch (dayButton.DataContext)
        {
            case DateTime dt:
                day = dt;
                return true;
            case DateOnly dateOnly:
                day = dateOnly.ToDateTime(TimeOnly.MinValue);
                return true;
        }

        if (dayButton.Content is DateTime contentDt)
        {
            day = contentDt;
            return true;
        }

        if (dayButton.Content is string text && DateTime.TryParse(text, out var parsed))
        {
            day = parsed;
            return true;
        }

        day = default;
        return false;
    }

    private static HashSet<DateOnly> ToDateOnlySet(IEnumerable dates)
    {
        var set = new HashSet<DateOnly>();
        foreach (var item in dates)
        {
            switch (item)
            {
                case DateTime dt:
                    set.Add(DateOnly.FromDateTime(dt));
                    break;
                case DateOnly d:
                    set.Add(d);
                    break;
            }
        }

        return set;
    }

    private static Calendar? FindCalendar(DatePicker picker)
    {
        if (picker.Template?.FindName("PART_Popup", picker) is Popup popup)
        {
            popup.ApplyTemplate();

            if (popup.Child is Calendar direct)
                return direct;

            if (popup.Child is not null)
            {
                var nested = FindVisualChild<Calendar>(popup.Child);
                if (nested is not null)
                    return nested;
            }
        }

        return FindVisualChild<Calendar>(picker);
    }

    private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                return match;

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
                return nested;
        }

        return null;
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

    private sealed class LayoutUpdateHook
    {
        private readonly DatePicker _picker;
        private readonly Calendar _calendar;
        private int _attempts;

        public LayoutUpdateHook(DatePicker picker, Calendar calendar)
        {
            _picker = picker;
            _calendar = calendar;
        }

        public void OnLayoutUpdated(object? sender, EventArgs e)
        {
            _attempts++;
            if (ApplyHighlights(_calendar, GetActiveLocalDates(_picker)) || _attempts >= 12)
                DetachLayoutHook(_calendar);
        }
    }
}
