using Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Services
{
    public interface IRealtimeConnectionService
    {
        Task StartAsync();
        Task SubscribeAsync(string panelId, string ticker);
        Task UnsubscribeAsync(string panelId);
        void OnDepth(Action<string, DepthEntry[]> handler);
        void OnPrint(Action<string, PrintEntry[]> handler);
    }
}
