using System.Collections.Generic;

namespace YandexClasses
{
    public class BoxesResponse_02_2024
    {
        public ResponseStatus Status { get; set; }
        public OrderBoxesLayoutDTO Result { get; set; }
        public List<Error> Errors { get; set; }
    }
    public class BoxesResponse
    {
        public ResponseStatus Status { get; set; }
        public BoxesResponseResult Result { get; set; }
        public List<Error> Errors { get; set; }
    }
    public class BoxesResponseResult
    {
        public List<Box> Boxes { get; set; }
    }
    public class OrderBoxesLayoutDTO
    {
        public List<EnrichedOrderBoxLayoutDTO> Boxes { get; set; }
    }
    public class EnrichedOrderBoxLayoutDTO
    {
        public long BoxId { get; set; }
        public List<OrderBoxLayoutItemDTO> Items { get; set; }
    }
}
