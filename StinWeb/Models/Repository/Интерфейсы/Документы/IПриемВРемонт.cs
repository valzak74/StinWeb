using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;

namespace StinWeb.Models.Repository.Интерфейсы.Документы
{
    interface IПриемВРемонт : IДокументМастерской
    {
        Task<ФормаПриемВРемонт> ПросмотрAsync(string idDoc);
        Task<ФормаПриемВРемонт> НовыйAsync(int UserRowId, string Параметр = null);
        Task<ФормаПриемВРемонт> ВводНаОснованииAsync(string докОснованиеId, int видДокОснование, int userRowId);
        Task<ExceptionData> ЗаписатьAsync(ФормаПриемВРемонт doc);
        Task<ExceptionData> ПровестиAsync(ФормаПриемВРемонт doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПриемВРемонт doc);
        Task<ПриемВРемонтПечать> ДанныеДляПечатиAsync(ФормаПриемВРемонт doc);
        Task<ExceptionData> ОтправитьСообщенияAsync(ФормаПриемВРемонт doc);
    }
}
