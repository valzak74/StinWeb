using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliExpressClasses
{
    public class GetLabelUrlRequest
    {
        public List<long>? Logistic_order_ids { get; set; }
    }
    public class GetLabelUrlResponse
    {
        public LabelUrl? Data { get; set; }
        public Error? Error { get; set; }
    }
    public class LabelUrl
    {
        public string? Label_url { get; set; }
    }
}
