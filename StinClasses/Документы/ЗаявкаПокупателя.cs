using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinClasses.Регистры;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinClasses.Models;
using StinClasses.Справочники;

namespace StinClasses.Документы
{
    public class ФормаЗаявкаПокупателя
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public KeyValuePair<string, string> ВидОперации { get; set; }
        public Склад Склад { get; set; }
        public Контрагент Контрагент { get; set; }
        public Договор Договор { get; set; }
        public БанковскийСчет БанковскийСчет { get; set; }
        public bool ОблагаетсяЕНВД { get; set; }
        public bool УчитыватьНДС { get; set; }
        public bool СуммаВклНДС { get; set; }
        public ТипЦен ТипЦен { get; set; }
        public Скидка Скидка { get; set; }
        public СкидКарта СкидКарта { get; set; }
        public DateTime ДатаОплаты { get; set; }
        public DateTime ДатаОтгрузки { get; set; }
        public string СпособОтгрузки { get; set; }
        public Маршрут Маршрут { get; set; }
        public Order Order { get; set; }
        public List<ФормаЗаявкаПокупателяТЧ> ТабличнаяЧасть { get; set; }
        public ФормаЗаявкаПокупателя()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаЗаявкаПокупателяТЧ>();
        }
    }
    public class ФормаЗаявкаПокупателяТЧ
    {
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public Единица Единица { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
        public СтавкаНДС СтавкаНДС { get; set; }
        public decimal СуммаНДС { get; set; }
    }
    public interface IЗаявкаПокупателя : IДокумент
    {
        Task<ФормаЗаявкаПокупателя> ВводНаОснованииAsync(ФормаПредварительнаяЗаявка докОснование, DateTime docDateTime, bool включатьУслуги, string видОперацииValue, string firmaId, string складId, List<ФормаПредварительнаяЗаявкаТЧ> таблицаОснования);
        Task<ExceptionData> ЗаписатьAsync(ФормаЗаявкаПокупателя doc);
        Task<ExceptionData> ПровестиAsync(ФормаЗаявкаПокупателя doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаЗаявкаПокупателя doc);
        Task<List<ФормаЗаявкаПокупателя>> ПолучитьСписокАктивныхСчетов(string orderId, bool includeЗаказыЗаявки);
        Task ОбновитьНомерМаршрута(ФормаЗаявкаПокупателя doc, string маршрутНаименование);
    }
    public class ЗаявкаПокупателя : Документ, IЗаявкаПокупателя
    {
        private IРегистрЗаказыЗаявки _регистрЗаказыЗаявки;
        private IРегистрРезервыТМЦ _регистрРезервыТМЦ;
        private IРегистрЗаявки _регистрЗаявки;
        public ЗаявкаПокупателя(StinDbContext context) : base(context)
        {
            _регистрЗаказыЗаявки = new Регистр_ЗаказыЗаявки(context);
            _регистрРезервыТМЦ = new Регистр_РезервыТМЦ(context);
            _регистрЗаявки = new Регистр_Заявки(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрЗаказыЗаявки.Dispose();
                    _регистрРезервыТМЦ.Dispose();
                    _регистрЗаявки.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаЗаявкаПокупателя> ВводНаОснованииAsync(ФормаПредварительнаяЗаявка докОснование, DateTime docDateTime, bool включатьУслуги, string видОперацииValue, string firmaId, string складId, List<ФормаПредварительнаяЗаявкаТЧ> таблицаОснования)
        {
            ФормаЗаявкаПокупателя doc = new ФормаЗаявкаПокупателя();

            string текВидОперацииValue = string.IsNullOrEmpty(видОперацииValue) ? "Счет на оплату" : видОперацииValue;

            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            Фирма фирмаDoc = doc.Общие.ДокОснование.Фирма;
            if (текВидОперацииValue == "Заявка дилера")
            {
                фирмаDoc = await _фирма.ПолучитьПоИННAsync("7701190698");
            }
            else
            {
                if (!string.IsNullOrEmpty(firmaId))
                    фирмаDoc = await _фирма.GetEntityByIdAsync(firmaId);
            }
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = 2457;
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = фирмаDoc;

            doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;

            doc.Общие.Комментарий = string.IsNullOrEmpty(докОснование.Общие.Комментарий) ? "" : докОснование.Общие.Комментарий.Trim();

            doc.ВидОперации = Common.ВидыОперации.FirstOrDefault(x => x.Value == текВидОперацииValue);
            doc.Склад = string.IsNullOrEmpty(складId) ? докОснование.Склад : await _склад.GetEntityByIdAsync(складId);
            doc.Контрагент = докОснование.Контрагент;
            doc.Договор = докОснование.Договор;
            doc.БанковскийСчет = фирмаDoc != doc.Общие.ДокОснование.Фирма ? фирмаDoc.Счет : докОснование.БанковскийСчет;
            doc.ОблагаетсяЕНВД = false;
            doc.УчитыватьНДС = фирмаDoc != doc.Общие.ДокОснование.Фирма ? (фирмаDoc.ЮрЛицо.УчитыватьНДС == 1) : докОснование.УчитыватьНДС;
            doc.СуммаВклНДС = докОснование.СуммаВклНДС;
            doc.ТипЦен = докОснование.ТипЦен;
            doc.Скидка = докОснование.Скидка;
            doc.ДатаОплаты = докОснование.ДатаОплаты;
            doc.ДатаОтгрузки = докОснование.ДатаОтгрузки;
            doc.СкидКарта = докОснование.СкидКарта;
            doc.СпособОтгрузки = докОснование.СпособОтгрузки;
            doc.Order = докОснование.Order;
            Маршрут маршрут = null;
            if (докОснование.Маршрут != null)
            {
                var ужеСозданныеСчета = await ПолучитьСписокАктивныхСчетов(doc.Order.Id, true);
                if ((ужеСозданныеСчета != null) && (ужеСозданныеСчета.Count > 0))
                {
                    маршрут = _маршрут.НовыйЭлемент();
                    маршрут.Наименование = докОснование.Маршрут.Наименование;
                }
                else
                {
                    маршрут = докОснование.Маршрут;
                }
            }
            doc.Маршрут = маршрут;

            var таблицаДокумента = (таблицаОснования != null && таблицаОснования.Count > 0) ? таблицаОснования : докОснование.ТабличнаяЧасть.Where(x => !x.Номенклатура.ЭтоУслуга).ToList();
            if (включатьУслуги)
            {
                таблицаДокумента.AddRange(докОснование.ТабличнаяЧасть.Where(x => x.Номенклатура.ЭтоУслуга));
            }    
            foreach (var строкаДокОснования in таблицаДокумента)
            {
                doc.ТабличнаяЧасть.Add(new ФормаЗаявкаПокупателяТЧ
                {
                    Номенклатура = строкаДокОснования.Номенклатура,
                    Единица = строкаДокОснования.Единица,
                    Количество = строкаДокОснования.Количество,
                    Цена = строкаДокОснования.Цена,
                    СтавкаНДС = строкаДокОснования.СтавкаНДС,
                    Сумма = строкаДокОснования.Сумма,
                    СуммаНДС = (фирмаDoc.ЮрЛицо.УчитыватьНДС == 1 ? строкаДокОснования.СуммаНДС : 0)
                });
            }
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаЗаявкаПокупателя doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(_context, 4588, doc.Общие.ВидДокумента10, null, "ЗаявкаПокупателя",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    doc.Контрагент.Наименование);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh2457 docHeader = new Dh2457
                {
                    Iddoc = j.Iddoc,
                    Sp4433 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp2621 = doc.БанковскийСчет.Id,
                    Sp2434 = doc.Контрагент.Id,
                    Sp2435 = doc.Договор.Id,
                    Sp2436 = Common.ВалютаРубль,
                    Sp2437 = 1, //Курс
                    Sp2439 = doc.УчитыватьНДС ? 1 : 0,
                    Sp2440 = 1, //СуммаВклНДС
                    Sp2441 = 0, //УчитыватьНП
                    Sp2442 = 0, //СуммаВклНП
                    Sp2443 = doc.ТабличнаяЧасть.Sum(x => x.Сумма), //СуммаВзаиморасчетов
                    Sp2444 = doc.ТипЦен.Id, //ТипЦен
                    Sp2445 = doc.Скидка != null ? doc.Скидка.Id : Common.ПустоеЗначение, //Скидка
                    Sp2438 = doc.ДатаОплаты,
                    Sp4434 = doc.ДатаОтгрузки,
                    Sp4437 = doc.Склад.Id,
                    Sp4760 = doc.ВидОперации.Key,//ВидОперации
                    Sp7943 = Common.СпособыРезервирования.FirstOrDefault(x => x.Value == "Резервировать только из текущего остатка").Key,
                    Sp8681 = (doc.СкидКарта == null ? Common.ПустоеЗначение : doc.СкидКарта.Id),
                    Sp8835 = 1, //ПоСтандарту
                    Sp8910 = 0, //ДанаДопСкидка
                    Sp10382 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.СпособОтгрузки).Key,
                    Sp10864 = "", //НомерКвитанции
                    Sp10865 = 0, //ДатаКвитанции
                    Sp11556 = doc.Маршрут != null ? doc.Маршрут.Code : "", //ИндМаршрута
                    Sp11557 = doc.Маршрут != null ? doc.Маршрут.Наименование : "", //НомерМаршрута
                    Sp13995 = doc.Order != null ? doc.Order.Id : Common.ПустоеЗначение,
                    Sp2451 = 0, //Сумма
                    Sp2452 = 0, //СуммаНДС
                    Sp2453 = 0, //СуммаНП
                    Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий,
                };
                await _context.Dh2457s.AddAsync(docHeader);

                short lineNo = 1;
                foreach (var item in doc.ТабличнаяЧасть)
                {
                    СтавкаНДС ставкаНДС = doc.УчитыватьНДС ? await _номенклатура.GetСтавкаНДСAsync(item.Номенклатура.Id) : _номенклатура.GetСтавкаНДС(Common.СтавкиНДС.FirstOrDefault(x => x.Value == "Без НДС").Key);
                    Dt2457 docRow = new Dt2457
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp2446 = item.Номенклатура.Id,
                        Sp2447 = item.Количество,
                        Sp2448 = item.Единица.Id,
                        Sp2449 = item.Единица.Коэффициент,
                        Sp2450 = item.Цена,
                        Sp2451 = item.Сумма,
                        Sp2454 = ставкаНДС.Id,
                        Sp2452 = item.Сумма * (ставкаНДС.Процент / (100 + ставкаНДС.Процент)),
                        Sp2455 = Common.ПустоеЗначение,
                        Sp2453 = 0,
                    };
                    await _context.Dt2457s.AddAsync(docRow);
                }
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync(
                    "exec _1sp_DH2457_UpdateTotals @num36",
                    new SqlParameter("@num36", j.Iddoc)
                    );
                await _context.SaveChangesAsync();
                if (doc.Общие.ДокОснование != null)
                    await ОбновитьПодчиненныеДокументы(doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
                //склад
                await ОбновитьГрафыОтбора(4747, Common.Encode36(55).PadLeft(4) + doc.Склад.Id, j.DateTimeIddoc, j.Iddoc);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаЗаявкаПокупателя doc)
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
                var разрешенныеФирмы = doc.ВидОперации.Value == "Заявка дилера" ? new List<string> { doc.Общие.Фирма.Id } : await _фирма.ПолучитьСписокРазрешенныхФирмAsync();
                var списокСкладов = new List<string> { doc.Склад.Id };
                var номенклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                    разрешенныеФирмы,
                    списокСкладов,
                    doc.ТабличнаяЧасть.Select(x => x.Номенклатура.Id).ToList(),
                    false);
                string message = "";
                List<KeyValuePair<string, string>> ВидыОперацииПоставки = new List<KeyValuePair<string, string>> 
                {
                    Common.ВидыОперации.FirstOrDefault(x => x.Value == "Заявка (на согласование)"),
                    Common.ВидыОперации.FirstOrDefault(x => x.Value == "Заявка (согласованная)"),
                    Common.ВидыОперации.FirstOrDefault(x => x.Value == "Заявка (одобренная)")
                };
                foreach (var row in doc.ТабличнаяЧасть)
                {
                    if (row.Номенклатура.ЭтоУслуга)
                        continue;
                    decimal Зарезервировать = row.Количество * row.Единица.Коэффициент;
                    if ((Зарезервировать > 0) && (!ВидыОперацииПоставки.Contains(doc.ВидОперации)))
                    {
                        foreach (string фирмаId in разрешенныеФирмы)
                        {
                            decimal СвободныйОстаток = номенклатураList
                                .Where(x => x.Id == row.Номенклатура.Id)
                                .SelectMany(x => x.Остатки.Where(y => y.ФирмаId == фирмаId && y.СкладId == doc.Склад.Id).Select(z => z.СвободныйОстаток))
                                .Sum();
                            decimal МожноЗарезервировать = Math.Min(СвободныйОстаток, Зарезервировать);
                            if (МожноЗарезервировать > 0)
                            {
                                КоличествоДвижений++;
                                j.Rf4480 = await _регистрРезервыТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                                    фирмаId, row.Номенклатура.Id, doc.Склад.Id, doc.Договор.Id, doc.Общие.IdDoc, МожноЗарезервировать);
                                КоличествоДвижений++;
                                j.Rf4674 = await _регистрЗаявки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                                    фирмаId, row.Номенклатура.Id, doc.Договор.Id, doc.Общие.IdDoc,
                                    МожноЗарезервировать,
                                    (МожноЗарезервировать / row.Единица.Коэффициент) * row.Цена);
                                Зарезервировать = Зарезервировать - МожноЗарезервировать;
                            }
                            if (Зарезервировать <= 0)
                                break;
                        }
                    }
                    if ((Зарезервировать > 0) && (ВидыОперацииПоставки.Contains(doc.ВидОперации)))
                    {
                        int наСогласование = 0;
                        switch (doc.ВидОперации.Value)
                        {
                            case "Заявка (на согласование)":
                                наСогласование = 1;
                                break;
                            case "Заявка (согласованная)":
                                наСогласование = 3;
                                break;
                            case "Заявка (одобренная)":
                                наСогласование = 0;
                                break;
                            default:
                                наСогласование = 0;
                                break;
                        }
                        КоличествоДвижений++;
                        j.Rf4667 = await _регистрЗаказыЗаявки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                            row.Номенклатура.Id, Common.ПустоеЗначение, doc.Общие.IdDoc, наСогласование, Зарезервировать);
                        Зарезервировать = 0;
                    }
                    if (Зарезервировать > 0)
                    {
                        if (!string.IsNullOrEmpty(message))
                            message += Environment.NewLine;
                        message += "На складе нет нужного свободного количества ТМЦ ";
                        if (!string.IsNullOrEmpty(row.Номенклатура.Артикул))
                            message += "(" + row.Номенклатура.Артикул + ") ";
                        if (!string.IsNullOrEmpty(row.Номенклатура.Наименование))
                            message += row.Номенклатура.Наименование;
                        else
                            message += "'" + row.Номенклатура.Id + "'";
                    }
                }
                if (!string.IsNullOrEmpty(message))
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = message };
                }
                if (doc.Маршрут != null && !string.IsNullOrEmpty(doc.Маршрут.Id) && !string.IsNullOrEmpty(doc.Маршрут.Наименование))
                {
                    КоличествоДвижений++;
                    _context.ИзменитьПериодическиеРеквизиты(doc.Маршрут.Id, 11552, j.Iddoc, doc.Общие.ДатаДок, Common.Encode36(doc.Общие.ВидДокумента10).PadLeft(4) + j.Iddoc, КоличествоДвижений);
                    КоличествоДвижений++;
                    _context.ИзменитьПериодическиеРеквизиты(doc.Маршрут.Id, 11553, j.Iddoc, doc.Общие.ДатаДок, doc.Маршрут.Наименование, КоличествоДвижений);
                }

                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Ds1946 = 2;

                _context.Update(j);
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                //await ОбновитьВремяТА(j.Iddoc, j.DateTimeIddoc);
                //await ОбновитьПоследовательность(j.DateTimeIddoc);
                //await ОбновитьСетевуюАктивность();
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаЗаявкаПокупателя doc)
        {
            var result = await ЗаписатьAsync(doc);
            if (result == null)
            {
                result = await ПровестиAsync(doc);
            }
            return result;
        }
        public async Task<List<ФормаЗаявкаПокупателя>> ПолучитьСписокАктивныхСчетов(string orderId, bool includeЗаказыЗаявки)
        {
            List<ФормаЗаявкаПокупателя> заявки = new List<ФормаЗаявкаПокупателя>();
            var заявкиIds = await _регистрЗаявки.ПолучитьСписокАктивныхЗаявокAsync(DateTime.Now, null, false, orderId);
            foreach (string заявкаId in заявкиIds)
            {
                заявки.Add(await GetФормаЗаявкаById(заявкаId));
            }
            if (includeЗаказыЗаявки)
            {
                var заказыЗаявкиIds = await _регистрЗаказыЗаявки.ПолучитьСписокАктивныхЗаказовЗаявокAsync(DateTime.Now, null, false, orderId);
                foreach (string заявкаId in заказыЗаявкиIds)
                {
                    заявки.Add(await GetФормаЗаявкаById(заявкаId));
                }
            }
            return заявки;
        }
        public async Task ОбновитьНомерМаршрута(ФормаЗаявкаПокупателя doc, string маршрутНаименование)
        {
            if (doc.Маршрут != null)
            {
                var dh = await _context.Dh2457s
                    .FirstOrDefaultAsync(x => (x.Iddoc == doc.Общие.IdDoc) && (x.Sp11557 != маршрутНаименование));
                if (dh != null)
                {
                    _context.ОбновитьПериодическиеРеквизиты(doc.Маршрут.Id, 11553, dh.Iddoc, маршрутНаименование);
                    dh.Sp11557 = маршрутНаименование;
                    _context.Update(dh);
                    _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, dh.Iddoc);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
