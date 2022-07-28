using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using StinWeb.Models.Repository;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class ПриемВРемонт : Controller
    {
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private StinDbContext _context;
        public ПриемВРемонт(StinDbContext context, ILogger<ПриемВРемонт> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _logger = logger;
            this._serviceScopeFactory = serviceScopeFactory;
        }
        [Authorize]
        public IActionResult Index()
        {
            if (_context.NeedToOpenPeriod())
                return Redirect("/Home/Error?Description=ПериодНеОткрыт");
            string docNo = Common.LockDocNo(_context, "9899");
            string НомерКвитанции = docNo;
            if (НомерКвитанции.Length != 7)
            {
                Regex re = new Regex(@"([a-zA-Z]+)(\d+)");
                Match result = re.Match(НомерКвитанции);

                string alphaPart = result.Groups[1].Value;
                int numberPart = Convert.ToInt32(result.Groups[2].Value);
                НомерКвитанции = alphaPart + numberPart.ToString().PadLeft(7 - alphaPart.Length, '0');
            }
            НомерКвитанции += "-" + DateTime.Now.ToString("yyyy");

            ViewBag.DocNo = docNo;
            ViewBag.НомерКвитанции = НомерКвитанции;
            return View();
        }
        //[HttpGet("НоменклатураБезЗапчастей")]
        //public JsonResult НоменклатураБезЗапчастей(LookupFilter filter, string фАртикул, string фПроизводитель)
        //{
        //    filter.AdditionalFilters["Артикул"] = фАртикул;
        //    if (фПроизводитель == "ЛЮБОЙ ПРОИЗВОДИТЕЛЬ")
        //        filter.AdditionalFilters["Производитель"] = null;
        //    else
        //        filter.AdditionalFilters["Производитель"] = фПроизводитель;

        //    return Json(new НоменклатураLookup(_context) { Filter = filter }.GetData());
        //}
        //[HttpGet("Неисправность")]
        //public JsonResult Неисправность(LookupFilter filter)
        //{
        //    return Json(new НеисправностьLookup(_context) { Filter = filter }.GetData());
        //}
        //[HttpGet("Заказчик")]
        //public JsonResult Заказчик(LookupFilter filter)
        //{ 
        //    return Json(new КонтрагентLookup(_context) { Filter = filter }.GetData());
        //}
        [HttpPost]
        public IActionResult ПолучитьТелефонEmail(string КонтрагентId)
        {
            var phones = (from sc12393 in _context.Sc12393s
                          where sc12393.Ismark == false && sc12393.Parentext == КонтрагентId
                          select new
                          {
                              Таблица = "phones",
                              Id = sc12393.Id,
                              Наименование = sc12393.Descr.Trim()
                          }).AsEnumerable();
            var emails = (from sc13650 in _context.Sc13650s
                         where sc13650.Ismark == false && sc13650.Parentext == КонтрагентId
                         select new
                         {
                             Таблица = "emails",
                             Id = sc13650.Id,
                             Наименование = sc13650.Descr.Trim()
                         }).AsEnumerable();
            if (phones.Count() > 0)
                return Ok(phones.Union(emails));
            else if (emails.Count() > 0)
                return Ok(emails);
            else
                return Ok(phones);
        }
        [HttpPost]
        public IActionResult ПроверкаИНН(string ИНН, string КПП)
        {
            string НайденныйКонтрагент = "";
            string search = ИНН;
            string searchAlt = "";
            if (!string.IsNullOrEmpty(КПП))
            {
                search = ИНН + "/" + КПП;
                searchAlt = ИНН + @"\" + КПП;
                НайденныйКонтрагент = _context.Sc172s.Where(x => x.Sp8380.Trim() == search || x.Sp8380.Trim() == searchAlt).Select(x => x.Descr.Trim()).FirstOrDefault();
            }
            else
                НайденныйКонтрагент = _context.Sc172s.Where(x => x.Sp8380.Trim() == search).Select(x => x.Descr.Trim()).FirstOrDefault();
            return Ok(new { 
                isValid = string.IsNullOrEmpty(НайденныйКонтрагент),
                message = string.IsNullOrEmpty(НайденныйКонтрагент) ? "" : "ИНН используется \"" + НайденныйКонтрагент + "\""
            });
        }
        [HttpPost]
        public async Task<IActionResult> НовыйЗаказчик(int ВидКонтрагента, string Наименование, string ИНН, string КПП,
            string Адрес, string Телефон, string Email)
        {
            using (var tran = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var code = _context.Sc172s.Where(x => x.Code.StartsWith(Common.ПрефиксИБ(_context))).Max(x => x.Code);
                    if (code == null)
                        code = "0";
                    else
                        code = code.Substring(Common.ПрефиксИБ(_context).Length);
                    int next_code = 0;
                    if (Int32.TryParse(code, out next_code))
                    {
                        next_code += 1;
                    }
                    code = Common.ПрефиксИБ(_context) + next_code.ToString().PadLeft(8 - Common.ПрефиксИБ(_context).Length,'0');

                    Sc172 Контрагент = new Sc172
                    {
                        Id = Common.GenerateId(_context, 172),
                        Parentid = ВидКонтрагента == 1 ? Common.КонтрагентИзМастерскойФизЛица : Common.КонтрагентИзМастерскойОрганизации,
                        Code = code,
                        Descr = Наименование,
                        Isfolder = 2,
                        Ismark = false,
                        Verstamp = 0,
                        Sp4137 = Common.ПустоеЗначение,
                        Sp573 = "",
                        Sp4426 = Common.ПустоеЗначение,
                        Sp572 = Email ?? "",
                        Sp583 = Common.ПустоеЗначение,
                        Sp8380 = ВидКонтрагента == 1 ? "" : ИНН + (КПП == null ? "" : "/" + КПП),
                        Sp9631 = Common.ПустоеЗначение,
                        Sp10379 = Common.ПустоеЗначение,
                        Sp12916 = 0,
                        Sp13072 = Common.ПустоеЗначение,
                        Sp13073 = 0,
                        Sp186 = ""
                    };

                    if (ВидКонтрагента == 1)
                    {
                        code = _context.Sc503s.Where(x => x.Code.StartsWith(Common.ПрефиксИБ(_context))).Max(x => x.Code);
                        if (code == null)
                            code = "0";
                        else
                            code = code.Substring(Common.ПрефиксИБ(_context).Length);
                        next_code = 0;
                        if (Int32.TryParse(code, out next_code))
                        {
                            next_code += 1;
                        }
                        code = Common.ПрефиксИБ(_context) + next_code.ToString().PadLeft(8 - Common.ПрефиксИБ(_context).Length, '0');

                        Sc503 ФизЛицо = new Sc503
                        {
                            Id = Common.GenerateId(_context,503),
                            Parentid = Common.ПустоеЗначение,
                            Code = code,
                            Descr = Наименование,
                            Isfolder = 2,
                            Ismark = false,
                            Verstamp = 0,
                            Sp508 = Наименование,
                            Sp504 = "",
                            Sp672 = Regex.Replace(Телефон.Substring(2), @"\s+", "") ?? "",
                            Sp673 = Адрес ?? "",
                            Sp674 = Адрес ?? ""
                        };
                        await _context.Sc503s.AddAsync(ФизЛицо);
                        Контрагент.Sp521 = Common.Encode36(503).PadLeft(4) + ФизЛицо.Id;
                    }
                    else
                    {
                        code = _context.Sc493s.Where(x => x.Code.StartsWith(Common.ПрефиксИБ(_context))).Max(x => x.Code);
                        if (code == null)
                            code = "0";
                        else
                            code = code.Substring(Common.ПрефиксИБ(_context).Length);
                        next_code = 0;
                        if (Int32.TryParse(code, out next_code))
                        {
                            next_code += 1;
                        }
                        code = Common.ПрефиксИБ(_context) + next_code.ToString().PadLeft(8 - Common.ПрефиксИБ(_context).Length, '0');

                        Sc493 ЮрЛицо = new Sc493
                        {
                            Id = Common.GenerateId(_context, 493),
                            Parentid = Common.ПустоеЗначение,
                            Code = code,
                            Descr = Наименование,
                            Isfolder = 2,
                            Ismark = false,
                            Verstamp = 0,
                            Sp498 = Наименование,
                            Sp494 = ИНН.Length == 10 ? ИНН + "/" + КПП : ИНН,
                            Sp497 = "",
                            Sp671 = Regex.Replace(Телефон.Substring(2), @"\s+", "") ?? "",
                            Sp666 = Адрес ?? "",
                            Sp499 = Адрес ?? ""
                        };
                        await _context.Sc493s.AddAsync(ЮрЛицо);
                        Контрагент.Sp521 = Common.Encode36(493).PadLeft(4) + ЮрЛицо.Id;
                    }

                    code = _context.Sc204s.Where(x => x.Code.StartsWith(Common.ПрефиксИБ(_context))).Max(x => x.Code);
                    if (code == null)
                        code = "0";
                    else
                        code = code.Substring(Common.ПрефиксИБ(_context).Length);
                    next_code = 0;
                    if (Int32.TryParse(code, out next_code))
                    {
                        next_code += 1;
                    }
                    code = Common.ПрефиксИБ(_context) + next_code.ToString().PadLeft(8 - Common.ПрефиксИБ(_context).Length, '0');

                    var условияДоговора = (from d in _context.Sc9678s
                                          where d.Id == Common.ДоговорыУсловияСтандартныеРозничные
                                          select new
                                          {
                                              id = d.Id,
                                              Наименование = d.Descr.Trim(),
                                              ТипЦен = d.Sp9676,
                                              Скидка = d.Sp9675,
                                              ГлубинаКредита = d.Sp9696,
                                              СкидкаДоставка = d.Sp10380
                                          }).FirstOrDefault();

                    Sc204 Договор = new Sc204
                    {
                        Id = Common.GenerateId(_context, 204),
                        Parentid = Common.ПустоеЗначение,
                        Code = code,
                        Descr = условияДоговора.Наименование,
                        Parentext = Контрагент.Id,
                        Isfolder = 2,
                        Ismark = false,
                        Verstamp = 0,
                        Sp9664 = условияДоговора.id,
                        Sp668 = Common.ВалютаРубль,
                        Sp1948 = условияДоговора.ТипЦен,
                        Sp1920 = условияДоговора.Скидка,
                        Sp870 = условияДоговора.ГлубинаКредита,
                        Sp2285 = 0,
                        Sp4764 = 1,
                        Sp8843 = Common.ПустоеЗначение,
                        Sp10377 = Common.ПустоеЗначение,
                        Sp10378 = условияДоговора.СкидкаДоставка,
                        Sp13486 = Common.FirmaSS,
                        Sp13487= 1
                    };
                    await _context.Sc204s.AddAsync(Договор);
                    Контрагент.Sp667 = Договор.Id;

                    await _context.Sc172s.AddAsync(Контрагент);
                    if (Телефон != null)
                    {
                        Sc12393 sc12393 = new Sc12393
                        {
                            Id = Common.GenerateId(_context, 12393),
                            Descr = Regex.Replace(Телефон.Substring(2), @"\s+", ""),
                            Parentext = Контрагент.Id,
                            Ismark = false,
                            Verstamp = 0
                        };
                        await _context.Sc12393s.AddAsync(sc12393);
                    }
                    if (Email != null)
                    {
                        Sc13650 sc13650 = new Sc13650
                        {
                            Id = Common.GenerateId(_context, 13650),
                            Descr = Email,
                            Parentext = Контрагент.Id,
                            Ismark = false,
                            Verstamp = 0
                        };
                        await _context.Sc13650s.AddAsync(sc13650);
                    }

                    await _context.SaveChangesAsync();
                    tran.Commit();
                    return Ok(new { id = Контрагент.Id });
                }
                catch
                {
                    tran.Rollback();
                    return NoContent();
                }
            }
        }
        [HttpPost]
        public IActionResult ПолучитьМастеров()
        {
            return Ok( from sc9864 in _context.Sc9864s
                        where sc9864.Ismark == false && sc9864.Isfolder == 2 && sc9864.Descr.Trim() != ""
                        orderby sc9864.Descr
                        select new
                        {
                            id = sc9864.Id,
                            наименование = sc9864.Descr.Trim()
                        });
        }
        [HttpPost]
        public IActionResult ПолучитьПроизводителей()
        {
            return Ok( from b in _context.Sc8840s
                       where b.Ismark == false && b.Descr.Trim() != "" && !b.Descr.Trim().EndsWith("*") && !b.Descr.ToUpper().Contains(" ЗАПЧАСТИ ")
                       orderby b.Descr
                       select new
                       {
                           id = b.Id,
                           наименование = b.Descr.Trim()
                       }
                );
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateDoc(StinWeb.Models.DataManager.Документы.ПриемВРемонт приемВРемонт)
        {
            Квитанция квитанция;
            if (Common.ПолучитьКвитанцию(приемВРемонт.КвитанцияId, out квитанция))
            {
                string Комплектность = приемВРемонт.Комплектность ?? "";
                if (Комплектность.Length > 100)
                    Комплектность = Комплектность.Substring(0, 100);
                string Комментарий = приемВРемонт.Комментарий ?? "";
                if (Комментарий.Length > 150)
                    Комментарий = Комментарий.Substring(0, 150);
                if (приемВРемонт.ДатаПродажи == DateTime.MinValue)
                    приемВРемонт.ДатаПродажи = Common.min1cDate;
                Склад склад = (from sc55 in _context.Sc55s
                               where sc55.Id == приемВРемонт.Склад.Id
                               select new Склад
                               {
                                   Id = sc55.Id,
                                   Наименование = sc55.Descr.Trim()
                               }).FirstOrDefault();
                string МестоХранения = _context.Sc8963s.Where(x => x.Id == приемВРемонт.ПодСклад.Id).Select(x => x.Descr.Trim()).FirstOrDefault();
                Контрагент контрагент = (from sc172 in _context.Sc172s
                                         where sc172.Id == приемВРемонт.Заказчик.Id
                                         select new Контрагент
                                         {
                                             Id = sc172.Id,
                                             Наименование = sc172.Descr.Trim(),
                                             ОсновнойДоговор = sc172.Sp667
                                         }).FirstOrDefault();
                Номенклатура Изделие = (from sc84 in _context.Sc84s
                                        join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                                        from sc8840 in _sc8840.DefaultIfEmpty()
                                       where sc84.Id == приемВРемонт.Изделие.Id
                                       select new Номенклатура
                                       {
                                           Наименование = sc84.Descr.Trim(),
                                           Артикул = sc84.Sp85.Trim(),
                                           Производитель = sc8840.Descr.Trim() ?? ""
                                       }).FirstOrDefault();
                string Неисправность = _context.Sc9866s.Where(x => x.Id == приемВРемонт.Неисправность.Id).Select(x => x.Descr.Trim()).FirstOrDefault();
                string Неисправность2 = приемВРемонт.Неисправность2.Id == null ? string.Empty : _context.Sc9866s.Where(x => x.Id == приемВРемонт.Неисправность2.Id).Select(x => x.Descr.Trim()).FirstOrDefault();
                string Неисправность3 = приемВРемонт.Неисправность3.Id == null ? string.Empty : _context.Sc9866s.Where(x => x.Id == приемВРемонт.Неисправность3.Id).Select(x => x.Descr.Trim()).FirstOrDefault();
                string Неисправность4 = приемВРемонт.Неисправность4.Id == null ? string.Empty : _context.Sc9866s.Where(x => x.Id == приемВРемонт.Неисправность4.Id).Select(x => x.Descr.Trim()).FirstOrDefault();
                string Неисправность5 = приемВРемонт.Неисправность5.Id == null ? string.Empty : _context.Sc9866s.Where(x => x.Id == приемВРемонт.Неисправность5.Id).Select(x => x.Descr.Trim()).FirstOrDefault();
                string Мастер = _context.Sc9864s.Where(x => x.Id == приемВРемонт.Мастер).Select(x => x.Descr.Trim()).FirstOrDefault();
                var Телефон = приемВРемонт.Телефон != null ?
                        (from sc12393 in _context.Sc12393s
                         where sc12393.Id == приемВРемонт.Телефон
                         select new
                         {
                             Id = sc12393.Id,
                             Номер = sc12393.Descr.Trim()
                         }).FirstOrDefault() :
                        new { Id = Common.ПустоеЗначение, Номер = "" };

                var Почта = приемВРемонт.Email != null ?
                    (from sc13650 in _context.Sc13650s
                     where sc13650.Id == приемВРемонт.Email
                     select new
                     {
                         Id = sc13650.Id,
                         Адрес = sc13650.Descr.Trim()
                     }).FirstOrDefault() :
                     new { Id = Common.ПустоеЗначение, Адрес = "" };
                string message = "";
                List<Корзина> авансовыеРаботы = HttpContext.Session.GetObjectFromJson<List<Корзина>>("ПодборРабот");
                HttpContext.Session.SetObjectAsJson("ПодборРабот", null);
                using (var docTran = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (Телефон == null)
                        {
                            Sc12393 sc12393 = new Sc12393
                            {
                                Id = Common.GenerateId(_context, 12393),
                                Descr = приемВРемонт.Телефон,
                                Parentext = контрагент.Id,
                                Ismark = false,
                                Verstamp = 0
                            };
                            await _context.Sc12393s.AddAsync(sc12393);
                            Телефон = new { Id = sc12393.Id, Номер = sc12393.Descr.Trim() };
                        }
                        if (Почта == null)
                        {
                            Sc13650 sc13650 = new Sc13650
                            {
                                Id = Common.GenerateId(_context, 13650),
                                Descr = приемВРемонт.Email,
                                Parentext = контрагент.Id,
                                Ismark = false,
                                Verstamp = 0
                            };
                            await _context.Sc13650s.AddAsync(sc13650);
                            Почта = new { Id = sc13650.Id, Адрес = sc13650.Descr.Trim() };
                        }
                        Common.UnLockDocNo(_context, "9899", приемВРемонт.НомерДок);
                        DateTime docDateTime = DateTime.Now;
                        _1sjourn j = Common.GetEntityJourn(_context, 1, 3, 10528, 9899, null, "ПриемВремонт",
                            приемВРемонт.НомерДок, docDateTime,
                            Common.FirmaSS,
                            User.FindFirstValue("UserId"),
                            склад.Наименование,
                            контрагент.Наименование);
                        j.Rf9972 = true;
                        j.Rf10471 = true;
                        j.Rf11049 = true;
                        await _context._1sjourns.AddAsync(j);

                        Dh9899 docHeader = new Dh9899
                        {
                            Iddoc = j.Iddoc,
                            Sp9889 = контрагент.Id,
                            Sp9890 = приемВРемонт.Изделие.Id,
                            Sp9891 = приемВРемонт.ЗаводскойНомер,
                            Sp9892 = приемВРемонт.Неисправность.Id,
                            Sp9893 = приемВРемонт.ТипРемонта,
                            Sp9894 = приемВРемонт.Мастер,
                            Sp9896 = приемВРемонт.ДатаПродажи,
                            Sp9897 = приемВРемонт.НомерРемонта,
                            Sp10012 = склад.Id,
                            Sp10013 = приемВРемонт.ПодСклад.Id,
                            Sp10014 = Common.ПустоеЗначениеИд13,
                            Sp10108 = квитанция.Номер,
                            Sp10109 = квитанция.Дата,
                            Sp10110 = Common.ПустоеЗначение,
                            Sp10111 = Common.ПустоеЗначение,
                            Sp10112 = Common.min1cDate,
                            Sp10113 = "",
                            Sp10553 = 0,
                            Sp10712 = Неисправность2 == string.Empty ? Common.ПустоеЗначение : приемВРемонт.Неисправность2.Id,
                            Sp10713 = Неисправность3 == string.Empty ? Common.ПустоеЗначение : приемВРемонт.Неисправность3.Id,
                            Sp10714 = Неисправность4 == string.Empty ? Common.ПустоеЗначение : приемВРемонт.Неисправность4.Id,
                            Sp10715 = Неисправность5 == string.Empty ? Common.ПустоеЗначение : приемВРемонт.Неисправность5.Id,
                            Sp10716 = Common.ПустоеЗначение,
                            Sp10717 = Common.ПустоеЗначение,
                            Sp10795 = Комплектность,
                            Sp660 = Комментарий,
                            Sp12394 = Телефон.Id,
                            Sp13651 = Почта.Id
                        };
                        await _context.Dh9899s.AddAsync(docHeader);

                        await _context.SaveChangesAsync();

                        await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 9899, docHeader.Iddoc);

                        int КоличествоДвижений = 1;
                        bool Приход = true;
                        DateTime docDate = DateTime.ParseExact(j.DateTimeIddoc.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture);
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA11049_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@НомерКвитанции,@ДатаКвитанции,@Склад,@ПодСклад,@Мастер,@Количество," +
                            "0,@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", docHeader.Iddoc),
                            new SqlParameter("@ActNo", КоличествоДвижений), 
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@НомерКвитанции", квитанция.Номер),
                            new SqlParameter("@ДатаКвитанции", квитанция.Дата),
                            new SqlParameter("@Склад", склад.Id),
                            new SqlParameter("@ПодСклад", приемВРемонт.ПодСклад.Id),
                            new SqlParameter("@Мастер", приемВРемонт.Мастер),
                            new SqlParameter("@Количество", 1),
                            new SqlParameter("@docDate", docDate.ToShortDateString()),
                            new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));
                        
                        КоличествоДвижений++;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA9972_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@Гарантия,@Изделие,@ЗавНомер,@СтатусПартии,@Заказчик,@СкладОткуда,@ДатаПриема," +
                            "@НомерКвитанции,@ДатаКвитанции,@Количество," +
                            "1,0,@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", docHeader.Iddoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@Гарантия", приемВРемонт.ТипРемонта),
                            new SqlParameter("@Изделие", приемВРемонт.Изделие.Id),
                            new SqlParameter("@ЗавНомер", приемВРемонт.ЗаводскойНомер),
                            new SqlParameter("@СтатусПартии", Common.СтатусПартии.Where(x => x.Value == "Принят в ремонт").Select(x => x.Key).FirstOrDefault()),
                            new SqlParameter("@Заказчик", контрагент.Id),
                            new SqlParameter("@СкладОткуда", Common.ПустоеЗначение),
                            new SqlParameter("@ДатаПриема", docDate.ToShortDateString()),
                            new SqlParameter("@НомерКвитанции", квитанция.Номер),
                            new SqlParameter("@ДатаКвитанции", квитанция.Дата),
                            new SqlParameter("@Количество", 1),
                            new SqlParameter("@docDate", docDate.ToShortDateString()),
                            new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                        КоличествоДвижений++;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA10471_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@НомерКвитанции,@ДатаКвитанции,@ДокПоступления,@Количество," +
                            "@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", docHeader.Iddoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@НомерКвитанции", квитанция.Номер),
                            new SqlParameter("@ДатаКвитанции", квитанция.Дата),
                            new SqlParameter("@ДокПоступления", docHeader.Iddoc),
                            new SqlParameter("@Количество", 1),
                            new SqlParameter("@docDate", docDate.ToShortDateString()),
                            new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                        await Common.ОбновитьВремяТА(_context, docHeader.Iddoc, j.DateTimeIddoc);
                        await Common.ОбновитьПоследовательность(_context, j.DateTimeIddoc);
                        await _context.ОбновитьСетевуюАктивность();

                        Sc13662 реестрСообщений = new Sc13662
                        {
                            Id = Common.GenerateId(_context, 13662),
                            Ismark = false,
                            Verstamp = 0,
                            Sp13658 = Common.Encode36(9899).PadLeft(4) + j.Iddoc,
                            Sp13659 = string.IsNullOrEmpty(Телефон.Номер) ? 0 : 1,
                            Sp13660 = string.IsNullOrEmpty(Почта.Адрес) ? 0 : 1
                        };
                        await _context.Sc13662s.AddAsync(реестрСообщений);
                        await _context.SaveChangesAsync();

                        //документ Авансовая оплата работ
                        if (приемВРемонт.ТипРемонта == 0 && авансовыеРаботы != null && авансовыеРаботы.Count > 0)
                        {
                            string КассаId = (from sc30 in _context.Sc30s
                                              where sc30.Id == User.FindFirstValue("UserId")
                                              select sc30.Sp2643).FirstOrDefault();
                            if (string.IsNullOrEmpty(КассаId))
                                КассаId = Common.DefaultКасса;
                            j = Common.GetEntityJourn(_context, 1, 3 + авансовыеРаботы.Count * 2, 10528, 10054, Common.НумераторВыдачаРемонт, "ВыдачаИзРемонта",
                                null, docDateTime.AddSeconds(1),
                                Common.FirmaSS,
                                User.FindFirstValue("UserId"),
                                склад.Наименование,
                                контрагент.Наименование);
                            j.Rf635 = true; //касса
                            j.Rf4335 = true; //покупатели
                            j.Rf9989 = true; //работы на изделиях
                            j.Rf10305 = true; //продажи мастерской
                            await _context._1sjourns.AddAsync(j);

                            Dh10054 АвансоваяОплатаШапка = new Dh10054
                            {
                                Iddoc = j.Iddoc,
                                Sp10026 = контрагент.Id,
                                Sp10027 = приемВРемонт.Изделие.Id,
                                Sp10028 = приемВРемонт.ЗаводскойНомер,
                                Sp10029 = приемВРемонт.Неисправность.Id,
                                Sp10030 = приемВРемонт.ТипРемонта,
                                Sp10033 = приемВРемонт.ДатаПродажи,
                                Sp10034 = docDateTime.Date,
                                Sp10035 = приемВРемонт.НомерРемонта,
                                Sp10036 = квитанция.Номер,
                                Sp10037 = квитанция.Дата,
                                Sp10038 = склад.Id,
                                Sp10039 = приемВРемонт.ПодСклад.Id,
                                Sp10040 = 1,
                                Sp10041 = Common.СтатусПартии.Where(x => x.Value == "Принят в ремонт").Select(x => x.Key).FirstOrDefault(),
                                Sp10119 = Common.ПустоеЗначение,
                                Sp10209 = Common.Encode36(9899).PadLeft(4) + docHeader.Iddoc,
                                Sp10210 = 1,
                                Sp10286 = Common.ПустоеЗначение,
                                Sp10461 = -3,
                                Sp10557 = 0,
                                Sp10852 = КассаId, 
                                Sp11593 = "",
                                Sp11594 = "",
                                Sp13601 = 0,
                                Sp10047 = 0,
                                Sp10048 = 0,
                                Sp10050 = 0,
                                Sp10051 = 0,
                                Sp10052 = 0,
                                Sp660 = Комментарий
                            };
                            await _context.Dh10054s.AddAsync(АвансоваяОплатаШапка);

                            decimal ИтогСумма = авансовыеРаботы.Sum(x => x.Сумма);
                            short lineNo = 1;
                            foreach (Корзина работа in авансовыеРаботы)
                            {
                                Dt10054 docRow = new Dt10054
                                {
                                    Iddoc = j.Iddoc,
                                    Lineno = lineNo++,
                                    Sp10120 = Common.ПустоеЗначение,
                                    Sp10042 = Common.ПустоеЗначение,
                                    Sp10043 = Common.ПустоеЗначение,
                                    Sp10044 = 0,
                                    Sp10045 = 0,
                                    Sp10046 = 0,
                                    Sp10047 = 0,
                                    Sp10048 = 0,
                                    Sp10049 = работа.Id, 
                                    Sp10121 = Common.ПустоеЗначение,
                                    Sp10050 = работа.Сумма,
                                    Sp10051 = 0,
                                    Sp10052 = работа.Сумма,
                                    Sp10763 = работа.Quantity,
                                    Sp10764 = работа.Цена,
                                    Sp10765 = 0
                                };
                                await _context.Dt10054s.AddAsync(docRow);
                            }
                            await _context.SaveChangesAsync();
                            await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 10054, j.Iddoc);
                            await Common.ОбновитьПодчиненныеДокументы(_context, Common.Encode36(9899).PadLeft(4) + docHeader.Iddoc, j.DateTimeIddoc, j.Iddoc);

                            await _context.Database.ExecuteSqlRawAsync(
                                "exec _1sp_DH10054_UpdateTotals @num36",
                                new SqlParameter("@num36", j.Iddoc)
                                );

                            КоличествоДвижений = 1;
                            Приход = true;
                            await _context.Database.ExecuteSqlRawAsync(
                                "exec _1sp_RA635_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                "@Фирма,@Касса,@Валюта,@СуммаВал,@СуммаУпр,@СуммаРуб,@КодОперации,@ДвижениеДенежныхС," +
                                "@docDate,@CurPeriod,1,0",
                                new SqlParameter("@num36", j.Iddoc),
                                new SqlParameter("@ActNo", КоличествоДвижений),
                                new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                new SqlParameter("@Фирма", j.Sp4056),
                                new SqlParameter("@Касса", АвансоваяОплатаШапка.Sp10852),
                                new SqlParameter("@Валюта", Common.ВалютаРубль),
                                new SqlParameter("@СуммаВал", ИтогСумма),
                                new SqlParameter("@СуммаУпр", ИтогСумма),
                                new SqlParameter("@СуммаРуб", ИтогСумма),
                                new SqlParameter("@КодОперации", Common.КодОперации.Where(x => x.Value == "Реализация (розница, ЕНВД)").Select(x => x.Key).FirstOrDefault()),
                                new SqlParameter("@ДвижениеДенежныхС", Common.ПустоеЗначение),
                                new SqlParameter("@docDate", docDate.ToShortDateString()),
                                new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                            КоличествоДвижений++;
                            Приход = true;
                            await _context.Database.ExecuteSqlRawAsync(
                                "exec _1sp_RA4335_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                "@Фирма,@Договор,@СтавкаНП,@ВидДолга,@КредДокумент,@СуммаВал,@СуммаУпр,@СуммаРуб,@СуммаНП,@Себестоимость,@КодОперации,@ДоговорКомитента,@ДокументОплаты," +
                                "@docDate,@CurPeriod,1,0",
                                new SqlParameter("@num36", j.Iddoc),
                                new SqlParameter("@ActNo", КоличествоДвижений),
                                new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                new SqlParameter("@Фирма", j.Sp4056),
                                new SqlParameter("@Договор", контрагент.ОсновнойДоговор),
                                new SqlParameter("@СтавкаНП", Common.ПустоеЗначение),
                                new SqlParameter("@ВидДолга", Common.ВидДолга.Where(x => x.Value == "Долг за работы (в рознице)").Select(x => x.Key).FirstOrDefault()),
                                new SqlParameter("@КредДокумент", Common.Encode36(10054).PadLeft(4) + j.Iddoc),
                                new SqlParameter("@СуммаВал", ИтогСумма),
                                new SqlParameter("@СуммаУпр", ИтогСумма),
                                new SqlParameter("@СуммаРуб", ИтогСумма),
                                new SqlParameter("@СуммаНП", Common.zero),
                                new SqlParameter("@Себестоимость", Common.zero),
                                new SqlParameter("@КодОперации", Common.КодОперации.Where(x => x.Value == "Реализация (розница, ЕНВД)").Select(x => x.Key).FirstOrDefault()),
                                new SqlParameter("@ДоговорКомитента", Common.ПустоеЗначение),
                                new SqlParameter("@ДокументОплаты", Common.ПустоеЗначениеИд13),
                                new SqlParameter("@docDate", docDate.ToShortDateString()),
                                new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                            КоличествоДвижений++;
                            Приход = false;
                            await _context.Database.ExecuteSqlRawAsync(
                                "exec _1sp_RA4335_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                "@Фирма,@Договор,@СтавкаНП,@ВидДолга,@КредДокумент,@СуммаВал,@СуммаУпр,@СуммаРуб,@СуммаНП,@Себестоимость,@КодОперации,@ДоговорКомитента,@ДокументОплаты," +
                                "@docDate,@CurPeriod,1,0",
                                new SqlParameter("@num36", j.Iddoc),
                                new SqlParameter("@ActNo", КоличествоДвижений),
                                new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                new SqlParameter("@Фирма", j.Sp4056),
                                new SqlParameter("@Договор", контрагент.ОсновнойДоговор),
                                new SqlParameter("@СтавкаНП", Common.ПустоеЗначение),
                                new SqlParameter("@ВидДолга", Common.ВидДолга.Where(x => x.Value == "Долг за работы (в рознице)").Select(x => x.Key).FirstOrDefault()),
                                new SqlParameter("@КредДокумент", Common.Encode36(10054).PadLeft(4) + j.Iddoc),
                                new SqlParameter("@СуммаВал", ИтогСумма),
                                new SqlParameter("@СуммаУпр", ИтогСумма),
                                new SqlParameter("@СуммаРуб", ИтогСумма),
                                new SqlParameter("@СуммаНП", Common.zero),
                                new SqlParameter("@Себестоимость", Common.zero),
                                new SqlParameter("@КодОперации", Common.КодОперации.Where(x => x.Value == "Реализация (розница, ЕНВД)").Select(x => x.Key).FirstOrDefault()),
                                new SqlParameter("@ДоговорКомитента", Common.ПустоеЗначение),
                                new SqlParameter("@ДокументОплаты", Common.ПустоеЗначениеИд13),
                                new SqlParameter("@docDate", docDate.ToShortDateString()),
                                new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                            foreach (Корзина работа in авансовыеРаботы)
                            {
                                КоличествоДвижений++;
                                Приход = true;

                                await _context.Database.ExecuteSqlRawAsync(
                                    "exec _1sp_RA9989_WriteDocAct @num36,0,@ActNo,@DebetCredit,@IdDocDef,@DateTimeIdDoc," +
                                    "@НомКвитанции,@ДатаКвитанции,@Изделие,@ЗавНомер,@Исполнитель,@Работа,@ФлагБензо,@ДопРаботы,@Количество,@Сумма,@СуммаЗавода,@КодГарантии," +
                                    "@docDate,@CurPeriod,1,0",
                                    new SqlParameter("@num36", j.Iddoc),
                                    new SqlParameter("@ActNo", КоличествоДвижений),
                                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                    new SqlParameter("@IdDocDef", 10054),
                                    new SqlParameter("@DateTimeIdDoc", j.DateTimeIddoc),
                                    new SqlParameter("@НомКвитанции", квитанция.Номер),
                                    new SqlParameter("@ДатаКвитанции", квитанция.Дата),
                                    new SqlParameter("@Изделие", приемВРемонт.Изделие.Id),
                                    new SqlParameter("@ЗавНомер", приемВРемонт.ЗаводскойНомер),
                                    new SqlParameter("@Исполнитель", Common.ПустоеЗначение),
                                    new SqlParameter("@Работа", работа.Id),
                                    new SqlParameter("@ФлагБензо", Common.zero),
                                    new SqlParameter("@ДопРаботы", Common.zero),
                                    new SqlParameter("@Количество", работа.Quantity),
                                    new SqlParameter("@Сумма", работа.Сумма),
                                    new SqlParameter("@СуммаЗавода", Common.zero),
                                    new SqlParameter("@КодГарантии", приемВРемонт.ТипРемонта),
                                    new SqlParameter("@docDate", docDate.ToShortDateString()),
                                    new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                                КоличествоДвижений++;
                                Приход = true;
                                await _context.Database.ExecuteSqlRawAsync(
                                    "exec _1sp_RA10305_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                    "@Фирма,@Склад,@Мастер,@Изделие,@ЗавНомер,@ЗапЧасть,@Работа,@Гарантия,@Себестоимость,@ПродСтоимость,@Количество,@СебестоимостьБезН," +
                                    "@docDate,@CurPeriod,1,0",
                                    new SqlParameter("@num36", j.Iddoc),
                                    new SqlParameter("@ActNo", КоличествоДвижений),
                                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                    new SqlParameter("@Фирма", j.Sp4056),
                                    new SqlParameter("@Склад", склад.Id),
                                    new SqlParameter("@Мастер", Common.ПустоеЗначение),
                                    new SqlParameter("@Изделие", приемВРемонт.Изделие.Id),
                                    new SqlParameter("@ЗавНомер", приемВРемонт.ЗаводскойНомер),
                                    new SqlParameter("@ЗапЧасть", Common.ПустоеЗначение),
                                    new SqlParameter("@Работа", работа.Id),
                                    new SqlParameter("@Гарантия", приемВРемонт.ТипРемонта),
                                    new SqlParameter("@Себестоимость", Common.zero),
                                    new SqlParameter("@ПродСтоимость", работа.Сумма),
                                    new SqlParameter("@Количество", работа.Quantity),
                                    new SqlParameter("@СебестоимостьБезН", Common.zero),
                                    new SqlParameter("@docDate", docDate.ToShortDateString()),
                                    new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));
                            }
                            await Common.ОбновитьВремяТА(_context, j.Iddoc, j.DateTimeIddoc);
                            await Common.ОбновитьПоследовательность(_context, j.DateTimeIddoc);
                            await _context.ОбновитьСетевуюАктивность();
                        }

                        //документ "На Диагностику"
                        int колДв = 4;
                        if (приемВРемонт.ТипРемонта == 0 && авансовыеРаботы != null && авансовыеРаботы.Count > 0)
                        {
                            колДв = колДв + авансовыеРаботы.Count;
                        }
                        j = Common.GetEntityJourn(_context, 1, колДв, 10528, 10995, null, "НаДиагностику",
                            null, docDateTime.AddSeconds(2),
                            Common.FirmaSS,
                            User.FindFirstValue("UserId"),
                            склад.Наименование,
                            контрагент.Наименование);
                        j.Rf9972 = true;
                        j.Rf11049 = true;
                        if (приемВРемонт.ТипРемонта == 0 && авансовыеРаботы != null && авансовыеРаботы.Count > 0)
                            j.Rf9989 = true;

                        await _context._1sjourns.AddAsync(j);

                        Dh10995 НаДиагностикуШапка = new Dh10995
                        {
                            Iddoc = j.Iddoc,
                            Sp10971 = квитанция.Номер,
                            Sp10972 = квитанция.Дата,
                            Sp10973 = приемВРемонт.Мастер,
                            Sp10974 = склад.Id,
                            Sp10975 = приемВРемонт.ПодСклад.Id,
                            Sp10976 = 1,
                            Sp10977 = Common.Encode36(9899).PadLeft(4) + docHeader.Iddoc,
                            Sp10978 = контрагент.Id,
                            Sp10979 = приемВРемонт.Изделие.Id,
                            Sp10980 = приемВРемонт.ЗаводскойНомер,
                            Sp10981 = приемВРемонт.Неисправность.Id,
                            Sp10982 = приемВРемонт.ТипРемонта,
                            Sp10983 = приемВРемонт.ДатаПродажи,
                            Sp10984 = приемВРемонт.НомерРемонта,
                            Sp10985 = Common.ПустоеЗначение,
                            Sp10986 = Common.СтатусПартии.Where(x => x.Value == "Принят в ремонт").Select(x => x.Key).FirstOrDefault(),
                            Sp10987 = Common.min1cDate,
                            Sp10988 = Неисправность2 == string.Empty ? Common.ПустоеЗначение : приемВРемонт.Неисправность2.Id,
                            Sp10989 = Неисправность3 == string.Empty ? Common.ПустоеЗначение : приемВРемонт.Неисправность3.Id,
                            Sp10990 = Неисправность4 == string.Empty ? Common.ПустоеЗначение : приемВРемонт.Неисправность4.Id,
                            Sp10991 = Неисправность5 == string.Empty ? Common.ПустоеЗначение : приемВРемонт.Неисправность5.Id,
                            Sp10992 = Common.ПустоеЗначение,
                            Sp10993 = Common.ПустоеЗначение,
                            Sp660 = Комментарий
                        };
                        await _context.Dh10995s.AddAsync(НаДиагностикуШапка);
                        if (приемВРемонт.ТипРемонта == 0 && авансовыеРаботы != null && авансовыеРаботы.Count > 0)
                        {
                            //есть авансовые работы
                            short lineNo = 1;
                            foreach (Корзина работа in авансовыеРаботы)
                            {
                                Dt10995 docRow = new Dt10995
                                {
                                    Iddoc = j.Iddoc,
                                    Lineno = lineNo++,
                                    Sp11621 = работа.Id,
                                    Sp11622 = работа.Quantity,
                                    Sp11623 = работа.Цена,
                                    Sp11624 = работа.Сумма
                                };
                                await _context.Dt10995s.AddAsync(docRow);
                            }    
                        }
                        await _context.SaveChangesAsync();

                        await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 10995, НаДиагностикуШапка.Iddoc);
                        await Common.ОбновитьПодчиненныеДокументы(_context, Common.Encode36(9899).PadLeft(4) + docHeader.Iddoc, j.DateTimeIddoc, j.Iddoc);

                        await _context.Database.ExecuteSqlRawAsync(
                            "exec _1sp_DH10995_UpdateTotals @num36",
                            new SqlParameter("@num36", j.Iddoc)
                            );

                        КоличествоДвижений = 1;
                        Приход = false;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA11049_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@НомерКвитанции,@ДатаКвитанции,@Склад,@ПодСклад,@Мастер,@Количество," +
                            "0,@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", НаДиагностикуШапка.Iddoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@НомерКвитанции", квитанция.Номер),
                            new SqlParameter("@ДатаКвитанции", квитанция.Дата),
                            new SqlParameter("@Склад", склад.Id),
                            new SqlParameter("@ПодСклад", приемВРемонт.ПодСклад.Id),
                            new SqlParameter("@Мастер", приемВРемонт.Мастер),
                            new SqlParameter("@Количество", 1),
                            new SqlParameter("@docDate", docDate.ToShortDateString()),
                            new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                        КоличествоДвижений++;
                        Приход = true;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA11049_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@НомерКвитанции,@ДатаКвитанции,@Склад,@ПодСклад,@Мастер,@Количество," +
                            "0,@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", НаДиагностикуШапка.Iddoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@НомерКвитанции", квитанция.Номер),
                            new SqlParameter("@ДатаКвитанции", квитанция.Дата),
                            new SqlParameter("@Склад", склад.Id),
                            new SqlParameter("@ПодСклад", приемВРемонт.ПодСклад.Id),
                            new SqlParameter("@Мастер", приемВРемонт.Мастер),
                            new SqlParameter("@Количество", 1),
                            new SqlParameter("@docDate", docDate.ToShortDateString()),
                            new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                        КоличествоДвижений++;
                        Приход = false;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA9972_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@Гарантия,@Изделие,@ЗавНомер,@СтатусПартии,@Заказчик,@СкладОткуда,@ДатаПриема," +
                            "@НомерКвитанции,@ДатаКвитанции,@Количество," +
                            "1,0,@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", НаДиагностикуШапка.Iddoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@Гарантия", приемВРемонт.ТипРемонта),
                            new SqlParameter("@Изделие", приемВРемонт.Изделие.Id),
                            new SqlParameter("@ЗавНомер", приемВРемонт.ЗаводскойНомер),
                            new SqlParameter("@СтатусПартии", Common.СтатусПартии.Where(x => x.Value == "Принят в ремонт").Select(x => x.Key).FirstOrDefault()),
                            new SqlParameter("@Заказчик", контрагент.Id),
                            new SqlParameter("@СкладОткуда", Common.ПустоеЗначение),
                            new SqlParameter("@ДатаПриема", docDate.ToShortDateString()),
                            new SqlParameter("@НомерКвитанции", квитанция.Номер),
                            new SqlParameter("@ДатаКвитанции", квитанция.Дата),
                            new SqlParameter("@Количество", 1),
                            new SqlParameter("@docDate", docDate.ToShortDateString()),
                            new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                        КоличествоДвижений++;
                        Приход = true;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA9972_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@Гарантия,@Изделие,@ЗавНомер,@СтатусПартии,@Заказчик,@СкладОткуда,@ДатаПриема," +
                            "@НомерКвитанции,@ДатаКвитанции,@Количество," +
                            "1,0,@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", НаДиагностикуШапка.Iddoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@Гарантия", приемВРемонт.ТипРемонта),
                            new SqlParameter("@Изделие", приемВРемонт.Изделие.Id),
                            new SqlParameter("@ЗавНомер", приемВРемонт.ЗаводскойНомер),
                            new SqlParameter("@СтатусПартии", Common.СтатусПартии.Where(x => x.Value == "На диагностике").Select(x => x.Key).FirstOrDefault()),
                            new SqlParameter("@Заказчик", контрагент.Id),
                            new SqlParameter("@СкладОткуда", Common.ПустоеЗначение),
                            new SqlParameter("@ДатаПриема", docDate.ToShortDateString()),
                            new SqlParameter("@НомерКвитанции", квитанция.Номер),
                            new SqlParameter("@ДатаКвитанции", квитанция.Дата),
                            new SqlParameter("@Количество", 1),
                            new SqlParameter("@docDate", docDate.ToShortDateString()),
                            new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));

                        if (приемВРемонт.ТипРемонта == 0 && авансовыеРаботы != null && авансовыеРаботы.Count > 0)
                        {
                            foreach (Корзина работа in авансовыеРаботы)
                            {
                                КоличествоДвижений++;
                                Приход = false;
                                await _context.Database.ExecuteSqlRawAsync(
                                    "exec _1sp_RA9989_WriteDocAct @num36,0,@ActNo,@DebetCredit,@IdDocDef,@DateTimeIdDoc," +
                                    "@НомКвитанции,@ДатаКвитанции,@Изделие,@ЗавНомер,@Исполнитель,@Работа,@ФлагБензо,@ДопРаботы,@Количество,@Сумма,@СуммаЗавода,@КодГарантии," +
                                    "@docDate,@CurPeriod,1,0",
                                    new SqlParameter("@num36", j.Iddoc),
                                    new SqlParameter("@ActNo", КоличествоДвижений),
                                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                    new SqlParameter("@IdDocDef", 10995),
                                    new SqlParameter("@DateTimeIdDoc", j.DateTimeIddoc),
                                    new SqlParameter("@НомКвитанции", квитанция.Номер),
                                    new SqlParameter("@ДатаКвитанции", квитанция.Дата),
                                    new SqlParameter("@Изделие", приемВРемонт.Изделие.Id),
                                    new SqlParameter("@ЗавНомер", приемВРемонт.ЗаводскойНомер),
                                    new SqlParameter("@Исполнитель", Common.ПустоеЗначение),
                                    new SqlParameter("@Работа", работа.Id),
                                    new SqlParameter("@ФлагБензо", Common.zero),
                                    new SqlParameter("@ДопРаботы", Common.zero),
                                    new SqlParameter("@Количество", работа.Quantity),
                                    new SqlParameter("@Сумма", работа.Сумма),
                                    new SqlParameter("@СуммаЗавода", Common.zero),
                                    new SqlParameter("@КодГарантии", приемВРемонт.ТипРемонта),
                                    new SqlParameter("@docDate", docDate.ToShortDateString()),
                                    new SqlParameter("@CurPeriod", Common.GetRegTA(_context).ToShortDateString()));
                            }
                        }    

                        await Common.ОбновитьВремяТА(_context, НаДиагностикуШапка.Iddoc, j.DateTimeIddoc);
                        await Common.ОбновитьПоследовательность(_context, j.DateTimeIddoc);
                        await _context.ОбновитьСетевуюАктивность();

                        //Photos
                        if (приемВРемонт.Photos != null)
                        {
                            var zippedImages = await Common.CreateZip(приемВРемонт.Photos);
                            VzPhoto vzP = new VzPhoto
                            {
                                Id = приемВРемонт.КвитанцияId,
                                Photo = zippedImages,
                                Extension = "zip"
                            };
                            await _context.VzPhotos.AddAsync(vzP);
                            await _context.SaveChangesAsync();
                        }

                        docTran.Commit();

                        var ДанныеФирмы = (from sc131 in _context.Sc131s
                                           join sc4014 in _context.Sc4014s on sc131.Id equals sc4014.Sp4011
                                           join sc1710 in _context.Sc1710s on sc4014.Sp4133 equals sc1710.Id into _sc1710
                                           from sc1710 in _sc1710.DefaultIfEmpty()
                                           join sc163 in _context.Sc163s on sc1710.Sp1712 equals sc163.Id 
                                           where sc4014.Id == Common.FirmaSS
                                           select new
                                           {
                                               Фирма = sc4014.Descr.Trim(),
                                               ЮрЛицо = sc131.Descr.Trim(),
                                               ИНН = sc131.Sp135.Trim(),
                                               Адрес = sc131.Sp149,
                                               РасчетныйСчет = sc1710.Sp4219.Trim() ?? string.Empty,
                                               КоррСчет = sc163.Sp165.Trim() ?? string.Empty,
                                               Банк = sc163.Descr.Trim() ?? string.Empty,
                                               БИК = sc163.Code.Trim() ?? string.Empty,
                                               городБанка = sc163.Sp164.Trim() ?? string.Empty,
                                           }).FirstOrDefault();
                        string ТелефонСервиса = _context._1sconsts.Where(x => x.Id == 10711).Select(x => x.Value).FirstOrDefault().Trim();

                        ПриемВРемонтПечать ДанныеПечати = new ПриемВРемонтПечать
                        {
                            ЮрЛицо = ДанныеФирмы.ЮрЛицо,
                            НомерКвитанции = квитанция.Номер,
                            ДатаКвитанции = квитанция.Дата,
                            ТелефонСервиса = ТелефонСервиса,
                            НомерДок = приемВРемонт.НомерДок,
                            ДатаДок = docDate.ToString("dd.MM.yyyy"),
                            Комментарий = Комментарий,
                            Заказчик = контрагент.Наименование + " " + Телефон.Номер ?? Почта.Адрес,
                            Мастер = Мастер,
                            ДатаПродажи = приемВРемонт.ДатаПродажи == Common.min1cDate ? "" : приемВРемонт.ДатаПродажи.ToString("dd.MM.yyyy"),
                            Изделие = Изделие.Наименование,
                            Артикул = Изделие.Артикул,
                            ЗаводскойНомер = приемВРемонт.ЗаводскойНомер,
                            Производитель = Изделие.Производитель,
                            НомерРемонта = приемВРемонт.НомерРемонта,
                            Комплектность = Комплектность,
                            Склад = склад.Наименование,
                            МестоХранения = МестоХранения,
                            Неисправность = Неисправность,
                            Неисправность2 = Неисправность2,
                            Неисправность3 = Неисправность3,
                            Неисправность4 = Неисправность4,
                            Неисправность5 = Неисправность5
                        };

                        message = message.CreateOrUpdateHtmlPrintPage("ПриемВРемонт", ДанныеПечати);

                        if (приемВРемонт.ТипРемонта == 0 && авансовыеРаботы != null && авансовыеРаботы.Count > 0)
                        {
                            decimal СуммаДокумента = авансовыеРаботы.Sum(x => x.Сумма);
                            var Работы = from sc9875 in _context.Sc9875s
                                         where авансовыеРаботы.Select(x => x.Id).Contains(sc9875.Id)
                                         select new
                                         {
                                             Код = sc9875.Id,
                                             Наименование = sc9875.Descr.Trim()
                                         };
                            АктВыполненныхРаботПечать ДанныеПечатиАкта = new АктВыполненныхРаботПечать
                            {
                                ЮрЛицо = ДанныеФирмы.ЮрЛицо,
                                ИНН = ДанныеФирмы.ИНН,
                                Адрес = ДанныеФирмы.Адрес.Trim().Replace(Environment.NewLine, ""),
                                Банк = ДанныеФирмы.Банк,
                                БИК = ДанныеФирмы.БИК,
                                городБанка = ДанныеФирмы.городБанка,
                                РасчетныйСчет = ДанныеФирмы.РасчетныйСчет,
                                КоррСчет = ДанныеФирмы.КоррСчет,
                                ТелефонСервиса = ТелефонСервиса,
                                НомерДок = приемВРемонт.НомерДок,
                                ДатаДок = docDate.ToString("dd.MM.yyyy"),
                                Изделие = Изделие.Наименование,
                                ЗаводскойНомер = приемВРемонт.ЗаводскойНомер,
                                Производитель = Изделие.Производитель,
                                НомерКвитанции = приемВРемонт.КвитанцияId,
                                ИтогоСумма = СуммаДокумента.ToString("0.00", CultureInfo.InvariantCulture),
                                КоличествоУслуг = авансовыеРаботы.Count,
                                СуммаПрописью = СуммаДокумента.Прописью()
                            };
                            foreach (var rab in авансовыеРаботы)
                            {
                                ДанныеПечатиАкта.ТаблЧасть.Add(new КорзинаРаботПечать
                                {
                                    Работа = Работы.Where(x => x.Код == rab.Id).Select(x => x.Наименование).FirstOrDefault(),
                                    Единица = "шт",
                                    КолВо = rab.Quantity.ToString("0", CultureInfo.InvariantCulture),
                                    Цена = rab.Цена.ToString("0.00", CultureInfo.InvariantCulture),
                                    Сумма = rab.Сумма.ToString("0.00", CultureInfo.InvariantCulture),
                                });
                            }    
                            message = message.CreateOrUpdateHtmlPrintPage("АктВыполненныхРабот", ДанныеПечатиАкта);
                        }

                        if (!string.IsNullOrEmpty(Телефон.Номер))
                            Common.ОтправитьSms(Телефон.Номер, 9899, Изделие.Наименование, приемВРемонт.КвитанцияId);
                        if (!string.IsNullOrEmpty(Почта.Адрес))
                        {
                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var EmailSender = scope.ServiceProvider.GetService<IEmailSender>();
                                await EmailSender.SendEmailAsync(Почта.Адрес, приемВРемонт.КвитанцияId, message);
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        docTran.Rollback();
                        if (ex.Number == -2)
                            return StatusCode(501, "timeout");
                        else
                            return StatusCode(502, ex.Message + Environment.NewLine + ex.InnerException);
                    }
                    catch (Exception ex)
                    {
                        docTran.Rollback();
                        return StatusCode(503, ex.Message + Environment.NewLine + ex.InnerException);
                    }
                }
                //byte[] image = System.IO.File.ReadAllBytes(@"F:\tmp\5.pdf");
                //message = Convert.ToBase64String(image);
                return Ok(message);//File(image, "application/pdf"); //
            }
            else
                return StatusCode(500);
        }

        [HttpPost]
        public IActionResult UnloadDoc(string НомерДок)
        {
            Common.UnLockDocNo(_context, "9899", НомерДок);
            return Ok();
        }

    }
}
