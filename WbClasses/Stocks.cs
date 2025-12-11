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
        public StocksRequestV3(Dictionary<(string Barcode, long ChrtId), int> items) : this()
        {
            foreach (var item in items)
                Stocks.Add(new StocksEntryV3(item.Key.Barcode, item.Key.ChrtId, item.Value));
        }
        public class StocksEntryV3
        {
            public long ChrtId { get; set; }
            public string? Sku { get; set; }
            public int Amount { get; set; }
            public StocksEntryV3(string barcode, long chrtId, int amount)
            {
                Sku = barcode;
                ChrtId = chrtId;
                Amount = amount;
            }
        }
    }
    //[JsonConverter(typeof(SingleObjectOrArrayJsonConverter<StockError>))]
    public class StockError : Response
    {
        public List<ErrorDetail>? Data { get; set; }
        public class ErrorDetail
        {
            public string? Sku { get; set; }
            public int Stock { get; set; }
        }
    }
}
