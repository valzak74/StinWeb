using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;

namespace StinWeb.Models.Repository.Интерфейсы.Документы
{
    interface IАвансоваяОплата : IДокументМастерской
    {
        Task<ФормаАвансоваяОплата> ПросмотрAsync(string idDoc);
        Task<ФормаАвансоваяОплата> ВводНаОснованииAsync(string докОснованиеId, int видДокОснование, string userId, List<Корзина> тч);
        Task<ExceptionData> ЗаписатьAsync(ФормаАвансоваяОплата doc);
        Task<ExceptionData> ПровестиAsync(ФормаАвансоваяОплата doc);
        Task<ExceptionData> ЗаписатьПровестиAsync(ФормаАвансоваяОплата doc);
        Task<АктВыполненныхРаботПечать> ДанныеДляПечатиAsync(ФормаАвансоваяОплата doc);
    }
}
