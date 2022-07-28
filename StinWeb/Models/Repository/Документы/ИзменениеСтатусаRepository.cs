using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Документы
{
    public class ИзменениеСтатусаRepository : ДокументМастерскойRepository, IИзменениеСтатуса
    {
        public ИзменениеСтатусаRepository(StinDbContext context) : base(context)
        {
            //_регистр_ОстаткиДоставки = new Регистр_ОстаткиДоставки(context);
            //_регистр_СтопЛистЗЧ = new Регистр_СтопЛистЗЧ(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    //_регистр_ОстаткиДоставки.Dispose();
                    //_регистр_СтопЛистЗЧ.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаИзменениеСтатуса> ПросмотрAsync(string idDoc)
        {
            return await ИзменениеСтатусаAsync(idDoc);
        }
        public async Task<ФормаИзменениеСтатуса> НовыйAsync(int UserRowId)
        {
            ФормаИзменениеСтатуса doc = new ФормаИзменениеСтатуса(ТипыФормы.Новый);
            var Пользователь = await userRepository.GetUserByRowIdAsync(UserRowId);
            if (!string.IsNullOrEmpty(Пользователь.ОсновнаяФирма.Id) && string.IsNullOrEmpty(Пользователь.ОсновнаяФирма.Наименование))
                Пользователь.ОсновнаяФирма = await фирмаRepository.GetEntityByIdAsync(Пользователь.ОсновнаяФирма.Id);
            if (NeedToOpenPeriod())
            {
                doc.Ошибка = new ExceptionData { Description = "Период не открыт!" };
            }
            else
            {
                doc.Общие.Автор = await userRepository.GetUserByRowIdAsync(UserRowId);
                doc.Общие.ВидДокумента10 = 13737;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.Фирма = Пользователь.ОсновнаяФирма;
                doc.Общие.ДатаДок = DateTime.Now;
                doc.ДанныеКвитанции.Изделие = new Номенклатура();
            }
            if (doc.Ошибка == null || doc.Ошибка.Skip)
            {
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, doc.Общие.ВидДокумента10.ToString(), 10, doc.Общие.Фирма.Id);
            }
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаИзменениеСтатуса doc)
        {
            ExceptionData result = await ЗаписатьAsync(doc);
            if (result == null)
                result = await ПровестиAsync(doc);
            return result;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаИзменениеСтатуса doc)
        {
            doc.ДанныеКвитанции.Склад = складRepository.GetEntityById(doc.ДанныеКвитанции.Склад.Id);
            doc.ДанныеКвитанции.Заказчик = контрагентRepository.GetEntityById(doc.ДанныеКвитанции.Заказчик.Id);
            try
            {
                Common.UnLockDocNo(_context, doc.Общие.ВидДокумента10.ToString(), doc.Общие.НомерДок);
                _1sjourn j = Common.GetEntityJourn(_context, 0, 0, 10528, doc.Общие.ВидДокумента10, null, doc.Общие.Наименование,
                    doc.Общие.НомерДок, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.ДанныеКвитанции.Склад.Наименование,
                    doc.ДанныеКвитанции.Заказчик.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                Dh13737 docHeader = new Dh13737
                {
                    Iddoc = doc.Общие.IdDoc,
                    Sp13713 = doc.ДанныеКвитанции.НомерКвитанции,
                    Sp13714 = doc.ДанныеКвитанции.ДатаКвитанции,
                    Sp13715 = doc.ДанныеКвитанции.Склад.Id,
                    Sp13716 = doc.ДанныеКвитанции.ПодСклад.Id,
                    Sp13717 = 1,
                    Sp13718 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp13719 = doc.ДанныеКвитанции.Заказчик.Id,
                    Sp13720 = doc.ДанныеКвитанции.Изделие.Id,
                    Sp13721 = doc.ДанныеКвитанции.ЗаводскойНомер != null ? doc.ДанныеКвитанции.ЗаводскойНомер : "",
                    Sp13722 = (doc.ДанныеКвитанции.Неисправность != null && doc.ДанныеКвитанции.Неисправность.Id != null) ? doc.ДанныеКвитанции.Неисправность.Id : Common.ПустоеЗначение,
                    Sp13723 = doc.ДанныеКвитанции.Гарантия,
                    Sp13724 = doc.ДанныеКвитанции.ДатаПродажи == DateTime.MinValue ? Common.min1cDate : doc.ДанныеКвитанции.ДатаПродажи,
                    Sp13725 = doc.ДанныеКвитанции.НомерРемонта,
                    Sp13726 = doc.ДанныеКвитанции.СкладОткуда != null ? doc.ДанныеКвитанции.СкладОткуда.Id : Common.ПустоеЗначение,
                    Sp13727 = doc.ДанныеКвитанции.СтатусПартииId,
                    Sp13728 = doc.ДанныеКвитанции.ДатаПриема <= Common.min1cDate ? Common.min1cDate : doc.ДанныеКвитанции.ДатаПриема,
                    Sp13729 = (doc.ДанныеКвитанции.Неисправность2 != null && doc.ДанныеКвитанции.Неисправность2.Id != null) ? doc.ДанныеКвитанции.Неисправность2.Id : Common.ПустоеЗначение,
                    Sp13730 = (doc.ДанныеКвитанции.Неисправность3 != null && doc.ДанныеКвитанции.Неисправность3.Id != null) ? doc.ДанныеКвитанции.Неисправность3.Id : Common.ПустоеЗначение,
                    Sp13731 = (doc.ДанныеКвитанции.Неисправность4 != null && doc.ДанныеКвитанции.Неисправность4.Id != null) ? doc.ДанныеКвитанции.Неисправность4.Id : Common.ПустоеЗначение,
                    Sp13732 = (doc.ДанныеКвитанции.Неисправность5 != null && doc.ДанныеКвитанции.Неисправность5.Id != null) ? doc.ДанныеКвитанции.Неисправность5.Id : Common.ПустоеЗначение,
                    Sp13733 = Common.ПустоеЗначение,
                    Sp13734 = Common.ПустоеЗначение,
                    Sp13735 = doc.СтатусНовыйId,
                    Sp660 = doc.Общие.Комментарий != null ? doc.Общие.Комментарий : "",
                };
                await _context.Dh13737s.AddAsync(docHeader);

                await _context.SaveChangesAsync();
                await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, doc.Общие.ВидДокумента10, j.Iddoc);
                if (doc.Общие.ДокОснование != null)
                    await Common.ОбновитьПодчиненныеДокументы(_context, doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаИзменениеСтатуса doc)
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
                var РегистрПартииМастерской_Остатки = await регистр_ПартииМастерской.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, doc.ДанныеКвитанции.НомерКвитанции, doc.ДанныеКвитанции.ДатаКвитанции, doc.ДанныеКвитанции.СтатусПартииId);
                if (РегистрПартииМастерской_Остатки == null || РегистрПартииМастерской_Остатки.Count == 0)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = "Квитанция не найдена в партиях." };
                }
                DateTime startOfMonth = new DateTime(doc.Общие.ДатаДок.Year, doc.Общие.ДатаДок.Month, 1);
                int КоличествоДвижений = 0;
                bool Приход = false;
                foreach (var r in РегистрПартииМастерской_Остатки)
                {
                    if (r != null)
                    {
                        КоличествоДвижений++;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA9972_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@Гарантия,@Изделие,@ЗавНомер,@СтатусПартии,@Заказчик,@СкладОткуда,@ДатаПриема," +
                            "@НомерКвитанции,@ДатаКвитанции,@Количество," +
                            "1,0,@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", doc.Общие.IdDoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@Гарантия", r.Гарантия),
                            new SqlParameter("@Изделие", r.Номенклатура),
                            new SqlParameter("@ЗавНомер", r.ЗавНомер),
                            new SqlParameter("@СтатусПартии", r.СтатусПартии),
                            new SqlParameter("@Заказчик", r.Контрагент),
                            new SqlParameter("@СкладОткуда", r.СкладОткуда),
                            new SqlParameter("@ДатаПриема", r.ДатаПриема),
                            new SqlParameter("@НомерКвитанции", r.НомерКвитанции),
                            new SqlParameter("@ДатаКвитанции", r.ДатаКвитанции),
                            new SqlParameter("@Количество", r.Количество),
                            new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                            new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                    }
                }
                Приход = true;
                КоличествоДвижений++;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA9972_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@Гарантия,@Изделие,@ЗавНомер,@СтатусПартии,@Заказчик,@СкладОткуда,@ДатаПриема," +
                    "@НомерКвитанции,@ДатаКвитанции,@Количество," +
                    "1,0,@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", doc.Общие.IdDoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@Гарантия", doc.ДанныеКвитанции.Гарантия),
                    new SqlParameter("@Изделие", doc.ДанныеКвитанции.Изделие.Id),
                    new SqlParameter("@ЗавНомер", string.IsNullOrEmpty(doc.ДанныеКвитанции.ЗаводскойНомер) ? "" : doc.ДанныеКвитанции.ЗаводскойНомер),
                    new SqlParameter("@СтатусПартии", doc.СтатусНовыйId),
                    new SqlParameter("@Заказчик", doc.ДанныеКвитанции.Заказчик.Id),
                    new SqlParameter("@СкладОткуда", (doc.ДанныеКвитанции.СкладОткуда != null ? doc.ДанныеКвитанции.СкладОткуда.Id : Common.ПустоеЗначение)),
                    new SqlParameter("@ДатаПриема", doc.ДанныеКвитанции.ДатаПриема.ToShortDateString()),
                    new SqlParameter("@НомерКвитанции", doc.ДанныеКвитанции.НомерКвитанции),
                    new SqlParameter("@ДатаКвитанции", doc.ДанныеКвитанции.ДатаКвитанции),
                    new SqlParameter("@Количество", 1),
                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Rf9972 = true;

                _context.Update(j);
                await _context.SaveChangesAsync();

                await Common.ОбновитьВремяТА(_context, j.Iddoc, j.DateTimeIddoc);
                await Common.ОбновитьПоследовательность(_context, j.DateTimeIddoc);
                await _context.ОбновитьСетевуюАктивность();
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
