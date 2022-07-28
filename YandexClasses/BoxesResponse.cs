using System.Collections.Generic;

namespace YandexClasses
{
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
}
