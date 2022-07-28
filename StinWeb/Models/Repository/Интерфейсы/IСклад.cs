using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Отчеты;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Models.Repository.Интерфейсы
{
    public interface IСклад : IDisposable
    {
        Склад GetEntityById(string Id);
        Task<Склад> GetEntityByIdAsync(string Id);
        ПодСклад GetПодСкладById(string Id);
        Task<ПодСклад> GetПодСкладByIdAsync(string Id);
        Склад КонстантаСкладДляРемонта();
        Склад КонстантаСкладСортировкиРемонтов();
        IQueryable<Склад> ПолучитьСклады();
        IQueryable<Склад> ПолучитьРазрешенныеСклады(string ПользовательId);
        IQueryable<Склад> ПолучитьСкладыМастерские();
        IQueryable<ПодСклад> ПолучитьПодСклады();
        IQueryable<ПодСклад> ПолучитьПодСклады(string СкладId);
        IQueryable<ЖурналДоставки> СформироватьОстаткиДоставки(DateTime dateReg, string фирмаId, string складId, РежимВыбора режим, string докId13, string номенклатураId);
    }
}
