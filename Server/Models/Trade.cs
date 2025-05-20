namespace Server.Models
{
    public class Trade
    {
        public string PanelId { get; set; }
        // 체결 시간
        public DateTime Time { get; set; }
        // 매수/매도 구분
        public string Side { get; set; }
        // 해당 체결의 티커
        public string Ticker { get; set; }
        // 체결 가격
        public decimal Price { get; set; }
        // 체결 수량
        public decimal Quantity { get; set; }

        public PrintEntry ToPrintEntry()
        {
            return new PrintEntry
            {
                Time = Time,
                Side = Side,
                Ticker = Ticker,
                Price = Price,
                Quantity = Quantity
            };
        }
    }
}
