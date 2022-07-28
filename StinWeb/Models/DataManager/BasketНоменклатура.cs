using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.DataManager
{
    public class BasketНоменклатура
    {
        public Номенклатура Номенклатура { get; set; }
        public decimal Quantity { get; set; }
    }
}
