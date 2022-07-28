using StinWeb.Models.DataManager.Справочники;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StinWeb.Models.DataManager.Документы
{
    public class ИнтернетЗаказ
    {
        public string DocId { get; set; }
        public string НомерДок { get; set; }
        public DateTime ДатаДок { get; set; }
        public User Пользователь { get; set; }
        public string Комментарий { get; set; }
        [Required(ErrorMessage = "Укажите Фирму")]
        public Фирма Фирма { get; set; }
        [Required(ErrorMessage = "Укажите Склад")]
        public Склад Склад { get; set; }
        [Required(ErrorMessage = "Укажите Контрагента")]
        public Контрагент Контрагент { get; set; }
        [Required(ErrorMessage = "Укажите Договор")]
        public Договор Договор { get; set; }
        public СкидКарта СкидКарта { get; set; }
        [Required(ErrorMessage = "Укажите Дату отгрузки")]
        public DateTime ДатаОтгрузки { get; set; }
        [Required(ErrorMessage = "Укажите Дату оплаты")]
        public DateTime ДатаОплаты { get; set; }
        public bool Доставка { get; set; }
        public Маршрут НомерМаршрута { get; set; }
        public string АдресДоставки { get; set; }
        public ТипЦен ТипЦен { get; set; }
        public Скидка Скидка { get; set; }
        public ТипыДоставки ТипДоставки { get; set; }
        public ТипыСамовывоза ТипСамовывоза { get; set; }
        public ТипыОплаты ТипОплаты { get; set; }
        public ТипыКоммуникации ТипКоммуникации { get; set; }
        public string CпособОтгрузки { get 
            {
                if (Доставка)
                {
                    switch (ТипДоставки)
                    {
                        case ТипыДоставки.Самара:
                            return "Доставка";
                        case ТипыДоставки.СамарскаяОбл:
                            return "Межгород доставка";
                        case ТипыДоставки.ТранспортнаяКомпания:
                            return "Дальняя доставка";
                        default:
                            return "Самовывоз";
                    }
                }
                else
                    return "Самовывоз";
            } 
        }
        private void SetDefaults()
        {
            this.ДатаДок = DateTime.Now;
            this.ДатаОтгрузки = DateTime.Now;
            this.ДатаОплаты = DateTime.Now;
            this.Доставка = false;
            this.ТипДоставки = ТипыДоставки.Самара;
            this.ТипСамовывоза = ТипыСамовывоза.Экран;
            this.ТипОплаты = ТипыОплаты.Наличными;
            this.ТипКоммуникации = ТипыКоммуникации.Телефон;
        }
        public ИнтернетЗаказ()
        {
            SetDefaults();
        }
        public ИнтернетЗаказ(User пользователь, Фирма фирма, Контрагент контрагент, Склад склад)
        {
            SetDefaults();
            //this.НомерДок = номерДок;
            this.Пользователь = пользователь;
            this.Фирма = фирма;
            this.Контрагент = контрагент;
            this.Склад = склад;
        }
    }
    public enum ТипыДоставки : short
    {
        [Display(Name = "по Самаре")]
        Самара = 0,
        [Display(Name = "Межгород")]
        СамарскаяОбл = 1,
        [Display(Name = "Транспортной компанией")]
        ТранспортнаяКомпания = 2
    }
    public enum ТипыСамовывоза : short
    {
        [Display(Name = "Магазин \"Дом и Сад\"")]
        ДомИСад = 0,
        [Display(Name = "Склад \"Экран\"")]
        Экран = 1
    }
    public enum ТипыОплаты : short
    {
        Наличными = 0,
        Картой = 1,
        НаложенныйПлатеж = 2,
        ПредоплатаНаличными = 3,
        ПредоплатаКартой = 4,
        ПредоплатаСчет = 5
    }
    public enum ТипыКоммуникации : short
    {
        Телефон = 0,
        Почта = 1,
        Сайт = 2,
        Прочее = 3
    }
    public class мнСтрока
    {
        public int НомерСтроки { get; set; }
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
    }
    public class мнСвободнаяСтрока
    {
        public int НомерСтроки { get; set; }
        public string Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
    }
}
