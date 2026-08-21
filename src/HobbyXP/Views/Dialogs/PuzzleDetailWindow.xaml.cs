using System.Windows;
using System.Windows.Input;
using HobbyXP.ViewModels.Entertainment;

namespace HobbyXP.Views.Dialogs;

public partial class PuzzleDetailWindow : Window
{
    private readonly PuzzleDetailViewModel _viewModel;

    public PuzzleDetailWindow(PuzzleDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
        Closed += OnClosed;
    }

    public PuzzleDetailViewModel ViewModel => _viewModel;

    private void OnRequestClose(bool accepted)
    {
        try
        {
            DialogResult = accepted;
        }
        catch (InvalidOperationException)
        {
            Close();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.RequestClose -= OnRequestClose;
        Closed -= OnClosed;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        _viewModel.CancelCommand.Execute(null);

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !_viewModel.IsBusy)
            _viewModel.CancelCommand.Execute(null);
    }
}
