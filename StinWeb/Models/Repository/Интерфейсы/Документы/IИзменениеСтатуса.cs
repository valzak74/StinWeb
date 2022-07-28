using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.Repository.Интерфейсы.Документы
{
    interface IИзменениеСтатуса : IДокументМастерской
    {
        Task<ФормаИзменениеСтатуса> ПросмотрAsync(string idDoc);
        Task<ФормаИзменениеСтатуса> НовыйAsync(int UserRowId);
        Task<ExceptionData> ЗаписатьAsync(ФормаИзменениеСтатуса doc);
        Task<ExceptionData> ПровестиAsync(ФормаИзменениеСтатуса doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаИзменениеСтатуса doc);
    }
}
