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
    public class ФормаНабор
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Склад Склад { get; set; }
        public ПодСклад ПодСклад { get; set; }
        public Контрагент Контрагент { get; set; }
        public Договор Договор { get; set; }
        public Маршрут Маршрут { get; set; }
        public bool Завершен { get; set; }
        public bool СниматьРезерв { get; set; }
        public DateTime ДатаОплаты { get; set; }
        public string СпособОтгрузки { get; set; }
        public СкидКарта СкидКарта { get; set; }
        public Кладовщик Кладовщик { get; set; }
        public Order Order { get; set; }
        public List<ФормаНаборТЧ> ТабличнаяЧасть { get; set; }
        public ФормаНабор()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаНаборТЧ>();
        }
    }
    public class ФормаНаборТЧ
    {
        public Фирма ФирмаНоменклатуры { get; set; }
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public Единица Единица { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
        public string Ячейки { get; set; }
    }
    public interface IНабор : IДокумент
    {
        //Task<ФормаНабор> GetФормаНаборById(string idDoc);
        Task<List<ФормаНабор>> ВводНаОснованииAsync(ФормаЗаявкаПокупателя докОснование, DateTime docDateTime);
        Task<ExceptionData> ЗаписатьAsync(ФормаНабор doc);
        Task<ExceptionData> ПровестиAsync(ФормаНабор doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаНабор doc);
        Task<List<ФормаНабор>> ПолучитьСписокАктивныхНаборов(string orderId, bool onlyFinished);
        Task ОбновитьНомерМаршрута(ФормаНабор doc, string маршрутНаименование);
    }
    public class Набор : Документ, IНабор
    {
        private IРегистрЗаявки _регистрЗаявки;
        private IРегистрОстаткиТМЦ _регистрОстаткиТМЦ;
        private IРегистрРезервыТМЦ _регистрРезервыТМЦ;
        private IРегистрНаборНаСкладе _регистрНабор;
        private IРегистрMarketplaceOrders _регистрMarketplaceOrders;
        public Набор(StinDbContext context) : base(context)
        {
            _регистрЗаявки = new Регистр_Заявки(context);
            _регистрОстаткиТМЦ = new Регистр_ОстаткиТМЦ(context);
            _регистрРезервыТМЦ = new Регистр_РезервыТМЦ(context);
            _регистрНабор = new Регистр_НаборНаСкладе(context);
            _регистрMarketplaceOrders = new Регистр_MarketplaceOrders(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрЗаявки.Dispose();
                    _регистрОстаткиТМЦ.Dispose();
                    _регистрРезервыТМЦ.Dispose();
                    _регистрНабор.Dispose();
                    _регистрMarketplaceOrders.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<List<ФормаНабор>> ВводНаОснованииAsync(ФормаЗаявкаПокупателя докОснование, DateTime docDateTime)
        {
            List<string> СписокФирм = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(докОснование.Общие.Фирма.Id);
            List<string> СписокСкладов = new List<string>() { докОснование.Склад.Id };
            List<string> СписокНоменклатуры = докОснование.ТабличнаяЧасть.Select(x => x.Номенклатура.Id).Distinct().ToList();
            Dictionary<ПодСклад, List<ФормаНаборТЧ>> ПереченьНаличия = new Dictionary<ПодСклад, List<ФормаНаборТЧ>>();

            var заявки_Остатки = await _регистрЗаявки.ПолучитьОстаткиAsync(
                DateTime.Now,
                null,
                false,
                СписокФирм,
                докОснование.Договор.Id,
                СписокНоменклатуры,
                докОснование.Общие.IdDoc
                );

            if (заявки_Остатки != null && заявки_Остатки.Count > 0)
            {
                var СписокПодСкладов = _склад.ПолучитьПодСклады(докОснование.Склад.Id).ToList();
                var номенклатураList = await _номенклатура.ПолучитьСвободныеОстатки(
                    СписокФирм,
                    СписокСкладов,
                    СписокНоменклатуры,
                    false);
                var номенклатура_Остатки = await _регистрОстаткиТМЦ.ПолучитьОстаткиAsync(DateTime.Now, null, false,
                    СписокФирм, СписокНоменклатуры, докОснование.Склад.Id);
                var номенклатура_Резервы = await _регистрРезервыТМЦ.ПолучитьОстаткиAsync(DateTime.Now, null, false,
                    СписокФирм, докОснование.Договор.Id, СписокНоменклатуры, СписокСкладов, докОснование.Общие.IdDoc);
                var заявкиGrouped = заявки_Остатки.GroupBy(x => x.НоменклатураId).Select(gr => new
                {
                    НоменклатураId = gr.Key,
                    Количество = gr.Sum(k => k.Количество),
                    Стоимость = gr.Sum(s => s.Стоимость)
                });
                var таблЧастьGrouped = докОснование.ТабличнаяЧасть.GroupBy(x => new { x.Номенклатура, x.Единица, x.Цена }).Select(gr => new
                {
                    gr.Key.Номенклатура,
                    gr.Key.Единица,
                    Количество = gr.Sum(k => k.Количество),
                    gr.Key.Цена,
                    Сумма = gr.Sum(s => s.Сумма)
                });
                string message = "";
                foreach (var row in таблЧастьGrouped) //докОснование.ТабличнаяЧасть)
                {
                    decimal ТекОстатокСуммы = row.Сумма;
                    var r = заявкиGrouped.FirstOrDefault(x => x.НоменклатураId == row.Номенклатура.Id);
                    if ((r != null) && (r.Количество > 0))
                    {
                        decimal вРезерве = номенклатура_Резервы.Where(x => x.НоменклатураId == r.НоменклатураId)
                            .Sum(x => x.Количество);
                        decimal СвободныйОстаток = номенклатураList
                            .Where(x => x.Id == r.НоменклатураId)
                            .SelectMany(x => x.Остатки.Where(y => y.СкладId == докОснование.Склад.Id).Select(z => z.СвободныйОстаток))
                            .Sum() + вРезерве;
                        decimal Отпустить = Math.Min(r.Количество, СвободныйОстаток);
                        if (Отпустить > 0)
                        {
                            foreach (var фирмаId in СписокФирм)
                            {
                                if (номенклатура_Остатки.Where(x => x.ФирмаId == фирмаId && x.СкладId == докОснование.Склад.Id && x.НоменклатураId == r.НоменклатураId).Sum(x => x.Количество) > 0)
                                    foreach (var подСклад in СписокПодСкладов)
                                    {
                                        var остатокПодСклада = номенклатура_Остатки.Where(x => x.ФирмаId == фирмаId && x.СкладId == докОснование.Склад.Id && x.ПодСкладId == подСклад.Id && x.НоменклатураId == r.НоменклатураId).Sum(x => x.Количество);
                                        var МожноОтпустить = Math.Min(остатокПодСклада, Отпустить);
                                        if (МожноОтпустить > 0)
                                        {
                                            var текНоменклатура = await _номенклатура.GetНоменклатураByIdAsync(r.НоменклатураId);
                                            if (!ПереченьНаличия.ContainsKey(подСклад))
                                                ПереченьНаличия.Add(подСклад, new List<ФормаНаборТЧ>());
                                            decimal ДобСумма = ТекОстатокСуммы;
                                            if (МожноОтпустить != Отпустить)
                                                ДобСумма = Math.Round(row.Сумма / (row.Количество * row.Единица.Коэффициент) * МожноОтпустить, 2);
                                            ПереченьНаличия[подСклад].Add(new ФормаНаборТЧ
                                            {
                                                ФирмаНоменклатуры = await _фирма.GetEntityByIdAsync(фирмаId),
                                                Номенклатура = текНоменклатура,
                                                Количество = МожноОтпустить / текНоменклатура.Единица.Коэффициент,
                                                Единица = текНоменклатура.Единица,
                                                Цена = row.Цена,
                                                Сумма = ДобСумма
                                            });
                                            ТекОстатокСуммы = ТекОстатокСуммы - ДобСумма;
                                            Отпустить = Отпустить - МожноОтпустить;
                                            if (Отпустить <= 0)
                                                break;
                                        }
                                    }
                                if (Отпустить <= 0)
                                    break;
                            }
                        }
                        if (Отпустить > 0)
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
                }
                if (!string.IsNullOrEmpty(message))
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new List<ФормаНабор> { new ФормаНабор { Ошибка = new ExceptionData { Description = message } } };
                }
            }

            var result = new List<ФормаНабор>();
            int docCount = 0;
            foreach (var данные in ПереченьНаличия)
            {
                docCount++;
                ФормаНабор doc = new ФормаНабор();

                doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
                doc.Общие.Автор = докОснование.Общие.Автор;
                doc.Общие.ВидДокумента10 = 11948;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;
                doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;
                doc.Общие.Комментарий = string.IsNullOrEmpty(докОснование.Общие.Комментарий) ? "" : докОснование.Общие.Комментарий.Trim();

                doc.Склад = докОснование.Склад;
                doc.Контрагент = докОснование.Контрагент;
                doc.Договор = докОснование.Договор;
                Маршрут маршрут = null;
                if (докОснование.Маршрут != null)
                {
                    if (docCount > 1)
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
                doc.Завершен = false;
                doc.СниматьРезерв = true;
                doc.ДатаОплаты = докОснование.ДатаОплаты;
                doc.СпособОтгрузки = докОснование.СпособОтгрузки;
                doc.СкидКарта = докОснование.СкидКарта;
                doc.Order = докОснование.Order;
                doc.Кладовщик = null;
                doc.ПодСклад = данные.Key;

                foreach (var строка in данные.Value)
                    doc.ТабличнаяЧасть.Add(new ФормаНаборТЧ
                    {
                        ФирмаНоменклатуры = строка.ФирмаНоменклатуры,
                        Номенклатура = строка.Номенклатура,
                        Единица = строка.Единица,
                        Количество = строка.Количество,
                        Цена = строка.Цена,
                        Сумма = строка.Сумма
                    });
                result.Add(doc);
            }

            return result;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаНабор doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(_context, 1913, doc.Общие.ВидДокумента10, null, "Набор",
                        doc.Общие.НомерДок, doc.Общие.ДатаДок,
                        doc.Общие.Фирма.Id,
                        doc.Общие.Автор.Id,
                        doc.Склад.Наименование,
                        doc.Контрагент.Наименование,
                        doc.Общие.IdDoc);
                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh11948 docHeader = await _context.Dh11948s.FirstOrDefaultAsync(x => x.Iddoc == j.Iddoc);
                bool isNew = docHeader == null;
                if (isNew)
                    docHeader = new Dh11948 { Iddoc  = j.Iddoc };
                docHeader.Sp11929 = doc.Склад.Id;
                docHeader.Sp11930 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13;
                docHeader.Sp11931 = doc.Контрагент.Id;
                docHeader.Sp11932 = doc.Договор.Id;
                docHeader.Sp11933 = doc.ПодСклад.Id;
                docHeader.Sp11934 = doc.Маршрут != null ? doc.Маршрут.Code : ""; //ИндМаршрута
                docHeader.Sp11935 = doc.Маршрут != null ? doc.Маршрут.Наименование : ""; //НомерМаршрута
                docHeader.Sp11936 = Common.ВалютаРубль;
                docHeader.Sp11937 = 1; //Курс
                docHeader.Sp11938 = doc.Завершен ? 1 : 0;
                docHeader.Sp11939 = doc.СниматьРезерв ? 1 : 0;
                docHeader.Sp12012 = doc.ДатаОплаты;
                docHeader.Sp12327 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.СпособОтгрузки).Key;
                docHeader.Sp12559 = doc.Кладовщик != null ? doc.Кладовщик.Id : Common.ПустоеЗначение;
                docHeader.Sp12996 = (doc.СкидКарта == null ? Common.ПустоеЗначение : doc.СкидКарта.Id);
                docHeader.Sp14003 = doc.Order != null ? doc.Order.Id : Common.ПустоеЗначение;
                docHeader.Sp11946 = 0; //Сумма
                docHeader.Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий;

                if (isNew)
                    await _context.Dh11948s.AddAsync(docHeader);
                else
                {
                    _context.Update(j);
                    _context.Dt11948s.RemoveRange(_context.Dt11948s.Where(x => x.Iddoc == j.Iddoc));
                }

                short lineNo = 1;
                foreach (var item in doc.ТабличнаяЧасть)
                {
                    Dt11948 docRow = new Dt11948
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp11940 = item.ФирмаНоменклатуры.Id,
                        Sp11941 = item.Номенклатура.Id,
                        Sp11942 = item.Количество,
                        Sp11943 = item.Единица.Id,
                        Sp11944 = item.Единица.Коэффициент,
                        Sp11945 = item.Цена,
                        Sp11946 = item.Сумма,
                        Sp12606 = ""
                    };
                    await _context.Dt11948s.AddAsync(docRow);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаНабор doc)
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
                _context.ОчиститьДвиженияДокумента(j);
                int КоличествоДвижений = j.Actcnt;

                List<string> СписокФирм = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(doc.Общие.Фирма.Id);
                List<string> СписокСкладов = new List<string>() { doc.Склад.Id };
                List<string> СписокНоменклатуры = doc.ТабличнаяЧасть.Select(x => x.Номенклатура.Id).ToList();

                var заявки_Остатки = await _регистрЗаявки.ПолучитьОстаткиAsync(
                    doc.Общие.ДатаДок,
                    doc.Общие.IdDoc,
                    false,
                    СписокФирм,
                    doc.Договор.Id,
                    doc.СниматьРезерв ? null : СписокНоменклатуры,
                    doc.Общие.ДокОснование.IdDoc
                    );
                var номенклатура_Остатки = await _регистрОстаткиТМЦ.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                    СписокФирм, СписокНоменклатуры, doc.Склад.Id, doc.ПодСклад.Id);
                var номенклатура_Резервы = await _регистрРезервыТМЦ.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                    СписокФирм, doc.Договор.Id, doc.СниматьРезерв ? null : СписокНоменклатуры, СписокСкладов, doc.Общие.ДокОснование.IdDoc);
                List<РегистрMarketplaceOrders> marketplaceOrders_Остатки = null;
                if (doc.Order != null)
                {
                    marketplaceOrders_Остатки = await _регистрMarketplaceOrders.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                        doc.Общие.Фирма.Id, doc.Order.Id, СписокНоменклатуры);
                }

                foreach (var r in заявки_Остатки)
                {
                    КоличествоДвижений++;
                    j.Rf4674 = await _регистрЗаявки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        r.ФирмаId, r.НоменклатураId, r.ДоговорId, r.ЗаявкаId, r.Количество, r.Стоимость);
                    var вРезерве = номенклатура_Резервы.Where(x => x.ФирмаId == r.ФирмаId && x.ДоговорId == r.ДоговорId &&
                        x.ЗаявкаId == r.ЗаявкаId && x.СкладId == doc.Склад.Id && x.НоменклатураId == r.НоменклатураId).Sum(x => x.Количество);
                    if (вРезерве > 0)
                    {
                        КоличествоДвижений++;
                        j.Rf4480 = await _регистрРезервыТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                            r.ФирмаId, r.НоменклатураId, doc.Склад.Id, r.ДоговорId, r.ЗаявкаId, вРезерве);
                    }
                }
                //Регистр.ЗаказыЗаявки ДОДЕЛАТЬ - здесь не участвует
                string message = "";
                foreach (var row in doc.ТабличнаяЧасть)
                {
                    decimal Отпустить = row.Количество * row.Единица.Коэффициент;
                    if (Отпустить > 0)
                    {
                        var ОстатокНаПодСкладе = номенклатура_Остатки.Where(x => x.ФирмаId == row.ФирмаНоменклатуры.Id &&
                            x.СкладId == doc.Склад.Id && x.ПодСкладId == doc.ПодСклад.Id && x.НоменклатураId == row.Номенклатура.Id)
                            .Sum(x => x.Количество);
                        if (ОстатокНаПодСкладе >= Отпустить)
                        {
                            КоличествоДвижений++;
                            j.Rf405 = await _регистрОстаткиТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                row.ФирмаНоменклатуры.Id, row.Номенклатура.Id, doc.Склад.Id, doc.ПодСклад.Id, 0, Отпустить, 0);
                            КоличествоДвижений++;
                            j.Rf11973 = await _регистрНабор.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                                row.ФирмаНоменклатуры.Id, doc.Склад.Id, doc.ПодСклад.Id, doc.Договор.Id, doc.Общие.IdDoc, row.Номенклатура.Id, Отпустить);
                            if (doc.Order != null)
                            {
                                foreach (var regMarketOrders in marketplaceOrders_Остатки
                                    .Where(x => x.ФирмаId == doc.Общие.Фирма.Id &&
                                        x.OrderId == doc.Order.Id &&
                                        x.НоменклатураId == row.Номенклатура.Id &&
                                        x.Status == 0))
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
                                        doc.Общие.Фирма.Id, doc.Order.Id, 0, row.Номенклатура.Id,
                                        regMarketOrders.НоменклатураMarketplaceId, regMarketOrders.WarehouseId, regMarketOrders.PartnerWarehouseId, regMarketOrders.Delivery,
                                        МожноОтпустить, МожноСумма, МожноСуммаСоСкидкой, false);
                                    КоличествоДвижений++;
                                    j.Rf14021 = await _регистрMarketplaceOrders.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                                        doc.Общие.Фирма.Id, doc.Order.Id, (decimal)StinOrderStatus.PROCESSING * 10 + (decimal)StinOrderSubStatus.STARTED + (doc.Завершен ? 1 : 0), row.Номенклатура.Id,
                                        regMarketOrders.НоменклатураMarketplaceId, regMarketOrders.WarehouseId, regMarketOrders.PartnerWarehouseId, regMarketOrders.Delivery,
                                        МожноОтпустить, МожноСумма, МожноСуммаСоСкидкой, false);
                                    Отпустить = Отпустить - МожноОтпустить;
                                    if (Отпустить <= 0)
                                        break;
                                }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(message))
                                message += Environment.NewLine;
                            message += "На месте хранения нет нужного свободного количества ТМЦ ";
                            if (!string.IsNullOrEmpty(row.Номенклатура.Артикул))
                                message += "(" + row.Номенклатура.Артикул + ") ";
                            if (!string.IsNullOrEmpty(row.Номенклатура.Наименование))
                                message += row.Номенклатура.Наименование;
                            else
                                message += "'" + row.Номенклатура.Id + "'";
                        }
                    }
                }
                //Регистр.РаботаКладовщика ДОДЕЛАТЬ
                //Работа с ячейками доделать
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаНабор doc)
        {
            return await ЗаписатьAsync(doc) ?? await ПровестиAsync(doc);
        }
        public async Task<List<ФормаНабор>> ПолучитьСписокАктивныхНаборов(string orderId, bool onlyFinished)
        {
            List<ФормаНабор> наборы = new List<ФормаНабор>();
            var наборыIds = await _регистрНабор.ПолучитьСписокАктивныхНаборовAsync(DateTime.Now, null, false, orderId, onlyFinished);
            foreach (string наборId in наборыIds)
            {
                наборы.Add(await GetФормаНаборById(наборId));
            }
            return наборы;
        }
        public async Task ОбновитьНомерМаршрута(ФормаНабор doc, string маршрутНаименование)
        {
            if (doc.Маршрут != null)
            {
                var dh = await _context.Dh11948s
                .FirstOrDefaultAsync(x => (x.Iddoc == doc.Общие.IdDoc) && (x.Sp11935 != маршрутНаименование));
                if (dh != null)
                {
                    _context.ОбновитьПериодическиеРеквизиты(doc.Маршрут.Id, 11553, dh.Iddoc, маршрутНаименование);
                    dh.Sp11935 = маршрутНаименование;
                    _context.Update(dh);
                    _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, dh.Iddoc);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
