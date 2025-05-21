using Microsoft.AspNetCore.SignalR;
using Server.Models;
using Server.Services;
using System.Collections.Concurrent;

namespace Server.Hubs
{
    public class OrderBookHub : Hub
    {
        // connectionId → panelId 목록
        private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _connPanels = new();
        // connectionId → Print 콜백 매핑
        private static readonly ConcurrentDictionary<string, Action<string, PrintEntry[]>> _connPrintCallbacks = new();

        private readonly IOrderBookSimulator _sim;
        private readonly IHubContext<OrderBookHub> _ctx;

        public OrderBookHub(
            IOrderBookSimulator sim,
            IHubContext<OrderBookHub> ctx)
        {
            _sim = sim;
            _ctx = ctx;
        }

        /// <summary>
        /// 클라이언트가 호출: 해당 panelId, ticker 로 Depth 구독 시작
        /// </summary>
        public async Task Subscribe(string panelId, string ticker)
        {
            var connId = Context.ConnectionId;

            Console.WriteLine($"Subscribe Depth: connId={connId}, panelId={panelId}");

            // Depth 구독
            _sim.SubscribeDepth(panelId, ticker, (pid, entries)
                => _ctx.Clients.Client(connId)
                       .SendAsync("ReceiveDepth", pid, entries));

            // panel 목록에 추가
            var bag = _connPanels.GetOrAdd(connId, _ => new ConcurrentBag<string>());
            bag.Add(panelId);

            // 과거 체결 내역: 전체 중 마지막 100개만 전송
            var recent = _sim.GetRecentTrades(100).Select(trade => trade.ToPrintEntry()).ToArray();
            if (recent != null && recent.Length > 0)
            {
                await _ctx.Clients.Client(connId)
                       .SendAsync("ReceivePrint", panelId, recent);
            }

            // 최초 구독 시 글로벌 Print 구독 등록
            if (!_connPrintCallbacks.ContainsKey(connId))
            {
                Action<string, PrintEntry[]> printCb = (tk, prints)
                    => _ctx.Clients.Client(connId)
                           .SendAsync("ReceivePrint", tk, prints);

                if (_connPrintCallbacks.TryAdd(connId, printCb))
                {
                    _sim.SubscribePrint(printCb);
                }
            }                      
        }

        /// <summary>
        /// 클라이언트가 호출: panelId Depth 구독 해제
        /// </summary>
        public Task Unsubscribe(string panelId)
        {
            var connId = Context.ConnectionId;
            Console.WriteLine($"Unsubscribe Depth: connId={connId}, panelId={panelId}");

            // Depth 언구독
            _sim.UnsubscribeDepth(panelId);

            // panel 목록 업데이트
            if (_connPanels.TryGetValue(connId, out var bag))
            {
                var remaining = bag.Where(id => id != panelId);
                _connPanels[connId] = new ConcurrentBag<string>(remaining);
            }

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var connId = Context.ConnectionId;
            Console.WriteLine($"Disconnected: connId={connId}");

            // Depth 언구독: 모든 panelId
            if (_connPanels.TryRemove(connId, out var bag))
            {
                foreach (var panelId in bag)
                {
                    Console.WriteLine($"Unsubscribe Depth on disconnect: connId={connId}, panelId={panelId}");
                    _sim.UnsubscribeDepth(panelId);
                }
            }

            // Print 언구독
            if (_connPrintCallbacks.TryRemove(connId, out var printCb))
            {
                _sim.UnsubscribePrint(printCb);
            }

            return base.OnDisconnectedAsync(exception);
        }

        // 매수 주문 처리
        public async Task PlaceBuyOrder(string ticker, decimal price, int quantity)
        {
            Console.WriteLine($"[BUY ORDER] Ticker: {ticker}, Price: {price}, Qty: {quantity}");

            // 시뮬레이터에 주문 요청
            _sim.PlaceOrder(ticker: ticker, side: "Buy", price:price, quantity: quantity,timestamp: DateTime.UtcNow);

            //await Clients.Caller.SendAsync("BuyOrderConfirmed", ticker, price, quantity);
        }


        // 매도 주문 처리
        public async Task PlaceSellOrder(string ticker, decimal price, int quantity)
        {
            Console.WriteLine($"[SELL ORDER] Ticker: {ticker}, Price: {price}, Qty: {quantity}");

            // 시뮬레이터에 주문 요청
            _sim.PlaceOrder(ticker: ticker, side: "Sell", price:  price, quantity: quantity, timestamp:DateTime.UtcNow);

            //await Clients.Caller.SendAsync("SellOrderConfirmed", ticker, price, quantity);
        }
    }
}
