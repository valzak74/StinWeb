using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WbClasses
{
    public class StocksRequest
    {
        public string? Barcode { get; set; }
        public int Stock { get; set; }
        public int WarehouseId { get; set; }
        public StocksRequest(string barcode, int stock, int warehouseId)
        {
            Barcode = barcode;
            Stock = stock;
            WarehouseId = warehouseId;
        }
    }
    public class StocksResponse: Response
    {
        public StockData? Data { get; set; }
        public class StockData
        {
            public List<StockError>? Error { get; set; }
            public class StockError
            {
                public string? Barcode { get; set;}
                public string? Err { get; set; }
            }
        }
    }
}
