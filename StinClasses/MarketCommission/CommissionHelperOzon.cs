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
            _volumeWeightFactor = VolumeWeightFactor(volumeWeight, price);
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
                            <= 0.2m => 112m,
                            <= 0.4m => 118m,
                            <= 0.6m => 121m,
                            <= 0.8m => 121m,
                            <= 1m => 121m,
                            <= 1.25m => 124m,
                            <= 1.5m => 134m,
                            <= 1.75m => 134m,
                            <= 2m => 134m,
                            <= 3m => 134m,
                            <= 4m => 144m,
                            <= 5m => 166m,
                            <= 6m => 166m,
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
                            <= 0.2m => 28m,
                            <= 0.4m => 30m,
                            <= 0.6m => 30m,
                            <= 0.8m => 33m,
                            <= 1m => 35m,
                            <= 1.25m => 37m,
                            <= 1.5m => 40m,
                            <= 1.75m => 41m,
                            <= 2m => 44m,
                            <= 3m => 50m,
                            <= 4m => 58m,
                            <= 5m => 67m,
                            <= 6m => 75m,
                            <= 7m => 95m,
                            <= 8m => 106m,
                            <= 9m => 117m,
                            <= 10m => 123m,
                            <= 11m => 138m,
                            <= 12m => 144m,
                            <= 13m => 160m,
                            <= 14m => 162m,
                            <= 15m => 176m,
                            <= 17m => 192m,
                            <= 20m => 212m,
                            <= 25m => 256m,
                            <= 30m => 296m,
                            <= 35m => 344m,
                            <= 40m => 370m,
                            <= 45m => 423m,
                            <= 50m => 482m,
                            <= 60m => 541m,
                            <= 70m => 604m,
                            <= 80m => 668m,
                            <= 90m => 757m,
                            <= 100m => 838m,
                            <= 125m => 939m,
                            <= 150m => 1161m,
                            <= 175m => 1348m,
                            <= 200m => 1571m,
                            <= 400m => 1999m,
                            <= 600m => 3404m,
                            <= 800m => 5115m,
                            _ => 6322m
                        };
                case ModelTypeOzon.RealFBS:
                    return volumeWeight switch
                    {
                        <= 1m => 900,
                        <= 6m => 1000,
                        <= 14m => 1200,
                        <= 30m => 1700,
                        <= 65m => 2800,
                        <= 120m => 4300,
                        <= 200m => 5600,
                        _ => 9900
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
