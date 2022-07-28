using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Refresher1C.Models.YouKassa
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ReceiptRegistrationStatus
    {
        Pending = 1,
        Succeeded = 2,
        Canceled = 3
    }
}
