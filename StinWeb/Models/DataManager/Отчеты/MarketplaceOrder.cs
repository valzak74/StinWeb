using StinClasses;
using System;
using System.Text;

namespace StinWeb.Models.DataManager.Отчеты
{
    public abstract class OrderTemplate
    {
        public string Склады { get; set; }
        public string МаршрутНаименование { get; set; }
        public string ТипДоставки { get; set; }
        public decimal Status { get; set; }
        public int StatusCode { get; set; }
        public string StatusDescription 
        { 
            get
            {
                StringBuilder sb = new StringBuilder();
                switch (this.StatusCode)
                {
                    case 1:
                        sb.Append("Заказ(одобрен)");
                        break;
                    case 2:
                        sb.Append("Резерв");
                        break;
                    case 3:
                        sb.Append("Набор");
                        break;
                    case 4:
                        sb.Append("Готов/");
                        sb.Append(Status switch 
                        {
                            1 => "Груз сформирован",
                            2 => "Этикетки получены",
                            3 => "Готов к отгрузке",
                            7 => "Поступил запрос на отмену",
                            9 => ТипДоставки,
                            13 => "Спорный",
                            < 0 => Status.ToString(),
                            _ => ""
                        });
                        break;
                    case 5:
                        sb.Append("Отменен");
                        break;
                    case 6:
                        sb.Append("Реализация");
                        break;
                    default:
                        sb.Append("Состояние ");
                        sb.Append(StatusCode.ToString());
                        break;
                }
                return sb.ToString();
            }
        }
    }
    public class MarketplaceOrder : OrderTemplate
    {
        public string Id { get; set; }
        public string Тип { get; set; }
        public string MarketplaceType { get; set; }
        public string MarketplaceId { get; set; }
        public string ПредварительнаяЗаявкаНомер { get; set; }
        public string СкладIds { get; set; }
        public bool isFBS { get; set; }
        public bool isExpress { get; set; }
        public bool NeedToGetPayment { get; set; }
        public decimal Сумма { get; set; }
        public string ИнформацияAPI { get; set; }
        public DateTime ShipmentDate { get; set; }
        public string RecipientName { get; set; }
        public string Family { get; set; }
        public string Name { get; set; }
        public string SerName { get; set; }
        public string Recipient
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (string.IsNullOrEmpty(RecipientName))
                {
                    sb.ConditionallyAppend(Family, " ");
                    sb.ConditionallyAppend(Name, " ");
                    sb.ConditionallyAppend(SerName, " ");
                }
                else
                    sb.ConditionallyAppend(RecipientName);
                return sb.ToString();
            }
        }
        public string Phone { get; set; }
        public string Town { get; set; }
        public string Street { get; set; }
        public string House { get; set; }
        public string Block { get; set; }
        public string Entrance { get; set; }
        public string Intercom { get; set; }
        public string Floor { get; set; }
        public string Flat { get; set; }
        public string Address
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.ConditionallyAppend(Town);
                if (!string.IsNullOrEmpty(Street))
                {
                    sb.ConditionallyAppend("ул.");
                    sb.Append(Street);
                }
                if (!string.IsNullOrEmpty(House))
                {
                    sb.ConditionallyAppend("д.");
                    sb.Append(House);
                }
                if (!string.IsNullOrEmpty(Block))
                {
                    sb.ConditionallyAppend("корп.");
                    sb.Append(Block);
                }
                if (!string.IsNullOrEmpty(Entrance))
                {
                    sb.ConditionallyAppend("п.");
                    sb.Append(Entrance);
                }
                if (!string.IsNullOrEmpty(Intercom))
                {
                    sb.ConditionallyAppend("домофон ");
                    sb.Append(Intercom);
                }
                if (!string.IsNullOrEmpty(Floor))
                {
                    sb.ConditionallyAppend("эт.");
                    sb.Append(Floor);
                }
                if (!string.IsNullOrEmpty(Flat))
                {
                    sb.ConditionallyAppend("кв.");
                    sb.Append(Flat);
                }
                return sb.ToString();
            }
        }
        public decimal СуммаКОплате { get; set; }
        public string CustomerNotes { get; set; }
        public string Labels { get; set; }
        public bool Printed { get; set; }
    }
    public class LoadingListOrder : OrderTemplate
    {
        public string OrderNo { get; set; }
        public int Scanned { get; set; }
        public decimal КолГрузоМест { get; set; }
        public decimal КолТовара { get; set; }
        public decimal СуммаТовара { get; set; }
    }
}
