using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WbClasses
{
    public class StocksRequestV3
    {
        public List<StocksEntryV3> Stocks { get; set; }
        public StocksRequestV3() => Stocks = new List<StocksEntryV3>();
        public StocksRequestV3(Dictionary<string, int> items) : this()
        {
            foreach (var item in items)
                Stocks.Add(new StocksEntryV3(item.Key, item.Value));
        }
        public class StocksEntryV3
        {
            public string? Sku { get; set; }
            public int Amount { get; set; }
            public StocksEntryV3(string barcode, int amount)
            {
                Sku = barcode;
                Amount = amount;
            }
        }
    }
    //[JsonConverter(typeof(SingleObjectOrArrayJsonConverter<StockError>))]
    public class StockError : ResponseV3
    {
        public List<ErrorDetail>? Data { get; set; }
        public class ErrorDetail
        {
            public string? Sku { get; set; }
            public int Stock { get; set; }
        }
    }
}
