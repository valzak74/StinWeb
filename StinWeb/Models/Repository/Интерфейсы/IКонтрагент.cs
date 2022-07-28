using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;

namespace StinWeb.Models.Repository.Интерфейсы
{
    public interface IКонтрагент : IDisposable
    {
        IQueryable<Контрагент> GetAll();
        Контрагент GetEntityById(string Id);
        Task<Контрагент> GetEntityByIdAsync(string Id);
        Task<Контрагент> НовыйКонтрагентAsync(int ВидКонтрагента, string Наименование, string ИНН, string КПП,
            string Адрес, string Телефон, string Email);
        IQueryable<Менеджер> GetAllManagers();
        IQueryable<ГруппаКонтрагентов> GetAllCustomerGroups();
        IQueryable<СкидКарта> ВсеСкидКарты();
        Task<Контрагент> ПолучитьПоИННAsync(string инн, bool строгоеСоответствие);
        Task<decimal> ПолучитьГлубинуКредитаПоДоговоруAsync(string договорId);
        Task<decimal> ПолучитьПроцентСкидкиЗаДоставкуAsync(string договорId, int типДоставки);
        Task<List<Договор>> ПолучитьДоговорыКонтрагентаAsync(string контрагентId, string фирмаId);
        Task<УсловияДоговора> ПолучитьУсловияДоговораКонтрагентаAsync(string договорId);
        Task<УсловияДисконтКарты> ПолучитьУсловияДисконтКартыAsync(string картаId);
        IQueryable<УсловияБрендов> ПолучитьУсловияБрендов(УсловияДоговора данныеДоговора);
        Task<ИнфоУсловия> ПолучитьИнфоУсловияAsync(string договорId, string картаId);
        IQueryable<ДолгиКонтрагента> ДолгиКонтрагентов(List<string> СписокФирмId, DateTime RegDate, string КонтрагентId);
        Task<List<Долги>> ДолгиКонтрагентовПросрочкаAsync(string ФирмаId, string МенеджерId, string ГруппаId, string КонтрагентId,
            bool groupГруппа, bool groupКонтрагент, bool groupДокументы, bool толькоПросроченные, bool толькоДокументыНеОбнаружены);
        Task<IOrderedEnumerable<ДолгиТаблица>> ДолгиМенеджеровAsync(string ФирмаId, string МенеджерId, string ГруппаId, string КонтрагентId,
            bool groupГруппа, bool groupКонтрагент, bool groupДокументы, bool толькоПросроченные, bool толькоДокументыНеОбнаружены);
        Task<bool> ПроверкаНаДилераAsync(string контрагентId, string ВидСвойства, string ЗначениеСвойства);
        Task<Скидка> ПолучитьСпрСкидкиПоПроцентуAsync(decimal процент);
        Task<Скидка> НовыйСпрСкидкиAsync(decimal процент);
        IQueryable<Телефон> Телефоны(string контрагентId = null);
        Task<Телефон> НовыйТелефонAsync(string контрагентId, string НомерТелефона);
        IQueryable<Email> Emails(string контрагентId = null);
        Task<Email> НовыйEmailAsync(string контрагентId, string АдресEmail);
        Task<Телефон> ТелефонByIdAsync(string Id);
        Task<Email> EmailByIdAsync(string Id);
    }
}
