﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Справочники.Мастерская;

namespace StinWeb.Models.DataManager.Документы.Мастерская
{
    public class ФормаПеремещениеИзделий
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public bool ExpressForm { get; set; }
        public string НомерКвитанции { get; set; }
        public decimal ДатаКвитанции { get; set; }
        public string КвитанцияId
        {
            get { return string.IsNullOrEmpty(this.НомерКвитанции) ? "" : this.НомерКвитанции + "-" + this.ДатаКвитанции.ToString(); }
        }
        public Контрагент Заказчик { get; set; }
        public Телефон Телефон { get; set; }
        public Email Email { get; set; }
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
        public Мастер Мастер { get; set; }
        public DateTime ДатаПродажи { get; set; }
        public DateTime ДатаПриема { get; set; }
        public DateTime ДатаОбращения { get; set; }
        public decimal НомерРемонта { get; set; }
        public string Комплектность { get; set; }
        public Склад Склад { get; set; }
        public ПодСклад ПодСклад { get; set; }
        public Склад СкладПолучатель { get; set; }
        public ПодСклад ПодСкладПолучатель { get; set; }
        public Склад СкладОткуда { get; set; }
        public string СтатусПартииId { get; set; }
        public string СтатусПартии
        {
            get
            {
                return Common.СтатусПартии.Where(x => x.Key == this.СтатусПартииId).Select(y => y.Value).FirstOrDefault();
            }
        }
        public Неисправность Неисправность { get; set; }
        public Неисправность Неисправность2 { get; set; }
        public Неисправность Неисправность3 { get; set; }
        public Неисправность Неисправность4 { get; set; }
        public Неисправность Неисправность5 { get; set; }
        public string ВидДокумента { get; set; }
        public Маршрут НомерМаршрута { get; set; }
        public ФормаПеремещениеИзделий()
        {
            this.Общие = new ОбщиеРеквизиты();
        }
        public ФормаПеремещениеИзделий(ТипыФормы типФормы)
        {
            this.Общие = new ОбщиеРеквизиты();
            this.Общие.ТипФормы = типФормы;
        }
    }
}
