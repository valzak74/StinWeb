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
        public CommissionHelperOzon(ModelTypeOzon typeOzon, int quant, decimal zakupPrice, decimal volumeWeight, decimal showcasePercent) : base(zakupPrice, quant)
        {
            _model = typeOzon;
            _volumeWeightFactor = VolumeWeightFactor(volumeWeight);
            PercentFactors = new Dictionary<string, decimal> 
            {
                { "Showcase", showcasePercent },
                { "Ekvaring", 1.5m }
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
                        <= 0.4m => 43,
                        <= 1m => 76,
                        <= 190m => 76 + (Math.Ceiling(volumeWeight) - 1) * 18, // 18 руб за каждый дополнительный литр свыше 1
                        _ => 3478
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
                        <= 1m => 43,
                        <= 190m => 43 + (Math.Ceiling(volumeWeight) - 1) * 10, // 10 руб за каждый дополнительный литр свыше 1
                        _ => 1933
                    };
            }
            return 0;
        }
        public override decimal MinPrice()
        {
            decimal calcMinPrice = base.MinPrice();
            if (_model == ModelTypeOzon.RealFBS)
                return calcMinPrice;
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
    }
    public enum ModelTypeOzon
    {
        FBS = 0,
        FBO = 1,
        RealFBS = 2
    }
}
