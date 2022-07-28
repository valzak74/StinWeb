using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.DataManager.Документы
{
    public class ОбщиеРеквизиты
    {
        public string Родитель { get; set; }
        public ТипыФормы ТипФормы { get; set; }
        public string IdDoc { get; set; }
        public ДокОснование ДокОснование { get; set; }
        public bool Удален { get; set; }
        public bool Проведен { get; set; }
        public int ВидДокумента10 { get; set; }
        public string ВидДокумента36 { get; set; }
        public string Наименование
        {
            get
            {
                return Common.ВидыДокументов.Where(x => x.Key == this.ВидДокумента10).Select(y => y.Value).FirstOrDefault();
            }
        }
        private string _названиеВЖурнале;
        public string НазваниеВЖурнале 
        { 
            get => _названиеВЖурнале; 
            set => _названиеВЖурнале = string.IsNullOrEmpty(value) ? this.Наименование : string.Format(value, this.Наименование); 
        }
        public string Информация { get; set; }
        public string НомерДок { get; set; }
        public DateTime ДатаДок { get; set; }
        public Фирма Фирма { get; set; }
        public User Автор { get; set; }
        public string Комментарий { get; set; }
    }
    public class ДокОснование
    {
        public string IdDoc { get; set; }
        public int ВидДокумента10 { get; set; }
        public string Значение
        {
            get { return Common.Encode36(this.ВидДокумента10).PadLeft(4) + this.IdDoc; }
        }
        public string Наименование
        {
            get
            {
                return Common.ВидыДокументов.Where(x => x.Key == this.ВидДокумента10).Select(y => y.Value).FirstOrDefault();
            }
        }
        public string НомерДок { get; set; }
        public DateTime ДатаДок { get; set; }
        public bool Проведен { get; set; }
        public Фирма Фирма { get; set; }
        public User Автор { get; set; }
    }
    public enum ТипыФормы : short
    {
        [Display(Name = "форма для просмотра")]
        Просмотр = 0,
        [Display(Name = "форма нового документа")]
        Новый = 1,
        [Display(Name = "форма документа, введенного на основании")]
        НаОсновании = 2
    }
}
