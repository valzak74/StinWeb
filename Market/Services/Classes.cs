using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Market.Services
{
    public class HeadersParameters
    {
        [FromHeader]
        [FromQuery]
        [Required]
        public string Authorization { get; set; }

        [FromHeader]
        public string Host { get; set; }

        [FromHeader(Name = "User-Agent")]
        public string UserAgent { get; set; }
    }
}
