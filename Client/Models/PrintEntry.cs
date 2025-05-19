using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models
{
    public class PrintEntry
    {
        // 체결 시간
        public DateTime Time { get; set; }
        // 매수/매도 구분
        public string Side { get; set; }
        // 해당 체결의 티커
        public string Ticker { get; set; }
        // 체결 가격
        public decimal Price { get; set; }
        // 체결 수량
        public decimal Quantity { get; set; } // 소수점 개수 염두
        public PrintEntry(DateTime time, string side, string ticker, decimal price, decimal quantity)
        {
            Time = time;
            Side = side;
            Ticker = ticker;
            Price = price;
            Quantity = quantity;
        }
    }
}
