using StinClasses.Регистры;
using StinClasses.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Документы
{
    public class ФормаОтменаЗаявок
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public List<ФормаОтменаЗаявокТЧ> ТабличнаяЧасть { get; set; }
        public ФормаОтменаЗаявок()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаОтменаЗаявокТЧ>();
        }
    }
    public class ФормаОтменаЗаявокТЧ
    {
        public ФормаЗаявкаПокупателя Заявка { get; set; }
    }
    public interface IОтменаЗаявок : IДокумент
    {
        Task<List<ФормаОтменаЗаявок>> ВводНаОснованииAsync(List<ФормаЗаявкаПокупателя> докОснования, DateTime docDateTime);
        Task<ExceptionData> ЗаписатьAsync(ФормаОтменаЗаявок doc);
        Task<ExceptionData> ПровестиAsync(ФормаОтменаЗаявок doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаОтменаЗаявок doc);
    }
    public class ОтменаЗаявок : Документ, IОтменаЗаявок
    {
        private IРегистрЗаявки _регистрЗаявки;
        private IРегистрРезервыТМЦ _регистрРезервыТМЦ;
        private IРегистрЗаказыЗаявки _регистрЗаказыЗаявки;
        private IРегистрMarketplaceOrders _регистрMarketplaceOrders;
        public ОтменаЗаявок(StinDbContext context) : base(context)
        {
            _регистрЗаявки = new Регистр_Заявки(context);
            _регистрРезервыТМЦ = new Регистр_РезервыТМЦ(context);
            _регистрЗаказыЗаявки = new Регистр_ЗаказыЗаявки(context);
            _регистрMarketplaceOrders = new Регистр_MarketplaceOrders(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрЗаявки.Dispose();
                    _регистрРезервыТМЦ.Dispose();
                    _регистрЗаказыЗаявки.Dispose();
                    _регистрMarketplaceOrders.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<List<ФормаОтменаЗаявок>> ВводНаОснованииAsync(List<ФормаЗаявкаПокупателя> докОснования, DateTime docDateTime)
        {
            List<ФормаОтменаЗаявок> result = new List<ФормаОтменаЗаявок>();
            foreach (var gr in докОснования.GroupBy(x => x.Договор))
            {
                var defDoc = gr.FirstOrDefault();
                ФормаОтменаЗаявок doc = new ФормаОтменаЗаявок();

                doc.Общие.ДокОснование = await ДокОснованиеAsync(defDoc.Общие.IdDoc);
                doc.Общие.Автор = defDoc.Общие.Автор;
                doc.Общие.ВидДокумента10 = 6313;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;
                doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;
                doc.Общие.Комментарий = string.IsNullOrEmpty(defDoc.Общие.Комментарий) ? "" : defDoc.Общие.Комментарий.Trim();

                foreach (var docЗаявка in gr)
                {
                    doc.ТабличнаяЧасть.Add(new ФормаОтменаЗаявокТЧ { Заявка = docЗаявка });
                }
                result.Add(doc);
            }
            return result;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаОтменаЗаявок doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(0, 0, 4588, doc.Общие.ВидДокумента10, null, "ОтменаЗаявок",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    "",
                    "");
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh6313 docHeader = new Dh6313
                {
                    Iddoc = j.Iddoc,
                    Sp9157 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий
                };
                await _context.Dh6313s.AddAsync(docHeader);

                short lineNo = 1;
                foreach (var item in doc.ТабличнаяЧасть)
                {
                    Dt6313 docRow = new Dt6313
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp6311 = item.Заявка.Общие.IdDoc
                    };
                    await _context.Dt6313s.AddAsync(docRow);
                }
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

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
        public async Task<ExceptionData> ПровестиAsync(ФормаОтменаЗаявок doc)
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

                List<string> СписокФирм = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(doc.Общие.Фирма.Id);

                foreach (var gr in doc.ТабличнаяЧасть.Select(x => x.Заявка).GroupBy(x => x.Договор))
                {
                    List<string> СписокЗаявок = gr.Select(x => x.Общие.IdDoc).ToList();
                    List<string> СписокСкладов = gr.Select(x => x.Склад.Id).ToList();
                    List<string> СписокНоменклатуры = gr.SelectMany(x => x.ТабличнаяЧасть.Select(y => y.Номенклатура.Id)).ToList();
                    var заявки_Остатки = await _регистрЗаявки.ПолучитьОстаткиПоЗаявкамAsync(
                        doc.Общие.ДатаДок,
                        doc.Общие.IdDoc,
                        false,
                        СписокФирм,
                        gr.Key.Id,
                        СписокНоменклатуры,
                        СписокЗаявок);
                    var номенклатура_Резервы = await _регистрРезервыТМЦ.ПолучитьОстаткиПоЗаявкамAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                        СписокФирм, gr.Key.Id, СписокНоменклатуры, СписокЗаявок, СписокСкладов);
                    var заказыЗаявки_Остатки = await _регистрЗаказыЗаявки.ПолучитьОстаткиПоЗаявкамAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                        null, СписокЗаявок, СписокНоменклатуры);
                    if (заявки_Остатки != null)
                    {
                        foreach (var r in заявки_Остатки)
                        {
                            КоличествоДвижений++;
                            j.Rf4674 = await _регистрЗаявки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                r.ФирмаId, r.НоменклатураId, r.ДоговорId, r.ЗаявкаId, r.Количество, r.Стоимость);
                        }
                    }
                    if (заказыЗаявки_Остатки != null)
                    {
                        foreach (var r in заказыЗаявки_Остатки)
                        {
                            КоличествоДвижений++;
                            j.Rf4667 = await _регистрЗаказыЗаявки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                r.НоменклатураId, r.ЗаказId, r.ЗаявкаId, r.НаСогласование, r.Количество);
                        }
                    }
                    if (номенклатура_Резервы != null)
                    {
                        foreach (var r in номенклатура_Резервы)
                        {
                            КоличествоДвижений++;
                            j.Rf4480 = await _регистрРезервыТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                r.ФирмаId, r.НоменклатураId, r.СкладId, r.ДоговорId, r.ЗаявкаId, r.Количество);
                        }
                    }
                }
                foreach (var d in doc.ТабличнаяЧасть.Select(x => x.Заявка))
                {
                    if (d.Маршрут != null && !string.IsNullOrEmpty(d.Маршрут.Id) && !string.IsNullOrEmpty(d.Маршрут.Наименование))
                    {
                        КоличествоДвижений++;
                        await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(d.Маршрут.Id, 11552, j.Iddoc, doc.Общие.ДатаДок, Common.ПустоеЗначениеИд13, КоличествоДвижений));
                        КоличествоДвижений++;
                        await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(d.Маршрут.Id, 11553, j.Iddoc, doc.Общие.ДатаДок, "", КоличествоДвижений));
                    }
                    if (d.Order != null)
                    {
                        var marketplaceOrders_Остатки = await _регистрMarketplaceOrders.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                            d.Общие.Фирма.Id, d.Order.Id, d.ТабличнаяЧасть.Select(x => x.Номенклатура.Id).ToList());
                        foreach (var regMarketOrders in marketplaceOrders_Остатки)
                        {
                            КоличествоДвижений++;
                            j.Rf14021 = await _регистрMarketplaceOrders.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                regMarketOrders.ФирмаId, regMarketOrders.OrderId, regMarketOrders.Status, regMarketOrders.НоменклатураId,
                                regMarketOrders.НоменклатураMarketplaceId, regMarketOrders.WarehouseId, regMarketOrders.PartnerWarehouseId, regMarketOrders.Delivery,
                                regMarketOrders.Количество, regMarketOrders.Сумма, regMarketOrders.СуммаСоСкидкой, true);
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаОтменаЗаявок doc)
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
