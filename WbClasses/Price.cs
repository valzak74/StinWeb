using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WbClasses
{
    public class PriceRequestV2
    {
        public List<PriceRequest>? Data { get; set; }
    }
    public class PriceRequest
    {
        public long NmId { get; set; }
        public int Price { get; set; }
    }
    public class PriceError 
    {
        public List<string>? Errors { get; set; }
        public int ErrorCode { get; set; }
    }
}
