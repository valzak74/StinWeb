using System.Collections.Generic;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    public interface IFirmaFunctions
    {
        Task<decimal> ПолучитьУчитыватьНДСAsync(string Id);
        Task<List<string>> GetListAcseptedAsync(string firmaId = null);
    }
}
