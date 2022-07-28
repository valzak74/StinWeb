namespace OzonClasses
{
    public class PostingOrderRequest
    {
        public List<PostingPackage>? Packages { get; set; }
        public string? Posting_number { get; set; }
        public PostingWith? With { get; set; }
        public PostingOrderRequest()
        {
            Packages = new List<PostingPackage>();
        }
    }
    public class PostingOrderResponse
    {
        public List<PostingAdditionalData>? Additional_data { get; set; }
        public List<string>? Result { get; set; }
    }
    public class PostingAdditionalData
    {
        public string? Posting_number { get; set; }
        public List<PostingProduct>? Products { get; set; }
    }
    public class PostingPackage
    {
        public List<PostingPackageProduct>? Products { get; set; }
    }
    public class PostingPackageProduct
    {
        public List<PostingExemplarInfo>? Exemplar_info { get; set; }
        public long Product_id { get; set; }
        public int Quantity { get; set; }
    }
    public class PostingExemplarInfo
    {
        public string? Mandatory_mark { get; set; }
        public string? Gtd { get; set; }
        public bool Is_gtd_absent { get; set; }
    }
    public class PostingWith
    {
        public bool additional_data { get; set; }
    }
}
