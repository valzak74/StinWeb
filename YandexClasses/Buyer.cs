using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YandexClasses
{
    public class Buyer
    {
        public string Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Phone { get; set; }
    }
    public class BuyerDetailsResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Error> Errors { get; set; }
        public Buyer Result { get; set; }
    }
}
