using Client.Models;
using Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly Func<string, OrderBookPanelViewModel> _panelFactory;
        private readonly IRealtimeConnectionService _realtimeService;
        public ObservableCollection<string> AvailableTickers { get; } = new()
        {
            "AAPL","MSFT","AMZN","GOOGL","NVDA",
            "BRK.B","META","TSLA","UNH","JNJ"
        };

        [ObservableProperty]
        private string selectedTicker;

        public ObservableCollection<OrderBookPanelViewModel> Panels { get; } = new();
        // 체결 내역
        public ObservableCollection<PrintEntry> TradeHistory { get; } = new();

        public IRelayCommand AddPanelCommand { get; }

        public MainViewModel(IRealtimeConnectionService realtimeService, Func<string, OrderBookPanelViewModel> panelFactory)
        {
            _realtimeService = realtimeService;
            _panelFactory = panelFactory;
            // 백그라운드에서 허브 연결 시도
            _ = ConnectAsync();
            // MainViewModel 에서는 OnPrint 이벤트만 사용 (Segregation of Concerns)
            _realtimeService.OnPrint(HandleTradeHistoryPrint);


            // AddPanel 커맨드
            AddPanelCommand = new RelayCommand(AddPanel);
        }

        private async Task ConnectAsync()
        {
            try
            {
                await _realtimeService.StartAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"SignalR 연결 실패: {ex.Message}");
            }
        }

        private void AddPanel()
        {
            if (string.IsNullOrEmpty(SelectedTicker))
                return;

            var panel = _panelFactory(SelectedTicker);
            panel.RemovePanelRequested += OnPanelRemoveRequested;
            Panels.Add(panel);
        }

        private void HandleTradeHistoryPrint(string panelId, PrintEntry[] entries)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var entry in entries.Reverse())
                {
                    TradeHistory.Insert(0, entry);
                }
                // 히스토리 최대 100개 유지
                const int maxHistory = 100;
                if (TradeHistory.Count > maxHistory)
                {
                    for (int i = TradeHistory.Count - 1; i >= maxHistory; i--)
                    {
                        TradeHistory.RemoveAt(i);
                    }
                }
            });
        }

        private async void OnPanelRemoveRequested(OrderBookPanelViewModel panel)
        {
            Console.WriteLine("Remove");
            panel.RemovePanelRequested -= OnPanelRemoveRequested;
            try
            {
                await panel.UnsubscribeAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unsubscribe 실패: {ex.Message}");
            }
            Panels.Remove(panel);
        }
    }
}
