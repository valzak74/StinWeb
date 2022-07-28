namespace OzonClasses
{
    public class ErrorResponse
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
        public List<Detail>? Details { get; set; }
    }

    public class Detail
    {
        public string? TypeUrl { get; set; }
        public string? Value { get; set; }
    }
}
