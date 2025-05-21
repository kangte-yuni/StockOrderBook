using Server.Models;

namespace Server.Services
{
    public interface IOrderBookSimulator
    {
        // Depth 구독/구독 해제
        void SubscribeDepth(string panelId, string ticker, Action<string, DepthEntry[]> depthCallback);
        void UnsubscribeDepth(string panelId);
        // 화면용 Print 구독/구독 해제
        void SubscribePrint(Action<string, PrintEntry[]> printCallback);
        void UnsubscribePrint(Action<string, PrintEntry[]> printCallback);
        // DB 저장용 Trade 구독/구독 해제
        void SubscribeTrade(Action<string, Trade[]> tradeCallback);
        void UnsubscribeTrade(Action<string, Trade[]> tradeCallback);
        void InitializeTrades(IEnumerable<Trade> tradeHistory);
        IReadOnlyList<Trade> GetAllTrades();
        IReadOnlyList<Trade> GetRecentTrades(int count);
        // 매수/매도 주문 처리
        void PlaceOrder(string ticker, string side, decimal price, int quantity, DateTime timestamp);
    }
}
