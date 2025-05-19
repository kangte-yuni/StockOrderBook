using Server.Models;

namespace Server.Services
{
    public interface IOrderBookSimulator
    {
        void SubscribeDepth(string panelId, string ticker, Action<string, DepthEntry[]> depthCallback);
        void UnsubscribeDepth(string panelId);
        void SubscribePrint(Action<string, PrintEntry[]> printCallback);
        void UnsubscribePrint(Action<string, PrintEntry[]> printCallback);
    }
}
