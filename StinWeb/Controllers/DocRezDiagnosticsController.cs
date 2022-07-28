using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class DocRezDiagnosticsController : Controller
    {
        private readonly ILogger _logger;
        private readonly StinDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DocRezDiagnosticsController(StinDbContext context, ILogger<DocRezDiagnosticsController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }
        [Authorize]
        public IActionResult Index(string Kvit, string IzdelieId, decimal Garantia)
        //public ViewResult Index(string? id)
        {
            ViewBag.Квитанция = Kvit ?? "";
            ViewBag.ИзделиеId = IzdelieId ?? "";
            ViewBag.Гарантия = Convert.ToInt32(Garantia);

            return View();
        }
        [HttpGet]
        public PartialViewResult IndexGrid()
        {
            var basket = Common.GetObjectFromJson<List<BasketНоменклатура>>(HttpContext.Session, "cart");
            if (basket == null)
                basket = new List<BasketНоменклатура>();
            return PartialView("_IndexBasketНоменклатура", basket);
        }

        [HttpGet]
        public PartialViewResult IndexКорзинаРабот()
        {
            var basket = Common.GetObjectFromJson<List<КорзинаРабот>>(HttpContext.Session, "КорзинаРабот");
            if (basket == null)
                basket = new List<КорзинаРабот>();
            return PartialView("_IndexКорзинаРабот", basket);
        }

        [HttpPost]
        public IActionResult Clear()
        {
            Common.SetObjectAsJson(HttpContext.Session, "cart", null);
            Common.SetObjectAsJson(HttpContext.Session, "КорзинаРабот", null);
            return View();
        }
        [HttpPost]
        public IActionResult ClearKey(string Key)
        {
            Common.SetObjectAsJson(HttpContext.Session, Key, null);
            return Ok();
        }
        [HttpGet]
        public IActionResult GetImage(string id)
        {
            var imageData = (from v in _context.VzImages
                               where v.Id == id
                               select new {
                                   image = v.Image,
                                   type = v.Dtype ?? "png"
                               }).FirstOrDefault();
            byte[] image = null;
            string imageType = "image/png";
            if (imageData != null && imageData.image != null)
            {
                image = imageData.image;
                switch (imageData.type.Trim().ToLower())
                {
                    case "pdf":
                        imageType = "application/pdf";
                        break;
                    case "png":
                        imageType = "image/png";
                        break;
                    default:
                        imageType = "image/png";
                        break;
                }
            }
            else
            {
                string webRootPath = _webHostEnvironment.WebRootPath;
                string contentRootPath = _webHostEnvironment.ContentRootPath;

                string path = System.IO.Path.Combine(webRootPath, "lib", "images", "not-found-image.jpg");
                image = System.IO.File.ReadAllBytes(path);
            }
            return File(image, imageType);
        }
        [HttpPost]
        public IActionResult ClearAndLoad(string Квитанция)
        {
            Common.SetObjectAsJson(HttpContext.Session, "cart", null);
            Common.SetObjectAsJson(HttpContext.Session, "КорзинаРабот", null);
            Common.SetObjectAsJson(HttpContext.Session, "ТипРемонта", null);
            Common.SetObjectAsJson(HttpContext.Session, "Описание", null);

            string[] kv = Квитанция.Split('-');
            if (kv.Length == 2)
            {
                string НомерКвитанции = kv[0];
                int ДатаКвитанции = Convert.ToInt32(kv[1]);

                string РезультатДиагностикиId = 
                    (from rg10471 in _context.Rg10471s
                    join dh9899 in _context.Dh9899s on rg10471.Sp10469 equals dh9899.Iddoc
                    join __1scrdoc in _context._1scrdocs on "O1" + Common.Encode36(9899).PadLeft(4) + dh9899.Iddoc equals __1scrdoc.Parentval into crdoc
                    from __1scrdoc in crdoc.DefaultIfEmpty()
                    join j in _context._1sjourns.Where(x => x.Iddocdef == 11037 && x.Closed == 0 && x.Ismark == false) on __1scrdoc.Childid equals j.Iddoc
                    where rg10471.Period == Common.GetRegTA(_context)
                    && rg10471.Sp10470 > 0
                    && rg10471.Sp10467 == НомерКвитанции
                    && rg10471.Sp10468 == ДатаКвитанции
                    select j.Iddoc).FirstOrDefault();
                if (!string.IsNullOrEmpty(РезультатДиагностикиId))
                {
                    var docHeader = (from dh11037 in _context.Dh11037s
                                    where dh11037.Iddoc == РезультатДиагностикиId
                                    select new
                                    {
                                        гарантия = Convert.ToInt32(dh11037.Sp11001),
                                        требуется = dh11037.Sp11364
                                    }).FirstOrDefault();
                    if (docHeader != null)
                    {
                        Common.SetObjectAsJson(HttpContext.Session, "ТипРемонта", docHeader.гарантия);
                        Common.SetObjectAsJson(HttpContext.Session, "Описание", docHeader.требуется);
                    }
                    var docTable = from dt11037 in _context.Dt11037s
                                   join sc84 in _context.Sc84s on dt11037.Sp11022 equals sc84.Id into _sc84
                                   from sc84 in _sc84.DefaultIfEmpty()
                                   join sc9875 in _context.Sc9875s on dt11037.Sp11029 equals sc9875.Id into _sc9875
                                   from sc9875 in _sc9875.DefaultIfEmpty()
                                   where dt11037.Iddoc == РезультатДиагностикиId
                                   select new
                                   {
                                       номенклатура = new Номенклатура { Id = sc84.Id, Code = sc84.Code, Артикул = sc84.Sp85.Trim(), Наименование = sc84.Descr.Trim()},
                                       КолВо = dt11037.Sp11024,
                                       работа = new Работа { Id = sc9875.Id, Артикул = sc9875.Sp11503.Trim(), АртикулОригинал = sc9875.Sp12644.Trim(), Наименование = sc9875.Descr.Trim()},
                                       КолВоР = dt11037.Sp11033
                                   };
                    List<BasketНоменклатура> cart = new List<BasketНоменклатура>();
                    List<КорзинаРабот> cartR = new List<КорзинаРабот>();
                    foreach (var a in docTable)
                    {
                        if (a.номенклатура.Id != null)
                            cart.Add(new BasketНоменклатура { Номенклатура = a.номенклатура, Quantity = a.КолВо });
                        if (a.работа.Id != null)
                            cartR.Add(new КорзинаРабот { Работа = a.работа, Quantity = a.КолВоР });
                    }
                    Common.SetObjectAsJson(HttpContext.Session, "cart", cart);
                    Common.SetObjectAsJson(HttpContext.Session, "КорзинаРабот", cartR);
                }
            }
            return View();
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> OnBtnOk(string Квитанция, bool Гарантийный, string Требуется)
        {
            string[] kv = Квитанция.Split('-');
            if (kv.Length == 2)
            {
                string НомерКвитанции = kv[0];
                int ДатаКвитанции = Convert.ToInt32(kv[1]);
                if (Требуется == null)
                    Требуется = "";
                DateTime period = Common.GetRegTA(_context);
                var prefixDB = Common.ПрефиксИБ(_context);
                string num36 =
                    (from rg10471 in _context.Rg10471s
                     join dh9899 in _context.Dh9899s on rg10471.Sp10469 equals dh9899.Iddoc
                     join __1scrdoc in _context._1scrdocs on "O1" + Common.Encode36(9899).PadLeft(4) + dh9899.Iddoc equals __1scrdoc.Parentval into crdoc
                     from __1scrdoc in crdoc.DefaultIfEmpty()
                     join j in _context._1sjourns.Where(x => x.Iddocdef == 11037 && x.Closed == 0 && x.Ismark == false) on __1scrdoc.Childid equals j.Iddoc
                     where rg10471.Period == period
                                     && rg10471.Sp10470 > 0
                                     && rg10471.Sp10467 == НомерКвитанции
                                     && rg10471.Sp10468 == ДатаКвитанции
                     select j.Iddoc).FirstOrDefault();
                bool IsNew = string.IsNullOrEmpty(num36);
                using (var docTran = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (string.IsNullOrEmpty(num36))
                        {
                            num36 = Common.GenerateIdDoc(_context);
                        }
                        var datestr = DateTime.Now.ToString("yyyyMMdd");
                        var h = DateTime.Now.Hour;
                        var m = DateTime.Now.Minute;
                        var s = DateTime.Now.Second;
                        var ms = DateTime.Now.Millisecond;
                        var time = (h * 3600 * 10000) + (m * 60 * 10000) + (s * 10000) + (ms * 100);
                        var timestr = Common.Encode36(time).PadLeft(6);
                        var dateTimeIddoc = datestr + timestr + num36;
                        var партииМастерской = (from rg9972 in _context.Rg9972s
                                                join sc172 in _context.Sc172s on rg9972.Sp9964 equals sc172.Id into _sc172
                                                from sc172 in _sc172.DefaultIfEmpty()
                                                where rg9972.Period == period
                                                && rg9972.Sp9970 > 0
                                                && rg9972.Sp9969 == НомерКвитанции
                                                && rg9972.Sp10084 == ДатаКвитанции
                                                select new
                                                {
                                                    КонтрагентId = sc172.Id ?? Common.ПустоеЗначение,
                                                    Контрагент = sc172.Descr.Trim() ?? "",
                                                    ИзделиеId = rg9972.Sp9960,
                                                    ЗавНомер = rg9972.Sp9961.Trim(),
                                                    Гарантия = rg9972.Sp9958,
                                                    ДатаПриема = rg9972.Sp9967,
                                                    СтатусПартии = rg9972.Sp9963,
                                                    СкладОткуда = rg9972.Sp10083
                                                }).FirstOrDefault();
                        var НомерПриемаВремонт = (from rg10471 in _context.Rg10471s
                                                  join dh9899 in _context.Dh9899s on rg10471.Sp10469 equals dh9899.Iddoc
                                                  join sc9866 in _context.Sc9866s on dh9899.Sp9892 equals sc9866.Id into _sc9866
                                                  from sc9866 in _sc9866.DefaultIfEmpty()
                                                  where rg10471.Period == period
                                                  && rg10471.Sp10470 > 0
                                                  && rg10471.Sp10467 == НомерКвитанции
                                                  && rg10471.Sp10468 == ДатаКвитанции
                                                  select new
                                                  {
                                                      Неисправность = sc9866.Id ?? Common.ПустоеЗначение,
                                                      ДатаПродажи = dh9899.Sp9896,
                                                      НомерРемонта = dh9899.Sp9897,
                                                      ДокПриемВремонтId = dh9899.Iddoc,
                                                      ДокПриемВремонтId13 = Common.Encode36(9899).PadLeft(4) + dh9899.Iddoc,
                                                      Комментарий = dh9899.Sp660.Trim()
                                                  }).FirstOrDefault();
                        decimal гарантия;
                        switch (партииМастерской.Гарантия)
                        {
                            case 0:
                                гарантия = 4;
                                break;
                            case 1:
                                гарантия = Гарантийный == true ? 1 : 0;
                                break;
                            case 2:
                                гарантия = Гарантийный == true ? 2 : 0;
                                break;
                            case 3:
                                гарантия = Гарантийный == true ? 1 : 0;
                                break;
                            case 4:
                                гарантия = Гарантийный == true ? 1 : 4;
                                break;
                            default:
                                гарантия = 4;
                                break;
                        };
                        if (!IsNew)
                        {
                            _1sjourn journ = (from _j in _context._1sjourns
                                              where _j.Iddoc == num36
                                              select _j).FirstOrDefault();
                            if (journ != null)
                            {
                                journ.Verstamp = journ.Verstamp + 1;
                                journ.DateTimeIddoc = dateTimeIddoc;
                            }
                            else
                                return NoContent();
                            Dh11037 docH = (from dh in _context.Dh11037s
                                            where dh.Iddoc == num36
                                            select dh).FirstOrDefault();
                            if (docH != null)
                            {
                                docH.Sp11001 = гарантия;
                                docH.Sp11364 = Требуется.Length > 100 ? Требуется.Substring(0, 100) : Требуется;
                            }
                            else
                                return NoContent();
                            _context.Update(journ);
                            _context.Update(docH);
                            var Dt11037s = from dt in _context.Dt11037s
                                           where dt.Iddoc == num36
                                           select dt;
                            foreach (Dt11037 row in Dt11037s)
                            {
                                _context.Dt11037s.Remove(row);
                            }
                            var crDocs = from cr in _context._1scrdocs
                                         where cr.Childid == num36
                                         select cr;
                            foreach (var row in crDocs)
                            {
                                _context._1scrdocs.Remove(row);
                            }
                        }
                        else
                        {

                            var остаткиИзделий = (from rg11049 in _context.Rg11049s
                                                  join sc55 in _context.Sc55s on rg11049.Sp11044 equals sc55.Id
                                                  where rg11049.Period == period
                                                  && rg11049.Sp11047 > 0
                                                  && rg11049.Sp11042 == НомерКвитанции
                                                  && rg11049.Sp11043 == ДатаКвитанции
                                                  select new
                                                  {
                                                      СкладId = sc55.Id,
                                                      Склад = sc55.Descr.Trim(),
                                                      ПодСклад = rg11049.Sp11045,
                                                      Слесарь = rg11049.Sp11046
                                                  }).FirstOrDefault();
                            string ФирмаДляОпта = (from _const in _context._1sconsts
                                                   where _const.Id == 8959 && _const.Objid == "     0   " && _const.Date <= Common.min1cDate
                                                   orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                                                   select _const.Value).FirstOrDefault();
                            string ФирмаДляОпта2 = (from _const in _context._1sconsts
                                                    where _const.Id == 9834 && _const.Objid == "     0   " && _const.Date <= Common.min1cDate
                                                    orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                                                    select _const.Value).FirstOrDefault();
                            string ФирмаДляОпта3 = (from _const in _context._1sconsts
                                                    where _const.Id == 9852 && _const.Objid == "     0   " && _const.Date <= Common.min1cDate
                                                    orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                                                    select _const.Value).FirstOrDefault();
                            string СвояФирма = (from _const in _context._1sconsts
                                                where _const.Id == 8958 && _const.Objid == "     0   " && _const.Date <= Common.min1cDate
                                                orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                                                select _const.Value).FirstOrDefault();
                            string ИнфоИзПользователей = (from _const in _context._1sconsts
                                                          where _const.Id == 11599 && _const.Objid == "     0   " && _const.Date <= Common.min1cDate
                                                          orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                                                          select _const.Value).FirstOrDefault();
                            if (ИнфоИзПользователей == "1")
                            {
                                string ПользовательРознФирма = (from sc30 in _context.Sc30s
                                                                where sc30.Id == User.FindFirstValue("UserId")
                                                                select sc30.Sp11600).FirstOrDefault();
                                if (!string.IsNullOrEmpty(ПользовательРознФирма) && ПользовательРознФирма != Common.ПустоеЗначение)
                                    СвояФирма = ПользовательРознФирма;
                            }
                            var idDocDef = 11037; //Результат диагностики
                            var dnPrefix = idDocDef.ToString().PadLeft(10) + DateTime.Now.ToString("yyyy").PadRight(8);
                            var ФирмаЮрЛицо = (from sc131 in _context.Sc131s
                                               join sc4014 in _context.Sc4014s on sc131.Id equals sc4014.Sp4011
                                               where sc4014.Id == Common.FirmaSS
                                               select new
                                               {
                                                   Фирма = sc4014.Descr.Trim(),
                                                   ЮрЛицоId = sc131.Id,
                                                   ЮрЛицоПрефикс = sc131.Sp145.Trim()
                                               }).FirstOrDefault();
                            string prefixFr = ФирмаЮрЛицо.ЮрЛицоПрефикс;
                            string prefix = prefixDB + prefixFr;
                            var number = (from _j in _context._1sjourns
                                          where _j.Dnprefix == dnPrefix && string.Compare(_j.Docno, prefix) >= 0 && _j.Docno.Substring(0, prefix.Length) == prefix
                                          orderby _j.Dnprefix descending, _j.Docno descending
                                          select _j.Docno).FirstOrDefault();
                            if (number == null)
                                number = "0";
                            else
                                number = number.Substring(prefix.Length);
                            number = prefix + (Convert.ToInt32(number) + 1).ToString().PadLeft(10 - prefix.Length, '0');
                            _1sjourn j = new _1sjourn
                            {
                                //RowId = _context._1sjourn.Max(e => e.RowId) + 1,
                                Idjournal = 10528,
                                Iddoc = num36,
                                Iddocdef = idDocDef,
                                Appcode = 1,
                                DateTimeIddoc = dateTimeIddoc,
                                Dnprefix = dnPrefix,
                                Docno = number,
                                Closed = 0,
                                Ismark = false,
                                Actcnt = 0,
                                Verstamp = 1,
                                Sp74 = User.FindFirstValue("UserId"),
                                Sp798 = Common.ПустоеЗначение,
                                Sp4056 = Common.FirmaSS,
                                Sp5365 = ФирмаЮрЛицо.ЮрЛицоId,
                                Sp8662 = prefixDB,
                                Sp8663 = prefixDB + ";" + (остаткиИзделий.Склад.Length > (29 - prefixDB.Length) ? остаткиИзделий.Склад.Substring(0, 29 - prefixDB.Length) : остаткиИзделий.Склад),
                                Sp8664 = prefixDB + ";" + (партииМастерской.Контрагент.Length > (29 - prefixDB.Length) ? партииМастерской.Контрагент.Substring(0, 29 - prefixDB.Length) : партииМастерской.Контрагент),
                                Sp8665 = prefixDB + ";РезультатДиагностики",
                                Sp8666 = prefixDB + ";" + (ФирмаЮрЛицо.Фирма.Length > (29 - prefixDB.Length) ? ФирмаЮрЛицо.Фирма.Substring(0, 29 - prefixDB.Length) : ФирмаЮрЛицо.Фирма),
                                Sp8720 = "",
                                Sp8723 = ""
                            };
                            Dh11037 docHeader = new Dh11037
                            {
                                Iddoc = num36,
                                Sp10996 = партииМастерской.КонтрагентId,
                                Sp10997 = партииМастерской.ИзделиеId,
                                Sp10998 = партииМастерской.ЗавНомер,
                                Sp10999 = НомерПриемаВремонт.Неисправность,
                                Sp11000 = партииМастерской.Гарантия,
                                Sp11001 = гарантия,
                                Sp11003 = НомерПриемаВремонт.ДатаПродажи,
                                Sp11004 = партииМастерской.ДатаПриема,
                                Sp11005 = НомерПриемаВремонт.НомерРемонта,
                                Sp11006 = НомерКвитанции,
                                Sp11007 = ДатаКвитанции,
                                Sp11008 = остаткиИзделий.СкладId,
                                Sp11009 = остаткиИзделий.ПодСклад,
                                Sp11010 = 1,
                                Sp11011 = партииМастерской.СтатусПартии,
                                Sp11012 = партииМастерской.СкладОткуда,
                                Sp11013 = НомерПриемаВремонт.ДокПриемВремонтId13,
                                Sp11014 = 0,
                                Sp11015 = 0,
                                Sp11016 = остаткиИзделий.Слесарь,
                                Sp11017 = ФирмаДляОпта,
                                Sp11018 = ФирмаДляОпта2,
                                Sp11019 = ФирмаДляОпта3,
                                Sp11020 = СвояФирма,
                                Sp11364 = Требуется.Length > 100 ? Требуется.Substring(0, 100) : Требуется,
                                Sp11027 = 0,
                                Sp11028 = 0,
                                Sp11030 = 0,
                                Sp11031 = 0,
                                Sp11032 = 0,
                                Sp660 = НомерПриемаВремонт.Комментарий
                            };
                            await _context._1sjourns.AddAsync(j);
                            await _context.Dh11037s.AddAsync(docHeader);
                        }
                        string РозничныйТипЦен = (from _const in _context._1sconsts
                                                  where _const.Id == 6500 && _const.Objid == "     0   " && _const.Date <= Common.min1cDate
                                                  orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                                                  select _const.Value).FirstOrDefault();
                        string ЗакупочныйТипЦен = (from _const in _context._1sconsts
                                                   where _const.Id == 8943 && _const.Objid == "     0   " && _const.Date <= Common.min1cDate
                                                   orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                                                   select _const.Value).FirstOrDefault();
                        short lineNo = 1;
                        List<BasketНоменклатура> корзина = Common.GetObjectFromJson<List<BasketНоменклатура>>(HttpContext.Session, "cart");
                        if (корзина != null && корзина.Count > 0)
                        {
                            var номенклатура = from sc84 in _context.Sc84s
                                               join цены in _context.VzTovars on sc84.Id equals цены.Id into _цены
                                               from цены in _цены.DefaultIfEmpty()
                                               where корзина.Select(e => e.Номенклатура.Id).Contains(sc84.Id)
                                               select new
                                               {
                                                   НоменклатураId = sc84.Id,
                                                   ЕдиницаId = sc84.Sp94,
                                                   Цена = цены.Rozn ?? 0,
                                                   ЦенаЗак = цены.Zakup ?? 0
                                               };
                            foreach (BasketНоменклатура drop in корзина)
                            {
                                var dropНоменклатура = номенклатура.Where(e => e.НоменклатураId == drop.Номенклатура.Id).FirstOrDefault();
                                Dt11037 docRow = new Dt11037
                                {
                                    Iddoc = num36,
                                    Lineno = lineNo++,
                                    Sp11021 = Common.ПустоеЗначение,
                                    Sp11022 = drop.Номенклатура.Id,
                                    Sp11023 = dropНоменклатура.ЕдиницаId,
                                    Sp11024 = drop.Quantity, //кол запчастей
                                    Sp11025 = dropНоменклатура.Цена, //цена запчасти
                                    Sp11026 = dropНоменклатура.ЦенаЗак, //цена запчасти поставщика
                                    Sp11027 = dropНоменклатура.Цена * drop.Quantity, //сумма запчастей
                                    Sp11028 = dropНоменклатура.ЦенаЗак * drop.Quantity, //сумма запчастей поставщика
                                    Sp11029 = Common.ПустоеЗначение,
                                    Sp11030 = 0, //сумма работы платная
                                    Sp11031 = 0, //сумма работы поставщика
                                    Sp11032 = dropНоменклатура.Цена * drop.Quantity, //сумма (равна sp27 + sp30)
                                    Sp11033 = 0, //кол работ
                                    Sp11034 = 0, //цена работы
                                    Sp11035 = 0 //цена работы поставщика
                                };
                                await _context.Dt11037s.AddAsync(docRow);
                            }
                        }
                        List<КорзинаРабот> корзинаР = Common.GetObjectFromJson<List<КорзинаРабот>>(HttpContext.Session, "КорзинаРабот");
                        if (корзинаР != null && корзинаР.Count > 0)
                        {
                            var работа = from sc9875 in _context.Sc9875s
                                         from цены in _context._1sconsts.Where(x => x.Id == 9872 && x.Objid == sc9875.Id).OrderByDescending(e => e.Date).ThenByDescending(e => e.Time).Take(1)
                                         from ценыЗ in _context._1sconsts.Where(x => x.Id == 9873 && x.Objid == sc9875.Id).OrderByDescending(e => e.Date).ThenByDescending(e => e.Time).Take(1)
                                         where корзинаР.Select(x => x.Работа.Id).Contains(sc9875.Id)
                                         select new
                                         {
                                             Id = sc9875.Id,
                                             Цена = Convert.ToDecimal(цены.Value ?? "0"),
                                             ЦенаЗак = Convert.ToDecimal(ценыЗ.Value ?? "0")
                                         };
                            foreach (КорзинаРабот drop in корзинаР)
                            {
                                var dropРабота = работа.Where(x => x.Id == drop.Работа.Id).FirstOrDefault();
                                Dt11037 docRow = new Dt11037
                                {
                                    Iddoc = num36,
                                    Lineno = lineNo++,
                                    Sp11021 = Common.ПустоеЗначение,
                                    Sp11022 = Common.ПустоеЗначение,
                                    Sp11023 = Common.ПустоеЗначение,
                                    Sp11024 = 0, //кол запчастей
                                    Sp11025 = 0, //цена запчасти
                                    Sp11026 = 0, //цена запчасти поставщика
                                    Sp11027 = 0, //сумма запчастей
                                    Sp11028 = 0, //сумма запчастей поставщика
                                    Sp11029 = drop.Работа.Id,
                                    Sp11030 = dropРабота.Цена * drop.Quantity, //сумма работы платная
                                    Sp11031 = dropРабота.ЦенаЗак * drop.Quantity, //сумма работы поставщика
                                    Sp11032 = dropРабота.Цена * drop.Quantity, //сумма (равна sp27 + sp30)
                                    Sp11033 = drop.Quantity, //кол работ
                                    Sp11034 = dropРабота.Цена, //цена работы
                                    Sp11035 = dropРабота.ЦенаЗак //цена работы поставщика
                                };
                                await _context.Dt11037s.AddAsync(docRow);
                            }
                        }
                        await _context.SaveChangesAsync();

                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_DH11037_UpdateTotals @num36", new SqlParameter("@num36", num36));

                        SqlParameter paramParentVal = new SqlParameter("@parentVal", "O1" + НомерПриемаВремонт.ДокПриемВремонтId13);
                        SqlParameter paramDateTimeIddoc = new SqlParameter("@date_time_iddoc", dateTimeIddoc);
                        SqlParameter paramNum36 = new SqlParameter("@num36", num36);
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp__1SCRDOC_Write 0,@parentVal,@date_time_iddoc,@num36,1", paramParentVal, paramDateTimeIddoc, paramNum36);

                        var signs = (from dbset in _context._1sdbsets
                                     where dbset.Dbsign.Trim() != prefixDB
                                     select dbset.Dbsign).ToList();
                        foreach (string sign in signs)
                        {
                            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RegisterUpdate @sign,11037,@num36,' '", new SqlParameter("@sign", sign), new SqlParameter("@num36", num36));
                        }
                        await _context.SaveChangesAsync();

                        docTran.Commit();
                    }
                    catch
                    {
                        docTran.Rollback();
                        return NoContent();
                    }
                }
                return Ok();
            }
            else
                return NoContent();
        }
    }
}
