using NonFactors.Mvc.Lookup;
using System.ComponentModel.DataAnnotations;

namespace StinWeb.Models.DataManager.Справочники
{
    public class Campaign
    {
        [Key]
        [Required(ErrorMessage = "Укажите магазин маркетплейса")]
        public string Id { get; set; }
        [LookupColumn]
        public string Тип { get; set; }
        [LookupColumn]
        public string Наименование { get; set; }
    }
}
