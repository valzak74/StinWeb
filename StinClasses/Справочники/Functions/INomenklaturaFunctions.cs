using System.Collections.Generic;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    public interface INomenklaturaFunctions
    {
        Task<List<Номенклатура>> GetНоменклатураByListIdAsync(List<string> Ids, bool isCode = false);
        Task<IEnumerable<Номенклатура>> ПолучитьСвободныеОстатки(List<string> ФирмаIds, List<string> СкладIds, List<string> НоменклатураIds, bool IsCode = false);
        Task<IDictionary<string, decimal>> GetReserveByMarketplace(string marketplaceId, IEnumerable<string> nomIds);
    }
}
