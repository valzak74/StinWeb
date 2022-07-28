using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace JsonExtensions
{
    public class DateFormatConverter : IsoDateTimeConverter
    {
        public DateFormatConverter(string format)
        {
            DateTimeFormat = format;
        }
    }
    public class NewtonsoftDateTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                string readValue = reader.Value.ToString();
                if (DateTime.TryParseExact(readValue, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", null, System.Globalization.DateTimeStyles.None, out DateTime value))
                    return value;
                if (DateTime.TryParseExact(readValue, "dd-MM-yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime value2))
                    return value2;
                throw new JsonException();
            }
            catch (Exception ex)
            {
                throw new JsonException();
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue(value.ToString());
    }
}
