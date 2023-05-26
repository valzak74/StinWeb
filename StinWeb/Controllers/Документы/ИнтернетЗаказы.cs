using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using System.Security.Claims;
using StinWeb.Models.Repository.Справочники;
using StinWeb.Models.DataManager.Отчеты;
using StinWeb.Models.DataManager.Документы;
using Microsoft.AspNetCore.Mvc.Rendering;
using NonFactors.Mvc.Lookup;
using StinWeb.Lookups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;
using StinClasses.Models;
using JsonExtensions;
using HttpExtensions;
using System.Threading;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace StinWeb.Controllers
{
    public class ИнтернетЗаказы : Controller
    {
        bool disposed = false;
        private IHttpService _httpService;
        private StinDbContext _context;
        private UserRepository _userRepository;
        private ФирмаRepository _фирмаRepository;
        private СкладRepository _складRepository;
        private КонтрагентRepository _контрагентRepository;
        private НоменклатураRepository _номенклатураRepository;
        private string MaxIddoc;
        private string MaxDateTimeIddoc;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private StinClasses.Справочники.IOrder _order;
        private StinClasses.Справочники.IНоменклатура _номенклатура;
        private StinClasses.Справочники.IСообщения _сообщения;
        private StinClasses.Документы.IНабор _набор;
        private StinClasses.Регистры.IРегистрНаборНаСкладе _регистрНабор;

        StinClasses.Справочники.Functions.IOrderFunctions _orderFunctions;

        public ИнтернетЗаказы(StinDbContext context, IWebHostEnvironment webHostEnvironment, IHttpService httpService, StinClasses.Справочники.Functions.IOrderFunctions orderFunctions)
        {
            _httpService = httpService;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userRepository = new UserRepository(context);
            _фирмаRepository = new ФирмаRepository(context);
            _складRepository = new СкладRepository(context);
            _контрагентRepository = new КонтрагентRepository(context);
            _номенклатураRepository = new НоменклатураRepository(context);
            _order = new StinClasses.Справочники.OrderEntity(context);
            _номенклатура = new StinClasses.Справочники.НоменклатураEntity(context);
            _сообщения = new StinClasses.Справочники.СообщенияEntity(context);
            _набор = new StinClasses.Документы.Набор(context);
            _регистрНабор = new StinClasses.Регистры.Регистр_НаборНаСкладе(context);
            _orderFunctions = orderFunctions;
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _userRepository.Dispose();
                    _фирмаRepository.Dispose();
                    _складRepository.Dispose();
                    _контрагентRepository.Dispose();
                    _номенклатураRepository.Dispose();
                    _order.Dispose();
                    _номенклатура.Dispose();
                    _сообщения.Dispose();
                    _набор.Dispose();
                    _регистрНабор.Dispose();
                    _context.Dispose();
                    base.Dispose(disposing);
                }
            }
            this.disposed = true;
        }
        public IActionResult Index()
        {
            return View();
        }
        [Authorize]
        public async Task<IActionResult> CreateNewDoc()
        {
            if (_context.NeedToOpenPeriod())
                return Redirect("/Home/Error?Description=ПериодНеОткрыт");
            HttpContext.Session.SetObjectAsJson("мнТабличнаяЧасть", null);
            var Пользователь = await _userRepository.GetUserByRowIdAsync(Int32.Parse(User.FindFirstValue("UserRowId")));

            var doc = new ИнтернетЗаказ(Пользователь, Пользователь.ОсновнаяФирма, null, Пользователь.ОсновнойСклад);

            ViewBag.Фирмы = new SelectList(_фирмаRepository.СписокСвоихФирм(), "Id", "Наименование");
            ViewBag.Склады = new SelectList(_складRepository.ПолучитьРазрешенныеСклады(Пользователь.Id), "Id", "Наименование");

            return View(doc);
        }
        [Authorize]
        public async Task<IActionResult> Console()
        {
            var Пользователь = await _userRepository.GetUserByRowIdAsync(Int32.Parse(User.FindFirstValue("UserRowId")));

            var CampaignIds = _context.Sc14042s
                .Where(x => !x.Ismark && (x.Sp14164.Trim().ToUpper() == "FBS"))
                .Select(x => new { CampaignId = x.Code.Trim(), Description = x.Sp14155.Trim() + " " + x.Descr.Trim() })
                .Concat(
                _context.Sc14042s
                .Where(x => !x.Ismark && (x.Sp14155.Trim().ToUpper() == "OZON") && (x.Sp14164.Trim().ToUpper() == "FBS") && !string.IsNullOrWhiteSpace(x.Sp14154))
                .Select(x => new { CampaignId = x.Code.Trim() + "/" + x.Sp14154.Trim(), Description = x.Sp14155.Trim() + " " + x.Sp14195.Trim() })
                )
                .OrderBy(x => x.Description);
            ViewBag.CampaignIds = new SelectList(CampaignIds, "CampaignId", "Description");

            var типыОплат = new List<Tuple<int, string>>();
            типыОплат.Add(new((int)StinClasses.ReceiverPaymentType.Наличными, "Наличными"));
            типыОплат.Add(new((int)StinClasses.ReceiverPaymentType.БанковскойКартой, "Банковской картой"));
            ViewBag.PaymentTypes = new SelectList(типыОплат.AsEnumerable(), "Item1", "Item2");

            ViewBag.ЭтоВодитель = Пользователь.Department.ToLower().Trim() == "водители";
            ViewBag.DriverId = Пользователь.Role.Replace(" ", "_");
            ViewBag.СкладыСтрокой = string.Join(',', _складRepository.ПолучитьРазрешенныеСклады(Пользователь.Id).Select(x => x.Id));
            ViewBag.DefaultEmail = Startup.sConfiguration["Settings:DefaultEmailforCashReceipt"];
            return View("Console");
        }
        [Authorize]
        public IActionResult NaborPrintLabel()
        {
            return View("NaborPrintLabel");
        }
        [HttpPost]
        public async Task<IActionResult> NaborScan(string barcodeText, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(barcodeText) && ((barcodeText.Length == 13) || (barcodeText.Length == 14)) && (barcodeText.Substring(0,4) == "%97W"))
            {
                var docId = barcodeText.Substring(4).Replace('%', ' ');
                if (barcodeText.Length == 14)
                    docId = docId.Remove(docId.Length - 1);
                var formNabor = await _набор.GetФормаНаборById(docId);
                if (formNabor == null)
                    return StatusCode(502, "Не удалось получить форму набора");
                if (!formNabor.Общие.Проведен)
                    return StatusCode(502, "Набор не проведен");
                //if (formNabor.Завершен)
                //    return StatusCode(502, "Набор уже завершен");
                if (formNabor.Order?.InternalStatus == 5)
                    return StatusCode(502, "Заказ уже отменен");
                if (!_набор.IsActive(docId))
                    return StatusCode(502, "Набор отменен");
                //if (formNabor.StartCompectation <= Common.min1cDate)
                //    return StatusCode(502, "Набор не начат");
                var printData = await _набор.PrintForm("", 1, 0, formNabor, cancellationToken);
                return Ok(printData.html);
            }
            return StatusCode(502, "Недопустимое значение штрихкода");
        }
        [Authorize]
        public async Task<IActionResult> LoadingList(CancellationToken cancellationToken)
        {
            var Пользователь = await _userRepository.GetUserByRowIdAsync(Int32.Parse(User.FindFirstValue("UserRowId")));
            var CampaignIds = _context.Sc14042s
                .Where(x => !x.Ismark && (x.Sp14164.Trim().ToUpper() == "FBS") && (x.Sp14155.Trim().ToUpper() != "ЯНДЕКС"))
                .Select(x => new { CampaignId = x.Id.Replace(" ","_"), Description = x.Sp14155.Trim() + " " + x.Descr.Trim() })
                .AsEnumerable()
                .Concat(
                _context.Sc14042s
                .Where(x => !x.Ismark && (x.Sp14155.Trim().ToUpper() == "OZON") && (x.Sp14164.Trim().ToUpper() == "FBS") && !string.IsNullOrWhiteSpace(x.Sp14154))
                .Select(x => new { CampaignId = x.Id.Replace(" ", "_") + "/" + x.Sp14154.Trim(), Description = x.Sp14155.Trim() + " " + x.Sp14195.Trim() })
                .AsEnumerable()
                )
                .Concat(
                _context.Sc14042s
                .Where(x => !x.Ismark && (x.Sp14164.Trim().ToUpper() == "FBS") && (x.Sp14155.Trim().ToUpper() == "ЯНДЕКС"))
                .AsEnumerable()
                .GroupBy(x => x.Sp14155.Trim().ToUpper())
                .Select(gr => new { CampaignId = string.Join(',', gr.Select(y => y.Id.Replace(" ", "_"))), Description = "Яндекс FBS (все маркетплейс)" })
                )
                .OrderBy(x => x.Description);
            ViewBag.CampaignIds = new SelectList(CampaignIds, "CampaignId", "Description", CampaignIds.Select(x => x.CampaignId).FirstOrDefault());
            return View("LoadingList");
        }
        [HttpGet]
        public async Task<PartialViewResult> CreateLoadingList(string campaignInfo, DateTime reportDate, int reportType, CancellationToken cancellationToken)
        {
            var campaignData = campaignInfo.Split('/');
            var campaignIds = campaignData[0].Split(',').Select(x => x.Replace('_', ' ')).ToList();
            var warehouseId = (campaignData.Length > 1) ? campaignData[1] : string.Empty;
            if (reportDate == DateTime.MinValue)
                reportDate = DateTime.Today;
            return PartialView("~/Views/ИнтернетЗаказы/_LoadingList.cshtml", await GetLoadingListData(reportDate, reportType, campaignIds, null, warehouseId, cancellationToken));
        }
        [HttpPost]
        public async Task<IActionResult> LoadingListOrderScan(string campaignInfo, DateTime shipDate, string barcodeText, CancellationToken cancellationToken)
        {
            var result = await _orderFunctions.SetOrderScanned(shipDate, campaignInfo, barcodeText, 1, cancellationToken);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> LoadingListClearScanned(string campaignInfo, DateTime shipDate, CancellationToken cancellationToken)
        {
            var campaignData = campaignInfo.Split('/');
            var campaignIds = campaignData[0].Split(',').Select(x => x.Replace('_', ' ')).ToList();
            var warehouseId = (campaignData.Length > 1) ? campaignData[1] : string.Empty;
            await _orderFunctions.ClearOrderScanned(shipDate, campaignIds, warehouseId, cancellationToken);
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> GetLoadingAct(string campaignInfo, CancellationToken cancellationToken)
        {
            var currentTime = DateTime.Now.TimeOfDay;
            if (!TimeSpan.TryParse("16:00", out TimeSpan limitTime))
            {
                limitTime = TimeSpan.MaxValue;
            }
            var reportDate = currentTime > limitTime ? DateTime.Today.AddDays(1) : DateTime.Today;
            reportDate = reportDate.DayOfWeek switch
            {
                DayOfWeek.Saturday => reportDate.AddDays(2),
                DayOfWeek.Sunday => reportDate.AddDays(1),
                _ => reportDate
            };
            var campaignData = campaignInfo.Split('/');
            var campaignCode = campaignData[0];
            var warehouseId = (campaignData.Length > 1) ? campaignData[1] : string.Empty;
            var shop = _context.Sc14042s
                .Where(x => (x.Code.Trim() == campaignCode) && (string.IsNullOrEmpty(warehouseId) ? true : x.Sp14154 == warehouseId))
                .Select(market => market.Sp14155.ToUpper().Trim() + " " + (string.IsNullOrEmpty(warehouseId) ? market.Descr.Trim() : market.Sp14195.Trim())).FirstOrDefault();
            var dataE = await GetLoadingListData(reportDate, 0, null, campaignCode, warehouseId, cancellationToken);

            var printData = Enumerable.Repeat(new
            {
                ДатаЛистаСборки = reportDate.ToString("dd.MM.yyyy"),
                МагазинМаркетплейс = shop,
                Итого = dataE.Sum(x => x.КолТовара).ToString("0", CultureInfo.InvariantCulture),
                ИтогоГрузомест = dataE.Sum(x => x.КолГрузоМест).ToString("0", CultureInfo.InvariantCulture),
                ИтогоСумма = dataE.Sum(x => x.СуммаТовара).ToString("N2", CultureInfo.InvariantCulture),
                ТаблЧасть = dataE.Select((x, y) => new
                {
                    ном = (y + 1).ToString("0", CultureInfo.InvariantCulture),
                    OrderNo = x.OrderNo,
                    Status = x.Status,
                    КолГрузоМест = x.КолГрузоМест.ToString("0", CultureInfo.InvariantCulture),
                    КолТовара = x.КолТовара.ToString("0", CultureInfo.InvariantCulture),
                    StatusCode = x.StatusCode,
                    StatusDescription = x.StatusDescription,
                    Склады = x.Склады,
                    МаршрутНаименование = x.МаршрутНаименование
                }).ToList()
            }, 1).FirstOrDefault();
            return Ok("".CreateOrUpdateHtmlPrintPage("ЛистСборки", printData));
        }
        async Task<IEnumerable<LoadingListOrder>> GetLoadingListData(DateTime reportDate, int reportType, List<string> campaignIds, string campaignCode, string warehouseId, CancellationToken cancellationToken)
        {
            var data = await (from order in _context.Sc13994s
                              join market in _context.Sc14042s on order.Sp14038 equals market.Id
                              join item in _context.Sc14033s on order.Id equals item.Parentext
                              join nom in _context.Sc84s on item.Sp14022 equals nom.Id
                              join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                              join markUse in _context.Sc14152s on new { nomId = nom.Id, marketId = market.Id } equals new { nomId = markUse.Parentext, marketId = markUse.Sp14147 } into _markUse
                              from markUse in _markUse.DefaultIfEmpty()
                              where !order.Ismark && (order.Sp13982 != 5) &&
                                    (campaignIds != null ? campaignIds.Contains(market.Id) : (market.Code.Trim() == campaignCode)) &&
                                    (order.Sp13990.Date == reportDate) &&
                                    (!string.IsNullOrEmpty(warehouseId) ? markUse.Sp14190.Trim() == warehouseId :
                                        (!string.IsNullOrWhiteSpace(market.Sp14154) ? markUse.Sp14190.Trim() != market.Sp14154.Trim() :
                                            true))
                              group new { order, market, item, nom, ed } by new
                              {
                                  orderId = order.Id,
                                  orderNo = order.Code + 
                                    (market.Sp14155.ToUpper().Trim() == "ALIEXPRESS" ? " / " + order.Sp13987.Trim() :
                                     market.Sp14155.ToUpper().Trim() == "WILDBERRIES" ? " / " + order.Sp13986.ToString() + " / " + order.Sp13991.ToString() : ""),
                                  status = order.Sp13982,
                                  типДоставкиПартнер = order.Sp13985,
                                  типДоставки = order.Sp13988,
                                  scanned = order.Sp14254
                              } into gr
                              select new
                              {
                                  OrderId = gr.Key.orderId,
                                  OrderNo = gr.Key.orderNo.Trim(),
                                  Status = gr.Key.status,
                                  ТипДоставки = (((StinClasses.StinDeliveryPartnerType)gr.Key.типДоставкиПартнер == StinClasses.StinDeliveryPartnerType.SHOP) && ((StinClasses.StinDeliveryType)gr.Key.типДоставки == StinClasses.StinDeliveryType.PICKUP)) ? "Самовывоз" : "Доставка",
                                  Scanned = gr.Key.scanned,
                                  КолТовара = gr.Sum(x => x.item.Sp14023),
                                  СуммаТовара = gr.Sum(x => ((x.item.Sp14025 > 0 ? x.item.Sp14025 : x.item.Sp14024) + x.item.Sp14026) * x.item.Sp14023),
                                  КолГрузоМест = gr.Sum(x => ((x.market.Sp14155.ToUpper().Trim() == "ЯНДЕКС") || (x.market.Sp14155.ToUpper().Trim() == "SBER")) ? ((x.ed.Sp14063 == 0 ? 1 : x.ed.Sp14063) * x.item.Sp14023) / (x.nom.Sp14188 == 0 ? 1 : x.nom.Sp14188) : 1)
                              }).ToListAsync(cancellationToken);
            if (reportType == 1)
                data = data.Where(x => x.Scanned == x.КолГрузоМест).ToList();
            else if (reportType == 2)
                data = data.Where(x => x.Scanned < x.КолГрузоМест).ToList();
            var orderIds = data.Select(x => x.OrderId);
            DateTime dateRegTA = _context.GetRegTA();
            var dataReg = await (
                        (from r in _context.Rg4667s //ЗаказыЗаявки
                         join doc in _context.Dh2457s on r.Sp4664 equals doc.Iddoc
                         join sklad in _context.Sc55s on doc.Sp4437 equals sklad.Id
                         where r.Period == dateRegTA &&
                             orderIds.Contains(doc.Sp13995)
                         group new { r, doc } by new { orderId = doc.Sp13995, маршрутName = doc.Sp11557, складName = sklad.Descr } into gr
                         where gr.Sum(x => x.r.Sp4666) != 0
                         select new { orderId = gr.Key.orderId, маршрутName = gr.Key.маршрутName.Trim(), складName = gr.Key.складName.Trim(), statusOrder = 1 })
                        .Concat
                        (from r in _context.Rg4674s //Заявки
                         join doc in _context.Dh2457s on r.Sp4671 equals doc.Iddoc
                         join sklad in _context.Sc55s on doc.Sp4437 equals sklad.Id
                         where r.Period == dateRegTA &&
                             orderIds.Contains(doc.Sp13995)
                         group new { r, doc } by new { orderId = doc.Sp13995, маршрутName = doc.Sp11557, складName = sklad.Descr } into gr
                         where gr.Sum(x => x.r.Sp4672) != 0
                         select new { orderId = gr.Key.orderId, маршрутName = gr.Key.маршрутName.Trim(), складName = gr.Key.складName.Trim(), statusOrder = 2 })
                        .Concat
                        (from r in _context.Rg11973s //НаборНаСкладе
                         join doc in _context.Dh11948s on r.Sp11970 equals doc.Iddoc
                         join sklad in _context.Sc55s on r.Sp11967 equals sklad.Id
                         where r.Period == dateRegTA &&
                              orderIds.Contains(doc.Sp14003)
                         group new { r, doc } by new { orderId = doc.Sp14003, маршрутName = doc.Sp11935, status = doc.Sp11938, складName = sklad.Descr } into gr
                         where gr.Sum(x => x.r.Sp11972) != 0
                         select new { orderId = gr.Key.orderId, маршрутName = gr.Key.маршрутName.Trim(), складName = gr.Key.складName.Trim(), statusOrder = gr.Key.status == 1 ? 4 : 3 })
                        .Concat
                        (from docComplex in _context.Dh12542s
                         join sklad in _context.Sc55s on docComplex.Sp12518 equals sklad.Id
                         where orderIds.Contains(docComplex.Sp14005)
                         select new { orderId = docComplex.Sp14005, маршрутName = docComplex.Sp12524.Trim(), складName = sklad.Descr.Trim(), statusOrder = 6 })
                        .Concat
                        (from docProdaga in _context.Dh9109s
                         join sklad in _context.Sc55s on docProdaga.Sp9077 equals sklad.Id
                         where orderIds.Contains(docProdaga.Sp13999)
                         select new { orderId = docProdaga.Sp13999, маршрутName = docProdaga.Sp11575.Trim(), складName = sklad.Descr.Trim(), statusOrder = 6 })
                        ).ToListAsync(cancellationToken);
            return from d in data
                   join reg in dataReg on d.OrderId equals reg.orderId into _reg
                   from reg in _reg.DefaultIfEmpty()
                   group new { d, reg } by new
                   {
                       d.OrderNo,
                       d.Status,
                       d.ТипДоставки,
                       d.Scanned,
                       d.КолГрузоМест,
                       d.КолТовара,
                       d.СуммаТовара
                   } into gr
                   orderby gr.Key.OrderNo.Contains("-") ? gr.Key.OrderNo : gr.Key.OrderNo.Substring(gr.Key.OrderNo.Length - 3)
                   select new LoadingListOrder
                   {
                       OrderNo = gr.Key.OrderNo,
                       ТипДоставки = gr.Key.ТипДоставки,
                       Status = gr.Key.Status,
                       Scanned = (int)gr.Key.Scanned,
                       КолГрузоМест = gr.Key.КолГрузоМест,
                       КолТовара = gr.Key.КолТовара,
                       СуммаТовара = gr.Key.СуммаТовара,
                       StatusCode = gr.Min(o => o.reg.statusOrder),
                       Склады = string.Join(", ", gr.Select(y => y.reg.складName).Distinct()),
                       МаршрутНаименование = string.Join(", ", gr.Select(y => y.reg.маршрутName).Distinct()),
                   };
        }
        async Task<string> GetSberReestr(string campaignId, TimeSpan limitTime, CancellationToken cancellationToken)
        {
            DateTime departureDate = limitTime <= DateTime.Now.TimeOfDay ? DateTime.Today.AddDays(1) : DateTime.Today;
            if (departureDate.DayOfWeek == DayOfWeek.Saturday)
                departureDate = departureDate.AddDays(2);
            if (departureDate.DayOfWeek == DayOfWeek.Sunday)
                departureDate = departureDate.AddDays(1);

            var dataFirma = await (from market in _context.Sc14042s
                                   join firma in _context.Sc4014s on market.Parentext equals firma.Id
                                   join своиЮрЛица in _context.Sc131s on firma.Sp4011 equals своиЮрЛица.Id
                                   where market.Code.Trim() == campaignId
                                   select new
                                   {
                                       НазваниеФирмы = своиЮрЛица.Descr.Trim(),
                                       ИНН = своиЮрЛица.Sp135.Trim(),
                                       своиЮрЛица.Id,
                                       Договор = market.Sp14053.Trim()
                                   }).FirstOrDefaultAsync(cancellationToken);
            var РуководительId = await StinClasses.CommonDB.ПолучитьЗначениеПериодическогоРеквизита(_context, dataFirma.Id, 146);
            var Директор = await _context.Sc503s.Where(x => x.Id == РуководительId).Select(x => x.Descr.Trim()).FirstOrDefaultAsync(cancellationToken);
            var dataOrder = await (from order in _context.Sc13994s
                                   join market in _context.Sc14042s on order.Sp14038 equals market.Id
                                   join item in _context.Sc14033s on order.Id equals item.Parentext
                                   join nom in _context.Sc84s on item.Sp14022 equals nom.Id
                                   join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                                   where !order.Ismark && (order.Sp13982 != 5)
                                    && (market.Code.Trim() == campaignId)
                                    && (order.Sp13990 == departureDate)
                                   select new
                                   {
                                       DeliveryId = order.Sp13989.Trim(),
                                       ShipmentId = order.Code.Trim(),
                                       НомерСПрефиксом = market.Code.Trim() + "*" + order.Sp13981.Trim(),
                                       КолМест = ed.Sp14063 == 0 ? 1 : ed.Sp14063,
                                       Квант = nom.Sp14188 == 0 ? 1 : nom.Sp14188,
                                       Количество = item.Sp14023,
                                       Цена = item.Sp14024,
                                   }).ToListAsync(cancellationToken);
            var dataOrderNum = dataOrder
                .GroupBy(x => new { x.DeliveryId, x.ShipmentId, x.НомерСПрефиксом })
                .Select((gr, y) =>
                {
                    string boxPref = gr.Key.НомерСПрефиксом + "*";
                    int boxCount = 0;
                    var boxNos = new List<string>();
                    var boxCosts = new List<string>();
                    foreach (var item in gr)
                    {
                        int boxes = (int)(item.Количество / item.Квант * item.КолМест);
                        int b = 0;
                        while (b < boxes)
                        {
                            b++;
                            boxNos.Add(boxPref + (boxCount + b).ToString());
                            boxCosts.Add(((item.Цена * item.Количество) / boxes).ToString(Common.ФорматЦеныСи, CultureInfo.InvariantCulture));
                        }
                        boxCount += boxes;
                    }
                    return new
                    {
                        Ном = (y + 1).ToString("0", CultureInfo.InvariantCulture),
                        gr.Key.DeliveryId,
                        gr.Key.ShipmentId,
                        gr.Key.НомерСПрефиксом,
                        КолМест = gr.Sum(x => x.Количество / x.Квант * x.КолМест).ToString("0", CultureInfo.InvariantCulture),
                        НомерМеста = string.Join("<br>", boxNos),
                        СтоимостьМеста = string.Join("<br>", boxCosts),
                        СтоимостьОтправления = gr.Sum(x => x.Количество * x.Цена).ToString(Common.ФорматЦеныСи, CultureInfo.InvariantCulture),
                        КолМестNum = gr.Sum(x => x.Количество / x.Квант * x.КолМест),
                        СтоимостьОтправленияNum = gr.Sum(x => x.Количество * x.Цена)
                    };
                }).ToList();
            var printData = Enumerable.Repeat(new
            {
                НомерДок = DateTime.Today.DayOfYear.ToString() + DateTime.Today.ToString("yyyy"),
                ДатаДок = DateTime.Today.ToString("dd.MM.yyyy"),
                НазваниеФирмы = dataFirma.НазваниеФирмы,
                Директор,
                ДокУстава = dataFirma.ИНН.Length == 12 ? "Свидетельства" : "Устава",
                Договор = dataFirma.Договор,
                ТаблЧасть = dataOrderNum,
                ИтогоМест = dataOrderNum.Sum(x => x.КолМестNum).ToString("0", CultureInfo.InvariantCulture),
                ИтогоСтоимость = dataOrderNum.Sum(x => x.СтоимостьОтправленияNum).ToString(Common.ФорматЦеныСи, CultureInfo.InvariantCulture)
            }, 1).FirstOrDefault();

            return "".CreateOrUpdateHtmlPrintPage("РеестрСберМегаМаркет", printData);
        }
        [HttpGet]
        public async Task<IActionResult> GetReceptionTransferAct(string campaignInfo, CancellationToken cancellationToken)
        {
            var campaignData = campaignInfo.Split('/');
            var campaignId = campaignData[0];
            var warehouseId = (campaignData.Length > 1) ? campaignData[1] : string.Empty;
            var data = await _context.Sc14042s.Where(x => x.Code.Trim() == campaignId).Select(x => new
            {
                тип = x.Sp14155.ToUpper().Trim(),
                clientId = x.Sp14053.Trim(),
                token = x.Sp14054.Trim(),
                authApi = x.Sp14077.Trim()
            }).FirstOrDefaultAsync(cancellationToken);
            if (data != null)
            {
                var currentTime = DateTime.Now.TimeOfDay;
                if (!TimeSpan.TryParse("16:00", out TimeSpan limitTime))
                {
                    limitTime = TimeSpan.MaxValue;
                }
                if (data.тип == "ЯНДЕКС")
                {
                    var request = new YandexClasses.FirstMileShipmentsRequest();
                    if (currentTime > limitTime)
                    {
                        request.DateFrom = DateTime.Today.AddDays(1);
                        request.DateTo = DateTime.Today.AddDays(1);
                    }
                    else
                    {
                        request.DateFrom = DateTime.Today;
                        request.DateTo = DateTime.Today;
                    }
                    string err = "";
                    var result = await YandexClasses.YandexOperators.Exchange<YandexClasses.FirstMileShipmentsResponse>(_httpService,
                        string.Format(Startup.sConfiguration["Settings:UrlFirstMileShipments"], campaignId),
                        HttpMethod.Put,
                        data.clientId,
                        data.token,
                        request,
                        cancellationToken);
                    //string context = request.SerializeObject();
                    //var result = await YandexClasses.YandexOperators.YandexExchange(null, string.Format(Startup.sConfiguration["Settings:UrlFirstMileShipments"], campaignId), HttpMethod.Put, data.clientId, data.token, context);
                    if ((result.Item1 == YandexClasses.ResponseStatus.ERROR) && !string.IsNullOrEmpty(result.Item3))
                        err += result.Item3;
                    if ((result.Item1 == YandexClasses.ResponseStatus.OK) && (result.Item2 != null))
                    {
                        var firstMileShipmentsResponse = result.Item2;
                        if ((firstMileShipmentsResponse.Status == YandexClasses.ResponseStatus.OK) &&
                            (firstMileShipmentsResponse.Result != null) &&
                            (firstMileShipmentsResponse.Result.Shipments != null) &&
                            (firstMileShipmentsResponse.Result.Shipments.Count > 0))
                        {
                            string shipmentId = firstMileShipmentsResponse.Result.Shipments[0].Id.ToString();
                            string externalId = firstMileShipmentsResponse.Result.Shipments[0].ExternalId;
                            var resultInfo = await YandexClasses.YandexOperators.Exchange<YandexClasses.FirstMileShipmentInfoResponse>(_httpService,
                                string.Format(Startup.sConfiguration["Settings:UrlFirstMileShipmentInfo"], campaignId, shipmentId),
                                HttpMethod.Get,
                                data.clientId,
                                data.token,
                                null,
                                cancellationToken);
                            //await YandexClasses.YandexOperators.YandexExchange(null, string.Format(Startup.sConfiguration["Settings:UrlFirstMileShipmentInfo"], campaignId, shipmentId), HttpMethod.Get, data.clientId, data.token, null);
                            if ((resultInfo.Item1 == YandexClasses.ResponseStatus.ERROR) && !string.IsNullOrEmpty(resultInfo.Item3))
                                err += resultInfo.Item3;
                            if ((resultInfo.Item1 == YandexClasses.ResponseStatus.OK) && (result.Item2 != null))
                            {
                                var info = resultInfo.Item2;
                                if ((info.Status == YandexClasses.ResponseStatus.OK) &&
                                    (info.Result != null) &&
                                    (info.Result.AvailableActions != null) &&
                                    (info.Result.AvailableActions.Count > 0))
                                {
                                    bool canDownload = !info.Result.AvailableActions.Contains(YandexClasses.LogisticsActions.CONFIRM);
                                    if (!canDownload)
                                    {
                                        var requestConfirm = new YandexClasses.FirstMileConfirmRequest();
                                        requestConfirm.ExternalShipmentId = externalId;
                                        requestConfirm.OrderIds = await (from x in _context.Sc13994s
                                                                         join m in _context.Sc14042s on x.Sp14038 equals m.Id
                                                                         where (m.Sp14077.Trim() == data.authApi) &&
                                                                            (x.Sp13982 != 5) &&
                                                                            info.Result.OrderIds.Select(y => y.ToString()).Contains(x.Code.Trim())
                                                                         select Convert.ToInt64(x.Code.Trim())).ToListAsync();
                                        var resultConfirm = await YandexClasses.YandexOperators.Exchange<YandexClasses.ErrorResponse>(_httpService,
                                            string.Format(Startup.sConfiguration["Settings:UrlFirstMileShipmentConfirm"], campaignId, shipmentId),
                                            HttpMethod.Post,
                                            data.clientId,
                                            data.token,
                                            requestConfirm,
                                            cancellationToken);
                                        if ((resultConfirm.Item1 == YandexClasses.ResponseStatus.ERROR) && !string.IsNullOrEmpty(resultConfirm.Item3))
                                            err += resultConfirm.Item3;
                                        canDownload = (resultConfirm.Item1 == YandexClasses.ResponseStatus.OK) &&
                                            (resultConfirm.Item2 != null) &&
                                            (resultConfirm.Item2.Status == YandexClasses.ResponseStatus.OK);
                                        //context = requestConfirm.SerializeObject();
                                        //result = await YandexClasses.YandexOperators.YandexExchange(null, string.Format(Startup.sConfiguration["Settings:UrlFirstMileShipmentConfirm"], campaignId, shipmentId), HttpMethod.Post, data.clientId, data.token, context);
                                    }
                                    if (canDownload)
                                    {
                                        var resultDownload = await YandexClasses.YandexOperators.Exchange<byte[]>(_httpService,
                                            string.Format(Startup.sConfiguration["Settings:UrlReceptionTransferAct"], campaignId, shipmentId),
                                            HttpMethod.Get,
                                            data.clientId,
                                            data.token,
                                            null,
                                            cancellationToken);
                                        //result = await YandexClasses.YandexOperators.YandexExchange(null, string.Format(Startup.sConfiguration["Settings:UrlReceptionTransferAct"], campaignId, shipmentId), HttpMethod.Get, data.clientId, data.token, null);
                                        if ((resultDownload.Item1 == YandexClasses.ResponseStatus.ERROR) && !string.IsNullOrEmpty(resultDownload.Item3))
                                            err += resultDownload.Item3;
                                        if ((resultDownload.Item1 == YandexClasses.ResponseStatus.OK) && (resultDownload.Item2 != null))
                                            return File(resultDownload.Item2, "application/pdf");
                                    }
                                }
                            }
                        }
                    }
                    return BadRequest(new ExceptionData { Code = -4, Description = err });
                }
                else if (data.тип == "OZON")
                {
                    long.TryParse(campaignId, out long deliveryMethodId);
                    if (!string.IsNullOrEmpty(warehouseId))
                    {
                        long.TryParse(warehouseId, out long altDeliveryMethodId);
                        DateTime departureDate = limitTime <= DateTime.Now.TimeOfDay ? DateTime.Today.AddDays(1) : DateTime.Today;
                        if (departureDate.DayOfWeek == DayOfWeek.Saturday)
                            departureDate = departureDate.AddDays(2);
                        if (departureDate.DayOfWeek == DayOfWeek.Sunday)
                            departureDate = departureDate.AddDays(1);

                        var deliveryServiceId = await (from o in _context.Sc13994s
                                                       join m in _context.Sc14042s on o.Sp14038 equals m.Id
                                                       where !o.Ismark && (m.Code.Trim() == campaignId) &&
                                                             !o.Sp13986.Equals(deliveryMethodId) && !o.Sp13986.Equals(altDeliveryMethodId) &&
                                                             (o.Sp13990.Date == departureDate)
                                                       select o.Sp13986)
                                                       .FirstOrDefaultAsync(cancellationToken);
                        if (deliveryServiceId > 0)
                            deliveryMethodId = (long)deliveryServiceId;
                        else
                            deliveryMethodId = altDeliveryMethodId;
                    }
                    var ozonResult = await OzonClasses.OzonOperators.GetAct(_httpService, data.clientId, data.token,
                        limitTime,
                        deliveryMethodId,
                        cancellationToken);
                    if (ozonResult.data != null)
                        return File(ozonResult.data, "application/pdf");
                    if (ozonResult.Item2 != null)
                        return BadRequest(new ExceptionData { Code = -100, Description = ozonResult.error });
                }
                else if (data.тип == "ALIEXPRESS")
                {
                    DateTime departureDate = limitTime <= DateTime.Now.TimeOfDay ? DateTime.Today.AddDays(1) : DateTime.Today;
                    if (departureDate.DayOfWeek == DayOfWeek.Saturday)
                        departureDate = departureDate.AddDays(2);
                    if (departureDate.DayOfWeek == DayOfWeek.Sunday)
                        departureDate = departureDate.AddDays(1);
                    var deliveryServiceIds = await (from o in _context.Sc13994s
                                                    join m in _context.Sc14042s on o.Sp14038 equals m.Id
                                                    where !o.Ismark && (m.Code.Trim() == campaignId) &&
                                                          (o.Sp13990.Date == departureDate)
                                                    select (long)o.Sp13986)
                                                   .ToListAsync(cancellationToken);
                    var handoverIds = new List<long>();
                    var usedLogisticsOrderIds = new List<long>();
                    int page = 0;
                    int pageSize = 100;
                    int totalPages = 1;
                    while (page < totalPages)
                    {
                        page++;
                        var aliHandovers = await AliExpressClasses.Functions.GetHandoverList(_httpService, data.token,
                            page, pageSize, deliveryServiceIds, cancellationToken);
                        if (!string.IsNullOrEmpty(aliHandovers.error))
                            return BadRequest(new ExceptionData { Code = -100, Description = aliHandovers.error });
                        totalPages = aliHandovers.response?.PageInfo?.Total ?? 1;
                        foreach (var item in aliHandovers.response?.Data?.Data_source)
                        {
                            if ((item.Arrival_date != departureDate) && (item.Status == AliExpressClasses.HandoverStatus.Created))
                            {
                                var needDeleteLogisticsOrderIds = item.Logistic_order_ids.Where(x => deliveryServiceIds.Contains(x)).ToList();
                                var deleteResult = await AliExpressClasses.Functions.DeleteFromHandoverList(_httpService, data.token, item.Handover_list_id, needDeleteLogisticsOrderIds, cancellationToken);
                                if (!string.IsNullOrEmpty(deleteResult.error))
                                    return BadRequest(new ExceptionData { Code = -100, Description = deleteResult.error });
                            }
                            else
                            {
                                handoverIds.Add(item.Handover_list_id);
                                usedLogisticsOrderIds.AddRange(item.Logistic_order_ids);
                            }
                        }
                    }
                    if (handoverIds.Count == 0)
                    {
                        var createHandoverList = await AliExpressClasses.Functions.CreateHandoverList(_httpService, data.token, departureDate, deliveryServiceIds, cancellationToken);
                        if (!string.IsNullOrEmpty(createHandoverList.error))
                            return BadRequest(new ExceptionData { Code = -100, Description = createHandoverList.error });
                        handoverIds.Add(createHandoverList.handoverListId ?? 0);
                    }
                    else if ((handoverIds.Count > 0) && (deliveryServiceIds.Where(x => !usedLogisticsOrderIds.Contains(x)).Count() > 0))
                    {
                        var addToHandover = await AliExpressClasses.Functions.AddToHandoverList(_httpService, data.token,
                            handoverIds.FirstOrDefault(),
                            deliveryServiceIds.Where(x => !usedLogisticsOrderIds.Contains(x)).ToList(),
                            cancellationToken);
                        if (!string.IsNullOrEmpty(addToHandover.error))
                            return BadRequest(new ExceptionData { Code = -100, Description = addToHandover.error });
                    }
                    if (handoverIds.Count == 1)
                    {
                        var printResult = await AliExpressClasses.Functions.PrintHandoverList(_httpService, data.token, handoverIds.FirstOrDefault(), cancellationToken);
                        if (!string.IsNullOrEmpty(printResult.error))
                            return BadRequest(new ExceptionData { Code = -100, Description = printResult.error });
                        return File(printResult.Item1, "application/pdf");
                    }
                    else
                    {
                        var pdfLibrary = new List<byte[]>();
                        foreach (var handoverId in handoverIds)
                        {
                            var printResult = await AliExpressClasses.Functions.PrintHandoverList(_httpService, data.token, handoverId, cancellationToken);
                            if (!string.IsNullOrEmpty(printResult.error))
                                return BadRequest(new ExceptionData { Code = -100, Description = printResult.error });
                            if (printResult.pdf != null)
                                pdfLibrary.Add(printResult.pdf);
                        }
                        return File(PdfHelper.PdfFunctions.Instance.MergePdf(pdfLibrary), "application/pdf");
                    }
                }
                else if (data.тип == "WILDBERRIES")
                {
                    DateTime departureDate = limitTime <= DateTime.Now.TimeOfDay ? DateTime.Today.AddDays(1) : DateTime.Today;
                    if (departureDate.DayOfWeek == DayOfWeek.Saturday)
                        departureDate = departureDate.AddDays(2);
                    if (departureDate.DayOfWeek == DayOfWeek.Sunday)
                        departureDate = departureDate.AddDays(1);
                    var deliveryServiceNames = await (from o in _context.Sc13994s
                                                      join m in _context.Sc14042s on o.Sp14038 equals m.Id
                                                      where !o.Ismark && (m.Code.Trim() == campaignId) &&
                                                            (o.Sp13990.Date == departureDate)
                                                      select o.Sp13987.Trim())
                                                    .Distinct()
                                                    .ToListAsync(cancellationToken);
                    if (deliveryServiceNames.Count == 0)
                        return BadRequest(new ExceptionData { Code = -100, Description = "No supplies" });
                    if (deliveryServiceNames.Count == 1)
                    {
                        string supplyId = deliveryServiceNames.FirstOrDefault();
                        var supplyInfo = await WbClasses.Functions.GetSupplyInfo(_httpService, data.token, supplyId, cancellationToken);
                        if (!string.IsNullOrEmpty(supplyInfo.error))
                            return BadRequest(new ExceptionData { Code = -100, Description = supplyInfo.error });
                        if ((supplyInfo.supply?.ClosedAt == null) || (supplyInfo.supply?.ClosedAt == DateTime.MinValue))
                            await WbClasses.Functions.CloseSupply(_httpService, data.token, supplyId, cancellationToken);
                        var pngBarcodeResult = await WbClasses.Functions.GetSupplyBarcode(_httpService, data.token, supplyId, cancellationToken);
                        if (!string.IsNullOrEmpty(pngBarcodeResult.error))
                            return BadRequest(new ExceptionData { Code = -100, Description = pngBarcodeResult.error });
                        return File(pngBarcodeResult.png, "image/png");
                    }
                    else
                    {
                        var pngLibrary = new List<byte[]>();
                        foreach (var dsn in deliveryServiceNames)
                        {
                            var supplyInfo = await WbClasses.Functions.GetSupplyInfo(_httpService, data.token, dsn, cancellationToken);
                            if (!string.IsNullOrEmpty(supplyInfo.error))
                                return BadRequest(new ExceptionData { Code = -100, Description = supplyInfo.error });
                            if ((supplyInfo.supply?.ClosedAt == null) || (supplyInfo.supply?.ClosedAt == DateTime.MinValue))
                                await WbClasses.Functions.CloseSupply(_httpService, data.token, dsn, cancellationToken);
                            var pngBarcodeResult = await WbClasses.Functions.GetSupplyBarcode(_httpService, data.token, dsn, cancellationToken);
                            if (!string.IsNullOrEmpty(pngBarcodeResult.error))
                                return BadRequest(new ExceptionData { Code = -100, Description = pngBarcodeResult.error });
                            if (pngBarcodeResult.png != null)
                                pngLibrary.Add(pngBarcodeResult.png);
                        }
                        return File(PdfHelper.PdfFunctions.Instance.GetPdfFromImage(pngLibrary), "application/pdf");
                    }
                }
                else if (data.тип == "SBER")
                {
                    string html = await GetSberReestr(campaignId, limitTime, cancellationToken);
                    return File(System.Text.Encoding.UTF8.GetBytes(html), "text/html;charset=utf-8");
                }
                return BadRequest(new ExceptionData { Code = -100, Description = "Unhandling exception" });
            }
            else
                return BadRequest(new ExceptionData { Code = -100, Description = "No data for " + campaignId });
        }
        [HttpGet]
        public IActionResult OrdersConsole(bool isDriver, string driverId, bool transferred)
        {
            if (transferred)
                return OrdersConsoleLastMile();
            return OrdersConsoleFirstMile(isDriver, driverId);
        }
        private IActionResult OrdersConsoleFirstMile(bool isDriver, string driverId)
        {
            if (!string.IsNullOrWhiteSpace(driverId))
            {
                driverId = driverId.Replace("_", " ");
                driverId = StinClasses.Common.FormatTo1CId(driverId);
            }
            DateTime limitDate = DateTime.Today.AddDays(-3);
            DateTime limitSelfDate = DateTime.Today.AddDays(-6);
            DateTime dateRegTA = _context.GetRegTA();
            var dataRegistry = from r in _context.Rg4667s //ЗаказыЗаявки
                               join doc in _context.Dh2457s on r.Sp4664 equals doc.Iddoc
                               join m in _context.Sc11555s on doc.Sp11556 equals m.Code into _m
                               from m in _m.DefaultIfEmpty()
                               where (r.Period == dateRegTA) && (r.Sp4666 != 0) &&
                                   (doc.Sp13995 != Common.ПустоеЗначение) &&
                                   (isDriver ? ((m != null) && (m.Sp11669 == driverId)) : true)
                               select new
                               {
                                   orderId = doc.Sp13995,
                                   маршрутName = Convert.ToString(doc.Sp11557.Trim()),
                                   складId = Convert.ToString(doc.Sp4437),
                                   statusOrder = 1,
                                   SumZZ = (int)r.Sp4666,
                                   SumZ = 0,
                                   SumN = 0,
                                   SumD = 0
                               };

            dataRegistry = dataRegistry.Concat(from r in _context.Rg4674s //Заявки
                                               join doc in _context.Dh2457s on r.Sp4671 equals doc.Iddoc
                                               join m in _context.Sc11555s on doc.Sp11556 equals m.Code into _m
                                               from m in _m.DefaultIfEmpty()
                                               where (r.Period == dateRegTA) && (r.Sp4672 != 0) &&
                                                   (doc.Sp13995 != Common.ПустоеЗначение) &&
                                                   (isDriver ? ((m != null) && (m.Sp11669 == driverId)) : true)
                                               select new
                                               {
                                                   orderId = doc.Sp13995,
                                                   маршрутName = Convert.ToString(doc.Sp11557.Trim()),
                                                   складId = Convert.ToString(doc.Sp4437),
                                                   statusOrder = 2,
                                                   SumZZ = 0, 
                                                   SumZ = (int)(r.Sp4672 * 100000),
                                                   SumN = 0,
                                                   SumD = 0
                                               });
            dataRegistry = dataRegistry.Concat(from r in _context.Rg11973s //НаборНаСкладе
                                               join doc in _context.Dh11948s on r.Sp11970 equals doc.Iddoc
                                               join m in _context.Sc11555s on doc.Sp11934 equals m.Code into _m
                                               from m in _m.DefaultIfEmpty()
                                               where (r.Period == dateRegTA) && (r.Sp11972 != 0) &&
                                                    (doc.Sp14003 != Common.ПустоеЗначение) &&
                                                    (isDriver ? ((m != null) && (m.Sp11669 == driverId)) : true)
                                               select new
                                               {
                                                   orderId = doc.Sp14003,
                                                   маршрутName = Convert.ToString(doc.Sp11935.Trim()),
                                                   складId = Convert.ToString(r.Sp11967),
                                                   statusOrder = 3 + (int)doc.Sp11938,
                                                   SumZZ = 0,
                                                   SumZ = 0,
                                                   SumN = (int)(r.Sp11972 * 100000),
                                                   SumD = 0
                                               });
            if (!isDriver)
                dataRegistry = dataRegistry.Concat(from o in _context.Sc13994s
                                                   where !o.Ismark && (o.Sp13982 == 5) &&
                                                      (((o.Sp13988 == (decimal)StinClasses.StinDeliveryType.PICKUP) &&
                                                      (o.Sp13990 >= limitSelfDate) &&
                                                      ((StinClasses.StinDeliveryPartnerType)o.Sp13985 == StinClasses.StinDeliveryPartnerType.SHOP)) ||
                                                      (o.Sp13990 >= limitDate))
                                                   select new
                                                   {
                                                       orderId = o.Id,
                                                       маршрутName = string.Empty,
                                                       складId = string.Empty,
                                                       statusOrder = 5,
                                                       SumZZ = 0,
                                                       SumZ = 0,
                                                       SumN = 0,
                                                       SumD = 1
                                                   });
            var dataDB = from r in dataRegistry
                           join sklad in _context.Sc55s on r.складId equals sklad.Id into _sklad
                           from sklad in _sklad.DefaultIfEmpty()
                           join order in _context.Sc13994s on r.orderId equals order.Id
                           join market in _context.Sc14042s on order.Sp14038 equals market.Id
                           join превЗаявка in _context.Dh12747s on order.Id equals превЗаявка.Sp14007
                           join j in _context._1sjourns on превЗаявка.Iddoc equals j.Iddoc
                           join binary in _context.VzOrderBinaries.Where(x => x.Extension.Trim().ToUpper() == "LABELS") on order.Id equals binary.Id into _binary
                           from binary in _binary.DefaultIfEmpty()
                           select new
                           {
                               Id = order.Id,
                               Тип = market.Sp14155.ToUpper().Trim(),
                               MarketplaceId = order.Code.Trim(),
                               MarketplaceType = order.Descr.Trim(), 
                               ShipmentDate = order.Sp13990,
                               ПредварительнаяЗаявкаНомер = order.Sp13981.Trim(),
                               CustomerNotes = order.Sp14122,
                               Town = order.Sp14125.Trim(),
                               Street = order.Sp14127.Trim(),
                               House = order.Sp14128.Trim(),
                               Block = order.Sp14129.Trim(),
                               Entrance = order.Sp14130.Trim(),
                               Intercom = order.Sp14131.Trim(),
                               Floor = order.Sp14132.Trim(),
                               Flat = order.Sp14133.Trim(),
                               СкладId = (sklad != null ? sklad.Id : ""),
                               Склад = (sklad != null ? sklad.Descr.Trim() : ""),
                               Статус = (int)order.Sp13982,
                               состояние = r.statusOrder,
                               Recipient = order.Sp14119.Trim(),
                               Family = order.Sp14116.Trim(),
                               Name = order.Sp14117.Trim(),
                               SerName = order.Sp14118.Trim(),
                               Phone = order.Sp14120.Trim(),
                               МаршрутНаименование = r.маршрутName,
                               isFBS = (StinClasses.StinDeliveryPartnerType)order.Sp13985 != StinClasses.StinDeliveryPartnerType.SHOP,
                               isExpress = market.Sp14164.ToUpper().Trim() == "EXPRESS",
                               ТипДоставки = (((StinClasses.StinDeliveryPartnerType)order.Sp13985 == StinClasses.StinDeliveryPartnerType.SHOP) && ((StinClasses.StinDeliveryType)order.Sp13988 == StinClasses.StinDeliveryType.PICKUP)) ? "Самовывоз" : "Доставка",
                               Сумма = превЗаявка.Sp12741,
                               DateTimeDoc = j.DateTimeIddoc,
                               СуммаКОплате = (((StinClasses.StinDeliveryPartnerType)order.Sp13985 == StinClasses.StinDeliveryPartnerType.SHOP) && ((StinClasses.StinPaymentType)order.Sp13983 == StinClasses.StinPaymentType.POSTPAID)) ? (превЗаявка.Sp12741 - order.Sp14135) : 0,
                               NeedToGetPayment = ((StinClasses.StinDeliveryPartnerType)order.Sp13985 == StinClasses.StinDeliveryPartnerType.SHOP) && ((StinClasses.StinPaymentType)order.Sp13983 == StinClasses.StinPaymentType.POSTPAID),
                               ИнформацияAPI = order.Sp14055,
                               Printed = order.Sp14192 == 1,
                               Labels = binary != null ? binary.Id : null,
                               r.SumZZ,
                               r.SumZ,
                               r.SumN,
                               r.SumD
                           };

            var dataResult = dataDB.AsEnumerable()
                .GroupBy(x => new
                {
                    x.Id,
                    x.Тип,
                    x.MarketplaceId,
                    x.MarketplaceType,
                    x.ShipmentDate,
                    x.ПредварительнаяЗаявкаНомер,
                    x.CustomerNotes,
                    x.Town,
                    x.Street,
                    x.House,
                    x.Block,
                    x.Entrance,
                    x.Intercom,
                    x.Floor,
                    x.Flat,
                    x.Статус,
                    x.Recipient,
                    x.Family,
                    x.Name,
                    x.SerName,
                    x.Phone,
                    x.isFBS,
                    x.isExpress,
                    x.ТипДоставки,
                    x.Сумма,
                    x.DateTimeDoc,
                    x.СуммаКОплате,
                    x.NeedToGetPayment,
                    x.ИнформацияAPI,
                    x.Printed,
                    x.Labels
                })
                .Where(x => x.Sum(y => y.SumZZ + y.SumZ + y.SumN + y.SumD) != 0)
                .OrderBy(x => x.Key.DateTimeDoc)
                .Select(gr => new MarketplaceOrder
                {
                    Id = gr.Key.Id,
                    Тип = gr.Key.Тип,
                    MarketplaceId = gr.Key.MarketplaceId,
                    MarketplaceType = gr.Key.MarketplaceType,
                    ShipmentDate = gr.Key.ShipmentDate,
                    ПредварительнаяЗаявкаНомер = gr.Key.ПредварительнаяЗаявкаНомер,
                    CustomerNotes = gr.Key.CustomerNotes,
                    Town = gr.Key.Town,
                    Street = gr.Key.Street,
                    House = gr.Key.House,
                    Block = gr.Key.Block,
                    Entrance = gr.Key.Entrance,
                    Intercom = gr.Key.Intercom,
                    Floor = gr.Key.Floor,
                    Flat = gr.Key.Flat,
                    Status = gr.Key.Статус,
                    StatusCode = gr.Min(o => o.состояние),
                    RecipientName = gr.Key.Recipient,
                    Family = gr.Key.Family,
                    Name = gr.Key.Name,
                    SerName = gr.Key.SerName,
                    Phone = gr.Key.Phone,
                    isFBS = gr.Key.isFBS,
                    isExpress = gr.Key.isExpress,
                    ТипДоставки = gr.Key.ТипДоставки,
                    Сумма = gr.Key.Сумма,
                    СуммаКОплате = gr.Key.СуммаКОплате,
                    NeedToGetPayment = gr.Key.NeedToGetPayment,
                    ИнформацияAPI = gr.Key.ИнформацияAPI,
                    Labels = gr.Key.Labels,
                    СкладIds = string.Join(", ", gr.Select(y => y.СкладId).Distinct()),
                    Склады = string.Join(", ", gr.Select(y => y.Склад).Distinct()),
                    МаршрутНаименование = string.Join(", ", gr.Select(y => y.МаршрутНаименование).Distinct()),
                    Printed = gr.Key.Printed,
                });

            return PartialView("~/Views/ИнтернетЗаказы/Orders.cshtml", dataResult);
        }
        private IActionResult OrdersConsoleLastMile()
        {
            var data = from order in _context.Sc13994s 
                       join market in _context.Sc14042s on order.Sp14038 equals market.Id
                       join превЗаявка in _context.Dh12747s on order.Id equals превЗаявка.Sp14007
                       where !order.Ismark && 
                            ((order.Sp13982 == 14) || (order.Sp13982 == 15) || (order.Sp13982 == 16))
                       select new MarketplaceOrder
                       {
                           Id = order.Id,
                           Тип = market.Sp14155.ToUpper().Trim(),
                           MarketplaceId = order.Code.Trim(),
                           MarketplaceType = order.Descr.Trim(),
                           ShipmentDate = order.Sp13990,
                           ПредварительнаяЗаявкаНомер = order.Sp13981.Trim(),
                           CustomerNotes = order.Sp14122,
                           Town = order.Sp14125.Trim(),
                           Street = order.Sp14127.Trim(),
                           House = order.Sp14128.Trim(),
                           Block = order.Sp14129.Trim(),
                           Entrance = order.Sp14130.Trim(),
                           Intercom = order.Sp14131.Trim(),
                           Floor = order.Sp14132.Trim(),
                           Flat = order.Sp14133.Trim(),
                           Status = (int)order.Sp13982,
                           StatusCode = 7,
                           RecipientName = order.Sp14119.Trim(),
                           Family = order.Sp14116.Trim(),
                           Name = order.Sp14117.Trim(),
                           SerName = order.Sp14118.Trim(),
                           Phone = order.Sp14120.Trim(),
                           isFBS = (StinClasses.StinDeliveryPartnerType)order.Sp13985 != StinClasses.StinDeliveryPartnerType.SHOP,
                           isExpress = market.Sp14164.ToUpper().Trim() == "EXPRESS",
                           ТипДоставки = (((StinClasses.StinDeliveryPartnerType)order.Sp13985 == StinClasses.StinDeliveryPartnerType.SHOP) && ((StinClasses.StinDeliveryType)order.Sp13988 == StinClasses.StinDeliveryType.PICKUP)) ? "Самовывоз" : "Доставка",
                           Сумма = превЗаявка.Sp12741,
                           СуммаКОплате = (((StinClasses.StinDeliveryPartnerType)order.Sp13985 == StinClasses.StinDeliveryPartnerType.SHOP) && ((StinClasses.StinPaymentType)order.Sp13983 == StinClasses.StinPaymentType.POSTPAID)) ? (превЗаявка.Sp12741 - order.Sp14135) : 0,
                           NeedToGetPayment = ((StinClasses.StinDeliveryPartnerType)order.Sp13985 == StinClasses.StinDeliveryPartnerType.SHOP) && ((StinClasses.StinPaymentType)order.Sp13983 == StinClasses.StinPaymentType.POSTPAID),
                           ИнформацияAPI = order.Sp14055,
                           Labels = "",
                           СкладIds = "",
                           Склады = "",
                           МаршрутНаименование = "",
                           Printed = false,
                       };
            var dataE = data.AsEnumerable()
                .Select(x => x);
            return PartialView("~/Views/ИнтернетЗаказы/Orders.cshtml", dataE);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteLabel(string orderId, CancellationToken cancellationToken)
        {
            var entity = await _context.VzOrderBinaries.FirstOrDefaultAsync(x => (x.Id == orderId) && (x.Extension == "LABELS"), cancellationToken);
            if (entity != null)
            {
                _context.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);
            }
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> GetLabelsPdf(string id, string[] nomBarcodes, CancellationToken cancellationToken)
        {
            if (nomBarcodes?.Length == 0)
                return StatusCode(502, "НЕ СКАНИРОВАН ТОВАР");
            if (!string.IsNullOrEmpty(id) && ((id.Length == 13) || (id.Length == 14)) && (id.Substring(0, 4) == "%97W"))
            {
                var docId = id.Substring(4).Replace('%', ' ');
                if (id.Length == 14)
                    docId = docId.Remove(docId.Length - 1);
                var formNabor = await _набор.GetФормаНаборById(docId);
                if (formNabor == null)
                    return StatusCode(502, "Не удалось получить форму набора");
                if (!formNabor.Общие.Проведен)
                    return StatusCode(502, "Набор не проведен");
                if (formNabor.Order?.InternalStatus == 5)
                    return StatusCode(502, "Заказ уже отменен");
                if (!_набор.IsActive(docId))
                    return StatusCode(502, "Набор отменен");
                var formBarcodes = formNabor?.ТабличнаяЧасть.Select(x => x.Единица?.Barcode).ToArray();
                if (nomBarcodes.Any(x => x == "000000000") || (Enumerable.SequenceEqual(nomBarcodes.OrderBy(x => x), formBarcodes.OrderBy(x => x))))
                {
                    if (!formNabor.Завершен)
                    {
                        formNabor.Завершен = true;
                        formNabor.EndComplectation = DateTime.Now;
                        var реквизитыПроведенныхДокументов = new List<StinClasses.Документы.ОбщиеРеквизиты>();
                        using var tran = await _context.Database.BeginTransactionAsync(cancellationToken);
                        try
                        {
                            var result = await _набор.ЗаписатьПровестиAsync(formNabor);
                            if (result != null)
                            {
                                if (_context.Database.CurrentTransaction != null)
                                    tran.Rollback();
                                return StatusCode(502, result.Description);
                            }
                            else
                            {
                                реквизитыПроведенныхДокументов.Add(formNabor.Общие);
                                await _набор.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                            }
                            if (_context.Database.CurrentTransaction != null)
                                tran.Commit();
                        }
                        catch (Exception ex)
                        {
                            if (_context.Database.CurrentTransaction != null)
                                _context.Database.CurrentTransaction.Rollback();
                            return StatusCode(502, ex.Message);
                        }
                    }
                    return Ok();
                }
                else
                    return StatusCode(502, "Товары набора не соответствуют отсканированным");
            }
            return StatusCode(502, "Не удалось получить документ набора по штрих-коду");
        }
        [HttpGet]
        public async Task<IActionResult> GetLabelsPdf(string id, bool isNaborDocId = false)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (id.Length == 14)
                    id = id.Substring(0, 13);
                if (id.Length > 9)
                    id = id.Substring(id.Length - 9);
                id = id.Replace('_', ' ');
            }
            byte[] f = null;
            if (isNaborDocId)
            {
                f = await (from b in _context.VzOrderBinaries
                           join dh in _context.Dh11948s on b.Id equals dh.Sp14003
                           where (dh.Iddoc == id) && (b.Extension == "LABELS")
                           select b.Binary).FirstOrDefaultAsync();
                id = await _context.Dh11948s.Where(x => x.Iddoc == id).Select(x => x.Sp14003).FirstOrDefaultAsync();
            }
            else
                f = await (from b in _context.VzOrderBinaries
                           where b.Id == id && (b.Extension == "LABELS")
                           select b.Binary).FirstOrDefaultAsync();
            if (f?.Length > 0)
            {
                var order = await _order.ПолучитьOrderWithItems(id);
                if (order?.Тип == "WILDBERRIES")
                {
                    var products = await _номенклатура.GetНоменклатураByListIdAsync(order.Items?.Select(x => x.НоменклатураId).ToList());
                    var stickers = new List<byte[]> { f };
                    foreach (var product in products)
                    {
                        string color = await _номенклатура.GetColorProperty(product.Id);
                        if (string.IsNullOrEmpty(color))
                            color = "белый";
                        var sticker = PdfHelper.PdfFunctions.Instance.ProductSticker(name: product.Наименование, barcodeText: product.Единица.Barcode, vendor: StinClasses.Common.Encode(product.Code, order.Encode), color: color);
                        stickers.Add(sticker);
                    }
                    f = PdfHelper.PdfFunctions.Instance.MergePdf(stickers);
                }
                return File(f, "application/pdf");
            }
            else
            {
                if (id == Common.ПустоеЗначение)
                    return Ok();
                string webRootPath = _webHostEnvironment.WebRootPath;
                string contentRootPath = _webHostEnvironment.ContentRootPath;

                string path = System.IO.Path.Combine(webRootPath, "lib", "images", "not-found-image.jpg");
                return File(System.IO.File.ReadAllBytes(path), "image/png");
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetMultiLabelsPdf(string[] ids)
        {
            var orderIds = ids.Select(x => x.Replace('_', ' ')).ToList();
            var files = (from b in _context.VzOrderBinaries
                         where orderIds.Contains(b.Id) && b.Extension == "LABELS"
                         orderby b.Id
                         select new { b.Id, b.Binary }).ToList();
            if (files?.Count > 0)
            {
                var stickers = new List<byte[]>();
                foreach (var file in files)
                {
                    stickers.Add(file.Binary);
                    var order = await _order.ПолучитьOrderWithItems(file.Id);
                    if (order?.Тип == "WILDBERRIES")
                    {
                        var products = await _номенклатура.GetНоменклатураByListIdAsync(order.Items?.Select(x => x.НоменклатураId).ToList());
                        foreach (var product in products)
                        {
                            string color = await _номенклатура.GetColorProperty(product.Id);
                            if (string.IsNullOrEmpty(color))
                                color = "белый";
                            var sticker = PdfHelper.PdfFunctions.Instance.ProductSticker(name: product.Наименование, barcodeText: product.Единица.Barcode, vendor: StinClasses.Common.Encode(product.Code, order.Encode), color: color);
                            stickers.Add(sticker);
                        }
                    }
                }
                return File(PdfHelper.PdfFunctions.Instance.MergePdf(stickers), "application/pdf");
            }
            else
            {
                string webRootPath = _webHostEnvironment.WebRootPath;
                string contentRootPath = _webHostEnvironment.ContentRootPath;

                string path = System.IO.Path.Combine(webRootPath, "lib", "images", "not-found-image.jpg");
                return File(System.IO.File.ReadAllBytes(path), "image/png");
            }
        }
        private async Task<string> SendOrderShippedAsync(string orderId, StinClasses.ReceiverPaymentType receiverPaymentType, string receiverEmail, string receiverPhone)
        {
            string functionResult = "";
            var validMarketTypes = new List<string> { "ЯНДЕКС", "SBER" };
            var order = await _order.ПолучитьOrder(orderId);
            if ((order != null) && validMarketTypes.Contains(order.Тип))
            {
                using var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                using var client = new HttpClient(httpClientHandler);
                var url = Startup.sConfiguration["Settings:UrlYandexApi"] + "/order/int_status_shipped";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("User-Agent", "HttpClientFactory-StinClient");
                request.Headers.Add("Authorization", Startup.sConfiguration["Settings:UrlYandexApiAuthorization"]);

                var requestedOrderId = new StinClasses.RequestedOrderId();
                requestedOrderId.Id = orderId;
                requestedOrderId.UserId = User.FindFirstValue("UserId");
                requestedOrderId.PaymentType = receiverPaymentType;
                requestedOrderId.ReceiverEmail = receiverEmail;
                requestedOrderId.ReceiverPhone = receiverPhone;

                request.Content = new StringContent(requestedOrderId.SerializeObject(), System.Text.Encoding.UTF8, "application/json");
                try
                {
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        if (response.Content != null)
                        {
                            functionResult += await response.Content.ReadAsStringAsync();
                        }
                    }
                    else
                    {
                        functionResult += "Bad request for orderId = " + orderId;
                    }
                }
                catch (Exception ex)
                {
                    functionResult += ex.Message + " " + ex.InnerException;
                }
            }
            return functionResult;
        }
        [HttpPost]
        public async Task<IActionResult> СформироватьОтгрузочныеДокументы(string orderId, int intReceiverPaymentType, string receiverEmail, string receiverPhone)
        {
            if (!string.IsNullOrWhiteSpace(receiverPhone))
            {
                receiverPhone = receiverPhone.Replace(" ", "");
                if (receiverPhone.StartsWith("+7"))
                    receiverPhone = receiverPhone.Substring(2);
            }
            var result = await SendOrderShippedAsync(orderId, (StinClasses.ReceiverPaymentType)intReceiverPaymentType, receiverEmail, receiverPhone);
            if (string.IsNullOrEmpty(result))
                return Ok();
            return BadRequest(result);
        }
        [HttpPost]
        public async Task<IActionResult> СформироватьМультиОтгрузочныеДокументы(string[] ids)
        {
            string functionResult = "";
            foreach (var orderId in ids.Select(x => x.Replace('_', ' ')))
            {
                functionResult = await SendOrderShippedAsync(orderId, StinClasses.ReceiverPaymentType.NotFound, "", "");
                if (!string.IsNullOrEmpty(functionResult))
                    break;
            }
            if (string.IsNullOrEmpty(functionResult))
                return Ok();
            return BadRequest(functionResult);
        }
        [HttpPost]
        public async Task<IActionResult> ПередатьОтправлениеКОтгрузке(string orderId, CancellationToken cancellationToken)
        {
            try
            {
                var data = await (
                    from m in _context.Sc14042s
                    join o in _context.Sc13994s on m.Id equals o.Sp14038
                    where o.Id == orderId
                    select new
                    {
                        тип = m.Sp14155.ToUpper().Trim(),
                        clientId = m.Sp14053.Trim(),
                        token = m.Sp14054.Trim(),
                        postingNumber = o.Code.Trim()
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                if ((data != null) && (data.тип == "OZON"))
                {
                    var ozonResult = await OzonClasses.OzonOperators.AddToDelivery(_httpService, data.clientId, data.token,
                        data.postingNumber,
                        cancellationToken);
                    if (ozonResult.Item1 != null)
                    {
                        if (ozonResult.Item1.Value)
                        {
                            await _order.ОбновитьOrderStatus(orderId, 3);
                            return Ok();
                        }
                        else
                            return BadRequest(new ExceptionData { Code = -100, Description = "Add to delivery result is false" });
                    }
                    if (ozonResult.Item2 != null)
                        return BadRequest(new ExceptionData { Code = -100, Description = ozonResult.Item2 });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ExceptionData { Code = -100, Description = "AddToDelivery : " + ex.Message });
            }
            return BadRequest(new ExceptionData { Code = -100, Description = "AddToDelivery : Unhandling exception" });
        }
        [HttpPost]
        public async Task<IActionResult> ПечатьНабор(string[] ids, CancellationToken cancellationToken)
        {
            var html = "";
            int docOnPage = 0;
            var printedOrderIds = new List<string>();
            foreach (var orderId in ids.Select(x => x.Replace('_', ' ')).OrderBy(x => x))
            {
                var активныеНаборы = await _набор.ПолучитьСписокАктивныхНаборов(orderId, false);
                foreach (var формаНабор in активныеНаборы)
                {
                    var data = await _набор.PrintForm(html, 1, docOnPage, формаНабор, cancellationToken);
                    html = data.html;
                    docOnPage = data.docOnPage;
                    if (!printedOrderIds.Contains(orderId))
                        printedOrderIds.Add(orderId);
                }
            }
            if (printedOrderIds.Count > 0)
            {
                int tryCount = 5;
                TimeSpan sleepPeriod = TimeSpan.FromSeconds(1);
                while (true)
                {
                    using var tran = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        await _orderFunctions.SetOrdersPrinted(printedOrderIds);

                        if (_context.Database.CurrentTransaction != null)
                            tran.Commit();
                        break;
                    }
                    catch
                    {
                        if (_context.Database.CurrentTransaction != null)
                            _context.Database.CurrentTransaction.Rollback();
                        if (--tryCount == 0)
                        {
                            break;
                        }
                        await Task.Delay(sleepPeriod);
                    }
                }
            }
            return Ok(html);
        }
        [HttpPost]
        public async Task<IActionResult> ПодтвердитьОтмену(string orderId)
        {
            using var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var client = new HttpClient(httpClientHandler);
            var url = Startup.sConfiguration["Settings:UrlYandexApi"] + "/order/int_status_cancelled_user_changed_mind";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("User-Agent", "HttpClientFactory-StinClient");
            request.Headers.Add("Authorization", Startup.sConfiguration["Settings:UrlYandexApiAuthorization"]);
            var requestedOrderId = new StinClasses.RequestedOrderId();
            requestedOrderId.Id = orderId;
            requestedOrderId.UserId = User.FindFirstValue("UserId");
            string content = Newtonsoft.Json.JsonConvert.SerializeObject(requestedOrderId,
               Newtonsoft.Json.Formatting.None,
               new Newtonsoft.Json.JsonSerializerSettings
               {
                   ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
               });
            //var content = $"{{\"docId\": \"{orderId}\" }}";
            if (!string.IsNullOrEmpty(content))
            {
                request.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            }
            try
            {
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    if (response.Content != null)
                    {
                        var functionResult = await response.Content.ReadAsStringAsync();
                        return Ok(functionResult);
                    }
                    return Ok();
                }
                else
                {
                    return BadRequest("Bad request");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message + " " + ex.InnerException);
            }
        }
        [HttpPost]
        public IActionResult GetOrderDetails(string orderId)
        {
            HttpContext.Session.SetObjectAsJson("мнТабличнаяЧасть", null);

            return ViewComponent("Order", new
            {
                orderId
            });
        }
        [HttpGet]
        public IActionResult ТаблицаЗаказов(DateTime startDate, DateTime endDate, bool alive)
        {
            IQueryable<VzZayavki> result0;
            //var result0 = Enumerable.Repeat(new { z = new VzZayavki(), ispoln = false }, 0).AsQueryable();
            if (alive)
            {
                DateTime dateRegTA = _context.GetRegTA();
                var reg = (from r in _context.Rg4674s
                           where r.Period == dateRegTA
                           group r by r.Sp4671 into gr
                           where gr.Sum(x => x.Sp4672) != 0 || gr.Sum(x => x.Sp4673) != 0
                           select new { zDoc = gr.Key })
                          .Concat
                          (from r in _context.Rg4667s
                           where r.Period == dateRegTA
                           group r by r.Sp4664 into gr
                           where gr.Sum(x => x.Sp4666) != 0
                           select new { zDoc = gr.Key }
                           );
                var regN = from r in _context.Rg11973s
                           where r.Period == dateRegTA
                           group r by r.Sp11970 into gr
                           where gr.Sum(x => x.Sp11972) != 0
                           select new { docN = gr.Key };

                result0 = (from z in _context.VzZayavkis
                           join inv in _context.VzInvoices on z.IdDoc equals inv.ZIdDoc
                           join r in reg on inv.IdDoc equals r.zDoc
                           select z)
                           .Concat(
                           from z in _context.VzZayavkis
                           join nab in _context.VzNabors on z.IdDoc equals nab.ZIdDoc
                           join r in regN on nab.IdDoc equals r.docN
                           select z);
            }
            else
            {
                if (startDate == DateTime.MinValue)
                    startDate = DateTime.Now.Date;
                if (endDate == DateTime.MinValue)
                    endDate = DateTime.Now;
                //string j_startDateTime = startDate.JournalDateTime();
                //string j_endDateTime = endDate.JournalDateTime();
                //DateTime dateRegTA = _context.GetRegTA();
                //var reg = from r in _context.Rg4674s
                //          where r.Period == dateRegTA
                //          group r by r.Sp4671 into gr
                //          where gr.Sum(x => x.Sp4672) != 0 || gr.Sum(x => x.Sp4673) != 0
                //          select new { zDoc = gr.Key };
                result0 = from z in _context.VzZayavkis
                              //join inv in _context.VzInvoices on z.IdDoc equals inv.ZIdDoc
                              //join r in reg on inv.IdDoc equals r.zDoc into _r
                              //from r in _r.DefaultIfEmpty()
                          where z.DocDate >= startDate && z.DocDate <= endDate
                          select z;
            }
            var result = from z in result0
                         join j in _context._1sjourns on z.IdDoc equals j.Iddoc
                         orderby z.DocDate
                         select new ЗаказКлиента
                         {
                             НомерЗаказа = z.ZNo.Trim(),
                             ДатаЗаказа = z.ZDate.HasValue ? new DateTime(z.ZDate.Value.Ticks) : DateTime.MinValue,
                             Корень = new СчетЗаказа
                             {
                                 IdDoc = z.IdDoc,
                                 ДатаДок = z.DocDate,
                                 НомерДок = j.Docno,
                                 Контрагент = new Контрагент
                                 {
                                     Id = z.DocCustomer,
                                     Наименование = z.DocCustomerName
                                 },
                                 Менеджер = new Менеджер
                                 {
                                     Id = z.DocManager,
                                     Наименование = z.DocManagerName
                                 }
                             },
                             СчетНаОплату = z.VzInvoices.Where(i => i.DocType == "   3O2   ").OrderByDescending(o => o.DocDate).Select(s => s.DocSumma).FirstOrDefault(),
                             ЗаявкаНаСогласование = z.VzInvoices.Where(i => i.DocType == "   3O3   ").OrderByDescending(o => o.DocDate).Select(s => s.DocSumma).FirstOrDefault(),
                             ЗаявкаСогласованная = z.VzInvoices.Where(i => i.DocType == "   AD6   ").OrderByDescending(o => o.DocDate).Select(s => s.DocSumma).FirstOrDefault(),
                             ЗаявкаОдобренная = z.VzInvoices.Where(i => i.DocType == "   APG   ").OrderByDescending(o => o.DocDate).Select(s => s.DocSumma).FirstOrDefault(),
                             ЗаявкаИсполненная = true,
                             ОтменаЗаявки = z.VzCancelZayavkis.Count > 0,
                             Набор = z.VzNabors.Sum(s => s.DocSumma),
                             ОтменаНабора = z.VzCancelNabors.Sum(c => c.DocSumma),
                             Продажа = z.VzProdagis.Sum(x => x.DocSumma),
                             ОплатаОжидание = z.VzPayments.Where(x => x.DocType == 13849 && (x.DocStatus == "   AOW   " || x.DocStatus == "   AOX   ")).Sum(x => x.DocSumma),
                             ОплатаВыполнено = z.VzPayments.Where(x => x.DocType == 2196).Sum(x => x.DocSumma),
                             ОплатаОтменено = z.VzPayments.Where(x => x.DocType == 13849 && x.DocStatus == "   AOZ   ").Sum(x => x.DocSumma),
                         };

            return PartialView("~/Views/ИнтернетЗаказы/Zayavki.cshtml", result);
        }
        public JsonResult ЖурналЗаказов(string id, int вид, DateTime startDate, DateTime endDate)
        {
            if (id != "#" && вид > 0)
            {
                var tree = from t in _context.fn_GetTreeById(id.Replace("_", " "), false)
                           join j in _context._1sjourns on t.Iddoc equals j.Iddoc
                           where j.Closed == 1
                           select new
                           {
                               t.Iddoc,
                               t.Parentid,
                               j.Iddocdef
                           };
                var treeList = tree.ToList();
                var ПродажиMdId = new List<int> { 1611, 3114 };
                var ПродажиIdDoc = treeList.Where(x => ПродажиMdId.Contains(x.Iddocdef)).Select(x => x.Iddoc);
                var Продажи = (from j in _context._1sjourns.Where(x => ПродажиIdDoc.Contains(x.Iddoc))
                               join docРеализация in _context.Dh1611s on j.Iddoc equals docРеализация.Iddoc into _docРеализация
                               from docРеализация in _docРеализация.DefaultIfEmpty()
                               join docОтчетККМ in _context.Dh3114s on j.Iddoc equals docОтчетККМ.Iddoc into _docОтчетККМ
                               from docОтчетККМ in _docОтчетККМ.DefaultIfEmpty()
                               select new
                               {
                                   id = "Продажа",
                                   сумма = docРеализация != null ? docРеализация.Sp1604 :
                                           docОтчетККМ != null ? docОтчетККМ.Sp3107 :
                                           0
                               })
                              .AsEnumerable();
                var parentIds = treeList.Select(y => y.Parentid);
                var validIds = treeList.Where(x => !parentIds.Contains(x.Iddoc)).Select(x => x.Iddoc);
                var result = (from j in _context._1sjourns.Where(x => validIds.Contains(x.Iddoc))
                              join docSpros in _context.Dh12784s on j.Iddoc equals docSpros.Iddoc into _docSpros
                              from docSpros in _docSpros.DefaultIfEmpty()
                              join docСчет in _context.Dh2457s.Where(x => x.Sp4760 == "   3O2   ") on j.Iddoc equals docСчет.Iddoc into _docСчет
                              from docСчет in _docСчет.DefaultIfEmpty()
                              join docНСчет in _context.Dh2457s.Where(x => x.Sp4760 == "   3O1   ") on j.Iddoc equals docНСчет.Iddoc into _docНСчет
                              from docНСчет in _docНСчет.DefaultIfEmpty()
                              join docЗаякаНаСогл in _context.Dh2457s.Where(x => x.Sp4760 == "   3O3   ") on j.Iddoc equals docЗаякаНаСогл.Iddoc into _docЗаякаНаСогл
                              from docЗаякаНаСогл in _docЗаякаНаСогл.DefaultIfEmpty()
                              join docЗаякаСогл in _context.Dh2457s.Where(x => x.Sp4760 == "   AD6   ") on j.Iddoc equals docЗаякаСогл.Iddoc into _docЗаякаСогл
                              from docЗаякаСогл in _docЗаякаСогл.DefaultIfEmpty()
                              join docЗаякаОдобр in _context.Dh2457s.Where(x => x.Sp4760 == "   APG   ") on j.Iddoc equals docЗаякаОдобр.Iddoc into _docЗаякаОдобр
                              from docЗаякаОдобр in _docЗаякаОдобр.DefaultIfEmpty()
                              join docНабор in _context.Dh11948s on j.Iddoc equals docНабор.Iddoc into _docНабор
                              from docНабор in _docНабор.DefaultIfEmpty()
                                  //join docРеализация in _context.Dh1611s on j.Iddoc equals docРеализация.Iddoc into _docРеализация
                                  //from docРеализация in _docРеализация.DefaultIfEmpty()
                                  //join docОтчетККМ in _context.Dh3114s on j.Iddoc equals docОтчетККМ.Iddoc into _docОтчетККМ
                                  //from docОтчетККМ in _docОтчетККМ.DefaultIfEmpty()
                                  //join docЧекККМ in _context.Dh3046s on j.Iddoc equals docЧекККМ.Iddoc into _docЧекККМ
                                  //from docЧекККМ in _docЧекККМ.DefaultIfEmpty()

                              select new
                              {
                                  id = docСчет != null ? "СчетНаОплату" :
                                       docНСчет != null ? "НеподтвержденныйСчет" :
                                       docЗаякаНаСогл != null ? "ЗаявкаНаСогл" :
                                       docЗаякаСогл != null ? "ЗаякаСогл" :
                                       docЗаякаОдобр != null ? "ЗаякаОдобр" :
                                       //(docРеализация != null || docОтчетККМ != null || docЧекККМ != null) ? "Продажа" :
                                       j.Iddocdef.ToString(),
                                  сумма = docСчет != null ? docСчет.Sp2451 :
                                          docНСчет != null ? docНСчет.Sp2451 :
                                          docЗаякаНаСогл != null ? docЗаякаНаСогл.Sp2451 :
                                          docЗаякаСогл != null ? docЗаякаСогл.Sp2451 :
                                          docЗаякаОдобр != null ? docЗаякаОдобр.Sp2451 :
                                          docSpros != null ? docSpros.Sp12778 :
                                          docНабор != null ? docНабор.Sp11946 :
                                          //docРеализация != null ? docРеализация.Sp1604 :
                                          //docОтчетККМ != null ? docОтчетККМ.Sp3107 :
                                          //docЧекККМ != null ? docЧекККМ.Sp13055 :
                                          0
                              })
                              .AsEnumerable();
                result = result.Concat(Продажи);

                return Json((from t in result
                             group t by t.id into gr
                             select new
                             {
                                 id = gr.Key,
                                 parent = id,
                                 text = gr.Key == "СчетНаОплату" ? "Счета на оплату" :
                                      gr.Key == "НеподтвержденныйСчет" ? "Неподтвержденные счета" :
                                      gr.Key == "ЗаявкаНаСогл" ? "Заявки (на согласование)" :
                                      gr.Key == "ЗаякаСогл" ? "Заявки (согласованные)" :
                                      gr.Key == "ЗаякаОдобр" ? "Заяки (одобренные)" :
                                      gr.Key == "12784" ? "Неудовлетворенный спрос" :
                                      gr.Key == "11948" ? "Набор" :
                                      gr.Key == "Продажа" ? "Продажи" :
                                      "Прочее",
                                 children = false,
                                 icon = "jstree-file",
                                 data = new
                                 {
                                     priority = gr.Key == "12784" ? 10 :
                                              gr.Key == "НеподтвержденныйСчет" ? 20 :
                                              gr.Key == "СчетНаОплату" ? 30 :
                                              gr.Key == "ЗаявкаНаСогл" ? 40 :
                                              gr.Key == "ЗаякаСогл" ? 50 :
                                              gr.Key == "ЗаякаОдобр" ? 60 :
                                              gr.Key == "11948" ? 100 :
                                              gr.Key == "Продажа" ? 150 :
                                              1000,
                                     сумма = gr.Sum(x => x.сумма),
                                 }
                             })
                          .OrderBy(x => x.data.priority));
            }
            else
            {
                if (startDate == DateTime.MinValue)
                    startDate = DateTime.Now.Date;
                if (endDate == DateTime.MinValue)
                    endDate = DateTime.Now;
                string j_startDateTime = startDate.JournalDateTime();
                string j_endDateTime = endDate.JournalDateTime();

                var ПредварительнаяЗаявка = (from doc in _context.Dh12747s
                                             join j in _context._1sjourns on doc.Iddoc equals j.Iddoc
                                             join docSpros in _context.Dh12784s on doc.Iddoc equals docSpros.Sp12748.Substring(4, 9) into _docSpros
                                             from docSpros in _docSpros.DefaultIfEmpty()
                                             where j.Iddocdef == 12747 &&
                                              j.DateTimeIddoc.CompareTo(j_startDateTime) >= 0 &&
                                              j.DateTimeIddoc.CompareTo(j_endDateTime) <= 0
                                             orderby j.DateTimeIddoc
                                             select new
                                             {
                                                 id = j.Iddoc.Replace(' ', '_'),
                                                 parent = "#",
                                                 text = "Предварительная заявка №" + j.Docno + " от " +
                                                    j.DateTimeIddoc.Substring(6, 2) + "." +
                                                    j.DateTimeIddoc.Substring(4, 2) + "." +
                                                    j.DateTimeIddoc.Substring(0, 4) + ".",
                                                 children = j.Closed == 1,
                                                 icon = j.Closed == 1 ? "" : "jstree-file",
                                                 data = new
                                                 {
                                                     вид = j.Iddocdef,
                                                     датаДок = j.DateTimeIddoc,
                                                     суммаСпроса = docSpros != null ? docSpros.Sp12778 : 0m,
                                                     сумма = doc.Sp12741,
                                                 }
                                             })
                                             .OrderBy(x => x.data.датаДок)
                                             .AsEnumerable();

                return Json(ПредварительнаяЗаявка);
            }
        }
        public async Task<string> ЗаписатьДокумент(ИнтернетЗаказ doc)
        {
            string message = "";
            doc.Фирма = _фирмаRepository.GetEntityById(doc.Фирма.Id);
            doc.Склад = _складRepository.GetEntityById(doc.Склад.Id);
            doc.Контрагент = _контрагентRepository.GetEntityById(doc.Контрагент.Id);
            УсловияДоговора условияДоговора = await _контрагентRepository.ПолучитьУсловияДоговораКонтрагентаAsync(doc.Договор.Id);
            doc.ТипЦен = new ТипЦен { Id = условияДоговора.ТипЦенId, Наименование = условияДоговора.ТипЦен };
            decimal ПроцентСкидки = условияДоговора.СкидкаОтсрочка + (doc.Доставка ? await _контрагентRepository.ПолучитьПроцентСкидкиЗаДоставкуAsync(doc.Договор.Id, (int)doc.ТипДоставки) : 0);
            doc.Скидка = await _контрагентRepository.ПолучитьСпрСкидкиПоПроцентуAsync(ПроцентСкидки);
            List<Корзина> корзина = HttpContext.Session.GetObjectFromJson<List<Корзина>>("мнТабличнаяЧасть");
            using (var docTran = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    Common.UnLockDocNo(_context, "12747", doc.НомерДок);
                    if (doc.Доставка && !string.IsNullOrEmpty(doc.НомерМаршрута.Наименование))
                    {
                        Маршрут маршрут = _context.СоздатьЭлементМаршрута();
                        doc.НомерМаршрута.Id = маршрут.Id;
                        doc.НомерМаршрута.Code = маршрут.Code;
                    }
                    DateTime docDateTime = DateTime.Now;

                    _1sjourn j = Common.GetEntityJourn(_context, 0, 0, 4588, 12747, null, "ПредварительнаяЗаявка",
                        doc.НомерДок, docDateTime,
                        doc.Фирма.Id,
                        doc.Пользователь.Id,
                        doc.Склад.Наименование,
                        doc.Контрагент.Наименование);
                    await _context._1sjourns.AddAsync(j);

                    doc.DocId = j.Iddoc;
                    doc.ДатаДок = docDateTime;
                    Dh12747 docHeader = new Dh12747
                    {
                        Iddoc = j.Iddoc,
                        Sp12711 = Common.ПустоеЗначениеИд13,//докОснование
                        Sp12712 = doc.Фирма.Счет.Id, //БанковскийСчет
                        Sp12713 = doc.Контрагент.Id,
                        Sp12714 = doc.Договор.Id,
                        Sp12715 = Common.ВалютаРубль,
                        Sp12716 = 1, //Курс
                        Sp12717 = doc.Фирма.ЮрЛицо.УчитыватьНДС,
                        Sp12718 = 1, //СуммаВклНДС
                        Sp12719 = 0, //УчитыватьНП
                        Sp12720 = 0, //СуммаВклНП
                        Sp12721 = корзина.Sum(x => x.Сумма), //СуммаВзаиморасчетов
                        Sp12722 = doc.ТипЦен.Id, //ТипЦен
                        Sp12723 = doc.Скидка.Id, //Скидка
                        Sp12724 = doc.ДатаОплаты,
                        Sp12725 = doc.ДатаОтгрузки,
                        Sp12726 = doc.Склад.Id,
                        Sp12727 = Common.СпособыРезервирования.FirstOrDefault(x => x.Value == "Резервировать только из текущего остатка").Key,
                        Sp12728 = (doc.СкидКарта.Id == null ? Common.ПустоеЗначение : doc.СкидКарта.Id),
                        Sp12729 = 1, //ПоСтандарту
                        Sp12730 = 0, //ДанаДопСкидка
                        Sp12731 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.CпособОтгрузки).Key,
                        Sp12732 = "", //НомерКвитанции
                        Sp12733 = 0, //ДатаКвитанции
                        Sp12734 = (doc.Доставка ? doc.НомерМаршрута.Code : ""), //ИндМаршрута
                        Sp12735 = (doc.Доставка ? doc.НомерМаршрута.Наименование : ""), //НомерМаршрута
                        Sp14007 = Common.ПустоеЗначение,
                        Sp12741 = 0, //Сумма
                        Sp12743 = 0, //СуммаНДС
                        Sp12745 = 0, //СуммаНП
                        Sp660 = string.IsNullOrEmpty(doc.Комментарий) ? "" : doc.Комментарий
                    };
                    await _context.Dh12747s.AddAsync(docHeader);

                    short lineNo = 1;
                    foreach (Корзина item in корзина)
                    {
                        Единицы ОсновнаяЕдиница = await _номенклатураRepository.ОсновнаяЕдиницаAsync(item.Id);
                        СтавкаНДС ставкаНДС = await _номенклатураRepository.СтавкаНДСAsync(item.Id);
                        Dt12747 docRow = new Dt12747
                        {
                            Iddoc = j.Iddoc,
                            Lineno = lineNo++,
                            Sp12736 = item.Id,
                            Sp12737 = item.Quantity,
                            Sp12738 = ОсновнаяЕдиница.Id,
                            Sp12739 = ОсновнаяЕдиница.Коэффициент,
                            Sp12740 = item.Цена,
                            Sp12741 = item.Сумма,
                            Sp12742 = ставкаНДС.Id,
                            Sp12743 = item.Сумма * (ставкаНДС.Процент / (100 + ставкаНДС.Процент)),
                            Sp12744 = Common.ПустоеЗначение,
                            Sp12745 = 0,
                            Sp13041 = 0
                        };
                        await _context.Dt12747s.AddAsync(docRow);
                    }
                    await _context.SaveChangesAsync();
                    await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 12747, j.Iddoc);

                    await _context.Database.ExecuteSqlRawAsync(
                        "exec _1sp_DH12747_UpdateTotals @num36",
                        new SqlParameter("@num36", j.Iddoc)
                        );
                    await _context.SaveChangesAsync();

                    docTran.Commit();
                    message = "OK";
                }
                catch (SqlException ex)
                {
                    docTran.Rollback();
                    if (ex.Number == -2)
                        return "timeout";
                    else
                        return ex.Message + Environment.NewLine + ex.InnerException;
                }
                catch (Exception ex)
                {
                    docTran.Rollback();
                    return ex.Message + Environment.NewLine + ex.InnerException;
                }
            }
            return message;
        }
        public async Task<string> СоздатьДокументСчет(ИнтернетЗаказ doc, Dictionary<string, List<ДанныеТабличнойЧасти>> переченьНаличия, List<string> СписокФирм, List<string> СписокСкладов)
        {
            string message = "";
            DateTime dateReg = Common.GetRegTA(_context);
            List<string> СписокТоваров = new List<string>();
            foreach (string склId in переченьНаличия.Keys)
            {
                СписокТоваров.AddRange(переченьНаличия[склId].Select(x => x.Номенклатура.Id));
            }
            IEnumerable<ТаблицаСвободныхОстатков> ТзОстатки = _номенклатураRepository.ПодготовитьОстатки(dateReg, СписокФирм, СписокСкладов, СписокТоваров).AsEnumerable();

            foreach (string склId in переченьНаличия.Keys)
            {
                List<ДанныеТабличнойЧасти> данныеТабличнойЧасти = переченьНаличия[склId];
                DateTime docDateTime = doc.ДатаДок.AddSeconds(1);
                DateTime docDate = docDateTime.Date;
                _1sjourn j = Common.GetEntityJourn(_context, 1, 0, 4588, 2457, null, "ЗаявкаПокупателя",
                    null, docDateTime,
                    doc.Фирма.Id,
                    doc.Пользователь.Id,
                    doc.Склад.Наименование,
                    doc.Контрагент.Наименование);
                //await _context._1sjourns.AddAsync(j);
                //DateTime docDate = DateTime.ParseExact(j.DateTimeIddoc.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture);

                Dh2457 docHeader = new Dh2457
                {
                    Iddoc = j.Iddoc,
                    Sp4433 = Common.Encode36(12747).PadLeft(4) + doc.DocId,
                    Sp2621 = doc.Фирма.Счет.Id,
                    Sp2434 = doc.Контрагент.Id,
                    Sp2435 = doc.Договор.Id,
                    Sp2436 = Common.ВалютаРубль,
                    Sp2437 = 1, //Курс
                    Sp2439 = doc.Фирма.ЮрЛицо.УчитыватьНДС,
                    Sp2440 = 1, //СуммаВклНДС
                    Sp2441 = 0, //УчитыватьНП
                    Sp2442 = 0, //СуммаВклНП
                    Sp2443 = данныеТабличнойЧасти.Sum(x => x.Сумма), //СуммаВзаиморасчетов
                    Sp2444 = doc.ТипЦен.Id, //ТипЦен
                    Sp2445 = doc.Скидка.Id, //Скидка
                    Sp2438 = doc.ДатаОплаты,
                    Sp4434 = doc.ДатаОтгрузки,
                    Sp4437 = склId,
                    Sp4760 = Common.ВидыОперации.FirstOrDefault(x => x.Value == "Счет на оплату").Key,//ВидОперации
                    Sp7943 = Common.СпособыРезервирования.FirstOrDefault(x => x.Value == "Резервировать только из текущего остатка").Key,
                    Sp8681 = (doc.СкидКарта.Id == null ? Common.ПустоеЗначение : doc.СкидКарта.Id),
                    Sp8835 = 1, //ПоСтандарту
                    Sp8910 = 0, //ДанаДопСкидка
                    Sp10382 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.CпособОтгрузки).Key,
                    Sp10864 = "", //НомерКвитанции
                    Sp10865 = 0, //ДатаКвитанции
                    Sp11556 = (doc.Доставка ? doc.НомерМаршрута.Code : ""), //ИндМаршрута
                    Sp11557 = (doc.Доставка ? doc.НомерМаршрута.Наименование : ""), //НомерМаршрута
                    Sp2451 = 0, //Сумма
                    Sp2452 = 0, //СуммаНДС
                    Sp2453 = 0, //СуммаНП
                    Sp660 = string.IsNullOrEmpty(doc.Комментарий) ? "" : doc.Комментарий,
                };
                await _context.Dh2457s.AddAsync(docHeader);
                short lineNo = 1;
                int КоличествоДвижений = 0;
                bool Приход = true;
                foreach (ДанныеТабличнойЧасти item in данныеТабличнойЧасти)
                {
                    СтавкаНДС ставкаНДС = await _номенклатураRepository.СтавкаНДСAsync(item.Номенклатура.Id);
                    Dt2457 docRow = new Dt2457
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp2446 = item.Номенклатура.Id,
                        Sp2447 = Math.Round(item.Количество / item.Номенклатура.Единица.Коэффициент, 3),
                        Sp2448 = item.Номенклатура.Единица.Id,
                        Sp2449 = item.Номенклатура.Единица.Коэффициент,
                        Sp2450 = item.Цена,
                        Sp2451 = item.Сумма,
                        Sp2454 = ставкаНДС.Id,
                        Sp2452 = item.Сумма * (ставкаНДС.Процент / (100 + ставкаНДС.Процент)),
                        Sp2455 = Common.ПустоеЗначение,
                        Sp2453 = 0,
                    };
                    await _context.Dt2457s.AddAsync(docRow);

                    decimal Зарезервировать = item.Количество;
                    if (Зарезервировать > 0)
                        foreach (string фирмаId in СписокФирм)
                        {
                            decimal Остаток = ТзОстатки
                                .Where(x => x.Фирма.Id == фирмаId && x.Склад.Id == склId && x.Номенклатура.Id == item.Номенклатура.Id)
                                .Sum(x => x.СвободныйОстаток);
                            decimal МожноЗарезервировать = Math.Min(Остаток, Зарезервировать);
                            if (МожноЗарезервировать > 0)
                            {
                                КоличествоДвижений++;
                                //РезервыТМЦ
                                j.Rf4480 = true;
                                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4480_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                    "@Фирма,@Номенклатура,@Склад,@ДоговорПокупателя,@ЗаявкаПокупателя,@Количество," +
                                    "@docDate,@CurPeriod,1,0",
                                    new SqlParameter("@num36", j.Iddoc),
                                    new SqlParameter("@ActNo", КоличествоДвижений),
                                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                    new SqlParameter("@Фирма", фирмаId),
                                    new SqlParameter("@Номенклатура", item.Номенклатура.Id),
                                    new SqlParameter("@Склад", склId),
                                    new SqlParameter("@ДоговорПокупателя", doc.Договор.Id),
                                    new SqlParameter("@ЗаявкаПокупателя", j.Iddoc),
                                    new SqlParameter("@Количество", МожноЗарезервировать),
                                    new SqlParameter("@docDate", docDate.ToShortDateString()),
                                    new SqlParameter("@CurPeriod", dateReg.ToShortDateString()));

                                КоличествоДвижений++;
                                //Заявки
                                j.Rf4674 = true;
                                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4674_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                    "@Фирма,@Номенклатура,@ДоговорПокупателя,@ЗаявкаПокупателя,@КоличествоРасход,@СтоимостьРасход," +
                                    "@docDate,@CurPeriod,1,0",
                                    new SqlParameter("@num36", j.Iddoc),
                                    new SqlParameter("@ActNo", КоличествоДвижений),
                                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                    new SqlParameter("@Фирма", фирмаId),
                                    new SqlParameter("@Номенклатура", item.Номенклатура.Id),
                                    new SqlParameter("@ДоговорПокупателя", doc.Договор.Id),
                                    new SqlParameter("@ЗаявкаПокупателя", j.Iddoc),
                                    new SqlParameter("@КоличествоРасход", МожноЗарезервировать),
                                    new SqlParameter("@СтоимостьРасход", (МожноЗарезервировать / item.Номенклатура.Единица.Коэффициент) * item.Цена),
                                    new SqlParameter("@docDate", docDate.ToShortDateString()),
                                    new SqlParameter("@CurPeriod", dateReg.ToShortDateString()));
                                Зарезервировать = Зарезервировать - МожноЗарезервировать;
                            }
                            if (Зарезервировать <= 0)
                                break;
                        }
                    if (Зарезервировать > 0)
                    {
                        if (!string.IsNullOrEmpty(message))
                            message += Environment.NewLine;
                        message += "На складе нет нужного свободного количества ТМЦ ";
                        if (!string.IsNullOrEmpty(item.Номенклатура.Артикул))
                            message += "(" + item.Номенклатура.Артикул + ") ";
                        if (!string.IsNullOrEmpty(item.Номенклатура.Наименование))
                            message += item.Номенклатура.Наименование;
                        else
                            message += "'" + item.Номенклатура.Id + "'";
                    }
                }
                if (doc.Доставка && !string.IsNullOrEmpty(doc.НомерМаршрута.Id) && !string.IsNullOrEmpty(doc.НомерМаршрута.Наименование))
                {
                    КоличествоДвижений++;
                    await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(doc.НомерМаршрута.Id, 11552, j.Iddoc, docDateTime, Common.Encode36(2457).PadLeft(4) + j.Iddoc, КоличествоДвижений));
                    КоличествоДвижений++;
                    await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(doc.НомерМаршрута.Id, 11553, j.Iddoc, docDateTime, doc.НомерМаршрута.Наименование, КоличествоДвижений));
                }
                j.Actcnt = КоличествоДвижений;
                await _context._1sjourns.AddAsync(j);

                await _context.SaveChangesAsync();
                await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 2457, j.Iddoc);
                await Common.ОбновитьПодчиненныеДокументы(_context, Common.Encode36(12747).PadLeft(4) + doc.DocId, j.DateTimeIddoc, j.Iddoc);

                await _context.Database.ExecuteSqlRawAsync(
                    "exec _1sp_DH2457_UpdateTotals @num36",
                    new SqlParameter("@num36", j.Iddoc)
                    );

                //await Common.ОбновитьВремяТА(_context, j.Iddoc, j.DateTimeIddoc);
                //await Common.ОбновитьПоследовательность(_context, j.DateTimeIddoc);
                MaxIddoc = j.Iddoc;
                MaxDateTimeIddoc = j.DateTimeIddoc;
            }
            return message;
        }
        public async Task<string> СоздатьДокументСпрос(ИнтернетЗаказ doc, List<ДанныеТабличнойЧасти> СпросТаблица)
        {
            string message = "";
            DateTime dateReg = Common.GetRegTA(_context);
            DateTime docDateTime = doc.ДатаДок.AddSeconds(1);
            DateTime docDate = docDateTime.Date;
            _1sjourn j = Common.GetEntityJourn(_context, 1, 0, 4588, 12784, null, "Спрос",
                null, docDateTime,
                doc.Фирма.Id,
                doc.Пользователь.Id,
                doc.Склад.Наименование,
                doc.Контрагент.Наименование);

            Dh12784 docHeader = new Dh12784
            {
                Iddoc = j.Iddoc,
                Sp12748 = Common.Encode36(12747).PadLeft(4) + doc.DocId,
                Sp12749 = doc.Фирма.Счет.Id,
                Sp12750 = doc.Контрагент.Id,
                Sp12751 = doc.Договор.Id,
                Sp12752 = Common.ВалютаРубль,
                Sp12753 = 1, //Курс
                Sp12754 = doc.Фирма.ЮрЛицо.УчитыватьНДС,
                Sp12755 = 1, //СуммаВклНДС
                Sp12756 = 0, //УчитыватьНП
                Sp12757 = 0, //СуммаВклНП
                Sp12758 = СпросТаблица.Sum(x => x.Сумма), //СуммаВзаиморасчетов
                Sp12759 = doc.ТипЦен.Id, //ТипЦен
                Sp12760 = doc.Скидка.Id, //Скидка
                Sp12761 = doc.ДатаОплаты,
                Sp12762 = doc.ДатаОтгрузки,
                Sp12763 = doc.Склад.Id,
                Sp12764 = Common.СпособыРезервирования.FirstOrDefault(x => x.Value == "Резервировать только из текущего остатка").Key,
                Sp12765 = (doc.СкидКарта.Id == null ? Common.ПустоеЗначение : doc.СкидКарта.Id),
                Sp12766 = 1, //ПоСтандарту
                Sp12767 = 0, //ДанаДопСкидка
                Sp12768 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.CпособОтгрузки).Key,
                Sp12769 = "", //НомерКвитанции
                Sp12770 = 0, //ДатаКвитанции
                Sp12771 = "", //ИндМаршрута
                Sp12772 = "", //НомерМаршрута
                Sp12778 = 0, //Сумма
                Sp12780 = 0, //СуммаНДС
                Sp12782 = 0, //СуммаНП
                Sp660 = string.IsNullOrEmpty(doc.Комментарий) ? "" : doc.Комментарий,
            };
            await _context.Dh12784s.AddAsync(docHeader);
            short lineNo = 1;
            int КоличествоДвижений = 0;
            bool Приход = true;
            foreach (ДанныеТабличнойЧасти item in СпросТаблица)
            {
                СтавкаНДС ставкаНДС = await _номенклатураRepository.СтавкаНДСAsync(item.Номенклатура.Id);
                Dt12784 docRow = new Dt12784
                {
                    Iddoc = j.Iddoc,
                    Lineno = lineNo++,
                    Sp12773 = item.Номенклатура.Id,
                    Sp12774 = Math.Round(item.Количество / item.Номенклатура.Единица.Коэффициент, 3),
                    Sp12775 = item.Номенклатура.Единица.Id,
                    Sp12776 = item.Номенклатура.Единица.Коэффициент,
                    Sp12777 = item.Цена,
                    Sp12778 = item.Сумма,
                    Sp12779 = ставкаНДС.Id,
                    Sp12780 = item.Сумма * (ставкаНДС.Процент / (100 + ставкаНДС.Процент)),
                    Sp12781 = Common.ПустоеЗначение,
                    Sp12782 = 0,
                };
                await _context.Dt12784s.AddAsync(docRow);

                КоличествоДвижений++;
                //Спрос
                j.Rf12791 = true;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA12791_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@Номенклатура,@Покупатель,@Склад,@Количество,@Стоимость," +
                    "@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", j.Iddoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@Номенклатура", item.Номенклатура.Id),
                    new SqlParameter("@Покупатель", doc.Контрагент.Id),
                    new SqlParameter("@Склад", doc.Склад.Id),
                    new SqlParameter("@Количество", item.Количество),
                    new SqlParameter("@Стоимость", item.Сумма),
                    new SqlParameter("@docDate", docDate.ToShortDateString()),
                    new SqlParameter("@CurPeriod", dateReg.ToShortDateString()));

                КоличествоДвижений++;
                //СпросОстатки
                j.Rf12815 = true;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA12815_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@Номенклатура,@Покупатель,@Склад,@Фирма,@Количество," +
                    "@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", j.Iddoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@Номенклатура", item.Номенклатура.Id),
                    new SqlParameter("@Покупатель", doc.Контрагент.Id),
                    new SqlParameter("@Склад", doc.Склад.Id),
                    new SqlParameter("@Фирма", doc.Фирма.Id),
                    new SqlParameter("@Количество", item.Количество),
                    new SqlParameter("@docDate", docDate.ToShortDateString()),
                    new SqlParameter("@CurPeriod", dateReg.ToShortDateString()));
            }
            j.Actcnt = КоличествоДвижений;
            await _context._1sjourns.AddAsync(j);

            await _context.SaveChangesAsync();
            await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 12784, j.Iddoc);
            await Common.ОбновитьПодчиненныеДокументы(_context, Common.Encode36(12747).PadLeft(4) + doc.DocId, j.DateTimeIddoc, j.Iddoc);

            await _context.Database.ExecuteSqlRawAsync(
                "exec _1sp_DH12784_UpdateTotals @num36",
                new SqlParameter("@num36", j.Iddoc)
                );

            //await Common.ОбновитьВремяТА(_context, j.Iddoc, j.DateTimeIddoc);
            //await Common.ОбновитьПоследовательность(_context, j.DateTimeIddoc);
            MaxIddoc = j.Iddoc;
            MaxDateTimeIddoc = j.DateTimeIddoc;

            return message;
        }
        public async Task<string> СоздатьДокументЗаявкаДилера(ИнтернетЗаказ doc, Фирма ФирмаАлко, List<Корзина> ТаблицаДилерскойЗаявки)
        {
            string message = "";
            DateTime dateReg = Common.GetRegTA(_context);
            DateTime docDateTime = doc.ДатаДок.AddSeconds(1);
            DateTime docDate = docDateTime.Date;

            _1sjourn j = Common.GetEntityJourn(_context, 1, 0, 4588, 2457, null, "ЗаявкаПокупателя",
                null, docDateTime,
                ФирмаАлко.Id,
                doc.Пользователь.Id,
                doc.Склад.Наименование,
                doc.Контрагент.Наименование);

            Dh2457 docHeader = new Dh2457
            {
                Iddoc = j.Iddoc,
                Sp4433 = Common.Encode36(12747).PadLeft(4) + doc.DocId,
                Sp2621 = ФирмаАлко.Счет.Id,
                Sp2434 = doc.Контрагент.Id,
                Sp2435 = doc.Договор.Id,
                Sp2436 = Common.ВалютаРубль,
                Sp2437 = 1, //Курс
                Sp2439 = ФирмаАлко.ЮрЛицо.УчитыватьНДС,
                Sp2440 = 1, //СуммаВклНДС
                Sp2441 = 0, //УчитыватьНП
                Sp2442 = 0, //СуммаВклНП
                Sp2443 = ТаблицаДилерскойЗаявки.Sum(x => x.Сумма), //СуммаВзаиморасчетов
                Sp2444 = doc.ТипЦен.Id, //ТипЦен
                Sp2445 = doc.Скидка.Id, //Скидка
                Sp2438 = doc.ДатаОплаты,
                Sp4434 = doc.ДатаОтгрузки,
                Sp4437 = doc.Склад.Id,
                Sp4760 = Common.ВидыОперации.FirstOrDefault(x => x.Value == "Заявка дилера").Key,//ВидОперации
                Sp7943 = Common.СпособыРезервирования.FirstOrDefault(x => x.Value == "Резервировать только из текущего остатка").Key,
                Sp8681 = (doc.СкидКарта.Id == null ? Common.ПустоеЗначение : doc.СкидКарта.Id),
                Sp8835 = 1, //ПоСтандарту
                Sp8910 = 0, //ДанаДопСкидка
                Sp10382 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.CпособОтгрузки).Key,
                Sp10864 = "", //НомерКвитанции
                Sp10865 = 0, //ДатаКвитанции
                Sp11556 = (doc.Доставка ? doc.НомерМаршрута.Code : ""), //ИндМаршрута
                Sp11557 = (doc.Доставка ? doc.НомерМаршрута.Наименование : ""), //НомерМаршрута
                Sp2451 = 0, //Сумма
                Sp2452 = 0, //СуммаНДС
                Sp2453 = 0, //СуммаНП
                Sp660 = string.IsNullOrEmpty(doc.Комментарий) ? "" : doc.Комментарий,
            };
            await _context.Dh2457s.AddAsync(docHeader);

            short lineNo = 1;
            int КоличествоДвижений = 0;
            bool Приход = true;
            IEnumerable<ТаблицаСвободныхОстатков> ТзОстатки = _номенклатураRepository.ПодготовитьОстатки(
                dateReg,
                new List<string>() { ФирмаАлко.Id },
                new List<string>() { doc.Склад.Id },
                ТаблицаДилерскойЗаявки.Select(x => x.Id).ToList())
                .AsEnumerable();

            foreach (Корзина item in ТаблицаДилерскойЗаявки)
            {
                Единицы ОсновнаяЕдиница = await _номенклатураRepository.ОсновнаяЕдиницаAsync(item.Id);
                СтавкаНДС ставкаНДС = await _номенклатураRepository.СтавкаНДСAsync(item.Id);
                Dt2457 docRow = new Dt2457
                {
                    Iddoc = j.Iddoc,
                    Lineno = lineNo++,
                    Sp2446 = item.Id,
                    Sp2447 = item.Quantity,
                    Sp2448 = ОсновнаяЕдиница.Id,
                    Sp2449 = ОсновнаяЕдиница.Коэффициент,
                    Sp2450 = item.Цена,
                    Sp2451 = item.Сумма,
                    Sp2454 = ставкаНДС.Id,
                    Sp2452 = item.Сумма * (ставкаНДС.Процент / (100 + ставкаНДС.Процент)),
                    Sp2455 = Common.ПустоеЗначение,
                    Sp2453 = 0,
                };
                await _context.Dt2457s.AddAsync(docRow);

                decimal Зарезервировать = item.Quantity * ОсновнаяЕдиница.Коэффициент;
                if (Зарезервировать > 0)
                {
                    decimal Остаток = ТзОстатки
                        .Where(x => x.Фирма.Id == ФирмаАлко.Id && x.Склад.Id == doc.Склад.Id && x.Номенклатура.Id == item.Id)
                        .Sum(x => x.СвободныйОстаток);
                    decimal МожноЗарезервировать = Math.Min(Остаток, Зарезервировать);
                    if (МожноЗарезервировать > 0)
                    {
                        КоличествоДвижений++;
                        //РезервыТМЦ
                        j.Rf4480 = true;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4480_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@Фирма,@Номенклатура,@Склад,@ДоговорПокупателя,@ЗаявкаПокупателя,@Количество," +
                            "@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", j.Iddoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@Фирма", ФирмаАлко.Id),
                            new SqlParameter("@Номенклатура", item.Id),
                            new SqlParameter("@Склад", doc.Склад.Id),
                            new SqlParameter("@ДоговорПокупателя", doc.Договор.Id),
                            new SqlParameter("@ЗаявкаПокупателя", j.Iddoc),
                            new SqlParameter("@Количество", МожноЗарезервировать),
                            new SqlParameter("@docDate", docDate.ToShortDateString()),
                            new SqlParameter("@CurPeriod", dateReg.ToShortDateString()));

                        КоличествоДвижений++;
                        //Заявки
                        j.Rf4674 = true;
                        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA4674_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                            "@Фирма,@Номенклатура,@ДоговорПокупателя,@ЗаявкаПокупателя,@КоличествоРасход,@СтоимостьРасход," +
                            "@docDate,@CurPeriod,1,0",
                            new SqlParameter("@num36", j.Iddoc),
                            new SqlParameter("@ActNo", КоличествоДвижений),
                            new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                            new SqlParameter("@Фирма", ФирмаАлко.Id),
                            new SqlParameter("@Номенклатура", item.Id),
                            new SqlParameter("@ДоговорПокупателя", doc.Договор.Id),
                            new SqlParameter("@ЗаявкаПокупателя", j.Iddoc),
                            new SqlParameter("@КоличествоРасход", МожноЗарезервировать),
                            new SqlParameter("@СтоимостьРасход", (МожноЗарезервировать / ОсновнаяЕдиница.Коэффициент) * item.Цена),
                            new SqlParameter("@docDate", docDate.ToShortDateString()),
                            new SqlParameter("@CurPeriod", dateReg.ToShortDateString()));
                        Зарезервировать = Зарезервировать - МожноЗарезервировать;
                    }
                }
                if (Зарезервировать > 0)
                {
                    if (!string.IsNullOrEmpty(message))
                        message += Environment.NewLine;
                    message += "На складе нет нужного свободного количества ТМЦ ";
                    if (!string.IsNullOrEmpty(item.Артикул))
                        message += "(" + item.Артикул + ") ";
                    if (!string.IsNullOrEmpty(item.Наименование))
                        message += item.Наименование;
                    else
                        message += "'" + item.Id + "'";
                }
            }
            if (doc.Доставка && !string.IsNullOrEmpty(doc.НомерМаршрута.Id) && !string.IsNullOrEmpty(doc.НомерМаршрута.Наименование))
            {
                КоличествоДвижений++;
                await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(doc.НомерМаршрута.Id, 11552, j.Iddoc, docDateTime, Common.Encode36(2457).PadLeft(4) + j.Iddoc, КоличествоДвижений));
                КоличествоДвижений++;
                await _context._1sconsts.AddAsync(_context.ИзменитьПериодическиеРеквизиты(doc.НомерМаршрута.Id, 11553, j.Iddoc, docDateTime, doc.НомерМаршрута.Наименование, КоличествоДвижений));
            }
            j.Actcnt = КоличествоДвижений;
            await _context._1sjourns.AddAsync(j);

            await _context.SaveChangesAsync();
            await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 2457, j.Iddoc);
            await Common.ОбновитьПодчиненныеДокументы(_context, Common.Encode36(12747).PadLeft(4) + doc.DocId, j.DateTimeIddoc, j.Iddoc);

            await _context.Database.ExecuteSqlRawAsync(
                "exec _1sp_DH2457_UpdateTotals @num36",
                new SqlParameter("@num36", j.Iddoc)
                );

            MaxIddoc = j.Iddoc;
            MaxDateTimeIddoc = j.DateTimeIddoc;

            return message;
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateDoc(ИнтернетЗаказ doc)
        {
            string message = await ЗаписатьДокумент(doc);
            if (message == "OK")
            {
                #region temp 
                List<string> СписокСкладов = new List<string>() { doc.Склад.Id };
                #endregion temp
                message = "";
                List<Корзина> корзина = HttpContext.Session.GetObjectFromJson<List<Корзина>>("мнТабличнаяЧасть");
                Common.SetObjectAsJson(HttpContext.Session, "мнТабличнаяЧасть", null);

                Фирма ФирмаАлко = null;
                Контрагент КонтрагентАлко = null;
                IEnumerable<Номенклатура> СписокТоваровДилера = Enumerable.Empty<Номенклатура>();
                if (await _контрагентRepository.ПроверкаНаДилераAsync(doc.Контрагент.Id, "Дилер АЛ-КО КОБЕР", "ДА"))
                {
                    string АлкоИНН = "7701190698";
                    КонтрагентАлко = await _контрагентRepository.ПолучитьПоИННAsync(АлкоИНН) ?? new Контрагент { Id = "" };
                    СписокТоваровДилера = _номенклатураRepository.ПолучитьНоменклатуруПоАгентуПроизводителя(КонтрагентАлко.Id, корзина.Select(x => x.Id)).AsEnumerable();
                    if (СписокТоваровДилера != null && СписокТоваровДилера.Count() > 0)
                        ФирмаАлко = await _фирмаRepository.ПолучитьПоИННAsync(АлкоИНН);
                }
                List<string> СписокФирм = await _фирмаRepository.ПолучитьСписокРазрешенныхФирмAsync(doc.Фирма.Id);
                List<Корзина> ТаблицаДилерскойЗаявки = new List<Корзина>();
                Dictionary<string, List<ДанныеТабличнойЧасти>> ПереченьНаличия = new Dictionary<string, List<ДанныеТабличнойЧасти>>();
                List<ДанныеТабличнойЧасти> СпросТаблица = new List<ДанныеТабличнойЧасти>();
                DateTime dateReg = Common.GetRegTA(_context);
                using (var docTran = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var СпросОстатки = _context.Rg12815s.Where(x => x.Period == dateReg &&
                            x.Sp12812 == doc.Контрагент.Id &&
                            x.Sp12813 == doc.Склад.Id &&
                            x.Sp12818 == doc.Фирма.Id)
                            .DefaultIfEmpty()
                            .ToList();

                        _1sjourn j = await _context._1sjourns.FirstOrDefaultAsync(x => x.Iddoc == doc.DocId);
                        j.Closed = 1;
                        j.Actcnt = (СпросОстатки != null ? СпросОстатки.Count() : 0);
                        j.Rf12815 = СпросОстатки != null; //СпросОстатки
                        _context.Update(j);
                        await _context.SaveChangesAsync();
                        int КоличествоДвижений = 0;
                        bool Приход = false;
                        DateTime docDate = DateTime.ParseExact(j.DateTimeIddoc.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture);

                        if (СпросОстатки != null)
                            foreach (var r in СпросОстатки)
                            {
                                if (r != null)
                                {
                                    КоличествоДвижений++;
                                    await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA12815_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                        "@Номенклатура,@Покупатель,@Склад,@Фирма,@Количество," +
                                        "@docDate,@CurPeriod,1,0",
                                        new SqlParameter("@num36", j.Iddoc),
                                        new SqlParameter("@ActNo", КоличествоДвижений),
                                        new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                        new SqlParameter("@Номенклатура", r.Sp12811),
                                        new SqlParameter("@Покупатель", r.Sp12812),
                                        new SqlParameter("@Склад", r.Sp12813),
                                        new SqlParameter("@Фирма", r.Sp12818),
                                        new SqlParameter("@Количество", r.Sp12814),
                                        new SqlParameter("@docDate", docDate.ToShortDateString()),
                                        new SqlParameter("@CurPeriod", dateReg.ToShortDateString()));
                                }
                            }
                        //await Common.ОбновитьВремяТА(_context, j.Iddoc, j.DateTimeIddoc);
                        //await Common.ОбновитьПоследовательность(_context, j.DateTimeIddoc);
                        //await _context.SaveChangesAsync();
                        MaxIddoc = j.Iddoc;
                        MaxDateTimeIddoc = j.DateTimeIddoc;

                        IEnumerable<ТаблицаСвободныхОстатков> ТзОстатки = _номенклатураRepository.ПодготовитьСвободныеОстатки(Common.min1cDate, СписокФирм, СписокСкладов, корзина.Select(x => x.Id).ToList()).AsEnumerable();
                        foreach (Корзина item in корзина)
                        {
                            if (СписокТоваровДилера.Any(x => x.Id == item.Id))
                            {
                                ТаблицаДилерскойЗаявки.Add(item);
                            }
                            else
                            {
                                Единицы ОсновнаяЕдиница = await _номенклатураRepository.ОсновнаяЕдиницаAsync(item.Id);
                                decimal ТекОстатокСуммы = item.Сумма;
                                decimal Отпустить = item.Quantity * ОсновнаяЕдиница.Коэффициент;
                                if (Отпустить > 0)
                                {
                                    foreach (string склId in СписокСкладов)
                                    {
                                        decimal СвободныйОстаток = ТзОстатки
                                            .Where(x => x.Склад.Id == склId && x.Номенклатура.Id == item.Id)
                                            .Sum(x => x.СвободныйОстаток);
                                        decimal МожноОтпустить = Math.Min(Отпустить, СвободныйОстаток);
                                        if (МожноОтпустить > 0)
                                        {
                                            decimal ОстатокВОсновныхЕдиницах = МожноОтпустить / ОсновнаяЕдиница.Коэффициент;
                                            decimal ЦелыйОстатокВОсновныхЕдиницах = decimal.Round(ОстатокВОсновныхЕдиницах);
                                            if (ОстатокВОсновныхЕдиницах != ЦелыйОстатокВОсновныхЕдиницах)
                                                МожноОтпустить = ЦелыйОстатокВОсновныхЕдиницах * ОсновнаяЕдиница.Коэффициент;
                                            if (!ПереченьНаличия.ContainsKey(склId))
                                                ПереченьНаличия.Add(склId, new List<ДанныеТабличнойЧасти>());
                                            decimal ДобСумма = ТекОстатокСуммы;
                                            if (МожноОтпустить != Отпустить)
                                                ДобСумма = Math.Round(item.Сумма / (item.Quantity * ОсновнаяЕдиница.Коэффициент) * МожноОтпустить, 2);
                                            ПереченьНаличия[склId].Add(new ДанныеТабличнойЧасти
                                            {
                                                Номенклатура = new Номенклатура
                                                {
                                                    Id = item.Id,
                                                    Наименование = item.Наименование,
                                                    Артикул = item.Артикул,
                                                    Единица = new Единицы { Id = ОсновнаяЕдиница.Id, Коэффициент = ОсновнаяЕдиница.Коэффициент },
                                                },
                                                Количество = МожноОтпустить,
                                                Цена = item.Цена,
                                                Сумма = ДобСумма,
                                            });
                                            ТекОстатокСуммы = ТекОстатокСуммы - ДобСумма;
                                            Отпустить = Отпустить - МожноОтпустить;
                                            if (Отпустить <= 0)
                                                break;
                                        }
                                    }
                                }
                                if (Отпустить > 0)
                                {
                                    СпросТаблица.Add(new ДанныеТабличнойЧасти
                                    {
                                        Номенклатура = new Номенклатура
                                        {
                                            Id = item.Id,
                                            Наименование = item.Наименование,
                                            Артикул = item.Артикул,
                                            Единица = new Единицы { Id = ОсновнаяЕдиница.Id, Коэффициент = ОсновнаяЕдиница.Коэффициент },
                                        },
                                        Количество = Отпустить,
                                        Цена = item.Цена,
                                        Сумма = ТекОстатокСуммы,
                                    });
                                }
                            }
                        }
                        //Аналоги
                        if (СпросТаблица.Count() > 0)
                        {
                            List<Номенклатура> СписокТоваров = new List<Номенклатура>();
                            foreach (Номенклатура н in СпросТаблица.Select(x => x.Номенклатура))
                            {
                                СписокТоваров.AddRange(await _номенклатураRepository.АналогиНоменклатурыAsync(н.Id));
                            }
                            if (СписокТоваров.Count > 0)
                            {
                                ТзОстатки = _номенклатураRepository.ПодготовитьСвободныеОстатки(Common.min1cDate, СписокФирм, СписокСкладов, СписокТоваров.Select(x => x.Id).ToList()).AsEnumerable();
                                if (ТзОстатки.Count() > 0)
                                {
                                    foreach (ДанныеТабличнойЧасти row in СпросТаблица)
                                    {
                                        decimal Отпустить = row.Количество;
                                        List<Номенклатура> аналоги = await _номенклатураRepository.АналогиНоменклатурыAsync(row.Номенклатура.Id);
                                        foreach (Номенклатура н in аналоги)
                                        {
                                            Единицы ОсновнаяЕдиница = await _номенклатураRepository.ОсновнаяЕдиницаAsync(н.Id);
                                            foreach (string склId in СписокСкладов)
                                            {
                                                decimal СвободныйОстаток = ТзОстатки
                                                    .Where(x => x.Склад.Id == склId && x.Номенклатура.Id == н.Id)
                                                    .Sum(x => x.СвободныйОстаток);
                                                if (ПереченьНаличия.ContainsKey(склId))
                                                    СвободныйОстаток = Math.Max(СвободныйОстаток - ПереченьНаличия[склId].Where(x => x.Номенклатура.Id == н.Id).Sum(y => y.Количество), 0);
                                                decimal МожноОтпустить = Math.Min(Отпустить, СвободныйОстаток);
                                                decimal ОстатокВОсновныхЕдиницах = МожноОтпустить / ОсновнаяЕдиница.Коэффициент;
                                                decimal ЦелыйОстатокВОсновныхЕдиницах = decimal.Round(ОстатокВОсновныхЕдиницах);
                                                if (ОстатокВОсновныхЕдиницах != ЦелыйОстатокВОсновныхЕдиницах)
                                                    МожноОтпустить = ЦелыйОстатокВОсновныхЕдиницах * ОсновнаяЕдиница.Коэффициент;
                                                if (МожноОтпустить > 0)
                                                {
                                                    if (!ПереченьНаличия.ContainsKey(склId))
                                                        ПереченьНаличия.Add(склId, new List<ДанныеТабличнойЧасти>());
                                                    ДанныеТабличнойЧасти строка = ПереченьНаличия[склId].FirstOrDefault(x => x.Номенклатура.Id == н.Id);
                                                    if (строка != null)
                                                    {
                                                        строка.Количество += МожноОтпустить;
                                                        строка.Сумма = строка.Цена * строка.Количество / ОсновнаяЕдиница.Коэффициент;
                                                    }
                                                    else
                                                    {
                                                        var НоменклатураЦена = (await _номенклатураRepository.НоменклатураЦенаКлиентаAsync(new List<string> { н.Id }, doc.Договор.Id, doc.СкидКарта.Id, doc.Доставка, (int)doc.ТипДоставки, null)).FirstOrDefault(x => x.Id == н.Id).Цена.Клиента;
                                                        ПереченьНаличия[склId].Add(new ДанныеТабличнойЧасти
                                                        {
                                                            Номенклатура = new Номенклатура
                                                            {
                                                                Id = н.Id,
                                                                Единица = new Единицы { Id = ОсновнаяЕдиница.Id, Коэффициент = ОсновнаяЕдиница.Коэффициент },
                                                            },
                                                            Количество = МожноОтпустить,
                                                            Цена = НоменклатураЦена,
                                                            Сумма = НоменклатураЦена * МожноОтпустить / ОсновнаяЕдиница.Коэффициент,
                                                        });
                                                    }
                                                    row.Количество = row.Количество - МожноОтпустить;
                                                    row.Сумма = row.Цена * row.Количество / ОсновнаяЕдиница.Коэффициент;
                                                    Отпустить = Отпустить - МожноОтпустить;
                                                    if (Отпустить <= 0)
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            СпросТаблица.RemoveAll(x => x.Количество <= 0);
                        }
                        string результат = "";
                        if (ПереченьНаличия.Count > 0)
                        {
                            результат = await СоздатьДокументСчет(doc, ПереченьНаличия, СписокФирм, СписокСкладов);
                            if (!string.IsNullOrEmpty(результат))
                            {
                                docTran.Rollback();
                                message += (!string.IsNullOrEmpty(message) ? Environment.NewLine : "") + результат;
                            }
                        }
                        if (string.IsNullOrEmpty(message) && СпросТаблица.Count > 0)
                        {
                            результат = await СоздатьДокументСпрос(doc, СпросТаблица);
                            if (!string.IsNullOrEmpty(результат))
                            {
                                docTran.Rollback();
                                message += (!string.IsNullOrEmpty(message) ? Environment.NewLine : "") + результат;
                            }
                        }
                        if (string.IsNullOrEmpty(message) && ТаблицаДилерскойЗаявки.Count > 0)
                        {
                            результат = await СоздатьДокументЗаявкаДилера(doc, ФирмаАлко, ТаблицаДилерскойЗаявки);
                            if (!string.IsNullOrEmpty(результат))
                            {
                                docTran.Rollback();
                                message += (!string.IsNullOrEmpty(message) ? Environment.NewLine : "") + результат;
                            }
                        }

                        if (string.IsNullOrEmpty(message))
                        {
                            await Common.ОбновитьВремяТА(_context, MaxIddoc, MaxDateTimeIddoc);
                            await Common.ОбновитьПоследовательность(_context, MaxDateTimeIddoc);
                            await _context.ОбновитьСетевуюАктивность();
                            docTran.Commit();
                            message = "OK";
                        }
                    }
                    catch (SqlException ex)
                    {
                        docTran.Rollback();
                        if (ex.Number == -2)
                            message += Environment.NewLine + "timeout";
                        else
                            message += Environment.NewLine + ex.Message + Environment.NewLine + ex.InnerException;
                    }
                    catch (Exception ex)
                    {
                        docTran.Rollback();
                        message += Environment.NewLine + ex.Message + Environment.NewLine + ex.InnerException;
                    }
                }
            }
            return Ok(message);
        }
        public IActionResult CallChangeCost(string TovarId, string текущееЗначение, string договорId, string типЦен, string картаId, bool доставка, int типДоставки)
        {
            return ViewComponent("ИзменениеЦены", new
            {
                номенклатураId = TovarId,
                текущееЗначение = текущееЗначение,
                договорId = договорId,
                типЦен = типЦен,
                картаId = картаId,
                доставка = доставка,
                типДоставки = типДоставки
            });
        }
        [HttpPost]
        public IActionResult ОбновитьЦенуВыбраннойНоменклатуры(string key, string значение)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(значение))
            {
                var корзина = HttpContext.Session.GetObjectFromJson<List<Корзина>>("мнТабличнаяЧасть");
                if (корзина != null)
                {
                    var item = корзина.FirstOrDefault(x => x.Id == key);
                    if (item != null)
                    {
                        item.Цена = decimal.Parse(значение.Replace('\u00A0', ' '), NumberStyles.AllowCurrencySymbol | NumberStyles.Number);
                        HttpContext.Session.AddOrUpdateObjectAsJson("мнТабличнаяЧасть", item, false);
                    }
                }
            }
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> ПересчитатьСтрокиДокументаAsync(string key, string договорId, string картаId, bool доставка, int типДоставки)
        {
            var корзина = HttpContext.Session.GetObjectFromJson<List<Корзина>>(key);
            if (корзина != null)
            {
                var TovarList = корзина.Select(x => x.Id).ToList();
                var gg = await _номенклатураRepository.НоменклатураЦенаКлиентаAsync(TovarList, договорId, картаId, доставка, типДоставки, null);
                foreach (var item in корзина)
                {
                    item.Цена = (await gg.FirstOrDefaultAsync(x => x.Id == item.Id)).Цена.Клиента;
                    HttpContext.Session.AddOrUpdateObjectAsJson(key, item, false);
                }
            }
            return Ok();
        }
        [HttpPost]
        public IActionResult ОбновитьНомерДокумента(string номерДок, string фирмаId)
        {
            if (!string.IsNullOrEmpty(номерДок))
                Common.UnLockDocNo(_context, "12747", номерДок);
            string docNo = Common.LockDocNo(_context, "12747", 10, фирмаId);
            return Ok(docNo);
        }
        [HttpGet]
        public JsonResult ВыбратьКонтрагента(LookupFilter filter)
        {
            return Json(new КонтрагентLookup(_context) { Filter = filter }.GetData());
        }
        [HttpGet]
        public JsonResult ВыбратьСкидКарту(LookupFilter filter)
        {
            return Json(new СкидКартаLookup(_context) { Filter = filter }.GetData());
        }
        [HttpPost]
        public async Task<IActionResult> СписокДоговоровКонтрагента(string контрагентId, string фирмаId)
        {
            var договоры = await _контрагентRepository.ПолучитьДоговорыКонтрагентаAsync(контрагентId, фирмаId);
            return Ok(new SelectList(договоры, "Id", "Наименование"));
        }
        [HttpPost]
        public async Task<IActionResult> УсловияДоговораКонтрагентаAsync(string договорId, string картаId)
        {
            return PartialView("_IndexИнфоУсловия", await _контрагентRepository.ПолучитьИнфоУсловияAsync(договорId, картаId));
        }
        [HttpPost]
        public async Task<IActionResult> УсловияДоговораГлубинаОтсрочкиAsync(string договорId)
        {
            return Ok(await _контрагентRepository.ПолучитьГлубинуКредитаПоДоговоруAsync(договорId));
        }
        [HttpPost]
        public async Task<IActionResult> ИнфоДолгиКонтрагентаAsync(string контрагентId)
        {
            Долги результат = new Долги();
            if (!string.IsNullOrEmpty(контрагентId))
                результат = (await _контрагентRepository.ДолгиКонтрагентовПросрочкаAsync(null, null, null, контрагентId, false, true, true, false, false)).FirstOrDefault();
            if (результат == null)
                результат = new Долги();
            return PartialView("_IndexИнфоДолги", результат);
        }
    }
}
