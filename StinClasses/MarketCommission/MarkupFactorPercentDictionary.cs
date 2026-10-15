using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StinClasses.MarketCommission;

public sealed class MarkupFactorPercentDictionary : IMarkupFactorPercentDictionary
{
    public MarkupFactorPercentDictionary(IConfiguration configuration)
    {
        var path = configuration["CommonSettings:MarkupFactorPercentPath"];
        var file = Path.Combine(path, "MarkupFactorPercentByPrice.json");
        var fileMarketAdjustment = Path.Combine(path, "MarketAdjustments.json");

        var options = new JsonSerializerOptions
        { 
            PropertyNameCaseInsensitive = true 
        };

        var jsonString = File.ReadAllText(file);
        var markupFactorPercentByPrices = JsonSerializer.Deserialize<List<MarkupFactorPercentByPrice>>(jsonString, options);
        PercentByPrices = markupFactorPercentByPrices
            .GroupBy(x => x.MaxPrice)
            .ToDictionary(
                k => k.Key,
                v => v.Max(x => x.Percent)
            );

        jsonString = File.ReadAllText(fileMarketAdjustment);
        var marketAdjustments = JsonSerializer.Deserialize<List<MarketAdjustment>>(jsonString, options);
        MultiplyerByType = marketAdjustments
            .GroupBy(x => x.MarketType)
            .ToDictionary(
                k => k.Key,
                v => v.Max(x => x.Multiplyer)
            );
    }

    public IReadOnlyDictionary<decimal, decimal> PercentByPrices { get; }
    private IReadOnlyDictionary<MarketType, decimal> MultiplyerByType { get; }

    public decimal GetPercent(decimal price)
    {
        var key = PercentByPrices.Keys
            .OrderBy(x => x)
            .FirstOrDefault(x => x >= price);

        return PercentByPrices.GetValueOrDefault(key);
    }

    public decimal GetMultiplier(MarketType marketType)
    {
        if (MultiplyerByType.TryGetValue(marketType, out var multiplier))
            return multiplier;
        return 1;
    }
}

public sealed record MarkupFactorPercentByPrice
{
    public decimal MaxPrice { get; init; }

    public decimal Percent { get; init; }
}

public sealed record MarketAdjustment
{
    public MarketType MarketType { get; init; }

    public decimal Multiplyer { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MarketType : byte
{
    Ozon = 1,
    Wb = 2,
    Yandex = 3,
    Sber = 4,
}
