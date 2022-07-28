using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliExpressClasses
{
    public class ErrorGlobal
    {
        public ErrorResponse? Error_response { get; set; }
    }

    public class ErrorResponse
    {
        public long Code { get; set; }
        public string? Msg { get; set; }
        public string? Sub_msg { get; set; }
        public string? Sub_code { get; set; }
        public string? Request_id { get; set; }
    }
}
