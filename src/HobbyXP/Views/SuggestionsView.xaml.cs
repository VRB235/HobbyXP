using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HobbyXP.Models.Feedback;
using HobbyXP.ViewModels.Suggestions;

namespace HobbyXP.Views;

public partial class SuggestionsView : UserControl
{
    public SuggestionsView()
    {
        InitializeComponent();
    }

    private void OnSuggestionsMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
            return;

        if (sender is not ListView listView || listView.SelectedItem is not Suggestion suggestion)
            return;

        if (DataContext is not SuggestionsViewModel viewModel)
            return;

        viewModel.OpenSuggestionDetailCommand.Execute(suggestion);
        e.Handled = true;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
