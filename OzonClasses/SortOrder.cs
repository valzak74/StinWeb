using JsonExtensions;
using Newtonsoft.Json;

namespace OzonClasses
{
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum SortOrder
    {
        NotFound = 0,
        ASC = 1,
        DESC = 2,
    }
}
