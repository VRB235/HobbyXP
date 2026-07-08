using System.Windows;
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
        Loaded += OnLoaded;
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
