using StinClasses.Регистры;
using StinClasses.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinClasses.Справочники;

namespace StinClasses.Документы
{
    public class ФормаОтменаНабора
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Склад Склад { get; set; }
        public Договор Договор { get; set; }
        public Маршрут Маршрут { get; set; }
        public Order Order { get; set; }
        public List<Номенклатура> НоменклатураОснования { get; set; }
        public ФормаОтменаНабора()
        {
            Общие = new ОбщиеРеквизиты();
            НоменклатураОснования = new List<Номенклатура>();
        }
    }
    public interface IОтменаНабора : IДокумент
    {
        Task<ФормаОтменаНабора> ВводНаОснованииAsync(ФормаНабор докОснование, DateTime docDateTime);
        Task<ExceptionData> ЗаписатьAsync(ФормаОтменаНабора doc);
        Task<ExceptionData> ПровестиAsync(ФормаОтменаНабора doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаОтменаНабора doc);
        Task<ФормаОтменаНабора> GetФормаОтменаНабораById(string idDoc);
        Task<List<ФормаНаборТЧ>> ПолучитьСписокВозвращаемыхПозиций(ФормаОтменаНабора doc);
    }
    public class ОтменаНабора : Документ, IОтменаНабора
    {
        private IРегистрОстаткиТМЦ _регистрОстаткиТМЦ;
        private IРегистрНаборНаСкладе _регистрНабор;
        private IРегистрMarketplaceOrders _регистрMarketplaceOrders;
        public ОтменаНабора(StinDbContext context) : base(context)
        {
            _регистрОстаткиТМЦ = new Регистр_ОстаткиТМЦ(context);
            _регистрНабор = new Регистр_НаборНаСкладе(context);
            _регистрMarketplaceOrders = new Регистр_MarketplaceOrders(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрОстаткиТМЦ.Dispose();
                    _регистрНабор.Dispose();
                    _регистрMarketplaceOrders.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаОтменаНабора> GetФормаОтменаНабораById(string idDoc)
        {
            var d = await (from dh in _context.Dh11964s
                           join j in _context._1sjourns on dh.Iddoc equals j.Iddoc
                           where dh.Iddoc == idDoc
                           select new
                           {
                               dh,
                               j
                           }).FirstOrDefaultAsync();
            var doc = new ФормаОтменаНабора
            {
                Общие = new ОбщиеРеквизиты
                {
                    IdDoc = d.dh.Iddoc,
                    ДокОснование = !(string.IsNullOrWhiteSpace(d.dh.Sp11962) || d.dh.Sp11962 == Common.ПустоеЗначениеИд13) ? await ДокОснованиеAsync(d.dh.Sp11962.Substring(4)) : null,
                    Фирма = await _фирма.GetEntityByIdAsync(d.j.Sp4056),
                    Автор = await _пользователь.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НазваниеВЖурнале = "Отмена набора",
                    НомерДок = d.j.Docno,
                    ДатаДок = d.j.DateTimeIddoc.ToDateTime(),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh.Sp660,
                    Удален = d.j.Ismark
                },
            };
            if (doc.Общие.ДокОснование != null && doc.Общие.ДокОснование.ВидДокумента10 == (int)ВидДокумента.Набор)
            {
                doc.Order = await ПолучитьOrderНабора(doc.Общие.ДокОснование.IdDoc);
                doc.Договор = await ПолучитьДоговорНабора(doc.Общие.ДокОснование.IdDoc);
                doc.Склад = await ПолучитьСкладНабора(doc.Общие.ДокОснование.IdDoc);

                var номенклатураОснования = await _context.Dt11948s
                    .Where(x => x.Iddoc == doc.Общие.ДокОснование.IdDoc)
                    .Select(x => x.Sp11941)
                    .ToListAsync();
                foreach (var row in номенклатураОснования)
                {
                    doc.НоменклатураОснования.Add(await _номенклатура.GetНоменклатураByIdAsync(row));
                }
            }
            return doc;
        }
        public async Task<List<ФормаНаборТЧ>> ПолучитьСписокВозвращаемыхПозиций(ФормаОтменаНабора doc)
        {
            List<string> СписокФирм = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(doc.Общие.Фирма.Id);
            var НоменклатураОснованияId = doc.НоменклатураОснования.Select(x => x.Id).ToList();
            var наборНаСкладе_Остатки = await _регистрНабор.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                СписокФирм, doc.Склад.Id, doc.Договор.Id, НоменклатураОснованияId, doc.Общие.ДокОснование.IdDoc);

            List<ФормаНаборТЧ> result = new List<ФормаНаборТЧ>();
            foreach (var r in наборНаСкладе_Остатки.GroupBy(x => x.НоменклатураId))
            {
                var текНоменклатура = doc.НоменклатураОснования.FirstOrDefault(x => x.Id == r.Key);
                var колВозврата = r.Sum(x => x.Количество) / текНоменклатура.Единица.Коэффициент;
                var ЦенаОриг = doc.Order.Items.Where(x => x.НоменклатураId == r.Key).Select(x => x.Цена).FirstOrDefault();
                result.Add(new ФормаНаборТЧ
                {
                    Номенклатура = текНоменклатура,
                    Единица = текНоменклатура.Единица,
                    Количество = колВозврата,
                    Цена = ЦенаОриг,
                    Сумма = колВозврата * ЦенаОриг
                });
            }
            return result;
        }
        public async Task<ФормаОтменаНабора> ВводНаОснованииAsync(ФормаНабор докОснование, DateTime docDateTime)
        {
            ФормаОтменаНабора doc = new ФормаОтменаНабора();

            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = 11964;
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;
            doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;
            doc.Общие.Комментарий = string.IsNullOrEmpty(докОснование.Общие.Комментарий) ? "" : докОснование.Общие.Комментарий.Trim();

            doc.Склад = докОснование.Склад;
            doc.Договор = докОснование.Договор;
            doc.Маршрут = докОснование.Маршрут;
            doc.Order = докОснование.Order;
            doc.НоменклатураОснования = докОснование.ТабличнаяЧасть.Select(x => x.Номенклатура).ToList();

            return doc;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаОтменаНабора doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(0, 0, 1913, doc.Общие.ВидДокумента10, null, "ОтменаНабора",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    "",
                    "");
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh11964 docHeader = new Dh11964
                {
                    Iddoc = j.Iddoc,
                    Sp11962 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp11997 = Common.КодОперации.FirstOrDefault(x => x.Value == "Отмена набора").Key,
                    Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий
                };
                await _context.Dh11964s.AddAsync(docHeader);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаОтменаНабора doc)
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
                var НоменклатураОснованияId = doc.НоменклатураОснования.Select(x => x.Id).ToList();
                var наборНаСкладе_Остатки = await _регистрНабор.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                    СписокФирм, doc.Склад.Id, doc.Договор.Id, НоменклатураОснованияId, doc.Общие.ДокОснование.IdDoc);
                List<РегистрMarketplaceOrders> marketplaceOrders_Остатки = null;
                if (doc.Order != null)
                {
                    marketplaceOrders_Остатки = await _регистрMarketplaceOrders.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                        doc.Общие.Фирма.Id, doc.Order.Id, НоменклатураОснованияId);
                }
                string message = "";
                foreach (var r in наборНаСкладе_Остатки)
                {
                    КоличествоДвижений++;
                    j.Rf11973 = await _регистрНабор.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        r.ФирмаId, r.СкладId, r.ПодСкладId, r.ДоговорId, r.НаборId, r.НоменклатураId, r.Количество);
                    КоличествоДвижений++;
                    j.Rf405 = await _регистрОстаткиТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                        r.ФирмаId, r.НоменклатураId, r.СкладId, r.ПодСкладId, 0, r.Количество, 0);
                    if (doc.Order != null)
                    {
                        var marketОстаток = marketplaceOrders_Остатки.Where(x => x.НоменклатураId == r.НоменклатураId).Sum(x => x.Количество);
                        if (marketОстаток >= r.Количество)
                        {
                            var marketСумма = marketplaceOrders_Остатки.Where(x => x.НоменклатураId == r.НоменклатураId).Sum(x => x.Сумма);
                            var marketСуммаСоСкидкой = marketplaceOrders_Остатки.Where(x => x.НоменклатураId == r.НоменклатураId).Sum(x => x.СуммаСоСкидкой);
                            var СписатьОстаток = r.Количество;
                            var СписатьСумма = СписатьОстаток == marketОстаток ? marketСумма : (marketСумма / marketОстаток) * СписатьОстаток;
                            var СписатьСуммаСоСкидкой = СписатьОстаток == marketОстаток ? marketСуммаСоСкидкой : (marketСуммаСоСкидкой / marketОстаток) * СписатьОстаток;
                            foreach (var regMarketOrders in marketplaceOrders_Остатки.Where(x => x.НоменклатураId == r.НоменклатураId))
                            {
                                var МожноОтпустить = Math.Min(СписатьОстаток, regMarketOrders.Количество);
                                var МожноОтпуститьСумма = МожноОтпустить == СписатьОстаток ? СписатьСумма : (СписатьСумма / СписатьОстаток) * МожноОтпустить;
                                var МожноОтпуститьСуммаСоСкидкой = МожноОтпустить == СписатьОстаток ? СписатьСуммаСоСкидкой : (СписатьСуммаСоСкидкой / СписатьОстаток) * МожноОтпустить;

                                КоличествоДвижений++;
                                j.Rf14021 = await _регистрMarketplaceOrders.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                    regMarketOrders.ФирмаId, regMarketOrders.OrderId, regMarketOrders.Status, regMarketOrders.НоменклатураId,
                                    regMarketOrders.НоменклатураMarketplaceId, regMarketOrders.WarehouseId, regMarketOrders.PartnerWarehouseId, regMarketOrders.Delivery,
                                    МожноОтпустить, МожноОтпуститьСумма, МожноОтпуститьСуммаСоСкидкой, true);
                                СписатьОстаток = СписатьОстаток - МожноОтпустить;
                                СписатьСумма = СписатьСумма - МожноОтпуститьСумма;
                                СписатьСуммаСоСкидкой = СписатьСуммаСоСкидкой - МожноОтпуститьСуммаСоСкидкой;
                                if (СписатьОстаток <= 0)
                                    break;
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(message))
                                message += Environment.NewLine;
                            message += "На order нет нужного свободного количества ТМЦ ";
                            var текНоменклатура = doc.НоменклатураОснования.FirstOrDefault(x => x.Id == r.НоменклатураId);
                            if (текНоменклатура != null)
                            {
                                if (!string.IsNullOrEmpty(текНоменклатура.Артикул))
                                    message += "(" + текНоменклатура.Артикул + ") ";
                                if (!string.IsNullOrEmpty(текНоменклатура.Наименование))
                                    message += текНоменклатура.Наименование;
                                else
                                    message += "'" + текНоменклатура.Id + "'";
                            }
                        }

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
                    await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(doc.Маршрут.Id, 11552, j.Iddoc, doc.Общие.ДатаДок, Common.ПустоеЗначениеИд13, КоличествоДвижений));
                    КоличествоДвижений++;
                    await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(doc.Маршрут.Id, 11553, j.Iddoc, doc.Общие.ДатаДок, "", КоличествоДвижений));
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаОтменаНабора doc)
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
