using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Models.DataManager.Справочники
{
    public class Работа
    {
        public string Id { get; set; }
        public string id { get; set; }
        public string parent { get; set; }
        public bool children { get; set; }
        public string Parent { get; set; }
        public string Наименование { get; set; }
        public string Артикул { get; set; }
        public string АртикулОригинал { get; set; }
        public decimal Цена { get; set; }
        public decimal ЦенаПоставщика { get; set; }
        public string text { get; set; }
        public string icon { get; set; }
        public string state { get; set; }
        public bool opened { get; set; }
        public bool disabled { get; set; }
        public bool selected { get; set; }
        public string li_attr { get; set; }
        public string a_attr { get; set; }
    }
}
