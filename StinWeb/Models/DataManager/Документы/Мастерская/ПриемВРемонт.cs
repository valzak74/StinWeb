using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.DataManager.Документы
{
    public class ПриемВРемонт 
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите номер документа")]
        public string НомерДок { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите номер квитанции")]
        public string КвитанцияId { get; set; }

        [Required(ErrorMessage = "Укажите Изделие")]
        public Номенклатура Изделие { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите Заводской номер")]
        public string ЗаводскойНомер { get; set; }

        [Required]
        public int ТипРемонта { get; set; }
        public DateTime ДатаПродажи { get; set; }
        public int НомерРемонта { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите Комплектность")]
        public string Комплектность { get; set; }

        [Required(ErrorMessage = "Укажите Неисправность")]
        public Неисправность Неисправность { get; set; }
        public НеисправностьДоп Неисправность2 { get; set; }
        public НеисправностьДоп Неисправность3 { get; set; }
        public НеисправностьДоп Неисправность4 { get; set; }
        public НеисправностьДоп Неисправность5 { get; set; }

        [Required(ErrorMessage = "Укажите Заказчика")]
        public Контрагент Заказчик { get; set; }

        public string Телефон { get; set; }

        public string Email { get; set; }

        [Required(ErrorMessage = "Укажите Склад")]
        public Склад Склад { get; set; }

        [Required(ErrorMessage = "Укажите Место хранения")]
        public ПодСклад ПодСклад { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите Мастера")]
        public string Мастер { get; set; }
        public string Комментарий { get; set; }
        public List<IFormFile> Photos { get; set; }
    }
    public class ПриемВРемонтПечать
    {
        public string ЮрЛицо { get; set; }
        public string ТелефонСервиса { get; set; }
        public string НомерКвитанции { get; set; }
        public int ДатаКвитанции { get; set; }
        public string КвитанцияId { get; set; }
        public string НомерДок { get; set; }
        public string ДатаДок { get; set; }
        public string Комментарий { get; set; }
        public string Заказчик { get; set; }
        public string ЗаказчикФИО { get; set; }
        public string ЗаказчикАдрес { get; set; }
        public string ЗаказчикТелефон { get; set; }
        public string ЗаказчикEmail { get; set; }
        public string Мастер { get; set; }
        public string ДатаПродажи { get; set; }
        public string Изделие { get; set; }
        public string Артикул { get; set; }
        public string ЗаводскойНомер { get; set; }
        public int НомерРемонта { get; set; }
        public string Производитель { get; set; }
        public string Неисправность { get; set; }
        public string Неисправность2 { get; set; }
        public string Неисправность3 { get; set; }
        public string Неисправность4 { get; set; }
        public string Неисправность5 { get; set; }
        public string Комплектность { get; set; }
        public string Склад { get; set; }
        public string МестоХранения { get; set; }
        public string ДатаОбращения { get; set; }
        public string ПриложенныйДокумент1 { get; set; }
        public string ПриложенныйДокумент2 { get; set; }
        public string ПриложенныйДокумент3 { get; set; }
        public string ПриложенныйДокумент4 { get; set; }
        public string ПриложенныйДокумент5 { get; set; }
        public string ВнешнийВид { get; set; }
        public string СпособВозвращения { get; set; }
    }
}
