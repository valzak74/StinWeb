using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.DataManager.Отчеты
{
    public class ЗаказКлиента
    {
        public string НомерЗаказа { get; set; }
        public DateTime ДатаЗаказа { get; set; }
        public СчетЗаказа Корень { get; set; }
        public decimal СчетНаОплату { get; set; }
        public decimal ЗаявкаНаСогласование { get; set; }
        public decimal ЗаявкаСогласованная { get; set; }
        public decimal ЗаявкаОдобренная { get; set; }
        public bool ЗаявкаИсполненная { get; set; }
        public bool ОтменаЗаявки { get; set; }
        public decimal ОплатаОжидание { get; set; }
        public decimal ОплатаВыполнено { get; set; }
        public decimal ОплатаОтменено { get; set; }
        public decimal Набор { get; set; }
        public decimal ОтменаНабора { get; set; }
        public decimal Продажа { get; set; }
        public decimal Возврат { get; set; }
    }

    public class СчетЗаказа
    {
        public string IdDoc { get; set; }
        public string НомерДок { get; set; }
        public DateTime ДатаДок { get; set; }
        public Контрагент Контрагент { get; set; }
        public Менеджер Менеджер { get; set; }
        public decimal Сумма { get; set; }
    }

}
