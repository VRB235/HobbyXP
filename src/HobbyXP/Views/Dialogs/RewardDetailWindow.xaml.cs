using System.Windows;
using System.Windows.Input;
using HobbyXP.ViewModels.Achievements;

namespace HobbyXP.Views.Dialogs;

public partial class RewardDetailWindow : Window
{
    private readonly RewardDetailViewModel _viewModel;

    public RewardDetailWindow(RewardDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
        Closed += OnClosed;
    }

    public RewardDetailViewModel ViewModel => _viewModel;

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
        if (DialogResult != true)
            _viewModel.OnClosedWithoutSave();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        _viewModel.CancelCommand.Execute(null);

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !_viewModel.IsBusy)
            _viewModel.CancelCommand.Execute(null);
    }
}
