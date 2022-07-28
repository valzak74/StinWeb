using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonExtensions
{
    public class JsonDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string stringValue = reader.GetString().Replace('.', ',');
                if (decimal.TryParse(stringValue, out decimal value))
                {
                    return value;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetDecimal(out decimal value))
                    return value;
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }
    public class DecimalFormat2PlacesConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal);
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteRawValue(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}", value)); 
        }

        public override bool CanRead => false;

        public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    public class DecimalFormat3PlacesConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal);
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteRawValue(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.000}", value));
        }

        public override bool CanRead => false;

        public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
