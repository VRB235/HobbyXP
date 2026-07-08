using System.Windows;
using HobbyXP.Data;
using HobbyXP.Services;
using HobbyXP.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HobbyXP;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(static services =>
            {
                services.AddHobbyXpData();
                services.AddHobbyXpServices();
                services.AddHobbyXpViewModels();
            })
            .Build();

        await _host.Services.EnsureHobbyXpDatabaseAsync();

        var mainWindow = new MainWindow(_host.Services);
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
            await _host.StopAsync();

        _host?.Dispose();
        base.OnExit(e);
    }
}
