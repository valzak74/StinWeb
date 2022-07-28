using StinClasses.Регистры;
using StinClasses.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinClasses.Справочники;

namespace StinClasses.Документы
{
    public class ФормаСпрос
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public KeyValuePair<string, string> ВидОперации { get; set; }
        public БанковскийСчет БанковскийСчет { get; set; }
        public Склад Склад { get; set; }
        public Контрагент Контрагент { get; set; }
        public Договор Договор { get; set; }
        public bool УчитыватьНДС { get; set; }
        public ТипЦен ТипЦен { get; set; }
        public Скидка Скидка { get; set; }
        public СкидКарта СкидКарта { get; set; }
        public DateTime ДатаОплаты { get; set; }
        public DateTime ДатаОтгрузки { get; set; }
        public string СпособОтгрузки { get; set; }
        public Маршрут Маршрут { get; set; }
        public List<ФормаСпросТЧ> ТабличнаяЧасть { get; set; }
        public ФормаСпрос()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаСпросТЧ>();
        }
    }
    public class ФормаСпросТЧ
    {
        public Номенклатура Номенклатура { get; set; }
        public decimal Количество { get; set; }
        public Единица Единица { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
        public СтавкаНДС СтавкаНДС { get; set; }
        public decimal СуммаНДС { get; set; }
    }
    public interface IСпрос : IДокумент
    {
        Task<ФормаСпрос> ВводНаОснованииAsync(ФормаПредварительнаяЗаявка докОснование, DateTime docDateTime, List<ФормаПредварительнаяЗаявкаТЧ> таблицаОснования);
        Task<ExceptionData> ЗаписатьAsync(ФормаСпрос doc);
        Task<ExceptionData> ПровестиAsync(ФормаСпрос doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаСпрос doc);
    }
    public class Спрос : Документ, IСпрос
    {
        private IРегистрСпросОстатки _регистрСпросОстатки;
        public Спрос(StinDbContext context) : base(context)
        {
            _регистрСпросОстатки = new Регистр_СпросОстатки(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрСпросОстатки.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаСпрос> ВводНаОснованииAsync(ФормаПредварительнаяЗаявка докОснование, DateTime docDateTime, List<ФормаПредварительнаяЗаявкаТЧ> таблицаОснования)
        {
            ФормаСпрос doc = new ФормаСпрос();
            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = 12784;
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;

            doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;

            doc.Общие.Комментарий = string.IsNullOrEmpty(докОснование.Общие.Комментарий) ? "" : докОснование.Общие.Комментарий.Trim();

            doc.БанковскийСчет = докОснование.БанковскийСчет;
            doc.Склад = докОснование.Склад;
            doc.Контрагент = докОснование.Контрагент;
            doc.Договор = докОснование.Договор;
            doc.УчитыватьНДС = докОснование.УчитыватьНДС;
            doc.ТипЦен = докОснование.ТипЦен;
            doc.Скидка = докОснование.Скидка;
            doc.ДатаОплаты = докОснование.ДатаОплаты;
            doc.ДатаОтгрузки = докОснование.ДатаОтгрузки;
            doc.СкидКарта = докОснование.СкидКарта;
            doc.СпособОтгрузки = докОснование.СпособОтгрузки;
            doc.Маршрут = докОснование.Маршрут;

            var таблицаДокумента = (таблицаОснования != null && таблицаОснования.Count > 0) ? таблицаОснования : докОснование.ТабличнаяЧасть;
            foreach (var строкаДокОснования in таблицаДокумента)
            {
                doc.ТабличнаяЧасть.Add(new ФормаСпросТЧ
                {
                    Номенклатура = строкаДокОснования.Номенклатура,
                    Единица = строкаДокОснования.Единица,
                    Количество = строкаДокОснования.Количество,
                    Цена = строкаДокОснования.Цена,
                    СтавкаНДС = строкаДокОснования.СтавкаНДС,
                    Сумма = строкаДокОснования.Сумма,
                    СуммаНДС = строкаДокОснования.СуммаНДС
                });
            }
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаСпрос doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(0, 0, 4588, doc.Общие.ВидДокумента10, null, "Спрос",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    doc.Контрагент.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh12784 docHeader = new Dh12784
                {
                    Iddoc = j.Iddoc,
                    Sp12748 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp12749 = doc.БанковскийСчет.Id,
                    Sp12750 = doc.Контрагент.Id,
                    Sp12751 = doc.Договор.Id,
                    Sp12752 = Common.ВалютаРубль,
                    Sp12753 = 1, //Курс
                    Sp12754 = doc.УчитыватьНДС ? 1 : 0,
                    Sp12755 = 1, //СуммаВклНДС
                    Sp12756 = 0, //УчитыватьНП
                    Sp12757 = 0, //СуммаВклНП
                    Sp12758 = doc.ТабличнаяЧасть.Sum(x => x.Сумма), //СуммаВзаиморасчетов
                    Sp12759 = doc.ТипЦен.Id, //ТипЦен
                    Sp12760 = doc.Скидка != null ? doc.Скидка.Id : Common.ПустоеЗначение, //Скидка
                    Sp12761 = doc.ДатаОплаты,
                    Sp12762 = doc.ДатаОтгрузки,
                    Sp12763 = doc.Склад.Id,
                    Sp12764 = Common.СпособыРезервирования.FirstOrDefault(x => x.Value == "Резервировать только из текущего остатка").Key,
                    Sp12765 = doc.СкидКарта == null ? Common.ПустоеЗначение : doc.СкидКарта.Id,
                    Sp12766 = 1, //ПоСтандарту
                    Sp12767 = 0, //ДанаДопСкидка
                    Sp12768 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.СпособОтгрузки).Key,
                    Sp12769 = "", //НомерКвитанции
                    Sp12770 = 0, //ДатаКвитанции
                    Sp12771 = doc.Маршрут != null ? doc.Маршрут.Code : "", //ИндМаршрута
                    Sp12772 = doc.Маршрут != null ? doc.Маршрут.Наименование : "", //НомерМаршрута
                    Sp12778 = 0, //Сумма
                    Sp12780 = 0, //СуммаНДС
                    Sp12782 = 0, //СуммаНП
                    Sp660 = string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий
                };
                await _context.Dh12784s.AddAsync(docHeader);

                short lineNo = 1;
                foreach (var item in doc.ТабличнаяЧасть)
                {
                    СтавкаНДС ставкаНДС = await _номенклатура.GetСтавкаНДСAsync(item.Номенклатура.Id);
                    Dt12784 docRow = new Dt12784
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp12773 = item.Номенклатура.Id,
                        Sp12774 = item.Количество,
                        Sp12775 = item.Единица.Id,
                        Sp12776 = item.Единица.Коэффициент,
                        Sp12777 = item.Цена,
                        Sp12778 = item.Сумма,
                        Sp12779 = ставкаНДС.Id,
                        Sp12780 = item.Сумма * (ставкаНДС.Процент / (100 + ставкаНДС.Процент)),
                        Sp12781 = Common.ПустоеЗначение,
                        Sp12782 = 0,
                    };
                    await _context.Dt12784s.AddAsync(docRow);
                }
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync(
                    "exec _1sp_DH12784_UpdateTotals @num36",
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
        public async Task<ExceptionData> ПровестиAsync(ФормаСпрос doc)
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
                foreach (var row in doc.ТабличнаяЧасть)
                {
                    decimal КоличествоРегистр = row.Количество * row.Единица.Коэффициент;
                    КоличествоДвижений++;
                    j.Rf12791 = await _регистрСпросОстатки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                        row.Номенклатура.Id, doc.Склад.Id, doc.Контрагент.Id, КоличествоРегистр, row.Сумма);
                    КоличествоДвижений++;
                    j.Rf12815 = await _регистрСпросОстатки.ВыполнитьДвижениеОстаткиAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                        doc.Общие.Фирма.Id, row.Номенклатура.Id, doc.Склад.Id, doc.Контрагент.Id, КоличествоРегистр);
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаСпрос doc)
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
