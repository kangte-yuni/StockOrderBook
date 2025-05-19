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

        // 글로벌 Print(체결) 콜백 목록 (ticker 기준)
        private readonly List<Action<string, PrintEntry[]>> _printSubscribers = new();
        private readonly object _printLock = new();

        private readonly Timer _depthTimer;
        private readonly Timer _printTimer;
        private readonly Dictionary<string, (decimal Min, decimal Max)> _basePriceRanges;
        private readonly List<(decimal Min, decimal Max, decimal TickSize)> _tickSizeTable;

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

        /// <summary>
        /// 글로벌 Print(체결) 구독
        /// </summary>
        public void SubscribePrint(Action<string, PrintEntry[]> printCallback)
        {
            lock (_printLock)
            {
                _printSubscribers.Add(printCallback);
            }
        }

        /// <summary>
        /// 글로벌 Print(체결) 구독 해제
        /// </summary>
        public void UnsubscribePrint(Action<string, PrintEntry[]> printCallback)
        {
            lock (_printLock)
            {
                _printSubscribers.Remove(printCallback);
            }
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
                var depthSubs = kv.Value;
                if (depthSubs.IsEmpty) continue;
                var rand = _rands[ticker];
                var basePrice = _basePriceRanges[ticker].Min; // 기본 가격은 Min으로 설정
                var tickSize = _tickSizeTable.First(x => basePrice >= x.Min && basePrice < x.Max).TickSize;
                // Print(체결) 생성 (한 건)
                var print = new PrintEntry
                {
                    Time = DateTime.UtcNow,
                    Side = rand.Next(0, 2) == 0 ? "Buy" : "Sell",
                    Ticker = ticker,
                    Price = basePrice + tickSize * rand.Next(-3, 4),
                    Quantity = Math.Round((decimal)rand.Next(1, 10), 2)
                };
                var printArray = new[] { print };
                // 글로벌 Print 전송
                lock (_printLock)
                {
                    foreach (var cb in _printSubscribers)
                        cb(ticker, printArray);
                }
            }

        }

        public void Dispose()
        {
            _depthTimer.Dispose();
            _printTimer.Dispose();
        }
    }
}
