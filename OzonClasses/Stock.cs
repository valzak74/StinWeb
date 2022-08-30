namespace OzonClasses
{
    public class OzonStockRequest
    {
        public List<StockRequest> Stocks { get; set; }
        public OzonStockRequest()
        {
            Stocks = new List<StockRequest>();
        }
    }
    public class StockRequest
    {
        public string? Offer_id { get; set; }
        public long Product_id { get; set; }
        public long Stock { get; set; }
        public long Warehouse_id { get; set; }
        public bool ShouldSerializeWarehouse_id() => Warehouse_id > 0;
    }
    public class OzonStockResponse
    {
        public List<StockResult>? Result { get; set; }
    }
    public class StockResult
    {
        public List<ErrorResponse>? Errors { get; set; }
        public string? Offer_id { get; set; }
        public long Product_id { get; set; }
        public bool Updated { get; set; }
        public long Warehouse_id { get; set; }
    }
}
