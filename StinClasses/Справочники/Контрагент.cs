using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinClasses.Справочники
{
    public class Контрагент
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Наименование { get; set; }
        public string ПолнНаименование { get; set; }
        public string ИНН { get; set; }
        public string ОсновнойДоговор { get; set; }
        public string ГруппаКонтрагентов { get; set; }
        public string ЮридическийАдрес { get; set; }
        public string ФактическийАдрес { get; set; }
        public Менеджер Менеджер { get; set; }
    }
    public class Договор
    {
        public string Id { get; set; }
        public string Владелец { get; set; }
        public string Наименование { get; set; }
        public int ГлубинаОтсрочки { get; set; }
    }
    public class ТипЦен
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
    }
    public class Скидка
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public decimal Процент { get; set; }
    }
    public class СкидКарта
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public string ФИО { get; set; }
        public bool Закрыта { get; set; }
    }
    public class УсловияДоговора
    {
        public string КонтрагентId { get; set; }
        public string ГрКонтрагентовId { get; set; }
        public string ТипЦенId { get; set; }
        public string ТипЦен { get; set; }
        public decimal ПроцентНаценкиТипаЦен { get; set; }
        public bool Экспорт { get; set; }
        public string КолонкаСкидкиId { get; set; }
        public decimal СкидкаОтсрочка { get; set; }
    }
    public class УсловияДисконтКарты
    {
        public string Наименование { get; set; }
        public string ФИО { get; set; }
        public bool Корпоративная { get; set; }
        public bool Закрыта { get; set; }
        public string ТипЦен { get; set; }
        public decimal ПроцентСкидки { get; set; }
        public decimal Накоплено { get; set; }
        public decimal СледующийПредел { get; set; }
        public decimal СледующаяСкидка { get; set; }
        public IQueryable<УсловияБрендов> УсловияБрендов { get; set; }
    }
    public class УсловияБрендов
    {
        public string БрендId { get; set; }
        public string БрендНаименование { get; set; }
        public decimal БазаБренда { get; set; }
        public decimal СкидкаВсем { get; set; }
        public decimal ДопУсловияПроцент { get; set; }
        public bool БеспОтсрочка { get; set; }
        public bool БеспДоставка { get; set; }
        public decimal КолонкаСкидок { get; set; }
    }
    public class Менеджер
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
    }
    public class ГруппаКонтрагентов
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
    }
    public interface IКонтрагент : IDisposable
    {
        Task<Контрагент> GetКонтрагентAsync(string Id);
        Task<Договор> GetДоговорAsync(string Id);
        Task<List<Договор>> GetAllДоговорыAsync(string контрагентId);
        Task<ТипЦен> GetТипЦенAsync(string Id);
        Task<Скидка> GetСкидкаAsync(string Id);
        Task<БанковскийСчет> GetБанковскийСчетAsync(string Id);
        Task<СкидКарта> GetСкидКартаAsync(string Id);
        Task<bool> ПроверкаНаДилераAsync(string контрагентId, string ВидСвойства, string ЗначениеСвойства);
        Task<Контрагент> ПолучитьПоИННAsync(string инн, bool строгоеСоответствие = false);
        bool ПроверкаНаОфКомиссию(string контрагентId);
    }
    public class КонтрагентEntity : IКонтрагент
    {
        private StinDbContext _context;
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public КонтрагентEntity(StinDbContext context)
        {
            _context = context;
        }
        private Контрагент Map(Sc172 entity)
        {
            if (entity != null)
                return new Контрагент
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    Наименование = entity.Descr.Trim(),
                    ИНН = entity.Sp8380,
                    ОсновнойДоговор = entity.Sp667,
                    ГруппаКонтрагентов = entity.Sp9631
                };
            else
                return null;
        }

        public async Task<Контрагент> GetКонтрагентAsync(string Id)
        {
            var sc172 = await _context.Sc172s.FirstOrDefaultAsync(x => x.Id == Id && !x.Ismark);
            Контрагент result = Map(sc172);
            string ВидЮрЛица = "";
            string ЮрФизЛицоId = "";
            if (!string.IsNullOrEmpty(sc172.Sp521))
            {
                ВидЮрЛица = sc172.Sp521.Substring(0, 4).Trim();
                ЮрФизЛицоId = sc172.Sp521.Substring(4);
            }
            if (!string.IsNullOrEmpty(ВидЮрЛица) && !string.IsNullOrEmpty(ЮрФизЛицоId))
            {
                if (ВидЮрЛица == "DP")
                {
                    //юр лицо
                    var ДанныеЮрЛица = await _context.Sc493s.FirstOrDefaultAsync(x => x.Id == ЮрФизЛицоId && x.Ismark == false);
                    result.ПолнНаименование = ДанныеЮрЛица.Sp498.Trim();
                    result.ЮридическийАдрес = ДанныеЮрЛица.Sp666.Trim();
                    result.ФактическийАдрес = ДанныеЮрЛица.Sp499.Trim();
                }
                else if (ВидЮрЛица == "3N")
                {
                    //собственное юр лицо
                    var ДанныеЮрЛица = await _context.Sc131s.FirstOrDefaultAsync(x => x.Id == ЮрФизЛицоId && x.Ismark == false);
                    result.ПолнНаименование = ДанныеЮрЛица.Sp143.Trim();
                    result.ЮридическийАдрес = ДанныеЮрЛица.Sp149.Trim();
                    result.ФактическийАдрес = ДанныеЮрЛица.Sp144.Trim();
                }
                else
                {
                    //физ лица
                    var ДанныеФизЛица = await _context.Sc503s.FirstOrDefaultAsync(x => x.Id == ЮрФизЛицоId && x.Ismark == false);
                    result.ПолнНаименование = ДанныеФизЛица.Sp508.Trim();
                    result.ЮридическийАдрес = ДанныеФизЛица.Sp673.Trim();
                    result.ФактическийАдрес = ДанныеФизЛица.Sp674.Trim();
                }
            }
            var ВидСвойстваМенеджер = await _context.Sc546s.FirstOrDefaultAsync(x => x.Descr.Trim() == "Менеджер");
            result.Менеджер = await (from спрСвваКонтрагентов in _context.Sc558s
                                     join спрЗначениеСвойства in _context.Sc556s on спрСвваКонтрагентов.Sp560 equals спрЗначениеСвойства.Id into _спрЗначениеСвойства
                                     from спрЗначениеСвойства in _спрЗначениеСвойства.DefaultIfEmpty()
                                     where спрСвваКонтрагентов.Sp559 == ВидСвойстваМенеджер.Id && спрСвваКонтрагентов.Parentext == result.Id
                                     select new Менеджер
                                     {
                                         Id = спрЗначениеСвойства.Id,
                                         Наименование = спрЗначениеСвойства.Descr.Trim()
                                     })
                    .FirstOrDefaultAsync();

            return result;
        }
        public async Task<Договор> GetДоговорAsync(string Id)
        {
            return await (from договоры in _context.Sc204s
                          join условияДоговоров in _context.Sc9678s on договоры.Sp9664 equals условияДоговоров.Id into _условияДоговоров
                          from условияДоговоров in _условияДоговоров.DefaultIfEmpty()
                          where договоры.Id == Id && !договоры.Ismark
                          select new Договор
                          {
                              Id = договоры.Id,
                              Наименование = договоры.Descr.Trim(),
                              Владелец = договоры.Parentext,
                              ГлубинаОтсрочки = (int)(условияДоговоров != null ? условияДоговоров.Sp9696 : договоры.Sp870)
                          })
                .FirstOrDefaultAsync();
        }
        public async Task<List<Договор>> GetAllДоговорыAsync(string контрагентId)
        {
            return await (from договоры in _context.Sc204s
                          join условияДоговоров in _context.Sc9678s on договоры.Sp9664 equals условияДоговоров.Id into _условияДоговоров
                          from условияДоговоров in _условияДоговоров.DefaultIfEmpty()
                          where договоры.Parentext == контрагентId && !договоры.Ismark
                          select new Договор
                          {
                              Id = договоры.Id,
                              Наименование = договоры.Descr.Trim(),
                              Владелец = договоры.Parentext,
                              ГлубинаОтсрочки = (int)(условияДоговоров != null ? условияДоговоров.Sp9696 : договоры.Sp870)
                          })
                .ToListAsync();
        }
        public async Task<ТипЦен> GetТипЦенAsync(string Id)
        {
            return await _context.Sc219s
                .Where(x => x.Id == Id && !x.Ismark)
                .Select(y => new ТипЦен
                {
                    Id = y.Id,
                    Наименование = y.Descr.Trim()
                })
                .FirstOrDefaultAsync();
        }
        public async Task<Скидка> GetСкидкаAsync(string Id)
        {
            return await _context.Sc426s
                .Where(x => x.Id == Id && !x.Ismark)
                .Select(y => new Скидка
                {
                    Id = y.Id,
                    Наименование = y.Descr.Trim(),
                    Процент = y.Sp429
                })
                .FirstOrDefaultAsync();
        }
        public async Task<БанковскийСчет> GetБанковскийСчетAsync(string Id)
        {
            return await (from банковскиеСчета in _context.Sc1710s
                          join банки in _context.Sc163s on банковскиеСчета.Sp1712 equals банки.Id into _банки
                          from банки in _банки.DefaultIfEmpty()
                          where банковскиеСчета.Id == Id && !банковскиеСчета.Ismark
                          select new БанковскийСчет
                          {
                              Id = банковскиеСчета.Id,
                              РасчетныйСчет = банковскиеСчета.Sp4219.Trim(),
                              Банк = банки == null ? null : new Банк
                              {
                                  Id = банки.Id,
                                  Наименование = банки.Descr.Trim(),
                                  КоррСчет = банки.Sp165.Trim() ?? string.Empty,
                                  БИК = банки.Code.Trim() ?? string.Empty,
                                  Город = банки.Sp164.Trim() ?? string.Empty
                              }
                          })
                    .FirstOrDefaultAsync();
        }
        public async Task<СкидКарта> GetСкидКартаAsync(string Id)
        {
            return await _context.Sc8667s
                .Where(x => x.Id == Id && !x.Ismark)
                .Select(y => new СкидКарта
                {
                    Id = y.Id,
                    Наименование = y.Descr.Trim(),
                    ФИО = y.Sp8917.Trim(),
                    Закрыта = y.Sp9860 == 1
                })
                .FirstOrDefaultAsync();
        }
        public async Task<Контрагент> ПолучитьПоИННAsync(string инн, bool строгоеСоответствие = false)
        {
            return await _context.Sc172s.Where(x => x.Ismark == false && (строгоеСоответствие ? (x.Sp8380.Trim() == инн) : (EF.Functions.Like(x.Sp8380, $"{инн}%")))).Select(entity => new Контрагент
            {
                Id = entity.Id,
                Code = entity.Code,
                Наименование = entity.Descr.Trim(),
                ИНН = entity.Sp8380,
                ОсновнойДоговор = entity.Sp667,
                ГруппаКонтрагентов = entity.Sp9631
            }).FirstOrDefaultAsync();
        }
        public async Task<bool> ПроверкаНаДилераAsync(string контрагентId, string ВидСвойства, string ЗначениеСвойства)
        {
            return await (from свойстваКонтрагентов in _context.Sc558s
                          join видыСвойств in _context.Sc546s on свойстваКонтрагентов.Sp559 equals видыСвойств.Id
                          join значенияСвойств in _context.Sc556s on свойстваКонтрагентов.Sp560 equals значенияСвойств.Id
                          where свойстваКонтрагентов.Ismark == false &&
                              свойстваКонтрагентов.Parentext == контрагентId &&
                              видыСвойств.Descr.Trim() == ВидСвойства &&
                              значенияСвойств.Descr.Trim() == ЗначениеСвойства
                          select свойстваКонтрагентов).FirstOrDefaultAsync() != null;
        }
        public bool ПроверкаНаОфКомиссию(string контрагентId)
        {
            return _context.Sc11421s.Any(x => !x.Ismark && (x.Sp11419 == контрагентId));
        }
    }
}
