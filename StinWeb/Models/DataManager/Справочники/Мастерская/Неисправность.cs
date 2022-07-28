using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NonFactors.Mvc.Lookup;
using System.ComponentModel.DataAnnotations;

namespace StinWeb.Models.DataManager.Справочники
{
    public class Неисправность
    {
        [Key]
        //[Required(ErrorMessage = "Укажите Неисправность")]
        public string Id { get; set; }
        [LookupColumn]
        public string Наименование { get; set; }
    }
    public class НеисправностьДоп
    {
        [Key]
        public string Id { get; set; }
        [LookupColumn]
        public string Наименование { get; set; }
    }
    public class ПриложенныйДокумент
    {
        [Key]
        public string Id { get; set; }
        [LookupColumn]
        public string Наименование { get; set; }
        public decimal ФлагГарантии { get; set; }
    }
}
