using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Справочники
{
    public class RefBook
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Наименование { get; set; }
        public bool Deleted { get; set; }
        public virtual RefBookType BookType { get; set; } = RefBookType.Default;
        public string BookType36 => ((long)BookType).Encode36();
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is RefBook)) return false;
            return Id == (obj as RefBook).Id;
        }
        public override int GetHashCode()
        {
            return (BookType36 + Id).GetHashCode();
        }
    }
    public enum RefBookType
    {
        Default = 0,
        Order = 13994,
        Marketplace = 14042,
        MarketUse = 14152,
        Stock = 55,
        Firma = 4014,
        Nomenklatura = 84,
        Unit = 75,
        Brend = 8840,
    }

}
