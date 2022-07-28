using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Документы
{
    public class АвансоваяОплатаRepository : ДокументМастерскойRepository, IАвансоваяОплата
    {
        public АвансоваяОплатаRepository(StinDbContext context) : base(context)
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
        public async Task<ФормаАвансоваяОплата> ПросмотрAsync(string idDoc)
        {
            return await АвансоваяОплатаAsync(idDoc);
        }
        public async Task<ФормаАвансоваяОплата> ВводНаОснованииAsync(string докОснованиеId, int видДокОснование,
            string userId, List<Корзина> тч)
        {
            ФормаАвансоваяОплата doc = new ФормаАвансоваяОплата(ТипыФормы.НаОсновании);
            if (NeedToOpenPeriod())
            {
                doc.Ошибка = new ExceptionData { Description = "Период не открыт!" };
            }
            else
            {
                doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснованиеId);
                doc.Общие.Автор = await userRepository.GetUserByIdAsync(userId);
                doc.Общие.ВидДокумента10 = 10054;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;
                doc.Общие.ДатаДок = DateTime.Now;
                doc.Касса = doc.Общие.Автор.ОсновнаяКасса;
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
                    doc.Телефон = ДокОснование.Телефон;
                    doc.Email = ДокОснование.Email;
                    doc.Склад = ДокОснование.Склад;
                    doc.ПодСклад = ДокОснование.ПодСклад;
                    doc.СкладОткуда = ДокОснование.СкладОткуда;
                    doc.СтатусПартииId = ДокОснование.СтатусПартииId;
                    doc.Комплектность = ДокОснование.Комплектность;
                    doc.Photos = ДокОснование.Photos;
                }
                if (doc.СтатусПартии != "Принят в ремонт")
                {
                    doc.Ошибка = new ExceptionData { Description = "Ремонт имеет неверный статус!" };
                    return doc;
                }
                if (doc.ТипРемонта != "Платный")
                {
                    doc.Ошибка = new ExceptionData { Description = "Авансовая оплата не может быть сформирована по типу ремонта " + doc.ТипРемонта };
                    return doc;
                }
                if (тч != null)
                {
                    foreach (var строка in тч)
                    {
                        doc.ТабличнаяЧасть.Add(new тчАвансоваяОплата
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
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, Common.НумераторВыдачаРемонт, 10, doc.Общие.Фирма.Id);
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаАвансоваяОплата doc)
        {
            ExceptionData result = await ЗаписатьAsync(doc);
            if (result == null)
                result = await ПровестиAsync(doc);
            return result;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаАвансоваяОплата doc)
        {
            if (doc.Гарантия != 0)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = 63, Description = "Документ \"Авансовая оплата\" не может быть записан по выбранному типу ремонта" };
            }
            doc.Склад = складRepository.GetEntityById(doc.Склад.Id);
            doc.Заказчик = контрагентRepository.GetEntityById(doc.Заказчик.Id);
            try
            {
                Common.UnLockDocNo(_context, Common.НумераторВыдачаРемонт, doc.Общие.НомерДок);
                _1sjourn j = Common.GetEntityJourn(_context, 0, 0, 10528, doc.Общие.ВидДокумента10, Common.НумераторВыдачаРемонт, doc.Общие.Наименование,
                    doc.Общие.НомерДок, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    doc.Заказчик.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                Dh10054 docHeader = new Dh10054
                {
                    Iddoc = j.Iddoc,
                    Sp10036 = doc.НомерКвитанции,
                    Sp10037 = doc.ДатаКвитанции,
                    Sp10038 = doc.Склад.Id,
                    Sp10039 = doc.ПодСклад.Id,
                    Sp10040 = 1,
                    Sp10209 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp10026 = doc.Заказчик.Id,
                    Sp10027 = doc.Изделие.Id,
                    Sp10028 = doc.ЗаводскойНомер,
                    Sp10029 = doc.Неисправность.Id,
                    Sp10030 = doc.Гарантия,
                    Sp10033 = doc.ДатаПродажи == DateTime.MinValue ? Common.min1cDate : doc.ДатаПродажи,
                    Sp10035 = doc.НомерРемонта,
                    Sp10119 = doc.СкладОткуда != null ? doc.СкладОткуда.Id : Common.ПустоеЗначение,
                    Sp10041 = doc.СтатусПартииId,
                    Sp10034 = doc.ДатаПриема,
                    Sp10210 = 0,
                    Sp10286 = Common.ПустоеЗначение,
                    Sp10461 = -3,
                    Sp10557 = 0,
                    Sp10852 = doc.Касса.Id,
                    Sp11593 = "",
                    Sp11594 = "",
                    Sp13601 = 0,
                    Sp10047 = 0,
                    Sp10048 = 0,
                    Sp10050 = 0,
                    Sp10051 = 0,
                    Sp10052 = 0,
                    Sp660 = doc.Общие.Комментарий != null ? doc.Общие.Комментарий : ""
                };
                await _context.Dh10054s.AddAsync(docHeader);

                if (doc.ТабличнаяЧасть != null && doc.ТабличнаяЧасть.Count > 0)
                {
                    short lineNo = 1;
                    foreach (var строка in doc.ТабличнаяЧасть)
                    {
                        Dt10054 docRow = new Dt10054
                        {
                            Iddoc = j.Iddoc,
                            Lineno = lineNo++,
                            Sp10120 = Common.ПустоеЗначение,
                            Sp10042 = Common.ПустоеЗначение,
                            Sp10043 = Common.ПустоеЗначение,
                            Sp10044 = 0,
                            Sp10045 = 0,
                            Sp10046 = 0,
                            Sp10047 = 0,
                            Sp10048 = 0,
                            Sp10049 = строка.Работа.Id,
                            Sp10121 = Common.ПустоеЗначение,
                            Sp10050 = строка.Сумма,
                            Sp10051 = 0,
                            Sp10052 = строка.Сумма,
                            Sp10763 = строка.Количество,
                            Sp10764 = строка.Цена,
                            Sp10765 = 0
                        };
                        await _context.Dt10054s.AddAsync(docRow);
                    }
                }
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync(
                    "exec _1sp_DH10054_UpdateTotals @num36",
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
        public async Task<ExceptionData> ПровестиAsync(ФормаАвансоваяОплата doc)
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
                var РегистрПартииМастерской_Остатки = await регистр_ПартииМастерской.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, doc.НомерКвитанции, doc.ДатаКвитанции, doc.СтатусПартииId);
                if (РегистрПартииМастерской_Остатки == null || РегистрПартииМастерской_Остатки.Count == 0)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = "Квитанция не найдена в партиях." };
                }
                DateTime startOfMonth = new DateTime(doc.Общие.ДатаДок.Year, doc.Общие.ДатаДок.Month, 1);
                decimal ИтогСумма = doc.ТабличнаяЧасть.Sum(x => x.Сумма);
                int КоличествоДвижений = 1;
                bool Приход = true;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA635_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@Фирма,@Касса,@Валюта,@СуммаВал,@СуммаУпр,@СуммаРуб,@КодОперации,@ДвижениеДенежныхС," +
                    "@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", doc.Общие.IdDoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@Фирма", doc.Общие.Фирма.Id),
                    new SqlParameter("@Касса", doc.Касса.Id),
                    new SqlParameter("@Валюта", Common.ВалютаРубль),
                    new SqlParameter("@СуммаВал", ИтогСумма),
                    new SqlParameter("@СуммаУпр", ИтогСумма),
                    new SqlParameter("@СуммаРуб", ИтогСумма),
                    new SqlParameter("@КодОперации", Common.КодОперации.Where(x => x.Value == "Реализация (розница, ЕНВД)").Select(x => x.Key).FirstOrDefault()),
                    new SqlParameter("@ДвижениеДенежныхС", Common.ПустоеЗначение),
                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                КоличествоДвижений++;
                Приход = true;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4335_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@Фирма,@Договор,@СтавкаНП,@ВидДолга,@КредДокумент,@СуммаВал,@СуммаУпр,@СуммаРуб,@СуммаНП,@Себестоимость,@КодОперации,@ДоговорКомитента,@ДокументОплаты," +
                    "@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", doc.Общие.IdDoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@Фирма", doc.Общие.Фирма.Id),
                    new SqlParameter("@Договор", doc.Заказчик.ОсновнойДоговор),
                    new SqlParameter("@СтавкаНП", Common.ПустоеЗначение),
                    new SqlParameter("@ВидДолга", Common.ВидДолга.Where(x => x.Value == "Долг за работы (в рознице)").Select(x => x.Key).FirstOrDefault()),
                    new SqlParameter("@КредДокумент", Common.Encode36(doc.Общие.ВидДокумента10).PadLeft(4) + doc.Общие.IdDoc),
                    new SqlParameter("@СуммаВал", ИтогСумма),
                    new SqlParameter("@СуммаУпр", ИтогСумма),
                    new SqlParameter("@СуммаРуб", ИтогСумма),
                    new SqlParameter("@СуммаНП", Common.zero),
                    new SqlParameter("@Себестоимость", Common.zero),
                    new SqlParameter("@КодОперации", Common.КодОперации.Where(x => x.Value == "Реализация (розница, ЕНВД)").Select(x => x.Key).FirstOrDefault()),
                    new SqlParameter("@ДоговорКомитента", Common.ПустоеЗначение),
                    new SqlParameter("@ДокументОплаты", Common.ПустоеЗначениеИд13),
                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                КоличествоДвижений++;
                Приход = false;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4335_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@Фирма,@Договор,@СтавкаНП,@ВидДолга,@КредДокумент,@СуммаВал,@СуммаУпр,@СуммаРуб,@СуммаНП,@Себестоимость,@КодОперации,@ДоговорКомитента,@ДокументОплаты," +
                    "@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", doc.Общие.IdDoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@Фирма", doc.Общие.Фирма.Id),
                    new SqlParameter("@Договор", doc.Заказчик.ОсновнойДоговор),
                    new SqlParameter("@СтавкаНП", Common.ПустоеЗначение),
                    new SqlParameter("@ВидДолга", Common.ВидДолга.Where(x => x.Value == "Долг за работы (в рознице)").Select(x => x.Key).FirstOrDefault()),
                    new SqlParameter("@КредДокумент", Common.Encode36(doc.Общие.ВидДокумента10).PadLeft(4) + doc.Общие.IdDoc),
                    new SqlParameter("@СуммаВал", ИтогСумма),
                    new SqlParameter("@СуммаУпр", ИтогСумма),
                    new SqlParameter("@СуммаРуб", ИтогСумма),
                    new SqlParameter("@СуммаНП", Common.zero),
                    new SqlParameter("@Себестоимость", Common.zero),
                    new SqlParameter("@КодОперации", Common.КодОперации.Where(x => x.Value == "Реализация (розница, ЕНВД)").Select(x => x.Key).FirstOrDefault()),
                    new SqlParameter("@ДоговорКомитента", Common.ПустоеЗначение),
                    new SqlParameter("@ДокументОплаты", Common.ПустоеЗначениеИд13),
                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                if (doc.ТабличнаяЧасть != null && doc.ТабличнаяЧасть.Count > 0)
                {
                    Приход = true; 
                    foreach (var строка in doc.ТабличнаяЧасть)
                    {
                        КоличествоДвижений++;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA9989_WriteDocAct @num36,0,@ActNo,@DebetCredit,@IdDocDef,@DateTimeIdDoc," +
                            "@НомКвитанции,@ДатаКвитанции,@Изделие,@ЗавНомер,@Исполнитель,@Работа,@ФлагБензо,@ДопРаботы,@Количество,@Сумма,@СуммаЗавода,@КодГарантии," +
                            "@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", doc.Общие.IdDoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@IdDocDef", doc.Общие.ВидДокумента10),
                            new SqlParameter("@DateTimeIdDoc", j.DateTimeIddoc),
                            new SqlParameter("@НомКвитанции", doc.НомерКвитанции),
                            new SqlParameter("@ДатаКвитанции", doc.ДатаКвитанции),
                            new SqlParameter("@Изделие", doc.Изделие.Id),
                            new SqlParameter("@ЗавНомер", doc.ЗаводскойНомер),
                            new SqlParameter("@Исполнитель", Common.ПустоеЗначение),
                            new SqlParameter("@Работа", строка.Работа.Id),
                            new SqlParameter("@ФлагБензо", Common.zero),
                            new SqlParameter("@ДопРаботы", Common.zero),
                            new SqlParameter("@Количество", строка.Количество),
                            new SqlParameter("@Сумма", строка.Сумма),
                            new SqlParameter("@СуммаЗавода", Common.zero),
                            new SqlParameter("@КодГарантии", doc.Гарантия),
                            new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                            new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                        КоличествоДвижений++;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA10305_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@Фирма,@Склад,@Мастер,@Изделие,@ЗавНомер,@ЗапЧасть,@Работа,@Гарантия,@Себестоимость,@ПродСтоимость,@Количество,@СебестоимостьБезН," +
                            "@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", doc.Общие.IdDoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@Фирма", doc.Общие.Фирма.Id),
                            new SqlParameter("@Склад", doc.Склад.Id),
                            new SqlParameter("@Мастер", Common.ПустоеЗначение),
                            new SqlParameter("@Изделие", doc.Изделие.Id),
                            new SqlParameter("@ЗавНомер", doc.ЗаводскойНомер),
                            new SqlParameter("@ЗапЧасть", Common.ПустоеЗначение),
                            new SqlParameter("@Работа", строка.Работа.Id),
                            new SqlParameter("@Гарантия", doc.Гарантия),
                            new SqlParameter("@Себестоимость", Common.zero),
                            new SqlParameter("@ПродСтоимость", строка.Сумма),
                            new SqlParameter("@Количество", строка.Количество),
                            new SqlParameter("@СебестоимостьБезН", Common.zero),
                            new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                            new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                    }
                }

                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Rf635 = true; //касса
                j.Rf4335 = true; //покупатели
                j.Rf9989 = (doc.ТабличнаяЧасть != null && doc.ТабличнаяЧасть.Count > 0); //работы на изделиях
                j.Rf10305 = (doc.ТабличнаяЧасть != null && doc.ТабличнаяЧасть.Count > 0); //продажи мастерской

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
        public async Task<АктВыполненныхРаботПечать> ДанныеДляПечатиAsync(ФормаАвансоваяОплата doc)
        {
            if (!string.IsNullOrEmpty(doc.Общие.Фирма.Id) && (doc.Общие.Фирма.Счет == null || doc.Общие.Фирма.Счет.Банк == null))
                doc.Общие.Фирма = await фирмаRepository.GetEntityByIdAsync(doc.Общие.Фирма.Id);
            if (!string.IsNullOrEmpty(doc.Изделие.Id) && string.IsNullOrEmpty(doc.Изделие.Наименование))
                doc.Изделие = await номенклатураRepository.GetНоменклатураByIdAsync(doc.Изделие.Id);
            decimal СуммаДокумента = doc.ТабличнаяЧасть.Sum(x => x.Сумма);
            АктВыполненныхРаботПечать ДанныеПечатиАкта = new АктВыполненныхРаботПечать
            {
                ЮрЛицо = doc.Общие.Фирма.ЮрЛицо.Наименование,
                ИНН = doc.Общие.Фирма.ЮрЛицо.ИНН,
                Адрес = doc.Общие.Фирма.ЮрЛицо.Адрес.Trim().Replace(Environment.NewLine, ""),
                Банк = doc.Общие.Фирма.Счет.Банк != null ? doc.Общие.Фирма.Счет.Банк.Наименование : "",
                БИК = doc.Общие.Фирма.Счет.Банк != null ? doc.Общие.Фирма.Счет.Банк.БИК : "",
                городБанка = doc.Общие.Фирма.Счет.Банк != null ? doc.Общие.Фирма.Счет.Банк.Город : "",
                РасчетныйСчет = doc.Общие.Фирма.Счет.РасчетныйСчет,
                КоррСчет = doc.Общие.Фирма.Счет.Банк != null ? doc.Общие.Фирма.Счет.Банк.КоррСчет : "",
                ТелефонСервиса = (await _context._1sconsts.FirstOrDefaultAsync(x => x.Id == 10711)).Value.Trim(),
                НомерДок = doc.Общие.НомерДок,
                ДатаДок = doc.Общие.ДатаДок.ToString("dd.MM.yyyy"),
                Изделие = doc.Изделие.Наименование,
                ЗаводскойНомер = doc.ЗаводскойНомер,
                Производитель = doc.Изделие.Производитель,
                НомерКвитанции = doc.КвитанцияId,
                ИтогоСумма = СуммаДокумента.ToString("0.00", CultureInfo.InvariantCulture),
                КоличествоУслуг = doc.ТабличнаяЧасть.Count,
                СуммаПрописью = СуммаДокумента.Прописью()
            };
            foreach (var row in doc.ТабличнаяЧасть)
            {
                ДанныеПечатиАкта.ТаблЧасть.Add(new КорзинаРаботПечать
                {
                    Работа = row.Работа.Наименование,
                    Единица = "шт",
                    КолВо = row.Количество.ToString("0", CultureInfo.InvariantCulture),
                    Цена = row.Цена.ToString("0.00", CultureInfo.InvariantCulture),
                    Сумма = row.Сумма.ToString("0.00", CultureInfo.InvariantCulture),
                });
            }
            return ДанныеПечатиАкта;
        }
    }
}
