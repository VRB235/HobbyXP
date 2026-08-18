using System.Windows;
using HobbyXP.Helpers;

namespace HobbyXP.Views.Controls;

public partial class MedalUnlockOverlay
{
    public MedalUnlockOverlay()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            CelebrationSoundPlayer.PlayLevelUp();
    }
}
