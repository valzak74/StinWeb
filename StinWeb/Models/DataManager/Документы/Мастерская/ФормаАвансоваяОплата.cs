using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Справочники.Мастерская;

namespace StinWeb.Models.DataManager.Документы.Мастерская
{
    public class ФормаАвансоваяОплата
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public string НомерКвитанции { get; set; }
        public decimal ДатаКвитанции { get; set; }
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
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите Комплектность")]
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
        public Касса Касса { get; set; }
        public Склад СкладОткуда { get; set; }
        public string СтатусПартииId { get; set; }
        public string СтатусПартии
        {
            get
            {
                return Common.СтатусПартии.Where(x => x.Key == this.СтатусПартииId).Select(y => y.Value).FirstOrDefault();
            }
        }
        public List<BinaryData> Photos { get; set; }
        public List<тчАвансоваяОплата> ТабличнаяЧасть { get; set; }
        public ФормаАвансоваяОплата()
        {
            this.Общие = new ОбщиеРеквизиты();
            this.ТабличнаяЧасть = new List<тчАвансоваяОплата>();
        }
        public ФормаАвансоваяОплата(ТипыФормы типФормы)
        {
            this.Общие = new ОбщиеРеквизиты();
            this.ТабличнаяЧасть = new List<тчАвансоваяОплата>();
            this.Общие.ТипФормы = типФормы;
        }
        public ФормаАвансоваяОплата(ТипыФормы типФормы, List<тчАвансоваяОплата> табЧасть)
        {
            this.Общие = new ОбщиеРеквизиты();
            this.Общие.ТипФормы = типФормы;
            this.ТабличнаяЧасть = табЧасть ?? new List<тчАвансоваяОплата>();
        }
    }
    public class тчАвансоваяОплата
    {
        public Работа Работа { get; set; }
        public decimal Количество { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
    }
}
