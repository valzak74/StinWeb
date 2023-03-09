using JsonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OzonClasses
{
    public class ActCreateRequest
    {
        public int? Containers_count { get; set; }
        public long? Delivery_method_id { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:sszzz")]
        public DateTime? Departure_date { get; set; }
        public bool ShouldSerializeContainers_count()
        {
            return Containers_count.HasValue;
        }
        public bool ShouldSerializeDelivery_method_id()
        {
            return Delivery_method_id.HasValue;
        }
        public bool ShouldSerializeDeparture_date()
        {
            return Departure_date.HasValue && (Departure_date.Value > DateTime.MinValue);
        }
    }
    public class ActCreateResponse
    {
        public ActCreateResult? Result { get; set; }
    }
    public class ActCreateResult
    {
        public long Id { get; set; }
    }
    public class ActCheckGetRequest
    {
        public long Id { get; set; }
        public DocType Doc_type { get; set; }
        public bool ShouldSerializeDoc_type()
        {
            return Doc_type != DocType.NotFound;
        }
    }
    public class ActCheckStatusResponse
    {
        public ActCheckStatusResult? Result { get; set; }
    }
    public class ActCheckStatusResult
    {
        public long Id { get; set; }
        public List<string>? Added_to_act { get; set; }
        public List<string>? Removed_from_act { get; set; }
        public ActStatus Status { get; set; }
    }
    public class ActGetResponse
    {
        public byte[]? Content { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ActStatus
    {
        NotFound = 0,
        in_process = 1,
        ready = 2,
        error = 3,
        [Display(Name = "The next postings aren't ready")]
        postingsNotReady = 4,
        FORMING = 5,
        FORMED = 6,
        CONFIRMED = 7,
        CONFIRMED_WITH_MISMATCH = 8,
        NOT_FOUND = 9,
        UNKNOWN_ERROR = 10
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum DocType
    {
        NotFound = 0,
        act_of_acceptance = 1,
        act_of_mismatch = 2,
        act_od_excess = 3,
    }
}
