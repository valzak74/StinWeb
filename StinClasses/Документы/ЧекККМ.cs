using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using StinClasses.Справочники;

namespace StinClasses.Документы
{
    public class ФормаЧекККМ
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Склад Склад { get; set; }
        public Касса Касса { get; set; }
        public string ВидОперации { get; set; }
        public string НомерЧекаККМ { get; set; }
        public bool ЧекПробит { get; set; }
        public string Email { get; set; }
        public string Sms { get; set; }
        public bool flagSms { get; set; }
        public bool flagEmail { get; set; }
        public bool flagElectron { get; set; }
        public decimal Сумма { get; set; }
        public List<ФормаЧекКкмТЧ> ТабличнаяЧасть { get; set; }
        public ФормаЧекККМ()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаЧекКкмТЧ>();
        }
    }
    public class ФормаЧекКкмТЧ
    {
        public string ТипОплаты { get; set; }
        public decimal СуммаОплаты { get; set; }
    }
    public static class ВидыОперацииЧекаККМ
    {
        public static readonly string Чек = "   3DG   ";
        public static readonly string ЧекНаВозврат = "   3DH   ";
    }
    public static class ТипыОплатыЧекаККМ
    {
        public static readonly string Наличными = "     1D  ";
        public static readonly string БанковскаяКарта = "     2D  ";
        public static readonly string Кредит = "     3D  ";
        public static readonly string ЗачетАванса = "     4D  ";
    }
    public interface IЧекККМ : IДокумент
    {
        Task<ФормаЧекККМ> ВводНаОснованииAsync(ФормаОтчетККМ докОснование, string видОперации, string paymentId, string email, string sms,
            bool flEmail, bool flSms, bool flElect, decimal сумма,
            List<ФормаЧекКкмТЧ> чекКкмТЧ);
        Task<ФормаЧекККМ> ВводНаОснованииAsync(ФормаПКО докОснование, DateTime docDateTime, string видОперации, string paymentId, string email, string sms,
                    bool flEmail, bool flSms, bool flElect, List<ФормаЧекКкмТЧ> чекКкмТЧ);
        Task<ExceptionData> ЗаписатьAsync(ФормаЧекККМ doc);
        Task<ExceptionData> ПровестиAsync(ФормаЧекККМ doc, bool групповоеПроведение = false);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаЧекККМ doc, bool групповоеПроведение = false);
    }
    public class ЧекККМ : Документ, IЧекККМ
    {
        public ЧекККМ(StinDbContext context) : base(context)
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
        public async Task<ФормаЧекККМ> ВводНаОснованииAsync(ФормаОтчетККМ докОснование, string видОперации, string paymentId, string email, string sms,
            bool flEmail, bool flSms, bool flElect, decimal сумма,
            List<ФормаЧекКкмТЧ> чекКкмТЧ)
        {
            ФормаЧекККМ doc = new ФормаЧекККМ();

            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = 3046;
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;

            doc.Общие.ДатаДок = DateTime.Now;

            doc.Общие.Комментарий = докОснование.Общие.Комментарий.Trim();
            doc.Склад = докОснование.Склад;
            doc.Касса = докОснование.Касса;
            doc.ВидОперации = видОперации;
            doc.НомерЧекаККМ = paymentId;
            doc.ЧекПробит = false;
            doc.Email = email;
            doc.Sms = sms;
            doc.flagEmail = flEmail;
            doc.flagSms = flSms;
            doc.flagElectron = flElect;
            doc.Сумма = сумма;
            doc.ТабличнаяЧасть.AddRange(чекКкмТЧ);

            if (doc.Ошибка == null || doc.Ошибка.Skip)
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, doc.Общие.ВидДокумента10.ToString(), 10, doc.Общие.Фирма.Id);
            return doc;
        }
        public async Task<ФормаЧекККМ> ВводНаОснованииAsync(ФормаПКО докОснование, DateTime docDateTime, string видОперации, string paymentId, string email, string sms,
            bool flEmail, bool flSms, bool flElect, List<ФормаЧекКкмТЧ> чекКкмТЧ)
        {
            ФормаЧекККМ doc = new ФормаЧекККМ();

            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = 3046;
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;

            doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;

            doc.Общие.Комментарий = докОснование.Общие.Комментарий.Trim();

            if (докОснование.Общие.ДокОснование.ВидДокумента10 == (int)ВидДокумента.ЗаявкаПокупателя) //Счет на оплату
            {
                var ДокСчет = await GetФормаЗаявкаById(докОснование.Общие.ДокОснование.IdDoc);
                if (ДокСчет != null)
                {
                    doc.Склад = ДокСчет.Склад;
                }
            }
            //doc.Склад = докОснование.Склад;

            doc.Касса = докОснование.Касса;
            doc.ВидОперации = видОперации;
            doc.НомерЧекаККМ = paymentId;
            doc.ЧекПробит = false;
            doc.Email = email;
            doc.Sms = sms;
            doc.flagEmail = flEmail;
            doc.flagSms = flSms;
            doc.flagElectron = flElect;
            doc.Сумма = чекКкмТЧ.Sum(x => x.СуммаОплаты);
            doc.ТабличнаяЧасть.AddRange(чекКкмТЧ);

            if (doc.Ошибка == null || doc.Ошибка.Skip)
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, doc.Общие.ВидДокумента10.ToString(), 10, doc.Общие.Фирма.Id);
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаЧекККМ doc)
        {
            try
            {
                await UnLockDocNoAsync(doc.Общие.ВидДокумента10.ToString(), doc.Общие.НомерДок);
                _1sjourn j = GetEntityJourn(0, 0, 4611, doc.Общие.ВидДокумента10, doc.Общие.ВидДокумента10.ToString(), doc.Общие.Наименование,
                    doc.Общие.НомерДок, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад != null ? doc.Склад.Наименование : "",
                    "");
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh3046 docHeader = new Dh3046
                {
                    Iddoc = j.Iddoc,
                    Sp4369 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp3029 = doc.Склад != null ? doc.Склад.Id : Common.ПустоеЗначение,
                    Sp3361 = doc.Касса != null ? doc.Касса.Id : Common.ПустоеЗначение,
                    Sp4368 = doc.ВидОперации,
                    Sp3036 = doc.НомерЧекаККМ,
                    Sp3037 = doc.ЧекПробит ? 1 : 0,
                    Sp13055 = doc.Сумма,
                    Sp13344 = string.IsNullOrEmpty(doc.Email) ? "" : doc.Email,
                    Sp13345 = string.IsNullOrEmpty(doc.Sms) ? "" : doc.Sms,
                    Sp13346 = doc.flagEmail ? 1 : 0,
                    Sp13347 = doc.flagSms ? 1 : 0,
                    Sp13395 = doc.flagElectron ? 1 : 0,
                    Sp660 = doc.Общие.Комментарий != null ? doc.Общие.Комментарий : ""
                };
                await _context.Dh3046s.AddAsync(docHeader);
                if (doc.ТабличнаяЧасть != null && doc.ТабличнаяЧасть.Count > 0)
                {
                    short lineNo = 1;
                    foreach (var строка in doc.ТабличнаяЧасть)
                    {
                        Dt3046 docRow = new Dt3046
                        {
                            Iddoc = j.Iddoc,
                            Lineno = lineNo++,
                            Sp13063 = string.IsNullOrEmpty(строка.ТипОплаты) ? Common.ПустоеЗначение : строка.ТипОплаты,
                            Sp13064 = строка.СуммаОплаты,
                        };
                        await _context.Dt3046s.AddAsync(docRow);
                    }
                }
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await ОбновитьTotals(doc.Общие.ВидДокумента10, j.Iddoc);
                if (doc.Общие.ДокОснование != null)
                    await ОбновитьПодчиненныеДокументы(doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
                //склад
                if (doc.Склад != null)
                    await ОбновитьГрафыОтбора(4747, Common.Encode36(55).PadLeft(4) + doc.Склад.Id, j.DateTimeIddoc, j.Iddoc);
                //контрагент
                //await ОбновитьГрафыОтбора(862, Common.Encode36(172).PadLeft(4) + doc.Контрагент.Id, j.DateTimeIddoc, j.Iddoc);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаЧекККМ doc, bool групповоеПроведение = false)
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
                j.Closed = 1;
                //j.Actcnt = КоличествоДвижений;
                //j.Ds1946 = 2;

                _context.Update(j);
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                if (!групповоеПроведение)
                {
                    await ОбновитьВремяТА(j.Iddoc, j.DateTimeIddoc);
                    await ОбновитьПоследовательность(j.DateTimeIddoc);
                    await ОбновитьСетевуюАктивность();
                    await _context.SaveChangesAsync();
                }
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаЧекККМ doc, bool групповоеПроведение = false)
        {
            ExceptionData result = await ЗаписатьAsync(doc);
            if (result == null)
                result = await ПровестиAsync(doc);
            return result;
        }
    }
}
