using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Models.DataManager.Отчеты
{
    public class MarketplaceOrder
    {
        public string Id { get; set; }
        public string Тип { get; set; }
        public string MarketplaceType { get; set; }
        public string MarketplaceId { get; set; }
        public string ПредварительнаяЗаявкаНомер { get; set; }
        public string СкладIds { get; set; }
        public string Склады { get; set; }
        public int Статус { get; set; }
        public string StatusDescription { get; set; }
        public string МаршрутНаименование { get; set; }
        public bool isFBS { get; set; }
        public bool isExpress { get; set; }
        public string ТипДоставки { get; set; }
        public bool NeedToGetPayment { get; set; }
        public decimal Сумма { get; set; }
        public string ИнформацияAPI { get; set; }
        public DateTime ShipmentDate { get; set; }
        public string Recipient { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public decimal СуммаКОплате { get; set; }
        public string CustomerNotes { get; set; }
        public string Labels { get; set; }
        public bool Printed { get; set; }
    }
}
