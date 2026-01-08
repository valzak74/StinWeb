using JsonExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AtolOnlineClasses
{
    public class SellRequest
    {
        [JsonConverter(typeof(DateFormatConverter), "dd.MM.yyyy HH:mm:ss")]
        public DateTime timestamp { get; set; }
        public string external_id { get; set; }
        public SellService service { get; set; }
        public Receipt receipt { get; set; }
        public bool ShouldSerializeservice()
        {
            return service != null;
        }
    }
    public class SellService
    {
        public string callback_url { get; set; }
    }
    public class Receipt
    {
        public SellClient client { get; set; }
        public SellCompany company { get; set; }
        public List<SellItem> items { get; set; }
        public List<SellPaymentItem> payments { get; set; }
        public List<SellVat> vats { get; set; }
        [JsonConverter(typeof(DecimalFormat2PlacesConverter))]
        public decimal total { get; set; }
    }
    public class SellClient
    {
        public string name { get; set; }
        public string inn { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public bool ShouldSerializename()
        {
            return !string.IsNullOrWhiteSpace(name);
        }
        public bool ShouldSerializeinn()
        {
            return !string.IsNullOrWhiteSpace(inn);
        }
        public bool ShouldSerializeemail()
        {
            return !string.IsNullOrWhiteSpace(email);
        }
        public bool ShouldSerializephone()
        {
            return !string.IsNullOrWhiteSpace(phone);
        }
    }
    public class SellCompany
    {
        public string email { get; set; }
        public Sno sno { get; set; }
        public string inn { get; set; }
        public string payment_address { get; set; }
        public bool ShouldSerializeemail()
        {
            return !string.IsNullOrWhiteSpace(email);
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum Sno
    {
        NotFound = 100,
        osn = 0,
        usn_income = 1,
        usn_income_outcome = 2,
        envd = 3,
        esn = 4,
        patent = 5
    }
    public class SellItem
    {
        public string name { get; set; }
        [JsonConverter(typeof(DecimalFormat2PlacesConverter))]
        public decimal price { get; set; }
        [JsonConverter(typeof(DecimalFormat3PlacesConverter))]
        public decimal quantity { get; set; }
        [JsonConverter(typeof(DecimalFormat2PlacesConverter))]
        public decimal sum { get; set; }
        public string measurement_unit { get; set; }
        public SellPaymentMethod payment_method { get; set; }
        public SellPaymentObject payment_object { get; set; }
        public SellVat vat { get; set; }
        public bool ShouldSerializemeasurement_unit()
        {
            return !string.IsNullOrWhiteSpace(measurement_unit);
        }
        public bool ShouldSerializevat()
        {
            return vat != null;
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum SellPaymentMethod
    {
        NotFound = 100,
        full_prepayment = 0,
        prepayment = 1,
        advance = 2,
        full_payment = 3,
        partial_payment = 4,
        credit = 5,
        credit_payment = 6
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum SellPaymentObject
    {
        NotFound = 100,
        commodity = 0,
        excise = 1,
        job = 2,
        service = 3,
        gambling_bet = 4,
        gambling_prize = 5,
        lottery = 6,
        lottery_prize = 7,
        intellectual_activity = 8,
        payment = 9,
        agent_commission = 10,
        composite = 11,
        award = 12,
        another = 13,
        property_right = 14,
        non_operating_gain = 15,
        insurance_premium = 16,
        sales_tax = 17,
        resort_fee = 18,
        deposit = 19,
        expense = 20,
        pension_insurance_ip = 21,
        pension_insurance = 22,
        medical_insurance_ip = 23,
        medical_insurance = 24,
        social_insurance = 25,
        casino_payment = 26
    }
    public class SellVat
    {
        public SellVatType type { get; set; }
        [JsonConverter(typeof(DecimalFormat2PlacesConverter))]
        public decimal sum { get; set; }
        public bool ShouldSerializesum()
        {
            return sum != 0m;
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum SellVatType
    {
        NotFound = 100,
        none = 0,
        vat0 = 1,
        vat10 = 2,
        vat18 = 3,
        vat110 = 4,
        vat118 = 5,
        vat20 = 6,
        vat120 = 7,
        vat22 = 8,
        vat122 = 9,
    }
    public class SellPaymentItem
    {
        public SellPaymentType type { get; set; }
        [JsonConverter(typeof(DecimalFormat2PlacesConverter))]
        public decimal sum { get; set; }
    }
    [JsonConverter(typeof(EnumConverter))]
    public enum SellPaymentType : int
    {
        наличные = 0,
        безналичный = 1,
        зачет_аванса = 2,
        кредит = 3,
        встречное_предоставление = 4,
        расширенный_вид_оплаты1 = 5,
        расширенный_вид_оплаты2 = 6,
        расширенный_вид_оплаты3 = 7,
        расширенный_вид_оплаты4 = 8,
        расширенный_вид_оплаты5 = 9
    }
}