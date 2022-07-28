using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace YandexClasses
{
    public class FirstMileShipmentsRequest
    {
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy")]
        public DateTime DateFrom { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy")]
        public DateTime DateTo { get; set; }
    }
    public class FirstMileConfirmRequest
    {
        public string ExternalShipmentId { get; set; }
        public List<long> OrderIds { get; set; }
    }
    public class FirstMileConfirmResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Error> Errors { get; set; }
    }
    public class FirstMileShipmentsResponse
    {
        public ResponseStatus Status { get; set; }
        public FirstMileResult Result { get; set; }
        public List<Error> Errors { get; set; }
    }
    public class FirstMileShipmentInfoResponse
    {
        public ResponseStatus Status { get; set; }
        public FirstMileInfoResult Result { get; set; }
        public List<Error> Errors { get; set; }
    }
    public class FirstMileResult
    {
        public FirstMilePaging Paging { get; set; }
        public List<FirstMileShipment> Shipments { get; set; }
    }
    public class FirstMileInfoResult
    {
        public long Id { get; set; }
        public DateTime PlanIntervalFrom { get; set; }
        public DateTime PlanIntervalTo { get; set; }
        public FirstMileShipmentType ShipmentType { get; set; }
        public FirstMileWarehouse Warehouse { get; set; }
        public FirstMileWarehouse WarehouseTo { get; set; }
        public FirstMileDeliveryService DeliveryService { get; set; }
        public FirstMileStatus CurrentStatus { get; set; }
        public List<long> OrderIds { get; set; }
        public List<LogisticsActions> AvailableActions { get; set; }
    }
    public class FirstMileStatus
    {
        public LogisticsStatus Status { get; set; }
        public string Description { get; set; }
        public DateTime UpdateTime { get; set; }
    }
    public class FirstMileWarehouse
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }
    public class FirstMilePaging
    {
        public string NextPageToken { get; set; }
    }
    public class FirstMileShipment
    {
        public long Id { get; set; }
        public DateTime PlanIntervalFrom { get; set; }
        public DateTime PlanIntervalTo { get; set; }
        public FirstMileShipmentType ShipmentType { get; set; }
        public string ExternalId { get; set; }
        public LogisticsStatus Status { get; set; }
        public string StatusDescription { get; set; }
        public FirstMileDeliveryService DeliveryService { get; set; }
        public int DraftCount { get; set; }
        public int PlannedCount { get; set; }
        public int FactCount { get; set; }
    }
    public class FirstMileDeliveryService
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum FirstMileShipmentType
    {
        NotFound = 0,
        IMPORT = 1,
        WITHDRAW = 2
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum LogisticsStatus
    {
        NotFound = 0,
        WAITING_DEPARTURE = 1,
        OUTBOUND_PLANNED = 2,
        OUTBOUND_CREATED = 3,
        OUTBOUND_CONFIRMED = 4,
        MOVEMENT_COURIER_FOUND = 5,
        MOVEMENT_HANDED_OVER = 6,
        MOVEMENT_DELIVERING = 7,
        MOVEMENT_DELIVERED = 8,
        INBOUND_ARRIVED = 9,
        INBOUND_ACCEPTANCE = 10,
        INBOUND_ACCEPTED = 11,
        INBOUND_SHIPPED = 12,
        FINISHED_WITHOUT_MATCH = 13,
        ERROR = 14
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum LogisticsActions
    {
        NotFound = 0,
        CONFIRM = 1,
        DOWNLOAD_ACT = 2
    }
}
