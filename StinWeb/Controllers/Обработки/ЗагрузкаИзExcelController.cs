using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager;
using StinWeb.Models.Repository.Справочники;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using StinWeb.Models.Repository;
using NonFactors.Mvc.Lookup;
using StinWeb.Lookups;
using StinClasses.Models;

namespace StinWeb.Controllers.Обработки
{
    public class ЗагрузкаИзExcelController : Controller
    {
        private StinDbContext _context;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private UserRepository _userRepository;
        private НоменклатураRepository _номенклатура; 
        public ЗагрузкаИзExcelController(StinDbContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _serviceScopeFactory = serviceScopeFactory;
            _userRepository = new UserRepository(context);
            _номенклатура = new НоменклатураRepository(context);
        }
        protected override void Dispose(bool disposing)
        {
            _userRepository.Dispose();
            _номенклатура.Dispose();
            _context.Dispose();
            base.Dispose(disposing);
        }
        [Authorize]
        public IActionResult Index()
        {
            if (_context.NeedToOpenPeriod())
                return Redirect("/Home/Error?Description=ПериодНеОткрыт");
            Dictionary<int, string> ExcelColumns = new Dictionary<int, string>();
            ExcelColumns[0] = "A";
            ExcelColumns[1] = "B";
            ExcelColumns[2] = "C";
            ExcelColumns[3] = "D";
            ExcelColumns[4] = "E";
            ExcelColumns[5] = "F";
            ExcelColumns[6] = "G";
            ExcelColumns[7] = "H";
            ExcelColumns[8] = "I";
            ExcelColumns[9] = "J";
            ExcelColumns[10] = "K";
            ExcelColumns[11] = "L";
            ExcelColumns[12] = "M";
            ExcelColumns[13] = "N";
            ExcelColumns[14] = "O";
            ExcelColumns[15] = "P";
            ExcelColumns[16] = "Q";
            ExcelColumns[17] = "R";
            ExcelColumns[18] = "S";
            ExcelColumns[19] = "T";
            ExcelColumns[20] = "U";
            ExcelColumns[21] = "V";
            ExcelColumns[22] = "W";
            ExcelColumns[23] = "X";
            ExcelColumns[24] = "Y";
            ExcelColumns[25] = "Z";
            for (int i = 0; i < 26; i++)
                ExcelColumns[i] = ExcelColumns[i] + " [" + (i+1).ToString() + "]";
            ViewBag.ExcelColumns = new SelectList(ExcelColumns, "Key", "Value");

            return View("~/Views/Обработки/ЗагрузкаИзExcel.cshtml");
        }
        public JsonResult GetChildren(string id)
        {
            var items = _номенклатура.GetAll()
                .Where(x => x.Isfolder == 1 && x.Parentid == (id == "#" ? Common.ПустоеЗначение : id))
                .Select(x => new
                {
                    id = x.Id,
                    parent = id,
                    text = x.Descr.Trim(),
                    children = x.Isfolder == 1,
                    icon = x.Isfolder == 2 ? "jstree-file" : ""
                })
                .OrderBy(x => x.text);

            return Json(items);
        }
        [HttpGet]
        public JsonResult SelectBrend(LookupFilter filter)
        {
            return Json(new ПроизводительLookup(_context) { Filter = filter }.GetData());
        }
        private async Task ProcessImage(IRow row, bool isMain, int columnIndex, string key)
        {
            if (columnIndex >= 0)
            {
                string ImageUrl = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == columnIndex));

                if (!string.IsNullOrEmpty(ImageUrl))
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var FileDownloader = scope.ServiceProvider.GetService<IFileDownloader>();
                        await FileDownloader.GetAndSaveDataAsync(_context, isMain, ImageUrl, key);
                    }
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> LoadDataAsync(string ParentId, string BrendId,
            bool Проверка, IFormFile sourceFile, int ТипЗагрузки, string sheetName, 
            int startRowNum, int ИнфоИндекс, int АртикулИндекс, int НаименованиеИндекс,
            int ЕдиницаИндекс, int ШтрихкодИндекс, int СнятСПрИндекс, int ПодЗаказИндекс, int WebИндекс, 
            int КомментИндекс, int КратОписИндекс, int ХарактеристикиИндекс, int ПодробОписИндекс, int АртикулОригиналИндекс, int ТнВэдИндекс,
            int ВесИндекс, int ВесБруттоИндекс, int КолМестИндекс, int ШиринаИндекс, int ВысотаИндекс, int ГлубинаИндекс,
            int Image1Index, int Image2Index, int Image3Index, int Image4Index, int Image5Index, int Image6Index)
        {
            if (startRowNum < 0)
                return StatusCode(502, "Недопустимое значение стартовой строки");
            string extension = Path.GetExtension(sourceFile.FileName).ToLower();
            IWorkbook wb;
            ICellStyle OkCellStyle;
            ICellStyle ErrorCellStyle;
            using (Stream s = sourceFile.OpenReadStream())
            {
                if (extension == ".xls")
                {
                    wb = new HSSFWorkbook(s);
                    OkCellStyle = (HSSFCellStyle)wb.CreateCellStyle();
                    OkCellStyle.FillForegroundColor = (Excel.setColor((HSSFWorkbook)wb, (byte)230, (byte)255, (byte)230, 56)).Indexed;
                    ErrorCellStyle = (HSSFCellStyle)wb.CreateCellStyle();
                    ErrorCellStyle.FillForegroundColor = (Excel.setColor((HSSFWorkbook)wb, (byte)248, (byte)236, (byte)242, 57)).Indexed;
                }
                else
                {
                    wb = new XSSFWorkbook(s);
                    OkCellStyle = (XSSFCellStyle)wb.CreateCellStyle();
                    (OkCellStyle as XSSFCellStyle).FillForegroundXSSFColor = new XSSFColor(new byte[] { 230, 255, 230 });
                    ErrorCellStyle = (XSSFCellStyle)wb.CreateCellStyle();
                    (ErrorCellStyle as XSSFCellStyle).FillForegroundXSSFColor = new XSSFColor(new byte[] { 248, 236, 242 });
                }
            }
            if (wb == null)
                return StatusCode(502, "Не могу инициализировать книгу Excel");
            ISheet sheet;
            if (string.IsNullOrEmpty(sheetName))
                sheet = wb.GetSheetAt(0);
            else
                sheet = wb.GetSheet(sheetName);
            if (sheet == null)
                return StatusCode(502, "В книге не найден лист " + (string.IsNullOrEmpty(sheetName) ? "" : sheetName));

            OkCellStyle.FillPattern = FillPattern.SolidForeground;
            ErrorCellStyle.FillPattern = FillPattern.SolidForeground;

            for (int i = startRowNum; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                if ((row != null) && (row.Cells.Count > 0))
                    try
                    {
                        bool Success = false;
                        string НоменклатураId = "";
                        var артикул = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == АртикулИндекс));
                        string наименование = "";
                        if (НаименованиеИндекс >= 0)
                            наименование = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == НаименованиеИндекс));
                        string s = "";
                        string ЕдиницаНазвание = "шт";
                        if (ЕдиницаИндекс >= 0)
                            ЕдиницаНазвание = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ЕдиницаИндекс));
                        string Штрихкод = "";
                        if (ШтрихкодИндекс >= 0)
                            Штрихкод = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ШтрихкодИндекс));
                        bool СнятСПроизводства = false;
                        if (СнятСПрИндекс >= 0)
                        {
                            s = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == СнятСПрИндекс));
                            СнятСПроизводства = (s == "1" || s.ToLower() == "true" || s.ToLower() == "yes" || s.ToLower() == "да");
                        }
                        bool ПодЗаказ = false;
                        if (ПодЗаказИндекс >= 0)
                        {
                            s = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ПодЗаказИндекс));
                            ПодЗаказ = (s == "1" || s.ToLower() == "true" || s.ToLower() == "yes" || s.ToLower() == "да");
                        }
                        bool Web = false;
                        if (WebИндекс >= 0)
                        {
                            s = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == WebИндекс));
                            Web = (s == "1" || s.ToLower() == "true" || s.ToLower() == "yes" || s.ToLower() == "да");
                        }
                        string Комментарий = "";
                        if (КомментИндекс >= 0)
                            Комментарий = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == КомментИндекс));
                        string КраткоеОписание = "";
                        if (КратОписИндекс >= 0)
                            КраткоеОписание = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == КратОписИндекс));
                        string Характеристики = "";
                        if (ХарактеристикиИндекс >= 0)
                            Характеристики = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ХарактеристикиИндекс));
                        string ПодробноеОписание = "";
                        if (ПодробОписИндекс >= 0)
                            ПодробноеОписание = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ПодробОписИндекс));
                        string АртикулОригинал = "";
                        if (АртикулОригиналИндекс >= 0)
                            АртикулОригинал = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == АртикулОригиналИндекс));
                        string ТнВЭД = "";
                        if (ТнВэдИндекс >= 0)
                            ТнВЭД = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ТнВэдИндекс));
                        decimal Вес = 0;
                        if (ВесИндекс >= 0)
                        {
                            s = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ВесИндекс));
                            s = s.Replace(".", ",").Replace("'", "");
                            decimal.TryParse(s, out Вес);
                        }
                        decimal ВесБрутто = 0;
                        if (ВесБруттоИндекс >= 0)
                        {
                            s = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ВесБруттоИндекс));
                            s = s.Replace(".", ",").Replace("'", "");
                            decimal.TryParse(s, out ВесБрутто);
                        }
                        decimal КолМест = 0;
                        if (КолМестИндекс >= 0)
                        {
                            s = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == КолМестИндекс));
                            s = s.Replace(".", ",").Replace("'", "");
                            decimal.TryParse(s, out КолМест);
                        }
                        decimal Ширина = 0;
                        if (ШиринаИндекс >= 0)
                        {
                            s = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ШиринаИндекс));
                            s = s.Replace(".", ",").Replace("'", "");
                            decimal.TryParse(s, out Ширина);
                        }
                        decimal Высота = 0;
                        if (ВысотаИндекс >= 0)
                        {
                            s = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ВысотаИндекс));
                            s = s.Replace(".", ",").Replace("'", "");
                            decimal.TryParse(s, out Высота);
                        }
                        decimal Глубина = 0;
                        if (ГлубинаИндекс >= 0)
                        {
                            s = Excel.GetStringCellValue(row.Cells.FirstOrDefault(c => c.ColumnIndex == ГлубинаИндекс));
                            s = s.Replace(".", ",").Replace("'", "");
                            decimal.TryParse(s, out Глубина);
                        }
                        if (ТипЗагрузки == 1 && !string.IsNullOrEmpty(артикул) && !string.IsNullOrEmpty(наименование))
                        {
                            if (_номенклатура.ПолучитьНоменклатуруПоАртикулу(артикул).Count() > 0)
                                Excel.CreateOrUpdateCell(row, ИнфоИндекс, "дублирование артикула", ErrorCellStyle);
                            else if (_номенклатура.ПолучитьНоменклатуруПоНаименованию(наименование).Count() > 0)
                                Excel.CreateOrUpdateCell(row, ИнфоИндекс, "дублирование наименования", ErrorCellStyle);
                            else
                            {
                                if (ШтрихкодИндекс >= 0 && !string.IsNullOrEmpty(Штрихкод))
                                {
                                    if (Штрихкод.Length > 50)
                                    {
                                        Excel.CreateOrUpdateCell(row, ИнфоИндекс, "Длина штрих-кода превышает 50 символов", ErrorCellStyle);
                                        continue;
                                    }
                                    if (_номенклатура.ПолучитьНоменклатуруПоШтрихКоду(Штрихкод).Count() > 0)
                                    {
                                        Excel.CreateOrUpdateCell(row, ИнфоИндекс, "дублирование штрих-кода", ErrorCellStyle);
                                        continue;
                                    }
                                }
                                var Результат = await _номенклатура.CreateNewAsync(Проверка, ParentId, BrendId, User.FindFirstValue("UserId"), артикул, наименование,
                                    ЕдиницаНазвание, Штрихкод, СнятСПроизводства, ПодЗаказ, Web, Комментарий, КраткоеОписание, ПодробноеОписание,
                                    Характеристики, АртикулОригинал, ТнВЭД,
                                    Вес, ВесБрутто, КолМест, Ширина, Высота, Глубина);
                                if (string.IsNullOrEmpty(Результат.Message))
                                {
                                    Excel.CreateOrUpdateCell(row, ИнфоИндекс, "Ok", OkCellStyle);
                                    Success = true;
                                    НоменклатураId = Результат.НоменклатураId;
                                }
                                else
                                    Excel.CreateOrUpdateCell(row, ИнфоИндекс, Результат.Message, ErrorCellStyle);
                            } 
                        }
                        else if (ТипЗагрузки == 0 && !string.IsNullOrEmpty(артикул))
                        {
                            //обновление элемента справочника
                            var НайденнаяНоменклатура = _номенклатура.ПолучитьНоменклатуруПоАртикулу(артикул);
                            if (НайденнаяНоменклатура.Count() == 0)
                                Excel.CreateOrUpdateCell(row, ИнфоИндекс, "Элемент не обнаружен", ErrorCellStyle);
                            else
                            {
                                НайденнаяНоменклатура = НайденнаяНоменклатура.Where(x => x.ПроизводительId == BrendId);
                                if (НайденнаяНоменклатура.Count() == 0)
                                {
                                    Excel.CreateOrUpdateCell(row, ИнфоИндекс, "Элемент производителя не обнаружен", ErrorCellStyle);
                                }
                                else if (НайденнаяНоменклатура.Count() > 1)
                                {
                                    Excel.CreateOrUpdateCell(row, ИнфоИндекс, "Найдено более одного элемента", ErrorCellStyle);
                                }
                                else
                                {
                                    var ОбновляемаяНоменклатура = НайденнаяНоменклатура.FirstOrDefault();
                                    if (ШтрихкодИндекс >= 0 && !string.IsNullOrEmpty(Штрихкод))
                                    {
                                        if (Штрихкод.Length > 50)
                                        {
                                            Excel.CreateOrUpdateCell(row, ИнфоИндекс, "Длина штрих-кода превышает 50 символов", ErrorCellStyle);
                                            continue;
                                        }
                                        if (_номенклатура.ПолучитьНоменклатуруПоШтрихКоду(Штрихкод)
                                            .Where(x => x.Id != ОбновляемаяНоменклатура.Id)
                                            .Count() > 0)
                                        {
                                            Excel.CreateOrUpdateCell(row, ИнфоИндекс, "дублирование штрих-кода", ErrorCellStyle);
                                            continue;
                                        }
                                    }
                                    var Результат = await _номенклатура.UpdateAsync(Проверка, ОбновляемаяНоменклатура.Id, User.FindFirstValue("UserId"), артикул,
                                        НаименованиеИндекс >= 0, наименование,
                                        ШтрихкодИндекс >= 0, Штрихкод,
                                        СнятСПрИндекс >= 0, СнятСПроизводства,
                                        ПодЗаказИндекс >= 0, ПодЗаказ,
                                        WebИндекс >= 0, Web,
                                        КомментИндекс >= 0, Комментарий,
                                        КратОписИндекс >= 0, КраткоеОписание,
                                        ПодробОписИндекс >= 0, ПодробноеОписание,
                                        ХарактеристикиИндекс >= 0, Характеристики,
                                        АртикулОригиналИндекс >= 0, АртикулОригинал,
                                        ТнВэдИндекс >= 0, ТнВЭД,
                                        ВесИндекс >= 0, Вес,
                                        ВесБруттоИндекс >= 0, ВесБрутто,
                                        КолМестИндекс >= 0, КолМест,
                                        ШиринаИндекс >= 0, Ширина,
                                        ВысотаИндекс >= 0, Высота,
                                        ГлубинаИндекс >= 0, Глубина
                                        );
                                    if (string.IsNullOrEmpty(Результат))
                                    {
                                        Excel.CreateOrUpdateCell(row, ИнфоИндекс, "Ok", OkCellStyle);
                                        Success = true;
                                        НоменклатураId = ОбновляемаяНоменклатура.Id;
                                    }
                                    else
                                        Excel.CreateOrUpdateCell(row, ИнфоИндекс, Результат, ErrorCellStyle);
                                }
                            }
                        }
                        if (Success && !Проверка)
                        {
                            await ProcessImage(row, true, Image1Index, НоменклатураId);
                            await ProcessImage(row, false, Image2Index, НоменклатураId);
                            await ProcessImage(row, false, Image3Index, НоменклатураId);
                            await ProcessImage(row, false, Image4Index, НоменклатураId);
                            await ProcessImage(row, false, Image5Index, НоменклатураId);
                            await ProcessImage(row, false, Image6Index, НоменклатураId);
                        }
                    }
                    catch
                    { }

                
            }
            GC.Collect();
            using (var stream = new MemoryStream())
            {
                wb.Write(stream);
                var fileName = System.Web.HttpUtility.UrlEncode(sourceFile.FileName, System.Text.Encoding.UTF8);
                return File(stream.ToArray(), "application/octet-stream", fileName);
            }
        }
    }
}
