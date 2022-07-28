using JsonExtensions;
using Newtonsoft.Json;

namespace AliExpressClasses
{
    public class CatalogListRequest
    {
        public string? Last_product_id { get; set; }
        public string? Limit { get; set; }
    }
    public class CatalogListResponse
    {
        public List<CatalogInfo>? Data { get; set; }
        public Error? Error { get; set; }
    }
    public class CatalogInfo
    {
        public string? Id { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:ssZ")]
        public DateTime? Ali_created_at { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd'T'HH:mm:ssZ")]
        public DateTime? Ali_updated_at { get; set; }
        public string? Category_id { get; set; }
        public string? Currency_code { get; set; }
        public string? Delivery_time { get; set; }
        public string? Owner_member_id { get; set; }
        public string? Owner_member_seq { get; set; }
        public string? Freight_template_id { get; set; }
        public List<string>? Group_ids { get; set; }
        public string? Main_image_url { get; set; }
        public List<string>? Main_image_urls { get; set; }
        public List<ProductSku>? Sku { get; set; }
        public string? Subject { get; set; }
    }
    public class ProductSku
    {
        public string? Id { get; set; }
        public string? Sku_id { get; set; }
        public string? Code { get; set; }
        public decimal? Price { get; set; }
        public decimal? Discount_price { get; set; }
        public decimal? Ipm_sku_stock { get; set; }
    }
    public class Error
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
    }
    public class CatalogListRequestGlobal
    {
        public Aeop_a_e_product_list_query? Aeop_a_e_product_list_query { get; set; }
    }
    public class Aeop_a_e_product_list_query
    {
        public long? Current_page { get; set; }
        public bool ShouldSerializeCurrent_page()
        {
            return Current_page.HasValue;
        }
        public List<long>? Excepted_product_ids { get; set; }
        public bool ShouldSerializeExcepted_product_ids()
        {
            return Excepted_product_ids != null;
        }
        public long? Off_line_time { get; set; }
        public bool ShouldSerializeOff_line_time()
        {
            return Off_line_time.HasValue;
        }
        public string? Owner_member_id { get; set; }
        public bool ShouldSerializeOwner_member_id()
        {
            return !string.IsNullOrEmpty(Owner_member_id);
        }
        public long? Page_size { get; set; }
        public bool ShouldSerializePage_size()
        {
            return Page_size.HasValue;
        }
        public long? Product_id { get; set; }
        public bool ShouldSerializeProduct_id()
        {
            return Product_id.HasValue;
        }
        public AliProductStatusType Product_status_type { get; set; }
        public string? Subject { get; set; }
        public bool ShouldSerializeSubject()
        {
            return !string.IsNullOrEmpty(Subject);
        }
        public string? Ws_display { get; set; }
        public bool ShouldSerializeWs_display()
        {
            return !string.IsNullOrEmpty(Ws_display);
        }
        public string? Have_national_quote { get; set; }
        public bool ShouldSerializeHave_national_quote()
        {
            return !string.IsNullOrEmpty(Have_national_quote);
        }
        public long? Group_id { get; set; }
        public bool ShouldSerializeGroup_id()
        {
            return Group_id.HasValue;
        }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_create_start { get; set; }
        public bool ShouldSerializeGmt_create_start()
        {
            return Gmt_create_start.HasValue && (Gmt_create_start.Value > DateTime.MinValue);
        }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_create_end { get; set; }
        public bool ShouldSerializeGmt_create_end()
        {
            return Gmt_create_end.HasValue && (Gmt_create_end.Value > DateTime.MinValue);
        }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_modified_start { get; set; }
        public bool ShouldSerializeGmt_modified_start()
        {
            return Gmt_modified_start.HasValue && (Gmt_modified_start.Value > DateTime.MinValue);
        }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_modified_end { get; set; }
        public bool ShouldSerializeGmt_modified_end()
        {
            return Gmt_modified_end.HasValue && (Gmt_modified_end.Value > DateTime.MinValue);
        }
        public string? Sku_code { get; set; }
        public bool ShouldSerializeSku_code()
        {
            return !string.IsNullOrEmpty(Sku_code);
        }
    }
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum AliProductStatusType
    {
        NotFound = 0,
        onSelling = 1,
        offline = 2,
        auditing = 3,
        editingRequired = 4,
    }
    public class CatalogListResponseGlobal
    {
        public Aliexpress_solution_product_list_get_response? Aliexpress_solution_product_list_get_response { get; set; }
    }
    public class Aliexpress_solution_product_list_get_response
    {
        public ProductListResult? Result { get; set; }
    }
    public class ProductListResult
    {
        public string? Error_message { get; set; }
        public long? Error_code { get; set; }
        public long? Total_page { get; set; }
        public bool Success { get; set; }
        public long? Product_count { get; set; }
        public string? Error_msg { get; set; }
        public long? Current_page { get; set; }
        public Aeop_a_e_product_display_d_t_o_list? Aeop_a_e_product_display_d_t_o_list { get; set; }
    }
    public class Aeop_a_e_product_display_d_t_o_list
    {
        public List<Item_display_dto>? Item_display_dto { get; set; }
    }
    public class Item_display_dto
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Ws_offline_date { get; set; }
        public string? Ws_display { get; set; }
        public string? Subject { get; set; }
        public string? Src { get; set; }
        public string? Product_min_price { get; set; }
        public string? Product_max_price { get; set; }
        public long? Product_id { get; set; }
        public long? Owner_member_seq { get; set; }
        public string? Owner_member_id { get; set; }
        public string? Image_u_r_ls { get; set; }
        public long? Group_id { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_modified { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Gmt_create { get; set; }
        public long? Freight_template_id { get; set; }
        public string? Currency_code { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Coupon_start_date { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd HH:mm:ss")]
        public DateTime? Coupon_end_date { get; set; }
    }
}