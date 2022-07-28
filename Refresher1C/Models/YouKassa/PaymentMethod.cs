using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Refresher1C.Models.YouKassa
{
    public class PaymentMethod
    {
        public VatData VatData { get; set; }
        public string PaymentData { get; set; }
        public string GoogleTransactionId { get; set; }
        public string PaymentMethodToken { get; set; }
        public string AccountNumber { get; set; }
        public string PaymentPurpose { get; set; }
        public string Login { get; set; }
        public string Phone { get; set; }
        public string Title { get; set; }
        public bool Saved { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public Card Card { get; set; }
        public PaymentMethod()
        {
        }
    }
    public class VatData
    {
        public VatDataType Type { get; set; }
        public string Rate { get; set; }
        public Amount Amount { get; set; }
        public PayerBankDetails PayerBankDetails { get; set; }
        public VatData()
        {
        }
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VatDataType
    {
        Calculated = 0,
        Untaxed = 1
    }
    public class PayerBankDetails
    {
        public string FullName { get; set; }
        public string ShortName { get; set; }
        public string Address { get; set; }
        public string Kpp { get; set; }
        public string Inn { get; set; }
        public string BankBranch { get; set; }
        public string BankBik { get; set; }
        public string Account { get; set; }
        public PayerBankDetails()
        {

        }
    }
}
