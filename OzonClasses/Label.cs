using JsonExtensions;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace OzonClasses
{
    public class CreatePackageLabelRequest
    {
        public IReadOnlyList<string> Posting_numbers { get; set; }
    }

    public class CreatePackageLabelResponse
    {
        public IReadOnlyList<PackageLabelTask> Tasks { get; set; }
    }

    public class PackageLabelTask
    {
        public long Task_id { get; set; }
        public PackageLabelTaskType Task_type { get; set; }
    }

    public class PackageLabelRequest
    {
        public long? Task_id { get; set; }
    }

    public class PackageLabelResponse
    {
        public PackageLabelStatus Status { get; set; }
        public string? File_url { get; set; }
        public PackageLabelError Error { get; set; }
    }

    public class PackageLabelStatus
    {
        public PackageLabelStatusCode Code { get; set; }
        public int Postings_count { get; set; }
        public int Printed_postings_count { get; set; }
        public IReadOnlyList<PackageLabelPostionInfo> Unprinted_postings { get; set; }
    }

    public class PackageLabelPostionInfo
    {
        public string Posting_number { get; set; }
        public string Message { get; set; }
    }

    public class PackageLabelError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum PackageLabelTaskType
    {
        NotFound = 0,
        big_label = 1,
        small_label = 2,
    }

    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum PackageLabelStatusCode
    {
        NotFound = 0,
        pending = 1,
        in_progress = 2,
        completed = 3,
        error = 10
    }
}
