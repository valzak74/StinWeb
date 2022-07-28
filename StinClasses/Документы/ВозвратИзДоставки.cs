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
    public class ФормаВозвратИзДоставки
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Склад СкладКуда { get; set; }
        public Склад СкладОткуда { get; set; }
        public ПодСклад ПодСкладОткуда { get; set; }
        public List<ФормаВозвратИзДоставкиТЧ> ТабличнаяЧасть { get; set; }
        public ФормаВозвратИзДоставки()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаВозвратИзДоставкиТЧ>();
        }
    }
    public class ФормаВозвратИзДоставкиТЧ
    {
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public Единица Единица { get; set; }
    }
    public interface IВозвратИзДоставки : IДокумент
    {
        Task<ФормаВозвратИзДоставки> ВводНаОснованииAsync(ФормаПеремещениеТМЦ докОснование, DateTime docDateTime);
        Task<ExceptionData> ЗаписатьAsync(ФормаВозвратИзДоставки doc);
        Task<ExceptionData> ПровестиAsync(ФормаВозвратИзДоставки doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаВозвратИзДоставки doc);
    }
    public class ВозвратИзДоставки : Документ, IВозвратИзДоставки
    {
        private IРегистрОстаткиТМЦ _регистрОстаткиТМЦ;
        private IРегистрОстаткиДоставки _регистрОстаткиДоставки;
        public ВозвратИзДоставки(StinDbContext context) : base(context)
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
        public async Task<ФормаВозвратИзДоставки> ВводНаОснованииAsync(ФормаПеремещениеТМЦ докОснование, DateTime docDateTime)
        {
            ФормаВозвратИзДоставки doc = new ФормаВозвратИзДоставки();
            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = await _пользователь.GetUserByIdAsync(Common.UserRobot);
            doc.Общие.ВидДокумента10 = (int)ВидДокумента.ВозвратИзДоставки;
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = докОснование.Общие.Фирма;
            doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;
            doc.Общие.Комментарий = "автоматически созданный документ";

            doc.СкладКуда = докОснование.СкладПолучатель;
            doc.СкладОткуда = докОснование.Склад;
            doc.ПодСкладОткуда = докОснование.ПодСклад;


            foreach (var row in докОснование.ТабличнаяЧасть)
            {
                doc.ТабличнаяЧасть.Add(new ФормаВозвратИзДоставкиТЧ
                {
                    Номенклатура = row.Номенклатура,
                    Единица = row.Единица,
                    Количество = row.Количество
                });
            }

            return doc;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаВозвратИзДоставки doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(0, 0, 1913, doc.Общие.ВидДокумента10, null, "ВозвратИзДоставки",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.СкладОткуда.Наименование,
                    "");
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh8724 docHeader = new Dh8724
                {
                    Iddoc = j.Iddoc,
                    Sp8725 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp8726 = doc.СкладКуда.Id,
                    Sp8735 = doc.СкладОткуда.Id,
                    Sp8727 = Common.ВалютаРубль,
                    Sp8728 = 1, //Курс
                    Sp8978 = doc.ПодСкладОткуда.Id,
                    Sp8733 = 0, //Сумма
                    Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий,
                };
                await _context.Dh8724s.AddAsync(docHeader);

                short lineNo = 1;
                foreach (var item in doc.ТабличнаяЧасть)
                {
                    Dt8724 docRow = new Dt8724
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp8729 = item.Номенклатура.Id,
                        Sp8730 = item.Количество,
                        Sp8731 = item.Единица.Id,
                        Sp8732 = item.Единица.Коэффициент,
                        Sp8733 = 0, //Сумма
                        Sp8734 = 0, //Цена
                    };
                    await _context.Dt8724s.AddAsync(docRow);
                }
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await ОбновитьTotals(doc.Общие.ВидДокумента10, j.Iddoc);
                if (doc.Общие.ДокОснование != null)
                    await ОбновитьПодчиненныеДокументы(doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
                //склад
                await ОбновитьГрафыОтбора(4747, Common.Encode36(55).PadLeft(4) + doc.СкладОткуда.Id, j.DateTimeIddoc, j.Iddoc);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаВозвратИзДоставки doc)
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

                var заявкаIds = new List<string> { doc.Общие.ДокОснование.IdDoc };
                var остаткиДоставки = await _регистрОстаткиДоставки.ПолучитьОстаткиПоЗаявкамAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, заявкаIds);
                string message = "";
                foreach (var row in doc.ТабличнаяЧасть)
                {
                    decimal остаток = остаткиДоставки
                        .Where(x =>
                            x.ФирмаId == doc.Общие.Фирма.Id &&
                            x.СкладКудаId == doc.СкладКуда.Id &&
                            x.НоменклатураId == row.Номенклатура.Id &&
                            !x.ЭтоИзделие)
                        .Sum(x => x.Количество);
                    if ((row.Количество * row.Единица.Коэффициент) > остаток)
                    {
                        if (!string.IsNullOrEmpty(message))
                            message += Environment.NewLine;
                        message += "В доставке нет нужного свободного количества ТМЦ ";
                        if (!string.IsNullOrEmpty(row.Номенклатура.Артикул))
                            message += "(" + row.Номенклатура.Артикул + ") ";
                        if (!string.IsNullOrEmpty(row.Номенклатура.Наименование))
                            message += row.Номенклатура.Наименование;
                        else
                            message += "'" + row.Номенклатура.Id + "'";
                    }
                    else
                    {
                        КоличествоДвижений++;
                        j.Rf8696 = await _регистрОстаткиДоставки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                            doc.Общие.Фирма.Id, row.Номенклатура.Id, doc.СкладКуда.Id, doc.Общие.ДокОснование.Значение, false, (row.Количество * row.Единица.Коэффициент), false);
                        КоличествоДвижений++;
                        j.Rf405 = await _регистрОстаткиТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                            doc.Общие.Фирма.Id, row.Номенклатура.Id, doc.СкладОткуда.Id, doc.ПодСкладОткуда.Id, 0, (row.Количество * row.Единица.Коэффициент), 0);
                    }
                }
                if (!string.IsNullOrEmpty(message))
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = message };
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаВозвратИзДоставки doc)
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
