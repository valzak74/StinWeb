namespace OzonClasses
{
    public class ExemplarCreateOrGetRequest
    {
        public string? Posting_number { get; set; }
    }

    public class ExemplarCreateOrGetResponse
    {
        public int Multi_box_qty { get; set; }
        public string? Posting_number { get; set; }
        public List<ProductExemplarCreateOrGetItem>? Products { get; set; }
    }

    public class ProductExemplarCreateOrGetItem
    {
        public List<ExemplarCreateOrGetItem>? Exemplars { get; set; }
        public bool Is_gtd_needed { get; set; }
        public bool Is_mandatory_mark_needed { get; set; }
        public bool Is_rnpt_needed { get; set; }
        public long Product_id { get; set; }
        public int Quantity { get; set; }
    }

    public class ExemplarCreateOrGetItem
    {
        public long Exemplar_id { get; set; }
        public string? Gtd { get; set; }
        public bool Is_gtd_absent { get; set; }
        public bool Is_rnpt_absent { get; set; }
        public string? Mandatory_mark { get; set; }
        public string? Rnpt { get; set; }
        public string? Jw_uin { get; set; }
    }
}
