using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Справочники.Мастерская;

namespace StinWeb.Models.DataManager.Документы.Мастерская
{
    public class ФормаПриемВРемонт
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public bool ExpressForm { get; set; }
        public string НомерКвитанции { get; set; }
        public decimal ДатаКвитанции { get; set; }
        public bool Претензия { get; set; }
        public string КвитанцияId
        {
            get { return string.IsNullOrEmpty(this.НомерКвитанции) ? "" : this.НомерКвитанции + "-" + this.ДатаКвитанции.ToString(); }
        }
        public Номенклатура Изделие { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите Заводской номер")]
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
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите комплектность")]
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
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите внешний вид")]
        public string ВнешнийВид { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент1 { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент2 { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент3 { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент4 { get; set; }
        public ПриложенныйДокумент ПриложенныйДокумент5 { get; set; }
        public List<BinaryData> Photos { get; set; }

        public ФормаПриемВРемонт()
        {
            this.Общие = new ОбщиеРеквизиты();
        }
        public ФормаПриемВРемонт(ТипыФормы типФормы)
        {
            this.Общие = new ОбщиеРеквизиты();
            this.Неисправность = new Неисправность();
            this.Неисправность2 = new Неисправность();
            this.Неисправность3 = new Неисправность();
            this.Неисправность4 = new Неисправность();
            this.Неисправность5 = new Неисправность();
            this.ПриложенныйДокумент1 = new ПриложенныйДокумент();
            this.ПриложенныйДокумент2 = new ПриложенныйДокумент();
            this.ПриложенныйДокумент3 = new ПриложенныйДокумент();
            this.ПриложенныйДокумент4 = new ПриложенныйДокумент();
            this.ПриложенныйДокумент5 = new ПриложенныйДокумент();
            this.Заказчик = new Контрагент();
            this.Телефон = new Телефон();
            this.Email = new Email();
            this.Общие.ТипФормы = типФормы;
        }
    }
}
