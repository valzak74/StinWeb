using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Справочники.Мастерская;

namespace StinWeb.Models.DataManager.Документы.Мастерская
{
    public class ФормаИзменениеСтатуса
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public ИнформацияИзделия ДанныеКвитанции { get; set; }
        public string СтатусНовыйId { get; set; }
        public string СтатусНовый
        {
            get
            {
                return Common.СтатусПартии.Where(x => x.Key == this.СтатусНовыйId).Select(y => y.Value).FirstOrDefault();
            }
        }
        public ФормаИзменениеСтатуса()
        {
            this.Общие = new ОбщиеРеквизиты();
            this.ДанныеКвитанции = new ИнформацияИзделия();
        }
        public ФормаИзменениеСтатуса(ТипыФормы типФормы)
        {
            this.Общие = new ОбщиеРеквизиты();
            this.ДанныеКвитанции = new ИнформацияИзделия();
            this.Общие.ТипФормы = типФормы;
        }
    }
}
