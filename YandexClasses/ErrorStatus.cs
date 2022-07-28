using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace YandexClasses
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ResponseStatus
    {
        OK = 1,
        ERROR = 2
    }
    public class Error
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
    public class ErrorResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Error> Errors { get; set; }
    }
}
