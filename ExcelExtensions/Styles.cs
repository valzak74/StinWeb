using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelHelper
{
    public static class Styles
    {
        public static IFont FontArial(this IWorkbook workbook, double size = 8, bool bold = false, short color = 0)
        {
            var result = workbook.CreateFont();
            result.FontName = HSSFFont.FONT_ARIAL;
            result.FontHeightInPoints = size;
            result.IsBold = bold;
            if (color != 0)
                result.Color = color;
            return result;
        }
        public static ICellStyle StyleHeader(this IWorkbook workbook, IFont font)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.Alignment = HorizontalAlignment.Center;
            style.BorderRight = BorderStyle.Medium;
            style.BorderLeft = BorderStyle.Medium;
            style.BorderTop = BorderStyle.Medium;
            style.BorderBottom = BorderStyle.Medium;
            style.FillPattern = FillPattern.SolidForeground;
            style.FillForegroundColor = HSSFColor.Grey25Percent.Index;
            style.SetFont(font);
            return style;
        }
        public static ICellStyle StyleValue(this IWorkbook workbook, IFont font)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.Alignment = HorizontalAlignment.Left;
            style.BorderRight = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.SetFont(font);
            return style;
        }
        public static ICellStyle StyleValueMarked(this IWorkbook workbook, IFont font, short backgroundColorIndex = 0)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.Alignment = HorizontalAlignment.Left;
            style.BorderRight = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            if (backgroundColorIndex > 0)
            {
                style.FillPattern = FillPattern.SolidForeground;//.AltBars.LessDots;
                style.FillForegroundColor = backgroundColorIndex;
            }
            style.SetFont(font);
            return style;
        }
        public static ICellStyle StyleValueNumber(this IWorkbook workbook, IFont font)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.Alignment = HorizontalAlignment.Right;
            style.BorderRight = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.SetFont(font);
            style.DataFormat = workbook.CreateDataFormat().GetFormat("# ##0.000;-# ##0.000;;@");
            return style;
        }
        public static ICellStyle StyleValueNumberMarked(this IWorkbook workbook, IFont font, short backgroundColorIndex = 0)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.Alignment = HorizontalAlignment.Right;
            style.BorderRight = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            if (backgroundColorIndex > 0)
            {
                style.FillPattern = FillPattern.SolidForeground;//.AltBars.LessDots;
                style.FillForegroundColor = backgroundColorIndex;
            }
            style.SetFont(font);
            style.DataFormat = workbook.CreateDataFormat().GetFormat("# ##0.000;-# ##0.000;;@");
            return style;
        }
        public static ICellStyle StyleValueNumber(this IWorkbook workbook, IFont font, short decimalPoints, short backgroundColorIndex = 0)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.Alignment = HorizontalAlignment.Right;
            style.BorderRight = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            if (backgroundColorIndex > 0)
            {
                style.FillPattern = FillPattern.SolidForeground;//.AltBars.LessDots;
                style.FillForegroundColor = backgroundColorIndex;
            }
            style.SetFont(font);
            string decimalTail = decimalPoints > 0 ? ".".PadRight(decimalPoints + 1,'0'): "";
            style.DataFormat = workbook.CreateDataFormat().GetFormat("# ##0" + decimalTail + ";-# ##0" + decimalTail + ";;@");
            return style;
        }
        public static ICellStyle StyleValueNumberMoney(this IWorkbook workbook, IFont font)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.Alignment = HorizontalAlignment.Right;
            style.BorderRight = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.SetFont(font);
            style.DataFormat = workbook.CreateDataFormat().GetFormat("# ##0.00;-# ##0.00;;@");
            return style;
        }
    }
}
