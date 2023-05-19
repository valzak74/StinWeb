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
                    { "ServiceCentre", _model == ModelTypeOzon.FBS ? 10 : 0 }
                };
                PercentFactors.Add("LastMile", _tariffLastMile.percent);
            }
        }
        decimal VolumeWeightFactor(decimal volumeWeight)
        {
            switch (_model)
            {
                case ModelTypeOzon.FBS:
                    if (IsKGT)
                        return 1100;
                    return volumeWeight switch
                    {
                        0.1m => 40,
                        0.2m => 41,
                        0.3m => 42,
                        0.4m => 43,
                        0.5m => 43,
                        0.6m => 45,
                        0.7m => 45,
                        0.8m => 47,
                        0.9m => 49,
                        1m => 51,
                        1.1m => 55,
                        1.2m => 57,
                        1.3m => 61,
                        1.4m => 63,
                        1.5m => 65,
                        1.6m => 67,
                        1.7m => 69,
                        1.8m => 70,
                        1.9m => 71,
                        < 3m => 79,
                        < 4m => 100,
                        < 5m => 120,
                        < 6m => 135,
                        < 7m => 160,
                        < 8m => 185,
                        < 9m => 210,
                        < 10m => 225,
                        < 11m => 265,
                        < 12m => 290,
                        < 13m => 315,
                        < 14m => 350,
                        < 15m => 370,
                        < 20m => 400,
                        < 25m => 525,
                        _ => 700
                    };
                case ModelTypeOzon.FBO:
                    return volumeWeight switch
                    {
                        0.1m => 40,
                        0.2m => 41,
                        0.3m => 42,
                        0.4m => 43,
                        0.5m => 43,
                        0.6m => 45,
                        0.7m => 45,
                        0.8m => 47,
                        0.9m => 49,
                        1m => 51,
                        1.1m => 55,
                        1.2m => 57,
                        1.3m => 61,
                        1.4m => 63,
                        1.5m => 65,
                        1.6m => 67,
                        1.7m => 69,
                        1.8m => 70,
                        1.9m => 71,
                        < 3m => 79,
                        < 4m => 100,
                        < 5m => 120,
                        < 6m => 135,
                        < 7m => 160,
                        < 8m => 185,
                        < 9m => 210,
                        < 10m => 225,
                        < 11m => 265,
                        < 12m => 290,
                        < 13m => 315,
                        < 14m => 350,
                        < 15m => 370,
                        < 20m => 400,
                        < 25m => 525,
                        < 30m => 700,
                        < 35m => 800,
                        _ => 1000
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
