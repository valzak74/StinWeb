using Newtonsoft.Json;
using JsonExtensions;

namespace YandexClasses
{
    public class Region
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public RegionType Type { get; set; }
        public Region Parent { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum RegionType
    {
        CITY = 1,
        CITY_DISTRICT = 2,
        CONTINENT = 3,
        COUNTRY = 4,
        COUNTRY_DISTRICT = 5,
        METRO_STATION = 6,
        MONORAIL_STATION = 7,
        OTHERS_UNIVERSAL = 8,
        OVERSEAS_TERRITORY = 9,
        REGION = 10,
        SECONDARY_DISTRICT = 11,
        SETTLEMENT = 12,
        SUBJECT_FEDERATION = 13,
        SUBJECT_FEDERATION_DISTRICT = 14,
        SUBURB = 15,
        VILLAGE = 16,
        NotFound
    }
}
