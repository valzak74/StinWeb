using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;

namespace StinWeb.Models.Repository.Интерфейсы.Документы
{
    public interface IДокумент : IDisposable
    {
        bool NeedToOpenPeriod();
        IQueryable<ОбщиеРеквизиты> ЖурналДокументов(DateTime startDate, DateTime endDate, string idDoc);
        Task<ОбщиеРеквизиты> ОбщиеРеквизитыAsync(string IdDoc);
        Task<ДокОснование> ДокОснованиеAsync(string IdDoc);
        Task<string> LockDocNoAsync(string userId, string ИдентификаторДокDds, int ДлинаНомера = 10, string FirmaId = null, string Год = null);
        Task UnLockDocNoAsync(string ИдентификаторДокDds, string DocNo, string Год = null);
        Task<ФормаПриемВРемонт> ПриемВРемонтAsync(string idDoc);
        Task<ФормаИзменениеСтатуса> ИзменениеСтатусаAsync(string idDoc);
        Task<ФормаПеремещениеИзделий> ПеремещениеИзделийAsync(string idDoc);
        Task<ФормаНаДиагностику> НаДиагностикуAsync(string idDoc);
        Task<ФормаАвансоваяОплата> АвансоваяОплатаAsync(string idDoc);
    }
}
