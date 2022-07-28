using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YandexClasses
{
    public class Paging
    {
        public string PrevPageToken { get; set; }
        public string NextPageToken { get; set; }
    }
    public class Pager
    {
        public int CurrentPage { get; set; }
        public int From { get; set; }
        public int PagesCount { get; set; }
        public int PageSize { get; set; }
        public int To { get; set; }
        public int Total { get; set; }
    }
}
