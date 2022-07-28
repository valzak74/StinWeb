using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
//using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Refresher1C.Models.YouKassa
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PaymentStatus
    {
        Pending = 0,
        WaitingForCapture = 1,
        Succeeded = 2,
        Canceled = 3,
        Unsupported = 4
    }
}
