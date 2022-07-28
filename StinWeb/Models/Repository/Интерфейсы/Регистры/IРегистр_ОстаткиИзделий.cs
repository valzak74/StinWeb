using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.Repository.Регистры;

namespace StinWeb.Models.Repository.Интерфейсы.Регистры
{
    public interface IРегистр_ОстаткиИзделий : IDisposable
    {
        Task<List<РегистрОстаткиИзделий>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции, string складId, string подСкладId = null);
        Task<List<РегистрОстаткиИзделий>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции, List<string> разрешенныеСкладыIds);
    }
}
