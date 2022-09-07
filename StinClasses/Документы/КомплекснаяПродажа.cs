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
    public class ФормаКомплекснаяПродажа
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public KeyValuePair<string, string> КодОперации { get; set; }
        public Склад Склад { get; set; }
        public Контрагент Контрагент { get; set; }
        public Договор Договор { get; set; }
        public Маршрут Маршрут { get; set; }
        public bool УчитыватьНДС { get; set; }
        public bool СуммаВклНДС { get; set; }
        public ТипЦен ТипЦен { get; set; }
        public Скидка Скидка { get; set; }
        public СкидКарта СкидКарта { get; set; }
        public DateTime ДатаОплаты { get; set; }
        public string СпособОтгрузки { get; set; }
        public Order Order { get; set; }
        public List<ФормаКомплекснаяПродажаТЧ> ТабличнаяЧасть { get; set; }
        public List<ФормаПродажаТЧ> ТабличнаяЧастьРазвернутая { get; set; }
        public ФормаКомплекснаяПродажа()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаКомплекснаяПродажаТЧ>();
            ТабличнаяЧастьРазвернутая = new List<ФормаПродажаТЧ>();
        }
    }
    public class ФормаКомплекснаяПродажаТЧ
    {
        public ФормаНабор Набор { get; set; }
        public decimal Сумма { get; set; }
    }
    public interface IКомплекснаяПродажа : IДокумент
    {
        Task<List<ФормаКомплекснаяПродажа>> ЗаполнитьНаОснованииAsync(string userId, ФормаПредварительнаяЗаявка предварительнаяЗаявка, DateTime docDateTime, List<ФормаНабор> активныеНаборы);
        Task<ExceptionData> ЗаписатьAsync(ФормаКомплекснаяПродажа doc);
        Task<ExceptionData> ПровестиAsync(ФормаКомплекснаяПродажа doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаКомплекснаяПродажа doc);
        Task<Dictionary<string, List<ФормаПродажаТЧ>>> РаспределитьТоварПоНаличиюAsync(ФормаКомплекснаяПродажа doc);
    }
    public class КомплекснаяПродажа : Документ, IКомплекснаяПродажа
    {
        private IРегистрНаборНаСкладе _регистрНабор;
        private IРегистрПартииНаличие _регистрПартииНаличие;
        private IРегистрMarketplaceOrders _регистрMarketplaceOrders;
        public КомплекснаяПродажа(StinDbContext context) : base(context)
        {
            _регистрНабор = new Регистр_НаборНаСкладе(context);
            _регистрПартииНаличие = new Регистр_ПартииНаличие(context);
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
                }
            }
            base.Dispose(disposing);
        }
        public async Task<List<ФормаКомплекснаяПродажа>> ЗаполнитьНаОснованииAsync(string userId, ФормаПредварительнаяЗаявка предварительнаяЗаявка, DateTime docDateTime, List<ФормаНабор> активныеНаборы)
        {
            var списокАктивныхФирм = активныеНаборы.Select(x => x.Общие.Фирма).DistinctBy(x => x.Id).ToList();
            var списокАктивныхСкладов = активныеНаборы.Select(x => x.Склад).DistinctBy(x => x.Id).ToList();

            List<ФормаКомплекснаяПродажа> result = new List<ФормаКомплекснаяПродажа>();
            int docCount = 0;
            foreach (var фирма in списокАктивныхФирм)
                foreach (var склад in списокАктивныхСкладов)
                {
                    docCount++;
                    ФормаКомплекснаяПродажа doc = new ФормаКомплекснаяПродажа();

                    //doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
                    doc.Общие.Автор = await _пользователь.GetUserByIdAsync(string.IsNullOrEmpty(userId) ? Common.UserRobot : userId);
                    doc.Общие.ВидДокумента10 = (int)ВидДокумента.КомплекснаяПродажа; //12542
                    doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                    doc.Общие.Фирма = фирма;

                    doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;

                    doc.Общие.Комментарий = string.IsNullOrEmpty(предварительнаяЗаявка.Общие.Комментарий) ? "" : предварительнаяЗаявка.Общие.Комментарий.Trim();

                    doc.КодОперации = Common.КодОперации.FirstOrDefault(x => x.Key == "   16S   "); //Продажа
                    doc.Склад = склад;
                    doc.Контрагент = предварительнаяЗаявка.Контрагент;
                    doc.Договор = предварительнаяЗаявка.Договор;
                    doc.ДатаОплаты = предварительнаяЗаявка.ДатаОплаты < doc.Общие.ДатаДок ? doc.Общие.ДатаДок : предварительнаяЗаявка.ДатаОплаты;
                    doc.СпособОтгрузки = предварительнаяЗаявка.СпособОтгрузки;
                    doc.Скидка = предварительнаяЗаявка.Скидка;
                    doc.СкидКарта = предварительнаяЗаявка.СкидКарта;
                    doc.ТипЦен = предварительнаяЗаявка.ТипЦен;
                    doc.Order = предварительнаяЗаявка.Order;
                    doc.УчитыватьНДС = предварительнаяЗаявка.УчитыватьНДС;
                    doc.СуммаВклНДС = предварительнаяЗаявка.СуммаВклНДС;
                    //doc.Маршрут = предварительнаяЗаявка.Маршрут;
                    if (предварительнаяЗаявка.Маршрут != null)
                    {
                        Маршрут маршрут = _маршрут.НовыйЭлемент();
                        маршрут.Наименование = предварительнаяЗаявка.Маршрут.Наименование;
                        doc.Маршрут = маршрут;
                    }

                    foreach (var набор in активныеНаборы.Where(x => x.Склад.Id == doc.Склад.Id))
                    {
                        doc.ТабличнаяЧасть.Add(new ФормаКомплекснаяПродажаТЧ
                        {
                            Набор = набор,
                            Сумма = набор.ТабличнаяЧасть.Sum(x => x.Сумма)
                        });
                    }
                    List<string> СписокФирм = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(doc.Общие.Фирма.Id);
                    var партииНаличие_Остатки = await _регистрПартииНаличие.ПолучитьОстаткиAsync(
                       DateTime.Now,
                       null,
                       false,
                       СписокФирм,
                       активныеНаборы.Where(x => x.Склад.Id == doc.Склад.Id).SelectMany(x => x.ТабличнаяЧасть.Select(x => x.Номенклатура.Id)).Distinct().ToList(),
                       null,
                       null);
                    var партииНаличиеGrouped = партииНаличие_Остатки.GroupBy(x => x.НоменклатураId).Select(gr => new
                    {
                        НоменклатураId = gr.Key,
                        Себестоимость = gr.Sum(x => x.Количество) != 0 ? gr.Sum(x => x.СуммаУпр) / gr.Sum(x => x.Количество) : 0
                    });
                    doc.ТабличнаяЧастьРазвернутая = активныеНаборы.Where(x => x.Склад.Id == doc.Склад.Id)
                        .SelectMany(x =>
                        x.ТабличнаяЧасть.GroupBy(y => new { y.Номенклатура, y.Единица })
                        .Select(gr => new
                        {
                            Номенклатура = gr.Key.Номенклатура,
                            Единица = gr.Key.Единица,
                            Количество = gr.Sum(z => z.Количество),
                            Сумма = gr.Sum(z => z.Сумма)
                        })
                        .Join(предварительнаяЗаявка.ТабличнаяЧасть,
                            наборТЧ => new { ннId = наборТЧ.Номенклатура.Id, неId = наборТЧ.Единица.Id },
                            предЗаявкаТЧ => new { ннId = предЗаявкаТЧ.Номенклатура.Id, неId = предЗаявкаТЧ.Единица.Id },
                            (наборТЧ, предЗаявкаТЧ) => new { НаборТЧ = наборТЧ, ПредЗаявкаТЧ = предЗаявкаТЧ })
                        .Select(s => new 
                        {
                            Номенклатура = s.НаборТЧ.Номенклатура,
                            Единица = s.НаборТЧ.Единица,
                            Количество = s.НаборТЧ.Количество,
                            Сумма = s.НаборТЧ.Сумма,
                            //Цена = s.НаборТЧ.Сумма / s.НаборТЧ.Количество,
                            СтавкаНДС = s.ПредЗаявкаТЧ.СтавкаНДС,
                            СуммаНДС = doc.УчитыватьНДС ? (s.НаборТЧ.Сумма == s.ПредЗаявкаТЧ.Сумма ? s.ПредЗаявкаТЧ.СуммаНДС : (s.НаборТЧ.Сумма * (s.ПредЗаявкаТЧ.СтавкаНДС.Процент / (100 + s.ПредЗаявкаТЧ.СтавкаНДС.Процент)))) : 0,
                            //Себестоимость = партииНаличиеGrouped.Where(x => x.НоменклатураId == s.НаборТЧ.Номенклатура.Id).Select(x => x.Себестоимость).FirstOrDefault()
                        }))
                        .GroupBy(x => new { x.Номенклатура, x.Единица, x.СтавкаНДС })
                        .Select(gr => new ФормаПродажаТЧ 
                        {
                            Номенклатура = gr.Key.Номенклатура,
                            Единица = gr.Key.Единица,
                            Количество = gr.Sum(z => z.Количество),
                            Сумма = gr.Sum(z => z.Сумма),
                            Цена = gr.Sum(z => z.Сумма) / gr.Sum(z => z.Количество),
                            СтавкаНДС = gr.Key.СтавкаНДС,
                            СуммаНДС = gr.Sum(z => z.СуммаНДС),
                            Себестоимость = партииНаличиеGrouped.Where(x => x.НоменклатураId == gr.Key.Номенклатура.Id).Select(x => x.Себестоимость).FirstOrDefault()
                        })
                        .ToList();
                    if (docCount == 1)
                    {
                        var услуги = предварительнаяЗаявка.ТабличнаяЧасть.Where(x => x.Номенклатура.ЭтоУслуга);
                        if (услуги.Count() > 0)
                        {
                            doc.ТабличнаяЧасть.Add(new ФормаКомплекснаяПродажаТЧ
                            {
                                Набор = null,
                                Сумма = услуги.Sum(x => x.Сумма)
                            });
                            foreach (var row in услуги)
                            {
                                doc.ТабличнаяЧастьРазвернутая.Add(new ФормаПродажаТЧ
                                {
                                    Номенклатура = row.Номенклатура,
                                    Единица = row.Единица,
                                    Количество = row.Количество,
                                    Сумма = row.Сумма,
                                    Цена = row.Цена,
                                    СтавкаНДС = row.СтавкаНДС,
                                    СуммаНДС = row.СуммаНДС,
                                    Себестоимость = 0
                                });
                            }
                        }
                    }
                    result.Add(doc);
                }
            return result;
        }
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаКомплекснаяПродажа doc)
        {
            var result = await ЗаписатьAsync(doc);
            if (result == null)
            {
                result = await ПровестиAsync(doc);
            }
            return result;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаКомплекснаяПродажа doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(0, 0, 4588, doc.Общие.ВидДокумента10, null, "КомплекснаяПродажа",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    doc.Контрагент.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh12542 docHeader = new Dh12542
                {
                    Iddoc = j.Iddoc,
                    Sp12517 = doc.КодОперации.Key,
                    Sp12516 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp12518 = doc.Склад.Id,
                    Sp12519 = doc.Контрагент.Id,
                    Sp12520 = doc.Договор.Id,
                    Sp12521 = Common.ВалютаРубль,
                    Sp12522 = 1, //Курс
                    Sp12527 = 0, //ОблагаетсяЕНВД
                    Sp12528 = doc.УчитыватьНДС ? 1 : 0,
                    Sp12529 = doc.СуммаВклНДС ? 1 : 0,
                    Sp12530 = 0, //УчитыватьНП
                    Sp12531 = 0, //СуммаВклНП
                    Sp12532 = Common.ПустоеЗначение, //касса
                    Sp12533 = doc.ТипЦен.Id,
                    Sp12534 = doc.Скидка != null ? doc.Скидка.Id : Common.ПустоеЗначение,
                    Sp12537 = doc.ТабличнаяЧасть.Sum(x => x.Сумма), //СуммаВзаиморасчетов
                    Sp12525 = doc.ДатаОплаты,
                    Sp12536 = 0, //КартОплата
                    Sp12535 = doc.СкидКарта == null ? Common.ПустоеЗначение : doc.СкидКарта.Id,
                    Sp12538 = 0, // Получено
                    Sp13784 = 0, //ВидОплаты
                    Sp12526 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.СпособОтгрузки).Key,
                    Sp12523 = doc.Маршрут != null ? doc.Маршрут.Code : "", //ИндМаршрута
                    Sp12524 = doc.Маршрут != null ? doc.Маршрут.Наименование : "", //НомерМаршрута
                    Sp13971 = Common.ПустоеЗначение, //СкладКомиссионера
                    Sp14005 = doc.Order != null ? doc.Order.Id : Common.ПустоеЗначение,
                    Sp12540 = 0, //Сумма
                    Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий,
                };
                await _context.Dh12542s.AddAsync(docHeader);

                short lineNo = 1;
                foreach (var item in doc.ТабличнаяЧасть)
                {
                    Dt12542 docRow = new Dt12542
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp12539 = item.Набор != null ? item.Набор.Общие.IdDoc : Common.ПустоеЗначение,
                        Sp12540 = item.Сумма,
                    };
                    await _context.Dt12542s.AddAsync(docRow);
                }

                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await ОбновитьTotals(doc.Общие.ВидДокумента10, j.Iddoc);
                foreach (var item in doc.ТабличнаяЧасть.Where(x => x.Набор != null).Select(x => x.Набор))
                    await ОбновитьПодчиненныеДокументы(item.Общие.ВидДокумента36.PadLeft(4) + item.Общие.IdDoc, j.DateTimeIddoc, j.Iddoc);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаКомплекснаяПродажа doc)
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

                string водительId = "";
                DateTime датаОтправки = Common.min1cDate;
                foreach (var набор in doc.ТабличнаяЧасть.Where(x => (x.Набор != null) && (x.Набор.Маршрут != null)).Select(x => x.Набор))
                {
                    if (string.IsNullOrEmpty(водительId) || датаОтправки == Common.min1cDate)
                    {
                        //перезаполним маршрут на всякий случай
                        var маршрут = await _маршрут.GetМаршрутByCodeAsync(набор.Маршрут.Code);
                        if (маршрут.Водитель != null)
                            водительId = маршрут.Водитель.Id;
                        датаОтправки = маршрут.ДатаОтправки;
                    }
                    КоличествоДвижений++;
                    await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(набор.Маршрут.Id, 11552, j.Iddoc, doc.Общие.ДатаДок, string.Empty, КоличествоДвижений));
                    КоличествоДвижений++;
                    await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(набор.Маршрут.Id, 11553, j.Iddoc, doc.Общие.ДатаДок, string.Empty, КоличествоДвижений));
                }

                if (doc.Маршрут != null && !string.IsNullOrEmpty(doc.Маршрут.Id) && !string.IsNullOrEmpty(doc.Маршрут.Наименование))
                {
                    if (!string.IsNullOrEmpty(водительId) && (датаОтправки > Common.min1cDate))
                        await _маршрут.ОбновитьМаршрут(doc.Маршрут.Id, водительId, датаОтправки);
                    КоличествоДвижений++;
                    await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(doc.Маршрут.Id, 11552, j.Iddoc, doc.Общие.ДатаДок, Common.Encode36(doc.Общие.ВидДокумента10).PadLeft(4) + j.Iddoc, КоличествоДвижений));
                    КоличествоДвижений++;
                    await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(doc.Маршрут.Id, 11553, j.Iddoc, doc.Общие.ДатаДок, doc.Маршрут.Наименование, КоличествоДвижений));
                }

                if (doc.Order != null)
                {
                    var marketplaceOrders_Остатки = await _регистрMarketplaceOrders.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                        doc.Общие.Фирма.Id, doc.Order.Id, doc.ТабличнаяЧастьРазвернутая.Select(x => x.Номенклатура.Id).ToList());
                    foreach (var row in doc.ТабличнаяЧастьРазвернутая)
                    {
                        var Отпустить = row.Количество * row.Единица.Коэффициент;
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
        public async Task<Dictionary<string, List<ФормаПродажаТЧ>>> РаспределитьТоварПоНаличиюAsync(ФормаКомплекснаяПродажа doc)
        {
            Dictionary<string, List<ФормаПродажаТЧ>> ПереченьНаличия = new Dictionary<string, List<ФормаПродажаТЧ>>();
            List<string> СписокФирм = doc.ТабличнаяЧасть.Where(x => x.Набор != null).SelectMany(x => x.Набор.ТабличнаяЧасть.Select(y => y.ФирмаНоменклатуры.Id)).Distinct().ToList();
            List<string> СписокНаборов = doc.ТабличнаяЧасть.Where(x => x.Набор != null).Select(x => x.Набор.Общие.IdDoc).ToList();
            List<string> СписокНоменклатуры = doc.ТабличнаяЧастьРазвернутая.Select(x => x.Номенклатура.Id).Distinct().ToList();
            var ТзОстаткиНабора = await _регистрНабор.ПолучитьОстаткиAsync(DateTime.Now, null, false,
                СписокФирм, doc.Склад.Id, doc.Договор.Id, СписокНоменклатуры, СписокНаборов);
            foreach (ФормаПродажаТЧ item in doc.ТабличнаяЧастьРазвернутая.Where(x => !x.Номенклатура.ЭтоУслуга))
            {
                decimal Отпустить = item.Количество * item.Единица.Коэффициент;
                decimal ТекОстатокСуммы = item.Сумма;
                foreach (var фирмаId in СписокФирм)
                {
                    var остатокВнаборе = ТзОстаткиНабора
                        .Where(x => x.ФирмаId == фирмаId && x.НоменклатураId == item.Номенклатура.Id)
                        .Sum(x => x.Количество);
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
