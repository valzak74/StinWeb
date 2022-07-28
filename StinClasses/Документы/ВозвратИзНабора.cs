using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinClasses.Models;
using StinClasses.Справочники;

namespace StinClasses.Документы
{
    public class ФормаВозвратИзНабора
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Склад Склад { get; set; }
        public ПодСклад ПодСклад { get; set; }
        public bool Завершен { get; set; }
        public Кладовщик Кладовщик { get; set; }
        public Order Order { get; set; }
        public List<ФормаВозвратИзНабораТЧ> ТабличнаяЧасть { get; set; }
        public ФормаВозвратИзНабора()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаВозвратИзНабораТЧ>();
        }
    }
    public class ФормаВозвратИзНабораТЧ
    {
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public Единица Единица { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
        public string Ячейки { get; set; }
    }
    public interface IВозвратИзНабора : IДокумент
    {
        Task<ФормаВозвратИзНабора> GetФормаВозвратИзНабораById(string idDoc);
    }
    public class ВозвратИзНабора : Документ, IВозвратИзНабора
    {
        public ВозвратИзНабора(StinDbContext context) : base(context)
        {
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаВозвратИзНабора> GetФормаВозвратИзНабораById(string idDoc)
        {
            var d = await (from dh in _context.Dh11961s
                           join j in _context._1sjourns on dh.Iddoc equals j.Iddoc
                           where dh.Iddoc == idDoc
                           select new
                           {
                               dh,
                               j
                           }).FirstOrDefaultAsync();
            var doc = new ФормаВозвратИзНабора
            {
                Общие = new ОбщиеРеквизиты
                {
                    IdDoc = d.dh.Iddoc,
                    ДокОснование = !(string.IsNullOrWhiteSpace(d.dh.Sp11949) || d.dh.Sp11949 == Common.ПустоеЗначениеИд13) ? await ДокОснованиеAsync(d.dh.Sp11949.Substring(4)) : null,
                    Фирма = await _фирма.GetEntityByIdAsync(d.j.Sp4056),
                    Автор = await _пользователь.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НазваниеВЖурнале = ((ВидДокумента)d.j.Iddocdef).ToString(),
                    НомерДок = d.j.Docno,
                    ДатаДок = d.j.DateTimeIddoc.ToDateTime(),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh.Sp660,
                    Удален = d.j.Ismark
                },
                Склад = await _склад.GetEntityByIdAsync(d.dh.Sp11950),
                ПодСклад = await _склад.GetПодСкладByIdAsync(d.dh.Sp11953),
                Завершен = d.dh.Sp13509 == 1,
                Кладовщик = await _кладовщик.GetКладовщикByIdAsync(d.dh.Sp13510),
            };
            if (doc.Общие.ДокОснование != null && doc.Общие.ДокОснование.ВидДокумента10 == (int)ВидДокумента.Набор)
                doc.Order = await ПолучитьOrderНабора(doc.Общие.ДокОснование.IdDoc);
            var ТаблЧасть = await _context.Dt11961s
                .Where(x => x.Iddoc == idDoc)
                .ToListAsync();
            foreach (var row in ТаблЧасть)
            {
                doc.ТабличнаяЧасть.Add(new ФормаВозвратИзНабораТЧ
                {
                    Номенклатура = await _номенклатура.GetНоменклатураByIdAsync(row.Sp11954),
                    Количество = row.Sp11955,
                    Единица = await _номенклатура.GetЕдиницаByIdAsync(row.Sp11956),
                    Цена = row.Sp11959,
                    Сумма = row.Sp11958,
                    Ячейки = row.Sp12608
                });
            }
            return doc;
        }
    }
}
