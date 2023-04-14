using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Регистры.Functions
{
    public interface IRegistrОстаткиТМЦ
    {
        Task<IEnumerable<РегистрОстаткиТМЦ>> ПолучитьОстаткиAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> номенклатураIds, string складId = null, string подСкладId = null);
        Task<IEnumerable<РегистрОстаткиТМЦ>> ПолучитьОстаткиПоСпискуСкладовAsync(DateTime dateReg, string idDocDeadLine, bool IncludeDeadLine, List<string> фирмаIds, List<string> номенклатураIds, List<string> складIds);
        Task<bool> ВыполнитьДвижениеAsync(string IdDoc, DateTime ДатаДок, int КоличествоДвижений, bool ДвижениеРасход,
                    string ФирмаId,
                    string НоменклатураId,
                    string СкладId,
                    string ПодСкладId,
                    decimal ЦенаПрод,
                    decimal Количество,
                    decimal Внутреннее
                    );
    }
}
