using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StinClasses.Models;
using StinWeb.Models.Repository.Документы;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using StinClasses;
using System.Text;

namespace StinWeb.Controllers.Отчеты
{
    public class NomChangesController : Controller
    {
        private StinDbContext _context;
        private readonly ILogger<NomChangesController> _logger;

        public NomChangesController(StinDbContext context, ILogger<NomChangesController> logger)
        {
            _context = context;
            _logger = logger;
        }
        [Authorize]
        public IActionResult Index()
        {
            return View("~/Views/Отчеты/NomChanges.cshtml");
        }
        [HttpGet]
        public IActionResult GetTableData(DateTime startDate)
        {
            if (startDate == DateTime.MinValue)
                startDate = DateTime.Now.Date;

            var dataGr = from hist in _context.VzNomHistories
                       where hist.ChDate >= startDate
                       group hist by hist.NomId into gr
                       select new
                       {
                           nomId = gr.Key,
                           minDate = gr.Min(x => x.ChDate)
                       };
            //var marketGr = (from mu in _context.Sc14152s
            //                join m in _context.Sc14042s on mu.Sp14147 equals m.Id
            //                group new { mu, m } by mu.Parentext into gr
            //                select new
            //                {
            //                    nomId = gr.Key,
            //                    shortNames = gr.Select(x => x.m.Sp14156)
            //                }
            //               ).AsEnumerable()
            //               .Select(x => new
            //               {
            //                   nomId = x.nomId,
            //                   shortNames = string.Join(',', x.shortNames)
            //               }).ToList();
            var data = (from hist in _context.VzNomHistories
                       join h in dataGr on new { nomId = hist.NomId, chDate = hist.ChDate } equals new { nomId = h.nomId, chDate = h.minDate }
                       join nom in _context.Sc84s on hist.NomId equals nom.Id
                       join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                       join brend in _context.Sc8840s on nom.Sp8842 equals brend.Id into _brend
                       from brend in _brend.DefaultIfEmpty()
                       join mu in _context.Sc14152s on hist.NomId equals mu.Parentext
                       join m in _context.Sc14042s on mu.Sp14147 equals m.Id 
                       where (hist.Descr != nom.Descr) ||
                             (hist.Vendor != nom.Sp85) ||
                             (hist.Brend != nom.Sp8842) ||
                             (hist.Params != nom.Sp8848) ||
                             (hist.Quantum != nom.Sp14188) ||
                             (hist.Barcode != ed.Sp80) ||
                             (hist.Weight != ed.Sp76) ||
                             (hist.WeightB != ed.Sp14056) ||
                             (hist.Width != ed.Sp14036) ||
                             (hist.Height != ed.Sp14035) ||
                             (hist.Length != ed.Sp14037) ||
                             (hist.Boxes != ed.Sp14063)
                       orderby m.Parentext, m.Sp14155, m.Sp14164 descending      
                       //group new { hist, nom, ed, brend, m } by new { hist, nom, ed, brend } into gr
                       //{ 
                       //    nomId = hist.NomId, 
                       //    code = nom.Code,
                       //    histVendor = hist.Vendor,
                       //    nomVendor = nom.Sp85,
                       //    histDescr = hist.Descr,
                       //    nomDescr = nom.Descr,
                       //    histBrend = hist.Brend,
                       //    nomBrend = nom.Sp8842,
                       //    nomBrendDescr = brend.Descr,
                       //    histParams = hist.Params,
                       //    nomParams = nom.Sp8848,
                       //    histBarcode = hist.Barcode,
                       //    nomBarcode = ed.Sp80,
                       //    histWeight = hist.Weight,
                       //    nomWeight = ed.Sp76,
                       //    histWeightB = hist.WeightB,
                       //    nomWeightB = ed.Sp14056,
                       //    histWidth = hist.Width,
                       //    nomWidth = ed.Sp14036,
                       //    histHeight = hist.Height,
                       //    nomHeight = ed.Sp14035,
                       //    histLength = hist.Length,
                       //    nomLength = ed.Sp14037,
                       //    histBoxies = hist.Boxes,
                       //    nomBoxies = ed.Sp14063,
                       //    histQuantum = hist.Quantum,
                       //    nomQuantum = nom.Sp14188
                       //} into gr
                       select new //HistInfo
                       {
                           NomId = hist.NomId,
                           Sku = nom.Code,
                           OldDescription = hist.Descr.Trim(),
                           Vendor = hist.Vendor != nom.Sp85 ? "Артикул: <b>" + nom.Sp85.Trim() + "</b>" : "",
                           Description = hist.Descr != nom.Descr ? "Наименование: <b>" + nom.Descr.Trim() + "</b>" : "",
                           Brend = hist.Brend != nom.Sp8842 ? "Производитель: <b>" + brend.Descr.Trim() + "</b>" : "",
                           Params = hist.Params != nom.Sp8848 ? "Характеристики: <b>" + nom.Sp8848.Trim() + "</b>" : "",
                           Barcode = hist.Barcode != ed.Sp80 ? "Штрихкод: <b>" + ed.Sp80.Trim() + "</b>" : "",
                           Weight = hist.Weight != ed.Sp76 ? "Вес (нетто): <b>" + ed.Sp76.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "",
                           WeightB = hist.WeightB != ed.Sp14056 ? "Вес (брутто): <b>" + ed.Sp14056.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "",
                           Width = hist.Width != ed.Sp14036 ? "Ширина: <b>" + ed.Sp14036.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "",
                           Height = hist.Height != ed.Sp14035 ? "Высота: <b>" + ed.Sp14035.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "",
                           Length = hist.Length != ed.Sp14037 ? "Длина: <b>" + ed.Sp14037.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "",
                           Boxies = hist.Boxes != ed.Sp14063 ? "Кол-во мест: <b>" + ed.Sp14063.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "",
                           Quantum = hist.Quantum != nom.Sp14188 ? "Квант: <b>" + nom.Sp14188.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "",
                           //Info = ""
                           // .ConditionallyAppend((hist.Descr != nom.Descr) ? "Наименование: <b>" + nom.Descr.Trim() + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Vendor != nom.Sp85) ? "Артикул: <b>" + nom.Sp85.Trim() + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Brend != nom.Sp8842) ? "Производитель: <b>" + brend.Descr.Trim() + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Params != nom.Sp8848) ? "Характеристики: <b>" + nom.Sp8848.Trim() + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Barcode != ed.Sp80) ? "Штрихкод: <b>" + ed.Sp80.Trim() + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Weight != ed.Sp76) ? "Вес (нетто): <b>" + ed.Sp76.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.WeightB != ed.Sp14056) ? "Вес (брутто): <b>" + ed.Sp14056.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Width != ed.Sp14036) ? "Ширина: <b>" + ed.Sp14036.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Height != ed.Sp14035) ? "Высота: <b>" + ed.Sp14035.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Length != ed.Sp14037) ? "Длина: <b>" + ed.Sp14037.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Boxes != ed.Sp14063) ? "Кол-во мест: <b>" + ed.Sp14063.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "", "<br/>")
                           // .ConditionallyAppend((hist.Quantum != nom.Sp14188) ? "Квант: <b>" + nom.Sp14188.ToString(StinWeb.Models.DataManager.Common.ФорматКоличестваСи) + "</b>" : "", "<br/>"),
                           marketName = m.Sp14156
                       }).AsEnumerable();
            var result = data
                .GroupBy(x => new 
                { 
                    x.NomId, 
                    x.Sku, 
                    x.OldDescription,
                    x.Vendor, 
                    x.Description, 
                    x.Brend, 
                    x.Params, 
                    x.Barcode, 
                    x.Weight, 
                    x.WeightB,
                    x.Width,
                    x.Height,
                    x.Length,
                    x.Boxies,
                    x.Quantum
                })
                .Select(gr => new HistInfo 
                {
                    NomId = gr.Key.NomId,
                    Sku = gr.Key.Sku,
                    Description = gr.Key.OldDescription,
                    Info = FormInfoString(new[] 
                    { 
                        gr.Key.Vendor, 
                        gr.Key.Description, 
                        gr.Key.Brend, 
                        gr.Key.Params, 
                        gr.Key.Barcode, 
                        gr.Key.Weight,
                        gr.Key.WeightB,
                        gr.Key.Width,
                        gr.Key.Height,
                        gr.Key.Length,
                        gr.Key.Boxies,
                        gr.Key.Quantum
                    }, "<br/>"),
                    MarketNames = String.Join("<br/>", gr.Select(s => s.marketName))
                })
                .AsQueryable();
            //var result0 = (from mu in _context.Sc14152s
            //               join m in _context.Sc14042s on mu.Sp14147 equals m.Id
            //               join d in data on mu.Parentext equals d.NomId
            //               group new { d, m } by d into g
            //               select new
            //               {
            //                   g.Key.NomId,
            //                   g.Key.Sku,
            //                   //SkuCoded = g.Key.Sku.EncodeHexString() == null ? g.Key.Sku : "",
            //                   g.Key.Vendor,
            //                   g.Key.Description,
            //                   g.Key.Brend,
            //                   g.Key.Params,
            //                   g.Key.Barcode,
            //                   g.Key.Weight,
            //                   g.Key.WeightB,
            //                   g.Key.Width,
            //                   g.Key.Height,
            //                   g.Key.Length,
            //                   g.Key.Boxies,
            //                   g.Key.Quantum,
            //                   marketNames = g.Select(x => x.m.Sp14156)
            //               })
            //             .AsEnumerable();
            ////var result = result0.Select(x => new HistInfo
            ////{
            ////    NomId = x.NomId,
            ////    Sku = x.Sku,
            ////    SkuCoded = x.Sku.EncodeHexString(),
            ////    Info = "",
            ////    //MarketNames = string.Join(',', x.marketNames)
            ////}).AsQueryable();
            //_logger.LogError(t[0].Sku);
            //if (!data.Any())
            //    data = Enumerable.Empty<HistInfo>().AsQueryable();
            return PartialView("~/Views/Shared/Components/Номенклатура/HistNomenklatura.cshtml", result);
        }
        string FormInfoString(string[] strArray, string separator)
        { 
            if ((strArray == null) || (strArray.Length == 0))
                return string.Empty;
            var sb = new StringBuilder();
            foreach (var s in strArray.Where(x => x != String.Empty))
            {
                if (sb.Length > 0)
                    sb.Append(separator);
                sb.Append(s);
            }
            return sb.ToString();
        }
    }
    public class HistInfo
    {
        public string NomId { get; set; }
        public string Sku { get; set; }
        public string SkuCoded { get { return this.Sku.EncodeHexString(); } }
        public string SkuCoded2 { get { return this.Sku.EncodeDecString(); } }
        public string Vendor { get; set; }
        public string Description { get; set; }
        public string Brend { get; set; }
        public string Params { get; set; }
        public string Barcode { get; set; }
        public decimal Weight { get; set; }
        public decimal WeightB { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Length { get; set; }
        public decimal Boxies { get; set; }
        public decimal Quantum { get; set; }
        public string Info { get; set; }
        public string MarketNames { get; set; }
    }
}
