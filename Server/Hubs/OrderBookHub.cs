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
        public Task Subscribe(string panelId, string ticker)
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

            return Task.CompletedTask;
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
    }
}
