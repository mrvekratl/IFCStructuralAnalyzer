using IFCStructuralAnalyzer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;


namespace IFCStructuralAnalyzer.Presentation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Infrastructure layer services (DbContext, Repositories)
                    services.AddInfrastructure();

                    // Application layer services (sonra ekleyeceğiz)
                    // services.AddApplication();

                    // ViewModels (sonra ekleyeceğiz)
                    // services.AddTransient<MainViewModel>();
                    // services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = new MainWindow();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(e);
        }
    }

}
