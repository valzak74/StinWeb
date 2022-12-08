using JsonExtensions;
using Newtonsoft.Json;

namespace WbClasses
{
    public class ChangeOrderStatusRequest
    {
        public string? OrderId { get; set; }
        public WbStatus Status { get; set; }
        public List<SgTin>? Sgtin { get; set; }
        public class SgTin
        {
            public string? Code { get; set; }
            public int Numerator { get; set; }
            public int Denominator { get; set; }
            public long Sid { get; set; }
        }
    }
    public class ChangeOrderStatusErrorResponse: Response
    {
        public object? Data { get; set; }
    }
    public class OrderList
    {
        public int Total { get; set; }
        public List<Order>? Orders { get; set; }
    }
    public class Order
    {
        public string? OrderId { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime DateCreated { get; set; }
        public long WbWhId { get; set; }
        public long StoreId { get; set; }
        public long Pid { get; set; }
        public string? OfficeAddress { get; set; }
        public double OfficeLatitude { get; set; }
        public double OfficeLongitude { get; set; }
        public string? DeliveryAddress { get; set; }
        public WbAddressDetails? DeliveryAddressDetails { get; set; }
        public WbUserInfo? UserInfo { get; set; }
        public long ChrtId { get; set; }
        public string? Barcode { get; set;}
        public List<string>? Barcodes { get; set; }
        public List<string>? ScOfficesNames { get; set; }
        public WbStatus Status { get; set; }
        public WbUserStatus UserStatus { get; set; }
        public string? Rid { get; set; }
        decimal _totalPrice;
        public decimal TotalPrice { get => _totalPrice; set { _totalPrice = value / 100; } }
        public int CurrencyCode { get; set; }
        public string? OrderUID { get; set; }
        public WbDeliveryType DeliveryType { get; set; }
        public class WbAddressDetails
        {
            public string? Province { get; set; }
            public string? Area { get; set; }
            public string? City { get; set; }
            public string? Street { get; set; }
            public string? Home { get; set; }
            public string? Flat { get; set; }
            public string? Entrance { get; set; }
            public double longitude { get; set; }
            public double latitude { get; set; }
        }
        public class WbUserInfo
        {
            public long UserId { get; set; }
            public string? Fio { get; set; }
            public string? Phone { get; set; }
        }
    }
    public class StickerRequest
    { 
        public List<long>? OrderIds { get; set; }
        public StickerRequest(List<long>? orderIds) => OrderIds = orderIds;
    }
    public class StickerResponse: Response
    {
        public WbBarcode? Data { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum WbStatus
    {
        NotFound = -1,
        НовыйЗаказ = 0,
        ВРаботе = 1,
        СборочноеЗаданиеЗавершено = 2,
        СборочноеЗаданиеОтклонено = 3,
        НаДоставкеКурьером = 5,
        КурьерДовезКлиентПринялТовар = 6,
        КлиентНеПринялТовар = 7,
        ТоварДляСамовывозаИзМагазинаПринятКРаботе = 8,
        ТоварДляСамовывозаИзМагазинаГотовКВыдаче = 9
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum WbUserStatus
    {
        NotFound = -1,
        НовыйЗаказ = 0,
        ОтменаКлиента = 1,
        Доставлен = 2,
        Возврат = 3,
        Ожидает = 4,
        Брак = 5
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum WbDeliveryType
    {
        NotFound = -1,
        ОбычнаяДоставка = 1,
        ДоставкаСиламиПоставщика = 2
    }
}
