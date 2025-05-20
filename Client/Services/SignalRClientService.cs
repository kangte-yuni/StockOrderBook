using Client.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Client.Services
{
    public class SignalRClientService : IRealtimeConnectionService
    {
        private readonly HubConnection _connection;
        // panelId → ticker 매핑: 재연결 시 재구독에 사용
        private readonly ConcurrentDictionary<string, string> _subscriptions
            = new();

        public SignalRClientService(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.Reconnecting += error =>
            {
                Console.WriteLine("SignalR: Reconnecting…");
                return Task.CompletedTask;
            };
            _connection.Reconnected += connectionId =>
            {
                Console.WriteLine("SignalR: Reconnected, re-subscribing…");
                return ResubscribeAllAsync();
            };
            _connection.Closed += error =>
            {
                // 완전 종료 후 필요하다면 다시 StartAsync 호출
                Console.WriteLine("SignalR: Closed. Waiting for manual restart or auto-retry.");
                return Task.CompletedTask;
            };
        }
        // event handler
        public void OnDepth(Action<string, DepthEntry[]> handler)
            => _connection.On<string, DepthEntry[]>("ReceiveDepth", handler);

        public void OnPrint(Action<string, PrintEntry[]> handler)
            => _connection.On<string, PrintEntry[]>("ReceivePrint", handler);

        public Task StartAsync() => _connection.StartAsync();

        public async Task SubscribeAsync(string panelId, string ticker)
        {        
            await _connection.InvokeAsync("Subscribe", panelId, ticker);
            _subscriptions[panelId] = ticker;
        }

        public async Task UnsubscribeAsync(string panelId)
        {        
            await _connection.InvokeAsync("Unsubscribe", panelId);
            _subscriptions.TryRemove(panelId, out _);
        }
        private Task ResubscribeAllAsync()
        {
            foreach (var kvp in _subscriptions)
            {
                var panelId = kvp.Key;
                var ticker = kvp.Value;
                _ = SubscribeAsync(panelId, ticker);
            }
            return Task.CompletedTask;
        }
    }
}
