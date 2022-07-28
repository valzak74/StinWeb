using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using NonFactors.Mvc.Lookup;
using NonFactors.Mvc.Grid;

namespace StinWeb.Models.DataManager.Справочники
{
    public class Номенклатура
    {
        [Key]
        [Required(ErrorMessage = "Укажите Номенклатуру")]
        [LookupColumn(Hidden = true)]
        public string Id { get; set; }
        public string ParentId { get; set; }
        public bool IsFolder { get; set; }
        public string Code { get; set; }
        [LookupColumn]
        public string Артикул { get; set; }
        [LookupColumn]
        public string Наименование { get; set; }
        public string ПроизводительId { get; set; }
        [LookupColumn]
        public string Производитель { get; set; }
        public Единицы Единица { get; set; }
        public СтатусыНоменклатуры Статус { get; set; }
        public Цены Цена { get; set; }
        public Регистры Регистр { get; set; }
    }
    public class Производитель
    {
        [Key]
        [Required(ErrorMessage = "Укажите Номенклатуру")]
        [LookupColumn(Hidden = true)]
        public string Id { get; set; }
        [LookupColumn]
        public string Наименование { get; set; }
    }
    public class Единицы
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public decimal Коэффициент { get; set; }
    }
    public class СтавкаНДС
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public decimal Процент { get; set; }
    }
    public class Цены
    {
        public decimal Закупочная { get; set; }
        public decimal Оптовая { get; set; }
        public decimal Розничная { get; set; }
        public decimal Особая { get; set; }
        public decimal ОптСП { get; set; }
        public decimal РозСП { get; set; }
        public decimal Клиента { get; set; }
        public decimal СП { get; set; }
        public decimal Себестоимость { get; set; }
        public decimal Порог { get; set; }

    }
    public class Регистры
    {
        public decimal Остаток { get; set; }
        public decimal ОстатокОтстой { get; set; }
        public decimal ОстатокВсего { get; set; }
        public decimal ОстатокАвСп { get; set; }
        public decimal Резерв { get; set; }
        public decimal РезервВсего { get; set; }
        public decimal ОжидаемыйПриход { get; set; }
    }
    public enum СтатусыНоменклатуры : short
    {
        Обычный = 0,
        ПодЗаказ = 1,
        СнятСПроизводства = 2
    }
    public class ДанныеПодбораНоменклатуры
    {
        public string Key { get; set; }
        public bool ShowАртикул { get; set; }
        public bool ShowПроизводитель { get; set; }
        public bool ShowЕдиницы { get; set; }
        public bool ShowЦены { get; set; }
        public IQueryable<Номенклатура> Данные { get; set; }
        public ДанныеПодбораНоменклатуры()
        {
            this.ShowАртикул = false;
            this.ShowПроизводитель = false;
            this.ShowЕдиницы = false;
            this.ShowЦены = true;
        }
        public ДанныеПодбораНоменклатуры(IQueryable<Номенклатура> данные)
        {
            this.ShowАртикул = false;
            this.ShowПроизводитель = false;
            this.ShowЕдиницы = false;
            this.ShowЦены = true;
            this.Данные = данные;
        }
    }
    public class ТаблицаСвободныхОстатков
    {
        public Фирма Фирма { get; set; }
        public Номенклатура Номенклатура { get; set; }
        public Склад Склад { get; set; }
        public Регистры Регистры { get; set; }
        public decimal СвободныйОстаток { get { return Math.Max((this.Регистры.Остаток - this.Регистры.Резерв - this.Регистры.ОстатокАвСп),0); } }
    }
}
