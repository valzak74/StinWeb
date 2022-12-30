using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WbClasses
{
    public abstract class Response
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
    }
    public class WbErrorResponse : Response
    {
        public object? Data { get; set; }
    }
    public class WbBarcode
    {
        public long OrderId { get; set; }
        public int PartA { get; set; }
        public int PartB { get; set; }
        public string? Barcode { get; set; }
        private string? File;
        public byte[]? Data
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
