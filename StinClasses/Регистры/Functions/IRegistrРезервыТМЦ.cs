using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Регистры.Functions
{
    public interface IRegistrРезервыТМЦ
    {
        Task<IEnumerable<РегистрРезервыТМЦ>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, List<string> складIds = null, string заявкаId = null);
        Task<IEnumerable<РегистрРезервыТМЦ>> ПолучитьОстаткиПоЗаявкамAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, string договорId, List<string> номенклатураIds, List<string> заявкаIds, List<string> складIds = null);
        Task<IDictionary<string, decimal>> ПолучитьКоличествоНоменклатурыВРезервахAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, IEnumerable<string> номенклатураIds, string marketplaceId = "");
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string ФирмаId,
                    string НоменклатураId,
                    string СкладId,
                    string ДоговорId,
                    string ЗаявкаId,
                    decimal Количество
                    );
    }
}
