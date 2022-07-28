using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace YandexClasses
{
    public class ChangeItemsRequest
    {
        public List<ChangeItem> Items { get; set; }
    }
    public class ChangeItem
    {
        public long Id { get; set; }
        public int Count { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Instance> Instances { get; set; }
    }
    public class Instance
    {
        public string Cis { get; set; }
    }
}
