using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.Repository.Интерфейсы.Регистры;
using StinWeb.Models.Repository.Регистры;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Документы
{
    public class НаДиагностикуRepository : ДокументМастерскойRepository, IНаДиагностику
    {
        private IРегистр_РаботыНаИзделиях _регистр_РаботыНаИзделиях;
        public НаДиагностикуRepository(StinDbContext context) : base(context)
        {
            _регистр_РаботыНаИзделиях = new Регистр_РаботыНаИзделиях(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистр_РаботыНаИзделиях.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаНаДиагностику> ПросмотрAsync(string idDoc)
        {
            return await НаДиагностикуAsync(idDoc);
        }
        public async Task<ФормаНаДиагностику> ВводНаОснованииAsync(string докОснованиеId, int видДокОснование,
            string userId, List<Корзина> тч)
        {
            ФормаНаДиагностику doc = new ФормаНаДиагностику(DataManager.Документы.ТипыФормы.НаОсновании);
            if (NeedToOpenPeriod())
            {
                doc.Ошибка = new ExceptionData { Description = "Период не открыт!" };
            }
            else
            {
                doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснованиеId);
                doc.Общие.Автор = await userRepository.GetUserByIdAsync(userId);
                doc.Общие.ВидДокумента10 = 10995;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;
                doc.Общие.ДатаДок = DateTime.Now;
                if (видДокОснование == 9899) //Прием в ремонт
                {
                    var ДокОснование = await ПриемВРемонтAsync(докОснованиеId);
                    doc.Общие.Комментарий = ДокОснование.Общие.Комментарий.Trim();
                    doc.НомерКвитанции = ДокОснование.НомерКвитанции;
                    doc.ДатаКвитанции = ДокОснование.ДатаКвитанции;
                    doc.Изделие = ДокОснование.Изделие;
                    doc.ЗаводскойНомер = ДокОснование.ЗаводскойНомер.Trim();
                    doc.Гарантия = ДокОснование.Гарантия;
                    doc.НомерРемонта = ДокОснование.НомерРемонта;
                    doc.ДатаПродажи = ДокОснование.ДатаПродажи;
                    doc.ДатаПриема = ДокОснование.ДатаПриема;
                    doc.Неисправность = ДокОснование.Неисправность;
                    doc.Неисправность2 = ДокОснование.Неисправность2;
                    doc.Неисправность3 = ДокОснование.Неисправность3;
                    doc.Неисправность4 = ДокОснование.Неисправность4;
                    doc.Неисправность5 = ДокОснование.Неисправность5;
                    doc.Заказчик = ДокОснование.Заказчик;
                    doc.Склад = ДокОснование.Склад;
                    doc.ПодСклад = ДокОснование.ПодСклад;
                    doc.Мастер = ДокОснование.Мастер;
                    doc.СкладОткуда = ДокОснование.СкладОткуда;
                    doc.СтатусПартииId = ДокОснование.СтатусПартииId;
                }
                if (doc.СтатусПартии != "Принят в ремонт")
                {
                    doc.Ошибка = new ExceptionData { Description = "Ремонт имеет неверный статус!" };
                }
                else
                {
                    //поиск данных в НомерПриемаВРемонт
                    var ПриемId = await регистр_номерПриемаВРемонт.ПолучитьДокументIdAsync(DateTime.MinValue, null, false, doc.НомерКвитанции, doc.ДатаКвитанции);
                    if (!string.IsNullOrEmpty(ПриемId))
                    {
                        var Прием = await ПриемВРемонтAsync(ПриемId);
                        doc.Комплектность = Прием.Комплектность;
                        doc.Телефон = Прием.Телефон;
                        doc.Email = Прием.Email;
                        doc.Photos = Прием.Photos;
                    }
                }
                if (тч != null && doc.ТипРемонта == "Платный")
                {
                    foreach (var строка in тч)
                    {
                        doc.ТабличнаяЧасть.Add(new тчНаДиагностику 
                        {
                            Работа = new Работа { Id = строка.Id, Наименование = строка.Наименование },
                            Количество = строка.Quantity,
                            Цена = строка.Цена,
                            Сумма = строка.Сумма
                        });
                    }    
                }
            }
            if (doc.Ошибка == null || doc.Ошибка.Skip)
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, "10995", 10, doc.Общие.Фирма.Id);
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаНаДиагностику doc)
        {
            ExceptionData result = await ЗаписатьAsync(doc);
            if (result == null)
                result = await ПровестиAsync(doc);
            return result;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаНаДиагностику doc)
        {
            doc.Склад = складRepository.GetEntityById(doc.Склад.Id);
            doc.Заказчик = контрагентRepository.GetEntityById(doc.Заказчик.Id);
            //doc.СтатусПартииId = Common.СтатусПартии.Where(x => x.Value == "Принят в ремонт").Select(x => x.Key).FirstOrDefault();
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
                Dh10995 docHeader = new Dh10995
                {
                    Iddoc = j.Iddoc,
                    Sp10971 = doc.НомерКвитанции,
                    Sp10972 = doc.ДатаКвитанции,
                    Sp10973 = doc.Мастер.Id == null ? Common.ПустоеЗначение : doc.Мастер.Id,
                    Sp10974 = doc.Склад.Id,
                    Sp10975 = doc.ПодСклад.Id,
                    Sp10976 = 1,
                    Sp10977 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp10978 = doc.Заказчик.Id,
                    Sp10979 = doc.Изделие.Id,
                    Sp10980 = doc.ЗаводскойНомер,
                    Sp10981 = doc.Неисправность.Id,
                    Sp10982 = doc.Гарантия,
                    Sp10983 = doc.ДатаПродажи == DateTime.MinValue ? Common.min1cDate : doc.ДатаПродажи,
                    Sp10984 = doc.НомерРемонта,
                    Sp10985 = doc.СкладОткуда != null ? doc.СкладОткуда.Id : Common.ПустоеЗначение,
                    Sp10986 = doc.СтатусПартииId,
                    Sp10987 = doc.ДатаПриема,
                    Sp10988 = (doc.Неисправность2 != null && doc.Неисправность2.Id != null) ? doc.Неисправность2.Id : Common.ПустоеЗначение,
                    Sp10989 = (doc.Неисправность3 != null && doc.Неисправность3.Id != null) ? doc.Неисправность3.Id : Common.ПустоеЗначение,
                    Sp10990 = (doc.Неисправность4 != null && doc.Неисправность4.Id != null) ? doc.Неисправность4.Id : Common.ПустоеЗначение,
                    Sp10991 = (doc.Неисправность5 != null && doc.Неисправность5.Id != null) ? doc.Неисправность5.Id : Common.ПустоеЗначение,
                    Sp10992 = Common.ПустоеЗначение,
                    Sp10993 = Common.ПустоеЗначение,
                    Sp660 = doc.Общие.Комментарий != null ? doc.Общие.Комментарий : ""
                };
                await _context.Dh10995s.AddAsync(docHeader);

                if (doc.Гарантия == 0 && doc.ТабличнаяЧасть != null && doc.ТабличнаяЧасть.Count > 0)
                {
                    //есть авансовые работы
                    short lineNo = 1;
                    foreach (var строка in doc.ТабличнаяЧасть)
                    {
                        Dt10995 docRow = new Dt10995
                        {
                            Iddoc = j.Iddoc,
                            Lineno = lineNo++,
                            Sp11621 = строка.Работа.Id,
                            Sp11622 = строка.Количество,
                            Sp11623 = строка.Цена,
                            Sp11624 = строка.Сумма
                        };
                        await _context.Dt10995s.AddAsync(docRow);
                    }
                }
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync(
                    "exec _1sp_DH10995_UpdateTotals @num36",
                    new SqlParameter("@num36", j.Iddoc)
                    );

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
        public async Task<ExceptionData> ПровестиAsync(ФормаНаДиагностику doc)
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
                var РегистрОстаткиИзделий_Остатки = await регистр_ОстаткиИзделий.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, doc.НомерКвитанции, doc.ДатаКвитанции, doc.Склад.Id);
                if (РегистрОстаткиИзделий_Остатки == null || РегистрОстаткиИзделий_Остатки.Count == 0)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = "Изделие отсутствует на складе." };
                }
                var РегистрПартииМастерской_Остатки = await регистр_ПартииМастерской.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, doc.НомерКвитанции, doc.ДатаКвитанции, doc.СтатусПартииId);
                if (РегистрПартииМастерской_Остатки == null || РегистрПартииМастерской_Остатки.Count == 0)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = "Квитанция не найдена в партиях." };
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
                Приход = true;
                КоличествоДвижений++;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA11049_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@НомерКвитанции,@ДатаКвитанции,@Склад,@ПодСклад,@Мастер,@Количество," +
                    "0,@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", doc.Общие.IdDoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@НомерКвитанции", doc.НомерКвитанции),
                    new SqlParameter("@ДатаКвитанции", doc.ДатаКвитанции),
                    new SqlParameter("@Склад", doc.Склад.Id),
                    new SqlParameter("@ПодСклад", doc.ПодСклад.Id),
                    new SqlParameter("@Мастер", doc.Мастер.Id),
                    new SqlParameter("@Количество", 1),
                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                Приход = false;
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
                    new SqlParameter("@Гарантия", doc.Гарантия),
                    new SqlParameter("@Изделие", doc.Изделие.Id),
                    new SqlParameter("@ЗавНомер", doc.ЗаводскойНомер),
                    new SqlParameter("@СтатусПартии", Common.СтатусПартии.Where(x => x.Value == "На диагностике").Select(x => x.Key).FirstOrDefault()),
                    new SqlParameter("@Заказчик", doc.Заказчик.Id),
                    new SqlParameter("@СкладОткуда", (doc.СкладОткуда != null ? doc.СкладОткуда.Id : Common.ПустоеЗначение)),
                    new SqlParameter("@ДатаПриема", doc.ДатаПриема.ToShortDateString()),
                    new SqlParameter("@НомерКвитанции", doc.НомерКвитанции),
                    new SqlParameter("@ДатаКвитанции", doc.ДатаКвитанции),
                    new SqlParameter("@Количество", 1),
                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                if (doc.ТабличнаяЧасть != null && doc.ТабличнаяЧасть.Count > 0)
                {
                    Приход = false;
                    var РегистрРаботыНаИзделиях_Остатки = await _регистр_РаботыНаИзделиях.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, doc.НомерКвитанции, doc.ДатаКвитанции);
                    foreach (var строка in doc.ТабличнаяЧасть)
                    {
                        if (РегистрРаботыНаИзделиях_Остатки.Where(x => x.РаботаId == строка.Работа.Id).Sum(x => x.Количество) < строка.Количество)
                        {
                            if (_context.Database.CurrentTransaction != null)
                                _context.Database.CurrentTransaction.Rollback();
                            return new ExceptionData { Description = "Работа " + строка.Работа.Наименование + " остаток превышает отстатки в регистре РаботыНаИзделиях" };
                        }
                    }
                    foreach (var r in РегистрРаботыНаИзделиях_Остатки)
                    {
                        КоличествоДвижений++;
                        await _context.Database.ExecuteSqlRawAsync(
                            "exec _1sp_RA9989_WriteDocAct @num36,0,@ActNo,@DebetCredit,@IdDocDef,@DateTimeIdDoc," +
                            "@НомКвитанции,@ДатаКвитанции,@Изделие,@ЗавНомер,@Исполнитель,@Работа,@ФлагБензо,@ДопРаботы,@Количество,@Сумма,@СуммаЗавода,@КодГарантии," +
                            "@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", doc.Общие.IdDoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@IdDocDef", 10995),
                            new SqlParameter("@DateTimeIdDoc", j.DateTimeIddoc),
                            new SqlParameter("@НомКвитанции", r.НомерКвитанции),
                            new SqlParameter("@ДатаКвитанции", r.ДатаКвитанции),
                            new SqlParameter("@Изделие", r.ИзделиеId),
                            new SqlParameter("@ЗавНомер", r.ЗаводскойНомер),
                            new SqlParameter("@Исполнитель", r.МастерId),
                            new SqlParameter("@Работа", r.РаботаId),
                            new SqlParameter("@ФлагБензо", r.ФлагБензо),
                            new SqlParameter("@ДопРаботы", r.ДопРаботы),
                            new SqlParameter("@Количество", r.Количество),
                            new SqlParameter("@Сумма", r.Сумма),
                            new SqlParameter("@СуммаЗавода", r.СуммаЗавода),
                            new SqlParameter("@КодГарантии", doc.Гарантия),
                            new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                            new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                    }
                }

                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Rf9972 = true;
                j.Rf11049 = true;
                j.Rf9989 = (doc.ТабличнаяЧасть != null && doc.ТабличнаяЧасть.Count > 0);

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
