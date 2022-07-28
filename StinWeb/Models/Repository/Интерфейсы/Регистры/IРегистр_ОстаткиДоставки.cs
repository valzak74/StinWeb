using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.Repository.Регистры;

namespace StinWeb.Models.Repository.Интерфейсы.Регистры
{
    interface IРегистр_ОстаткиДоставки : IDisposable
    {
        Task<List<РегистрОстаткиДоставки>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string складId, string докОснованиеId13, List<string> номенклатураIds);
    }
}
