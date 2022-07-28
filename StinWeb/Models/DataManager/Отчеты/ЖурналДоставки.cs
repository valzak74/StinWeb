using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Документы;

namespace StinWeb.Models.DataManager.Отчеты
{
    public class ЖурналДоставки
    {
        public ОбщиеРеквизиты ОбщиеРеквизиты { get; set; }
        public decimal Остаток { get; set; }
    }
}
