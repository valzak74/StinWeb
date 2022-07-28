using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace StinWeb.Models.DataManager.Справочники
{
    public class Касса
    {
        [Key]
        [Required(ErrorMessage = "Укажите кассу")]
        public string Id { get; set; }
        public string Наименование { get; set; }
    }
}
