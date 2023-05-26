using JsonExtensions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace YandexClasses
{
    public class OfferMappingEntriesResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Error> Errors { get; set; }
        public OfferMappingResult Result { get; set; }
    }

    public class OfferMappingResult
    {
        public Paging Paging { get; set; }
        public List<OfferMappingEntry> OfferMappingEntries { get; set; }
    }
    public class OfferMappingUpdateRequest
    {
        public List<OfferMappingEntry> OfferMappingEntries { get; set; }
    }
    public class OfferMappingUpdateResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Error> Errors { get; set; }
    }
    public class OfferMappingEntry
    {
        public Offer Offer { get; set; }
        public Mapping Mapping { get; set; }
        public Mapping AwaitingModerationMapping { get; set; }
        public Mapping RejectedMapping { get; set; }
        public bool ShouldSerializeAwaitingModerationMapping()
        {
            return AwaitingModerationMapping != null;
        }
        public bool ShouldSerializeRejectedMapping()
        {
            return RejectedMapping != null;
        }
    }
    public class Mapping 
    {
        public long MarketSku { get; set; }
        public long? ModelId { get; set; }
        public long? CategoryId { get; set; }
        public bool ShouldSerializeModelId()
        {
            return ModelId != null;
        }
        public bool ShouldSerializeCategoryId()
        {
            return CategoryId != null;
        }
    }
    public class Offer
    {
        public string ShopSku { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public List<string> ManufacturerCountries { get; set; }
        public WeightDimensions WeightDimensions { get; set; }
        public List<string> Urls { get; set; }
        public List<string> Pictures { get; set; }
        public string Vendor { get; set; }
        public string VendorCode { get; set; }
        public List<string> Barcodes { get; set; }
        public string Description { get; set; }
        public LifePeriod ShelfLife { get; set; }
        public LifePeriod LifeTime { get; set; }
        public LifePeriod GuaranteePeriod { get; set; }
        public List<string> CustomsCommodityCodes { get; set; }
        public string Certificate { get; set; }
        public long TransportUnitSize { get; set; }
        public long MinShipment { get; set; }
        public long QuantumOfSupply { get; set; }
        public DayOfWeekType[] SupplyScheduleDays { get; set; }
        public long DeliveryDurationDays { get; set; }
        public long BoxCount { get; set; }
        public long ShelfLifeDays { get; set; }
        public long LifeTimeDays { get; set; }
        public long GuaranteePeriodDays { get; set; }
        public Availability? Availability { get; set; }
        public ProcessingState? ProcessingState { get; set; }
        public bool ShouldSerializeAvailability()
        {
            return Availability != null;
        }
        public bool ShouldSerializeProcessingState()
        {
            return ProcessingState != null;
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum DayOfWeekType
    {
        NotFound,
        MONDAY, 
        TUESDAY, 
        WEDNESDAY, 
        THURSDAY, 
        FRIDAY, 
        SATURDAY, 
        SUNDAY
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum Availability
    {
        ACTIVE = 1,
        INACTIVE = 2,
        DELISTED = 3,
        NotFound
    }

    public class ProcessingState
    {
        public StateStatus Status { get; set; }
        public List<StateNote> Notes { get; set; }
    }

    public class StateNote
    {
        public StateType Type { get; set; }
        public string Payload { get; set; }
    }

    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StateType
    {
        NotFound,
        ASSORTMENT, 
        CANCELLED, 
        CONFLICTING_INFORMATION, 
        OTHER, 
        DEPARTMENT_FROZEN, 
        INCORRECT_INFORMATION, 
        LEGAL_CONFLICT, 
        NEED_CLASSIFICATION_INFORMATION, 
        NEED_INFORMATION, 
        NEED_PICTURES, 
        NEED_VENDOR, 
        NO_CATEGORY, 
        NO_KNOWLEDGE, 
        NO_PARAMETERS_IN_SHOP_TITLE, 
        NO_SIZE_MEASURE, 
        SAMPLE_LINE
    }

    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum StateStatus
    {
        NotFound,
        UNKNOWN, 
        READY, 
        IN_WORK, 
        NEED_INFO, 
        NEED_MAPPING, 
        NEED_CONTENT, 
        CONTENT_PROCESSING, 
        SUSPENDED, 
        REJECTED, 
        REVIEW, 
        CREATE_ERROR, 
        UPDATE_ERROR
    }

    public class LifePeriod
    {
        public long TimePeriod { get; set; }
        public TimeUnit TimeUnit { get; set; }
        public string Comment { get; set; }
    }

    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum TimeUnit
    {
        HOUR = 1,
        DAY = 2,
        WEEK = 3,
        MONTH = 4,
        YEAR = 5,
        NotFound
    }

    public class WeightDimensions
    {
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
    }
}
