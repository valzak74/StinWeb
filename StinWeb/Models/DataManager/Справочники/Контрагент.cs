using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NonFactors.Mvc.Lookup;
using System.ComponentModel.DataAnnotations;

namespace StinWeb.Models.DataManager.Справочники
{
    public class Контрагент
    {
        [Key]
        [Required(ErrorMessage = "Укажите Контрагента")]
        public string Id { get; set; }
        public string Code { get; set; }
        [LookupColumn]
        [Display(Name = "Наименование")]
        public string Наименование { get; set; }
        [LookupColumn]
        [Display(Name = "ИНН")]
        public string ИНН { get; set; }
        public string ОсновнойДоговор { get; set; }
        public string ГруппаКонтрагентов { get; set; }
        public string ЮридическийАдрес { get; set; }
        public string ФактическийАдрес { get; set; }
    }
    public class Договор
    {
        public string Id { get; set; }
        public string Владелец { get; set; }
        public string Наименование { get; set; }
    }
    public class ТипЦен
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
    }
    public class Скидка
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public decimal Процент { get; set; }
    }
    public class СкидКарта
    {
        [Key]
        public string Id { get; set; }
        [LookupColumn]
        [Display(Name = "Наименование")]
        public string Наименование { get; set; }
        [LookupColumn]
        [Display(Name = "ФИО")]
        public string ФИО { get; set; }
    }
    public class УсловияДоговора
    {
        public string КонтрагентId { get; set; }
        public string ГрКонтрагентовId { get; set; }
        public string ТипЦенId { get; set; }
        public string ТипЦен { get; set; }
        public decimal ПроцентНаценкиТипаЦен { get; set; }
        public bool Экспорт { get; set; }
        public string КолонкаСкидкиId { get; set; }
        public decimal СкидкаОтсрочка { get; set; }
    }
    public class УсловияДисконтКарты
    {
        public string Наименование { get; set; }
        public string ФИО { get; set; }
        public bool Корпоративная { get; set; }
        public bool Закрыта { get; set; }
        public string ТипЦен { get; set; }
        public decimal ПроцентСкидки { get; set; }
        public decimal Накоплено { get; set; }
        public decimal СледующийПредел { get; set; }
        public decimal СледующаяСкидка { get; set; }
        public IQueryable<УсловияБрендов> УсловияБрендов { get; set; }
    }
    public class УсловияБрендов
    {
        public string БрендId { get; set; }
        public string БрендНаименование { get; set; }
        public decimal БазаБренда { get; set; }
        public decimal СкидкаВсем { get; set; }
        public decimal ДопУсловияПроцент { get; set; }
        public bool БеспОтсрочка { get; set; }
        public bool БеспДоставка { get; set; }
        public decimal КолонкаСкидок { get; set; }
    }
    public class ИнфоУсловия
    {
        public string ТипЦен { get; set; }
        public УсловияДисконтКарты ДисконтнаяКарта { get; set; }
        public decimal ПроцентСкидкиЗаОтсрочку { get; set; }
        public decimal ПроцентСкидкиЗаДоставку { get; set; }
        public List<ИнфоУсловияБренда> УсловияБрендов { get; set; }
        public bool Экспорт { get; set; }
        public ИнфоУсловия()
        {
            ТипЦен = "Розничные";
            ПроцентСкидкиЗаОтсрочку = 0;
            ПроцентСкидкиЗаДоставку = 0;
            Экспорт = false;
            УсловияБрендов = new List<ИнфоУсловияБренда>();
        }
    }
    public class ИнфоУсловияБренда
    {
        public string Наименование { get; set; }
        public decimal ПроцентСкидки { get; set; }
        public bool БеспОтсрочка { get; set; }
        public bool БеспДоставка { get; set; }
    }
    public class Менеджер
    {
        [Key]
        [Required(ErrorMessage = "Укажите менеджера")]
        public string Id { get; set; }
        [LookupColumn]
        [Display(Name = "Наименование")]
        public string Наименование { get; set; }
    }
    public class ГруппаКонтрагентов
    {
        [Key]
        [Required(ErrorMessage = "Укажите группу контрагентов")]
        public string Id { get; set; }
        [LookupColumn]
        [Display(Name = "Наименование")]
        public string Наименование { get; set; }
    }
    public class ДокументДолга
    {
        public string IdDoc { get; set; }
        public string DocНазвание { get; set; }
        public string DocNo { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime ДатаОплаты { get; set; }
        public double ОтсрочкаДней { get; set; }
        public decimal СуммаДокумента { get; set; }
        public decimal СуммаТекущегоДолга { get; set; }
        public decimal СуммаПросроченногоДолга { get; set; }
    }
    public class ДолгиКонтрагента
    {
        public Контрагент Контрагент { get; set; }
        public decimal Долг { get; set; }
        public decimal ДолгПокупателя { get; set; }
        public decimal ДолгПередПоставщиком { get; set; }
    }
    public class Долги
    {
        public Менеджер Менеджер { get; set; }
        public ГруппаКонтрагентов Группа { get; set; }
        public Контрагент Контрагент { get; set; }
        public decimal Долг { get; set; }
        //public decimal ТекущийДолг { get; set; }
        //public decimal ПросроченныйДолг { get; set; }
        public decimal ДолгПокупателя { get; set; }
        public decimal ПокупателиТекущийДолг { get; set; }
        public decimal ПокупателиПросроченныйДолг { get; set; }
        public decimal ДолгПередПоставщиком { get; set; }
        public decimal ПоставщикиТекущийДолг { get; set; }
        public decimal ПоставщикиПросроченныйДолг { get; set; }
        //public IEnumerable<ДокументДолга> Документы { get; set; }
        public IEnumerable<ДокументДолга> ДокументыРеализации { get; set; }
        public IEnumerable<ДокументДолга> ДокументыПоступления { get; set; }
        public Долги()
        {
            //this.Документы = new List<ДокументДолга>();
            this.ДокументыРеализации = new List<ДокументДолга>();
            this.ДокументыПоступления = new List<ДокументДолга>();
        }
    }
    public class ДолгиТаблица
    {
        public int Флаг { get; set; }
        public int Count { get; set; }
        public string Наименование { get; set; }
        public string Менеджер { get; set; }
        public string Группа { get; set; }
        public string Контрагент { get; set; }
        public decimal Долг { get; set; }
        //public decimal ТекущийДолг { get; set; }
        //public decimal ПросроченныйДолг { get; set; }
        public decimal Покупатели_Долг { get; set; }
        public decimal Покупатели_ТекущийДолг { get; set; }
        public decimal Покупатели_ПросроченныйДолг { get; set; }
        public decimal Поставщики_Долг { get; set; }
        public decimal Поставщики_ТекущийДолг { get; set; }
        public decimal Поставщики_ПросроченныйДолг { get; set; }
        public IEnumerable<ДокументДолга> ДокументыРеализации { get; set; }
        public IEnumerable<ДокументДолга> ДокументыПоступления { get; set; }
    }
    public class ОтчетПоДолгам
    {
        public bool СортировкаПокупателиПоставщики { get; set; }
        public IOrderedEnumerable<ДолгиТаблица> Результаты { get; set; }
    }
}
