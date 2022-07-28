using NonFactors.Mvc.Lookup;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Models.DataManager.Справочники
{
    public class Фирма
    {
        [Key]
        [Required(ErrorMessage = "Укажите фирму")]
        public string Id { get; set; }
        [LookupColumn]
        public string Наименование { get; set; }
        public ЮрЛицо ЮрЛицо { get; set; }
        public БанковскийСчет Счет { get; set; }
    }
    public class ЮрЛицо
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public string ИНН { get; set; }
        public string Префикс { get; set; }
        public decimal УчитыватьНДС { get; set; }
        public string Адрес { get; set; }
    }
    public class БанковскийСчет
    {
        public string Id { get; set; }
        public string РасчетныйСчет { get; set; }
        public Банк Банк { get; set; }
    }
    public class Банк
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public string КоррСчет { get; set; }
        public string БИК { get; set; }
        public string Город { get; set; }
    }
}
