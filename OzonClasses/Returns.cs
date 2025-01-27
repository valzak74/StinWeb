using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OzonClasses
{
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ReturnStatus
    {
        NotFound = 0,
        returned_to_seller = 1,
        waiting_for_seller = 2,
        accepted_from_customer = 3,
        cancelled_with_compensation = 4,
        ready_for_shipment = 5,
        moving = 6
    }
    public class ReturnsRequest
    {
        public ReturnsRequestFilter? Filter { get; set; }
        public long Limit { get; set; }
        public long Last_id { get; set; }
        public class ReturnsRequestFilter
        {
            public FilterTimeRange? Accepted_from_customer_moment { get; set; }
            public FilterTimeRange? Last_free_waiting_day { get; set; }
            public long Order_id { get; set; }
            public List<string>? Posting_number { get; set; }
            public string? Product_name { get; set; }
            public string? Product_offer_id { get; set;}
            public ReturnStatus Status { get; set; }
            public class FilterTimeRange
            {
                [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
                public DateTime? Time_from { get; set; }
                [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
                public DateTime? Time_to { get; set; }
            }
        }
        public ReturnsRequest() => Limit = 1000;
        public ReturnsRequest(long limit) => Limit = limit;
        public ReturnsRequest(long limit, long offset) : this(limit) => Last_id = offset;
        public bool ShouldSerializeFilter()
        {
            return Filter != null;
        }
    }
    public class ReturnsResponse
    {
        public bool Has_next { get; set; }
        public List<ReturnItem>? Returns { get; set; }
    }
    public class ReturnItem
    {
        [JsonProperty("Accepted_from_customer_moment", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? Accepted_from_customer_moment { get; set; }
        public long Clearing_id { get; set; }
        public double? Commission { get; set; }
        public double? Commission_percent { get; set; }
        public long Id { get; set; }
        public bool Is_moving { get; set; }
        public bool Is_opened { get; set; }
        [JsonProperty("Last_free_waiting_day", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? Last_free_waiting_day { get; set; }
        public long Place_id { get; set; }
        public string? Moving_to_place_name { get; set; }
        public double? Picking_amount { get; set; }
        public string? Posting_number { get; set; }
        public double? Price { get; set; }
        public double? Price_without_commission { get; set; }
        public long Product_id { get; set; }
        public string? Product_name { get; set; }
        public long Quantity { get; set; }
        [JsonProperty("Return_date", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? Return_date { get; set; }
        public string? Return_reason_name { get; set; }
        [JsonProperty("Waiting_for_seller_date_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? Waiting_for_seller_date_time { get; set; }
        [JsonProperty("Returned_to_seller_date_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? Returned_to_seller_date_time { get; set; }
        public long Waiting_for_seller_days { get; set; }
        public double? Returns_keeping_cost { get; set; }
        public long Sku { get; set; }
        public ReturnStatus Status { get; set; }
        public OzonVisual? Visual { get; set; }
    }

    public class OzonVisual
    {
        public Status? Status { get; set; }
    }

    public class Status
    {
        public int Id { get; set; }
        public string? Display_name { get; set; }
        public ReturnStatus Sys_name { get; set; }
    }
}
