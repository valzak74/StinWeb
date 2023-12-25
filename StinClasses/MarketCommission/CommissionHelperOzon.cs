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
            PercentFactors = new Dictionary<string, decimal> 
            {
                { "Showcase", showcasePercent },
                { "Ekvaring", 1.5m }
            };
            if (_model == ModelTypeOzon.RealFBS)
            {
                FixCommissions = new Dictionary<string, decimal>
                {
                    { "Base15", 1300 },
                    { "Over15", (volumeWeight - 15) * 20 },
                };
            }
            else // ((_model == ModelTypeOzon.FBS) || (_model == ModelTypeOzon.FBO))
            {
                _volumeWeightFactor = VolumeWeightFactor(volumeWeight);
                FixCommissions = new Dictionary<string, decimal>
                {
                    { "VolumeWeight", _volumeWeightFactor },
                    { "LastMile", 0 },
                    { "ServiceCentre", _model == ModelTypeOzon.FBS ? 20 : 0 }
                };
                PercentFactors.Add("LastMile", _tariffLastMile.percent);
            }
        }
        decimal VolumeWeightFactor(decimal volumeWeight)
        {
            switch (_model)
            {
                case ModelTypeOzon.FBS:
                    return volumeWeight switch
                    {
                        <= 5m => 76,
                        <= 175m => 76 + (Math.Ceiling(volumeWeight) - 5) * 9, // 9 руб за каждый дополнительный литр свыше 5
                        _ => 1615
                    };
                case ModelTypeOzon.FBO:
                    return volumeWeight switch
                    {
                        <= 5m => 63,
                        <= 175m => 63 + (Math.Ceiling(volumeWeight) - 5) * 7, // 7 руб за каждый дополнительный литр свыше 5
                        _ => 1260
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
