using System.Windows.Controls;
using System.Windows.Input;
using HobbyXP.Services;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.Views.Controls;

public partial class HobbyXpBanner : UserControl
{
    public HobbyXpBanner() => InitializeComponent();

    private void OnNearestRewardImageClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not HobbyProgressPresenter presenter
            || string.IsNullOrWhiteSpace(presenter.NearestRewardImagePath))
        {
            return;
        }

        new ImagePreviewService().Show(
            presenter.NearestRewardImagePath,
            presenter.NearestRewardText ?? "Premio");
        e.Handled = true;
    }
}
