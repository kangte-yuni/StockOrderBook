using Server.Models;

namespace Server.Services
{
    public interface ITradeStorage
    {
        Task SaveAsync(IEnumerable<Trade> trades);
        Task AppendAsync(IEnumerable<Trade> newTrades);
        Task<IReadOnlyList<Trade>> LoadAsync();
    }
}
