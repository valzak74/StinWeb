using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы.Мастерская;

namespace StinWeb.Models.Repository.Интерфейсы.Документы
{
    interface IНаДиагностику : IДокументМастерской
    {
        Task<ФормаНаДиагностику> ПросмотрAsync(string idDoc);
        Task<ФормаНаДиагностику> ВводНаОснованииAsync(string докОснованиеId, int видДокОснование, string userId, List<Корзина> тч);
        Task<ExceptionData> ЗаписатьAsync(ФормаНаДиагностику doc);
        Task<ExceptionData> ПровестиAsync(ФормаНаДиагностику doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаНаДиагностику doc);
    }
}
