using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Models.DataManager.Документы
{
    public class АктВыполненныхРаботПечать
    {
        public string ЮрЛицо { get; set; }
        public string ИНН { get; set; }
        public string Адрес { get; set; }
        public string РасчетныйСчет { get; set; }
        public string КоррСчет { get; set; }
        public string Банк { get; set; }
        public string БИК { get; set; }
        public string городБанка { get; set; }
        public string ТелефонСервиса { get; set; }
        public string НомерДок { get; set; }
        public string ДатаДок { get; set; }
        public string Изделие { get; set; }
        public string ЗаводскойНомер { get; set; }
        public string Производитель { get; set; }
        public string НомерКвитанции { get; set; }
        public List<КорзинаРаботПечать> ТаблЧасть { get; set; }
        public string ИтогоСумма { get; set; }
        public decimal КоличествоУслуг { get; set; }
        public string СуммаПрописью { get; set; }

        public АктВыполненныхРаботПечать()
        {
            ТаблЧасть = new List<КорзинаРаботПечать>();
        }
    }
}
