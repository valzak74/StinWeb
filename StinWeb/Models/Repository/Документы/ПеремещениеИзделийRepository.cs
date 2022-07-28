using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinWeb.Models.Repository.Интерфейсы.Регистры;
using StinWeb.Models.Repository.Регистры;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Документы
{
    public class ПеремещениеИзделийRepository : ДокументМастерскойRepository, IПеремещениеИзделий
    {
        private IРегистр_ОстаткиДоставки _регистр_ОстаткиДоставки;
        private IРегистр_СтопЛистЗЧ _регистр_СтопЛистЗЧ;
        public ПеремещениеИзделийRepository(StinDbContext context) : base(context)
        {
            _регистр_ОстаткиДоставки = new Регистр_ОстаткиДоставки(context);
            _регистр_СтопЛистЗЧ = new Регистр_СтопЛистЗЧ(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистр_ОстаткиДоставки.Dispose();
                    _регистр_СтопЛистЗЧ.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаПеремещениеИзделий> ПросмотрAsync(string idDoc)
        {
            return await ПеремещениеИзделийAsync(idDoc);
        }
        public async Task<ФормаПеремещениеИзделий> НовыйAsync(int UserRowId)
        {
            ФормаПеремещениеИзделий doc = new ФормаПеремещениеИзделий(ТипыФормы.Новый);
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
                doc.Общие.ВидДокумента10 = 10080;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.Фирма = Пользователь.ОсновнаяФирма;
                doc.Общие.ДатаДок = DateTime.Now;
            }
            if (doc.Ошибка == null || doc.Ошибка.Skip)
            {
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, doc.Общие.ВидДокумента10.ToString(), 10, doc.Общие.Фирма.Id);
            }
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПеремещениеИзделий doc)
        {
            ExceptionData result = await ЗаписатьAsync(doc);
            if (result == null)
                result = await ПровестиAsync(doc);
            return result;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаПеремещениеИзделий doc)
        {
            doc.Склад = складRepository.GetEntityById(doc.Склад.Id);
            doc.Заказчик = контрагентRepository.GetEntityById(doc.Заказчик.Id);
            try
            {
                Common.UnLockDocNo(_context, doc.Общие.ВидДокумента10.ToString(), doc.Общие.НомерДок);
                _1sjourn j = Common.GetEntityJourn(_context, 0, 0, 10528, doc.Общие.ВидДокумента10, null, doc.Общие.Наименование,
                    doc.Общие.НомерДок, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    doc.Заказчик.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                Dh10080 docHeader = new Dh10080
                {
                    Iddoc = doc.Общие.IdDoc,
                    Sp10063 = doc.НомерКвитанции,
                    Sp10064 = doc.ДатаКвитанции,
                    Sp10065 = doc.Заказчик.Id,
                    Sp10066 = doc.Изделие.Id,
                    Sp10067 = string.IsNullOrEmpty(doc.ЗаводскойНомер) ? "" : doc.ЗаводскойНомер,
                    Sp10068 = (doc.Неисправность != null && doc.Неисправность.Id != null) ? doc.Неисправность.Id : Common.ПустоеЗначение,
                    Sp10069 = doc.Гарантия,
                    Sp10070 = doc.Мастер.Id == null ? Common.ПустоеЗначение : doc.Мастер.Id,
                    Sp10071 = doc.ДатаПродажи == DateTime.MinValue ? Common.min1cDate : doc.ДатаПродажи,
                    Sp10072 = doc.НомерРемонта,
                    Sp10073 = doc.Склад.Id,
                    Sp10074 = doc.ПодСклад.Id,
                    Sp10075 = doc.СкладПолучатель.Id,
                    Sp10076 = doc.СкладОткуда != null ? doc.СкладОткуда.Id : Common.ПустоеЗначение,
                    Sp10077 = 1,
                    Sp10078 = doc.СтатусПартииId,
                    Sp10124 = doc.ДатаПриема,
                    Sp10340 = Common.КодОперации.FirstOrDefault(x => x.Value == "Внутреннее перемещение с доставкой").Key,
                    Sp10341 = Common.ПустоеЗначение,
                    Sp10728 = (doc.Неисправность2 != null && doc.Неисправность2.Id != null) ? doc.Неисправность2.Id : Common.ПустоеЗначение,
                    Sp10729 = (doc.Неисправность3 != null && doc.Неисправность3.Id != null) ? doc.Неисправность3.Id : Common.ПустоеЗначение,
                    Sp10730 = (doc.Неисправность4 != null && doc.Неисправность4.Id != null) ? doc.Неисправность4.Id : Common.ПустоеЗначение,
                    Sp10731 = (doc.Неисправность5 != null && doc.Неисправность5.Id != null) ? doc.Неисправность5.Id : Common.ПустоеЗначение,
                    Sp10732 = Common.ПустоеЗначение,
                    Sp10733 = Common.ПустоеЗначение,
                    Sp10734 = (doc.Общие.ДокОснование != null && !string.IsNullOrWhiteSpace(doc.Общие.ДокОснование.Значение)) ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp11832 = "",
                    Sp11833 = "",
                    Sp660 = doc.Общие.Комментарий != null ? doc.Общие.Комментарий : "",
                };
                await _context.Dh10080s.AddAsync(docHeader);

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
        public async Task<ExceptionData> ПровестиAsync(ФормаПеремещениеИзделий doc)
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
                var РегистрОстаткиИзделий_Остатки = await регистр_ОстаткиИзделий.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, doc.НомерКвитанции, doc.ДатаКвитанции, doc.Склад.Id, doc.ПодСклад.Id);
                if (РегистрОстаткиИзделий_Остатки == null || РегистрОстаткиИзделий_Остатки.Count == 0)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = "Изделие отсутствует на складе или месте хранения." };
                }
                DateTime startOfMonth = new DateTime(doc.Общие.ДатаДок.Year, doc.Общие.ДатаДок.Month, 1);
                int КоличествоДвижений = 0;
                bool Приход = false;
                foreach (var r in РегистрОстаткиИзделий_Остатки)
                {
                    if (r != null)
                    {
                        КоличествоДвижений++;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA11049_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@НомерКвитанции,@ДатаКвитанции,@Склад,@ПодСклад,@Мастер,@Количество," +
                            "0,@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", doc.Общие.IdDoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@НомерКвитанции", r.НомерКвитанции),
                            new SqlParameter("@ДатаКвитанции", r.ДатаКвитанции),
                            new SqlParameter("@Склад", r.СкладId),
                            new SqlParameter("@ПодСклад", r.ПодСкладId),
                            new SqlParameter("@Мастер", r.МастерId),
                            new SqlParameter("@Количество", r.Количество),
                            new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                            new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                    }
                }
                var РегистрСтопЛистЗЧ_Остатки = await _регистр_СтопЛистЗЧ.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, doc.НомерКвитанции, doc.ДатаКвитанции);
                if (РегистрСтопЛистЗЧ_Остатки != null)
                {
                    Приход = false;
                    foreach (var r in РегистрСтопЛистЗЧ_Остатки)
                    {
                        if (r != null)
                        {
                            КоличествоДвижений++;
                            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA11055_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                "@Номенклатура,@Склад,@НомерКвитанции,@ДатаКвитанции,@Гарантия,@ДокРезультат," +
                                "@Количество," +
                                "@docDate,@CurPeriod,1,0",
                                new SqlParameter("@num36", doc.Общие.IdDoc),
                                new SqlParameter("@ActNo", КоличествоДвижений),
                                new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                new SqlParameter("@Номенклатура", r.НоменклатураId),
                                new SqlParameter("@Склад", r.СкладId),
                                new SqlParameter("@НомерКвитанции", r.НомерКвитанции),
                                new SqlParameter("@ДатаКвитанции", r.ДатаКвитанции),
                                new SqlParameter("@Гарантия", r.Гарантия),
                                new SqlParameter("@ДокРезультат", r.ДокРезультатId),
                                new SqlParameter("@Количество", r.Количество),
                                new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                                new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                            j.Rf11055 = true;
                        }
                    }
                }
                Приход = true;
                КоличествоДвижений++;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA8696_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@Фирма,@Номенклатура,@Склад,@ЦенаПрод,@ДокПеремещения,@ЭтоИзделие," +
                    "@Количество," +
                    "1,@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", doc.Общие.IdDoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@Фирма", doc.Общие.Фирма.Id),
                    new SqlParameter("@Номенклатура", doc.Изделие.Id),
                    new SqlParameter("@Склад", doc.СкладПолучатель.Id),
                    new SqlParameter("@ЦенаПрод", Common.zero),
                    new SqlParameter("@ДокПеремещения", doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc),
                    new SqlParameter("@ЭтоИзделие", 1),
                    new SqlParameter("@Количество", 1),
                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Rf11049 = true;
                j.Rf8696 = true;

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
