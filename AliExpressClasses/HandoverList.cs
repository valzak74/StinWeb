using JsonExtensions;
using Newtonsoft.Json;

namespace AliExpressClasses
{
    public class GetHandoverListRequest
    {
        public int Page_size { get; set; }
        public int Page_number { get; set;}
        public List<long>? Logistic_order_ids { get; set; }
        public GetHandoverListRequest() => Page_size = 100;
        public GetHandoverListRequest(int pageNumber): this() => Page_number = pageNumber;
        public GetHandoverListRequest(int pageNumber, int pageSize): this(pageNumber) => Page_size = pageSize;
    }
    public class GetHandoverListResponse
    {
        public HandoverListData? Data { get; set; }
        public Error? Error { get; set; }
        public PageInfo? PageInfo { get; set; }
    }
    public class HandoverListData
    {
        public List<HandoverList>? Data_source { get; set; }
    }
    public class HandoverList
    {
        public long Handover_list_id { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Arrival_date { get; set; }
        public HandoverStatus Status { get; set; }
        public FirstMileType Shipment_type { get; set; }
        public List<long>? Logistic_order_ids { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Gmt_create { get; set; }
    }
    public class PageInfo
    {
        public int Current { get; set; }
        public int Page_size { get; set; }
        public int Total { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum HandoverStatus
    {
        NotFound = 0,
        Created = 1,
        Transferred = 2,
        Accepted = 3,
        PartiallyAccepted = 4
    }
    public class CreateHandover
    {
        public List<long>? Logistic_order_ids { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:ss.fffZ")]
        public DateTime Arrival_date { get; set; }
    }
    public class CreateHandoverResponse
    {
        public HandoverData? Data { get; set; }
        public Error? Error { get; set; }
    }
    public class HandoverData
    {
        public long Handover_list_id { get; set; }
    }
    public class AddToHandover
    {
        public long Handover_list_id { get; set; }
        public List<long>? Order_ids { get; set; }
    }
    public class DeleteFromHandover
    {
        public long Handover_list_id { get; set; }
        public List<string>? Order_ids { get; set; }
    }
    public class AddDeleteFromHandoverResponse
    {
        public object? Data { get; set; }
        public Error? Error { get; set; }
    }
    public class PrintHandover
    {
        public long Handover_list_id { get; set; }
    }
}
