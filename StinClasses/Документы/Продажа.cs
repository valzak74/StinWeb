using StinClasses.Регистры;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Справочники;

namespace StinClasses.Документы
{
    public class ФормаПродажа
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public KeyValuePair<string, string> КодОперации { get; set; }
        public Склад Склад { get; set; }
        public ПодСклад ПодСклад { get; set; }
        public Контрагент Контрагент { get; set; }
        public Договор Договор { get; set; }
        public Маршрут Маршрут { get; set; }
        public bool УчитыватьНДС { get; set; }
        public bool СуммаВклНДС { get; set; }
        public ТипЦен ТипЦен { get; set; }
        public Скидка Скидка { get; set; }
        public СкидКарта СкидКарта { get; set; }
        public DateTime ДатаОплаты { get; set; }
        public bool СниматьРезерв { get; set; }
        public string СпособОтгрузки { get; set; }
        public Order Order { get; set; }
        public List<ФормаПродажаТЧ> ТабличнаяЧасть { get; set; }
        public ФормаПродажа()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаПродажаТЧ>();
        }
    }
    public class ФормаПродажаТЧ
    {
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public Единица Единица { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
        public СтавкаНДС СтавкаНДС { get; set; }
        public decimal СуммаНДС { get; set; }
        public decimal Себестоимость { get; set; }
    }
    public interface IПродажа : IДокумент
    {
        Task<ФормаПродажа> ВводНаОснованииAsync(ФормаНабор докОснование, ФормаЗаявкаПокупателя счетНабора);
        Task<ExceptionData> ЗаписатьAsync(ФормаПродажа doc);
        Task<ExceptionData> ПровестиAsync(ФормаПродажа doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПродажа doc);
        Task<Dictionary<string, List<ФормаПродажаТЧ>>> РаспределитьТоварПоНаличиюAsync(ФормаПродажа doc);
    }
    public class Продажа : Документ, IПродажа
    {
        private IРегистрНаборНаСкладе _регистрНабор;
        private IРегистрПартииНаличие _регистрПартииНаличие;
        private IРегистрПрямыеПродажи _регистрПрямыеПродажи;
        private IРегистрMarketplaceOrders _регистрMarketplaceOrders;
        public Продажа(StinDbContext context) : base(context)
        {
            _регистрНабор = new Регистр_НаборНаСкладе(context);
            _регистрПартииНаличие = new Регистр_ПартииНаличие(context);
            _регистрПрямыеПродажи = new Регистр_ПрямыеПродажи(context);
            _регистрMarketplaceOrders = new Регистр_MarketplaceOrders(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрНабор.Dispose();
                    _регистрПартииНаличие.Dispose();
                    _регистрMarketplaceOrders.Dispose();
                    _регистрПрямыеПродажи.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаПродажа> ВводНаОснованииAsync(ФормаНабор докОснование, ФормаЗаявкаПокупателя счетНабора)
        {
            ФормаПродажа doc = new ФормаПродажа();

            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = (int)StinClasses.Документы.ВидДокумента.ПродажаБанк; //9109
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;

            doc.Общие.ДатаДок = DateTime.Now;

            doc.Общие.Комментарий = string.IsNullOrEmpty(докОснование.Общие.Комментарий) ? "" : докОснование.Общие.Комментарий.Trim();

            doc.КодОперации = Common.КодОперации.FirstOrDefault(x => x.Key == "   16S   "); //Продажа
            doc.Склад = докОснование.Склад;
            doc.ПодСклад = докОснование.ПодСклад;
            doc.Контрагент = докОснование.Контрагент;
            doc.Договор = докОснование.Договор;
            doc.СниматьРезерв = true;
            doc.ДатаОплаты = докОснование.ДатаОплаты;
            doc.СпособОтгрузки = докОснование.СпособОтгрузки;
            doc.СкидКарта = докОснование.СкидКарта;
            doc.Маршрут = докОснование.Маршрут;
            doc.Order = докОснование.Order;

            if (счетНабора != null)
            {
                doc.УчитыватьНДС = счетНабора.УчитыватьНДС;
                doc.СуммаВклНДС = счетНабора.СуммаВклНДС;
                doc.ТипЦен = счетНабора.ТипЦен;
                doc.Скидка = счетНабора.Скидка;
            }
            else
            {
                doc.УчитыватьНДС = true;
                doc.СуммаВклНДС = true;
                doc.ТипЦен = await _контрагент.GetТипЦенAsync(StinClasses.Common.ТипЦенРозничный);
                doc.Скидка = null;
            }
            List<string> СписокФирм = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(doc.Общие.Фирма.Id);
            List<string> СписокНоменклатуры = докОснование.ТабличнаяЧасть.Select(x => x.Номенклатура.Id).ToList();
            var наборНаСкладе_Остатки = await _регистрНабор.ПолучитьОстаткиAsync(DateTime.Now, null, false,
                СписокФирм, doc.Склад.Id, doc.Договор.Id, СписокНоменклатуры, докОснование.Общие.IdDoc);
            var наборНаСкладеGrouped = наборНаСкладе_Остатки.GroupBy(x => x.НоменклатураId).Select(gr => new
            {
                НоменклатураId = gr.Key,
                Количество = gr.Sum(k => k.Количество),
            });
            var партииНаличие_Остатки = await _регистрПартииНаличие.ПолучитьОстаткиAsync(
                DateTime.Now,
                null,
                false,
                СписокФирм,
                наборНаСкладеGrouped.Select(x => x.НоменклатураId).ToList(),
                null,
                null);
            var партииНаличиеGrouped = партииНаличие_Остатки.GroupBy(x => x.НоменклатураId).Select(gr => new
            {
                НоменклатураId = gr.Key,
                Себестоимость = gr.Sum(x => x.Количество) != 0 ? gr.Sum(x => x.СуммаУпр) / gr.Sum(x => x.Количество) : 0
            });
            foreach (var row in докОснование.ТабличнаяЧасть)
            {
                var колСтроки = row.Количество * row.Единица.Коэффициент;
                var rowСчет = счетНабора.ТабличнаяЧасть.FirstOrDefault(x => x.Номенклатура == row.Номенклатура);
                var r = наборНаСкладеGrouped.FirstOrDefault(x => x.НоменклатураId == row.Номенклатура.Id);
                if ((r != null) && (r.Количество > 0))
                {
                    decimal Отпустить = Math.Min(r.Количество, колСтроки);
                    if (Отпустить > 0)
                    {
                        var ставкаНДС = rowСчет != null ? rowСчет.СтавкаНДС : await _номенклатура.GetСтавкаНДСAsync(row.Номенклатура.Id);
                        var сумма = (Отпустить != колСтроки) ? (row.Сумма * Отпустить / колСтроки) : row.Сумма;
                        doc.ТабличнаяЧасть.Add(new ФормаПродажаТЧ
                        {
                            Номенклатура = row.Номенклатура,
                            Единица = row.Единица,
                            Количество = Отпустить / row.Единица.Коэффициент,
                            Цена = row.Цена,
                            Сумма = сумма,
                            СтавкаНДС = ставкаНДС,
                            СуммаНДС = doc.УчитыватьНДС ? (сумма * (ставкаНДС.Процент / (100 + ставкаНДС.Процент))) : 0,
                            Себестоимость = партииНаличиеGrouped.Where(x => x.НоменклатураId == row.Номенклатура.Id).Select(x => x.Себестоимость).FirstOrDefault()
                        });
                    }
                }
            }
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПродажа doc)
        {
            var result = await ЗаписатьAsync(doc);
            if (result == null)
            {
                result = await ПровестиAsync(doc);
            }
            return result;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаПродажа doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(_context, 4588, doc.Общие.ВидДокумента10, null, "ПродажаБанк",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    doc.Контрагент.Наименование);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh9109 docHeader = new Dh9109
                {
                    Iddoc = j.Iddoc,
                    Sp9075 = doc.КодОперации.Key,
                    Sp9076 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp9077 = doc.Склад.Id,
                    Sp9078 = doc.Контрагент.Id,
                    Sp9079 = doc.Договор.Id,
                    Sp9080 = Common.ВалютаРубль,
                    Sp9081 = 1, //Курс
                    Sp9082 = 0, //ОблагаетсяЕНВД
                    Sp9083 = doc.УчитыватьНДС ? 1 : 0,
                    Sp9084 = doc.СуммаВклНДС ? 1 : 0,
                    Sp9085 = 0, //УчитыватьНП
                    Sp9086 = 0, //СуммаВклНП
                    Sp9087 = doc.ТипЦен.Id,
                    Sp9088 = doc.Скидка != null ? doc.Скидка.Id : Common.ПустоеЗначение,
                    Sp9089 = doc.ТабличнаяЧасть.Sum(x => x.Сумма), //СуммаВзаиморасчетов
                    Sp9090 = doc.ДатаОплаты,
                    Sp9091 = 0, //флагСвертки
                    Sp9092 = doc.СкидКарта == null ? Common.ПустоеЗначение : doc.СкидКарта.Id,
                    Sp9093 = doc.СниматьРезерв ? 1 : 0,
                    Sp9094 = 1, //поСтандарту
                    Sp9095 = "", //НомерСФПрямой
                    Sp9096 = "", //НомерСФКосвенный
                    Sp10387 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.СпособОтгрузки).Key,
                    Sp11574 = doc.Маршрут != null ? doc.Маршрут.Code : "", //ИндМаршрута
                    Sp11575 = doc.Маршрут != null ? doc.Маршрут.Наименование : "", //НомерМаршрута
                    Sp13968 = Common.ПустоеЗначение, //СкладКомиссионера
                    Sp13999 = doc.Order != null ? doc.Order.Id : Common.ПустоеЗначение,
                    Sp9102 = 0, //Сумма
                    Sp9104 = 0, //СуммаНДС
                    Sp9106 = 0, //СуммаНП
                    Sp9546 = 0, //Себестоимость
                    Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий,
                };
                await _context.Dh9109s.AddAsync(docHeader);

                short lineNo = 1;
                foreach (var item in doc.ТабличнаяЧасть)
                {
                    Dt9109 docRow = new Dt9109
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp9097 = item.Номенклатура.Id,
                        Sp9098 = item.Количество,
                        Sp9099 = item.Единица.Id,
                        Sp9100 = item.Единица.Коэффициент,
                        Sp9101 = item.Цена,
                        Sp9102 = item.Сумма,
                        Sp9103 = item.СтавкаНДС.Id,
                        Sp9104 = item.СуммаНДС,
                        Sp9105 = Common.ПустоеЗначение, //СтавкаНП
                        Sp9106 = 0, //СуммаНП
                        Sp9107 = Common.ПустоеЗначение, //Партия
                        Sp9546 = item.Себестоимость
                    };
                    await _context.Dt9109s.AddAsync(docRow);
                }

                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await ОбновитьTotals(doc.Общие.ВидДокумента10, j.Iddoc);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаПродажа doc)
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

                List<РегистрMarketplaceOrders> marketplaceOrders_Остатки = null;
                if (doc.Order != null)
                {
                    marketplaceOrders_Остатки = await _регистрMarketplaceOrders.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                        doc.Общие.Фирма.Id, doc.Order.Id, doc.ТабличнаяЧасть.Select(x => x.Номенклатура.Id).ToList());
                }

                foreach (var row in doc.ТабличнаяЧасть)
                {
                    var Отпустить = row.Количество * row.Единица.Коэффициент;
                    КоличествоДвижений++;
                    j.Rf9596 = await _регистрПрямыеПродажи.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений,
                        doc.Общие.Фирма.Id,
                        doc.Контрагент.Id,
                        Common.ПустоеЗначение,
                        row.Номенклатура.Id,
                        (doc.Контрагент.Менеджер != null ? doc.Контрагент.Менеджер.Id : Common.ПустоеЗначение),
                        doc.КодОперации.Key,
                        "I", //место продажи
                        doc.Склад.Id,
                        row.Себестоимость,
                        row.Сумма,
                        Отпустить,
                        0, 0, 0);
                    if (doc.Order != null)
                    {
                        foreach (var regMarketOrders in marketplaceOrders_Остатки
                            .Where(x => x.ФирмаId == doc.Общие.Фирма.Id &&
                                x.OrderId == doc.Order.Id &&
                                x.НоменклатураId == row.Номенклатура.Id &&
                                x.Status == (decimal)doc.Order.Status * 10 + (decimal)doc.Order.SubStatus))
                        {
                            var МожноОтпустить = Math.Min(Отпустить, regMarketOrders.Количество);
                            var МожноСумма = regMarketOrders.Сумма;
                            var МожноСуммаСоСкидкой = regMarketOrders.СуммаСоСкидкой;
                            if (МожноОтпустить < regMarketOrders.Количество)
                            {
                                МожноСумма = МожноОтпустить * (regMarketOrders.Сумма / regMarketOrders.Количество);
                                МожноСуммаСоСкидкой = МожноОтпустить * (regMarketOrders.СуммаСоСкидкой / regMarketOrders.Количество);
                            }
                            КоличествоДвижений++;
                            j.Rf14021 = await _регистрMarketplaceOrders.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                doc.Общие.Фирма.Id, doc.Order.Id, (decimal)doc.Order.Status * 10 + (decimal)doc.Order.SubStatus, row.Номенклатура.Id,
                                regMarketOrders.НоменклатураMarketplaceId, regMarketOrders.WarehouseId, regMarketOrders.PartnerWarehouseId, regMarketOrders.Delivery,
                                МожноОтпустить, МожноСумма, МожноСуммаСоСкидкой, false);
                            Отпустить = Отпустить - МожноОтпустить;
                            if (Отпустить <= 0)
                                break;
                        }
                    }
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
        public async Task<Dictionary<string, List<ФормаПродажаТЧ>>> РаспределитьТоварПоНаличиюAsync(ФормаПродажа doc)
        {
            Dictionary<string, List<ФормаПродажаТЧ>> ПереченьНаличия = new Dictionary<string, List<ФормаПродажаТЧ>>();
            List<string> СписокФирм = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(doc.Общие.Фирма.Id);
            List<string> СписокНоменклатуры = doc.ТабличнаяЧасть.Select(x => x.Номенклатура.Id).ToList();
            var ТзОстаткиНабора = await _регистрНабор.ПолучитьОстаткиAsync(DateTime.Now, null, false,
                СписокФирм, doc.Склад.Id, doc.Договор.Id, СписокНоменклатуры, doc.Общие.ДокОснование.IdDoc);
            var остаткиНабораGrouped = ТзОстаткиНабора
                .Where(x => x.ПодСкладId == doc.ПодСклад.Id)
                .GroupBy(gr => new { gr.ФирмаId, gr.НоменклатураId })
                .Select(x => new
                {
                    ФирмаId = x.Key.ФирмаId,
                    НоменклатураId = x.Key.НоменклатураId,
                    Количество = x.Sum(x => x.Количество)
                });
            foreach (ФормаПродажаТЧ item in doc.ТабличнаяЧасть)
            {
                decimal Отпустить = item.Количество * item.Единица.Коэффициент;
                decimal ТекОстатокСуммы = item.Сумма;
                foreach (var фирмаId in СписокФирм)
                {
                    var остатокВнаборе = остаткиНабораGrouped
                        .Where(x => x.ФирмаId == фирмаId && x.НоменклатураId == item.Номенклатура.Id)
                        .Select(x => x.Количество)
                        .FirstOrDefault();
                    decimal МожноОтпустить = Math.Min(Отпустить, остатокВнаборе);
                    if (МожноОтпустить > 0)
                    {
                        if (!ПереченьНаличия.ContainsKey(фирмаId))
                            ПереченьНаличия.Add(фирмаId, new List<ФормаПродажаТЧ>());
                        decimal ДобСумма = ТекОстатокСуммы;
                        if (МожноОтпустить != Отпустить)
                            ДобСумма = Math.Round(item.Сумма / (item.Количество * item.Единица.Коэффициент) * МожноОтпустить, 2);
                        ПереченьНаличия[фирмаId].Add(new ФормаПродажаТЧ
                        {
                            Номенклатура = item.Номенклатура,
                            Единица = item.Единица,
                            Количество = МожноОтпустить / item.Единица.Коэффициент,
                            Цена = item.Цена,
                            Сумма = ДобСумма,
                            СтавкаНДС = item.СтавкаНДС,
                            СуммаНДС = doc.УчитыватьНДС ? (ДобСумма * (item.СтавкаНДС.Процент / (100 + item.СтавкаНДС.Процент))) : 0,
                        });
                        ТекОстатокСуммы = ТекОстатокСуммы - ДобСумма;
                        Отпустить = Отпустить - МожноОтпустить;
                        if (Отпустить <= 0)
                            break;
                    }
                }
            }
            return ПереченьНаличия;
        }
    }
}
