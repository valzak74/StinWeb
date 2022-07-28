using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace ExcelHelper
{
    public static class Extensions
    {
        public static int CreateColumnWithWidth(this ISheet sheet, int index, int width)
        {
            sheet.SetColumnWidth(index, width);
            return index;
        }
        public static void SetValue(this ISheet ws, ICellStyle cellStyle, int row, int column, object value)
        {
            SetValue(ws, cellStyle, row, row, column, column, value);
        }
        public static void SetValue(this ISheet ws, ICellStyle cellStyle, int rowStart, int rowEnd, int columnStart, int columnEnd, object value)
        {
            for (int i = rowStart; i <= rowEnd; i++)
            {
                IRow rowTable = ws.GetRow(i);
                if (rowTable == null)
                    rowTable = ws.CreateRow(i);
                for (int y = columnStart; y <= columnEnd; y++)
                {
                    ICell cellGroup = rowTable.CreateCell(y);
                    if (cellStyle != null)
                        cellGroup.CellStyle = cellStyle;
                    if (value == null)
                        cellGroup.SetCellValue("");
                    else
                    {
                        var valueType = value.GetType();
                        if (valueType == typeof(string))
                            cellGroup.SetCellValue((string)value);
                        else if ((valueType == typeof(int)) ||
                            (valueType == typeof(double)) ||
                            (valueType == typeof(decimal)) ||
                            (valueType == typeof(float)))
                            cellGroup.SetCellValue(Convert.ToDouble(value));
                        else if (valueType == typeof(DateTime))
                            cellGroup.SetCellValue((DateTime)value);
                        else if (valueType == typeof(bool))
                            cellGroup.SetCellValue((bool)value);
                        else
                            cellGroup.SetCellValue(value.ToString());
                    }
                }
            }
            if ((rowEnd > rowStart) | (columnEnd > columnStart))
            {
                CellRangeAddress cra = new CellRangeAddress(rowStart, rowEnd, columnStart, columnEnd);
                ws.AddMergedRegion(cra);
            }
        }
        public static void SetFormula(this ISheet ws, ICellStyle cellStyle, int rowStart, int rowEnd, int columnStart, int columnEnd, string value)
        {
            for (int i = rowStart; i <= rowEnd; i++)
            {
                IRow rowTable = ws.GetRow(i);
                if (rowTable == null)
                    rowTable = ws.CreateRow(i);
                for (int y = columnStart; y <= columnEnd; y++)
                {
                    ICell cellGroup = rowTable.CreateCell(y);
                    if (cellStyle != null)
                        cellGroup.CellStyle = cellStyle;
                    cellGroup.SetCellType(CellType.Formula);
                    cellGroup.SetCellFormula(value);
                }
            }
            if ((rowEnd > rowStart) | (columnEnd > columnStart))
            {
                CellRangeAddress cra = new CellRangeAddress(rowStart, rowEnd, columnStart, columnEnd);
                ws.AddMergedRegion(cra);
            }
        }
        public static string GetStringValue(this ICell cell)
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
        public static HSSFColor SetColor(this HSSFWorkbook workbook, byte r, byte g, byte b, short index)
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
    }
}