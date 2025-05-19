namespace Server.Models
{
    public class DepthEntry
    {
        public string Side { get; set; }   // "Bid" or "Ask"
        public decimal Price { get; set; }
        public decimal Size { get; set; }
        public decimal PercentChange { get; set; } // 가격 변동률
    }
}
