using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.MarketCommission;

public class CommissionHelperWB : CommissionHelper
{
    decimal _baseLogistics = 35;
    decimal _addPerLiter = 8.5m;
    decimal _includeLiters = 1;
    decimal _minHard = 1000;
    decimal _maxSize = 120.0m;
    decimal _maxSumSize = 200.0m;
    decimal _maxWeight = 25;
    decimal _ekvaring = 1.5m;

    public CommissionHelperWB(
        decimal zakupPrice, 
        int quant, 
        decimal categoryPercent,
        decimal length,
        decimal width,
        decimal height,
        decimal weightBrutto
    ) : base(zakupPrice, quant)
    {
        decimal liters = length * width * height / 1000;
        decimal oversizeLiters = Math.Max(liters - _includeLiters, 0);
        decimal sumLogistics = _baseLogistics + (oversizeLiters * _addPerLiter);
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
            { "Transaction", 1.5m }, //процент за транзакцию 1.8%
        };
    }
    public override decimal MinPrice()
    {
        return base.MinPrice() * Quant;
    }
}
