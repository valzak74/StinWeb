using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExcelHelper;
using NonFactors.Mvc.Lookup;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using StinClasses.Models;
using StinWeb.Lookups;
using StinWeb.Models.DataManager.Справочники;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StinClasses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace StinWeb.Controllers.Обработки
{
    public class MarketplaceMatrixController : Controller
    {
        private StinDbContext _context;
        private readonly ILogger<MarketplaceMatrixController> _logger;

        public MarketplaceMatrixController(StinDbContext context, ILogger<MarketplaceMatrixController> logger)
        {
            _context = context;
            _logger = logger;
        }
        [Authorize]
        public IActionResult Index()
        {
            return View("~/Views/Обработки/MarketplaceMatrix.cshtml");
        }
        [HttpGet]
        public JsonResult Firma(LookupFilter filter)
        {
            IQueryable<Фирма> query = (from m in _context.Sc14042s
                                       join f in _context.Sc4014s on m.Parentext equals f.Id
                                       join своиЮрЛица in _context.Sc131s on f.Sp4011 equals своиЮрЛица.Id
                                       where !m.Ismark
                                       select new Фирма { Id = f.Id, Наименование = своиЮрЛица.Descr.Trim() }).Distinct();
            var lookup = new TemplateQueryLookup<Фирма>(_context, query, null, filter);
            return Json(lookup.GetData());
        }
        [HttpGet]
        public JsonResult Campaign(LookupFilter filter, string selectedFirma)
        {
            IQueryable<Campaign> query = null;
            if (!string.IsNullOrEmpty(selectedFirma))
            {
                query = _context.Sc14042s.Where(x => !x.Ismark && (x.Parentext == selectedFirma)).Select(x => new Campaign { Id = x.Id, Наименование = x.Descr.Trim(), Тип = x.Sp14155.Trim() });
            }
            else
                query = _context.Sc14042s.Where(x => !x.Ismark).Select(x => new Campaign { Id = x.Id, Наименование = x.Descr.Trim(), Тип = x.Sp14155.Trim() });
            
            var отображение = new List<string> { "Тип", "Наименование" };
            var lookup = new TemplateQueryLookup<Campaign>(_context, query, отображение, filter);
            return Json(lookup.GetData());
        }
        [HttpPost]
        public async Task<IActionResult> ExportDataAsync(string firmaId, string marketId, DateTime startDate, DateTime endDate, bool showDeleted, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(marketId))
                return StatusCode(502, "Недопустимое значение campaignId");
            
            string extension = "xlsx";
            IWorkbook workbook;
            IFormulaEvaluator formula;
            if (extension == "xls")
            {
                workbook = new HSSFWorkbook();
                formula = new HSSFFormulaEvaluator(workbook);
            }
            else
            {
                workbook = new XSSFWorkbook();
                formula = new XSSFFormulaEvaluator(workbook);
            }

            IFont fontArial8 = workbook.FontArial();
            IFont fontArialBold8 = workbook.FontArial(8, true);
            IFont fontArialBoldRed8 = workbook.FontArial(8, true, HSSFColor.Red.Index);

            ICellStyle styleHeader = workbook.StyleHeader(fontArialBold8);
            ICellStyle styleValue = workbook.StyleValue(fontArial8);
            ICellStyle styleValueMark = workbook.StyleValueMarked(fontArial8, 50);
            ICellStyle styleValueMarkDeleted = workbook.StyleValueMarked(fontArial8, 46);
            ICellStyle styleValueRed = workbook.StyleValue(fontArialBoldRed8);
            styleValueRed.Alignment = HorizontalAlignment.Center;
            ICellStyle styleValueRedMark = workbook.StyleValueMarked(fontArialBoldRed8, 50);
            styleValueRedMark.Alignment = HorizontalAlignment.Center;
            ICellStyle styleValueRedMarkDeleted = workbook.StyleValueMarked(fontArialBoldRed8, 46);
            styleValueRedMarkDeleted.Alignment = HorizontalAlignment.Center;
            ICellStyle styleValueCenter = workbook.StyleValue(fontArial8);
            styleValueCenter.Alignment = HorizontalAlignment.Center;
            ICellStyle styleValueCenterMark = workbook.StyleValueMarked(fontArial8, 50);
            styleValueCenterMark.Alignment = HorizontalAlignment.Center;
            ICellStyle styleValueCenterMarkDeleted = workbook.StyleValueMarked(fontArial8, 46);
            styleValueCenterMarkDeleted.Alignment = HorizontalAlignment.Center;
            ICellStyle styleValueNum = workbook.StyleValueNumber(fontArial8);
            ICellStyle styleValueNumMark = workbook.StyleValueNumberMarked(fontArial8, 50);
            ICellStyle styleValueNumMarkDeleted = workbook.StyleValueNumberMarked(fontArial8, 46);
            ICellStyle styleValueNumRed = workbook.StyleValueNumber(fontArialBoldRed8);
            ICellStyle styleValueNumRedMark = workbook.StyleValueNumberMarked(fontArialBoldRed8, 50);
            ICellStyle styleValueNumRedMarkDeleted = workbook.StyleValueNumberMarked(fontArialBoldRed8, 46);
            ICellStyle styleValueMoney = workbook.StyleValueNumberMoney(fontArial8);

            ICellStyle styleValueNum1tail = workbook.StyleValueNumber(fontArial8, 1);
            ICellStyle styleValueNum0tail = workbook.StyleValueNumber(fontArial8, 0);

            ICellStyle styleFrozenPrice = workbook.StyleValueNumber(fontArial8, 0, 13);
            ICellStyle styleDeltaPrice = workbook.StyleValueNumber(fontArial8, 0, 49);

            ISheet sheet = workbook.CreateSheet("Лист 1");

            var marketplaceData = await _context.Sc14042s
                .Where(x => !x.Ismark && (string.IsNullOrEmpty(firmaId) ? true : (x.Parentext == firmaId)))
                .Select(x => new
                {
                    Id = x.Id,
                    FirmaId = x.Parentext,
                    CampaignId = x.Code.Trim(),
                    Type = x.Sp14155.Trim(),
                    Model = x.Sp14164.ToUpper().Trim(),
                    Наименование = x.Descr.Trim(),
                    ShortName = x.Sp14156.Trim(),
                    Encoding = (EncodeVersion)x.Sp14153,
                    DefMultiplyer = x.Sp14165,
                    КонтрагентId = x.Sp14175
                })
                //.OrderBy(x => x.Type).ThenByDescending(x => x.Model)
                .ToListAsync(cancellationToken);
            var campaignData = marketplaceData.Where(x => x.Id == marketId).FirstOrDefault();
            marketplaceData = marketplaceData
                //.Where(x => x.FirmaId == campaignData.FirmaId)
                .OrderBy(x => (x.Id == campaignData.Id) ? 0 : 1)
                .ThenBy(x => x.FirmaId)
                .ThenBy(x => (x.Type == campaignData.Type) ? 0 : 1)
                .ThenBy(x => x.Type)
                .ThenByDescending(x => x.Model)
                .ToList();
            var skladData = await _context.Sc55s
                .Where(x => x.Sp14180 > 0)
                .OrderBy(x => x.Sp14180)
                .Select(x => new { x.Id, x.Code, Название = x.Descr.Trim() })
                .ToListAsync(cancellationToken);


            Dictionary<string, int> columnValues = new Dictionary<string, int>();
            int column = 0;
            columnValues.Add("Id", sheet.CreateColumnWithWidth(column++, 0));
            columnValues.Add("Sku_1", sheet.CreateColumnWithWidth(column++,4500));
            columnValues.Add("Sku_2", sheet.CreateColumnWithWidth(column++, 4500));
            columnValues.Add("Sku_3", sheet.CreateColumnWithWidth(column++, 5000));
            columnValues.Add("Адрес_группы", sheet.CreateColumnWithWidth(column++, 4300));
            columnValues.Add("Артикул", sheet.CreateColumnWithWidth(column++, 4300));
            columnValues.Add("Наименование", sheet.CreateColumnWithWidth(column++, 8000));
            columnValues.Add("Бренд", sheet.CreateColumnWithWidth(column++, 4000));
            columnValues.Add("Продано", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("ОжПриход", sheet.CreateColumnWithWidth(column++, 2900));
            foreach (var skl in skladData)
            {
                columnValues.Add("Остаток" + skl.Code, sheet.CreateColumnWithWidth(column++, 2900));
            }
            columnValues.Add("Себестоимость", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("Закупочная", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("Розничная", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("РозничнаяСП", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("ЦенаПродажи", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("ЦП_ЗЦ", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("МинЦена", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("МинЦенаКвант", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("ЦП_МЦ", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("ЦПизОБ_МЦ", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("КоррЦены", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("ЗаморозкаЦен", sheet.CreateColumnWithWidth(column++, 2900));
            foreach (var marketplace in marketplaceData)
            {
                columnValues.Add(marketplace.ShortName, sheet.CreateColumnWithWidth(column++, 2900));
            }
            columnValues.Add("Квант", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("КоррОстатков", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("КраткоеОписание", sheet.CreateColumnWithWidth(column++, 9000));
            columnValues.Add("Страна", sheet.CreateColumnWithWidth(column++, 3500));
            columnValues.Add("Категория", sheet.CreateColumnWithWidth(column++, 8000));
            columnValues.Add("Штрихкод", sheet.CreateColumnWithWidth(column++, 4300));
            columnValues.Add("Длина", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("Ширина", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("Высота", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("ГабаритыСумма", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("Габариты", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("Брутто", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("КолМест", sheet.CreateColumnWithWidth(column++, 2900));

            sheet.SetColumnHidden(columnValues["Id"], true);
            int row = 0;

            sheet.SetValue(styleHeader, row, columnValues["Id"], "_Id_");
            sheet.SetValue(styleHeader, row, columnValues["Sku_1"], "SKU 1");
            sheet.SetValue(styleHeader, row, columnValues["Sku_2"], "SKU 2");
            sheet.SetValue(styleHeader, row, columnValues["Sku_3"], "SKU 3");
            sheet.SetValue(styleHeader, row, columnValues["Адрес_группы"], "Адрес группы");
            sheet.SetValue(styleHeader, row, columnValues["Артикул"], "Артикул");
            sheet.SetValue(styleHeader, row, columnValues["Наименование"], "Наименование");
            sheet.SetValue(styleHeader, row, columnValues["Бренд"], "Бренд");
            sheet.SetValue(styleHeader, row, columnValues["Продано"], "Продано " + Environment.NewLine + startDate.ToString("dd-MM-yy") + Environment.NewLine + endDate.ToString("dd-MM-yy"));
            sheet.SetValue(styleHeader, row, columnValues["ОжПриход"], "Ожид. приход");
            foreach (var skl in skladData)
            {
                sheet.SetValue(styleHeader, row, columnValues["Остаток" + skl.Code], "Остаток " + skl.Название);
            }
            sheet.SetValue(styleHeader, row, columnValues["Себестоимость"], "Себестоимость");
            sheet.SetValue(styleHeader, row, columnValues["Закупочная"], "Закупочная цена");
            sheet.SetValue(styleHeader, row, columnValues["Розничная"], "Розничная");
            sheet.SetValue(styleHeader, row, columnValues["РозничнаяСП"], "Розничная спец");
            sheet.SetValue(styleHeader, row, columnValues["ЦенаПродажи"], "Цена продажи");
            sheet.SetValue(styleHeader, row, columnValues["ЦП_ЗЦ"], "ЦП/ЗЦ");
            sheet.SetValue(styleHeader, row, columnValues["МинЦена"], "Мин. цена");
            sheet.SetValue(styleHeader, row, columnValues["МинЦенаКвант"], "Мин. цена кванта");
            sheet.SetValue(styleHeader, row, columnValues["ЦП_МЦ"], "ЦП/МЦ");
            sheet.SetValue(styleHeader, row, columnValues["ЦПизОБ_МЦ"], "ЦП из ОБ/МЦ");
            sheet.SetValue(styleHeader, row, columnValues["КоррЦены"], "Корр. Цен, %");
            sheet.SetValue(styleHeader, row, columnValues["ЗаморозкаЦен"], "Цена не обновляется");
            foreach (var marketplace in marketplaceData)
            {
                sheet.SetValue(styleHeader, row, columnValues[marketplace.ShortName], marketplace.ShortName + " " + marketplace.DefMultiplyer.ToString() );
            }
            sheet.SetValue(styleHeader, row, columnValues["Квант"], "Квант");
            sheet.SetValue(styleHeader, row, columnValues["КоррОстатков"], "Корр. остатков");
            sheet.SetValue(styleHeader, row, columnValues["КраткоеОписание"], "Краткое описание");
            sheet.SetValue(styleHeader, row, columnValues["Страна"], "Страна происхождения");
            sheet.SetValue(styleHeader, row, columnValues["Категория"], "Категория конечная");
            sheet.SetValue(styleHeader, row, columnValues["Штрихкод"], "Штрихкод");
            sheet.SetValue(styleHeader, row, columnValues["Длина"], "Длина, см.");
            sheet.SetValue(styleHeader, row, columnValues["Ширина"], "Ширина, см.");
            sheet.SetValue(styleHeader, row, columnValues["Высота"], "Высота, см.");
            sheet.SetValue(styleHeader, row, columnValues["ГабаритыСумма"], "Сумма габаритов, см.");
            sheet.SetValue(styleHeader, row, columnValues["Габариты"], "Длина/ Ширина/ Высота");
            sheet.SetValue(styleHeader, row, columnValues["Брутто"], "Вес брутто, кг.");
            sheet.SetValue(styleHeader, row, columnValues["КолМест"], "Кол-во мест");

            var nativeData = from marketUsing in _context.Sc14152s
                             join market in _context.Sc14042s on marketUsing.Sp14147 equals market.Id
                             join nom in _context.Sc84s on marketUsing.Parentext equals nom.Id
                             join nomParent in _context.Sc84s on nom.Parentid equals nomParent.Id
                             join sc75 in _context.Sc75s on nom.Sp94 equals sc75.Id
                             join parent in _context.Sc84s on nom.Parentid equals parent.Id into _parent
                             from parent in _parent.DefaultIfEmpty()
                             join sc8840 in _context.Sc8840s on nom.Sp8842 equals sc8840.Id into _sc8840
                             from sc8840 in _sc8840.DefaultIfEmpty()
                             join vzTovar in _context.VzTovars on nom.Id equals vzTovar.Id into _vzTovar
                             from vzTovar in _vzTovar.DefaultIfEmpty()
                             where (market.Id == marketId) && (showDeleted ? true : !marketUsing.Ismark)
                             select new
                             {
                                 Id = marketUsing.Id,
                                 Deleted = marketUsing.Ismark,
                                 MarketId = market.Id,
                                 ParentComment = !string.IsNullOrEmpty(nomParent.Sp95) ? nomParent.Sp95 : string.Empty,
                                 NomId = nom.Id,
                                 NomCode = nom.Code,
                                 Артикул = nom.Sp85.Trim(),
                                 Наименование = nom.Descr.Trim(),
                                 Бренд = sc8840 != null ? sc8840.Descr.Trim() : "<Не указан>",
                                 ЦенаРозн = vzTovar != null ? vzTovar.Rozn ?? 0 : 0,
                                 ЦенаСп = vzTovar != null ? vzTovar.RoznSp ?? 0 : 0,
                                 ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                                 МинЦена = marketUsing.Sp14198,
                                 Квант = nom.Sp14188,
                                 DeltaStock = nom.Sp14215, //marketUsing.Sp14214,
                                 DeltaPrice = marketUsing.Sp14213,
                                 FrozenPrice = marketUsing.Sp14323,
                                 КраткоеОписание = nom.Sp12309.Trim(),
                                 Характеристики = nom.Sp8848.Trim(),
                                 Категория = parent != null ? parent.Descr.Trim() : "",
                                 Штрихкод = sc75.Sp80.Trim(),
                                 Длина = sc75.Sp14037 * 100,
                                 Ширина = sc75.Sp14036 * 100,
                                 Высота = sc75.Sp14035 * 100,
                                 Брутто = sc75.Sp14056,
                                 КолМест = sc75.Sp14063
                             };
            var usedMarketIds = marketplaceData.Select(x => x.Id).ToList();
            var otherData = await (from marketUsing in _context.Sc14152s
                            join market in _context.Sc14042s on marketUsing.Sp14147 equals market.Id
                            join nom in _context.Sc84s on marketUsing.Parentext equals nom.Id
                            where usedMarketIds.Contains(market.Id)
                            select new
                            {
                                MarketId = market.Id,
                                Deleted = marketUsing.Ismark,
                                NomId = nom.Id,
                                ЦенаФикс = marketUsing.Sp14148,
                                Коэф = marketUsing.Sp14149,
                                ЕстьВКаталоге = marketUsing.Sp14158 == 1,
                            }).ToListAsync(cancellationToken);
            var nomIds = nativeData.Select(x => x.NomId).Distinct().ToList();
            using var фирма = new StinClasses.Справочники.ФирмаEntity(_context);
            var разрешенныеФирмы = await фирма.ПолучитьСписокРазрешенныхФирмAsync(firmaId);
            DateTime dateReg = StinWeb.Models.DataManager.Common.GetRegTA(_context);
            var себестоимостьData = await (from rg328 in _context.Rg328s
                                           join sc84 in _context.Sc84s on rg328.Sp331 equals sc84.Id
                                           join sc75 in _context.Sc75s on sc84.Sp94 equals sc75.Id
                                           where rg328.Period == dateReg
                                              && nomIds.Contains(rg328.Sp331)
                                              && разрешенныеФирмы.Contains(rg328.Sp4061)
                                           group new { rg328, sc75 } by new { nomId = rg328.Sp331, koef = sc75.Sp78 } into g
                                           select new
                                           {
                                               НоменклатураId = g.Key.nomId,
                                               Себестоимость = (decimal)((g.Sum(x => x.rg328.Sp342) != 0 ? g.Sum(x => x.rg328.Sp421) / g.Sum(x => x.rg328.Sp342) : 0) * (g.Key.koef == 0 ? 1 : g.Key.koef))
                                           }).ToListAsync(cancellationToken);
            using var номенклатура = new StinClasses.Справочники.НоменклатураEntity(_context);
            var nomData = await номенклатура.ПолучитьСвободныеОстатки(
                разрешенныеФирмы, 
                skladData.Select(x => x.Id).ToList(),
                nomIds);
            using var регистрЗаказы = new StinClasses.Регистры.Регистр_Заказы(_context);
            var nomЗаказы = await регистрЗаказы.ПолучитьОстаткиAsync(
                DateTime.Now,
                null,
                false,
                null,
                nomIds);

            long видКомлексПродажа = (long)StinClasses.Документы.ВидДокумента.КомплекснаяПродажа;
            string основаниеВидКомлексПродажа = "O1" + Common.Encode36(видКомлексПродажа).PadLeft(4);
            var реализовано = Enumerable.Repeat(new
            {
                НоменклатураId = "",
                Количество = 0.000m,
            }, 0).ToList();
            if (campaignData.Model == "FBY")
                реализовано = await (from dh1611 in _context.Dh1611s
                                     join j in _context._1sjourns on dh1611.Iddoc equals j.Iddoc
                                     join dt1611 in _context.Dt1611s on dh1611.Iddoc equals dt1611.Iddoc
                                     where (j.Closed == 1) &&
                                       (string.Compare(j.DateTimeIddoc.Substring(0, 8), startDate.ToString("yyyyMMdd")) >= 0) &&
                                       (string.Compare(endDate.ToString("yyyyMMdd"), j.DateTimeIddoc.Substring(0, 8)) >= 0) &&
                                       (dh1611.Sp1583 == campaignData.КонтрагентId) && 
                                       nomIds.Contains(dt1611.Sp1599)
                                     select new
                                     {
                                         НоменклатураId = dt1611.Sp1599,
                                         Количество = dt1611.Sp1600
                                     })
                                     .ToListAsync(cancellationToken);
            else
                реализовано = await (from dh1611 in _context.Dh1611s
                                     join j in _context._1sjourns on dh1611.Iddoc equals j.Iddoc
                                     join dt1611 in _context.Dt1611s on dh1611.Iddoc equals dt1611.Iddoc
                                     join crDoc in _context._1scrdocs on j.Iddoc equals crDoc.Childid
                                     join dh12542 in _context.Dh12542s on crDoc.Parentval.Substring(6, 9) equals dh12542.Iddoc
                                     join sc13994 in _context.Sc13994s on dh12542.Sp14005 equals sc13994.Id
                                     //join sc14042 in _context.Sc14042s on sc13994.Sp14038 equals sc14042.Id
                                     where (j.Closed == 1) &&
                                       (string.Compare(j.DateTimeIddoc.Substring(0, 8), startDate.ToString("yyyyMMdd")) >= 0) &&
                                       (string.Compare(endDate.ToString("yyyyMMdd"), j.DateTimeIddoc.Substring(0, 8)) >= 0) &&
                                       (crDoc.Mdid == 0) && (crDoc.Parentval.Substring(0, 6) == основаниеВидКомлексПродажа) &&
                                       (sc13994.Sp14038 == marketId) &&
                                       nomIds.Contains(dt1611.Sp1599)
                                     select new
                                     {
                                         НоменклатураId = dt1611.Sp1599,
                                         Количество = dt1611.Sp1600
                                     })
                                     .ToListAsync(cancellationToken);

            foreach (var item in await nativeData.Where(x => x.MarketId == marketId).OrderBy(x => x.Бренд).ThenBy(x => x.Наименование).ToListAsync(cancellationToken))
            {
                row++;
                sheet.SetValue(styleValue, row, columnValues["Id"], item.Id.Replace(" ","_"));
                sheet.SetValue(styleValue, row, columnValues["Sku_1"], item.NomCode.Encode(EncodeVersion.None));
                sheet.SetValue(styleValue, row, columnValues["Sku_2"], item.NomCode.Encode(EncodeVersion.Hex));
                sheet.SetValue(styleValue, row, columnValues["Sku_3"], item.NomCode.Encode(EncodeVersion.Dec));
                sheet.SetValue(styleValue, row, columnValues["Адрес_группы"], item.ParentComment);
                sheet.SetValue(styleValue, row, columnValues["Артикул"], item.Артикул);
                sheet.SetValue(styleValue, row, columnValues["Наименование"], item.Наименование);
                sheet.SetValue(styleValue, row, columnValues["Бренд"], item.Бренд);
                decimal продано = реализовано
                    .Where(x => x.НоменклатураId == item.NomId)
                    .Sum(x => x.Количество);
                sheet.SetValue(styleValueNum, row, columnValues["Продано"], продано);
                decimal ожПриход = nomЗаказы
                    .Where(x => (x.НоменклатураId == item.NomId) && (x.ТипЗаказа != 2))
                    .Sum(x => x.Количество);
                sheet.SetValue(styleValueNum, row, columnValues["ОжПриход"], ожПриход); 
                foreach (var skl in skladData)
                {
                    decimal остаток = nomData
                        .Where(x => x.Id == item.NomId)
                        .Sum(x => x.Остатки.Where(s => s.СкладId == skl.Id).Sum(y => y.СвободныйОстаток) / x.Единица.Коэффициент);
                    sheet.SetValue(styleValueNum, row, columnValues["Остаток" + skl.Code], остаток);
                }
                decimal себестоимость = себестоимостьData
                    .Where(x => x.НоменклатураId == item.NomId)
                    .Sum(x => x.Себестоимость);
                sheet.SetValue(styleValueMoney, row, columnValues["Себестоимость"], себестоимость);
                sheet.SetValue(styleValueMoney, row, columnValues["Закупочная"], item.ЦенаЗакуп);
                sheet.SetValue(styleValueMoney, row, columnValues["Розничная"], item.ЦенаРозн);
                sheet.SetValue(styleValueMoney, row, columnValues["РозничнаяСП"], item.ЦенаСп);
                var ценаПродажиОБ = item.ЦенаРозн;
                //var ценаПродажиОБ = item.ЦенаСп > 0 ? Math.Min(item.ЦенаРозн, item.ЦенаСп) : item.ЦенаРозн;
                var ценаПродажи = item.МинЦена > 0 ? Math.Max(item.МинЦена, ценаПродажиОБ) : ценаПродажиОБ;
                sheet.SetValue(styleValueMoney, row, columnValues["ЦенаПродажи"], ценаПродажи);
                sheet.SetValue(styleValueNum, row, columnValues["ЦП_ЗЦ"], item.ЦенаЗакуп == 0 ? 0 : ценаПродажи / item.ЦенаЗакуп);
                sheet.SetValue(styleValueMoney, row, columnValues["МинЦена"], item.МинЦена);
                sheet.SetValue(styleValueMoney, row, columnValues["МинЦенаКвант"], item.МинЦена * (item.Квант == 0 ? 1 : item.Квант));
                sheet.SetValue(ценаПродажи < item.МинЦена ? styleValueNumRed : styleValueNum, row, columnValues["ЦП_МЦ"], item.МинЦена == 0 ? 0 : ценаПродажи / item.МинЦена);
                sheet.SetValue(ценаПродажиОБ < item.МинЦена ? styleValueNumRed : styleValueNum, row, columnValues["ЦПизОБ_МЦ"], item.МинЦена == 0 ? 0 : ценаПродажиОБ / item.МинЦена);
                sheet.SetValue(styleDeltaPrice, row, columnValues["КоррЦены"], item.DeltaPrice);
                sheet.SetValue(styleFrozenPrice, row, columnValues["ЗаморозкаЦен"], item.FrozenPrice);
                bool marked = false;
                foreach (var marketplace in marketplaceData)
                {
                    var m_data = otherData.Where(x => (x.MarketId == marketplace.Id) && (x.NomId == item.NomId)).FirstOrDefault();
                    if (m_data != null)
                    {
                        if (m_data.ЦенаФикс > 0)
                        {
                            if (m_data.ЦенаФикс >= ценаПродажи)
                            {
                                if (!m_data.ЕстьВКаталоге)
                                    sheet.SetValue(m_data.Deleted ? styleValueCenterMarkDeleted : (marked ? styleValueCenter : styleValueCenterMark), row, columnValues[marketplace.ShortName], "!!! " + m_data.ЦенаФикс.ToString("# ##0.000;-# ##0.000;;@"));
                                else
                                    sheet.SetValue(m_data.Deleted ? styleValueNumMarkDeleted : (marked ? styleValueNum : styleValueNumMark), row, columnValues[marketplace.ShortName], m_data.ЦенаФикс);
                            }
                            else
                            {
                                var Порог = item.ЦенаЗакуп * (m_data.Коэф > 0 ? m_data.Коэф : marketplace.DefMultiplyer);
                                if (Порог > m_data.ЦенаФикс)
                                {
                                    if (!m_data.ЕстьВКаталоге)
                                        sheet.SetValue(m_data.Deleted ? styleValueRedMarkDeleted : (marked ? styleValueRed : styleValueRedMark), row, columnValues[marketplace.ShortName], "!!! " + m_data.ЦенаФикс.ToString("# ##0.000;-# ##0.000;;@"));
                                    else
                                        sheet.SetValue(m_data.Deleted ? styleValueNumRedMarkDeleted : (marked ? styleValueNumRed : styleValueNumRedMark), row, columnValues[marketplace.ShortName], m_data.ЦенаФикс);
                                }
                                else
                                {
                                    if (!m_data.ЕстьВКаталоге)
                                        sheet.SetValue(m_data.Deleted ? styleValueCenterMarkDeleted : (marked ? styleValueCenter : styleValueCenterMark), row, columnValues[marketplace.ShortName], "!!! " + m_data.ЦенаФикс.ToString("# ##0.000;-# ##0.000;;@"));
                                    else
                                        sheet.SetValue(m_data.Deleted ? styleValueNumMarkDeleted : (marked ? styleValueNum : styleValueNumMark), row, columnValues[marketplace.ShortName], m_data.ЦенаФикс);
                                }
                            }
                        }
                        else
                        {
                            if (m_data.ЕстьВКаталоге)
                                sheet.SetValue(m_data.Deleted ? styleValueCenterMarkDeleted : (marked ? styleValueCenter : styleValueCenterMark), row, columnValues[marketplace.ShortName], "к");
                            else
                                sheet.SetValue(m_data.Deleted ? styleValueRedMarkDeleted : (marked ? styleValueRed : styleValueRedMark), row, columnValues[marketplace.ShortName], "!!!");
                        }
                    }
                    else
                        sheet.SetValue(marked ? styleValue : styleValueMark, row, columnValues[marketplace.ShortName], "");
                    marked = true;
                }
                sheet.SetValue(styleValueNum, row, columnValues["Квант"], item.Квант);
                sheet.SetValue(styleValueNum, row, columnValues["КоррОстатков"], item.DeltaStock);
                sheet.SetValue(styleValue, row, columnValues["КраткоеОписание"], item.Характеристики);
                var странаId = await _context.ПолучитьЗначениеПериодическогоРеквизита(item.NomId, 5012);
                string страна = "";
                if (!string.IsNullOrWhiteSpace(странаId))
                    страна = await _context.Sc566s.Where(x => x.Id == странаId).Select(x => x.Descr.Trim()).FirstOrDefaultAsync();
                sheet.SetValue(styleValue, row, columnValues["Страна"], страна);
                sheet.SetValue(styleValue, row, columnValues["Категория"], item.Категория);
                sheet.SetValue(styleValue, row, columnValues["Штрихкод"], item.Штрихкод);
                sheet.SetValue(styleValueNum1tail, row, columnValues["Длина"], item.Длина);
                sheet.SetValue(styleValueNum1tail, row, columnValues["Ширина"], item.Ширина);
                sheet.SetValue(styleValueNum1tail, row, columnValues["Высота"], item.Высота);
                sheet.SetValue(styleValueNum1tail, row, columnValues["ГабаритыСумма"], item.Длина + item.Ширина + item.Высота);
                sheet.SetValue(styleValue, row, columnValues["Габариты"], (item.Длина + item.Ширина + item.Высота) > 0 ? 
                    item.Длина.ToString("# ##0.0;-# ##0.0;;@", System.Globalization.CultureInfo.InvariantCulture) + 
                    "/" + 
                    item.Ширина.ToString("# ##0.0;-# ##0.0;;@", System.Globalization.CultureInfo.InvariantCulture) + 
                    "/" + 
                    item.Высота.ToString("# ##0.0;-# ##0.0;;@", System.Globalization.CultureInfo.InvariantCulture) 
                    : "");
                sheet.SetValue(styleValueNum1tail, row, columnValues["Брутто"], item.Брутто);
                sheet.SetValue(styleValueNum0tail, row, columnValues["КолМест"], item.КолМест < 1 ? 1 : item.КолМест);
            }

            formula.EvaluateAll();
            GC.Collect();
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                var fileName = System.Web.HttpUtility.UrlEncode(("Матрица маркетплейс " + campaignData.Type + " " + campaignData.Наименование + " " + DateTime.Now.ToString("dd-MM-yyyy") + "." + extension).Replace(" ","-"), System.Text.Encoding.UTF8);
                return File(stream.ToArray(), "application/octet-stream", fileName);
            }
        }
        [HttpPost]
        public async Task<IActionResult> ImportDataAsync(IFormFile sourceFile, string sheetName)
        {
            string extension = Path.GetExtension(sourceFile.FileName).ToLower();
            IWorkbook workbook;
            using (Stream s = sourceFile.OpenReadStream())
            {
                if (extension == ".xls")
                    workbook = new HSSFWorkbook(s);
                else
                    workbook = new XSSFWorkbook(s);
            }
            if (workbook == null)
                return StatusCode(502, "Не могу инициализировать книгу Excel");

            ISheet sheet;
            if (string.IsNullOrEmpty(sheetName))
                sheet = workbook.GetSheetAt(0);
            else
                sheet = workbook.GetSheet(sheetName);
            if (sheet == null)
                return StatusCode(502, "В книге не найден лист " + (string.IsNullOrEmpty(sheetName) ? "" : sheetName));
            
            var map = FindExcelMap(sheet);
            if (map.Id < 0)
                return StatusCode(502, "Не смогли обнаружить колонку Id");
            if (map.Value < 0)
                return StatusCode(502, "Не смогли обнаружить колонку цен");
            if (map.FrozenPrice < 0)
                return StatusCode(502, "Не смогли обнаружить колонку заморозки цен");
            if (map.StartRow < 0)
                return StatusCode(502, "Не смогли обнаружить стартовую строку");

            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                for (int i = map.StartRow; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if ((row != null) && (row.Cells.Count > 0))
                    {
                        string marketUsingId = row.Cells
                            .FirstOrDefault(c => c.ColumnIndex == map.Id)
                            .GetStringValue()
                            .Replace("_", " ");
                        decimal value = 0;
                        if (!string.IsNullOrWhiteSpace(marketUsingId))
                        {
                            ICell valueCell = row.Cells
                                .FirstOrDefault(c => c.ColumnIndex == map.Value);
                            if (valueCell != null)
                            {
                                if (valueCell.CellType == CellType.Numeric)
                                    value = Convert.ToDecimal(valueCell.NumericCellValue);
                                else
                                {
                                    string strValue = valueCell.GetStringValue();
                                    if (!string.IsNullOrWhiteSpace(strValue) &&
                                        (strValue.Length > 4) &&
                                        strValue.StartsWith("!!! "))
                                    {
                                        Decimal.TryParse(strValue.Substring(4), out value);
                                    }
                                }
                            }
                            decimal deltaPrice = decimal.MinValue;
                            if (map.DeltaPrice >= 0)
                            {
                                ICell deltaPriceCell = row.Cells
                                    .FirstOrDefault(c => c.ColumnIndex == map.DeltaPrice);
                                if (deltaPriceCell != null)
                                {
                                    if (deltaPriceCell.CellType == CellType.Numeric)
                                        deltaPrice = Convert.ToDecimal(deltaPriceCell.NumericCellValue);
                                    else
                                        Decimal.TryParse(deltaPriceCell.GetStringValue(), out deltaPrice);
                                }
                            }
                            decimal frozenPrice = decimal.MinValue;
                            if (map.FrozenPrice >= 0)
                            {
                                ICell frozenPriceCell = row.Cells
                                    .FirstOrDefault(c => c.ColumnIndex == map.FrozenPrice);
                                if (frozenPriceCell != null)
                                {
                                    if (frozenPriceCell.CellType == CellType.Numeric)
                                        frozenPrice = Convert.ToDecimal(frozenPriceCell.NumericCellValue);
                                    else
                                        Decimal.TryParse(frozenPriceCell.GetStringValue(), out frozenPrice);
                                }
                            }
                            await UpdateMarketUsing(marketUsingId, value, deltaPrice, frozenPrice);
                            valueCell.CellStyle.FillForegroundColor = 52;
                        }
                    }
                }
                if (_context.Database.CurrentTransaction != null)
                    tran.Commit();
                GC.Collect();
                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    var fileName = System.Web.HttpUtility.UrlEncode(sourceFile.FileName, System.Text.Encoding.UTF8);
                    return File(stream.ToArray(), "application/octet-stream", fileName);
                }
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(db_ex.InnerException.ToString());
                return StatusCode(502, db_ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(ex.Message);
                return StatusCode(502, ex.Message);
            }
        }
        private (int Id,int Value,int StartRow, int DeltaPrice, int FrozenPrice) FindExcelMap(ISheet sheet)
        {
            int id = -1;
            int value = -1;
            int startRow = -1;
            int deltaPrice = -1;
            int frozenPrice = -1;
            for (int i = sheet.FirstRowNum; i <= sheet.LastRowNum; i++)
            {
                if ((id >= 0) && (value >= 0) && (deltaPrice >= 0))
                    break;
                IRow row = sheet.GetRow(i);
                if ((row != null) && (row.Cells.Count > 0))
                    for (int c = row.FirstCellNum; c <= row.LastCellNum; c++)
                    {
                        if ((id >= 0) && (value >= 0) && (deltaPrice >= 0))
                            break;
                        try
                        {
                            ICell cell = row.GetCell(c);
                            if (cell != null)
                            {
                                if (cell.GetStringValue() == "_Id_")
                                {
                                    id = cell.ColumnIndex;
                                    continue;
                                }
                                var color = cell.CellStyle.FillForegroundColor;
                                if (color == HSSFColor.Lime.Index) //Lime = 50
                                {
                                    value = cell.ColumnIndex;
                                    startRow = cell.RowIndex;
                                }
                                else if (color == HSSFColor.Yellow.Index) //Yellow = 13
                                    frozenPrice = cell.ColumnIndex;
                                else if (color == HSSFColor.Aqua.Index) //Aqua = 49
                                    deltaPrice = cell.ColumnIndex;
                            }

                        }
                        catch
                        { }
                    }
            }
            return (Id: id, Value: value, StartRow: startRow, DeltaPrice: deltaPrice, FrozenPrice: frozenPrice);
        }
        private async Task UpdateMarketUsing(string id, decimal value, decimal deltaPrice, decimal frozenPrice)
        {
            var entity = await _context.Sc14152s.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (entity != null)
            {
                bool needUpdate = false;
                if (entity.Sp14148 != value)
                {
                    needUpdate = true;
                    entity.Sp14148 = value;
                }
                if ((deltaPrice > decimal.MinValue) && (deltaPrice != entity.Sp14213))
                {
                    needUpdate = true;
                    entity.Sp14213 = deltaPrice;
                }
                if ((frozenPrice > decimal.MinValue) && (frozenPrice != entity.Sp14323))
                {
                    needUpdate = true;
                    entity.Sp14323 = frozenPrice;
                }
                if (needUpdate)
                {
                    _context.Update(entity);
                    _context.РегистрацияИзмененийРаспределеннойИБ(14152, entity.Id);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
