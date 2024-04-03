using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonExtensions
{
    public class SingleObjectOrArrayJsonConverter<T> : JsonConverter<List<T>> where T : class, new()
    {
        public override void WriteJson(JsonWriter writer, List<T> value, JsonSerializer serializer) =>
            // avoid possibility of infinite recursion by wrapping the List<T> with AsReadOnly()
            serializer.Serialize(writer, value.Count == 1 ? (object)value.Single() : value.AsReadOnly());

        public override List<T> ReadJson(JsonReader reader, Type objectType, List<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            existingValue ??= new();
            switch (reader.TokenType)
            {
                case JsonToken.StartObject: existingValue.Add(serializer.Deserialize<T>(reader)); break;
                case JsonToken.StartArray: serializer.Populate(reader, existingValue); break;
                default: throw new ArgumentOutOfRangeException($"Converter does not support JSON token type {reader.TokenType}.");
            };
            return existingValue;
        }
    }

    public class SingleObjectOrArrayJsonStringConverter : JsonConverter<List<string>> 
    {
        public override void WriteJson(JsonWriter writer, List<string> value, JsonSerializer serializer) =>
            // avoid possibility of infinite recursion by wrapping the List<T> with AsReadOnly()
            serializer.Serialize(writer, value.Count == 1 ? (object)value.Single() : value.AsReadOnly());

        public override List<string> ReadJson(JsonReader reader, Type objectType, List<string> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            existingValue ??= new();
            switch (reader.TokenType)
            {
                case JsonToken.StartArray: serializer.Populate(reader, existingValue); break;
                case JsonToken.StartObject: 
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.Date:
                case JsonToken.Boolean:
                case JsonToken.String:
                case JsonToken.Bytes:
                case JsonToken.Comment:
                case JsonToken.None:
                case JsonToken.Raw:
                    existingValue.Add(serializer.Deserialize<string>(reader)); break;
                default: throw new ArgumentOutOfRangeException($"Converter does not support JSON token type {reader.TokenType}.");
            };
            return existingValue;
        }
    }
}
