using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.MarketCommission
{
    public class CommissionHelperYandex : CommissionHelper
    {
        readonly ModelTypeYandex _model;
        readonly decimal _dimensionsLimit = 150;
        readonly decimal _weightLimit = 25;
        (decimal percent, decimal limMin, decimal limMax) _tariffLastMile = (percent: 5.5m, limMin: 60.0m, limMax: 400.0m);
        public override IEnumerable<(string name, decimal percent, decimal limMin, decimal limMax)> _tariffWithBorders => new List<(string name, decimal percent, decimal limMin, decimal limMax)>
        {
            (name: "Stock", percent: 3.0m, limMin: 20.0m, limMax: 60.0m),
            (name: "Delivery", percent: 5.5m, limMin: 13.0m, limMax: 300.0m),
        };
        decimal _volumeWeight;
        bool _isLightFactor;
        bool _isHard;
        public CommissionHelperYandex(ModelTypeYandex typeYandex, int quant, decimal zakupPrice, 
            decimal volumeWeight, 
            decimal sumFactorPercent,
            decimal price,
            decimal weight,
            decimal dimensions) : base(zakupPrice, quant)
        {
            _model = typeYandex;
            _volumeWeight = Math.Max(weight, volumeWeight);
            _isLightFactor = (_model != ModelTypeYandex.DBS) && (price < 500) && (weight < 5) && (dimensions < _dimensionsLimit);
            _isHard = (_model != ModelTypeYandex.DBS) && ((weight > _weightLimit) || (dimensions > _dimensionsLimit));
            PercentFactors = new Dictionary<string, decimal>
            {
                { "AllFactors", sumFactorPercent },
                { "PerevodPlateg", 1.6m }
            };
            FixCommissions = new Dictionary<string, decimal>
            {
                { "PriemPlateg", 0.12m }
            };
            switch (_model)
            {
                case ModelTypeYandex.DBS:
                    break;
                case ModelTypeYandex.FBS:
                    if (_isLightFactor)
                    {
                        PercentFactors["AllFactors"] = 0;
                        FixCommissions.Add("LightFactor", GetLightFactor(price));
                    }
                    else if (_isHard)
                    {
                        FixCommissions.Add("PerOrder", 10);
                        FixCommissions.Add("Hard", 450);
                        FixCommissions.Add("WeightOutBorder", VolumeWeightFactor());
                    }
                    else
                    {
                        FixCommissions.Add("PerOrder", 10);
                        FixCommissions.Add("WeightOutBorder", VolumeWeightFactor());
                        FixCommissions.Add("LastMile", 0);

                        PercentFactors.Add("LastMile", _tariffLastMile.percent);
                    }
                    break;
                case ModelTypeYandex.FBY:
                    if (_isLightFactor)
                    {
                        PercentFactors["AllFactors"] = 0;
                        FixCommissions.Add("LightFactor", GetLightFactor(price));
                    }
                    else if (_isHard)
                    {
                        FixCommissions.Add("Stock", 350);
                        FixCommissions.Add("Delivery", 500);
                        FixCommissions.Add("WeightOutBorder", VolumeWeightFactor());
                    }
                    else
                    {
                        FixCommissions.Add("WeightOutBorder", VolumeWeightFactor());
                        foreach (var item in _tariffWithBorders)
                        {
                            FixCommissions.Add(item.name, 0);
                            PercentFactors.Add(item.name, item.percent);
                        }
                    }
                    break;
            }
        }
        decimal GetLightFactor(decimal price)
        {
            return _model switch
            {
                ModelTypeYandex.FBS => price switch
                {
                    < 100 => 80,
                    < 300 => 95,
                    _ => 115
                },
                ModelTypeYandex.FBY => price switch
                {
                    < 100 => 30,
                    < 300 => 40,
                    _ => 55
                },
                _ => 0
            };
        }
        decimal VolumeWeightFactor()
        {
            return _model switch
            {
                ModelTypeYandex.FBS => _volumeWeight switch
                {
                    < 0.2m => 15,
                    < 0.5m => 25,
                    < 1 => 35,
                    < 2 => 45,
                    < 5 => 70,
                    < 10 => 120,
                    < 15 => 225,
                    < 25 => 350,
                    < 50 => 500,
                    _ => 600
                },
                ModelTypeYandex.FBY => _volumeWeight switch
                {
                    < 0.2m => 10,
                    < 0.5m => 20,
                    < 1 => 30,
                    < 2 => 40,
                    < 5 => 60,
                    < 10 => 100,
                    < 15 => 200,
                    < 25 => 325,
                    < 50 => 400,
                    _ => 550
                },
                _ => 0
            };
        }
        public override decimal MinPrice()
        {
            decimal calcMinPrice = base.MinPrice();
            if ((_model == ModelTypeYandex.DBS) ||
                _isLightFactor ||
                _isHard)
                return calcMinPrice;
            switch (_model)
            {
                case ModelTypeYandex.FBS:
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
                    break;
                case ModelTypeYandex.FBY:
                    calcMinPrice = GetMinPriceWithBorder(calcMinPrice);
                    break;
            }
            return calcMinPrice;
        }
    }
    public enum ModelTypeYandex
    {
        FBS = 0,
        FBY = 1,
        DBS = 2
    }
}
