using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using StinClasses.Регистры;
using StinClasses.Справочники;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Документы
{
    public class ФормаПКО
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Касса Касса { get; set; }
        public string КодОперации { get; set; }
        public Контрагент Контрагент { get; set; }
        public Договор Договор { get; set; }
        public decimal Сумма { get; set; }
        public СтавкаНДС СтавкаНДС { get; set; }
        public decimal СуммаОплаты { get; set; }
        public string ПринятоОт { get; set; }
        public string Основание { get; set; }
        public string Приложение { get; set; }
        public ФормаПКО()
        {
            Общие = new ОбщиеРеквизиты();
        }
    }
    public interface IПКО : IДокумент
    {
        Task<ФормаПКО> ВводНаОснованииAsync(string докОснованиеId, string userId);
        Task<ФормаПКО> ВводНаОснованииAsync(ФормаРеализация докОснование, DateTime docDateTime, decimal сумма, ReceiverPaymentType paymentType);
        Task<ExceptionData> ЗаписатьAsync(ФормаПКО doc);
        Task<ExceptionData> ПровестиAsync(ФормаПКО doc, bool групповоеПроведение = false);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПКО doc, ФормаОплатаЧерезЮКасса оплатаЧерезЮКассаDoc, bool групповоеПроведение = false);
    }
    public class ПКО : Документ, IПКО
    {
        private IКасса _касса;
        private IРегистрКасса _регистрКасса;
        private IРегистрПокупатели _регистрПокупатели;
        private IЧекККМ _чекККМ;
        public ПКО(StinDbContext context) : base(context)
        {
            _регистрПокупатели = new Регистр_Покупатели(context);
            _регистрКасса = new Регистр_Касса(context);
            _касса = new КассаEntity(context);
            _чекККМ = new ЧекККМ(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрПокупатели.Dispose();
                    _регистрКасса.Dispose();
                    _касса.Dispose();
                    _чекККМ.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаПКО> ВводНаОснованииAsync(string докОснованиеId, string userId)
        {
            ФормаПКО doc = new ФормаПКО();
            if (NeedToOpenPeriod())
            {
                doc.Ошибка = new ExceptionData { Description = "Период не открыт!" };
            }
            else
            {
                doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснованиеId);
                doc.Общие.Автор = await _пользователь.GetUserByIdAsync(userId);
                doc.Общие.ВидДокумента10 = (int)ВидДокумента.ПКО; //2196;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;

                doc.Общие.ДатаДок = DateTime.Now;
                if (doc.Общие.ДокОснование.ВидДокумента10 == (int)ВидДокумента.ЗаявкаПокупателя) //Счет на оплату
                {
                    var ДокОснование = await GetФормаЗаявкаById(докОснованиеId);
                    if (ДокОснование != null)
                    {
                        doc.Общие.Комментарий = ДокОснование.Общие.Комментарий.Trim();
                        doc.Касса = await _касса.GetYouKassaAsync();
                        doc.КодОперации = Common.GetКодОперацииId("Оплата от покупателя");
                        doc.Контрагент = ДокОснование.Контрагент;
                        doc.Договор = ДокОснование.Договор;
                        doc.Сумма = ДокОснование.ТабличнаяЧасть.Sum(x => x.Сумма);
                        doc.ПринятоОт = ДокОснование.Контрагент.ПолнНаименование;
                        doc.Основание = "аванс по договору-аферте";
                        doc.Приложение = "";
                        doc.СуммаОплаты = doc.Сумма;
                    }
                }
            }
            if (doc.Ошибка == null || doc.Ошибка.Skip)
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, Common.НумераторПКО, 10, doc.Общие.Фирма.Id);
            return doc;
        }
        public async Task<ФормаПКО> ВводНаОснованииAsync(ФормаРеализация докОснование, DateTime docDateTime, decimal сумма, ReceiverPaymentType paymentType)
        {
            ФормаПКО doc = new ФормаПКО();

            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = (int)ВидДокумента.ПКО;
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;
            doc.Общие.Комментарий = string.IsNullOrEmpty(докОснование.Общие.Комментарий) ? "" : докОснование.Общие.Комментарий.Trim();

            doc.Общие.ДатаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;

            doc.Касса = await _касса.GetYouKassaAsync();
            doc.КодОперации = Common.GetКодОперацииId("Оплата от покупателя");
            doc.Контрагент = докОснование.Контрагент;
            doc.Договор = докОснование.Договор;
            doc.Сумма = сумма > 0 ? сумма : докОснование.ТабличнаяЧасть.Sum(x => x.Сумма);
            doc.ПринятоОт = докОснование.Контрагент.ПолнНаименование;
            doc.Основание = "оплата по н/к " + докОснование.Общие.НомерДок + " от " + докОснование.Общие.ДатаДок.ToShortDateString();
            doc.Приложение = "";
            doc.СуммаОплаты = paymentType == ReceiverPaymentType.БанковскойКартой ? doc.Сумма : 0; //КартОплата

            return doc;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаПКО doc)
        {
            try
            {
                if (!string.IsNullOrEmpty(doc.Общие.НомерДок))
                    await UnLockDocNoAsync(Common.НумераторПКО, doc.Общие.НомерДок);
                _1sjourn j = GetEntityJourn(0, 0, 1896, doc.Общие.ВидДокумента10, Common.НумераторПКО, doc.Общие.Наименование,
                    doc.Общие.НомерДок, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    "",
                    doc.Контрагент.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh2196 docHeader = new Dh2196
                {
                    Iddoc = j.Iddoc,
                    Sp2197 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp2201 = doc.Касса != null ? doc.Касса.Id : Common.ПустоеЗначение,
                    Sp3319 = doc.КодОперации,
                    Sp3320 = doc.Контрагент != null ? doc.Контрагент.Id : Common.ПустоеЗначение,
                    Sp3321 = doc.Договор != null ? doc.Договор.Id : Common.ПустоеЗначение,
                    Sp3322 = Common.ПустоеЗначение, //физ лицо
                    Sp2198 = Common.ВалютаРубль,
                    Sp2199 = 1, //Курс
                    Sp2203 = doc.Сумма, 
                    Sp2205 = Common.ПустоеЗначение, //СтавкаНДС
                    Sp2200 = 0, //ОблагаетсяНП
                    Sp2206 = Common.ПустоеЗначение, //СтавкаНП
                    Sp2204 = doc.Сумма,
                    Sp2208 = doc.ПринятоОт.Length > 80 ? doc.ПринятоОт.Substring(0, 80) : doc.ПринятоОт,
                    Sp2207 = doc.Основание.Length > 64 ? doc.Основание.Substring(0, 64) : doc.Основание,
                    Sp2209 = doc.Приложение.Length > 150 ? doc.Приложение.Substring(0, 150) : doc.Приложение,
                    Sp3876 = Common.ПустоеЗначениеИд23,
                    Tsp3876 = Common.ПустоеЗначениеTSP,
                    Sp3877 = Common.ПустоеЗначениеИд23,
                    Tsp3877 = Common.ПустоеЗначениеTSP,
                    Sp3878 = Common.ПустоеЗначениеИд23,
                    Tsp3878 = Common.ПустоеЗначениеTSP,
                    Sp3879 = Common.ПустоеЗначениеИд23,
                    Tsp3879 = Common.ПустоеЗначениеTSP,
                    Sp8015 = Common.ПустоеЗначениеИд23,
                    Tsp8015 = Common.ПустоеЗначениеTSP,
                    Sp8016 = Common.ПустоеЗначениеИд23,
                    Tsp8016 = Common.ПустоеЗначениеTSP,
                    Sp8017 = Common.ПустоеЗначениеИд23,
                    Tsp8017 = Common.ПустоеЗначениеTSP,
                    Sp8018 = Common.ПустоеЗначениеИд23,
                    Tsp8018 = Common.ПустоеЗначениеTSP,
                    Sp4265 = "     2S  ", //поступления от покупателей
                    Sp7819 = 0, //ФлагСвертки
                    Sp8341 = 0, //НомерЧекаККМ
                    Sp13326 = doc.СуммаОплаты, //КартОплата
                    Sp660 = doc.Общие.Комментарий != null ? doc.Общие.Комментарий : ""
                };
                await _context.Dh2196s.AddAsync(docHeader);

                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                //await ОбновитьTotals(doc.Общие.ВидДокумента10, j.Iddoc);
                if (doc.Общие.ДокОснование != null)
                    await ОбновитьПодчиненныеДокументы(doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
                //склад
                //await ОбновитьГрафыОтбора(4747, Common.Encode36(55).PadLeft(4) + doc.Склад.Id, j.DateTimeIddoc, j.Iddoc);
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
        public async Task<ExceptionData> ПровестиAsync(ФормаПКО doc, bool групповоеПроведение = false)
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
                var СписокДоговоров = (await _контрагент.GetAllДоговорыAsync(doc.Контрагент.Id)).Select(x => x.Id).ToList();
                var РегистрПокупатели_Остатки = await _регистрПокупатели.ПолучитьОстаткиAsync(
                    doc.Общие.ДатаДок,
                    doc.Общие.IdDoc,
                    false,
                    new List<string> { doc.Общие.Фирма.Id },
                    СписокДоговоров
                    );
                var ОстПогСумма = doc.Сумма;
                var РегистрПокупателиИтоги = РегистрПокупатели_Остатки.OrderBy(x => x.DateTimeIdDoc);
                foreach (var r in РегистрПокупателиИтоги)
                {
                    if (ОстПогСумма <= 0)
                        break;
                    if (r.СуммаУпр <= 0)
                        continue;
                    decimal КоэффПогашения = 1;
                    decimal Погасить = r.СуммаУпр;
                    if (r.СуммаУпр > ОстПогСумма)
                    {
                        КоэффПогашения = ОстПогСумма / r.СуммаУпр;
                        Погасить = ОстПогСумма;
                    }
                    decimal ПогаситьСебестоимость = Math.Round(r.Себестоимость * КоэффПогашения, 2, MidpointRounding.AwayFromZero);
                    decimal КоэффСписания = Погасить / ОстПогСумма;
                    bool Гасим = false;

                    if ((r.ВидДолгаId == "    B1   ") //долг за товары
                        || (r.ВидДолгаId == "    B2   ")) //долг за товары принятые
                    {
                        КоличествоДвижений++;
                        j.Rf4335 = await _регистрПокупатели.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                            r.ФирмаId, r.ДоговорId, r.ВидДолгаId, r.КредДокументId13,
                            ПогаситьСебестоимость, Погасить, doc.КодОперации, Common.ПустоеЗначение, doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc);
                        Гасим = true;
                    }
                    if (Гасим)
                    {
                        ОстПогСумма = ОстПогСумма - Погасить;
                    }
                }
                if (ОстПогСумма > 0)
                {
                    //Аванс
                    КоличествоДвижений++;
                    j.Rf4335 = await _регистрПокупатели.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        doc.Общие.Фирма.Id, doc.Договор.Id, "    B4   ", doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc,
                        0, ОстПогСумма, doc.КодОперации, Common.ПустоеЗначение, doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc);

                }
                КоличествоДвижений++;
                j.Rf635 = await _регистрКасса.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                    doc.Общие.Фирма.Id, doc.Касса.Id, doc.Сумма, doc.КодОперации, "     2S  ");
                if (doc.СуммаОплаты > 0)
                {
                    КоличествоДвижений++;
                    j.Rf635 = await _регистрКасса.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        doc.Общие.Фирма.Id, doc.Касса.Id, doc.СуммаОплаты, doc.КодОперации, "     2S  ");
                }

                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Ds1946 = 2;

                _context.Update(j);
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                if (!групповоеПроведение)
                {
                    await ОбновитьВремяТА(j.Iddoc, j.DateTimeIddoc);
                    await ОбновитьПоследовательность(j.DateTimeIddoc);
                    await ОбновитьСетевуюАктивность();
                }
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПКО doc, ФормаОплатаЧерезЮКасса оплатаЧерезЮКассаDoc, bool групповоеПроведение = false)
        {
            ExceptionData result = await ЗаписатьAsync(doc);
            if (result == null)
                result = await ПровестиAsync(doc, групповоеПроведение);
            if (result == null && оплатаЧерезЮКассаDoc != null)
            {
                var докЧекККМ = await _чекККМ.ВводНаОснованииAsync(doc,
                    DateTime.Now,
                    ВидыОперацииЧекаККМ.Чек,
                    оплатаЧерезЮКассаDoc.PaymentId,
                    оплатаЧерезЮКассаDoc.Email,
                    оплатаЧерезЮКассаDoc.Телефон.Length > 10 ? оплатаЧерезЮКассаDoc.Телефон.Substring(оплатаЧерезЮКассаDoc.Телефон.Length - 10) : оплатаЧерезЮКассаDoc.Телефон,
                    !string.IsNullOrEmpty(оплатаЧерезЮКассаDoc.Email),
                    !string.IsNullOrEmpty(оплатаЧерезЮКассаDoc.Телефон),
                    true,
                    new List<ФормаЧекКкмТЧ> { new ФормаЧекКкмТЧ { ТипОплаты = ТипыОплатыЧекаККМ.БанковскаяКарта, СуммаОплаты = doc.СуммаОплаты } }
                    );
                if (докЧекККМ.Ошибка == null || докЧекККМ.Ошибка.Skip)
                {
                    result = await _чекККМ.ЗаписатьПровестиAsync(докЧекККМ);
                    if (result != null)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            _context.Database.CurrentTransaction.Rollback();
                        return result;
                    }
                }
            }
            return result;
        }
    }
}
