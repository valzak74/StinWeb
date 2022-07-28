namespace OzonClasses
{
    public class CancelOrderRequest
    {
        public long Cancel_reason_id { get; set; }
        public string? Cancel_reason_message { get; set; }
        public List<CancelItem>? Items { get; set; }
        public string? Posting_number { get; set; }
        public bool ShouldSerializeItems()
        {
            return Items != null && Items.Count > 0;
        }
    }

    public class CancelItem
    {
        public int Quantity { get; set; }
        public long Sku { get; set; }
    }

    public class CancelOrderResponse
    {
        public bool Result { get; set; }
    }
}
