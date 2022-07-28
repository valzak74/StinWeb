using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliExpressClasses
{
    public class StockRequest
    {
        public List<Product>? Products { get; set; }
    }
    public class AliResponse
    {
        public string? Group_id { get; set; }
        public List<Result>? Results { get; set; }
    }
    public class Result
    {
        public bool Ok { get; set; }
        public string? Task_id { get; set; }
        public Error? Errors { get; set; }
    }
    public class Product
    {
        public string? Product_id { get; set; }
        public List<StockSku>? Skus { get; set; }
    }
    public class StockSku
    {
        public string? Sku_code { get; set; }
        public string? Inventory { get; set; }
    }
    public class StockProductGlobal
    {
        public long Product_id { get; set; }
        public List<StockSku>? Multiple_sku_update_list { get; set; }
    }
    public class StockGlobalResponse
    {
        public AliexpressSolutionBatchProductPriceStockUpdateResponse? Aliexpress_solution_batch_product_inventory_update_response { get; set; }
    }
}
