﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using StinClasses.Регистры;
using StinClasses.Справочники;

namespace StinClasses.Документы
{
    public class ФормаОтчетККМ
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Склад Склад { get; set; }
        public Касса Касса { get; set; }
        public bool ОблагаетсяЕНВД { get; set; }
        public bool УчитыватьНДС { get; set; }
        public bool СуммаВклНДС { get; set; }
        public ТипЦен ТипЦен { get; set; }
        public Контрагент Контрагент { get; set; }
        public Договор Договор { get; set; }
        public Скидка Скидка { get; set; }
        public СкидКарта СкидКарта { get; set; }
        public ПодСклад ПодСклад { get; set; }
        public bool Закрыт { get; set; }
        public decimal Собственный { get; set; }
        public Маршрут Маршрут { get; set; }
        public decimal ВидОплаты { get; set; }
        public decimal СуммаОплаты { get; set; }
        public List<ФормаОтчетКкмТЧ> ТабличнаяЧасть { get; set; }
        public ФормаОтчетККМ()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаОтчетКкмТЧ>();
        }
    }
    public class ФормаОтчетКкмТЧ
    {
        public string НоменклатураId { get; set; }
        public decimal Количество { get; set; }
        public string ЕдиницаId { get; set; }
        public decimal Коэффициент { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
        public СтавкаНДС СтавкаНДС { get; set; }
        public decimal СуммаНДС { get; set; }
    }
    public interface IОтчетККМ : IДокумент
    {
        Task<ФормаОтчетККМ> ВводНаОснованииAsync(ФормаПродажаКасса докОснование, string фирмаId, string подСкладId, List<ФормаОтчетКкмТЧ> отчетКкмТЧ);
        Task<ExceptionData> ЗаписатьAsync(ФормаОтчетККМ doc);
        Task<ExceptionData> ПровестиAsync(ФормаОтчетККМ doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаОтчетККМ doc, ФормаОплатаЧерезЮКасса оплатаЧерезЮКассаDoc);
    }
    public class ОтчетККМ : Документ, IОтчетККМ
    {
        private IРегистрЗаменаНоменклатуры _регистрЗаменаНоменклатуры;
        private IРегистрПродажи _регистрПродажи;
        private IРегистрЗаявки _регистрЗаявки;
        private IРегистрЗаказыЗаявки _регистрЗаказыЗаявки;
        private IРегистрОстаткиТМЦ _регистрОстаткиТМЦ;
        private IРегистрРезервыТМЦ _регистрРезервыТМЦ;
        private IРегистрПартииНаличие _регистрПартииНаличие;
        private IРегистрПокупатели _регистрПокупатели;
        private IРегистрКнигаПродаж _регистрКнигаПродаж;
        private IРегистрРеализованныйТовар _регистрРеализованныйТовар;
        private IРегистрКасса _регистрКасса;
        private IРегистрНакопСкидка _регистрНакопСкидка;
        private IРегистрНаборНаСкладе _регистрНаборНаСкладе;
        private IЧекККМ _чекККМ;
        public ОтчетККМ(StinDbContext context) : base(context)
        {
            _регистрЗаменаНоменклатуры = new Регистр_ЗаменаНоменклатуры(context);
            _регистрПродажи = new Регистр_Продажи(context);
            _регистрЗаявки = new Регистр_Заявки(context);
            _регистрЗаказыЗаявки = new Регистр_ЗаказыЗаявки(context);
            _регистрОстаткиТМЦ = new Регистр_ОстаткиТМЦ(context);
            _регистрРезервыТМЦ = new Регистр_РезервыТМЦ(context);
            _регистрПартииНаличие = new Регистр_ПартииНаличие(context);
            _регистрПокупатели = new Регистр_Покупатели(context);
            _регистрКнигаПродаж = new Регистр_КнигаПродаж(context);
            _регистрРеализованныйТовар = new Регистр_РеализованныйТовар(context);
            _регистрКасса = new Регистр_Касса(context);
            _регистрНакопСкидка = new Регистр_НакопСкидка(context);
            _регистрНаборНаСкладе = new Регистр_НаборНаСкладе(context);
            _чекККМ = new ЧекККМ(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрЗаменаНоменклатуры.Dispose();
                    _регистрПродажи.Dispose();
                    _регистрЗаявки.Dispose();
                    _регистрЗаказыЗаявки.Dispose();
                    _регистрОстаткиТМЦ.Dispose();
                    _регистрРезервыТМЦ.Dispose();
                    _регистрПартииНаличие.Dispose();
                    _регистрПокупатели.Dispose();
                    _регистрКнигаПродаж.Dispose();
                    _регистрРеализованныйТовар.Dispose();
                    _регистрКасса.Dispose();
                    _регистрНакопСкидка.Dispose();
                    _регистрНаборНаСкладе.Dispose();
                    _чекККМ.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаОтчетККМ> ВводНаОснованииAsync(ФормаПродажаКасса докОснование, string фирмаId, string подСкладId, List<ФормаОтчетКкмТЧ> отчетКкмТЧ)
        {
            ФормаОтчетККМ doc = new ФормаОтчетККМ();

            doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснование.Общие.IdDoc);
            doc.Общие.Автор = докОснование.Общие.Автор;
            doc.Общие.ВидДокумента10 = 3114;
            doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
            doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма;

            doc.Общие.ДатаДок = DateTime.Now;

            doc.Общие.Комментарий = докОснование.Общие.Комментарий.Trim();
            doc.Склад = докОснование.Склад;
            doc.Касса = докОснование.Касса;
            doc.УчитыватьНДС = докОснование.УчитыватьНДС;
            doc.СуммаВклНДС = докОснование.СуммаВклНДС;
            doc.ТипЦен = докОснование.ТипЦен;
            doc.Контрагент = докОснование.Контрагент;
            doc.Договор = докОснование.Договор;
            doc.Скидка = докОснование.Скидка;
            doc.СкидКарта = докОснование.СкидКарта;
            doc.Маршрут = докОснование.Маршрут;
            doc.ВидОплаты = докОснование.ВидОплаты;
            //doc.СуммаОплаты = докОснование.СуммаОплаты;
            doc.ПодСклад = await _склад.GetПодСкладByIdAsync(подСкладId);
            if (doc.Общие.Фирма.Id == фирмаId)
                doc.Собственный = 1;
            else if (await _фирма.ПолучитьФирмуДляОптаAsync() == фирмаId)
                doc.Собственный = 0;
            else if (await _фирма.ПолучитьФирмуДляОпта2Async() == фирмаId)
                doc.Собственный = 2;
            else if (await _фирма.ПолучитьФирмуДляОпта3Async() == фирмаId)
                doc.Собственный = 3;
            else
                doc.Собственный = 1;

            doc.ТабличнаяЧасть.AddRange(отчетКкмТЧ);
            doc.СуммаОплаты = doc.ТабличнаяЧасть.Sum(x => x.Сумма);

            if (doc.Ошибка == null || doc.Ошибка.Skip)
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, Common.НумераторПКО, 10, doc.Общие.Фирма.Id);
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаОтчетККМ doc)
        {
            try
            {
                await UnLockDocNoAsync(Common.НумераторПКО, doc.Общие.НомерДок);
                _1sjourn j = GetEntityJourn(0, 0, 4588, doc.Общие.ВидДокумента10, Common.НумераторПКО, doc.Общие.Наименование,
                    doc.Общие.НомерДок, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    doc.Контрагент.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                Dh3114 docHeader = new Dh3114
                {
                    Iddoc = j.Iddoc,
                    Sp3093 = doc.Склад != null ? doc.Склад.Id : Common.ПустоеЗначение,
                    Sp3363 = doc.Касса != null ? doc.Касса.Id : Common.ПустоеЗначение,
                    Sp3091 = Common.ВалютаРубль,
                    Sp3092 = 1,
                    Sp4256 = doc.ОблагаетсяЕНВД ? 1 : 0,
                    Sp3097 = doc.УчитыватьНДС ? 1 : 0,
                    Sp3099 = doc.СуммаВклНДС ? 1 : 0,
                    Sp3098 = 0,
                    Sp3100 = 0,
                    Sp6994 = doc.ТипЦен != null ? doc.ТипЦен.Id : Common.ПустоеЗначение,
                    Sp3658 = doc.Общие.ДокОснование != null ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp3659 = doc.Контрагент != null ? doc.Контрагент.Id : Common.ПустоеЗначение,
                    Sp4298 = "     2S  ", //поступления от покупателей
                    Sp4690 = doc.Договор != null ? doc.Договор.Id : Common.ПустоеЗначение,
                    Sp5150 = doc.Скидка != null ? doc.Скидка.Id : Common.ПустоеЗначение,
                    Sp5384 = "",
                    Sp7548 = 0,
                    Sp8817 = doc.СкидКарта != null ? doc.СкидКарта.Id : Common.ПустоеЗначение,
                    Sp8912 = 0,
                    Sp8989 = doc.ПодСклад != null ? doc.ПодСклад.Id : Common.ПустоеЗначение,
                    Sp9206 = doc.Закрыт ? 1 : 0,
                    Sp9240 = "",
                    Sp9444 = doc.Собственный,
                    Sp10263 = Common.ПустоеЗначение,
                    Sp11562 = doc.Маршрут != null ? doc.Маршрут.Code : "",
                    Sp11563 = doc.Маршрут != null ? doc.Маршрут.Наименование : "",
                    Sp11676 = doc.СуммаОплаты,
                    Sp660 = doc.Общие.Комментарий != null ? doc.Общие.Комментарий : ""
                };
                await _context.Dh3114s.AddAsync(docHeader);

                if (doc.ТабличнаяЧасть != null && doc.ТабличнаяЧасть.Count > 0)
                {
                    short lineNo = 1;
                    foreach (var строка in doc.ТабличнаяЧасть)
                    {
                        Dt3114 docRow = new Dt3114
                        {
                            Iddoc = j.Iddoc,
                            Lineno = lineNo++,
                            Sp3102 = string.IsNullOrEmpty(строка.НоменклатураId) ? Common.ПустоеЗначение : строка.НоменклатураId,
                            Sp3103 = строка.Количество,
                            Sp3104 = string.IsNullOrEmpty(строка.ЕдиницаId) ? Common.ПустоеЗначение : строка.ЕдиницаId,
                            Sp3105 = строка.Коэффициент,
                            Sp3106 = строка.Цена,
                            Sp3107 = строка.Сумма,
                            Sp3108 = строка.СтавкаНДС != null ? строка.СтавкаНДС.Id : "    I9   ", //Без НДС
                            Sp3109 = строка.СуммаНДС,
                            Sp3110 = "     1   ",
                            Sp3111 = 0,
                            Sp3112 = Common.ПустоеЗначение,
                        };
                        await _context.Dt3114s.AddAsync(docRow);
                    }
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
        private async Task ПеремещениеAsync(ФормаОтчетККМ doc, _1sjourn j)
        {
            int КоличествоДвижений = 0;

            string фирмаId = "";
            switch (doc.Собственный)
            {
                case 3:
                    фирмаId = await _фирма.ПолучитьФирмуДляОпта3Async();
                    break;
                case 2:
                    фирмаId = await _фирма.ПолучитьФирмуДляОпта2Async();
                    break;
                default:
                    фирмаId = await _фирма.ПолучитьФирмуДляОптаAsync();
                    break;
            }
            var РазрешенныеФирмы = new List<string> { фирмаId };
            var НаборId = await НайтиДокументВДеревеAsync(doc.Общие.IdDoc, 11948);
            var СписокТМЦ = doc.ТабличнаяЧасть.Select(x => x.НоменклатураId).ToList();
            var КодОперации = Common.КодОперации.Where(x => x.Value == "Реализация (купля-продажа)").Select(x => x.Key).FirstOrDefault();

            if (string.IsNullOrEmpty(НаборId))
            {
                var РегистрОстаткиТМЦ_Остатки = await _регистрОстаткиТМЦ.ПолучитьОстаткиAsync(
                    doc.Общие.ДатаДок,
                    doc.Общие.IdDoc,
                    false,
                    РазрешенныеФирмы,
                    СписокТМЦ,
                    doc.Склад.Id,
                    doc.ПодСклад.Id
                    );
                var РегистрРезервыТМЦ_Остатки = await _регистрРезервыТМЦ.ПолучитьОстаткиAsync(
                    doc.Общие.ДатаДок,
                    doc.Общие.IdDoc,
                    false,
                    РазрешенныеФирмы,
                    null,
                    СписокТМЦ,
                    new List<string> { doc.Склад.Id }
                    );
                foreach (var строка in doc.ТабличнаяЧасть)
                {
                    decimal КолВо = строка.Количество * строка.Коэффициент;
                    var Остатки = РегистрОстаткиТМЦ_Остатки
                        .Where(x => x.НоменклатураId == строка.НоменклатураId)
                        .Sum(x => x.Количество);
                    var Резервы = РегистрРезервыТМЦ_Остатки
                        .Where(x => x.НоменклатураId == строка.НоменклатураId && x.ДоговорId != doc.Договор.Id)
                        .Sum(x => x.Количество);
                    var МожноОтпустить = Math.Max(Math.Min(КолВо, (Остатки - Резервы)), 0);
                    if (МожноОтпустить < КолВо)
                    {
                        if (doc.Ошибка == null)
                            doc.Ошибка = new ExceptionData() { Description = "" };
                        if (!string.IsNullOrEmpty(doc.Ошибка.Description))
                            doc.Ошибка.Description += Environment.NewLine;
                        doc.Ошибка.Description += "На месте хранения нет нужного свободного количества ТМЦ " +
                            строка.НоменклатураId + ". Всего осталось " + ((Остатки - Резервы) / строка.Коэффициент).ToString() +
                            ". Требуемое количество " + строка.Количество.ToString();
                    }
                    else
                    {
                        КоличествоДвижений++;
                        j.Rf405 = await _регистрОстаткиТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                            фирмаId, строка.НоменклатураId, doc.Склад.Id, doc.ПодСклад.Id, 0, МожноОтпустить, 0
                            );


                        КоличествоДвижений++;
                        j.Rf405 = await _регистрОстаткиТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                            doc.Общие.Фирма.Id, строка.НоменклатураId, doc.Склад.Id, doc.ПодСклад.Id, 0, МожноОтпустить, 0
                            );
                    }
                }
            }

            var РегистрПартииНаличие_Остатки = await _регистрПартииНаличие.ПолучитьОстаткиAsync(
                doc.Общие.ДатаДок,
                doc.Общие.IdDoc,
                false,
                РазрешенныеФирмы,
                СписокТМЦ,
                null,
                null
                );
            var РегистрПартииНаличие_Итоги = РегистрПартииНаличие_Остатки.OrderBy(x => x.ФирмаId).ThenBy(x => x.ДатаПартии).ThenBy(x => x.DateTimeIdDoc);
            var ТаблСписПартий = Enumerable.Repeat(new
            {
                НомерСтрокиДокумента = (short)0,
                НоменклатураId = "",
                СтатусПартииId = "",
                ПартияId = "",
                ДатаПартии = DateTime.MinValue,
                Количество = 0.000m,
                СуммаУпр = 0.00m,
                СуммаБух = 0.00m,
                СуммаБезНДС = 0.00m,
                СуммаПрод = 0.00m
            }, 0).ToList();
            short lineNo = 0;
            foreach (var строка in doc.ТабличнаяЧасть)
            {
                lineNo++;
                decimal КолВо = строка.Количество * строка.Коэффициент;
                var ОстПогСуммаПродРуб = строка.Сумма + ((doc.УчитыватьНДС && !doc.СуммаВклНДС) ? строка.СуммаНДС : 0);
                var ОстПогНДСПрод = doc.УчитыватьНДС ? строка.СуммаНДС : 0;
                var МожноОтпустить = КолВо;
                foreach (var r in РегистрПартииНаличие_Итоги.Where(x => x.НоменклатураId == строка.НоменклатураId))
                {
                    if (МожноОтпустить <= 0)
                        break;
                    var КоэфСписания = 1m;
                    if (r.Количество > МожноОтпустить)
                        КоэфСписания = МожноОтпустить / r.Количество;
                    decimal кСписанию = Math.Round(r.Количество * КоэфСписания, 5, MidpointRounding.AwayFromZero);
                    decimal СуммаУпр = Math.Round(r.СуммаУпр * КоэфСписания, 2, MidpointRounding.AwayFromZero);
                    decimal СуммаРуб = Math.Round(r.СуммаРуб * КоэфСписания, 2, MidpointRounding.AwayFromZero);
                    decimal СуммаБезНДС = Math.Round(r.СуммаБезНДС * КоэфСписания, 2, MidpointRounding.AwayFromZero);

                    decimal КоэффПогашения = кСписанию / МожноОтпустить;
                    МожноОтпустить = МожноОтпустить - кСписанию;

                    var СписСуммаПродРуб = Math.Round(ОстПогСуммаПродРуб * КоэффПогашения, 2, MidpointRounding.AwayFromZero);
                    var СписНДСПрод = Math.Round(ОстПогНДСПрод * КоэффПогашения, 2, MidpointRounding.AwayFromZero);
                    var Выручка = Math.Round(СписСуммаПродРуб - СписНДСПрод, 2, MidpointRounding.AwayFromZero);

                    ОстПогСуммаПродРуб = ОстПогСуммаПродРуб - СписСуммаПродРуб;
                    ОстПогНДСПрод = ОстПогНДСПрод - СписНДСПрод;

                    ТаблСписПартий.Add(new
                    {
                        НомерСтрокиДокумента = lineNo,
                        НоменклатураId = строка.НоменклатураId,
                        СтатусПартииId = r.СтатусПартииId,
                        ПартияId = r.ПартияId,
                        ДатаПартии = r.ДатаПартии,
                        Количество = кСписанию,
                        СуммаУпр = СуммаУпр,
                        СуммаБух = СуммаРуб,
                        СуммаБезНДС = СуммаБезНДС,
                        СуммаПрод = СписСуммаПродРуб
                    });

                    КоличествоДвижений++;
                    j.Rf328 = await _регистрПартииНаличие.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        lineNo, фирмаId, строка.НоменклатураId, r.СтатусПартииId, r.ПартияId, r.ДатаПартии, r.ЦенаПрод,
                        кСписанию, СуммаУпр, СуммаРуб, СуммаБезНДС, КодОперации, СписСуммаПродРуб, Выручка
                        );
                }
                if (МожноОтпустить > 0)
                {
                    var СтатусПартии = Common.СтатусПартии.Where(x => x.Value == "Товар (купленный)").Select(x => x.Key).FirstOrDefault();
                    var Выручка = Math.Round(ОстПогСуммаПродРуб - ОстПогНДСПрод, 2, MidpointRounding.AwayFromZero);
                    ТаблСписПартий.Add(new
                    {
                        НомерСтрокиДокумента = lineNo,
                        НоменклатураId = строка.НоменклатураId,
                        СтатусПартииId = СтатусПартии,
                        ПартияId = Common.ПустоеЗначение,
                        ДатаПартии = Common.min1cDate,
                        Количество = МожноОтпустить,
                        СуммаУпр = 0m,
                        СуммаБух = 0m,
                        СуммаБезНДС = 0m,
                        СуммаПрод = ОстПогСуммаПродРуб
                    });
                    КоличествоДвижений++;
                    j.Rf328 = await _регистрПартииНаличие.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        lineNo, фирмаId, строка.НоменклатураId, СтатусПартии, Common.ПустоеЗначение, Common.min1cDate, 0,
                        МожноОтпустить, 0, 0, 0, КодОперации, ОстПогСуммаПродРуб, Выручка
                        );
                }

                var РегистрЗаменыНоменклатуры_Остатки = await _регистрЗаменаНоменклатуры.ПолучитьОстаткиAsync(
                        doc.Общие.ДатаДок,
                        doc.Общие.IdDoc,
                        false,
                        фирмаId,
                        СписокТМЦ
                        );
                МожноОтпустить = КолВо;
                foreach (var r in РегистрЗаменыНоменклатуры_Остатки)
                {
                    var КолСписания = Math.Max(Math.Min(МожноОтпустить, r.Количество), 0);
                    if (КолСписания > 0)
                    {
                        КоличествоДвижений++;
                        j.Rf12351 = await _регистрЗаменаНоменклатуры.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                            фирмаId, r.НоменклатураБухId, r.НоменклатураОБId, КолСписания
                            );
                        МожноОтпустить = МожноОтпустить - КолСписания;
                    }
                    if (МожноОтпустить == 0)
                        break;
                }
            }

            КодОперации = Common.КодОперации.Where(x => x.Value == "Поступление ТМЦ (купля-продажа)").Select(x => x.Key).FirstOrDefault();
            foreach (var r in ТаблСписПартий)
            {
                КоличествоДвижений++;
                j.Rf328 = await _регистрПартииНаличие.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                    r.НомерСтрокиДокумента, doc.Общие.Фирма.Id, r.НоменклатураId, r.СтатусПартииId, r.ПартияId, r.ДатаПартии, 0,
                    r.Количество, r.СуммаУпр, r.СуммаБух, r.СуммаБезНДС, КодОперации, r.СуммаПрод, 0
                    );
            }
            j.Actcnt = КоличествоДвижений;
        }
        private string ВидДолга(string СтатусПартииId)
        {
            if (СтатусПартииId == Common.GetСтатусПартииId("Товар (принятый)"))
                return "   5IW   "; //Долг за товара принятые в рознице
            else if (СтатусПартииId == Common.GetСтатусПартииId("Товар (купленный)") || СтатусПартииId == Common.GetСтатусПартииId("Товар (в рознице)"))
                return "   4XV   ";  //Долг за товары в рознице
            else
                return "    B1   "; //Долг за товары
        }
        public async Task<ExceptionData> ПровестиAsync(ФормаОтчетККМ doc)
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
                if (doc.Собственный != 1)
                    await ПеремещениеAsync(doc, j);
                if (doc.Ошибка != null && !doc.Ошибка.Skip)
                    return doc.Ошибка;

                int КоличествоДвижений = j.Actcnt;
                var РазрешенныеФирмы = new List<string> { doc.Общие.Фирма.Id };
                var ЗаявкаId = await НайтиДокументВДеревеAsync(doc.Общие.IdDoc, 2457);
                var НаборId = await НайтиДокументВДеревеAsync(doc.Общие.IdDoc, 11948);
                var СписокТМЦ = doc.ТабличнаяЧасть.Select(x => x.НоменклатураId).ToList();
                var КодОперации = Common.КодОперации.Where(x => x.Value == "Реализация (розница)").Select(x => x.Key).FirstOrDefault();
                var КодОперацииПередача = Common.КодОперации.Where(x => x.Value == "Передача в розницу").Select(x => x.Key).FirstOrDefault();
                var спТоварВРознице = Common.СтатусПартии.Where(x => x.Value == "Товар (в рознице)").Select(x => x.Key).FirstOrDefault();
                var спТоварКупленный = Common.СтатусПартии.Where(x => x.Value == "Товар (купленный)").Select(x => x.Key).FirstOrDefault();

                if (string.IsNullOrEmpty(НаборId))
                {
                    var РегистрОстаткиТМЦ_Остатки = await _регистрОстаткиТМЦ.ПолучитьОстаткиAsync(
                        doc.Общие.ДатаДок,
                        doc.Общие.IdDoc,
                        false,
                        РазрешенныеФирмы,
                        СписокТМЦ,
                        doc.Склад.Id,
                        doc.ПодСклад.Id
                        );
                    var РегистрРезервыТМЦ_Остатки = await _регистрРезервыТМЦ.ПолучитьОстаткиAsync(
                        doc.Общие.ДатаДок,
                        doc.Общие.IdDoc,
                        false,
                        РазрешенныеФирмы,
                        null,
                        СписокТМЦ,
                        new List<string> { doc.Склад.Id }
                        );
                    var РегистрЗаявки_Остатки = await _регистрЗаявки.ПолучитьОстаткиAsync(
                        doc.Общие.ДатаДок,
                        doc.Общие.IdDoc,
                        false,
                        РазрешенныеФирмы,
                        doc.Договор.Id,
                        СписокТМЦ,
                        ЗаявкаId
                        );
                    var РегистрЗаказыЗаявки_Остатки = await _регистрЗаказыЗаявки.ПолучитьОстаткиAsync(
                        doc.Общие.ДатаДок,
                        doc.Общие.IdDoc,
                        false,
                        null,
                        ЗаявкаId,
                        СписокТМЦ
                        );
                    foreach (var строка in doc.ТабличнаяЧасть)
                    {
                        decimal КолВо = строка.Количество * строка.Коэффициент;
                        var Остатки = РегистрОстаткиТМЦ_Остатки
                            .Where(x => x.НоменклатураId == строка.НоменклатураId)
                            .Sum(x => x.Количество);
                        var Резервы = РегистрРезервыТМЦ_Остатки
                            .Where(x => x.НоменклатураId == строка.НоменклатураId && x.ДоговорId != doc.Договор.Id)
                            .Sum(x => x.Количество);
                        var МожноОтпустить = Math.Max(Math.Min(КолВо, (Остатки - Резервы)), 0);
                        if (МожноОтпустить < КолВо)
                        {
                            if (doc.Ошибка == null)
                                doc.Ошибка = new ExceptionData() { Description = "" };
                            if (!string.IsNullOrEmpty(doc.Ошибка.Description))
                                doc.Ошибка.Description += Environment.NewLine;
                            doc.Ошибка.Description += "На месте хранения нет нужного свободного количества ТМЦ " +
                                строка.НоменклатураId + ". Всего осталось " + ((Остатки - Резервы) / строка.Коэффициент).ToString() +
                                ". Требуемое количество " + строка.Количество.ToString();
                        }
                        else
                        {
                            КоличествоДвижений++;
                            j.Rf405 = await _регистрОстаткиТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                doc.Общие.Фирма.Id, строка.НоменклатураId, doc.Склад.Id, doc.ПодСклад.Id, 0, МожноОтпустить, 0
                                );
                            var РезервПоЗаявке = РегистрРезервыТМЦ_Остатки
                                .Where(x => x.НоменклатураId == строка.НоменклатураId && x.ДоговорId == doc.Договор.Id && x.ЗаявкаId == ЗаявкаId);

                            var МожноПогасить = КолВо;
                            foreach (var r in РезервПоЗаявке)
                            {
                                if (МожноПогасить <= 0)
                                    break;
                                var Погасить = Math.Max(Math.Min(МожноПогасить, r.Количество), 0);
                                if (Погасить > 0)
                                {
                                    МожноПогасить = МожноПогасить - Погасить;
                                    КоличествоДвижений++;
                                    j.Rf4480 = await _регистрРезервыТМЦ.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                        r.ФирмаId, r.НоменклатураId, r.СкладId, r.ДоговорId, r.ЗаявкаId, Погасить);
                                    foreach (var rz in РегистрЗаявки_Остатки.Where(x => x.НоменклатураId == строка.НоменклатураId))
                                    {
                                        if (Погасить <= 0)
                                            break;
                                        var ПогаситьЗаявки = Math.Max(Math.Min(Погасить, rz.Количество), 0);
                                        if (ПогаситьЗаявки > 0)
                                        {
                                            Погасить = Погасить - ПогаситьЗаявки;
                                            КоличествоДвижений++;
                                            j.Rf4674 = await _регистрЗаявки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                                rz.ФирмаId, rz.НоменклатураId, rz.ДоговорId, rz.ЗаявкаId, ПогаситьЗаявки, rz.Стоимость * ПогаситьЗаявки / rz.Количество);
                                            foreach (var rzz in РегистрЗаказыЗаявки_Остатки.Where(x => x.НоменклатураId == строка.НоменклатураId))
                                            {
                                                if (ПогаситьЗаявки <= 0)
                                                    break;
                                                var ПогаситьЗаказыЗаявки = Math.Max(Math.Min(ПогаситьЗаявки, rzz.Количество), 0);
                                                if (ПогаситьЗаказыЗаявки > 0)
                                                {
                                                    ПогаситьЗаявки = ПогаситьЗаявки - ПогаситьЗаказыЗаявки;
                                                    КоличествоДвижений++;
                                                    j.Rf4667 = await _регистрЗаказыЗаявки.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                                        rzz.НоменклатураId, rzz.ЗаказId, rzz.ЗаявкаId, rzz.НаСогласование, ПогаситьЗаявки);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else //Набор
                {
                    string текФирма = "";
                    switch (doc.Собственный)
                    {
                        case 0:
                            текФирма = await _фирма.ПолучитьФирмуДляОптаAsync();
                            break;
                        case 2:
                            текФирма = await _фирма.ПолучитьФирмуДляОпта2Async();
                            break;
                        case 3:
                            текФирма = await _фирма.ПолучитьФирмуДляОпта3Async();
                            break;
                        default:
                            текФирма = doc.Общие.Фирма.Id;
                            break;
                    }
                    var РегистрНаборНаСкладе_Остатки = await _регистрНаборНаСкладе.ПолучитьОстаткиAsync(
                        doc.Общие.ДатаДок,
                        doc.Общие.IdDoc,
                        false,
                        new List<string> { текФирма },
                        doc.Склад.Id,
                        doc.Договор.Id,
                        СписокТМЦ,
                        НаборId
                        );
                    foreach (var строка in doc.ТабличнаяЧасть)
                    {
                        decimal КолВо = строка.Количество * строка.Коэффициент;
                        var Остатки = РегистрНаборНаСкладе_Остатки
                            .Where(x => x.НоменклатураId == строка.НоменклатураId &&
                                    x.СкладId == doc.Склад.Id &&
                                    x.ПодСкладId == doc.ПодСклад.Id &&
                                    x.ДоговорId == doc.Договор.Id)
                            .Sum(x => x.Количество);
                        var МожноОтпустить = Math.Max(Math.Min(КолВо, Остатки), 0);
                        if (МожноОтпустить < КолВо)
                        {
                            if (doc.Ошибка == null)
                                doc.Ошибка = new ExceptionData() { Description = "" };
                            if (!string.IsNullOrEmpty(doc.Ошибка.Description))
                                doc.Ошибка.Description += Environment.NewLine;
                            doc.Ошибка.Description += "В наборе нет нужного свободного количества ТМЦ " +
                                строка.НоменклатураId + ". Всего осталось " + (Остатки / строка.Коэффициент).ToString() +
                                ". Требуемое количество " + строка.Количество.ToString();
                        }
                        else
                        {
                            КоличествоДвижений++;
                            j.Rf11973 = await _регистрНаборНаСкладе.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                doc.Общие.Фирма.Id, doc.Склад.Id, doc.ПодСклад.Id, doc.Договор.Id, НаборId, строка.НоменклатураId, МожноОтпустить);
                        }
                    }
                }
                if (doc.Ошибка != null && !doc.Ошибка.Skip)
                    return doc.Ошибка;

                //ПартииНаличие
                var РегистрПартииНаличие_Остатки = await _регистрПартииНаличие.ПолучитьОстаткиAsync(
                    doc.Общие.ДатаДок,
                    doc.Общие.IdDoc,
                    false,
                    РазрешенныеФирмы,
                    СписокТМЦ,
                    null,
                    null
                    );
                var РегистрПартииНаличие_Итоги = РегистрПартииНаличие_Остатки.OrderBy(x => x.ФирмаId).ThenBy(x => x.ДатаПартии).ThenBy(x => x.DateTimeIdDoc);
                var ТаблСписПартий = Enumerable.Repeat(new
                {
                    НомерСтрокиДокумента = (short)0,
                    НоменклатураId = "",
                    Цена = 0.00m,
                    СтатусПартииId = "",
                    ПартияId = "",
                    ДатаПартии = DateTime.MinValue,
                    ПоставщикId = "",
                    ДоговорПоставщикаId = "",
                    СтавкаНДС = "",
                    Количество = 0.000m,
                    СуммаУпр = 0.00m,
                    СуммаБух = 0.00m,
                    СуммаБезНДС = 0.00m,
                    СуммаПрод = 0.00m,
                    СуммаНдсПрод = 0.00m
                }, 0).ToList();
                short lineNo = 0;
                foreach (var строка in doc.ТабличнаяЧасть)
                {
                    lineNo++;
                    decimal КолВо = строка.Количество * строка.Коэффициент;
                    var ОстПогСуммаПродРуб = строка.Сумма + ((doc.УчитыватьНДС && !doc.СуммаВклНДС) ? строка.СуммаНДС : 0);
                    var ОстПогНДСПрод = doc.УчитыватьНДС ? строка.СуммаНДС : 0;
                    var МожноОтпустить = КолВо;
                    foreach (var r in РегистрПартииНаличие_Итоги.Where(x => x.НоменклатураId == строка.НоменклатураId))
                    {
                        if (МожноОтпустить <= 0)
                            break;
                        string ПоставщикId = Common.ПустоеЗначение;
                        string ДоговорПоставщикаId = Common.ПустоеЗначение;
                        if (r.ПартияId != Common.ПустоеЗначение)
                        {
                            var ДанныеПартии = await _context.Sc214s
                                .Where(x => x.Parentext == r.НоменклатураId && x.Id == r.ПартияId)
                                .Select(x => new { Поставщик = x.Sp436, Договор = x.Sp217 })
                                .FirstOrDefaultAsync();
                            if (ДанныеПартии == null)
                            {
                                ПоставщикId = Common.ПустоеЗначение;
                                ДоговорПоставщикаId = Common.ПустоеЗначение;
                            }
                        }
                        var КоэфСписания = 1m;
                        if (r.Количество > МожноОтпустить)
                            КоэфСписания = МожноОтпустить / r.Количество;
                        decimal кСписанию = Math.Round(r.Количество * КоэфСписания, 5, MidpointRounding.AwayFromZero);
                        decimal СуммаУпр = Math.Round(r.СуммаУпр * КоэфСписания, 2, MidpointRounding.AwayFromZero);
                        decimal СуммаРуб = Math.Round(r.СуммаРуб * КоэфСписания, 2, MidpointRounding.AwayFromZero);
                        decimal СуммаБезНДС = Math.Round(r.СуммаБезНДС * КоэфСписания, 2, MidpointRounding.AwayFromZero);

                        decimal КоэффПогашения = кСписанию / МожноОтпустить;
                        МожноОтпустить = МожноОтпустить - кСписанию;

                        var СписСуммаПродРуб = Math.Round(ОстПогСуммаПродРуб * КоэффПогашения, 2, MidpointRounding.AwayFromZero);
                        var СписНДСПрод = Math.Round(ОстПогНДСПрод * КоэффПогашения, 2, MidpointRounding.AwayFromZero);
                        var Выручка = Math.Round(СписСуммаПродРуб - СписНДСПрод, 2, MidpointRounding.AwayFromZero);

                        ОстПогСуммаПродРуб = ОстПогСуммаПродРуб - СписСуммаПродРуб;
                        ОстПогНДСПрод = ОстПогНДСПрод - СписНДСПрод;

                        ТаблСписПартий.Add(new
                        {
                            НомерСтрокиДокумента = lineNo,
                            НоменклатураId = строка.НоменклатураId,
                            Цена = строка.Цена,
                            СтатусПартииId = r.СтатусПартииId,
                            ПартияId = r.ПартияId,
                            ДатаПартии = r.ДатаПартии,
                            ПоставщикId = ПоставщикId,
                            ДоговорПоставщикаId = ДоговорПоставщикаId,
                            СтавкаНДС = строка.СтавкаНДС.Id,
                            Количество = кСписанию,
                            СуммаУпр = СуммаУпр,
                            СуммаБух = СуммаРуб,
                            СуммаБезНДС = СуммаБезНДС,
                            СуммаПрод = СписСуммаПродРуб,
                            СуммаНдсПрод = СписНДСПрод
                        });

                        КоличествоДвижений++;
                        j.Rf328 = await _регистрПартииНаличие.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                            lineNo, doc.Общие.Фирма.Id, строка.НоменклатураId, r.СтатусПартииId, r.ПартияId, r.ДатаПартии, r.ЦенаПрод,
                            кСписанию, СуммаУпр, СуммаРуб, СуммаБезНДС, КодОперацииПередача, СписСуммаПродРуб, 0
                            );
                    }
                    if (МожноОтпустить > 0)
                    {
                        //var Выручка = Math.Round(ОстПогСуммаПродРуб - ОстПогНДСПрод, 2, MidpointRounding.AwayFromZero);
                        ТаблСписПартий.Add(new
                        {
                            НомерСтрокиДокумента = lineNo,
                            НоменклатураId = строка.НоменклатураId,
                            Цена = строка.Цена,
                            СтатусПартииId = спТоварКупленный,
                            ПартияId = Common.ПустоеЗначение,
                            ДатаПартии = Common.min1cDate,
                            ПоставщикId = Common.ПустоеЗначение,
                            ДоговорПоставщикаId = Common.ПустоеЗначение,
                            СтавкаНДС = строка.СтавкаНДС.Id,
                            Количество = МожноОтпустить,
                            СуммаУпр = 0m,
                            СуммаБух = 0m,
                            СуммаБезНДС = 0m,
                            СуммаПрод = ОстПогСуммаПродРуб,
                            СуммаНдсПрод = ОстПогНДСПрод
                        });
                        КоличествоДвижений++;
                        j.Rf328 = await _регистрПартииНаличие.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                            lineNo, doc.Общие.Фирма.Id, строка.НоменклатураId, спТоварКупленный, Common.ПустоеЗначение, Common.min1cDate, 0,
                            МожноОтпустить, 0, 0, 0, КодОперацииПередача, ОстПогСуммаПродРуб, 0
                            );
                    }

                    var РегистрЗаменыНоменклатуры_Остатки = await _регистрЗаменаНоменклатуры.ПолучитьОстаткиAsync(
                            doc.Общие.ДатаДок,
                            doc.Общие.IdDoc,
                            false,
                            doc.Общие.Фирма.Id,
                            СписокТМЦ
                            );
                    МожноОтпустить = КолВо;
                    foreach (var r in РегистрЗаменыНоменклатуры_Остатки)
                    {
                        var КолСписания = Math.Max(Math.Min(МожноОтпустить, r.Количество), 0);
                        if (КолСписания > 0)
                        {
                            КоличествоДвижений++;
                            j.Rf12351 = await _регистрЗаменаНоменклатуры.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                                doc.Общие.Фирма.Id, r.НоменклатураБухId, r.НоменклатураОБId, КолСписания
                                );
                            МожноОтпустить = МожноОтпустить - КолСписания;
                        }
                        if (МожноОтпустить == 0)
                            break;
                    }
                }
                foreach (var r in ТаблСписПартий)
                {
                    var текСтатусПартии = r.СтатусПартииId;
                    if (текСтатусПартии == спТоварКупленный)
                        текСтатусПартии = спТоварВРознице;
                    var Выручка = Math.Round(r.СуммаПрод - r.СуммаНдсПрод, 2, MidpointRounding.AwayFromZero);

                    КоличествоДвижений++;
                    j.Rf328 = await _регистрПартииНаличие.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                        r.НомерСтрокиДокумента, doc.Общие.Фирма.Id, r.НоменклатураId, текСтатусПартии, r.ПартияId, r.ДатаПартии, r.Цена,
                        r.Количество, r.СуммаУпр, r.СуммаБух, r.СуммаБезНДС, КодОперацииПередача, r.СуммаПрод, 0
                        );
                    КоличествоДвижений++;
                    j.Rf328 = await _регистрПартииНаличие.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        r.НомерСтрокиДокумента, doc.Общие.Фирма.Id, r.НоменклатураId, текСтатусПартии, r.ПартияId, r.ДатаПартии, r.Цена,
                        r.Количество, r.СуммаУпр, r.СуммаБух, r.СуммаБезНДС, КодОперации, r.СуммаПрод, Выручка
                        );

                    КоличествоДвижений++;
                    j.Rf2351 = await _регистрПродажи.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений,
                        doc.Общие.Фирма.Id,
                        doc.Контрагент.Id,
                        r.ПоставщикId,
                        r.НоменклатураId,
                        doc.Склад.Id,
                        r.СуммаУпр,
                        r.СуммаПрод,
                        r.Количество,
                        0, 0, 0,
                        r.СуммаБезНДС,
                        0
                        );
                }
                var GrТаблСписПартий = from t in ТаблСписПартий
                                       group t by new { t.СтатусПартииId, t.СтавкаНДС, t.ДоговорПоставщикаId } into gr
                                       select new
                                       {
                                           СтатусПартииId = gr.Key.СтатусПартииId,
                                           СтавкаНДС = gr.Key.СтавкаНДС,
                                           ДоговорПоставщикаId = gr.Key.ДоговорПоставщикаId,
                                           СуммаПрод = gr.Sum(x => x.СуммаПрод),
                                           НдсПрод = gr.Sum(x => x.СуммаНдсПрод),
                                           СуммаБезНдс = gr.Sum(x => x.СуммаБезНДС)
                                       };
                foreach (var r in GrТаблСписПартий)
                {
                    КоличествоДвижений++;
                    j.Rf4335 = await _регистрПокупатели.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                        doc.Общие.Фирма.Id, doc.Договор.Id, ВидДолга(r.СтатусПартииId), doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc,
                        r.СуммаБезНдс, r.СуммаПрод, КодОперации, r.ДоговорПоставщикаId, Common.ПустоеЗначениеИд13);
                    КоличествоДвижений++;
                    j.Rf4335 = await _регистрПокупатели.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        doc.Общие.Фирма.Id, doc.Договор.Id, ВидДолга(r.СтатусПартииId), doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc,
                        r.СуммаБезНдс, r.СуммаПрод, КодОперации, r.ДоговорПоставщикаId, Common.ПустоеЗначениеИд13);

                    КоличествоДвижений++;
                    j.Rf4343 = await _регистрКнигаПродаж.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                        doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc,
                        r.СтавкаНДС,
                        r.СтатусПартииId,
                        r.НдсПрод,
                        r.СуммаПрод,
                        КодОперации,
                        Common.ПустоеЗначениеИд13);
                    КоличествоДвижений++;
                    j.Rf4343 = await _регистрКнигаПродаж.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc,
                        r.СтавкаНДС,
                        r.СтатусПартииId,
                        r.НдсПрод,
                        r.СуммаПрод,
                        КодОперации,
                        doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc);
                }
                foreach (var r in ТаблСписПартий.Where(x => x.ПартияId != Common.ПустоеЗначение))
                {
                    КоличествоДвижений++;
                    j.Rf438 = await _регистрРеализованныйТовар.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                        doc.Общие.Фирма.Id, r.ДоговорПоставщикаId, r.НоменклатураId, r.ПартияId, doc.Общие.ВидДокумента36.PadLeft(4) + doc.Общие.IdDoc,
                        r.Количество, r.СуммаПрод - r.СуммаНдсПрод, 0);
                }
                decimal ИтогоСумма = doc.ТабличнаяЧасть.Sum(x => x.Сумма) + (doc.УчитыватьНДС && !doc.СуммаВклНДС ? doc.ТабличнаяЧасть.Sum(x => x.СуммаНДС) : 0);
                КоличествоДвижений++;
                j.Rf635 = await _регистрКасса.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                    doc.Общие.Фирма.Id, doc.Касса.Id, ИтогоСумма, КодОперации, "     2S  ");
                if (doc.СуммаОплаты > 0)
                {
                    КоличествоДвижений++;
                    j.Rf635 = await _регистрКасса.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        doc.Общие.Фирма.Id, doc.Касса.Id, doc.СуммаОплаты, КодОперации, "     2S  ");
                }
                if (doc.СкидКарта != null && !doc.СкидКарта.Закрыта)
                {
                    КоличествоДвижений++;
                    j.Rf8677 = await _регистрНакопСкидка.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений,
                        doc.СкидКарта.Id, ИтогоСумма);
                }

                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Ds1946 = 2;

                _context.Update(j);
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                await ОбновитьВремяТА(j.Iddoc, j.DateTimeIddoc);
                await ОбновитьПоследовательность(j.DateTimeIddoc);
                await ОбновитьСетевуюАктивность();
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
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаОтчетККМ doc, ФормаОплатаЧерезЮКасса оплатаЧерезЮКассаDoc)
        {
            ExceptionData result = await ЗаписатьAsync(doc);
            if (result == null)
                result = await ПровестиAsync(doc);
            if (result == null && оплатаЧерезЮКассаDoc != null)
            {
                var докЧекККМ = await _чекККМ.ВводНаОснованииAsync(doc,
                    ВидыОперацииЧекаККМ.Чек,
                    оплатаЧерезЮКассаDoc.PaymentId,
                    оплатаЧерезЮКассаDoc.Email,
                    оплатаЧерезЮКассаDoc.Телефон.Length > 10 ? оплатаЧерезЮКассаDoc.Телефон.Substring(1, 10) : оплатаЧерезЮКассаDoc.Телефон,
                    !string.IsNullOrEmpty(оплатаЧерезЮКассаDoc.Email),
                    !string.IsNullOrEmpty(оплатаЧерезЮКассаDoc.Телефон),
                    true,
                    doc.СуммаОплаты,
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
