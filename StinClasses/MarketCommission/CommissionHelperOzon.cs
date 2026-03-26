using System;
using System.Collections.Generic;

namespace StinClasses.MarketCommission
{
    public class CommissionHelperOzon: CommissionHelper
    {
        readonly ModelTypeOzon _model;
        public bool IsKGT = false;
        (decimal percent, decimal limMin, decimal limMax) _tariffLastMile = (percent: 5.5m, limMin: 20.0m, limMax: 500.0m);
        decimal _volumeWeightFactor;
        public CommissionHelperOzon(
            ModelTypeOzon typeOzon, 
            int quant, 
            IMarkupFactorPercentDictionary markupFactorPercentDictionary, 
            decimal zakupPrice, 
            decimal volumeWeight, 
            decimal showcasePercent,
            decimal price
            ) : base(markupFactorPercentDictionary, zakupPrice, quant)
        {
            _model = typeOzon;
            var dedline = new DateTime(2026, 4, 6);
            _volumeWeightFactor = DateTime.Today < dedline
                ? VolumeWeightFactor(volumeWeight)
                : VolumeWeightFactor(volumeWeight, price);
            PercentFactors = new Dictionary<string, decimal> 
            {
                { "Showcase", showcasePercent },
                { "Ekvaring", 2.0m }
            };
            if (_model == ModelTypeOzon.RealFBS)
            {
                var K = 1;
                FixCommissions = new Dictionary<string, decimal>
                {
                    { "VolumeWeight", _volumeWeightFactor * K },
                    { "ServiceCentre", 15 },
                };
            }
            else // ((_model == ModelTypeOzon.FBS) || (_model == ModelTypeOzon.FBO))
            {
                FixCommissions = new Dictionary<string, decimal>
                {
                    { "VolumeWeight", _volumeWeightFactor },
                    { "LastMile", 25 },
                    { "ServiceCentre", _model == ModelTypeOzon.FBS ? 20 : 0 }
                };
                //PercentFactors.Add("LastMile", _tariffLastMile.percent);
            }
        }
        decimal VolumeWeightFactor(decimal volumeWeight, decimal price)
        {
            switch (_model)
            {
                case ModelTypeOzon.FBS:
                case ModelTypeOzon.FBO:
                    return price > 300
                        ? volumeWeight switch
                        {
                            <= 0.2m => 68m,
                            <= 0.4m => 83m,
                            <= 0.6m => 90m,
                            <= 0.8m => 90m,
                            <= 1m => 90m,
                            <= 1.25m => 99m,
                            <= 1.5m => 106m,
                            <= 1.75m => 106m,
                            <= 2m => 106m,
                            <= 3m => 106m,
                            <= 4m => 125m,
                            <= 5m => 148m,
                            <= 6m => 148m,
                            <= 7m => 174m,
                            <= 8m => 174m,
                            <= 9m => 180m,
                            <= 10m => 180m,
                            <= 11m => 186m,
                            <= 12m => 186m,
                            <= 13m => 190m,
                            <= 14m => 205m,
                            <= 15m => 217m,
                            <= 17m => 232m,
                            <= 20m => 266m,
                            <= 25m => 322m,
                            <= 30m => 368m,
                            <= 35m => 420m,
                            <= 40m => 455m,
                            <= 45m => 530m,
                            <= 50m => 575m,
                            <= 60m => 640m,
                            <= 70m => 721m,
                            <= 80m => 801m,
                            <= 90m => 958m,
                            <= 100m => 996m,
                            <= 125m => 1189m,
                            <= 150m => 1403m,
                            <= 175m => 1667m,
                            <= 200m => 1988m,
                            <= 400m => 2924m,
                            <= 600m => 5116m,
                            <= 800m => 6483m,
                            _ => 7787m
                        }
                        : volumeWeight switch
                        {
                            <= 0.2m => 17.28m,
                            <= 0.4m => 19.32m,
                            <= 0.6m => 21.35m,
                            <= 0.8m => 22.37m,
                            <= 1m => 23.38m,
                            <= 1.25m => 25.42m,
                            <= 1.5m => 26.43m,
                            <= 1.75m => 27.45m,
                            <= 2m => 29.48m,
                            <= 3m => 31.52m,
                            <= 4m => 35.58m,
                            <= 5m => 38.63m,
                            <= 6m => 42.70m,
                            <= 7m => 57.95m,
                            <= 8m => 62.02m,
                            <= 9m => 65.07m,
                            <= 10m => 69.13m,
                            <= 11m => 79.30m,
                            <= 12m => 83.37m,
                            <= 13m => 87.43m,
                            <= 14m => 92.52m,
                            <= 15m => 96.58m,
                            <= 17m => 96.58m,
                            <= 20m => 110.82m,
                            <= 25m => 118.95m,
                            <= 30m => 131.15m,
                            <= 35m => 146.40m,
                            <= 40m => 156.57m,
                            <= 45m => 175.88m,
                            <= 50m => 189.10m,
                            <= 60m => 207.40m,
                            <= 70m => 230.78m,
                            <= 80m => 249.08m,
                            <= 90m => 274.50m,
                            <= 100m => 284.67m,
                            <= 125m => 331.43m,
                            <= 150m => 381.25m,
                            <= 175m => 436.15m,
                            <= 200m => 483.93m,
                            <= 400m => 805.20m,
                            <= 600m => 805.20m,
                            <= 800m => 805.20m,
                            _ => 805.20m
                        };
                case ModelTypeOzon.RealFBS:
                    return volumeWeight switch
                    {
                        <= 14m => 1700,
                        <= 30m => 2200,
                        <= 65m => 2800,
                        <= 120m => 4300,
                        <= 200m => 5600,
                        _ => 9900
                    };
            }
            return 0;
        }
        decimal VolumeWeightFactor(decimal volumeWeight)
        {
            switch (_model)
            {
                case ModelTypeOzon.FBS:
                    return volumeWeight switch
                    {
                        <= 1m => 81.34m,
                        <= 3m => 81.34m + (Math.Ceiling(volumeWeight) - 1) * 18.3m, // 18 руб за каждый дополнительный литр свыше 1 до 3
                        <= 190m => 81.34m + 2 * 18.3m + (Math.Ceiling(volumeWeight) - 3) * 23.39m, // 23 руб за каждый дополнительный литр свыше 3
                        <= 1000m => 81.34m + 2 * 18.3m + 187 * 23.39m + (Math.Ceiling(volumeWeight) - 190) * 6.1m, // 6 руб за каждый доп. литр свыше 190
                        _ => 9432.87m
                    };
                case ModelTypeOzon.RealFBS:
                    return volumeWeight switch
                    {
                        <= 14m => 1700,
                        <= 30m => 2200,
                        <= 65m => 2800,
                        <= 120m => 4300,
                        <= 200m => 5600,
                        _ => 9900
                    };
                case ModelTypeOzon.FBO:
                    return volumeWeight switch
                    {
                        <= 1m => 46.77m,
                        <= 3m => 46.77m + (Math.Ceiling(volumeWeight) - 1) * 10.17m, // 10 руб за каждый дополнительный литр свыше 1 до 3
                        <= 190m => 46.77m + 2 * 10.17m + (Math.Ceiling(volumeWeight) - 3) * 15.25m, // 15 руб за каждый дополнительный литр свыше 3
                        <= 1000m => 46.77m + 2 * 10.17m + 187 * 15.25m + (Math.Ceiling(volumeWeight) - 190) * 6.1m, // 6 руб за каждый доп. литр свыше 190
                        _ => 7859.86m
                    };
            }
            return 0;
        }
        public override decimal MinPrice()
        {
            decimal calcMinPrice = base.MinPrice();
            if (_model == ModelTypeOzon.RealFBS)
                return calcMinPrice;
            if (PercentFactors.ContainsKey("LastMile"))
            {
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
            }
            return calcMinPrice;
        }
    }
    public enum ModelTypeOzon
    {
        FBS = 0,
        FBO = 1,
        RealFBS = 2
    }
}
