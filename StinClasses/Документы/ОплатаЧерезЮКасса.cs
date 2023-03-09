using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StinClasses.Документы
{
    public class ФормаОплатаЧерезЮКасса
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public string ShopId { get; set; }
        public string SecretKey { get; set; }
        public string PaymentId { get; set; }
        public string СостояниеПлатежа { get; set; }
        public string Телефон { get; set; }
        public string Email { get; set; }
        public string ВидКонтакта { get; set; }
        public bool СообщениеОтправлено { get; set; }
        public bool УчитыватьНДС { get; set; }
        public bool СуммаВклНДС { get; set; }
        public string ConfirmationUrl { get; set; }
        public decimal Сумма { get; set; }
        public decimal СуммаНДС { get; set; }
        public List<ФормаОплатаЧерезЮКассаТЧ> ТабличнаяЧасть { get; set; }
        public ФормаОплатаЧерезЮКасса()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаОплатаЧерезЮКассаТЧ>();
        }
    }
    public class ФормаОплатаЧерезЮКассаТЧ
    {

    }
    public static class СостоянииПлатежа
    {
        public static readonly string ОжиданиеОплаты = "   AOW   ";
        public static readonly string ОжиданиеЗахватаПлатежа = "   AOX   ";
        public static readonly string УспешнаяОплата = "   AOY   ";
        public static readonly string ОплатаОтменена = "   AOZ   ";
    }
    public static class СпособыКонтакта
    {
        public static readonly string ПоТелефону = "   AOQ   ";
        public static readonly string ПоEmail = "   AOR   ";
        public static readonly string ПоViber = "   AOS   ";
        public static readonly string ПоWhatsApp = "   AOT   ";
        public static readonly string ПоTelegram = "   AOU   ";
    }
    public interface IОплатаЧерезЮКасса : IДокумент
    {
        Task<ExceptionData> ЗаписатьAsync(ФормаОплатаЧерезЮКасса doc);
        Task<ExceptionData> ОбновитьСтатусAsync(ФормаОплатаЧерезЮКасса doc);
    }
    public class ОплатаЧерезЮКасса : Документ, IОплатаЧерезЮКасса
    {
        public ОплатаЧерезЮКасса(StinDbContext context) : base(context)
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
        public async Task<ExceptionData> ЗаписатьAsync(ФормаОплатаЧерезЮКасса doc)
        {
            try
            {
                await UnLockDocNoAsync(doc.Общие.ВидДокумента10.ToString(), doc.Общие.НомерДок);
                _1sjourn j = GetEntityJourn(_context, 1896, doc.Общие.ВидДокумента10, null, doc.Общие.Наименование,
                    doc.Общие.НомерДок, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    "",
                    "");

                doc.Общие.IdDoc = j.Iddoc;
                Dh13849 docHeader = new Dh13849
                {
                    Iddoc = j.Iddoc,
                    Sp13828 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp13829 = doc.ShopId,
                    Sp13830 = doc.SecretKey,
                    Sp13831 = doc.PaymentId,
                    Sp13832 = doc.ConfirmationUrl,
                    Sp13833 = doc.СостояниеПлатежа,
                    Sp13834 = doc.Телефон,
                    Sp13835 = doc.Email,
                    Sp13836 = doc.ВидКонтакта,
                    Sp13837 = doc.СообщениеОтправлено ? 1 : 0,
                    Sp13838 = doc.УчитыватьНДС ? 1 : 0,
                    Sp13839 = doc.СуммаВклНДС ? 1 : 0,
                    Sp13845 = doc.Сумма,
                    Sp13847 = doc.СуммаНДС,
                    Sp660 = doc.Общие.Комментарий != null ? doc.Общие.Комментарий : ""
                };
                await _context.Dh13849s.AddAsync(docHeader);
                await _context.SaveChangesAsync();

                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await ОбновитьTotals(doc.Общие.ВидДокумента10, j.Iddoc);
                if (doc.Общие.ДокОснование != null)
                    await ОбновитьПодчиненныеДокументы(doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
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
        public async Task<ExceptionData> ОбновитьСтатусAsync(ФормаОплатаЧерезЮКасса doc)
        {
            try
            {
                Dh13849 dh = await _context.Dh13849s.FirstOrDefaultAsync(x => x.Iddoc == doc.Общие.IdDoc);
                if (dh != null)
                {
                    dh.Sp13833 = doc.СостояниеПлатежа;
                    _context.Update(dh);
                    _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, doc.Общие.IdDoc);
                    await _context.SaveChangesAsync();
                }
                else
                    return new ExceptionData { Description = "DH13849 не найден" };
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
    }
}
