using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliExpressClasses
{
    public class LocalOrdersRequest
    {
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Date_start { get; set; }
        public bool ShouldSerializeDate_start() => Date_start > DateTime.MinValue;
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Date_end { get; set; }
        public bool ShouldSerializeDate_end() => Date_end > DateTime.MinValue;
        public List<OrderStatus>? Order_statuses { get; set; }
        public bool ShouldSerializeOrder_statuses() => Order_statuses?.Count > 0;
        public List<PaymentStatus>? Payment_statuses { get; set; }
        public bool ShouldSerializePayment_statuses() => Payment_statuses?.Count > 0;
        public List<DeliveryStatus>? Delivery_statuses { get; set; }
        public bool ShouldSerializeDelivery_statuses() => Delivery_statuses?.Count > 0;
        public List<AntifraudStatus>? Antifraud_statuses { get; set; }
        public bool ShouldSerializeAntifraud_statuses() => Antifraud_statuses?.Count > 0;
        public List<long>? Order_ids { get; set; }
        public bool ShouldSerializeOrder_ids() => Order_ids?.Count > 0;
        public SortingOrder Sorting_order { get; set; }
        public bool ShouldSerializeSorting_order() => Sorting_order != SortingOrder.NotFound;
        public string? Sorting_field { get; set; }
        public bool ShouldSerializeSorting_field() => !string.IsNullOrWhiteSpace(Sorting_field);
        public List<string>? Tracking_numbers { get; set; }
        public bool ShouldSerializeTracking_numbers() => Tracking_numbers?.Count > 0;
        public int Page_size { get; set; }
        public int Page { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Update_at_from { get; set; }
        public bool ShouldSerializeUpdate_at_from() => Update_at_from > DateTime.MinValue;
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Updated_at_to { get; set; }
        public bool ShouldSerializeUpdated_at_to() => Updated_at_to > DateTime.MinValue;
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Shipping_day_from { get; set; }
        public bool ShouldSerializeShipping_day_from() => Shipping_day_from > DateTime.MinValue;
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Shipping_day_to { get; set; }
        public bool ShouldSerializeShipping_day_to() => Shipping_day_to > DateTime.MinValue;
        public TradeOrderInfo Trade_order_info { get; set; }
        public bool ShouldSerializeTrade_order_info() => Trade_order_info != TradeOrderInfo.NotFound;
        public LocalOrdersRequest() => Page_size = 20;
        public LocalOrdersRequest(int page): this() => Page = page;
        public LocalOrdersRequest(int page, int pageSize) : this(page) => Page_size = pageSize;
    }
    public class OrdersRequest
    {
        public int Page_size { get; set; }
        public int Current_page { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Create_date_start { get; set; }
        public bool ShouldSerializeCreate_date_start()
        {
            return Create_date_start.HasValue && (Create_date_start.Value > DateTime.MinValue);
        }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Create_date_end { get; set; }
        public bool ShouldSerializeCreate_date_end()
        {
            return Create_date_end.HasValue && (Create_date_end.Value > DateTime.MinValue);
        }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Modified_date_start { get; set; }
        public bool ShouldSerializeModified_date_start()
        {
            return Modified_date_start.HasValue && (Modified_date_start.Value > DateTime.MinValue);
        }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Modified_date_end { get; set; }
        public bool ShouldSerializeModified_date_end()
        {
            return Modified_date_end.HasValue && (Modified_date_end.Value > DateTime.MinValue);
        }
        public OrderStatus Order_status { get; set; }
        public bool ShouldSerializeOrder_status()
        {
            return Order_status != OrderStatus.NotFound;
        }
        public List<OrderStatus>? Order_status_list { get; set; }
        public bool ShouldSerializeOrder_status_list()
        {
            return Order_status_list != null;
        }
        public string? Buyer_login_id { get; set; }
        public bool ShouldSerializeBuyer_login_id()
        {
            return !string.IsNullOrEmpty(Buyer_login_id);
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum OrderStatus
    {
        NotFound = 0,
        PLACE_ORDER_SUCCESS = 1,
        IN_CANCEL = 2,
        WAIT_SELLER_SEND_GOODS = 3,
        SELLER_PART_SEND_GOODS = 4,
        WAIT_BUYER_ACCEPT_GOODS = 5,
        FUND_PROCESSING = 6,
        IN_ISSUE = 7,
        IN_FROZEN = 8,
        WAIT_SELLER_EXAMINE_MONEY = 9,
        RISK_CONTROL = 10,
        FINISH = 11,
        Created = 12,
        InProgress = 13,
        Finished = 14,
        Cancelled = 15
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum PaymentStatus
    {
        NotFound = 0,
        NotPaid = 1,
        Hold = 2,
        Paid = 3,
        Cancelled = 4,
        Failed = 5
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum DeliveryStatus
    {
        NotFound = 0,
        Init = 1,
        PartialShipped = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum AntifraudStatus
    {
        NotFound = 0,
        NotChecked = 1,
        Checking = 2,
        Blocked = 3,
        Passed = 4,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum TradeOrderInfo
    {
        NotFound = 0,
        Common = 1,
        LogisticInfo = 2,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum LogisticsStatus
    {
        NotFound = 0,
        WAIT_SELLER_SEND_GOODS = 1,
        SELLER_SEND_PART_GOODS = 2,
        SELLER_SEND_GOODS = 3,
        BUYER_ACCEPT_GOODS = 4,
        NO_LOGISTICS = 5,
        New = 6,
        AwaitingCreateOrder = 7,
        OrderCreationProblems = 8,
        AwaitingHandoverList = 9,
        AddingToHandoverProblems = 10,
        AwaitingConfirmation = 11,
        AwaitingDispatch = 12,
        OrderReceivedFromSeller = 13,
        CrossDocSorting = 14,
        CrossDocSent = 15,
        ProviderPostingReceive = 16,
        ProviderPostingLeftTheReception = 17,
        ProviderPostingArrivedAtSorting = 18,
        ProviderPostingSorting = 19,
        ProviderPostingLeftTheSorting = 20,
        ProviderPostingArrived = 21,
        ProviderPostingDelivered = 22,
        ProviderPostingUnsuccessfulAttemptOfDelivery = 23,
        ProviderPostingInReturn = 24,
        ProviderPostingTemporaryStorage = 25,
        ProviderPostingReturned = 26,
        Rejected = 27,
        Cancelled = 28
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum IssueStatus
    {
        NotFound = 0,
        NO_ISSUE = 1,
        IN_ISSUE = 2,
        END_ISSUE = 3,
        NoDispute = 4,
        InProcess = 5,
        Finished = 6
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum FundStatus
    {
        NotFound = 0,
        NOT_PAY = 1,
        PAY_SUCCESS = 2,
        WAIT_SELLER_CHECK = 3,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum FrozenStatus
    {
        NotFound = 0,
        NO_FROZEN = 1,
        IN_FROZEN = 2,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum BizType
    {
        NotFound = 0,
        AE_COMMON = 1,
        AE_TRIAL= 2,
        AE_RECHARGE = 3,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ShipperType
    {
        NotFound = 0,
        SELLER_SEND_GOODS = 1,
        WAREHOUSE_SEND_GOODS = 2,
    }
    public class OrdersResponse
    {
        public Aliexpress_solution_order_get_response? Aliexpress_solution_order_get_response { get; set; }
    }
    public class Aliexpress_solution_order_get_response
    {
        public GetOrderResult? Result { get; set; }
    }
    public class GetOrderResult
    {
        public string? Error_message { get; set; }
        public int Total_count { get; set; }
        public int Current_page { get; set; }
        public int Page_size { get; set; }
        public int Total_page { get; set; }
        public string? Error_code { get; set; }
        public bool Success { get; set; }
        public string? Time_stamp { get; set; }
        public Target_list? Target_list { get; set; }
    }
    public class Target_list
    {
        public List<Order_dto>? Order_dto { get; set; }
    }
    public class Order_dto
    {
        public long Timeout_left_time { get; set; }
        public string? Seller_signer_fullname { get; set; }
        public string? Seller_operator_login_id { get; set; }
        public string? Seller_login_id { get; set; }
        public Product_list? Product_list { get; set; }
        public bool Phone { get; set; }
        public string? Payment_type { get; set; }
        public SimpleMoney? Pay_amount { get; set; }
        public OrderStatus Order_status { get; set; }
        public long Order_id { get; set; }
        public string? Order_detail_url { get; set; }
        public LogisticsStatus Logistics_status { get; set; }
        public string? Logisitcs_escrow_fee_rate { get; set; }
        public SimpleMoney? Loan_amount { get; set; }
        public string? Left_send_good_min { get; set; }
        public string? Left_send_good_hour { get; set; }
        public string? Left_send_good_day { get; set; }
        public IssueStatus Issue_status { get; set; }
        public bool Has_request_loan { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_update { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_send_goods_time { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_pay_time { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_create { get; set; }
        public FundStatus Fund_status { get; set; }
        public FrozenStatus Frozen_status { get; set; }
        public long Escrow_fee_rate { get; set; }
        public SimpleMoney? Escrow_fee { get; set; }
        public string? End_reason { get; set; }
        public string? Buyer_signer_fullname { get; set; }
        public string? Buyer_login_id { get; set; }
        public BizType Biz_type { get; set; }
        public string? Offline_pickup_type { get; set; }
    }
    public class Product_list
    {
        public List<OrderProduct>? Order_product_dto { get; set; }
    }
    public class OrderProduct
    {
        public SimpleMoney? Total_product_amount { get; set; }
        public OrderStatus Son_order_status { get; set; }
        public string? Sku_code { get; set; }
        public OrderStatus Show_status { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Send_goods_time { get; set; }
        public ShipperType Send_goods_operator { get; set; }
        public SimpleMoney? Product_unit_price { get; set; }
        public string? Product_unit { get; set; }
        public string? Product_standard { get; set; }
        public string? Product_snap_url { get; set; }
        public string? Product_name { get; set; }
        public string? Product_img_url { get; set; }
        public long Product_id { get; set; }
        public long Product_count { get; set; }
        public long Order_id { get; set; }
        public bool Money_back3x { get; set; }
        public string? Memo { get; set; }
        public string? Logistics_type { get; set; }
        public string? Logistics_service_name { get; set; }
        public SimpleMoney? Logistics_amount { get; set; }
        public IssueStatus Issue_status { get; set; }
        public string? Issue_mode { get; set; }
        public int Goods_prepare_time { get; set; }
        public FundStatus fund_status { get; set; }
        public string? Freight_commit_day { get; set; }
        public string? Escrow_fee_rate { get; set; }
        public string? Delivery_time { get; set; }
        public long Child_id { get; set; }
        public bool Can_submit_issue { get; set; }
        public string? Buyer_signer_last_name { get; set; }
        public string? Buyer_signer_first_name { get; set; }
        public string? Afflicate_fee_rate { get; set; }
    }
    public class SimpleMoney
    {
        public string? Currency_code { get; set; }
        public string? Amount { get; set; }
    }
    public class LocalOrderResponse
    {
        public ResponseData? Data { get; set; }
        public Error? Error { get; set; }
    }
    public class ResponseData
    {
        public int Total_count { get; set; }
        public List<AliOrder>? Orders { get; set; }
    }
    public class AliOrder
    {
        public string? Id { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:ssffffffzzz")]
        public DateTime Created_at { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:ssffffffzzz")]
        public DateTime Paid_at { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:ssffffffzzz")]
        public DateTime Updated_at { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus Payment_status { get; set; }
        public DeliveryStatus Delivery_status { get; set; }
        public string? Delivery_address { get; set; }
        public AntifraudStatus Antifraud_status { get; set; }
        public string? Buyer_country_code { get; set; }
        public string? Buyer_name { get; set; }
        public OrderDisplayStatus Order_display_status { get; set; }
        public string? Buyer_phone { get; set; }
        public List<OrderLine>? Order_lines { get; set; }
        decimal _total_amount;
        public decimal Total_amount { get => _total_amount; set { _total_amount = value / 100; } }
        public string? Seller_comment { get; set; }
        public bool Fully_prepared { get; set; }
        public string? Finish_reason { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Cut_off_date { get; set; }
        public List<CutOffDateHistory>? Cut_off_date_histories { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Shipping_deadline { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Next_cut_off_date { get; set; }
        public List<PreSplitPosition>? Pre_split_postings { get; set; }
        public List<LogisticsOrder>? Logistic_orders { get; set; }
    }
    public class LogisticsOrder
    {
        public long Id { get; set; }
        public long Trade_order_id { get; set; }
        public string? Track_number { get; set; }
        public LogisticsStatus Status { get; set; }
        public CreationError? Creation_error { get; set; }
        public List<Line>? Lines { get; set; }
        public Commission? Commission { get; set; }
    }
    public class Commission
    {
        public decimal Platform_fee { get; set; }
        public decimal Affiliate_fee { get; set; }
        public decimal Estimate_revenue { get; set; }
    }
    public class Line
    {
        public long Sku_id { get; set; }
        public int Quantity { get; set; }
    }
    public class CreationError
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
    }
    public class PreSplitPosition
    {
        public long Id { get; set; }
        private decimal _deliveryFee;
        public decimal Delivery_fee { get { return _deliveryFee; } set { _deliveryFee = value / 100; } }
        public FirstMileType First_mile_type { get; set; }
        public string? Logistic_method { get; set; }
        public string? Logistics_type { get; set; }
        public List<OrderLine>? Posting_lines { get; set; }
    }
    public class CutOffDateHistory
    {
        public int Shift_number { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-ddTHH:mm:sszzz")]
        public DateTime Cut_off_date { get; set; }
    }
    public class OrderLine
    {
        public long Id { get; set; }
        public long Order_line_id { get; set; }
        public string? Item_id { get; set; }
        public string? Sku_id { get; set; }
        public string? Sku_code { get; set; }
        public string? Name { get; set; }
        public string? Img_url { get; set; }
        decimal _item_price;
        public decimal Item_price { get => _item_price; set { _item_price = value / 100; } }
        public decimal Quantity { get; set; }
        decimal _total_amount;
        public decimal Total_amount { get => _total_amount; set { _total_amount = value / 100; } }
        public List<string>? Properties { get; set; }
        public string? Buyer_comment { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public IssueStatus Issue_status { get; set; }
        public List<Promotion>? Promotions { get; set; }
    }
    public class Promotion
    {
        public long Ae_promotion_id { get; set; }
        public long Ae_activity_id { get; set; }
        public string? Code { get; set; }
        public string? Promotion_type { get; set; }
        public decimal? Discount { get; set; }
        public string? Discount_currency { get; set; }
        public decimal? Original_discount { get; set; }
        public string? Original_discount_currency { get; set; }
        public string? Promotion_target { get; set; }
        public string? Budget_sponsor { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)Unknown)]
    public enum OrderDisplayStatus
    {
        Unknown = 0,
        PlaceOrderSuccess = 1,
        PaymentPending = 2,
        WaitExamineMoney = 3,
        WaitGroup = 4,
        WaitSendGoods = 5,
        PartialSendGoods = 6,
        WaitAcceptGoods = 7,
        InCancel = 8,
        Complete = 9,
        Close = 10,
        Finish = 11,
        InFrozen = 12,
        InIssue = 13
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum FirstMileType
    {
        NotFound = 0,
        Pickup = 1,
        Dropoff = 2,
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum SortingOrder
    {
        NotFound = 0,
        ASC = 1,
        DESC = 2,
        NONE = 3,
    }
}
