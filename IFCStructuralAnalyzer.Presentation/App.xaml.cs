using IFCStructuralAnalyzer.Application;
using IFCStructuralAnalyzer.Infrastructure;
using IFCStructuralAnalyzer.Presentation.Services;
using IFCStructuralAnalyzer.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;

namespace IFCStructuralAnalyzer.Presentation
{
    public partial class App : System.Windows.Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Infrastructure Layer (Data Access)
                    services.AddInfrastructure();

                    // Application Layer (Business Logic)
                    services.AddApplication();

                    // Presentation Layer Services
                    services.AddSingleton<Rendering3DService>();

                    // ViewModels
                    services.AddTransient<MainViewModel>();

                    // Views
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                await _host.StartAsync();

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Application startup error: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                using (_host)
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                // Log error during shutdown if needed
                System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}