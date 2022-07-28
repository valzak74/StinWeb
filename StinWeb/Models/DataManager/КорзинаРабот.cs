using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.DataManager
{
    public class КорзинаРабот
    {
        public Работа Работа { get; set; }
        public decimal Quantity { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
    }
    public class КорзинаРаботПечать
    {
        public string Работа { get; set; }
        public string Единица { get; set; }
        public string КолВо { get; set; }
        public string Цена { get; set; }
        public string Сумма { get; set; }
    }
}
