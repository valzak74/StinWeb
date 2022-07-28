using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Справочники.Мастерская;

namespace StinWeb.Models.Repository.Интерфейсы
{
    public interface IМастерская : IDisposable
    {
        Task<Работа> РаботаByIdAsync(string Id);
        Работа РаботаById(string Id);
        Task<Неисправность> НеисправностьByIdAsync(string Id);
        Task<ПриложенныйДокумент> ПриложенныйДокументByIdAsync(string Id);
        Task<Мастер> МастерByIdAsync(string Id);
        IQueryable<Мастер> Мастера();
        Task<List<BinaryData>> ФотоПриемВРемонтAsync(string КвитанцияId);
        bool РеестрСообщенийByIdDoc(int ВидДок, string IdDoc);
        string DefaultPrefix(string userId, string юрлицоПрефикс);
        Task ЗаписьРеестрСообщенийAsync(int ВидДок, string IdDoc, Телефон телефон, Email почта);
    }
}
