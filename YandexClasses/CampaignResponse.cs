using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YandexClasses
{
    public class CampaignResponse
    {
        public List<CampaignDTO> Campaigns { get; set; }
    }

    public class CampaignDTO
    {
        public string Domain { get; set; }
        public string Id { get; set; }
        public string ClientId { get; set; }
        public BusinessDTO Business { get; set; }
        public string PlacementType { get; set; }
    }

    public class BusinessDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
