using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refresher1C.Models.YouKassa
{
    public class Card
    {
        public string First6 { get; set; }
        public string Last4 { get; set; }
        public string Expiry_Year { get; set; }
        public string Expiry_Month { get; set; }
        public string Card_Type { get; set; }
        public string Csc { get; set; }
        public string Card_Holder { get; set; }
        public string Number { get; set; }
        public string Issuer_Country { get; set; }
        public string Issuer_Name { get; set; }
        public Card()
        {

        }
    }
}
