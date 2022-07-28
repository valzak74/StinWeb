using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;

namespace StinWeb.Models.DataManager.Справочники.Мастерская
{
    public class ИнформацияИзделия
    {
        public ДокОснование ДокОснование { get; set; }
        public ФормаПриемВРемонт ДокПрием { get; set; }
        public string НомерКвитанции { get; set; }
        public decimal ДатаКвитанции { get; set; }
        public string КвитанцияId
        {
            get { return string.IsNullOrEmpty(this.НомерКвитанции) ? "" : this.НомерКвитанции + "-" + this.ДатаКвитанции.ToString(); }
        }
        public bool Претензия { get; set; }
        public Номенклатура Изделие { get; set; }
        public string ЗаводскойНомер { get; set; }
        public decimal Гарантия { get; set; }
        public string ТипРемонта
        {
            get
            {
                return Common.ТипРемонта.Where(x => x.Key == this.Гарантия).Select(y => y.Value).FirstOrDefault();
            }
        }
        public DateTime ДатаПродажи { get; set; }
        public DateTime ДатаПриема { get; set; }
        public DateTime ДатаОбращения { get; set; }
        public decimal НомерРемонта { get; set; }
        public string Комплектность { get; set; }
        public Неисправность Неисправность { get; set; }
        public Неисправность Неисправность2 { get; set; }
        public Неисправность Неисправность3 { get; set; }
        public Неисправность Неисправность4 { get; set; }
        public Неисправность Неисправность5 { get; set; }
        public Контрагент Заказчик { get; set; }
        public Телефон Телефон { get; set; }
        public Email Email { get; set; }
        public Склад Склад { get; set; }
        public ПодСклад ПодСклад { get; set; }
        public Мастер Мастер { get; set; }
        public Склад СкладОткуда { get; set; }
        public string СтатусПартииId { get; set; }
        public string СтатусПартии
        {
            get
            {
                return Common.СтатусПартии.Where(x => x.Key == this.СтатусПартииId).Select(y => y.Value).FirstOrDefault();
            }
        }
        public decimal СпособВозвращенияId { get; set; }
        public string СпособВозвращения
        {
            get
            {
                return Common.СпособыВозвращения.Where(x => x.Key == this.СпособВозвращенияId).Select(y => y.Value).FirstOrDefault();
            }
        }
        public string ВнешнийВид { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент1 { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент2 { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент3 { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент4 { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент5 { get; set; }
        public string Комментарий { get; set; }
        public ИнформацияИзделия()
        {
            this.ДокОснование = new ДокОснование();
        }
    }
}
