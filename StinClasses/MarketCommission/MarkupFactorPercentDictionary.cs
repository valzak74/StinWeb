using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace StinClasses.MarketCommission;

public sealed class MarkupFactorPercentDictionary : IMarkupFactorPercentDictionary
{
    public MarkupFactorPercentDictionary(IConfiguration configuration)
    {
        var path = configuration["CommonSettings:MarkupFactorPercentPath"];
        var file = Path.Combine(path, "MarkupFactorPercentByPrice.json");

        var jsonString = File.ReadAllText(file);

        var options = new JsonSerializerOptions
        { 
            PropertyNameCaseInsensitive = true 
        };
        var markupFactorPercentByPrices = JsonSerializer.Deserialize<List<MarkupFactorPercentByPrice>>(jsonString, options);
        PercentByPrices = markupFactorPercentByPrices
            .GroupBy(x => x.MaxPrice)
            .ToDictionary(
                k => k.Key,
                v => v.Max(x => x.Percent)
            );
    }

    public IReadOnlyDictionary<decimal, decimal> PercentByPrices { get; }

    public decimal GetPercent(decimal price)
    {
        var key = PercentByPrices.Keys
            .OrderBy(x => x)
            .FirstOrDefault(x => x >= price);

        return PercentByPrices.GetValueOrDefault(key);
    }
}

public sealed record MarkupFactorPercentByPrice
{
    public decimal MaxPrice { get; init; }

    public decimal Percent { get; init; }
}
