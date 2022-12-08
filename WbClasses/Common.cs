using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WbClasses.StocksResponse;

namespace WbClasses
{
    public abstract class Response
    {
        public bool Error { get; set; }
        public string? ErrorText { get; set; }
        public object? AdditionalErrors { get; set; }
    }
    public class WbErrorResponse : Response
    {
        public object? Data { get; set; }
    }
    public class WbBarcode
    {
        public string? MimeType { get; set; }
        public string? Name { get; set; }
        public string? File { get; set; }
        public byte[]? Barcode
        {
            get
            {
                if (string.IsNullOrEmpty(File))
                    return null;
                return Convert.FromBase64String(File);
            }
        }
    }
}
