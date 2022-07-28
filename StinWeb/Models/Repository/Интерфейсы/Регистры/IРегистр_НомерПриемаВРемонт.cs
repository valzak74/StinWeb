using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.Repository.Регистры;

namespace StinWeb.Models.Repository.Интерфейсы.Регистры
{
    public interface IРегистр_НомерПриемаВРемонт: IDisposable
    {
        Task<string> ПолучитьДокументIdAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции);
        Task<List<РегистрНомерПриемаВРемонт>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции);
    }
}
