using JsonExtensions;
using Newtonsoft.Json;

namespace OzonClasses
{
    public class AddToDeliveryRequest
    {
        public List<string>? Posting_number { get; set; }
    }
    public class AddToDeliveryResponse
    {
        public bool Result { get; set; }
    }
    public class GetOrderByPostingNumberRequest
    {
        public string? Posting_number { get; set; }
        public RequestWithParams? With { get; set; }
    }
    public class GetOrderByPostingNumberResponse
    {
        public FbsPostingDetail? Result { get; set; } 
    }
    public class FbsPostingDetail
    {
        public List<AdditionalDataItem>? Additional_data { get; set; }
        public Receiver? Addressee { get; set; }
        public AnalyticsData? Analytics_data { get; set; }
        public PostingBarcodes? Barcodes { get; set; }
        public Cancellation? Cancellation { get; set; }
        public Customer? Customer { get; set; }
        [JsonProperty("Delivering_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Delivering_date { get; set; }
        public DeliveryMethod? Delivery_method { get; set; }
        public string? Delivery_price { get; set; }
        public FinancialData? Financial_data { get; set; }
        [JsonProperty("In_process_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime In_process_at { get; set; }
        public bool Is_express { get; set; }
        public long Order_id { get; set; }
        public string? Order_number { get; set; }
        public string? Posting_number { get; set; }
        public ProductExemplar? Product_exemplars { get; set; }
        public List<PostingProductDetail>? Products { get; set; }
        public string? Provider_status { get; set; }
        public PostingRequirements? Requirements { get; set; }
        public DateTime Shipment_date { get; set; }
        public OrderStatus? Status { get; set; }
        public TplIntegrationType tpl_integration_type { get; set; }
        public string? Tracking_number { get; set; }
    }
    public class PostingProductDetail
    {
        public ProductDimensions? Dimensions { get; set; }
        public List<string>? Mandatory_mark { get; set; }
        public string? Name { get; set; }
        public string? Offer_id { get; set; }
        public string? Price { get; set; }
        public int Quantity { get; set; }
        public long Sku { get; set; }
    }
    public class ProductDimensions
    {
        public string? Height { get; set; }
        public string? Length { get; set; }
        public string? Weight { get; set; }
        public string? Width { get; set; }
    }
    public class ProductExemplar
    {
        public List<ExemplarItem>? Products { get; set; }
    }
    public class ExemplarItem
    {
        public List<ExemplarInfo>? Exemplars { get; set; }
        public long Sku { get; set; }
    }
    public class ExemplarInfo
    {
        public string? Mandatory_mark { get; set; }
        public string? Gtd { get; set; }
        public bool Is_gtd_absent { get; set; }
    }
    public class AdditionalDataItem
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }
    public class DetailOrderRequest
    {
        public SortOrder Dir { get; set; }
        public DetailFilter? Filter { get; set; }
        public long Limit { get; set; }
        public long Offset { get; set; }
        public RequestWithParams? With { get; set; }
    }
    public class DetailOrderResponse
    {
        public DetailOrderResult? Result { get; set; }
    }
    public class OzonUnfulfilledOrderRequest
    {
        public SortOrder Dir { get; set; }
        public UnfulfilledFilter? Filter { get; set; }
        public long Limit { get; set; }
        public long Offset { get; set; }
        public RequestWithParams? With { get; set; }
    }
    public class OzonUnfulfilledOrderResponse
    {
        public UnfulfilledOrderResult? Result { get; set; }
    }
    public class DetailFilter
    {
        public List<long>? Delivery_method_id { get; set; }
        public long? Order_id { get; set; }
        public List<long>? Provider_id { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? Since { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? To { get; set; }
        public OrderStatus? Status { get; set; }
        public List<long>? Warehouse_id { get; set; }
        public bool ShouldSerializeDelivery_method_id()
        {
            return (Delivery_method_id != null) && (Delivery_method_id.Count > 0);
        }
        public bool ShouldSerializeOrder_id()
        {
            return (Order_id != null) && (Order_id > 0);
        }
        public bool ShouldSerializeProvider_id()
        {
            return (Provider_id != null) && (Provider_id.Count > 0);
        }
        public bool ShouldSerializeSince()
        {
            return (Since != null) && (Since > DateTime.MinValue);
        }
        public bool ShouldSerializeTo()
        {
            return (To != null) && (To > DateTime.MinValue);
        }
        public bool ShouldSerializeStatus()
        {
            return Status != null;
        }
        public bool ShouldSerializeWarehouse_id()
        {
            return (Warehouse_id != null) && (Warehouse_id.Count > 0);
        }
    }
    public class DetailOrderResult
    {
        public bool Has_next { get; set; }
        public List<FbsPosting>? Postings { get; set; }
    }
    public class UnfulfilledOrderResult
    {
        public long Count { get; set; }
        public List<FbsPosting>? Postings { get; set; }
    }
    public class FbsPosting
    {
        public Receiver? Addressee { get; set; }
        public AnalyticsData? Analytics_data { get; set; }
        public PostingBarcodes? Barcodes { get; set; }
        public Cancellation? Cancellation { get; set; }
        public Customer? Customer { get; set; }
        //[JsonConverter(typeof(NewtonsoftDateTimeConverter))]
        [JsonProperty("Delivering_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Delivering_date { get; set; }
        public DeliveryMethod? Delivery_method { get; set; }
        public FinancialData? Financial_data { get; set; }
        public DateTime In_process_at { get; set; }
        public bool Is_express { get; set; }
        public long Order_id { get; set; }
        public string? Order_number { get; set; }
        public string? Posting_number { get; set; }
        public List<PostingProduct>? Products { get; set; }
        public PostingRequirements? Requirements { get; set; }
        public DateTime Shipment_date { get; set; }
        public string? Status { get; set; }
        public TplIntegrationType tpl_integration_type { get; set; }
        public string? Tracking_number { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum TplIntegrationType
    {
        NotFound = 0,
        ozon = 1,
        tpl_tracking = 2,
        non_integrated = 3,
        aggregator = 4,
    }
    public class PostingRequirements
    {
        public List<long>? Products_requiring_gtd { get; set; }
        public List<long>? Products_requiring_country { get; set; }
        public List<long>? Products_requiring_mandatory_mark { get; set; }
    }
    public class PostingProduct
    {
        public List<string>? Mandatory_mark { get; set; }
        public string? Name { get; set; }
        public string? Offer_id { get; set; }
        public string? Price { get; set; }
        public int Quantity { get; set; }
        public long Sku { get; set; }
    }
    public class FinancialData
    {
        public FinancialServices? Posting_services { get; set; }
        public List<FinancialProduct>? Products { get; set; }
    }
    public class FinancialProduct
    {
        public List<string>? Actions { get; set; }
        public string? Client_price { get; set; }
        public double Commission_amount { get; set; }
        public long Commission_percent { get; set; }
        public FinancialServices? item_services { get; set; }
        public double Old_price { get; set; }
        public double Payout { get; set; }
        public Picking? Picking { get; set; }
        public double Price { get; set; }
        public long Product_id { get; set; }
        public long Quantity { get; set; }
        public double Total_discount_percent { get; set; }
        public double Total_discount_value { get; set; }
    }
    public class Picking
    {
        public double Amount { get; set; }
        public DateTime Moment { get; set; }
        public string? Tag { get; set; }
    }
    public class FinancialServices
    {
        public double Marketplace_service_item_deliv_to_customer { get; set; }
        public double Marketplace_service_item_direct_flow_trans { get; set; }
        public double Marketplace_service_item_dropoff_ff { get; set; }
        public double Marketplace_service_item_dropoff_pvz { get; set; }
        public double Marketplace_service_item_dropoff_sc { get; set; }
        public double Marketplace_service_item_fulfillment { get; set; }
        public double Marketplace_service_item_pickup { get; set; }
        public double Marketplace_service_item_return_after_deliv_to_customer { get; set; }
        public double Marketplace_service_item_return_flow_trans { get; set; }
        public double Marketplace_service_item_return_not_deliv_to_customer { get; set; }
        public double Marketplace_service_item_return_part_goods_customer { get; set; }
    }
    public class DeliveryMethod
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Tpl_provider { get; set; }
        public long Tpl_provider_id { get; set; }
        public string? Warehouse { get; set; }
        public long Warehouse_id { get; set; }
    }
    public class Customer
    {
        public Address? Address { get; set; }
        public string? Customer_email { get; set; }
        public long Customer_id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
    }
    public class Address
    {
        public string? Address_tail { get; set; }
        public string? City { get; set; }
        public string? Comment { get; set; }
        public string? Country { get; set; }
        public string? District { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Provider_pvz_code { get; set; }
        public long Pvz_code { get; set; }
        public string? Region { get; set; }
        public string? Zip_code { get; set; }
    }
    public class Cancellation
    {
        public bool Affect_cancellation_rating { get; set; }
        public string? Cancel_reason { get; set; }
        public long Cancel_reason_id { get; set; }
        public string? Cancellation_initiator { get; set; }
        public string? Cancellation_type { get; set; }
        public bool Cancelled_after_ship { get; set; }
    }
    public class PostingBarcodes
    {
        public string? Lower_barcode { get; set; }
        public string? Upper_barcode { get; set; }
    }
    public class AnalyticsData
    {
        public string? City { get; set; }
        public DateTime Delivery_date_begin { get; set; }
        public DateTime Delivery_date_end { get; set; }
        public string? Delivery_type { get; set; }
        public bool Is_legal { get; set; }
        public bool Is_premium { get; set; }
        public string? Payment_type_group_name { get; set; }
        public string? Region { get; set; }
        public string? Tpl_provider { get; set; }
        public long Tpl_provider_id { get; set; }
        public string? Warehouse { get; set; }
        public long Warehouse_id { get; set; }
    }
    public class Receiver
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
    }
    public class RequestWithParams
    {
        public bool Analytics_data { get; set; }
        public bool Barcodes { get; set; }
        public bool Financial_data { get; set; }
        public bool Translit { get; set; }
    }
    public class UnfulfilledFilter
    {
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime Cutoff_from { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime Cutoff_to { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime Delivering_date_from { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime Delivering_date_to { get; set; }
        public List<long>? Delivery_method_id { get; set; }
        public List<long>? Provider_id { get; set; }
        public OrderStatus Status { get; set; }
        public List<long>? Warehouse_id { get; set; }
        public bool ShouldSerializeCutoff_from()
        {
            return Cutoff_from > DateTime.MinValue;
        }
        public bool ShouldSerializeCutoff_to()
        {
            return Cutoff_to > DateTime.MinValue;
        }
        public bool ShouldSerializeDelivering_date_from()
        {
            return Delivering_date_from > DateTime.MinValue;
        }
        public bool ShouldSerializeDelivering_date_to()
        {
            return Delivering_date_to > DateTime.MinValue;
        }
        public bool ShouldSerializeStatus()
        {
            return Status != OrderStatus.NotFound;
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum OrderStatus
    {
        NotFound = 0,
        acceptance_in_progress = 1,
        awaiting_approve = 2,
        awaiting_packaging = 3,
        awaiting_registration = 4,
        awaiting_deliver = 5,
        arbitration = 6,
        client_arbitration = 7,
        delivering = 8,
        driver_pickup = 9,
        not_accepted = 10,
        delivered = 11,
        cancelled = 12
    }
}
