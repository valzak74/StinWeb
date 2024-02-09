using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YandexClasses
{
    public class BoxesRequest_02_2024
    {
        public List<OrderBoxLayoutDTO> Boxes { get; set; }

        public bool AllowRemove { get; set; }
        public BoxesRequest_02_2024()
        {
            Boxes = new List<OrderBoxLayoutDTO>();
            AllowRemove = false;
        }
    }
    public class BoxesRequest
    {
        public List<Box> Boxes { get; set; }
        public BoxesRequest()
        {
            Boxes = new List<Box>();
        }
    }
    public class Box
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long Id { get; set; }
        public string FulfilmentId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long Weight { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long Width { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long Height { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long Depth { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BoxItem> Items { get; set; }
        public bool ShouldSerializeId()
        {
            return Id > 0;
        }
        public bool ShouldSerializeItems()
        {
            return (Items != null && Items.Count > 0);
        }
        public bool ShouldSerializeWeight()
        {
            return Weight > 0;
        }
        public bool ShouldSerializeWidth()
        {
            return Width > 0;
        }
        public bool ShouldSerializeHeight()
        {
            return Height > 0;
        }
        public bool ShouldSerializeDepth()
        {
            return Depth > 0;
        }
    }
    public class BoxItem
    {
        public long Id { get; set; }
        public int Count { get; set; }
    }

    public class OrderBoxLayoutDTO
    {
        public List<OrderBoxLayoutItemDTO> Items { get; set; }
    }

    public class OrderBoxLayoutItemDTO
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long Id { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int FullCount { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OrderBoxLayoutPartialCountDTO PartialCount { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BriefOrderItemInstanceDTO> Instances { get; set; }
        public bool ShouldSerializeId()
        {
            return Id > 0;
        }
        public bool ShouldSerializeFullCount()
        {
            return FullCount > 0;
        }
        public bool ShouldSerializePartialCount()
        {
            return PartialCount != null;
        }
        public bool ShouldSerializeInstances()
        {
            return (Instances != null && Instances.Count > 0);
        }
    }
    public class OrderBoxLayoutPartialCountDTO
    {
        public int Current { get; set; }
        public int Total { get; set; }
    }
    public class BriefOrderItemInstanceDTO
    {
        public string Cis { get; set; }
        public string Uin { get; set; }
        public string Rnpt { get; set; }
        public string Gtd { get; set; }
    }
}