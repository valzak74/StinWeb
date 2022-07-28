using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtolOnlineClasses
{
    public class TokenRequest
    {
        public string login { get; set; }
        public string pass { get; set; }
    }
    public class TokenResponse
    {
        [JsonConverter(typeof(DateFormatConverter), "dd.MM.yyyy HH:mm:ss")]
        public DateTime timestamp { get; set; }
        public string token { get; set; }
        public AtolError error { get; set; }
    }
    public class AtolError
    {
        public int code { get; set; }
        public string error_id { get; set; }
        public string text { get; set; }
        public AtolErrorType type { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum AtolErrorType
    {
        NotFound = -1,
        system = 0,
        unknown = 1
    }
}
