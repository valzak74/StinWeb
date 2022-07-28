using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.Repository.Регистры;

namespace StinWeb.Models.Repository.Интерфейсы.Регистры
{
    public interface IРегистр_ПартииМастерской : IDisposable
    {
        Task<List<РегистрПартииМастерской>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции, string СтатусПартииId = null);
    }
}
