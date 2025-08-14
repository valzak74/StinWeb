using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.MarketCommission;

public interface IMarkupFactorPercentDictionary
{
    IReadOnlyDictionary<decimal, decimal> PercentByPrices { get; }

    decimal GetPercent(decimal price);
}
