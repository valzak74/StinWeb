using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Регистры.Functions
{
    public interface IRegistrСтопЛистЗЧ
    {
        Task<IEnumerable<РегистрСтопЛистЗЧ>> ВыбратьДвиженияДокумента(string idDoc);
        Task<IEnumerable<РегистрСтопЛистЗЧ>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> номенклатураIds, List<string> складIds);
    }
}
