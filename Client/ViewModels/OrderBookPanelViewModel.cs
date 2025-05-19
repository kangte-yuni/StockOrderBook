using Client.Models;
using Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Client.ViewModels
{
    public partial class OrderBookPanelViewModel : ObservableObject, IDisposable
    {
        // SignalR 구독 식별용 고유 ID
        public string PanelId { get; } = Guid.NewGuid().ToString();

        // 구독할 티커
        public string Ticker { get; }

        // 10단계 매도·매수 호가
        public ObservableCollection<DepthEntry> Asks { get; } = new();
        public ObservableCollection<DepthEntry> Bids { get; } = new();

        // 패널 제거 요청
        public event Action<OrderBookPanelViewModel> RemovePanelRequested;
        public IRelayCommand RemovePanelCommand { get; }

        private readonly IRealtimeConnectionService _realtimeService;

        public OrderBookPanelViewModel(IRealtimeConnectionService realtimeService, string ticker)
        {
            _realtimeService = realtimeService;
            Ticker = ticker;

            // OrderbookPanelViewModel 에서는 OnDepth 이벤트만 사용 (Segregation of Concerns)
            _realtimeService.OnDepth(HandleDepth);

            // Remove 버튼
            RemovePanelCommand = new RelayCommand(() =>
            {                
                RemovePanelRequested?.Invoke(this);
            });

            // 생성 직후 구독 시작
            _ = SubscribeAsync();
        }

        /// <summary>
        /// SignalR로 Depth/Prints 구독 시작
        /// </summary>
        public async Task SubscribeAsync()
        {
            try
            {
                await _realtimeService.SubscribeAsync(PanelId, Ticker);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Subscribe failed: {ex.Message}");
            }
        }

        /// <summary>
        /// SignalR 구독 해제
        /// </summary>
        public async Task UnsubscribeAsync()
        {
            try
            {
                await _realtimeService.UnsubscribeAsync(PanelId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unsubscribe failed: {ex.Message}");
            }
        }

        private void HandleDepth(string panelId, DepthEntry[] entries)
        {
            if (panelId != PanelId) return;

            // UI 스레드에서 실행
            Application.Current.Dispatcher.Invoke(() =>
            {
                var asks = entries
                    .Where(e => e.Side.Equals("Ask", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(e => e.Price)
                    .Take(10)
                    .ToList();
                UpdateCollection(Asks, asks);

                var bids = entries
                    .Where(e => e.Side.Equals("Bid", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.Price)
                    .Take(10)
                    .ToList();
                UpdateCollection(Bids, bids);
            });
        }


        private void UpdateCollection(ObservableCollection<DepthEntry> target, System.Collections.Generic.List<DepthEntry> items)
        {
            target.Clear();
            foreach (var item in items)
                target.Add(item);
        }

        public void Dispose()
        {
            _ = UnsubscribeAsync();
        }
    }
}
