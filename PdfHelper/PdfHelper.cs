using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Fonts;
using System.Diagnostics;
using MigraDocCore.DocumentObjectModel;
//using MigraDocCore.Rendering;
//using MigraDocCore.DocumentObjectModel.Tables;
using System.Text.RegularExpressions;
//using PdfSharpCore.Utils;
//using MigraDocCore.DocumentObjectModel.Shapes;
using PdfSharpCore.Drawing.BarCodes;
using PdfSharpCore.Utils;
using MigraDocCore.Rendering;
using MigraDocCore.DocumentObjectModel.Shapes;
using BarcodeLib;
using System.Drawing;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace PdfHelper
{
    public class PdfFunctions
    {
        private static readonly PdfFunctions instance = new PdfFunctions();
        private PdfFunctions() => GlobalFontSettings.FontResolver = new CustomFontResolver();
        public static PdfFunctions Instance => instance;
        public byte[] MergePdf(List<byte[]> files)
        {
            using var outputStream = new MemoryStream();
            using PdfDocument output = new PdfDocument(outputStream);
            foreach (var f in files)
            {
                using Stream stream = new MemoryStream(f);
                stream.Position = 0;
                using PdfDocument pdfFile = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
                foreach (var page in pdfFile.Pages)
                    output.AddPage(page);
            }
            output.Save(outputStream);
            return outputStream.ToArray();
        }
        public byte[] GetPdfFromImage(byte[] file, double marginHeight = 4, double marginWidth = 4)
        {
            using var outputStream = new MemoryStream();
            using PdfDocument document = new PdfDocument(outputStream);
            PdfPage page = document.AddPage();
            using Stream stream = new MemoryStream(file);
            stream.Position = 0;
            using XImage img = XImage.FromStream(() => stream);
            page.Width = img.PointWidth + marginWidth;
            page.Height = img.PointHeight + marginHeight;
            using XGraphics gfx = XGraphics.FromPdfPage(page, XPageDirection.Downwards);
            gfx.DrawImage(img, marginWidth / 2, marginHeight / 2);
            document.Save(outputStream);
            return outputStream.ToArray();
        }
        public byte[] GetPdfFromImage(List<byte[]> files, double marginHeight = 4, double marginWidth = 4)
        {
            using var outputStream = new MemoryStream();
            using PdfDocument document = new PdfDocument(outputStream);
            foreach (var f in files)
            {
                PdfPage page = document.AddPage();
                using Stream stream = new MemoryStream(f);
                stream.Position = 0;
                using XImage img = XImage.FromStream(() => stream);
                page.Width = img.PointWidth + marginWidth;
                page.Height = img.PointHeight + marginHeight;
                using XGraphics gfx = XGraphics.FromPdfPage(page, XPageDirection.Downwards);
                gfx.DrawImage(img, marginWidth / 2, marginHeight / 2);
            }
            document.Save(outputStream);
            return outputStream.ToArray();
        }
        public byte[] GenerateBarcode128(string barcodeText, int height = 0, int width = 0, bool withLabel = false)
        {
            if (!string.IsNullOrEmpty(barcodeText))
            {
                BarcodeLib.Barcode barcode = new BarcodeLib.Barcode();
                if (height > 0)
                    barcode.Height = height;
                if (width > 0)
                    barcode.Width = width;
                if (withLabel)
                {
                    barcode.IncludeLabel = true;
                    barcode.LabelPosition = LabelPositions.BOTTOMCENTER;
                }
                System.Drawing.Image img = barcode.Encode(TYPE.CODE128, barcodeText, System.Drawing.Color.Black, System.Drawing.Color.Transparent);
                using var strm = new MemoryStream();
                img.Save(strm, System.Drawing.Imaging.ImageFormat.Png);
                return strm.ToArray();
            }
            return null;
        }
        public byte[] ProductSticker (string barcodeText, string name, string vendor, string color = "белый", string size = "1")
        {
            using var outputStream = new MemoryStream();
            using PdfDocument document = new PdfDocument();
            //GlobalFontSettings.FontResolver = new CustomFontResolver();

            ////create pdf header
            document.Info.Title = "Product sticker";
            document.Info.Author = "Valentin Zakharov";
            document.Info.Subject = "Sticker";
            document.Info.Keywords = "Product, Barcode, Vendor";
            document.Info.CreationDate = DateTime.Now;

            ////create new pdf page
            PdfPage page = document.AddPage();
            //page.Size = PdfSharpCore.PageSize.A4;
            page.Width = XUnit.FromMillimeter(58);
            page.Height = XUnit.FromMillimeter(40);

            using XGraphics gfx = XGraphics.FromPdfPage(page);
            gfx.MUH = PdfFontEncoding.Unicode;

            if (!string.IsNullOrEmpty(barcodeText))
            {
                BarcodeLib.Barcode barcode = new BarcodeLib.Barcode();
                System.Drawing.Image img = barcode.Encode(TYPE.CODE128, barcodeText, System.Drawing.Color.Black, System.Drawing.Color.Transparent);
                using var strm = new MemoryStream();
                img.Save(strm, System.Drawing.Imaging.ImageFormat.Png);
                strm.Position = 0;
                XImage xBarcode = XImage.FromStream(() => strm);
                gfx.DrawImage(xBarcode, new XRect(0, 0, page.Width, 45));
            }
            //PdfSharpCore.Drawing.BarCodes.Code128 c128 = new PdfSharpCore.Drawing.BarCodes.Code128("4650001850591", new XSize(200, 50), CodeDirection.LeftToRight);
            //c128.TextLocation = TextLocation.BelowEmbedded;
            //gfx.DrawBarCode(c128, XBrushes.Navy, new XPoint(10, 10));

            Document doc = new Document();

            Section sec = doc.AddSection();

            //// Add a single paragraph with some text and format information.
            //Paragraph paragBarcode = sec.AddParagraph();
            //paragBarcode.Format.Alignment = ParagraphAlignment.Center;
            //paragBarcode.Format.Font.Name = "Code 128";
            //paragBarcode.Format.Font.Size = 46;
            //paragBarcode.Format.Font.Color = Colors.Black;
            //paragBarcode.AddText(Code128.Instance.Encode(barcodeText));

            //Barcode b = sec.Elements.AddBarcode();
            //b.Type = BarcodeType.Barcode39;
            //b.Top = "1cm";
            //b.Left = "1cm";
            //b.Code = "0123456789";
            //b.Text = true;// "0123456789";
            //b.Width = "5cm";
            //b.Height = "1cm";
            //b.BearerBars = true;


            ////b.BearersBars = true;
            //b.LineHeight = 10;
            //b.LineRatio = 2;


            Paragraph paragBarcodeText = sec.AddParagraph(barcodeText);
            paragBarcodeText.Format.Alignment = ParagraphAlignment.Center;
            paragBarcodeText.Format.Font.Name = "bahnschrift condensed";
            paragBarcodeText.Format.Font.Size = 14;
            paragBarcodeText.Format.Font.Color = Colors.Black;
            //paragBarcodeText.AddFormattedText(" ПРивет мир!", TextFormat.Bold);
            //paragBarcodeText.AddText(" to be continued... (будет продолжение)");
            //para.Format.Borders.Distance = "5pt";
            Paragraph paragName = sec.AddParagraph(name);
            paragName.Format.Alignment = ParagraphAlignment.Left;
            paragName.Format.Font.Name = "arial narrow";
            paragName.Format.Font.Size = 9;
            paragName.Format.Font.Color = Colors.Black;

            Paragraph paragInfo = sec.AddParagraph();
            paragInfo.Format.Alignment = ParagraphAlignment.Left;
            paragInfo.Format.Font.Name = "arial narrow";
            paragInfo.Format.Font.Size = 10;
            paragInfo.Format.Font.Color = Colors.Black;
            paragInfo.AddText("Артикул: " + vendor);
            paragInfo.AddLineBreak();
            paragInfo.AddText("Цвет: " + color);
            paragInfo.AddLineBreak();
            paragInfo.AddText("Размер: " + size);
            ////paragInfo.AddFormattedText("Размер: " + size, TextFormat.Bold | TextFormat.Italic);
            ////var f2 = para.AddFormattedText(barcodeText);
            ////f2.Font.Name = "Arial";
            //MigraDocCore.DocumentObjectModel.IO.DdlWriter.WriteToFile(doc, "MigraDocCore.mdddl");
            //PdfDocumentRenderer docRenderer = new PdfDocumentRenderer(true);
            //docRenderer.Document = doc;
            //docRenderer.RenderDocument();
            //docRenderer.PdfDocument.Save(outputStream);
            DocumentRenderer docRenderer = new DocumentRenderer(doc);
            docRenderer.PrepareDocument();
            //docRenderer.RenderPage(gfx, 1);
            //docRenderer.RenderObject(gfx, XUnit.FromPoint(10), XUnit.FromPoint(10), page.Width, b);
            docRenderer.RenderObject(gfx, XUnit.FromPoint(0), XUnit.FromPoint(43), page.Width, paragBarcodeText);
            docRenderer.RenderObject(gfx, XUnit.FromPoint(2), XUnit.FromPoint(58), page.Width, paragName);
            docRenderer.RenderObject(gfx, XUnit.FromPoint(10), XUnit.FromPoint(80), page.Width, paragInfo);



            //XFont fontBarcode = new XFont("Code 128", 48, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.WinAnsi));
            //XFont font14 = new("Arial", 14, XFontStyle.Bold);
            //XFont font10 = new("Arial", 10, XFontStyle.Regular);

            //gfx.DrawString(Code128.Instance.Encode(barcodeText), fontBarcode, XBrushes.Black, new XRect(0, 0, page.Width, page.Height), XStringFormats.TopCenter);
            //gfx.DrawString(barcodeText, font14, XBrushes.Black, new XRect(0, 46, page.Width, page.Height - 46), XStringFormats.TopCenter);
            //gfx.DrawString(name.Length > 26 ? name.Substring(0, 26) : name, font10, XBrushes.Black, new XRect(10, 63, page.Width, page.Height - 63), XStringFormats.TopLeft);
            //gfx.DrawString("Артикул: " + vendor, font10, XBrushes.Black, new XRect(10, 75, page.Width, page.Height - 75), XStringFormats.TopLeft);
            //gfx.DrawString("Цвет: " + color, font10, XBrushes.Black, new XRect(10, 87, page.Width, page.Height - 87), XStringFormats.TopLeft);
            //gfx.DrawString("Размер: " + size, font10, XBrushes.Black, new XRect(10, 99, page.Width, page.Height - 99), XStringFormats.TopLeft);

            document.Save(outputStream);
            return outputStream.ToArray();
        }
        public byte[] Test()
        {
            using var outputStream = new MemoryStream();
            DateTime now = DateTime.Now;
            string filename = "MixMigraDocAndPdfSharp.pdf";
            filename = Guid.NewGuid().ToString("D").ToUpper() + ".pdf";
            PdfDocument document = new PdfDocument();
            document.Info.Title = "PDFsharp XGraphic Sample";
            document.Info.Author = "Stefan Lange";
            document.Info.Subject = "Created with code snippets that show the use of graphical functions";
            document.Info.Keywords = "PDFsharp, XGraphics";

            SamplePage1(document);

            Debug.WriteLine("seconds=" + (DateTime.Now - now).TotalSeconds.ToString());

            // Save the document...
            document.Save(outputStream);
            // ...and start a viewer
            // Process.Start(filename);
            return outputStream.ToArray();
        }
        static void SamplePage1(PdfDocument document)
        {
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            // HACK²
            gfx.MUH = PdfFontEncoding.Unicode;
            //gfx.MFEH = PdfFontEmbedding.Default;

            XFont font = new XFont("Verdana", 13, XFontStyle.Bold);

            gfx.DrawString("The following paragraph was rendered using MigraDoc:", font, XBrushes.Black,
              new XRect(100, 100, page.Width - 200, 300), XStringFormats.Center);

            //// You always need a MigraDoc document for rendering.
            //Document doc = new Document();
            //Section sec = doc.AddSection();
            //// Add a single paragraph with some text and format information.
            //Paragraph para = sec.AddParagraph();
            //para.Format.Alignment = ParagraphAlignment.Justify;
            //para.Format.Font.Name = "Times New Roman";
            //para.Format.Font.Size = 12;
            //para.Format.Font.Color = MigraDocCore.DocumentObjectModel.Colors.DarkGray;
            //para.Format.Font.Color = MigraDocCore.DocumentObjectModel.Colors.DarkGray;
            //para.AddText("Duisism odigna acipsum delesenisl ");
            //para.AddFormattedText("ullum in velenit", TextFormat.Bold);
            //para.AddText(" ipit iurero dolum zzriliquisis nit wis dolore vel et nonsequipit, velendigna " +
            //  "auguercilit lor se dipisl duismod tatem zzrit at laore magna feummod oloborting ea con vel " +
            //  "essit augiati onsequat luptat nos diatum vel ullum illummy nonsent nit ipis et nonsequis " +
            //  "niation utpat. Odolobor augait et non etueril landre min ut ulla feugiam commodo lortie ex " +
            //  "essent augait el ing eumsan hendre feugait prat augiatem amconul laoreet. ≤≥≈≠");
            //para.Format.Borders.Distance = "5pt";
            //para.Format.Borders.Color = Colors.Gold;

            //// Create a renderer and prepare (=layout) the document
            //MigraDocCore.Rendering.DocumentRenderer docRenderer = new DocumentRenderer(doc);
            //docRenderer.PrepareDocument();

            //// Render the paragraph. You can render tables or shapes the same way.
            //docRenderer.RenderObject(gfx, XUnit.FromCentimeter(5), XUnit.FromCentimeter(10), "12cm", para);
        }
    }
}