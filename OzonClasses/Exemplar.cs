namespace OzonClasses
{
    public class ExemplarSetRequest
    {
        public int Multi_box_qty { set; get; }
        public string? Posting_number { get; set; }
        public List<ProductExemplarRequest>? Products { get; set; }
    }

    public class ExemplarSetResponse
    {
        public bool Result { get; set; }
    }

    public class ProductExemplarRequest
    {
        public List<ProductExemplarRequestItem>? Exemplars { get; set; }

        public bool Is_gtd_needed { get; set; }

        public bool Is_mandatory_mark_needed { get; set; }

        public bool Is_rnpt_needed { get; set; }

        public long Product_id { get; set; }

        public int Quantity { get; set; }
    }

    public class ProductExemplarRequestItem
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
