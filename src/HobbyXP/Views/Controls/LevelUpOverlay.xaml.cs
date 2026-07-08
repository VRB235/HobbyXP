using System.Windows;
using System.Windows.Media.Animation;

namespace HobbyXP.Views.Controls;

public partial class LevelUpOverlay
{
    public LevelUpOverlay()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible && Resources["CelebrateStoryboard"] is Storyboard storyboard)
            storyboard.Begin(this, true);
        else if (Resources["CelebrateStoryboard"] is Storyboard running)
            running.Stop(this);
    }
}
