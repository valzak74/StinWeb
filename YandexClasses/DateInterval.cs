using System;
using System.Collections.Generic;
using JsonExtensions;
using Newtonsoft.Json;

namespace YandexClasses
{
    public class Interval
    {
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy")]
        public DateTime Date { get; set; }
        [JsonConverter(typeof(TimespanConverter), @"hh\:mm")]
        public TimeSpan FromTime { get; set; }
        [JsonConverter(typeof(TimespanConverter), @"hh\:mm")]
        public TimeSpan ToTime { get; set; }
    }
    public class Date
    {
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy")]
        public DateTime FromDate { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy")]
        public DateTime ToDate { get; set; }
        [JsonConverter(typeof(TimespanConverter), @"hh\:mm")]
        public TimeSpan FromTime { get; set; }
        [JsonConverter(typeof(TimespanConverter), @"hh\:mm")]
        public TimeSpan ToTime { get; set; }
        public List<Interval> Intervals { get; set; }
        public Date()
        {
            Intervals = new List<Interval>();
        }
        public bool ShouldSerializeFromTime()
        {
            return (FromTime > TimeSpan.Zero) && (FromTime < ToTime);
        }
        public bool ShouldSerializeToTime()
        {
            return ToTime > TimeSpan.Zero;
        }
        public bool ShouldSerializeIntervals()
        {
            return (Intervals != null) && (Intervals.Count > 0);
        }
    }
}
