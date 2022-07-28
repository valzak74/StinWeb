namespace OzonClasses
{
    public class SetCountryRequest
    {
        public string? Posting_number { get; set; }
        public long Product_id { get; set; }
        public string? Country_iso_code { get; set; }
    }
    public class SetCountryResponse
    {
        public long Product_id { get; set; }
        public bool Is_gtd_needed { get; set; }
    }
    public class CountryCodeRequest
    {
        public string? Name_search { get; set; }
    }
    public class CountryCodeResponse
    {
        public List<CountryCodeResult>? Result { get; set; } 
    }
    public class CountryCodeResult
    {
        public string? Name { get; set; }
        public string? Country_iso_code { get; set; }
    }
}
