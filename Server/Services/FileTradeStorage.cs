using Server.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Server.Services
{
    public class FileTradeStorage : ITradeStorage
    {
        private readonly string _path = "tradeHistory.json";
        private readonly JsonSerializerOptions _opts = new() { WriteIndented = false };
        public async Task<IReadOnlyList<Trade>> LoadAsync()
        {
            if (!File.Exists(_path)) return Array.Empty<Trade>();
            var lines = await File.ReadAllLinesAsync(_path);
            return lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => JsonSerializer.Deserialize<Trade>(l, _opts)!)
                .ToList();
        }

        public async Task SaveAsync(IEnumerable<Trade> trades)
        {
            var lines = trades.Select(t => JsonSerializer.Serialize(t, _opts));
            await File.WriteAllLinesAsync(_path, lines);
        }
        public async Task AppendAsync(IEnumerable<Trade> newTrades)
        {
            var lines = newTrades.Select(t => JsonSerializer.Serialize(t, _opts));
            await File.AppendAllLinesAsync(_path, lines);
        }
    }
}
