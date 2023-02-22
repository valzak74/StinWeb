using StinClasses.Справочники;
using StinClasses.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace StinClasses.Документы
{
    public class ФормаСчетФактура
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Контрагент Контрагент { get; set; }
        public Договор Договор { get; set; }
        public bool УчитыватьНДС { get; set; }
        public bool СуммаВклНДС { get; set; }
        public ФормаСчетФактура()
        {
            Общие = new ОбщиеРеквизиты();
        }
    }
    public interface IСчетФактура : IДокумент
    {
        Task<ФормаСчетФактура> ВводНаОснованииAsync(ФормаРеализация докОснование, DateTime docDateTime);
        Task<ФормаСчетФактура> ВводНаОснованииAsync(ФормаОтчетКомиссионера докОснование, DateTime docDateTime);
        Task<ExceptionData> ЗаписатьAsync(ФормаСчетФактура doc);
        Task<ExceptionData> ПровестиAsync(ФормаСчетФактура doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаСчетФактура doc);
    }
    public class СчетФактура : Документ, IСчетФактура
    {
        public СчетФактура(StinDbContext context) : base(context)
        {
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    //_регистрНабор.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаСчетФактура> ВводНаОснованииAsync(ФормаРеализация докОснование, DateTime docDateTime)
        {
            ФормаСчетФактура doc = new ФормаСчетФактура();

            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = (int)StinClasses.Документы.ВидДокумента.СчетФактура; //2051
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;

            doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;

            doc.Общие.Комментарий = string.IsNullOrEmpty(докОснование.Общие.Комментарий) ? "" : докОснование.Общие.Комментарий.Trim();

            doc.Контрагент = докОснование.Контрагент;
            doc.Договор = докОснование.Договор;
            doc.УчитыватьНДС = докОснование.УчитыватьНДС;
            doc.СуммаВклНДС = докОснование.СуммаВклНДС;

            return doc;
        }
        public async Task<ФормаСчетФактура> ВводНаОснованииAsync(ФормаОтчетКомиссионера докОснование, DateTime docDateTime)
        {
            ФормаСчетФактура doc = new ФормаСчетФактура();

            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = (int)StinClasses.Документы.ВидДокумента.СчетФактура; //2051
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;

            doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;

            doc.Общие.Комментарий = string.IsNullOrEmpty(докОснование.Общие.Комментарий) ? "" : докОснование.Общие.Комментарий.Trim();

            doc.Контрагент = докОснование.Контрагент;
            doc.Договор = докОснование.Договор;
            doc.УчитыватьНДС = докОснование.УчитыватьНДС;
            doc.СуммаВклНДС = докОснование.СуммаВклНДС;

            return doc;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаСчетФактура doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(0, 0, 4601, doc.Общие.ВидДокумента10, Common.НумераторСчФактуры, "СчетФактураВыданный",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    "",
                    doc.Контрагент.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh2051 docHeader = new Dh2051
                {
                    Iddoc = j.Iddoc,
                    Sp2034 = doc.Контрагент.Id,
                    Sp2035 = doc.Договор.Id,
                    Sp2038 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp4394 = 1, //АвтоКнигаПродаж
                    Sp4395 = 0, //СФНаАванс
                    Sp4396 = doc.УчитыватьНДС ? 1 : 0,
                    Sp4397 = doc.СуммаВклНДС ? 1 : 0,
                    Sp4398 = 0, //УчитыватьНП
                    Sp4399 = 0, //СуммаВклНП
                    Sp4400 = "", //НомерПлатРасчДок
                    Sp4401 = Common.min1cDate, //ДатаПлатРасчДок
                    Sp2036 = Common.ВалютаРубль,
                    Sp2037 = 1, //Курс,
                    Sp7847 = 0, //флагСвертки
                    Sp2043 = 0, //Сумма
                    Sp2044 = 0, //СуммаНДС
                    Sp2045 = 0, //СуммаНП
                    Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий,
                };
                await _context.Dh2051s.AddAsync(docHeader);

                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await ОбновитьTotals(doc.Общие.ВидДокумента10, j.Iddoc);
                if (doc.Общие.ДокОснование != null)
                    await ОбновитьПодчиненныеДокументы(doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
                //контрагент
                await ОбновитьГрафыОтбора(862, Common.Encode36(172).PadLeft(4) + doc.Контрагент.Id, j.DateTimeIddoc, j.Iddoc);
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = db_ex.HResult, Description = db_ex.InnerException.ToString() };
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = ex.HResult, Description = ex.Message };
            }
            return null;
        }
        public async Task<ExceptionData> ПровестиAsync(ФормаСчетФактура doc)
        {
            try
            {
                _1sjourn j = await _context._1sjourns.FirstOrDefaultAsync(x => x.Iddoc == doc.Общие.IdDoc);
                if (j == null)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = "Не обнаружена запись журнала." };
                }
                int КоличествоДвижений = j.Actcnt;

                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Ds1946 = 2;

                _context.Update(j);
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = db_ex.HResult, Description = db_ex.InnerException.ToString() };
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = ex.HResult, Description = ex.Message };
            }
            return null;
        }
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаСчетФактура doc)
        {
            var result = await ЗаписатьAsync(doc);
            if (result == null)
            {
                result = await ПровестиAsync(doc);
            }
            return result;
        }
    }
}
