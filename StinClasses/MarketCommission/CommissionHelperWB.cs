using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.MarketCommission;

public class CommissionHelperWB : CommissionHelper
{
    decimal _baseLogistics = 46;
    decimal _addPerLiter = 14.0m;
    decimal _includeLiters = 1;
    decimal _skladFactor = 2.0m;
    decimal _minHard = 1000;
    decimal _maxSize = 120.0m;
    decimal _maxSumSize = 200.0m;
    decimal _maxWeight = 25;
    decimal _ekvaring = 2.0m;

    public CommissionHelperWB(
        IMarkupFactorPercentDictionary markupFactorPercentDictionary,
        decimal zakupPrice, 
        int quant, 
        decimal categoryPercent,
        decimal length,
        decimal width,
        decimal height,
        decimal weightBrutto
    ) : base(markupFactorPercentDictionary, zakupPrice, quant)
    {
        decimal liters = length * width * height / 1000;
        decimal sumLogistics = VolumeWeightFactor(liters);
        bool isHard = (weightBrutto > _maxWeight)
            || ((length > _maxSize) || (width > _maxSize) || (height > _maxSize))
            || (length + width + height > _maxSumSize);
        if (isHard)
            sumLogistics = Math.Max(sumLogistics, _minHard);
        FixCommissions = new Dictionary<string, decimal>
        {
            { "VolumeLogistics", sumLogistics }, //за логистику в зависимости от объема
        };
        PercentFactors = new Dictionary<string, decimal>
        {
            { "Category", categoryPercent }, //процент за категорию
            { "Transaction", _ekvaring }, //процент за транзакцию 2.0%
        };
    }
    decimal VolumeWeightFactor(decimal liters)
    {
        var baseLogistics = liters switch
        {
            <= 0.2m => 23,
            <= 0.4m => 26,
            <= 0.6m => 29,
            <= 0.8m => 30,
            <= 1m => 32,
            _ => _baseLogistics
        };

        return (baseLogistics + Math.Max(Math.Ceiling(liters) - _includeLiters, 0) * _addPerLiter) * _skladFactor;
    }
    public override decimal MinPrice()
    {
        return base.MinPrice() * Quant;
    }
}
