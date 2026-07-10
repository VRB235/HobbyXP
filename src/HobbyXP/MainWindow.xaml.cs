using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using HobbyXP.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HobbyXP;

public partial class MainWindow : Window
{
    private readonly IServiceScope _scope;

    public MainWindow(IServiceProvider serviceProvider)
    {
        _scope = serviceProvider.CreateScope();
        DataContext = _scope.ServiceProvider.GetRequiredService<MainViewModel>();
        InitializeComponent();
        ApplyWindowIcon();
        Loaded += OnLoaded;
    }

    private void ApplyWindowIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "HobbyXP.ico");
        if (!File.Exists(iconPath))
            return;

        Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel)
            await mainViewModel.InitializeAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        _scope.Dispose();
        base.OnClosed(e);
    }
}
