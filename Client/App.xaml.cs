using Client.Services;
using Client.ViewModels;
using Client.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var services = new ServiceCollection();
            // DI 등록

            string hubUrl = Environment.GetEnvironmentVariable("ORDERBOOK_HUB_URL")
                         ?? "http://localhost:5000/orderbook";
            services.AddSingleton<IRealtimeConnectionService>(provider =>
                new SignalRClientService(hubUrl));

            services.AddSingleton<MainViewModel>();
            services.AddTransient<Func<string, OrderBookPanelViewModel>>(provider => ticker =>
            {
                var connection = provider.GetRequiredService<IRealtimeConnectionService>();
                return new OrderBookPanelViewModel(connection, ticker);
            });                        

            ServiceProvider = services.BuildServiceProvider();

            // MainWindow 생성 및 DataContext 주입
            var mainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
            mainWindow.Show();
        }
    }
}
