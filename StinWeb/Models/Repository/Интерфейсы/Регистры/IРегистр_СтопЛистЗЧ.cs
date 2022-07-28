using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.Repository.Регистры;

namespace StinWeb.Models.Repository.Интерфейсы.Регистры
{
    interface IРегистр_СтопЛистЗЧ : IDisposable
    {
        Task<List<РегистрСтопЛистЗЧ>> ВыбратьДвиженияДокумента(string idDoc);
        Task<List<РегистрСтопЛистЗЧ>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string НомерКвитанции, decimal ДатаКвитанции);
    }
}
