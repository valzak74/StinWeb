using System.Collections.Generic;

namespace StinClasses.MarketCommission
{
    public class CommissionHelperSber : CommissionHelper
    {
        decimal _volumeLogisticsFactor;
        (decimal percent, decimal limMin, decimal limMax) _tariffLastMile = (percent: 4.0m, limMin: 30.0m, limMax: 215.0m);
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
                { "Category", categoryPercent / 100 }, //процент за категорию
                { "Transaction", 1.8m / 100 }, //процент за транзакцию 1.8%
                { "LastMile", _tariffLastMile.percent }, //процент за последнюю милю
            };
        }
        public override decimal MinPrice()
        {
            decimal calcMinPrice = base.MinPrice();
            if (calcMinPrice > GetLimit(_tariffLastMile.percent, _tariffLastMile.limMax))
            {
                FixCommissions["LastMile"] = _tariffLastMile.limMax;
                PercentFactors["LastMile"] = 0;
                calcMinPrice = base.MinPrice();
            }
            else if (calcMinPrice < GetLimit(_tariffLastMile.percent, _tariffLastMile.limMin))
            {
                FixCommissions["LastMile"] = _tariffLastMile.limMin;
                PercentFactors["LastMile"] = 0;
                calcMinPrice = base.MinPrice();
            }
            return calcMinPrice;
        }
        decimal VolumeLogisticsFactor(decimal volume)
        {
            return volume switch
            {
                < 1m => 35,
                < 3m => 42,
                < 4m => 48,
                < 6m => 50,
                < 10m => 55,
                < 15m => 80,
                < 25m => 95,
                < 35m => 160,
                < 40m => 220,
                < 50m => 240,
                < 55m => 310,
                < 65m => 340,
                < 75m => 350,
                < 100m => 440,
                < 125m => 580,
                < 150m => 725,
                < 175m => 770,
                _ => 800
            };
        }
    }
}
