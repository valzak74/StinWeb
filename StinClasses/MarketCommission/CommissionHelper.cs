using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.MarketCommission
{
    public abstract class CommissionHelper : IDisposable
    {
        protected readonly decimal _zakupPrice;
        public virtual IEnumerable<(string name, decimal percent, decimal limMin, decimal limMax)> _tariffWithBorders { get; }
        public decimal MarkupFactorPercent = 10m;
        public int Quant = 1;
        public Dictionary<string, decimal> FixCommissions;
        public Dictionary<string, decimal> PercentFactors;

        public CommissionHelper(decimal zakupPrice) => _zakupPrice = zakupPrice;
        public CommissionHelper(decimal zakupPrice, int quant) : this(zakupPrice) => Quant = quant;
        protected decimal GetLimit(decimal percent, decimal borderLimit) => (borderLimit * 100 / percent) / Quant;
        protected Dictionary<string, decimal> LimitValues(decimal price, IEnumerable<(string name, decimal percent, decimal limMin, decimal limMax)> tariffs)
        {
            Dictionary<string, decimal> result = new Dictionary<string, decimal>();
            foreach (var item in tariffs)
            {
                result.Add("Min" + item.name, GetLimit(item.percent, item.limMin));
                result.Add("Max" + item.name, GetLimit(item.percent, item.limMax));
            }
            return result.OrderByDescending(x => Math.Abs(x.Value - price)).ThenBy(x => x.Key).ToDictionary(k => k.Key, v => v.Value);
        }
        protected decimal GetMinPriceWithBorder(decimal price)
        {
            var limits = LimitValues(price, _tariffWithBorders);
            foreach (var limit in limits)
            {
                var key = limit.Key.Substring(3);
                var border = _tariffWithBorders.FirstOrDefault(x => x.name == key);
                if (limit.Key.Substring(0, 3) == "Max")
                {
                    if (price > limit.Value)
                    {
                        FixCommissions[key] = border.limMax;
                        PercentFactors[key] = 0;
                        price = _minPrice();
                    }
                }
                else // Min
                {
                    if (price < limit.Value)
                    {
                        FixCommissions[key] = border.limMin;
                        PercentFactors[key] = 0;
                        price = _minPrice();
                    }
                }
            }
            return price;
        }
        public void SetFixCommissions(Dictionary<string, decimal> fixCommissions) => FixCommissions = fixCommissions;
        public void SetPercentFactors(Dictionary<string, decimal> percentFactors) => PercentFactors = percentFactors;
        public virtual decimal PorogPrice() => (_zakupPrice * Quant) * (100 + MarkupFactorPercent) / 100;
        decimal _minPrice()
        {
            decimal porog = PorogPrice();
            decimal fixCommissions = FixCommissions?.Sum(x => x.Value) ?? 0;
            decimal commissions = (PercentFactors?.Sum(x => x.Value) ?? 0) / 100;
            return ((porog + fixCommissions) / (1 - commissions)) / Quant;
        }
        public virtual decimal MinPrice()
        {
            return _minPrice();
        }
        public void Dispose()
        {
            FixCommissions?.Clear();
            PercentFactors?.Clear();
        }
    }
}
