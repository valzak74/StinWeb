using Newtonsoft.Json;
using System;

namespace JsonExtensions
{
    public class TimespanConverter : JsonConverter<TimeSpan>
    {
        private string _timeSpanFormatString = "";
        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            var timespanFormatted = $"{value.ToString(_timeSpanFormatString)}";
            writer.WriteValue(timespanFormatted);
        }
        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            TimeSpan parsedTimeSpan;
            TimeSpan.TryParseExact((string)reader.Value, _timeSpanFormatString, null, out parsedTimeSpan);
            return parsedTimeSpan;
        }
        public TimespanConverter(string format)
        {
            _timeSpanFormatString = format;
        }
    }
}
