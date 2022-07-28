using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using NPOI.SS.Converter;
using System.Text.RegularExpressions;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.DataManager
{
    public class DataEntry
    {
        public string Имя { get; set; }
        public string Тип { get; set; }
        public string Значение { get; set; }
    }

    public class nDataEntry
    {
        public string мнИмя { get; set; }
        public List<List<DataEntry>> мнЗначения { get; set; }
    }

    public static class Excel
    {
        public static void CreateOrUpdateCell(IRow CurrentRow, int CellIndex, string Value = null, ICellStyle Style = null)
        {
            ICell Cell = CurrentRow.GetCell(CellIndex);
            if (Cell == null)
                Cell = CurrentRow.CreateCell(CellIndex);
            if (!string.IsNullOrEmpty(Value))
                Cell.SetCellValue(Value);
            if (Style != null)
                Cell.CellStyle = Style;
        }
        public static HSSFColor setColor(HSSFWorkbook workbook, byte r, byte g, byte b, short index)
        {
            //index стандартного цвета в HSSFColor. Диапазон 8 - 63
            HSSFPalette palette = workbook.GetCustomPalette();
            HSSFColor hssfColor = null;
            try
            {
                hssfColor = palette.FindColor(r, g, b);
                if (hssfColor == null)
                {
                    palette.SetColorAtIndex(index, r, g, b);
                    hssfColor = palette.GetColor(index);
                }
            }
            catch
            {
            }

            return hssfColor;
        }
        private static void CopyRow(IWorkbook workbook, ISheet worksheet, int sourceRowNum, List<List<DataEntry>> entries)
        {
            int shift = entries.Count;
            IRow sourceRow = worksheet.GetRow(sourceRowNum);
            worksheet.ShiftRows(sourceRowNum + 1, worksheet.LastRowNum, shift, true, true);
            for (int i = 1; i <= shift; i++)
            {
                IRow row = worksheet.GetRow(sourceRowNum + i);
                if (row != null)
                    worksheet.RemoveRow(row);
            }
            for (int r = 1; r <= shift; r++)
            {
                IRow newRow = worksheet.GetRow(sourceRowNum + r);
                if (newRow == null)
                    newRow = worksheet.CreateRow(sourceRowNum + r);

                // Loop through source columns to add to new row
                for (int i = 0; i < sourceRow.LastCellNum; i++)
                {
                    // Grab a copy of the old/new cell
                    ICell oldCell = sourceRow.GetCell(i);
                    ICell newCell = newRow.CreateCell(i);

                    // If the old cell is null jump to next cell
                    if (oldCell == null)
                    {
                        newCell = null;
                        continue;
                    }

                    // Copy style from old cell and apply to new cell
                    //ICellStyle newCellStyle = workbook.CreateCellStyle();
                    //newCellStyle.CloneStyleFrom(oldCell.CellStyle); ;
                    //newCell.CellStyle = newCellStyle;
                    newCell.CellStyle = oldCell.CellStyle;

                    // If there is a cell comment, copy
                    if (oldCell.CellComment != null) newCell.CellComment = oldCell.CellComment;

                    // If there is a cell hyperlink, copy
                    if (oldCell.Hyperlink != null) newCell.Hyperlink = oldCell.Hyperlink;

                    // Set the cell data type
                    newCell.SetCellType(oldCell.CellType);

                    // Set the cell data value
                    switch (oldCell.CellType)
                    {
                        case CellType.Blank:
                            newCell.SetCellValue(oldCell.StringCellValue);
                            break;
                        case CellType.Boolean:
                            newCell.SetCellValue(oldCell.BooleanCellValue);
                            break;
                        case CellType.Error:
                            newCell.SetCellErrorValue(oldCell.ErrorCellValue);
                            break;
                        case CellType.Formula:
                            newCell.SetCellFormula(oldCell.CellFormula);
                            break;
                        case CellType.Numeric:
                            newCell.SetCellValue(oldCell.NumericCellValue);
                            break;
                        case CellType.String:
                            string cellText = oldCell.StringCellValue;
                            var variants = Regex.Matches(cellText, @"(\A|\s*)\{(\w+)\}(\s*|\z)")
                            .Cast<Match>()
                            .Select(m => m.Value.Trim());
                            foreach (string variant in variants)
                            {
                                DataEntry entry = entries[r - 1].Find(x => "{" + x.Имя + "}" == variant);
                                if (entry != null)
                                {
                                    cellText = cellText.Replace(variant, entry.Значение);
                                }
                            }
                            newCell.SetCellValue(cellText);
                            //newCell.SetCellValue(oldCell.RichStringCellValue);
                            break;
                        case CellType.Unknown:
                            newCell.SetCellValue(oldCell.StringCellValue);
                            break;
                    }
                }

                //newRow.Height = -1;
                // If there are are any merged regions in the source row, copy to new row
                for (int i = 0; i < worksheet.NumMergedRegions; i++)
                {
                    CellRangeAddress cellRangeAddress = worksheet.GetMergedRegion(i);
                    if (cellRangeAddress.FirstRow == sourceRow.RowNum)
                    {
                        CellRangeAddress newCellRangeAddress = new CellRangeAddress(newRow.RowNum,
                                                                                    (newRow.RowNum +
                                                                                     (cellRangeAddress.FirstRow -
                                                                                      cellRangeAddress.LastRow)),
                                                                                    cellRangeAddress.FirstColumn,
                                                                                    cellRangeAddress.LastColumn);
                        worksheet.AddMergedRegion(newCellRangeAddress);
                    }
                }
            }

        }
        private static void CopyRow(IWorkbook sourceWB, ISheet sourceWS, int sourceRowNum, IWorkbook destWB, ISheet destWS, int destinationRowNum)
        {
            // Get the source / new row
            IRow sourceRow = sourceWS.GetRow(sourceRowNum);
            IRow newRow = destWS.GetRow(destinationRowNum);
            // If the row exist in destination, push down all rows by 1 else create a new row
            if (newRow != null)
            {
                destWS.ShiftRows(destinationRowNum, destWS.LastRowNum, 1);
            }
            else
            {
                newRow = destWS.CreateRow(destinationRowNum);
            }

            // Loop through source columns to add to new row
            for (int i = 0; i < sourceRow.LastCellNum; i++)
            {
                // Grab a copy of the old/new cell
                ICell oldCell = sourceRow.GetCell(i);
                ICell newCell = newRow.CreateCell(i);

                // If the old cell is null jump to next cell
                if (oldCell == null)
                {
                    newCell = null;
                    continue;
                }

                // Copy style from old cell and apply to new cell
                ICellStyle newCellStyle = destWB.CreateCellStyle();
                newCellStyle.CloneStyleFrom(oldCell.CellStyle); ;
                newCell.CellStyle = newCellStyle;

                // If there is a cell comment, copy
                if (newCell.CellComment != null) newCell.CellComment = oldCell.CellComment;

                // If there is a cell hyperlink, copy
                if (oldCell.Hyperlink != null) newCell.Hyperlink = oldCell.Hyperlink;

                // Set the cell data type
                newCell.SetCellType(oldCell.CellType);

                // Set the cell data value
                switch (oldCell.CellType)
                {
                    case CellType.Blank:
                        newCell.SetCellValue(oldCell.StringCellValue);
                        break;
                    case CellType.Boolean:
                        newCell.SetCellValue(oldCell.BooleanCellValue);
                        break;
                    case CellType.Error:
                        newCell.SetCellErrorValue(oldCell.ErrorCellValue);
                        break;
                    case CellType.Formula:
                        newCell.SetCellFormula(oldCell.CellFormula);
                        break;
                    case CellType.Numeric:
                        newCell.SetCellValue(oldCell.NumericCellValue);
                        break;
                    case CellType.String:
                        newCell.SetCellValue(oldCell.RichStringCellValue);
                        break;
                    case CellType.Unknown:
                        newCell.SetCellValue(oldCell.StringCellValue);
                        break;
                }
            }

            // If there are are any merged regions in the source row, copy to new row
            for (int i = 0; i < sourceWS.NumMergedRegions; i++)
            {
                CellRangeAddress cellRangeAddress = sourceWS.GetMergedRegion(i);
                if (cellRangeAddress.FirstRow == sourceRow.RowNum)
                {
                    CellRangeAddress newCellRangeAddress = new CellRangeAddress(newRow.RowNum,
                                                                                (newRow.RowNum +
                                                                                 (cellRangeAddress.FirstRow -
                                                                                  cellRangeAddress.LastRow)),
                                                                                cellRangeAddress.FirstColumn,
                                                                                cellRangeAddress.LastColumn);
                    destWS.AddMergedRegion(newCellRangeAddress);
                }
            }

        }
        public static string GetStringCellValue(ICell cell)
        {
            if (cell == null)
                return "";
            else
                switch (cell.CellType)
                {
                    case CellType.Blank:
                        return "";
                    case CellType.Boolean:
                        return cell.BooleanCellValue == true ? "1" : "0";
                    case CellType.Error:
                        return "Error";
                    case CellType.Formula:
                        return cell.CellFormula;
                    case CellType.Numeric:
                        return cell.NumericCellValue.ToString();
                    case CellType.String:
                        return cell.RichStringCellValue.String;
                    case CellType.Unknown:
                        return cell.StringCellValue;
                    default:
                        return cell.StringCellValue;
                }
        }
        static public class PixelUtil
        {

            public static short EXCEL_COLUMN_WIDTH_FACTOR = 256;
            public static short EXCEL_ROW_HEIGHT_FACTOR = 20;
            public static int UNIT_OFFSET_LENGTH = 7;
            public static int[] UNIT_OFFSET_MAP = new int[] { 0, 36, 73, 109, 146, 182, 219 };

            public static int pixel2WidthUnits(int pxs)
            {
                int widthUnits = (short)(EXCEL_COLUMN_WIDTH_FACTOR * (pxs / UNIT_OFFSET_LENGTH));
                widthUnits += UNIT_OFFSET_MAP[(pxs % UNIT_OFFSET_LENGTH)];
                return widthUnits;
            }

            public static int widthUnits2Pixel(short widthUnits)
            {
                int pixels = (widthUnits / EXCEL_COLUMN_WIDTH_FACTOR) * UNIT_OFFSET_LENGTH;
                int offsetWidthUnits = widthUnits % EXCEL_COLUMN_WIDTH_FACTOR;
                pixels += (int)Math.Floor((float)offsetWidthUnits / ((float)EXCEL_COLUMN_WIDTH_FACTOR / UNIT_OFFSET_LENGTH));
                return pixels;
            }

            public static int heightUnits2Pixel(short heightUnits)
            {
                int pixels = (heightUnits / EXCEL_ROW_HEIGHT_FACTOR);
                int offsetWidthUnits = heightUnits % EXCEL_ROW_HEIGHT_FACTOR;
                pixels += (int)Math.Floor((float)offsetWidthUnits / ((float)EXCEL_ROW_HEIGHT_FACTOR / UNIT_OFFSET_LENGTH));
                return pixels;
            }
        }
        private static void CopyCell(ICell source, ISheet destinationSheet, int offset)
        {
            if (source != null)
            {
                var h = PixelUtil.heightUnits2Pixel((short)source.Row.Height);
                var w = PixelUtil.widthUnits2Pixel((short)source.Row.Sheet.GetColumnWidth(source.ColumnIndex));

                if (destinationSheet.GetRow(source.RowIndex + offset) == null)
                {
                    destinationSheet.CreateRow(source.RowIndex + offset);
                }
                IRow destinationRow = destinationSheet.GetRow(source.RowIndex + offset);
                destinationRow.Height = (short)h;
                ICell destination = destinationRow.GetCell(source.ColumnIndex);
                if (destination == null)
                {
                    destinationRow.CreateCell(source.ColumnIndex);
                    var width = source.Row.Sheet.GetColumnWidth(source.ColumnIndex);
                    destinationSheet.SetColumnWidth(source.ColumnIndex, w);
                    destination = destinationRow.GetCell(source.ColumnIndex);
                }

                //you can comment these out if you don't want to copy the style ...
                // Copy style from old cell and apply to new cell
                ICellStyle newCellStyle = destinationSheet.Workbook.CreateCellStyle();
                newCellStyle.CloneStyleFrom(source.CellStyle);
                destination.CellStyle = newCellStyle;

                destination.CellComment = source.CellComment;
                //destination.CellStyle = source.CellStyle;
                //destination.Hyperlink = source.Hyperlink;

                switch (source.CellType)
                {
                    case CellType.Formula:
                        destination.CellFormula = source.CellFormula; break;
                    case CellType.Numeric:
                        destination.SetCellValue(source.NumericCellValue); break;
                    case CellType.String:
                        destination.SetCellValue(source.StringCellValue); break;
                }
            }
        }
        public static byte[] CreateExcelFromTemplate(string templateName, List<nDataEntry> Data)
        {
            string extension = Path.GetExtension(templateName).ToLower();
            IWorkbook template;
            NPOI.SS.SpreadsheetVersion ExcelVersion;
            using (var tmpFile = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", templateName), FileMode.Open, FileAccess.Read))
            {
                if (extension == ".xls")
                {
                    template = new HSSFWorkbook(tmpFile);
                    ExcelVersion = NPOI.SS.SpreadsheetVersion.EXCEL97;
                }
                else
                {
                    template = new XSSFWorkbook(tmpFile);
                    ExcelVersion = NPOI.SS.SpreadsheetVersion.EXCEL2007;
                }
            }
            string sheetName = "";
            List<int> nRowNum = new List<int>();
            List<DataEntry> staticVariants = Data.Find(x => x.мнИмя == "" && x.мнЗначения != null).мнЗначения[0];
            foreach (var entries in Data.Where(x => x.мнИмя != "" && x.мнЗначения != null))
            {
                int namedCellIdx = template.GetNameIndex(entries.мнИмя);
                IName aNamedCell = template.GetNameAt(namedCellIdx);

                AreaReference aref = new AreaReference(aNamedCell.RefersToFormula, ExcelVersion);
                nRowNum.Add(aref.FirstCell.Row);
                sheetName = aref.FirstCell.SheetName;
            }
            ISheet s;
            if (string.IsNullOrEmpty(sheetName))
                s = template.GetSheetAt(0);
            else
                s = template.GetSheet(sheetName);
            for (int i = s.FirstRowNum; i <= s.LastRowNum; i++)
            {
                if (!nRowNum.Contains(i))
                {
                    IRow row = s.GetRow(i);
                    if ((row != null) && (row.Cells.Count > 0))
                    {
                        for (int j = row.FirstCellNum; j <= row.LastCellNum; j++)
                        {
                            ICell cell = row.GetCell(j);
                            if (cell != null && cell.CellType == CellType.String)
                            {
                                string cellText = cell.StringCellValue; //GetStringCellValue(cell);
                                var variants = Regex.Matches(cellText, @"(\A|\s*)\{(\w+)\}(\s*|\z)")
                                .Cast<Match>()
                                .Select(m => m.Value.Trim());
                                foreach (string variant in variants)
                                {
                                    DataEntry entry = staticVariants.Find(x => "{" + x.Имя + "}" == variant);
                                    if (entry != null)
                                    {
                                        cellText = cellText.Replace(variant, entry.Значение);
                                        //cell.SetCellValue(cellText.Replace(variant, entry.Значение));
                                    }
                                }
                                cell.SetCellValue(cellText);
                            }
                        }
                    }
                }
            }

            foreach (var entries in Data.Where(x => x.мнИмя != "" && x.мнЗначения != null))
            {
                int namedCellIdx = template.GetNameIndex(entries.мнИмя);
                IName aNamedCell = template.GetNameAt(namedCellIdx);

                AreaReference aref = new AreaReference(aNamedCell.RefersToFormula, ExcelVersion);
                CopyRow(template, s, aref.FirstCell.Row, entries.мнЗначения);
                IRow row = s.GetRow(aref.FirstCell.Row);
                if (row != null)
                {
                    s.RemoveRow(row);
                    row = s.CreateRow(aref.FirstCell.Row);
                    row.ZeroHeight = true;
                }
            }

            using (var stream = new MemoryStream())
            {
                //ExcelToHtmlConverter excelToHtmlConverter = new ExcelToHtmlConverter();
                //excelToHtmlConverter.
                template.Write(stream);
                return stream.ToArray();
            }
        }
        public static byte[] CreateExcel(this IOrderedEnumerable<ДолгиТаблица> результаты, string extension, bool сортировкаПокупателиПоставщики)
        {
            bool IsHSSF = extension.Trim().ToLower() == "xls";
            IWorkbook workbook;
            IFont regularFont;
            IFont smallFont;
            Dictionary<string, ICellStyle> wbStyles = new Dictionary<string, ICellStyle>();
            ICellStyle regularCellStyle;

            ICellStyle regularCellStyleColor0;

            ICellStyle smallCellStyle;
            ICellStyle smallCellStyleRightAlignment;
            ICellStyle smallCellStyleCenterAlignment;

            ICellStyle smallCellStyleLevel0;
            ICellStyle smallCellStyleRightAlignmentLevel0;
            ICellStyle smallCellStyleCenterAlignmentLevel0;

            ICellStyle smallCellStyleLevel1;
            ICellStyle smallCellStyleRightAlignmentLevel1;
            ICellStyle smallCellStyleCenterAlignmentLevel1;

            ICellStyle smallCellStyleLevel2;
            ICellStyle smallCellStyleRightAlignmentLevel2;
            ICellStyle smallCellStyleCenterAlignmentLevel2;

            ICellStyle smallCellStyleLevel3;
            ICellStyle smallCellStyleRightAlignmentLevel3;
            ICellStyle smallCellStyleCenterAlignmentLevel3;

            ICellStyle smallCellStyleLevel4;
            ICellStyle smallCellStyleRightAlignmentLevel4;
            ICellStyle smallCellStyleCenterAlignmentLevel4;

            if (IsHSSF)
            {
                workbook = new HSSFWorkbook();

                regularFont = (HSSFFont)workbook.CreateFont();
                smallFont = (HSSFFont)workbook.CreateFont();

                regularCellStyle = (HSSFCellStyle)workbook.CreateCellStyle();
                regularCellStyleColor0 = (HSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyle = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignment = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignment = (HSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel0 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel0 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel0 = (HSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel1 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel1 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel1 = (HSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel2 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel2 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel2 = (HSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel3 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel3 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel3 = (HSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel4 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel4 = (HSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel4 = (HSSFCellStyle)workbook.CreateCellStyle();

            }
            else
            {
                workbook = new XSSFWorkbook();

                regularFont = (XSSFFont)workbook.CreateFont();
                smallFont = (XSSFFont)workbook.CreateFont();

                regularCellStyle = (XSSFCellStyle)workbook.CreateCellStyle();
                regularCellStyleColor0 = (XSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyle = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignment = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignment = (XSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel0 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel0 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel0 = (XSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel1 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel1 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel1 = (XSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel2 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel2 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel2 = (XSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel3 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel3 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel3 = (XSSFCellStyle)workbook.CreateCellStyle();

                smallCellStyleLevel4 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleRightAlignmentLevel4 = (XSSFCellStyle)workbook.CreateCellStyle();
                smallCellStyleCenterAlignmentLevel4 = (XSSFCellStyle)workbook.CreateCellStyle();

            }
            //key - 1 Font, 2 Alignment, 3 Color 
            wbStyles.Add("R", regularCellStyle);
            wbStyles.Add("R0", regularCellStyleColor0);
            wbStyles.Add("S", smallCellStyle);
            wbStyles.Add("SR", smallCellStyleRightAlignment);
            wbStyles.Add("SC", smallCellStyleCenterAlignment);
            wbStyles.Add("S0", smallCellStyleLevel0);
            wbStyles.Add("SR0", smallCellStyleRightAlignmentLevel0);
            wbStyles.Add("SC0", smallCellStyleCenterAlignmentLevel0);
            wbStyles.Add("S1", smallCellStyleLevel1);
            wbStyles.Add("SR1", smallCellStyleRightAlignmentLevel1);
            wbStyles.Add("SC1", smallCellStyleCenterAlignmentLevel1);
            wbStyles.Add("S2", smallCellStyleLevel2);
            wbStyles.Add("SR2", smallCellStyleRightAlignmentLevel2);
            wbStyles.Add("SC2", smallCellStyleCenterAlignmentLevel2);
            wbStyles.Add("S3", smallCellStyleLevel3);
            wbStyles.Add("SR3", smallCellStyleRightAlignmentLevel3);
            wbStyles.Add("SC3", smallCellStyleCenterAlignmentLevel3);
            wbStyles.Add("S4", smallCellStyleLevel4);
            wbStyles.Add("SR4", smallCellStyleRightAlignmentLevel4);
            wbStyles.Add("SC4", smallCellStyleCenterAlignmentLevel4);

            regularFont.FontHeightInPoints = 10;
            regularFont.FontName = "Arial";

            smallFont.FontHeightInPoints = 8;
            smallFont.FontName = "Arial";

            regularCellStyle.SetFont(regularFont);
            regularCellStyle.BorderLeft = BorderStyle.Medium;
            regularCellStyle.BorderTop = BorderStyle.Medium;
            regularCellStyle.BorderRight = BorderStyle.Medium;
            regularCellStyle.BorderBottom = BorderStyle.Medium;
            regularCellStyle.VerticalAlignment = VerticalAlignment.Center;
            regularCellStyle.Alignment = HorizontalAlignment.Center;
            regularCellStyle.WrapText = true;

            regularCellStyleColor0.CloneStyleFrom(regularCellStyle);

            smallCellStyle.SetFont(smallFont);
            smallCellStyle.BorderLeft = BorderStyle.Thin;
            smallCellStyle.BorderTop = BorderStyle.Thin;
            smallCellStyle.BorderRight = BorderStyle.Thin;
            smallCellStyle.BorderBottom = BorderStyle.Thin;
            smallCellStyle.VerticalAlignment = VerticalAlignment.Center;
            smallCellStyle.Alignment = HorizontalAlignment.Left;
            smallCellStyle.WrapText = true;

            smallCellStyleLevel0.CloneStyleFrom(smallCellStyle);
            smallCellStyleLevel1.CloneStyleFrom(smallCellStyle);
            smallCellStyleLevel2.CloneStyleFrom(smallCellStyle);
            smallCellStyleLevel3.CloneStyleFrom(smallCellStyle);
            smallCellStyleLevel4.CloneStyleFrom(smallCellStyle);
            if (IsHSSF)
            {
                regularCellStyleColor0.FillForegroundColor = (setColor((HSSFWorkbook)workbook, (byte)127, (byte)255, (byte)212, 58)).Indexed;
                smallCellStyleLevel0.FillForegroundColor = (setColor((HSSFWorkbook)workbook, (byte)222, (byte)184, (byte)135, 53)).Indexed;
                smallCellStyleLevel1.FillForegroundColor = (setColor((HSSFWorkbook)workbook, (byte)143, (byte)188, (byte)143, 54)).Indexed;
                smallCellStyleLevel2.FillForegroundColor = (setColor((HSSFWorkbook)workbook, (byte)240, (byte)230, (byte)140, 55)).Indexed;
                smallCellStyleLevel3.FillForegroundColor = (setColor((HSSFWorkbook)workbook, (byte)230, (byte)255, (byte)230, 56)).Indexed;
                smallCellStyleLevel4.FillForegroundColor = (setColor((HSSFWorkbook)workbook, (byte)248, (byte)236, (byte)242, 57)).Indexed;
            }
            else
            {
                (regularCellStyleColor0 as XSSFCellStyle).FillForegroundXSSFColor = new XSSFColor(new byte[] { 127, 255, 212 });
                (smallCellStyleLevel0 as XSSFCellStyle).FillForegroundXSSFColor = new XSSFColor(new byte[] { 222, 184, 135 });
                (smallCellStyleLevel1 as XSSFCellStyle).FillForegroundXSSFColor = new XSSFColor(new byte[] { 143, 188, 143 });
                (smallCellStyleLevel2 as XSSFCellStyle).FillForegroundXSSFColor = new XSSFColor(new byte[] { 240, 230, 140 });
                (smallCellStyleLevel3 as XSSFCellStyle).FillForegroundXSSFColor = new XSSFColor(new byte[] { 230, 255, 230 });
                (smallCellStyleLevel4 as XSSFCellStyle).FillForegroundXSSFColor = new XSSFColor(new byte[] { 248, 236, 242 });
            }
            regularCellStyleColor0.FillPattern = FillPattern.SolidForeground;
            smallCellStyleLevel0.FillPattern = FillPattern.SolidForeground;
            smallCellStyleLevel1.FillPattern = FillPattern.SolidForeground;
            smallCellStyleLevel2.FillPattern = FillPattern.SolidForeground;
            smallCellStyleLevel3.FillPattern = FillPattern.SolidForeground;
            smallCellStyleLevel4.FillPattern = FillPattern.SolidForeground;

            smallCellStyleRightAlignment.CloneStyleFrom(smallCellStyle);
            smallCellStyleRightAlignment.Alignment = HorizontalAlignment.Right;
            smallCellStyleCenterAlignment.CloneStyleFrom(smallCellStyle);
            smallCellStyleCenterAlignment.Alignment = HorizontalAlignment.Center;

            smallCellStyleRightAlignmentLevel0.CloneStyleFrom(smallCellStyleLevel0);
            smallCellStyleRightAlignmentLevel0.Alignment = HorizontalAlignment.Right;
            smallCellStyleCenterAlignmentLevel0.CloneStyleFrom(smallCellStyleLevel0);
            smallCellStyleCenterAlignmentLevel0.Alignment = HorizontalAlignment.Center;

            smallCellStyleRightAlignmentLevel1.CloneStyleFrom(smallCellStyleLevel1);
            smallCellStyleRightAlignmentLevel1.Alignment = HorizontalAlignment.Right;
            smallCellStyleCenterAlignmentLevel1.CloneStyleFrom(smallCellStyleLevel1);
            smallCellStyleCenterAlignmentLevel1.Alignment = HorizontalAlignment.Center;

            smallCellStyleRightAlignmentLevel2.CloneStyleFrom(smallCellStyleLevel2);
            smallCellStyleRightAlignmentLevel2.Alignment = HorizontalAlignment.Right;
            smallCellStyleCenterAlignmentLevel2.CloneStyleFrom(smallCellStyleLevel2);
            smallCellStyleCenterAlignmentLevel2.Alignment = HorizontalAlignment.Center;

            smallCellStyleRightAlignmentLevel3.CloneStyleFrom(smallCellStyleLevel3);
            smallCellStyleRightAlignmentLevel3.Alignment = HorizontalAlignment.Right;
            smallCellStyleCenterAlignmentLevel3.CloneStyleFrom(smallCellStyleLevel3);
            smallCellStyleCenterAlignmentLevel3.Alignment = HorizontalAlignment.Center;

            smallCellStyleRightAlignmentLevel4.CloneStyleFrom(smallCellStyleLevel4);
            smallCellStyleRightAlignmentLevel4.Alignment = HorizontalAlignment.Right;
            smallCellStyleCenterAlignmentLevel4.CloneStyleFrom(smallCellStyleLevel4);
            smallCellStyleCenterAlignmentLevel4.Alignment = HorizontalAlignment.Center;

            ISheet Sheet = workbook.CreateSheet("ОтчётДолгиКонтрагентов");
            IRow HeaderRow0 = Sheet.CreateRow(0);
            IRow HeaderRow1 = Sheet.CreateRow(1);
            IRow HeaderRow2 = Sheet.CreateRow(2);

            CreateOrUpdateCell(HeaderRow1, 1, "Менеджер / Группа / Контрагент", regularCellStyle);
            CreateOrUpdateCell(HeaderRow2, 1, "Менеджер / Группа / Контрагент", regularCellStyle);
            Sheet.AddMergedRegion(new CellRangeAddress(1, 2, 1, 1));
            CreateOrUpdateCell(HeaderRow1, 2, "Сумма долга общая", regularCellStyle);
            CreateOrUpdateCell(HeaderRow2, 2, "Сумма долга общая", regularCellStyle);
            Sheet.AddMergedRegion(new CellRangeAddress(1, 2, 2, 2));
            int startColumnПокупатели = 10;
            int startColumnПоставщики = 3;
            int длинаПоставщики = 6;
            int длинаПокупатели = 8;
            int startDataПоставщики = startColumnПоставщики;
            int startEmptyData = startColumnПокупатели;
            int startDataПокупатели = startEmptyData + 2;
            int startColumnПокупателиДокумент = 10;
            int startColumnПоставщикиДокумент = 1;
            if (сортировкаПокупателиПоставщики)
            {
                startColumnПокупатели = 3;
                startColumnПоставщики = 10;
                длинаПоставщики = 8;
                длинаПокупатели = 6;
                startDataПокупатели = startColumnПокупатели;
                startEmptyData = startColumnПоставщики;
                startDataПоставщики = startEmptyData + 2;
                startColumnПокупателиДокумент = 1;
                startColumnПоставщикиДокумент = 10;
            }
            CreateOrUpdateCell(HeaderRow1, startColumnПокупатели, "Покупатели", (длинаПокупатели > длинаПоставщики ? regularCellStyle : regularCellStyleColor0));
            for (int i = 1; i < длинаПокупатели; i++)
                CreateOrUpdateCell(HeaderRow1, startColumnПокупатели + i, "Покупатели", (длинаПокупатели > длинаПоставщики ? regularCellStyle : regularCellStyleColor0));
            Sheet.AddMergedRegion(new CellRangeAddress(1, 1, startColumnПокупатели, startColumnПокупатели + длинаПокупатели - 1));
            int curColumn = startColumnПокупатели;
            if (длинаПокупатели > длинаПоставщики)
            {
                CreateOrUpdateCell(HeaderRow2, startColumnПокупатели, "", regularCellStyle);
                CreateOrUpdateCell(HeaderRow2, startColumnПокупатели + 1, "", regularCellStyle);
                Sheet.AddMergedRegion(new CellRangeAddress(2, 2, startColumnПокупатели, startColumnПокупатели + 1));
                curColumn = startColumnПокупатели + 2;
            }
            CreateOrUpdateCell(HeaderRow2, curColumn, "Сумма долга", (длинаПокупатели > длинаПоставщики ? regularCellStyle : regularCellStyleColor0));
            CreateOrUpdateCell(HeaderRow2, curColumn + 1, "Сумма долга", (длинаПокупатели > длинаПоставщики ? regularCellStyle : regularCellStyleColor0));
            Sheet.AddMergedRegion(new CellRangeAddress(2, 2, curColumn, curColumn + 1));
            CreateOrUpdateCell(HeaderRow2, curColumn + 2, "Сумма / ТДЗ", (длинаПокупатели > длинаПоставщики ? regularCellStyle : regularCellStyleColor0));
            CreateOrUpdateCell(HeaderRow2, curColumn + 3, "Сумма / ТДЗ", (длинаПокупатели > длинаПоставщики ? regularCellStyle : regularCellStyleColor0));
            Sheet.AddMergedRegion(new CellRangeAddress(2, 2, curColumn + 2, curColumn + 3));
            CreateOrUpdateCell(HeaderRow2, curColumn + 4, "Сумма / ПДЗ", (длинаПокупатели > длинаПоставщики ? regularCellStyle : regularCellStyleColor0));
            CreateOrUpdateCell(HeaderRow2, curColumn + 5, "Сумма / ПДЗ", (длинаПокупатели > длинаПоставщики ? regularCellStyle : regularCellStyleColor0));
            Sheet.AddMergedRegion(new CellRangeAddress(2, 2, curColumn + 4, curColumn + 5));

            CreateOrUpdateCell(HeaderRow1, startColumnПоставщики, "Поставщики", (длинаПоставщики > длинаПокупатели ? regularCellStyle : regularCellStyleColor0));
            for (int i = 1; i < длинаПоставщики; i++)
                CreateOrUpdateCell(HeaderRow1, startColumnПоставщики + i, "Поставщики", (длинаПоставщики > длинаПокупатели ? regularCellStyle : regularCellStyleColor0));
            Sheet.AddMergedRegion(new CellRangeAddress(1, 1, startColumnПоставщики, startColumnПоставщики + длинаПоставщики - 1));
            curColumn = startColumnПоставщики;
            if (длинаПоставщики > длинаПокупатели)
            {
                CreateOrUpdateCell(HeaderRow2, startColumnПоставщики, "", regularCellStyle);
                CreateOrUpdateCell(HeaderRow2, startColumnПоставщики + 1, "", regularCellStyle);
                Sheet.AddMergedRegion(new CellRangeAddress(2, 2, startColumnПоставщики, startColumnПоставщики + 1));
                curColumn = startColumnПоставщики + 2;
            }
            CreateOrUpdateCell(HeaderRow2, curColumn, "Сумма долга", (длинаПоставщики > длинаПокупатели ? regularCellStyle : regularCellStyleColor0));
            CreateOrUpdateCell(HeaderRow2, curColumn + 1, "Сумма долга", (длинаПоставщики > длинаПокупатели ? regularCellStyle : regularCellStyleColor0));
            Sheet.AddMergedRegion(new CellRangeAddress(2, 2, curColumn, curColumn + 1));
            CreateOrUpdateCell(HeaderRow2, curColumn + 2, "Сумма / ТДЗ", (длинаПоставщики > длинаПокупатели ? regularCellStyle : regularCellStyleColor0));
            CreateOrUpdateCell(HeaderRow2, curColumn + 3, "Сумма / ТДЗ", (длинаПоставщики > длинаПокупатели ? regularCellStyle : regularCellStyleColor0));
            Sheet.AddMergedRegion(new CellRangeAddress(2, 2, curColumn + 2, curColumn + 3));
            CreateOrUpdateCell(HeaderRow2, curColumn + 4, "Сумма / ПДЗ", (длинаПоставщики > длинаПокупатели ? regularCellStyle : regularCellStyleColor0));
            CreateOrUpdateCell(HeaderRow2, curColumn + 5, "Сумма / ПДЗ", (длинаПоставщики > длинаПокупатели ? regularCellStyle : regularCellStyleColor0));
            Sheet.AddMergedRegion(new CellRangeAddress(2, 2, curColumn + 4, curColumn + 5));

            int RowIndex = 3;
            foreach (var row in результаты)
            {
                IRow CurrentRow = Sheet.CreateRow(RowIndex);
                CreateOrUpdateCell(CurrentRow, 1, row.Наименование, wbStyles["S" + row.Флаг.ToString()]);
                CreateOrUpdateCell(CurrentRow, 2, row.Долг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);

                CreateOrUpdateCell(CurrentRow, startEmptyData, "", wbStyles["S" + row.Флаг.ToString()]);
                CreateOrUpdateCell(CurrentRow, startEmptyData + 1, "", wbStyles["S" + row.Флаг.ToString()]);
                Sheet.AddMergedRegion(new CellRangeAddress(RowIndex, RowIndex, startEmptyData, startEmptyData + 1));

                CreateOrUpdateCell(CurrentRow, startDataПокупатели, row.Покупатели_Долг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                CreateOrUpdateCell(CurrentRow, startDataПокупатели + 1, row.Покупатели_Долг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                Sheet.AddMergedRegion(new CellRangeAddress(RowIndex, RowIndex, startDataПокупатели, startDataПокупатели + 1));
                CreateOrUpdateCell(CurrentRow, startDataПокупатели + 2, row.Покупатели_ТекущийДолг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                CreateOrUpdateCell(CurrentRow, startDataПокупатели + 3, row.Покупатели_ТекущийДолг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                Sheet.AddMergedRegion(new CellRangeAddress(RowIndex, RowIndex, startDataПокупатели + 2, startDataПокупатели + 3));
                CreateOrUpdateCell(CurrentRow, startDataПокупатели + 4, row.Покупатели_ПросроченныйДолг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                CreateOrUpdateCell(CurrentRow, startDataПокупатели + 5, row.Покупатели_ПросроченныйДолг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                Sheet.AddMergedRegion(new CellRangeAddress(RowIndex, RowIndex, startDataПокупатели + 4, startDataПокупатели + 5));

                CreateOrUpdateCell(CurrentRow, startDataПоставщики, row.Поставщики_Долг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                CreateOrUpdateCell(CurrentRow, startDataПоставщики + 1, row.Поставщики_Долг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                Sheet.AddMergedRegion(new CellRangeAddress(RowIndex, RowIndex, startDataПоставщики, startDataПоставщики + 1));
                CreateOrUpdateCell(CurrentRow, startDataПоставщики + 2, row.Поставщики_ТекущийДолг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                CreateOrUpdateCell(CurrentRow, startDataПоставщики + 3, row.Поставщики_ТекущийДолг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                Sheet.AddMergedRegion(new CellRangeAddress(RowIndex, RowIndex, startDataПоставщики + 2, startDataПоставщики + 3));
                CreateOrUpdateCell(CurrentRow, startDataПоставщики + 4, row.Поставщики_ПросроченныйДолг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                CreateOrUpdateCell(CurrentRow, startDataПоставщики + 5, row.Поставщики_ПросроченныйДолг.ToString(Common.ФорматЦеныСи), wbStyles["SR" + row.Флаг.ToString()]);
                Sheet.AddMergedRegion(new CellRangeAddress(RowIndex, RowIndex, startDataПоставщики + 4, startDataПоставщики + 5));

                RowIndex++;
                IRow DocRow;
                int MaxDocRow = 0;
                int DocRowIndex = 0;
                bool шапка = true;
                foreach (var doc in row.ДокументыРеализации)
                {
                    if (шапка)
                    {
                        DocRow = Sheet.CreateRow(RowIndex + DocRowIndex);
                        CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент, "Наименование", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 1, "Номер", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 2, "Дата", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 3, "Отсрочка", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 4, "Дата оплаты", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 5, "Сумма по документу", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 6, "Сумма / ТДЗ", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 7, "Сумма / ПДЗ", wbStyles["R"]);
                        шапка = false;
                        DocRowIndex++;
                    }
                    DocRow = Sheet.CreateRow(RowIndex + DocRowIndex);
                    string ColorKey = "";
                    if (doc.СуммаДокумента != doc.СуммаПросроченногоДолга + doc.СуммаТекущегоДолга)
                        ColorKey = "3";
                    else if (doc.СуммаПросроченногоДолга > 0)
                        ColorKey = "4";
                    CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент, doc.DocНазвание, wbStyles["S" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 1, doc.DocNo, wbStyles["S" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 2, doc.DocDate.ToString("dd.MM.yyyy"), wbStyles["SC" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 3, doc.ОтсрочкаДней.ToString(), wbStyles["SC" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 4, doc.ДатаОплаты.ToString("dd.MM.yyyy"), wbStyles["SC" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 5, doc.СуммаДокумента.ToString(Common.ФорматЦеныСи), wbStyles["SR" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 6, doc.СуммаТекущегоДолга.ToString(Common.ФорматЦеныСи), wbStyles["SR" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПокупателиДокумент + 7, doc.СуммаПросроченногоДолга.ToString(Common.ФорматЦеныСи), wbStyles["SR" + ColorKey]);

                    DocRowIndex++;
                }
                MaxDocRow = DocRowIndex;
                DocRowIndex = 0;
                шапка = true;
                foreach (var doc in row.ДокументыПоступления)
                {
                    if (шапка)
                    {
                        if (DocRowIndex < MaxDocRow)
                            DocRow = Sheet.GetRow(RowIndex + DocRowIndex);
                        else
                            DocRow = Sheet.CreateRow(RowIndex + DocRowIndex);
                        CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент, "Наименование", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 1, "Номер", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 2, "Дата", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 3, "Отсрочка", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 4, "Дата оплаты", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 5, "Сумма по документу", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 6, "Сумма / ТДЗ", wbStyles["R"]);
                        CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 7, "Сумма / ПДЗ", wbStyles["R"]);
                        шапка = false;
                        DocRowIndex++;
                    }
                    if (DocRowIndex < MaxDocRow)
                        DocRow = Sheet.GetRow(RowIndex + DocRowIndex);
                    else
                        DocRow = Sheet.CreateRow(RowIndex + DocRowIndex);
                    string ColorKey = "";
                    if (doc.СуммаДокумента != doc.СуммаПросроченногоДолга + doc.СуммаТекущегоДолга)
                        ColorKey = "3";
                    else if (doc.СуммаПросроченногоДолга > 0)
                        ColorKey = "4";
                    CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент, doc.DocНазвание, wbStyles["S" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 1, doc.DocNo, wbStyles["S" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 2, doc.DocDate.ToString("dd.MM.yyyy"), wbStyles["SC" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 3, doc.ОтсрочкаДней.ToString(), wbStyles["SC" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 4, doc.ДатаОплаты.ToString("dd.MM.yyyy"), wbStyles["SC" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 5, doc.СуммаДокумента.ToString(Common.ФорматЦеныСи), wbStyles["SR" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 6, doc.СуммаТекущегоДолга.ToString(Common.ФорматЦеныСи), wbStyles["SR" + ColorKey]);
                    CreateOrUpdateCell(DocRow, startColumnПоставщикиДокумент + 7, doc.СуммаПросроченногоДолга.ToString(Common.ФорматЦеныСи), wbStyles["SR" + ColorKey]);

                    DocRowIndex++;
                }
                MaxDocRow = Math.Max(MaxDocRow, DocRowIndex);
                RowIndex = RowIndex + MaxDocRow;
            }

            Sheet.SetColumnWidth(0, 500);
            Sheet.SetColumnWidth(1, 7000);
            Sheet.SetColumnWidth(2, 2900);
            Sheet.SetColumnWidth(3, 2500);
            Sheet.SetColumnWidth(4, 1500);
            Sheet.SetColumnWidth(5, 2500);
            Sheet.SetColumnWidth(6, 2500);
            Sheet.SetColumnWidth(7, 2500);
            Sheet.SetColumnWidth(8, 2500);
            Sheet.SetColumnWidth(9, 500);
            Sheet.SetColumnWidth(10, 7000);
            Sheet.SetColumnWidth(11, 2900);
            Sheet.SetColumnWidth(12, 2500);
            Sheet.SetColumnWidth(13, 1500);
            Sheet.SetColumnWidth(14, 2500);
            Sheet.SetColumnWidth(15, 2500);
            Sheet.SetColumnWidth(16, 2500);
            Sheet.SetColumnWidth(17, 2500);
            //// Auto sized all the affected columns
            //int lastColumNum = Sheet.GetRow(0).LastCellNum;
            //for (int i = 0; i <= lastColumNum; i++)
            //{
            //    //Sheet.AutoSizeColumn(i);
            //}
            GC.Collect();
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }
    }
}
