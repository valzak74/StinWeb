using JsonExtensions;
using Newtonsoft.Json;
using System;

namespace YandexClasses
{
    public class ReturnsResponse
    {
        public ResponseStatus Status { get; set; }
        public ReturnsResult Result { get; set; }
    }
    public class ReturnsResult
    {
        public Paging Paging { get; set; }
        public Return[] Returns { get; set; }
    }
    public class Return
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? CreationDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? UpdateDate { get; set; }
        public RefundStatusType RefundStatus { get; set; }
        public LogisticPickupPoint LogisticPickupPoint { get; set; }
        public RecipientType ShipmentRecipientType { get; set; }
        public ReturnShipmentStatusType ShipmentStatus { get; set; }
        public int RefundAmount { get; set; }
        public ReturnItem[] Items { get; set; }
        public ReturnType ReturnType { get; set; }
    }
    public class LogisticPickupPoint
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
        public string Instruction { get; set; }
        public LogisticPointType Type { get; set; }
        public long LogisticPartnerId { get; set; }
    }
    public class ReturnItem
    {
        public long MarketSku { get; set; }
        public string ShopSku { get; set; }
        public int Count { get; set; }
        public ReturnDecision[] Decisions { get; set; }
        public ReturnInstance[] Instances { get; set; }
        public Track[] tracks { get; set; }
    }
    public class ReturnDecision
    {
        public int ReturnItemId { get; set; }
        public int Count { get; set; }
        public string Comment { get; set; }
        public ReturnDecisionReasonType ReasonType { get; set; }
        public ReturnDecisionSubreasonType SubreasonType { get; set; }
        public ReturnDecisionType DecisionType { get; set; }
        public int RefundAmount { get; set; }
        public int PartnerCompensation { get; set; }
        public string[] Images { get; set; }
    }
    public class ReturnInstance
    {
        public ReturnInstanceStockType StockType { get; set; }
        public ReturnInstanceStatusType Status { get; set; }
        public string Cis { get; set; }
        public string Imei { get; set; }
    }
    public class Track
    {
        public string TrackCode { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum RefundStatusType
    {
        NotFound,
        STARTED_BY_USER,
        REFUND_IN_PROGRESS,
        REFUNDED,
        FAILED,
        WAITING_FOR_DECISION,
        DECISION_MADE,
        REFUNDED_WITH_BONUSES,
        REFUNDED_BY_SHOP,
        CANCELLED
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum LogisticPointType
    {
        NotFound,
        WAREHOUSE,
        PICKUP_POINT,
        PICKUP_TERMINAL,
        PICKUP_POST_OFFICE,
        PICKUP_MIXED,
        PICKUP_RETAIL
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum RecipientType
    {
        NotFound,
        SHOP,
        DELIVERY_SERVICE,
        POST
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ReturnShipmentStatusType
    {
        NotFound,
        CREATED,
        RECEIVED,
        IN_TRANSIT,
        READY_FOR_PICKUP,
        PICKED,
        LOST,
        CANCELLED,
        FULFILMENT_RECEIVED,
        PREPARED_FOR_UTILIZATION,
        UTILIZED
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ReturnType
    {
        NotFound,
        RETURN,
        UNREDEEMED
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ReturnDecisionReasonType
    {
        NotFound,
        BAD_QUALITY,
        DO_NOT_FIT,
        WRONG_ITEM,
        DAMAGE_DELIVERY,
        LOYALTY_FAIL,
        CONTENT_FAIL,
        UNKNOWN
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ReturnDecisionSubreasonType
    {
        NotFound,
        USER_DID_NOT_LIKE, 
        USER_CHANGED_MIND, 
        DELIVERED_TOO_LONG, 
        BAD_PACKAGE, 
        DAMAGED, 
        NOT_WORKING, 
        INCOMPLETENESS, 
        WRONG_ITEM, 
        WRONG_COLOR, 
        DID_NOT_MATCH_DESCRIPTION, 
        UNKNOWN
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ReturnDecisionType
    {
        NotFound,
        REFUND_MONEY, 
        REFUND_MONEY_INCLUDING_SHIPMENT, 
        REPAIR, 
        REPLACE, 
        SEND_TO_EXAMINATION, 
        DECLINE_REFUND, 
        OTHER_DECISION, 
        UNKNOWN
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ReturnInstanceStockType
    {
        NotFound,
        FIT, 
        DEFECT, 
        ANOMALY, 
        SURPLUS, 
        EXPIRED, 
        MISGRADING, 
        UNDEFINED,
        INCORRECT_IMEI, 
        INCORRECT_SERIAL_NUMBER, 
        INCORRECT_CIS, 
        PART_MISSING, 
        NON_COMPLIENT, 
        NOT_ACCEPTABLE, 
        UNKNOWN
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ReturnInstanceStatusType
    {
        NotFound,
        CREATED, 
        RECEIVED, 
        IN_TRANSIT, 
        READY_FOR_PICKUP, 
        PICKED, 
        RECEIVED_ON_FULFILLMENT, 
        CANCELLED, 
        LOST, 
        UTILIZED, 
        PREPARED_FOR_UTILIZATION, 
        NOT_IN_DEMAND
    }
}
