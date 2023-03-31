using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using StinClasses.Регистры;
using StinClasses.Справочники;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Документы
{
    public class ФормаСогласие
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Контрагент Контрагент { get; set; }
        public List<ФормаСогласиеТЧ> ТабличнаяЧасть { get; set; }
        public List<ФормаСогласиеТЧР> ТабличнаяЧастьРаботы { get; set; }
        public ФормаСогласие()
        {
            Общие = new ОбщиеРеквизиты(ВидДокумента.Согласие);
            ТабличнаяЧасть = new List<ФормаСогласиеТЧ>();
            ТабличнаяЧастьРаботы = new List<ФормаСогласиеТЧР>();
        }
        //public ФормаСогласие(StinDbContext context, string idDoc) :this(context)
        //{
        //    var d = (from header in context.Dh11101s
        //             join journ in context._1sjourns on header.Iddoc equals journ.Iddoc
        //             where header.Iddoc == idDoc
        //             select new
        //             {
        //                 header,
        //                 journ
        //             }).SingleOrDefault();
        //    if (d == null)
        //        throw new ArgumentNullException("header");
        //    if (d.journ.Iddocdef != Общие.ВидДокумента10)
        //        throw new Exception("Wrong document type for idDoc = '" + idDoc + "'");
        //    GetCommonProperties(d.journ, d.header.Sp11087, d.header.Sp660);
        //    IКонтрагент контрагент = new КонтрагентEntity(context);
        //    Контрагент = контрагент.GetКонтрагентById(d.header.Sp11075);
        //    using IНоменклатура номенклатура = new НоменклатураEntity(context);
        //    var ТаблЧасть = context.Dt11101s
        //        .Where(x => x.Iddoc == idDoc)
        //        .ToList();
        //    foreach (var row in ТаблЧасть.Where(x => x.Sp11090 != Common.ПустоеЗначение))
        //    {
        //        ТабличнаяЧасть.Add(new ФормаСогласиеТЧ
        //        {
        //            Номенклатура = номенклатура.GetНоменклатураById(row.Sp11090),
        //            Количество = row.Sp11092,
        //            Единица = номенклатура.GetЕдиницаById(row.Sp11091),
        //            Цена = row.Sp11093,
        //            Сумма = row.Sp11097,
        //        });
        //    }
        //    foreach (var row in ТаблЧасть.Where(x => x.Sp11095 != Common.ПустоеЗначение))
        //    {
        //        ТабличнаяЧастьРаботы.Add(new ФормаСогласиеТЧР
        //        {
        //            Работа = new Работа(context, row.Sp11095),
        //            Количество = row.Sp11098,
        //            Цена = row.Sp11099,
        //            Сумма = row.Sp11097,
        //        });
        //    }
        //}
    }
    public class ФормаСогласиеТЧ
    {
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public Единица Единица { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
    }
    public class ФормаСогласиеТЧР
    {
        public Работа Работа { get; set; }
        public decimal Количество { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
    }
    public interface IСогласие : IДокумент
    {
        Task<ФормаСогласие> GetФормаСогласиеAsync(string докId);
    }
    public class Согласие : Документ, IСогласие
    {
        IРабота _работа;
        public Согласие(StinDbContext context) : base(context)
        {
            _работа = new РаботаEntity(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _работа.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаСогласие> GetФормаСогласиеAsync(string докId)
        {
            var d = (from header in _context.Dh11101s
                     join journ in _context._1sjourns on header.Iddoc equals journ.Iddoc
                     where header.Iddoc == докId
                     select new
                     {
                         header,
                         journ
                     }).SingleOrDefault();
            if (d == null)
                throw new ArgumentNullException("header");
            var doc = new ФормаСогласие();
            if (d.journ.Iddocdef != doc.Общие.ВидДокумента10)
                throw new Exception("Wrong document type for idDoc = '" + докId + "'");
            await SetCommonProperties(doc.Общие, d.journ, d.header.Sp11087, d.header.Sp660);
            doc.Контрагент = await _контрагент.GetКонтрагентAsync(d.header.Sp11075);
            var ТаблЧасть = _context.Dt11101s
                .Where(x => x.Iddoc == докId)
                .ToList();
            foreach (var row in ТаблЧасть.Where(x => x.Sp11090 != Common.ПустоеЗначение))
            {
                doc.ТабличнаяЧасть.Add(new ФормаСогласиеТЧ
                {
                    Номенклатура = await _номенклатура.GetНоменклатураByIdAsync(row.Sp11090),
                    Количество = row.Sp11092,
                    Единица = await _номенклатура.GetЕдиницаByIdAsync(row.Sp11091),
                    Цена = row.Sp11093,
                    Сумма = row.Sp11097,
                });
            }
            foreach (var row in ТаблЧасть.Where(x => x.Sp11095 != Common.ПустоеЗначение))
            {
                doc.ТабличнаяЧастьРаботы.Add(new ФормаСогласиеТЧР
                {
                    Работа = await _работа.GetРаботаByIdAsync(row.Sp11095),
                    Количество = row.Sp11098,
                    Цена = row.Sp11099,
                    Сумма = row.Sp11097,
                });
            }
            return doc;
        }
    }
}
