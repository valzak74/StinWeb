using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StinWeb.Models.DataManager.Справочники
{
    public class Склад
    {
        public int RowId { get; set; }
        [Key]
        [Required(ErrorMessage = "Укажите Склад")]
        public string Id { get; set; }
        public string Code { get; set; }
        public string Наименование { get; set; }
    }

    public class ПодСклад
    {
        public int RowId { get; set; }
        [Key]
        [Required(ErrorMessage = "Укажите Место хранения")]
        public string Id { get; set; }
        public string Наименование { get; set; }
    }
}
