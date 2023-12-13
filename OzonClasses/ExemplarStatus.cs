using JsonExtensions;
using Newtonsoft.Json;

namespace OzonClasses
{
    public class ExemplarStatusRequest
    {
        public string? Posting_number { get; set; }
    }
    public class ExemplarStatusResponse
    {
        public string? Posting_number { get; set; }
        public List<ProductExemplarStatus>? Products { get; set; }
        public ExemplarStatus Status { get; set; }
    }
    public class ProductExemplarStatus
    {
        public List<ExemplarStatusItem>? Exemplars { get; set; }
        public long Product_id { get; set; }
    }
    public class ExemplarStatusItem
    {
        public long Exemplar_id { get; set; }
        public string? Gtd { get; set; }
        public string? Gtd_check_status { get; set; }
        public List<string>? Gtd_error_codes { get; set; }
        public bool Is_gtd_absent { get; set; }
        public List<string>? Jw_uin { get; set; }
        public string? Jw_uin_check_status { get; set; }
        public List<string>? Jw_uin_error_codes { get; set; }
        public string? Mandatory_mark { get; set; }
        public string? Mandatory_mark_check_status { get; set; }
        public List<string>?Mandatory_mark_error_codes { get; set; }
        public string? Rnpt { get; set; }
        public string? Rnpt_check_status { get; set; }
        public List<string>? Rnpt_error_codes { get; set; }
        public bool Is_rnpt_absent { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ExemplarStatus
    {
        NotFound = 0,
        ship_available = 1,
        ship_not_available = 2,
        validation_in_process = 3,
    }
}
