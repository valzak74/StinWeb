using Microsoft.Data.SqlClient;
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
    public class ФормаПеремещениеТМЦ
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Склад Склад { get; set; }
        public ПодСклад ПодСклад { get; set; }
        public Склад СкладПолучатель { get; set; }
        public ПодСклад ПодСкладКуда { get; set; }
        public Маршрут Маршрут { get; set; }
        public List<ФормаПеремещениеТМЦТЧ> ТабличнаяЧасть { get; set; }
        public ФормаПеремещениеТМЦ()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаПеремещениеТМЦТЧ>();
        }
    }
    public class ФормаПеремещениеТМЦТЧ
    {
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public Единица Единица { get; set; }
    }
    public interface IПеремещениеТМЦ : IДокумент
    {
        Task<ФормаПеремещениеТМЦ> GetФормаПеремещенияТмцById(string idDoc);
        Task<List<ФормаПеремещениеТМЦ>> ДляЗаказаЗаявки(ФормаЗаявкаПокупателя докОснование, DateTime docDateTime, string firmaId, string складId);
        Task<ExceptionData> ЗаписатьAsync(ФормаПеремещениеТМЦ doc);
        Task<ExceptionData> ПровестиAsync(ФормаПеремещениеТМЦ doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПеремещениеТМЦ doc);
        Task<List<ФормаПеремещениеТМЦ>> ПолучитьСписокАктивныхПеремещений(List<ФормаЗаявкаПокупателя> списокОснований);
    }
    public class ПеремещениеТМЦ : Документ, IПеремещениеТМЦ
    {
        private IРегистрОстаткиТМЦ _регистрОстаткиТМЦ;
        private IРегистрОстаткиДоставки _регистрОстаткиДоставки;
        public ПеремещениеТМЦ(StinDbContext context) : base(context)
        {
            _регистрОстаткиТМЦ = new Регистр_ОстаткиТМЦ(context);
            _регистрОстаткиДоставки = new Регистр_ОстаткиДоставки(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрОстаткиТМЦ.Dispose();
                    _регистрОстаткиДоставки.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаПеремещениеТМЦ> GetФормаПеремещенияТмцById(string idDoc)
        {
            var d = await (from dh in _context.Dh1628s
                           join j in _context._1sjourns on dh.Iddoc equals j.Iddoc
                           where dh.Iddoc == idDoc
                           select new
                           {
                               dh,
                               j
                           }).FirstOrDefaultAsync();

            var doc = new ФормаПеремещениеТМЦ
            {
                Общие = new ОбщиеРеквизиты
                {
                    IdDoc = d.dh.Iddoc,
                    ДокОснование = !(string.IsNullOrWhiteSpace(d.dh.Sp1619) || d.dh.Sp1619 == Common.ПустоеЗначениеИд13) ? await ДокОснованиеAsync(d.dh.Sp1619.Substring(4)) : null,
                    Фирма = await _фирма.GetEntityByIdAsync(d.j.Sp4056),
                    Автор = await _пользователь.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НазваниеВЖурнале = "Перемещение ТМЦ" + (Common.GetКодОперацииName(d.dh.Sp8694) == "Внутреннее перемещение с доставкой" ? " (с доставкой)" : ""),
                    НомерДок = d.j.Docno,
                    ДатаДок = d.j.DateTimeIddoc.ToDateTime(),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh.Sp660,
                    Удален = d.j.Ismark
                },
                Склад = await _склад.GetEntityByIdAsync(d.dh.Sp3078),
                ПодСклад = await _склад.GetПодСкладByIdAsync(d.dh.Sp8991),
                СкладПолучатель = await _склад.GetEntityByIdAsync(d.dh.Sp1615),
                ПодСкладКуда = await _склад.GetПодСкладByIdAsync(d.dh.Sp8992),
                Маршрут = await _маршрут.GetМаршрутByCodeAsync(d.dh.Sp11565),
            };
            var ТаблЧасть = await _context.Dt1628s
                .Where(x => x.Iddoc == idDoc)
                .ToListAsync();
            foreach (var row in ТаблЧасть)
            {
                doc.ТабличнаяЧасть.Add(new ФормаПеремещениеТМЦТЧ
                {
                    Номенклатура = await _номенклатура.GetНоменклатураByIdAsync(row.Sp1620),
                    Количество = row.Sp1621,
                    Единица = await _номенклатура.GetЕдиницаByIdAsync(row.Sp1622),
                });
            }
            return doc;
        }
        public async Task<List<ФормаПеремещениеТМЦ>> ДляЗаказаЗаявки(ФормаЗаявкаПокупателя докОснование, DateTime docDateTime, string firmaId, string складId)
        {
            var СкладОтгрузки = string.IsNullOrEmpty(складId) ? докОснование.Склад : await _склад.GetEntityByIdAsync(складId);
            var СписокПодСкладов = _склад.ПолучитьПодСклады(СкладОтгрузки.Id).ToList();
            Фирма фирмаDoc = докОснование.Общие.Фирма;
            if (!string.IsNullOrEmpty(firmaId))
                фирмаDoc = await _фирма.GetEntityByIdAsync(firmaId);
            List<string> СписокФирм = new List<string> { фирмаDoc.Id };
            List<string> СписокНоменклатуры = докОснование.ТабличнаяЧасть.Where(x => !x.Номенклатура.ЭтоУслуга).Select(x => x.Номенклатура.Id).ToList();
            var номенклатура_Остатки = await _регистрОстаткиТМЦ.ПолучитьОстаткиAsync(DateTime.Now, null, false,
                СписокФирм, СписокНоменклатуры, СкладОтгрузки.Id);

            Dictionary<ПодСклад, List<ФормаПеремещениеТМЦТЧ>> ПереченьНаличия = new Dictionary<ПодСклад, List<ФормаПеремещениеТМЦТЧ>>();
            string message = "";
            foreach (var строкаДокОснования in докОснование.ТабличнаяЧасть.Where(x => !x.Номенклатура.ЭтоУслуга))
            {
                decimal Отпустить = строкаДокОснования.Количество * строкаДокОснования.Единица.Коэффициент;
                if (номенклатура_Остатки.Where(x => x.ФирмаId == фирмаDoc.Id && x.СкладId == СкладОтгрузки.Id && x.НоменклатураId == строкаДокОснования.Номенклатура.Id).Sum(x => x.Количество) > 0)
                {
                    foreach (var подСклад in СписокПодСкладов)
                    {
                        var остатокПодСклада = номенклатура_Остатки.Where(x => x.ФирмаId == фирмаDoc.Id && x.СкладId == СкладОтгрузки.Id && x.ПодСкладId == подСклад.Id && x.НоменклатураId == строкаДокОснования.Номенклатура.Id).Sum(x => x.Количество);
                        var МожноОтпустить = Math.Min(остатокПодСклада, Отпустить);
                        if (МожноОтпустить > 0)
                        {
                            if (!ПереченьНаличия.ContainsKey(подСклад))
                                ПереченьНаличия.Add(подСклад, new List<ФормаПеремещениеТМЦТЧ>());
                            ПереченьНаличия[подСклад].Add(new ФормаПеремещениеТМЦТЧ
                            {
                                Номенклатура = строкаДокОснования.Номенклатура,
                                Количество = МожноОтпустить / строкаДокОснования.Единица.Коэффициент,
                                Единица = строкаДокОснования.Единица,
                            });
                            Отпустить = Отпустить - МожноОтпустить;
                            if (Отпустить <= 0)
                                break;
                        }
                    }
                }
                if (Отпустить > 0)
                {
                    if (!string.IsNullOrEmpty(message))
                        message += Environment.NewLine;
                    message += "На складе нет нужного свободного количества ТМЦ ";
                    if (!string.IsNullOrEmpty(строкаДокОснования.Номенклатура.Артикул))
                        message += "(" + строкаДокОснования.Номенклатура.Артикул + ") ";
                    if (!string.IsNullOrEmpty(строкаДокОснования.Номенклатура.Наименование))
                        message += строкаДокОснования.Номенклатура.Наименование;
                    else
                        message += "'" + строкаДокОснования.Номенклатура.Id + "'";
                }
            }
            if (!string.IsNullOrEmpty(message))
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new List<ФормаПеремещениеТМЦ> { new ФормаПеремещениеТМЦ { Ошибка = new ExceptionData { Description = message } } };
            }
            var result = new List<ФормаПеремещениеТМЦ>();

            //var колДнейНаПеремещение = await _склад.ЭтоРабочийДень(складId, DateTime.Now.TimeOfDay > TimeSpan.Parse("16:00") ? 1 : 0);
            //var датаПеремещения = DateTime.Today.AddDays(колДнейНаПеремещение);
            string маршрутКод = докОснование.Маршрут?.Наименование; //_графикМаршрутов.ПолучитьКодМаршрута(датаПеремещения, "50"); //направление 50 == Самара Преображенка

            foreach (var данные in ПереченьНаличия)
            {
                ФормаПеремещениеТМЦ doc = new ФормаПеремещениеТМЦ();
                doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
                doc.Общие.Автор = докОснование.Общие.Автор;
                doc.Общие.ВидДокумента10 = (int)ВидДокумента.ПеремещениеТМЦ;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.Фирма = фирмаDoc;
                doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;
                doc.Общие.Комментарий = докОснование.Order != null ? "Для интернет-заказа " + докОснование.Order.OrderNo : "";

                doc.Склад = СкладОтгрузки;
                doc.СкладПолучатель = докОснование.Склад;
                doc.ПодСклад = данные.Key;

                if (!string.IsNullOrEmpty(маршрутКод))
                {
                    Маршрут маршрут = _маршрут.НовыйЭлемент();
                    маршрут.Наименование = маршрутКод;
                    doc.Маршрут = маршрут;
                }
                foreach (var строка in данные.Value)
                {
                    doc.ТабличнаяЧасть.Add(new ФормаПеремещениеТМЦТЧ
                    {
                        Номенклатура = строка.Номенклатура,
                        Единица = строка.Единица,
                        Количество = строка.Количество,
                    });
                }
                result.Add(doc);
            }
            return result;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаПеремещениеТМЦ doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(_context, 1913, doc.Общие.ВидДокумента10, null, "ПеремещениеТМЦ",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    "");

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh1628 docHeader = new Dh1628
                {
                    Iddoc = j.Iddoc,
                    Sp1619 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp3078 = doc.Склад.Id,
                    Sp4855 = doc.Общие.Фирма.Id,
                    Sp1615 = doc.СкладПолучатель.Id,
                    Sp1612 = Common.ВалютаРубль,
                    Sp1613 = 1, //Курс
                    Sp1616 = Common.ПустоеЗначение, //тип цен
                    Sp3910 = 0, //учитывать ндс
                    Sp3911 = 1, //сумма вкл ндс
                    Sp3912 = 0, //учитывать нп
                    Sp3913 = 0, //сумма вкл нп
                    Sp8694 = Common.GetКодОперацииId("Внутреннее перемещение с доставкой"),
                    Sp8991 = doc.ПодСклад.Id,
                    Sp8992 = doc.ПодСкладКуда != null ? doc.ПодСкладКуда.Id : Common.ПустоеЗначение, 
                    Sp8876 = 0, //СтатусДокумента
                    Sp10265 = Common.ПустоеЗначение, //Водитель
                    Sp11565 = doc.Маршрут != null ? doc.Маршрут.Code : "", //ИндМаршрута
                    Sp11566 = doc.Маршрут != null ? doc.Маршрут.Наименование : "", //НомерМаршрута
                    Sp1625 = 0, //Сумма
                    Sp3915 = 0, //СуммаНДС
                    Sp3917 = 0, //СуммаНП
                    Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий,
                };
                await _context.Dh1628s.AddAsync(docHeader);

                short lineNo = 1;
                foreach (var item in doc.ТабличнаяЧасть)
                {
                    Dt1628 docRow = new Dt1628
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp1620 = item.Номенклатура.Id,
                        Sp1621 = item.Количество,
                        Sp1622 = item.Единица.Id,
                        Sp1623 = item.Единица.Коэффициент,
                        Sp1624 = 0,
                        Sp1625 = 0,
                        Sp3914 = Common.ПустоеЗначение,
                        Sp3915 = 0,
                        Sp3916 = Common.ПустоеЗначение,
                        Sp3917 = 0,
                        Sp1626 = Common.ПустоеЗначение,
                    };
                    await _context.Dt1628s.AddAsync(docRow);
                }
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync(
                    "exec _1sp_DH1628_UpdateTotals @num36",
                    new SqlParameter("@num36", j.Iddoc)
                    );
                await _context.SaveChangesAsync();
                if (doc.Общие.ДокОснование != null)
                    await ОбновитьПодчиненныеДокументы(doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
                //склад
                await ОбновитьГрафыОтбора(4747, Common.Encode36(55).PadLeft(4) + doc.Склад.Id, j.DateTimeIddoc, j.Iddoc);
                //контрагент
                //await ОбновитьГрафыОтбора(862, Common.Encode36(172).PadLeft(4) + doc.Контрагент.Id, j.DateTimeIddoc, j.Iddoc);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаПеремещениеТМЦ doc)
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

                List<string> СписокФирм = new List<string> { doc.Общие.Фирма.Id };
                List<string> СписокСкладов = new List<string>() { doc.Склад.Id };
                List<string> СписокНоменклатуры = doc.ТабличнаяЧасть.Select(x => x.Номенклатура.Id).ToList();

                var номенклатура_Остатки = await _регистрОстаткиТМЦ.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false,
                    СписокФирм, СписокНоменклатуры, doc.Склад.Id, doc.ПодСклад.Id);
                string message = "";
                foreach (var row in doc.ТабличнаяЧасть)
                {
                    decimal Отпустить = row.Количество * row.Единица.Коэффициент;
                    if (Отпустить > 0)
                    {
                        var ОстатокНаПодСкладе = номенклатура_Остатки.Where(x => x.ФирмаId == doc.Общие.Фирма.Id &&
                            x.СкладId == doc.Склад.Id && x.ПодСкладId == doc.ПодСклад.Id && x.НоменклатураId == row.Номенклатура.Id)
                            .Sum(x => x.Количество);
                        if (ОстатокНаПодСкладе >= Отпустить)
                        {
                            КоличествоДвижений++;
                            j.Rf405 = await _регистрОстаткиТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                doc.Общие.Фирма.Id, row.Номенклатура.Id, doc.Склад.Id, doc.ПодСклад.Id, 0, Отпустить, 0);
                            КоличествоДвижений++;
                            //приход в доставку
                            j.Rf8696 = await _регистрОстаткиДоставки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                                doc.Общие.Фирма.Id, row.Номенклатура.Id, doc.СкладПолучатель.Id, (doc.Общие.ВидДокумента36).PadLeft(4) + doc.Общие.IdDoc, false, Отпустить, false);
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПеремещениеТМЦ doc)
        {
            var result = await ЗаписатьAsync(doc);
            if (result == null)
            {
                result = await ПровестиAsync(doc);
            }
            return result;
        }
        public async Task<List<ФормаПеремещениеТМЦ>> ПолучитьСписокАктивныхПеремещений(List<ФормаЗаявкаПокупателя> списокОснований)
        {
            var заявкиIds13 = списокОснований.Select(x => x.Общие.ВидДокумента36.PadLeft(4) + x.Общие.IdDoc);
            var док = await _context.Dh1628s.Where(x => заявкиIds13.Any(y => y == x.Sp1619))
                .Select(x => x.Iddoc).ToListAsync();
            var остаткиДоставки = await _регистрОстаткиДоставки.ПолучитьОстаткиПоЗаявкамAsync(DateTime.Now, null, false, док);
            List<ФормаПеремещениеТМЦ> result = new List<ФормаПеремещениеТМЦ>();
            foreach (var docGrp in остаткиДоставки.GroupBy(x => new { ФирмаId = x.ФирмаId, СкладКудаId = x.СкладКудаId, ДокПеремещенияId13 = x.ДокПеремещенияId13, ЭтоИзделие = x.ЭтоИзделие }))
            {
                var doc = await GetФормаПеремещенияТмцById(docGrp.Key.ДокПеремещенияId13.Substring(4));
                List<ФормаПеремещениеТМЦТЧ> табЧасть = new List<ФормаПеремещениеТМЦТЧ>();
                foreach (var rowData in docGrp.GroupBy(x => x.НоменклатураId))
                {
                    var номенклатура = await _номенклатура.GetНоменклатураByIdAsync(rowData.Key);
                    var единица = doc.ТабличнаяЧасть.Where(x => x.Номенклатура.Id == номенклатура.Id).Select(x => x.Единица).FirstOrDefault();
                    табЧасть.Add(new ФормаПеремещениеТМЦТЧ
                    {
                        Номенклатура = await _номенклатура.GetНоменклатураByIdAsync(rowData.Key),
                        Единица = единица,
                        Количество = rowData.Sum(x => x.Количество) / единица.Коэффициент
                    });
                }
                doc.ТабличнаяЧасть.Clear();
                doc.ТабличнаяЧасть.AddRange(табЧасть);
                result.Add(doc);
            }
            return result;
        }
    }
}
