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
            decimal showcasePercent
            ) : base(markupFactorPercentDictionary, zakupPrice, quant)
        {
            _model = typeOzon;
            _volumeWeightFactor = VolumeWeightFactor(volumeWeight);
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
