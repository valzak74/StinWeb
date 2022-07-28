using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonExtensions
{
    public class JsonDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert == typeof(DateTime));
            if (DateTime.TryParseExact(reader.GetString(), "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", null, System.Globalization.DateTimeStyles.None, out DateTime value))
                return value;
            if (DateTime.TryParseExact(reader.GetString(), "dd-MM-yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime value2))
                return value2;
            throw new JsonException();
        }
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }
}
