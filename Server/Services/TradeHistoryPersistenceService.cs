
using Server.Models;

namespace Server.Services
{
    public class TradeHistoryPersistenceService : IHostedService, IDisposable
    {
        private readonly IOrderBookSimulator _sim;
        private readonly ITradeStorage _storage;        

        private Action<string, Trade[]> _tradeCallback;
        private bool _subscribed = false;

        public TradeHistoryPersistenceService(IOrderBookSimulator sim, ITradeStorage storage)
        {
            _sim = sim;
            _storage = storage;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // 로컬 File에 Trade History 가 저장되어있는 것을 Load하여 시뮬레이터에 전달
            var persistedTrades = await _storage.LoadAsync();
            _sim.InitializeTrades(persistedTrades);

            _tradeCallback = async (ticker, trades) =>
            {
                await _storage.AppendAsync(trades);
            };

            _sim.SubscribeTrade(_tradeCallback);
            _subscribed = true;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // 종료 시점에 메모리에 남은 체결내역 중 최대 100개만 저장
            var allTrades = _sim.GetRecentTrades(100);
            await _storage.SaveAsync(allTrades);
        }
        public void Dispose()
        {
            if (_subscribed)
            {
                _sim.UnsubscribeTrade(_tradeCallback);
                _subscribed = false;
            }            
        }
    }
}
