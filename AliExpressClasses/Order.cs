using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliExpressClasses
{
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
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum IssueStatus
    {
        NotFound = 0,
        NO_ISSUE = 1,
        IN_ISSUE = 2,
        END_ISSUE = 3,
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
}
