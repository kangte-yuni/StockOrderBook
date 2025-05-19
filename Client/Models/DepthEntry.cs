using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models
{
    public class DepthEntry
    {
        public string Side { get; set; }
        public decimal Price { get; set; }
        public decimal Size { get; set; }
        public decimal PercentChange { get; set; } // 가격 변동률

        public DepthEntry(string side, decimal price, decimal size, decimal percentChange)
        {
            Side = side;
            Price = price;
            Size = size;
            PercentChange = percentChange;
        }
    }
}
