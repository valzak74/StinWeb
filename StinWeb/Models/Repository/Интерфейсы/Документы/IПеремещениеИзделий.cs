using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.Repository.Интерфейсы.Документы
{
    interface IПеремещениеИзделий : IДокументМастерской
    {
        Task<ФормаПеремещениеИзделий> ПросмотрAsync(string idDoc);
        Task<ФормаПеремещениеИзделий> НовыйAsync(int UserRowId);
        //Task<ФормаПеремещениеИзделий> ВводНаОснованииAsync(string докОснованиеId, int видДокОснование);
        Task<ExceptionData> ЗаписатьAsync(ФормаПеремещениеИзделий doc);
        Task<ExceptionData> ПровестиAsync(ФормаПеремещениеИзделий doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПеремещениеИзделий doc);
    }
}
