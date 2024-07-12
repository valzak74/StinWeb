using System.Collections.Generic;

namespace StinClasses.MarketCommission
{
    public class CommissionHelperSber : CommissionHelper
    {
        decimal _volumeLogisticsFactor;
        (decimal percent, decimal limMin, decimal limMax) _tariffLastMile = (percent: 5.0m, limMin: 10.0m, limMax: 500.0m);
        public CommissionHelperSber(decimal zakupPrice, int quant, decimal volume, decimal categoryPercent) : base(zakupPrice, quant)
        {
            _volumeLogisticsFactor = VolumeLogisticsFactor(volume);
            FixCommissions = new Dictionary<string, decimal>
            {
                { "Sorting", 10m }, //сортировка 10 руб
                { "ProgLoyalnost", 1m }, //программа лояльности 1 руб 
                { "VolumeLogistics", _volumeLogisticsFactor }, //за логистику в зависимости от объема
                { "LastMile", 0 },
            };
            PercentFactors = new Dictionary<string, decimal>
            {
                { "Category", categoryPercent }, //процент за категорию
                { "Transaction", 1.8m }, //процент за транзакцию 1.8%
                { "LastMile", _tariffLastMile.percent }, //процент за последнюю милю
            };
        }
        public override decimal MinPrice()
        {
            decimal calcMinPrice = base.MinPrice() * Quant;
            if (calcMinPrice > GetLimit(_tariffLastMile.percent, _tariffLastMile.limMax))
            {
                FixCommissions["LastMile"] = _tariffLastMile.limMax;
                PercentFactors["LastMile"] = 0;
                calcMinPrice = base.MinPrice() * Quant;
            }
            else if (calcMinPrice < GetLimit(_tariffLastMile.percent, _tariffLastMile.limMin))
            {
                FixCommissions["LastMile"] = _tariffLastMile.limMin;
                PercentFactors["LastMile"] = 0;
                calcMinPrice = base.MinPrice() * Quant;
            }
            return calcMinPrice;
        }
        decimal VolumeLogisticsFactor(decimal volume)
        {
            return volume switch
            {
                < 1m => 50,
                < 3m => 60,
                < 4m => 70,
                < 6m => 80,
                < 10m => 90,
                < 15m => 120,
                < 25m => 140,
                < 35m => 190,
                < 40m => 250,
                < 50m => 320,
                < 55m => 395,
                < 65m => 490,
                < 75m => 590,
                < 100m => 740,
                < 125m => 945,
                < 150m => 1140,
                < 175m => 1340,
                _ => 1600
            };
        }
    }
}
