using StinClasses.Регистры;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Справочники;
using System.Globalization;
using System.Text;
using System.Threading;

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
        public DateTime StartCompectation { get; set; }
        public DateTime EndComplectation { get; set; }
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
        Task<(string html, int docOnPage)> PrintForm(string html, int totalPerPage, int docOnPage, ФормаНабор form, CancellationToken cancellationToken);
        bool IsActive(string idDoc);
        Task<List<ФормаНабор>> ПолучитьСписокАктивныхНаборов(string orderId, bool onlyFinished);
        Task ОбновитьНомерМаршрута(ФормаНабор doc, string маршрутНаименование);
        Task ОбновитьКладовщика(ФормаНабор doc, string кладовщикId);
    }
    public class Набор : Документ, IНабор
    {
        private IРегистрЗаявки _регистрЗаявки;
        private IРегистрОстаткиТМЦ _регистрОстаткиТМЦ;
        private IРегистрРезервыТМЦ _регистрРезервыТМЦ;
        private IРегистрНаборНаСкладе _регистрНабор;
        private IРегистрMarketplaceOrders _регистрMarketplaceOrders;
        IРегистрРаботаКладовщика _регистрРаботаКладовщика;
        public Набор(StinDbContext context) : base(context)
        {
            _регистрЗаявки = new Регистр_Заявки(context);
            _регистрОстаткиТМЦ = new Регистр_ОстаткиТМЦ(context);
            _регистрРезервыТМЦ = new Регистр_РезервыТМЦ(context);
            _регистрНабор = new Регистр_НаборНаСкладе(context);
            _регистрРаботаКладовщика = new Регистр_РаботаКладовщика(context);
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
                    _регистрРаботаКладовщика.Dispose();
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
                docHeader.Sp14285 = doc.StartCompectation < Common.min1cDate ? Common.min1cDate : doc.StartCompectation; //ДатаНачалаСборки
                docHeader.Sp14286 = doc.EndComplectation < Common.min1cDate ? Common.min1cDate : doc.EndComplectation; //ДатаКонцаСборки
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
                if (doc.Завершен && doc.Кладовщик != null)
                {
                    КоличествоДвижений++;
                    j.Rf12566 = await _регистрРаботаКладовщика.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений,
                        doc.Кладовщик.Id,
                        1,
                        doc.ТабличнаяЧасть.Count,
                        doc.ТабличнаяЧасть.Sum(x => x.Количество)
                        );
                }
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
        public async Task<(string html, int docOnPage)> PrintForm(string html, int totalPerPage, int docOnPage, ФормаНабор form, CancellationToken cancellationToken)
        {
            string pref = "&nbsp;&nbsp;&nbsp;&nbsp;";
            var singleBoxMarket = new List<string> { "ALIEXPRESS", "OZON", "WILDBERRIES" };
            var isSingleBox = singleBoxMarket.Contains(form.Order?.Тип);
            bool withOrder = form.Order != null;
            var таблЧасть = Enumerable.Repeat(new
            {
                Ном = "",
                Товар = "",
                ТоварЧек = "",
                Производитель = "",
                Артикул = "",
                КолВо = 0m,
                Количество = "",
                КоличествоЧек = "",
                Единица = "",
                ЕдиницаЧек = "",
                КолМест = "",
                КолЭтикеток = "",
                Цена = "",
                ЦенаСоСкидкой = "",
                Сумма = "",
                СуммаСоСкидкой = ""
            }, 0).ToList();
            var СписокФирм = form.ТабличнаяЧасть.Select(x => x.ФирмаНоменклатуры.Id).Distinct().ToList();
            var НоменклатураIds = form.ТабличнаяЧасть.Select(x => x.Номенклатура.Id).Distinct().ToList();
            var наборНаСкладе_Остатки = await _регистрНабор.ПолучитьОстаткиAsync(DateTime.Now, null, false,
                СписокФирм, form.Склад.Id, form.Договор.Id, НоменклатураIds, form.Общие.IdDoc);
            int номСтроки = 0; decimal итого = 0; decimal итогоСоСкидкой = 0;
            var nomQuantums = new Dictionary<string, decimal>();
            if ((form.Order?.Модель == "FBY") || (form.Order?.Модель == "FBS"))
                nomQuantums = await _номенклатура.ПолучитьКвант(form.ТабличнаяЧасть.Select(x => x.Номенклатура.Code).ToList(), cancellationToken);
            var groupedОстатки = наборНаСкладе_Остатки.GroupBy(x => x.НоменклатураId).Select(gr => new { НоменклатураId = gr.Key, Остаток = gr.Sum(s => s.Количество) });
            foreach (var row in form.ТабличнаяЧасть)
            {
                if (groupedОстатки.Any(x => (x.НоменклатураId == row.Номенклатура.Id) && (x.Остаток > 0)))
                {
                    var остатокПоРегистру = groupedОстатки.Where(x => x.НоменклатураId == row.Номенклатура.Id).Sum(x => x.Остаток) / row.Единица.Коэффициент;
                    var можноОтпустить = Math.Max(Math.Min(row.Количество, остатокПоРегистру), 0);
                    if (можноОтпустить > 0)
                    {
                        номСтроки++;
                        decimal ценаСоСкидкой = 0;
                        decimal суммаСоСкидкой = 0;
                        decimal сумма = row.Количество == можноОтпустить ? row.Сумма : (row.Количество * row.Цена);
                        if (withOrder)
                        {
                            var orderItems = form.Order.Items.Where(x => x.НоменклатураId == row.Номенклатура.Id);
                            var orderCount = orderItems.Sum(x => x.Количество);
                            var orderPriceDiscount = orderItems.Sum(x => x.ЦенаСоСкидкой) / orderItems.Count();
                            ценаСоСкидкой = row.Количество == orderCount ? orderPriceDiscount : (row.Количество * orderPriceDiscount / orderCount);
                            суммаСоСкидкой = ценаСоСкидкой * row.Количество;
                        }
                        итого = итого + сумма;
                        итогоСоСкидкой += суммаСоСкидкой;
                        var quantum = (int)nomQuantums.Where(q => q.Key == row.Номенклатура.Id).Select(q => q.Value).FirstOrDefault();
                        var accessoriesList = await _номенклатура.GetAccessoriesList(row.Номенклатура.Id);
                        decimal boxCount = await _номенклатура.ПолучитьКоличествоМест(row.Номенклатура.Id);
                        if (boxCount < 1)
                            boxCount = 1;
                        var sbKvant = new StringBuilder("Квант по ");
                        sbKvant.Append(quantum.ToString("0", CultureInfo.InvariantCulture));
                        sbKvant.Append(" ");
                        sbKvant.Append(row.Единица.Наименование);
                        var sbComplect = new StringBuilder("Комплект из ");
                        sbComplect.Append(accessoriesList.Count.ToString("0", CultureInfo.InvariantCulture));
                        sbComplect.Append("-х товаров");
                        var sbMultiBox = new StringBuilder("Товар из ");
                        sbMultiBox.Append(boxCount.ToString("0", CultureInfo.InvariantCulture));
                        sbMultiBox.Append("-х частей");
                        var isMultiLine = (quantum > 1) || (accessoriesList.Count > 1) || (boxCount > 1);
                        таблЧасть.Add(new
                        {
                            Ном = номСтроки.ToString("0", CultureInfo.InvariantCulture),
                            Товар = (quantum > 1) ? sbKvant.ToString() :
                                accessoriesList.Count > 1 ? sbComplect.ToString() :
                                boxCount > 1 ? sbMultiBox.ToString() :
                                row.Номенклатура.ПолнНаименование,
                            ТоварЧек = row.Номенклатура.ПолнНаименование,
                            Производитель = isMultiLine ? "" : row.Номенклатура.Производитель.Наименование,
                            Артикул = isMultiLine ? "" : row.Номенклатура.Артикул,
                            КолВо = isMultiLine ? 0 : можноОтпустить,
                            Количество = isMultiLine ? "" : можноОтпустить.ToString("0", CultureInfo.InvariantCulture),
                            КоличествоЧек = можноОтпустить.ToString("0", CultureInfo.InvariantCulture),
                            Единица = isMultiLine ? "" : row.Единица.Наименование,
                            ЕдиницаЧек = row.Единица.Наименование,
                            КолМест = !withOrder ? row.Цена.ToString("0.00", CultureInfo.InvariantCulture) : ((accessoriesList.Count > 1) || (boxCount > 1)) ? (boxCount * можноОтпустить).ToString("0", CultureInfo.InvariantCulture) :
                                можноОтпустить.ToString("0", CultureInfo.InvariantCulture),
                            КолЭтикеток = !withOrder ? сумма.ToString("0.00", CultureInfo.InvariantCulture) : (((quantum > 1) && (можноОтпустить >= quantum)) ? Math.Round(можноОтпустить / quantum, MidpointRounding.AwayFromZero) :
                                isSingleBox ? можноОтпустить : (можноОтпустить * boxCount)).ToString("0", CultureInfo.InvariantCulture),
                            Цена = row.Цена.ToString("0.00", CultureInfo.InvariantCulture),
                            ЦенаСоСкидкой = ценаСоСкидкой.ToString("0.00", CultureInfo.InvariantCulture),
                            Сумма = сумма.ToString("0.00", CultureInfo.InvariantCulture),
                            СуммаСоСкидкой = суммаСоСкидкой.ToString("0.00", CultureInfo.InvariantCulture)
                        });
                        if (quantum > 1)
                            таблЧасть.Add(new
                            {
                                Ном = "",
                                Товар = pref + row.Номенклатура.Наименование,
                                ТоварЧек = "",
                                Производитель = row.Номенклатура.Производитель.Наименование,
                                Артикул = row.Номенклатура.Артикул,
                                КолВо = можноОтпустить,
                                Количество = можноОтпустить.ToString("0", CultureInfo.InvariantCulture),
                                КоличествоЧек = "",
                                Единица = row.Единица.Наименование,
                                ЕдиницаЧек = "",
                                КолМест = "",
                                КолЭтикеток = "",
                                Цена = "",
                                ЦенаСоСкидкой = "",
                                Сумма = "",
                                СуммаСоСкидкой = ""
                            });
                        if (accessoriesList.Count > 1)
                            foreach (var comp in accessoriesList)
                                таблЧасть.Add(new
                                {
                                    Ном = "",
                                    Товар = pref + comp.Key.Наименование,
                                    ТоварЧек = "",
                                    Производитель = comp.Key.Производитель.Наименование,
                                    Артикул = comp.Key.Артикул,
                                    КолВо = comp.Value * можноОтпустить,
                                    Количество = (comp.Value * можноОтпустить).ToString("0", CultureInfo.InvariantCulture),
                                    КоличествоЧек = "",
                                    Единица = comp.Key.Единица.Наименование,
                                    ЕдиницаЧек = "",
                                    КолМест = "",
                                    КолЭтикеток = "",
                                    Цена = "",
                                    ЦенаСоСкидкой = "",
                                    Сумма = "",
                                    СуммаСоСкидкой = ""
                                });
                        if ((accessoriesList.Count <= 1) && (boxCount > 1))
                        {
                            таблЧасть.Add(new
                            {
                                Ном = "",
                                Товар = pref + row.Номенклатура.Наименование,
                                ТоварЧек = "",
                                Производитель = row.Номенклатура.Производитель.Наименование,
                                Артикул = row.Номенклатура.Артикул,
                                КолВо = можноОтпустить,
                                Количество = можноОтпустить.ToString("0", CultureInfo.InvariantCulture),
                                КоличествоЧек = "",
                                Единица = row.Единица.Наименование,
                                ЕдиницаЧек = "",
                                КолМест = "",
                                КолЭтикеток = "",
                                Цена = "",
                                ЦенаСоСкидкой = "",
                                Сумма = "",
                                СуммаСоСкидкой = ""
                            });
                            for (int i = 2; i <= boxCount; i++)
                                таблЧасть.Add(new
                                {
                                    Ном = "",
                                    Товар = pref + "Доп. место № " + (i - 1).ToString("0", CultureInfo.InvariantCulture),
                                    ТоварЧек = "",
                                    Производитель = "",
                                    Артикул = "",
                                    КолВо = можноОтпустить,
                                    Количество = можноОтпустить.ToString("0", CultureInfo.InvariantCulture),
                                    КоличествоЧек = "",
                                    Единица = row.Единица.Наименование,
                                    ЕдиницаЧек = "",
                                    КолМест = "",
                                    КолЭтикеток = "",
                                    Цена = "",
                                    ЦенаСоСкидкой = "",
                                    Сумма = "",
                                    СуммаСоСкидкой = ""
                                });
                        }
                        groupedОстатки = groupedОстатки
                            .Select(item => new
                            {
                                НоменклатураId = item.НоменклатураId,
                                Остаток = item.НоменклатураId == row.Номенклатура.Id ? item.Остаток - (можноОтпустить * row.Единица.Коэффициент) : item.Остаток
                            });
                    }
                }
            }
            var barcodeText = form.Общие.IdDoc13.Replace(' ', '%');
            var data = Enumerable.Repeat(new
            {
                BarcodeStart = Convert.ToBase64String(PdfHelper.PdfFunctions.Instance.GenerateQRCode(barcodeText, 2)),
                BarcodeEnd = Convert.ToBase64String(PdfHelper.PdfFunctions.Instance.GenerateQRCode(barcodeText + "0", 2)),
                Название = form.Завершен ? "Готов" : "Набор",
                НомерДок = form.Общие.НомерДок,
                ДатаДок = form.Общие.ДатаДок.ToString("dd.MM.yyyy"),
                Маршрут = form.Маршрут?.Наименование ?? "",
                КолДокументов = "Док-тов = " + form.ТабличнаяЧасть.Select(x => x.ФирмаНоменклатуры.Id).Distinct().Count().ToString(),
                OrderId = form.Order?.Id ?? "",
                Тип = form.Order?.Тип,
                OrderNo = (form.Order?.MarketplaceId ?? "") + 
                    (form.Order?.Тип == "ALIEXPRESS" ? " / " + (form.Order?.DeliveryServiceName ?? "") :
                     form.Order?.Тип == "WILDBERRIES" ? " / " + (form.Order?.DeliveryServiceId ?? "") + " / " + (form.Order?.RegionId ?? "") : ""),
                Поставщик = form.Общие.Фирма.Наименование,
                Покупатель = form.Контрагент?.Наименование ?? "",
                Склад = (form.Склад?.Наименование ?? "") + "/" + (form.ПодСклад?.Наименование ?? ""),
                КолонкаЦена = withOrder ? "Места" : "Цена",
                КолонкаСумма = withOrder ? "Этикетки" : "Сумма",
                ТаблЧасть = таблЧасть,
                Итого = итого.ToString("0.00", CultureInfo.InvariantCulture),
                ИтогоСоСкидкой = итогоСоСкидкой.ToString("0.00", CultureInfo.InvariantCulture),
                КолСтрок = номСтроки.ToString("0", CultureInfo.InvariantCulture),
                СуммаПрописью = итого.Прописью(),
                СуммаПрописьюСоСкидкой = итогоСоСкидкой.Прописью(),
                ФирмаНаименование = form.Общие.Фирма.ЮрЛицо.Наименование,
                ФирмаАдрес = form.Общие.Фирма.ЮрЛицо.Адрес,
                ФирмаИНН = form.Общие.Фирма.ЮрЛицо.ИНН,
                ФирмаСайт = "https://stinmarket.ru",
                ФирмаEmail = "omfbs@yandex.ru",
                ФирмаТелефон = "+7-927-768-97-89",
                КлиентНаименование = form.Order?.Recipient?.Recipient ?? "",
                КлиентАдрес = form.Order?.Address?.Street ?? "",
                КлиентТелефон = form.Order?.Recipient?.Phone ?? "",
                ИтКолВо = таблЧасть.Sum(x => x.КолВо).ToString("0", CultureInfo.InvariantCulture),
                СуммаНДС = form.Общие.Фирма.ЮрЛицо.УчитыватьНДС == 1 ? "В том числе НДС " + (итого * 0.166666666666666666666666666667m).ToString("0.00", CultureInfo.InvariantCulture) + " руб." : "Без НДС",
                СуммаНДСсоСкидкой = form.Общие.Фирма.ЮрЛицо.УчитыватьНДС == 1 ? "В том числе НДС " + (итогоСоСкидкой * 0.166666666666666666666666666667m).ToString("0.00", CultureInfo.InvariantCulture) + " руб." : "Без НДС"
            }, 1).FirstOrDefault();

            docOnPage++;
            html = html.CreateOrUpdateHtmlPrintPage("Набор", data, docOnPage > totalPerPage);
            if (docOnPage > totalPerPage)
                docOnPage = 1;
            if (data.Тип == "SBER")
            {
                data.ТаблЧасть.RemoveAll(x => string.IsNullOrEmpty(x.ТоварЧек));
                docOnPage++;
                html = html.CreateOrUpdateHtmlPrintPage("ТоварныйЧек", data, docOnPage > totalPerPage);
                if (docOnPage > totalPerPage)
                    docOnPage = 1;
            }

            return (html, docOnPage);
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
        public bool IsActive(string idDoc)
        {
            DateTime dateRegTA = _context.GetRegTA();
            return _context.Rg11973s
                .Where(x => x.Period == dateRegTA && x.Sp11970 == idDoc)
                .Sum(x => x.Sp11972) > 0;
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
        public async Task ОбновитьКладовщика(ФормаНабор doc, string кладовщикId)
        {
            if (doc.Кладовщик?.Id != кладовщикId)
            {
                if (string.IsNullOrEmpty(кладовщикId))
                    кладовщикId = Common.ПустоеЗначение;
                doc.Кладовщик = await _кладовщик.GetКладовщикByIdAsync(кладовщикId);
            }
        }
    }
}
