using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StinClasses.Регистры.Functions
{
    public interface IRegistrНаборНаСкладе
    {
        Task<List<РегистрНаборНаСкладе>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string складId, string договорId, List<string> номенклатураIds, string наборId = null);
        Task<List<РегистрНаборНаСкладе>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string складId, string договорId, List<string> номенклатураIds, List<string> наборIds);
        Task<List<string>> ПолучитьСписокАктивныхНаборовAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, string orderId, bool onlyFinished);
        Task<Dictionary<string, decimal>> ПолучитьКоличествоНоменклатурыВНаборахAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, IEnumerable<string> номенклатураIds, string marketplaceId = "");
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string ФирмаId,
                    string СкладId,
                    string ПодСкладId,
                    string ДоговорId,
                    string НаборId,
                    string НоменклатураId,
                    decimal Количество
                    );
    }
}
