namespace OzonClasses
{
    public class OzonPriceRequest
    {
        public List<PriceRequest> Prices { get; set; }
        public OzonPriceRequest()
        {
            Prices = new List<PriceRequest>();
        }
    }
    public class PriceRequest
    {
        public string? Min_price { get; set; }
        public string? Offer_id { get; set; }
        public string? Old_price { get; set; }
        public string? Price { get; set; }
        public long Product_id { get; set; }
    }
    public class OzonPriceResponse
    {
        public List<PriceResult>? Result { get; set; }
    }
    public class PriceResult
    {
        public List<ErrorResponse>? Errors { get; set; }
        public string? Offer_id { get; set; }
        public long Product_id { get; set; }
        public bool Updated { get; set; }
    }
}
