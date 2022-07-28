using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Models.DataManager
{
    public class ПартииМастерской
    {
        public string Квитанция { get; set; }
        public string КвитанцияНомер { get; set; }
        public decimal КвитанцияГод { get; set; }
        public decimal Гарантия { get; set; } 
        public int AttentionId { get; set; }
        public string ТипРемонта { get; set; }
        public string Заказчик { get; set; }
        public string ИзделиеId { get; set; }
        public string Изделие { get; set; }
        public string Артикул { get; set; }
        public string Производитель { get; set; }
    }
}
