using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.DataManager
{
    public class ДанныеКорзины
    {
        public string Key { get; set; }
        public bool modalVersion { get; set; }
        public bool isReadOnly { get; set; }
        public bool ShowАртикул { get; set; }
        public bool ShowПроизводитель { get; set; }
        public bool ShowЕдиницы { get; set; }
        public bool ShowЦены { get; set; }
        public List<Корзина> Данные { get; set; }
        public ДанныеКорзины()
        {
            this.modalVersion = false;
            this.ShowАртикул = false;
            this.ShowПроизводитель = false;
            this.ShowЕдиницы = false;
            this.ShowЦены = true;
            this.Данные = new List<Корзина>();
        }
        public ДанныеКорзины(List<Корзина> данные)
        {
            this.modalVersion = false;
            this.ShowАртикул = false;
            this.ShowПроизводитель = false;
            this.ShowЕдиницы = false;
            this.ShowЦены = true;
            this.Данные = данные ?? new List<Корзина>();
        }
    }
    public class Корзина
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public string Артикул { get; set; }
        public string Производитель { get; set; }
        public string ЕдиницаId { get; set; }
        public decimal ЕдиницаКоэффициент { get; set; }
        public string ЕдиницаНаименование { get; set; }
        public decimal Quantity { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get { return this.Quantity * this.Цена; } }
    }
    public class ДанныеТабличнойЧасти
    {
        public Фирма Фирма { get; set; }
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
    }
}
