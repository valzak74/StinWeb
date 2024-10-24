using ExcelHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using StinClasses.Models;
using StinClasses.Документы;
using StinWeb.Models.DataManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;

namespace StinWeb.Controllers.Обработки
{
    public class HistoryReportController : Controller
    {
        private StinDbContext _context;
        private readonly ILogger<MarketplaceMatrixController> _logger;

        public HistoryReportController(StinDbContext context, ILogger<MarketplaceMatrixController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View("~/Views/Отчеты/HistoryReport.cshtml");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ExportDataAsync(DateTime startDate, DateTime endDate, string marketId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(marketId))
                return StatusCode(502, "Недопустимое значение marketId");

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

            ICellStyle styleHeader = workbook.StyleHeader(fontArialBold8);
            ICellStyle styleValue = workbook.StyleValue(fontArial8);
            ICellStyle styleValueNum = workbook.StyleValueNumber(fontArial8);
            ICellStyle styleValueMoney = workbook.StyleValueNumberMoney(fontArial8);

            ICellStyle styleValueNum0tail = workbook.StyleValueNumber(fontArial8, 0);

            ISheet sheet = workbook.CreateSheet("Лист 1");

            Dictionary<string, int> columnValues = new Dictionary<string, int>();
            int column = 0;
            columnValues.Add("Id", sheet.CreateColumnWithWidth(column++, 0));
            columnValues.Add("НомерПП", sheet.CreateColumnWithWidth(column++, 1500));
            columnValues.Add("ДатаПоступления", sheet.CreateColumnWithWidth(column++, 3100));
            columnValues.Add("ДатаОтгрузки", sheet.CreateColumnWithWidth(column++, 3100));
            columnValues.Add("ДатаРеализации", sheet.CreateColumnWithWidth(column++, 3100));
            columnValues.Add("Время", sheet.CreateColumnWithWidth(column++, 2700));
            columnValues.Add("Документ", sheet.CreateColumnWithWidth(column++, 4200));
            columnValues.Add("Номер", sheet.CreateColumnWithWidth(column++, 3500));
            columnValues.Add("Автор", sheet.CreateColumnWithWidth(column++, 3500));
            columnValues.Add("Статус", sheet.CreateColumnWithWidth(column++, 3200));
            columnValues.Add("Артикул", sheet.CreateColumnWithWidth(column++, 4200));
            columnValues.Add("Наименование", sheet.CreateColumnWithWidth(column++, 6000));
            columnValues.Add("Количество", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("Сумма", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("Себестоимость", sheet.CreateColumnWithWidth(column++, 2900));
            columnValues.Add("НомерЗаказа", sheet.CreateColumnWithWidth(column++, 4000));
            columnValues.Add("ДатаВозврата", sheet.CreateColumnWithWidth(column++, 3100));
            columnValues.Add("ВозвратОтмена", sheet.CreateColumnWithWidth(column++, 3100));
            columnValues.Add("Коэффициент", sheet.CreateColumnWithWidth(column++, 2500));

            sheet.SetColumnHidden(columnValues["Id"], true);
            int row = 0;

            sheet.SetValue(styleHeader, row, columnValues["Id"], "_Id_");
            sheet.SetValue(styleHeader, row, columnValues["НомерПП"], "Номер п/п");
            sheet.SetValue(styleHeader, row, columnValues["ДатаПоступления"], "Дата поступления заказа");
            sheet.SetValue(styleHeader, row, columnValues["ДатаОтгрузки"], "Дата отгрузки заказа");
            sheet.SetValue(styleHeader, row, columnValues["ДатаРеализации"], "Дата реализации");
            sheet.SetValue(styleHeader, row, columnValues["Время"], "Время");
            sheet.SetValue(styleHeader, row, columnValues["Документ"], "Документ");
            sheet.SetValue(styleHeader, row, columnValues["Номер"], "Номер");
            sheet.SetValue(styleHeader, row, columnValues["Автор"], "Автор");
            sheet.SetValue(styleHeader, row, columnValues["Статус"], "Статус");
            sheet.SetValue(styleHeader, row, columnValues["Артикул"], "Артикул");
            sheet.SetValue(styleHeader, row, columnValues["Наименование"], "Наименование товара");
            sheet.SetValue(styleHeader, row, columnValues["Количество"], "Кол-во");
            sheet.SetValue(styleHeader, row, columnValues["Сумма"], "Сумма продажи");
            sheet.SetValue(styleHeader, row, columnValues["Себестоимость"], "Себестоимость");
            sheet.SetValue(styleHeader, row, columnValues["НомерЗаказа"], "Номер заказа");
            sheet.SetValue(styleHeader, row, columnValues["ДатаВозврата"], "Дата возврата");
            sheet.SetValue(styleHeader, row, columnValues["ВозвратОтмена"], "Возврат/Отмена");
            sheet.SetValue(styleHeader, row, columnValues["Коэффициент"], "Коэффициент цп/себ");

            string j_startDateTime = startDate == DateTime.MinValue ? "" : startDate.JournalDateTime();
            string j_endDateTime = endDate == DateTime.MinValue ? "" : endDate.JournalDateTime();

            var query = from order in _context.Sc13994s
                        join market in _context.Sc14042s on order.Sp14038 equals market.Id
                        join item in _context.Sc14033s on order.Id equals item.Parentext
                        join nom in _context.Sc84s on item.Sp14022 equals nom.Id
                        join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                        join markUse in _context.Sc14152s on new { nomId = nom.Id, marketId = market.Id } equals new { nomId = markUse.Parentext, marketId = markUse.Sp14147 } into _markUse
                        from markUse in _markUse.DefaultIfEmpty()

                        join превЗаявка in _context.Dh12747s on order.Id equals превЗаявка.Sp14007
                        join j in _context._1sjourns on превЗаявка.Iddoc equals j.Iddoc

                        join счет in (
                             from crDoc in _context._1scrdocs
                             join таблЧасть in _context.Dt2457s on crDoc.Childid equals таблЧасть.Iddoc
                             join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed }
                             select new
                             {
                                 crDoc,
                                 таблЧасть,
                                 j,
                             }
                        ) on new { превЗаявка.Iddoc, nom.Id } equals new { Iddoc = счет.crDoc.Parentval.Substring(6, 9), Id = счет.таблЧасть.Sp2446 } into _счет
                        from счет in _счет.DefaultIfEmpty()

                        join набор in (
                             from crDoc in _context._1scrdocs
                             join таблЧасть in _context.Dt11948s on crDoc.Childid equals таблЧасть.Iddoc
                             join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed }
                             select new
                             {
                                 crDoc,
                                 таблЧасть,
                                 j,
                             }
                        ) on new { счет.j.Iddoc, nom.Id } equals new { Iddoc = набор.crDoc.Parentval.Substring(6, 9), Id = набор.таблЧасть.Sp11941 } into _набор
                        from набор in _набор.DefaultIfEmpty()

                        join компПродажа in _context.Dh12542s on order.Id equals компПродажа.Sp14005 into _компПродажа
                        from компПродажа in _компПродажа.DefaultIfEmpty()
                        join j_компПродажа in _context._1sjourns on компПродажа.Iddoc equals j_компПродажа.Iddoc into _j_компПродажа
                        from j_компПродажа in _j_компПродажа.DefaultIfEmpty()

                        join реализация in (
                             from crDoc in _context._1scrdocs
                             join таблЧасть in _context.Dt1611s on crDoc.Childid equals таблЧасть.Iddoc
                             join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed } 
                             select new
                             {
                                 crDoc,
                                 таблЧасть,
                                 j,
                             }
                        ) on new { компПродажа.Iddoc, nom.Id } equals new { Iddoc = реализация.crDoc.Parentval.Substring(6, 9), Id = реализация.таблЧасть.Sp1599 } into _реализация
                        from реализация in _реализация.DefaultIfEmpty()

                        join отчКомиссионера in (
                            from crDoc in _context._1scrdocs
                            join таблЧасть in _context.Dt1774s on crDoc.Childid equals таблЧасть.Iddoc
                            join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed }
                            join пользователи in _context.Sc30s on j.Sp74 equals пользователи.Id
                            join регПартииНаличие in _context.Ra328s on new { таблЧасть.Iddoc, nomId = таблЧасть.Sp1764 } equals new { регПартииНаличие.Iddoc, nomId = регПартииНаличие.Sp331 }
                            select new
                            {
                                crDoc,
                                таблЧасть,
                                j,
                                пользователи,
                                регПартииНаличие,
                            }
                        ) on new { компПродажа.Iddoc, nom.Id } equals new { Iddoc = отчКомиссионера.crDoc.Parentval.Substring(6, 9), Id = отчКомиссионера.таблЧасть.Sp1764 } into _отчКомиссионера
                        from отчКомиссионера in _отчКомиссионера.DefaultIfEmpty()

                        join отчКомиссионераРучной in (
                            from crDoc in _context._1scrdocs
                            join таблЧасть in _context.Dt1774s on crDoc.Childid equals таблЧасть.Iddoc
                            join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed }
                            join пользователи in _context.Sc30s on j.Sp74 equals пользователи.Id 
                            join регПартииНаличие in _context.Ra328s on new { таблЧасть.Iddoc, nomId = таблЧасть.Sp1764 } equals new { регПартииНаличие.Iddoc, nomId = регПартииНаличие.Sp331 }
                            select new
                            {
                                crDoc,
                                таблЧасть,
                                j,
                                пользователи,
                                регПартииНаличие,
                            }
                        ) on new { реализация.j.Iddoc, nom.Id } equals new { Iddoc = отчКомиссионераРучной.crDoc.Parentval.Substring(6, 9), Id = отчКомиссионераРучной.таблЧасть.Sp1764 } into _отчКомиссионераРучной
                        from отчКомиссионераРучной in _отчКомиссионераРучной.DefaultIfEmpty()

                        join возвратКомКомплекс in (
                             from crDoc in _context._1scrdocs
                             join таблЧасть in _context.Dt1656s on crDoc.Childid equals таблЧасть.Iddoc
                             join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed }
                             select new
                             {
                                 crDoc,
                                 таблЧасть,
                                 j,
                             }
                        ) on new { компПродажа.Iddoc, nom.Id } equals new { Iddoc = возвратКомКомплекс.crDoc.Parentval.Substring(6, 9), Id = возвратКомКомплекс.таблЧасть.Sp1644 } into _возвратКомКомплекс
                        from возвратКомКомплекс in _возвратКомКомплекс.DefaultIfEmpty()

                        join возвратКомРеализация in (
                             from crDoc in _context._1scrdocs
                             join таблЧасть in _context.Dt1656s on crDoc.Childid equals таблЧасть.Iddoc
                             join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed }
                             select new
                             {
                                 crDoc,
                                 таблЧасть,
                                 j,
                             }
                        ) on new { реализация.j.Iddoc, nom.Id } equals new { Iddoc = возвратКомРеализация.crDoc.Parentval.Substring(6, 9), Id = возвратКомРеализация.таблЧасть.Sp1644 } into _возвратКомРеализация
                        from возвратКомРеализация in _возвратКомРеализация.DefaultIfEmpty()

                        join возвратКуп in (
                             from crDoc in _context._1scrdocs
                             join таблЧасть in _context.Dt1656s on crDoc.Childid equals таблЧасть.Iddoc
                             join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed }
                             select new
                             {
                                 crDoc,
                                 таблЧасть,
                                 j,
                             }
                        ) on new { отчКомиссионера.таблЧасть.Iddoc, nom.Id } equals new { Iddoc = возвратКуп.crDoc.Parentval.Substring(6, 9), Id = возвратКуп.таблЧасть.Sp1644 } into _возвратКуп
                        from возвратКуп in _возвратКуп.DefaultIfEmpty()

                        join отменаЗаявки in (
                             from crDoc in _context._1scrdocs
                             join таблЧасть in _context.Dt6313s on crDoc.Childid equals таблЧасть.Iddoc 
                             join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed }
                             select new
                             {
                                 crDoc,
                                 таблЧасть,
                                 j,
                             }
                        ) on счет.j.Iddoc equals отменаЗаявки.crDoc.Parentval.Substring(6, 9) into _отменаЗаявки
                        from отменаЗаявки in _отменаЗаявки.DefaultIfEmpty()

                        join отменаНабора in (
                             from crDoc in _context._1scrdocs
                             join таблЧасть in _context.Dh11964s on crDoc.Childid equals таблЧасть.Iddoc
                             join j in _context._1sjourns on new { таблЧасть.Iddoc, DateTimeIdDoc = crDoc.ChildDateTimeIddoc, Closed = (byte)1 } equals new { j.Iddoc, DateTimeIdDoc = j.DateTimeIddoc, j.Closed }
                             select new
                             {
                                 crDoc,
                                 таблЧасть,
                                 j,
                             }
                        ) on набор.j.Iddoc equals отменаНабора.crDoc.Parentval.Substring(6, 9) into _отменаНабора
                        from отменаНабора in _отменаНабора.DefaultIfEmpty()

                        where !order.Ismark && j.Closed == 1 &&
                            (j_компПродажа != null ? j_компПродажа.Closed == 1 : true) &&
                            (string.IsNullOrEmpty(j_startDateTime) ? true : j.DateTimeIddoc.CompareTo(j_startDateTime) >= 0) &&
                            (string.IsNullOrEmpty(j_endDateTime) ? true : j.DateTimeIddoc.CompareTo(j_endDateTime) <= 0) &&
                            (string.IsNullOrEmpty(marketId) ? true : market.Id == marketId)
                        orderby j.DateTimeIddoc
                        let отчетКомиссионера = отчКомиссионера.j != null
                            ? отчКомиссионера
                            : отчКомиссионераРучной.j != null
                                ? отчКомиссионераРучной
                                : null
                        select new
                        {
                            Id = item.Id,
                            ПредвЗаявкаДатаДок = Common.DateTimeIddoc(j.DateTimeIddoc).ToString("dd-MM-yy"),
                            Проведен = отчетКомиссионера.j != null ? "Проведен" : "",
                            КомпПродажаДатаДок = j_компПродажа != null ? Common.DateTimeIddoc(j_компПродажа.DateTimeIddoc).ToString("dd-MM-yy") : "",
                            ОтчКомиссионераДатаДок = отчетКомиссионера.j != null ? Common.DateTimeIddoc(отчетКомиссионера.j.DateTimeIddoc).ToString("dd-MM-yy") : "",
                            ОтчКомиссионераВремяДок = отчетКомиссионера.j != null ? Common.DateTimeIddoc(отчетКомиссионера.j.DateTimeIddoc).ToString("HH:mm:ss") : "",
                            ОтчКомиссионераНомер = отчетКомиссионера.j != null ? отчетКомиссионера.j.Docno : "",
                            ОтчКомиссионераАвтор = отчетКомиссионера.пользователи != null ? отчетКомиссионера.пользователи.Descr.Trim() : "",

                            Артикул = nom.Sp85.Trim(),
                            Товар = nom.Descr.Trim(),
                            Количество = item.Sp14023,
                            Сумма = Math.Round(item.Sp14024 * item.Sp14023, 2, MidpointRounding.AwayFromZero),
                            Себестоимость = отчетКомиссионера.регПартииНаличие != null ? Math.Round(отчетКомиссионера.регПартииНаличие.Sp421, 2, MidpointRounding.AwayFromZero) : 0,
                            НомерЗаказа = order.Code.Trim(),
                            ДатаВозврата = возвратКуп.j != null ? Common.DateTimeIddoc(возвратКуп.j.DateTimeIddoc).ToString("dd-MM-yy") : "",
                            ДатаОтмены = 
                                  возвратКомКомплекс.j != null ? Common.DateTimeIddoc(возвратКомКомплекс.j.DateTimeIddoc).ToString("dd-MM-yy")
                                : возвратКомРеализация.j != null ? Common.DateTimeIddoc(возвратКомРеализация.j.DateTimeIddoc).ToString("dd-MM-yy")
                                : отменаЗаявки.j != null ? Common.DateTimeIddoc(отменаЗаявки.j.DateTimeIddoc).ToString("dd-MM-yy")
                                : отменаНабора.j != null ? Common.DateTimeIddoc(отменаНабора.j.DateTimeIddoc).ToString("dd-MM-yy")
                                : "",
                        };
            var nativeData = await query.ToListAsync(cancellationToken);

            foreach (var item in nativeData)
            {
                row++;
                sheet.SetValue(styleValue, row, columnValues["Id"], item.Id.Replace(" ", "_"));
                sheet.SetValue(styleValueNum0tail, row, columnValues["НомерПП"], row);
                sheet.SetValue(styleValue, row, columnValues["ДатаПоступления"], item.ПредвЗаявкаДатаДок);
                sheet.SetValue(styleValue, row, columnValues["ДатаОтгрузки"], item.КомпПродажаДатаДок);
                sheet.SetValue(styleValue, row, columnValues["ДатаРеализации"], item.ОтчКомиссионераДатаДок);
                sheet.SetValue(styleValue, row, columnValues["Время"], item.ОтчКомиссионераВремяДок);
                sheet.SetValue(styleValue, row, columnValues["Документ"], string.IsNullOrEmpty(item.ОтчКомиссионераДатаДок) ? "" : "Отчет комиссионера");
                sheet.SetValue(styleValue, row, columnValues["Номер"], item.ОтчКомиссионераНомер);
                sheet.SetValue(styleValue, row, columnValues["Автор"], item.ОтчКомиссионераАвтор);
                sheet.SetValue(styleValue, row, columnValues["Статус"], item.Проведен);
                sheet.SetValue(styleValue, row, columnValues["Артикул"], item.Артикул);
                sheet.SetValue(styleValue, row, columnValues["Наименование"], item.Товар);
                sheet.SetValue(styleValueNum0tail, row, columnValues["Количество"], item.Количество);
                sheet.SetValue(styleValueMoney, row, columnValues["Сумма"], item.Сумма);
                sheet.SetValue(styleValueMoney, row, columnValues["Себестоимость"], item.Себестоимость);
                sheet.SetValue(styleValue, row, columnValues["НомерЗаказа"], item.НомерЗаказа);
                sheet.SetValue(styleValue, row, columnValues["ДатаВозврата"], item.ДатаВозврата);
                sheet.SetValue(styleValue, row, columnValues["ВозвратОтмена"], item.ДатаОтмены);
                sheet.SetValue(styleValueNum, row, columnValues["Коэффициент"], item.Себестоимость == 0 ? 0 : Math.Round(item.Сумма / item.Себестоимость, 3, MidpointRounding.AwayFromZero));
            }

            formula.EvaluateAll();
            GC.Collect();
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                var fileName = System.Web.HttpUtility.UrlEncode(("Отчет истории заказов маркетплейс " + DateTime.Now.ToString("dd-MM-yyyy") + "." + extension).Replace(" ", "-"), System.Text.Encoding.UTF8);
                return File(stream.ToArray(), "application/octet-stream", fileName);
            }
        }
    }
}
