using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    public interface IFirmaFunctions
    {
        Task<Фирма> GetEntityByIdAsync(string Id, CancellationToken cancellationToken);
        Task<decimal> ПолучитьУчитыватьНДСAsync(string Id);
        Task<List<string>> GetListAcseptedAsync(string firmaId = null);
    }
}
