using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники.Мастерская;

namespace StinWeb.Models.Repository.Интерфейсы.Документы
{
    public interface IДокументМастерской : IДокумент
    {
        Task<ИнформацияИзделия> АктивироватьИзделиеAsync(string квитанцияНомер, decimal квитанцияДата, string userId, string idDoc);
    }
}
