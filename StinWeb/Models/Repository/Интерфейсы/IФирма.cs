using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.Repository.Интерфейсы
{
    public interface IФирма : IDisposable
    {
        Фирма GetEntityById(string Id);
        Task<Фирма> GetEntityByIdAsync(string Id);
        Task<Фирма> ПолучитьПоИННAsync(string инн, bool строгоеСоответствие);
        List<Фирма> СписокСвоихФирм();
        Task<decimal> ПолучитьУчитыватьНДСAsync(string Id);
        Task<string> ОсновнойБанковскийСчетAsync(string фирмаId);
        Task<string> ПолучитьФирмуДляОптаAsync();
        Task<string> ПолучитьФирмуДляОпта2Async();
        Task<string> ПолучитьФирмуДляОпта3Async();
        Task<bool> РазрешенаПерепродажаAsync(string fromId, string toId);
        Task<List<string>> ПолучитьСписокРазрешенныхФирмAsync(string firmaId);
    }
}
