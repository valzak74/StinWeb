using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WbClasses
{
    public class WbTariffResponse
    {
        public List<WbTariff> Report { get; set; }
    }

    public class WbTariff
    {
        public decimal KgvpMarketplace { get; set; }
        public string SubjectID { get; set; }
        public string SubjectName { get; set; }
    }
}
