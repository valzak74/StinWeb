using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliExpressClasses
{
    public class PriceRequest
    {
        public List<PriceProduct>? Products { get; set; }
    }
    public class PriceProduct
    {
        public string? Product_id { get; set; }
        public List<PriceSku>? Skus { get; set; }
    }
    public class PriceSku
    {
        public string? Sku_code { get; set; }
        public string? Price { get; set; }
        public string? Discount_price { get; set; }
    }
    public class PriceProductGlobal
    {
        public long Product_id { get; set; }
        public List<PriceSku>? Multiple_sku_update_list { get; set; }
    }
    public class PriceGlobalResponse
    {
        public AliexpressSolutionBatchProductPriceStockUpdateResponse? Aliexpress_solution_batch_product_price_update_response { get; set; }
    }
    public class AliexpressSolutionBatchProductPriceStockUpdateResponse
    {
        public string? Update_error_code { get; set; }
        public string? Update_error_message { get; set; }
        public bool Update_success { get; set; }
        public ProductResponseDto? Update_failed_list { get; set; }
        public ProductResponseDto? Update_successful_list { get; set; }
        public string? Request_id { get; set; }
    }
    public class ProductResponseDto
    {
        public List<SynchronizeProductResponseDto>? Synchronize_product_response_dto { get; set; }
    }
    public class SynchronizeProductResponseDto
    {
        public string? Error_code { get; set; }
        public string? Error_message { get; set; }
        public long Product_id { get; set; }
    }
}
