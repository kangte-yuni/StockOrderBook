using Server.Models;
using System.Collections.Concurrent;

namespace Server.Services
{
    public class OrderBookSimulator : IOrderBookSimulator, IDisposable
    {
        // panelId → ticker 매핑 (Depth 전용)
        private readonly ConcurrentDictionary<string, string> _panelTickerMap = new();

        // ticker → (panelId → DepthCallback)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Action<string, DepthEntry[]>>> _depthSubscriptions = new();

        // Random seeds per ticker
        private readonly ConcurrentDictionary<string, Random> _rands = new();

        // Trade 체결 내역 ->Print 용 객체로 콜백 목록 (ticker 기준)
        private readonly List<Action<string, PrintEntry[]>> _printSubscribers = new();
        private readonly object _printLock = new();

        // Trade 체결 내역 저장 용 객체 (panelId 기준)
        private readonly List<Action<string, Trade[]>> _tradeSubscribers = new();
        private readonly object _tradeLock = new();

        private readonly Timer _depthTimer;
        private readonly Timer _printTimer;
        private readonly Dictionary<string, (decimal Min, decimal Max)> _basePriceRanges;
        private readonly List<(decimal Min, decimal Max, decimal TickSize)> _tickSizeTable;

        // Trade 데이터 저장을 위한 필드
        private readonly ConcurrentBag<Trade> _allTradeHistory = new();        
        public OrderBookSimulator()
        {
            // S&P500 상위 10개 범위 설정
            _basePriceRanges = new Dictionary<string, (decimal, decimal)>
            {
                ["AAPL"] = (150.00m, 200.00m),
                ["MSFT"] = (280.00m, 330.00m),
                ["AMZN"] = (120.00m, 170.00m),
                ["GOOGL"] = (120.00m, 160.00m),
                ["NVDA"] = (450.00m, 550.00m),
                ["BRK.B"] = (320.00m, 360.00m),
                ["META"] = (250.00m, 300.00m),
                ["TSLA"] = (160.00m, 220.00m),
                ["UNH"] = (480.00m, 550.00m),
                ["JNJ"] = (150.00m, 180.00m),
            };

            // Tick size 룰
            _tickSizeTable = new List<(decimal, decimal, decimal)> {
                (     0m,   100m,    0.01m),
                (   100m,   500m,    0.05m),
                (   500m,  1000m,    0.10m),
                (  1000m,  5000m,    0.50m),
                (  5000m, decimal.MaxValue, 1.00m)
            };

            // Order 데이터 500ms 간격 시뮬레이션
            _depthTimer = new Timer(DepthTimerCallback, null, 0, 500);
            // Print 데이터 2s 간격 시뮬레이션
            _printTimer = new Timer(PrintTimerCallback, null, 0, 2000);
        }

        /// <summary>
        /// Trade 내역이 있으면 Load
        /// </summary>
        public void InitializeTrades(IEnumerable<Trade> tradeHistory)
        {
            foreach (var trade in tradeHistory)
            {
                _allTradeHistory.Add(trade);
            }
        }

        /// <summary>
        /// 전체 체결 내역 조회.
        /// </summary>
        public IReadOnlyList<Trade> GetAllTrades()
            => _allTradeHistory.ToList();

        /// <summary>
        /// 전체 체결 중 마지막 'count'개 반환, 기본값 100
        /// </summary>
        public IReadOnlyList<Trade> GetRecentTrades(int count = 100)
        {
            var all = GetAllTrades();
            if (all.Count <= count)
                return all;
            return all.Skip(all.Count - count).ToList();
        }


        /// <summary>
        /// Depth 전용 구독 (panelId 별)
        /// </summary>
        public void SubscribeDepth(string panelId, string ticker, Action<string, DepthEntry[]> depthCallback)
        {
            _panelTickerMap[panelId] = ticker;
            _rands.GetOrAdd(ticker, t => new Random(t.GetHashCode()));

            var subs = _depthSubscriptions.GetOrAdd(ticker, _ => new ConcurrentDictionary<string, Action<string, DepthEntry[]>>());
            subs[panelId] = depthCallback;
        }

        /// <summary>
        /// Depth 구독 해제
        /// </summary>
        public void UnsubscribeDepth(string panelId)
        {
            if (!_panelTickerMap.TryRemove(panelId, out var ticker)) return;
            if (_depthSubscriptions.TryGetValue(ticker, out var subs))
            {
                subs.TryRemove(panelId, out _);
                if (subs.IsEmpty)
                {
                    _depthSubscriptions.TryRemove(ticker, out _);
                    _rands.TryRemove(ticker, out _);
                }
            }
        }

        public void SubscribePrint(Action<string, PrintEntry[]> printCallback)
        {
            lock (_printLock)
                _printSubscribers.Add(printCallback);
        }
        public void UnsubscribePrint(Action<string, PrintEntry[]> printCallback)
        {
            lock (_printLock) 
                _printSubscribers.Remove(printCallback);
        }
        public void SubscribeTrade(Action<string, Trade[]> tradeCallback)
        {
            lock (_tradeLock) 
                _tradeSubscribers.Add(tradeCallback);
        }
        public void UnsubscribeTrade(Action<string, Trade[]> tradeCallback)
        {
            lock (_tradeLock)
                _tradeSubscribers.Remove(tradeCallback);
        }        

        private void DepthTimerCallback(object _)
        {
            foreach (var kv in _depthSubscriptions)
            {
                var ticker = kv.Key;
                var depthSubs = kv.Value;
                if (depthSubs.IsEmpty) continue;

                var rand = _rands[ticker];
                // basePrice 산출
                var (minP, maxP) = _basePriceRanges.TryGetValue(ticker, out var r) ? r : (100m, 200m);
                var basePrice = Math.Round((decimal)rand.NextDouble() * (maxP - minP) + minP, 2);
                var tickSize = _tickSizeTable.First(x => basePrice >= x.Min && basePrice < x.Max).TickSize;

                // Depth 생성
                var depths = new List<DepthEntry>(20);
                for (int lvl = 10; lvl >= 1; lvl--)
                {
                    var priceLvl = basePrice + tickSize * lvl;
                    depths.Add(new DepthEntry { Side = "Ask", Price = priceLvl, Size = Math.Round((decimal)rand.Next(1, 50), 2), PercentChange = Math.Round((priceLvl - basePrice) / basePrice * 100, 2) });
                }
                for (int lvl = 1; lvl <= 10; lvl++)
                {
                    var priceLvl = basePrice - tickSize * lvl;
                    depths.Add(new DepthEntry { Side = "Bid", Price = priceLvl, Size = Math.Round((decimal)rand.Next(1, 50), 2), PercentChange = Math.Round((priceLvl - basePrice) / basePrice * 100, 2) });
                }

                var depthArray = depths.ToArray();
                // panel 별 Depth 전송
                foreach (var kv2 in depthSubs)
                {
                    var panelId = kv2.Key;
                    var callback = kv2.Value; //(pid, entries) => _ctx.Clients.Client(connId).SendAsync("ReceiveDepth", pid, entries))
                    callback(panelId, depthArray);
                }
            }
        }

        private void PrintTimerCallback(object _)
        {
            foreach (var kv in _depthSubscriptions)
            {
                var ticker = kv.Key;
                if (kv.Value.IsEmpty) continue;
                var rand = _rands[ticker];
                var basePrice = _basePriceRanges[ticker].Min; // 기본 가격은 Min으로 설정
                var tickSize = _tickSizeTable.First(x => basePrice >= x.Min && basePrice < x.Max).TickSize;

                var panelIds = kv.Value.Keys;
                foreach (var panelId in panelIds)
                {
                    // Trade 엔티티 생성
                    var trade = new Trade
                    {                        
                        Time = DateTime.UtcNow,
                        Side = rand.Next(0, 2) == 0 ? "Buy": "Sell",
                        Ticker = ticker,
                        Price = basePrice + tickSize * rand.Next(-3, 4),
                        Quantity = Math.Round((decimal)rand.Next(1, 10), 2)
                    };

                    HandleTradeEvent(ticker, trade);
                }                           
            }

        }

        // Client 에서 매수/매도 주문 즉시 체결로 가정.
        public void PlaceOrder(string ticker, string side, decimal price, int quantity, DateTime timestamp)
        {
            var trade = new Trade
            {
                Ticker = ticker,
                Side = side,
                Price = price,
                Quantity = quantity,
                Time = timestamp
            };

            HandleTradeEvent(ticker, trade);
        }

        // File에 저장하거나 UI에 표시하는 이벤트 발생
        private void HandleTradeEvent(string ticker, Trade trade)
        {            
            lock (_tradeLock)
            {
                foreach (var cb in _tradeSubscribers)
                    cb(ticker, new[] { trade });
            }
            lock (_printLock)
            {
                // Print 용 데이터로 변경
                foreach (var cb in _printSubscribers)
                    cb(ticker, new[] { trade.ToPrintEntry() });
            }
            _allTradeHistory.Add(trade);
        }

        public void Dispose()
        {
            _depthTimer.Dispose();
            _printTimer.Dispose();
        }        
    }
}
