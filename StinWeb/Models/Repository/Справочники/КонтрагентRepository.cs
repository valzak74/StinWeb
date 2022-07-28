using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.Repository.Интерфейсы;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Справочники
{
    public class КонтрагентRepository : IКонтрагент, IDisposable
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
        public КонтрагентRepository(StinDbContext context)
        {
            this._context = context;
        }
        public IQueryable<Контрагент> GetAll()
        {
            return _context.Sc172s.Where(x => x.Ismark == false && x.Isfolder == 2)
                .Select(x => new Контрагент
                {
                    Id = x.Id,
                    ИНН = x.Sp8380.Trim(),
                    Наименование = x.Descr.Trim()
                });
        }
        public Контрагент GetEntityById(string Id)
        {
            return _context.Sc172s.FirstOrDefault(x => x.Id == Id && x.Ismark == false).Map();
        }
        public async Task<Контрагент> GetEntityByIdAsync(string Id)
        {
            var sc172 = await _context.Sc172s.FirstOrDefaultAsync(x => x.Id == Id && x.Ismark == false);
            Контрагент result = sc172.Map();
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
                    result.ЮридическийАдрес = ДанныеЮрЛица.Sp666.Trim();
                    result.ФактическийАдрес = ДанныеЮрЛица.Sp499.Trim();
                }
                else
                {
                    //физ лица
                    var ДанныеФизЛица = await _context.Sc503s.FirstOrDefaultAsync(x => x.Id == ЮрФизЛицоId && x.Ismark == false);
                    result.ЮридическийАдрес = ДанныеФизЛица.Sp673.Trim();
                    result.ФактическийАдрес = ДанныеФизЛица.Sp674.Trim();
                }
            }
            return result;
        }
        public IQueryable<Менеджер> GetAllManagers()
        {
            return (from спрЗначениеСвойства in _context.Sc556s
                    join спрСвваКонтрагентов in _context.Sc558s on спрЗначениеСвойства.Id equals спрСвваКонтрагентов.Sp560
                    where спрЗначениеСвойства.Ismark == false &&
                    спрСвваКонтрагентов.Sp559 == _context.Sc546s.FirstOrDefault(x => x.Descr.Trim() == "Менеджер").Id
                    select new Менеджер
                    {
                        Id = спрЗначениеСвойства.Id,
                        Наименование = спрЗначениеСвойства.Descr.Trim()
                    }).Distinct();
        }
        public IQueryable<ГруппаКонтрагентов> GetAllCustomerGroups()
        {
            return _context.Sc9633s.Where(x => x.Ismark == false && x.Isfolder == 2)
                .Select(x => new ГруппаКонтрагентов
                { Id = x.Id, Наименование = x.Descr.Trim() });
        }
        public IQueryable<СкидКарта> ВсеСкидКарты()
        {
            return _context.Sc8667s.Where(x => x.Ismark == false)
                .OrderBy(x => x.Descr)
                .Select(x => new СкидКарта
                {
                    Id = x.Id,
                    Наименование = x.Descr.Trim(),
                    ФИО = x.Sp8917.Trim()
                });
        }
        public async Task<List<Договор>> ПолучитьДоговорыКонтрагентаAsync(string контрагентId, string фирмаId)
        {
            return await _context.Sc204s.Where(x =>
            x.Ismark == false &&
            x.Isfolder == 2 &&
            x.Parentext == контрагентId &&
            x.Sp13487 == 1 &&
            x.Sp13486 == фирмаId
            )
                .Select(d => new Договор
                {
                    Id = d.Id,
                    Наименование = d.Descr.Trim(),
                    Владелец = d.Parentext
                }).ToListAsync();
        }
        public async Task<decimal> ПолучитьГлубинуКредитаПоДоговоруAsync(string договорId)
        {
            return await (from договоры in _context.Sc204s
                          join условияДоговоров in _context.Sc9678s on договоры.Sp9664 equals условияДоговоров.Id into _условияДоговоров
                          from условияДоговоров in _условияДоговоров.DefaultIfEmpty()
                          where договоры.Id == договорId
                          select условияДоговоров != null ? условияДоговоров.Sp9696 : договоры.Sp870
                          ).FirstOrDefaultAsync();
        }
        public async Task<decimal> ПолучитьПроцентСкидкиЗаДоставкуAsync(string договорId, int типДоставки)
        {
            return await (from договоры in _context.Sc204s
                              //join спрСкидка in _context.Sc426s on договоры.Sp10378 equals спрСкидка.Id into _спрСкидка
                              //from спрСкидка in _спрСкидка.DefaultIfEmpty()
                          join условияДоговоров in _context.Sc9678s on договоры.Sp9664 equals условияДоговоров.Id into _условияДоговоров
                          from условияДоговоров in _условияДоговоров.DefaultIfEmpty()
                          where договоры.Id == договорId
                          select условияДоговоров != null ?
                          (типДоставки == (int)ТипыДоставки.Самара ? _context.Sc426s.FirstOrDefault(x => x.Id == условияДоговоров.Sp10380).Sp429 :
                          типДоставки == (int)ТипыДоставки.СамарскаяОбл ? _context.Sc426s.FirstOrDefault(x => x.Id == условияДоговоров.Sp11181).Sp429 :
                          типДоставки == (int)ТипыДоставки.ТранспортнаяКомпания ? _context.Sc426s.FirstOrDefault(x => x.Id == условияДоговоров.Sp13112).Sp429 : 0) :
                          договоры.Sp10378 != Common.ПустоеЗначение ? _context.Sc426s.FirstOrDefault(x => x.Id == договоры.Sp10378).Sp429 : 0
                          ).FirstOrDefaultAsync();
        }
        public async Task<Скидка> ПолучитьСпрСкидкиПоПроцентуAsync(decimal процент)
        {
            Скидка результат = await _context.Sc426s
                .Where(x => x.Sp429 == процент && x.Ismark == false)
                .Select(x => new Скидка { Id = x.Id, Наименование = x.Descr.Trim(), Процент = x.Sp429 })
                .FirstOrDefaultAsync();
            if (результат == null)
                результат = await НовыйСпрСкидкиAsync(процент);
            return результат;
        }
        public async Task<Контрагент> НовыйКонтрагентAsync(int ВидКонтрагента, string Наименование, string ИНН, string КПП,
            string Адрес, string Телефон, string Email)
        {
            using (var tran = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    string ПрефиксБД = Common.ПрефиксИБ(_context);
                    var code = _context.Sc172s.Where(x => x.Code.StartsWith(ПрефиксБД)).Max(x => x.Code);
                    if (code == null)
                        code = "0";
                    else
                        code = code.Substring(ПрефиксБД.Length);
                    int next_code = 0;
                    if (Int32.TryParse(code, out next_code))
                    {
                        next_code += 1;
                    }
                    code = ПрефиксБД + next_code.ToString().PadLeft(8 - ПрефиксБД.Length, '0');

                    Sc172 Контрагент = new Sc172
                    {
                        Id = _context.GenerateId(172),
                        Parentid = ВидКонтрагента == 1 ? Common.КонтрагентИзМастерскойФизЛица : Common.КонтрагентИзМастерскойОрганизации,
                        Code = code,
                        Descr = Наименование,
                        Isfolder = 2,
                        Ismark = false,
                        Verstamp = 0,
                        Sp4137 = Common.ПустоеЗначение,
                        Sp573 = "",
                        Sp4426 = Common.ПустоеЗначение,
                        Sp572 = Email ?? "",
                        Sp583 = Common.ПустоеЗначение,
                        Sp8380 = ВидКонтрагента == 1 ? "" : ИНН + (КПП == null ? "" : "/" + КПП),
                        Sp9631 = Common.ПустоеЗначение,
                        Sp10379 = Common.ПустоеЗначение,
                        Sp12916 = 0,
                        Sp13072 = Common.ПустоеЗначение,
                        Sp13073 = 0,
                        Sp186 = ""
                    };

                    if (ВидКонтрагента == 1)
                    {
                        code = _context.Sc503s.Where(x => x.Code.StartsWith(ПрефиксБД)).Max(x => x.Code);
                        if (code == null)
                            code = "0";
                        else
                            code = code.Substring(ПрефиксБД.Length);
                        next_code = 0;
                        if (Int32.TryParse(code, out next_code))
                        {
                            next_code += 1;
                        }
                        code = ПрефиксБД + next_code.ToString().PadLeft(8 - ПрефиксБД.Length, '0');

                        Sc503 ФизЛицо = new Sc503
                        {
                            Id = _context.GenerateId(503),
                            Parentid = Common.ПустоеЗначение,
                            Code = code,
                            Descr = Наименование,
                            Isfolder = 2,
                            Ismark = false,
                            Verstamp = 0,
                            Sp508 = Наименование,
                            Sp504 = "",
                            Sp672 = string.IsNullOrEmpty(Телефон) ? "" : Regex.Replace(Телефон.Substring(2), @"\s+", "") ?? "",
                            Sp673 = Адрес ?? "",
                            Sp674 = Адрес ?? ""
                        };
                        await _context.Sc503s.AddAsync(ФизЛицо);
                        Контрагент.Sp521 = Common.Encode36(503).PadLeft(4) + ФизЛицо.Id;
                    }
                    else
                    {
                        code = _context.Sc493s.Where(x => x.Code.StartsWith(ПрефиксБД)).Max(x => x.Code);
                        if (code == null)
                            code = "0";
                        else
                            code = code.Substring(ПрефиксБД.Length);
                        next_code = 0;
                        if (Int32.TryParse(code, out next_code))
                        {
                            next_code += 1;
                        }
                        code = ПрефиксБД + next_code.ToString().PadLeft(8 - ПрефиксБД.Length, '0');

                        Sc493 ЮрЛицо = new Sc493
                        {
                            Id = _context.GenerateId(493),
                            Parentid = Common.ПустоеЗначение,
                            Code = code,
                            Descr = Наименование,
                            Isfolder = 2,
                            Ismark = false,
                            Verstamp = 0,
                            Sp498 = Наименование,
                            Sp494 = ИНН.Length == 10 ? ИНН + "/" + КПП : ИНН,
                            Sp497 = "",
                            Sp671 = string.IsNullOrEmpty(Телефон) ? "" : Regex.Replace(Телефон.Substring(2), @"\s+", "") ?? "",
                            Sp666 = Адрес ?? "",
                            Sp499 = Адрес ?? ""
                        };
                        await _context.Sc493s.AddAsync(ЮрЛицо);
                        Контрагент.Sp521 = Common.Encode36(493).PadLeft(4) + ЮрЛицо.Id;
                    }

                    code = _context.Sc204s.Where(x => x.Code.StartsWith(ПрефиксБД)).Max(x => x.Code);
                    if (code == null)
                        code = "0";
                    else
                        code = code.Substring(ПрефиксБД.Length);
                    next_code = 0;
                    if (Int32.TryParse(code, out next_code))
                    {
                        next_code += 1;
                    }
                    code = ПрефиксБД + next_code.ToString().PadLeft(8 - ПрефиксБД.Length, '0');

                    var условияДоговора = (from d in _context.Sc9678s
                                           where d.Id == Common.ДоговорыУсловияСтандартныеРозничные
                                           select new
                                           {
                                               id = d.Id,
                                               Наименование = d.Descr.Trim(),
                                               ТипЦен = d.Sp9676,
                                               Скидка = d.Sp9675,
                                               ГлубинаКредита = d.Sp9696,
                                               СкидкаДоставка = d.Sp10380
                                           }).FirstOrDefault();

                    Sc204 Договор = new Sc204
                    {
                        Id = _context.GenerateId(204),
                        Parentid = Common.ПустоеЗначение,
                        Code = code,
                        Descr = условияДоговора.Наименование,
                        Parentext = Контрагент.Id,
                        Isfolder = 2,
                        Ismark = false,
                        Verstamp = 0,
                        Sp9664 = условияДоговора.id,
                        Sp668 = Common.ВалютаРубль,
                        Sp1948 = условияДоговора.ТипЦен,
                        Sp1920 = условияДоговора.Скидка,
                        Sp870 = условияДоговора.ГлубинаКредита,
                        Sp2285 = 0,
                        Sp4764 = 1,
                        Sp8843 = Common.ПустоеЗначение,
                        Sp10377 = Common.ПустоеЗначение,
                        Sp10378 = условияДоговора.СкидкаДоставка,
                        Sp13486 = Common.FirmaSS,
                        Sp13487 = 1
                    };
                    await _context.Sc204s.AddAsync(Договор);
                    Контрагент.Sp667 = Договор.Id;

                    await _context.Sc172s.AddAsync(Контрагент);

                    await НовыйТелефонAsync(Контрагент.Id, Телефон);
                    await НовыйEmailAsync(Контрагент.Id, Email);

                    await _context.SaveChangesAsync();
                    tran.Commit();
                    return Контрагент.Map();
                }
                catch
                {
                    tran.Rollback();
                    return null;
                }
            }
        }
        public async Task<Телефон> НовыйТелефонAsync(string контрагентId, string НомерТелефона)
        {
            if (!string.IsNullOrEmpty(НомерТелефона))
            {
                Sc12393 sc12393 = new Sc12393
                {
                    Id = _context.GenerateId(12393),
                    Descr = Regex.Replace(НомерТелефона.Substring(2), @"\s+", ""),
                    Parentext = контрагентId,
                    Ismark = false,
                    Verstamp = 0
                };
                await _context.Sc12393s.AddAsync(sc12393);
                await _context.SaveChangesAsync();
                await _context.РегистрацияИзмененийРаспределеннойИБAsync(12393, sc12393.Id);
                return new Телефон { Id = sc12393.Id, КонтрагентId = sc12393.Parentext, Номер = sc12393.Descr.Trim() };
            }

            return null;
        }
        public async Task<Email> НовыйEmailAsync(string контрагентId, string АдресEmail)
        {
            if (!string.IsNullOrEmpty(АдресEmail))
            {
                Sc13650 sc13650 = new Sc13650
                {
                    Id = _context.GenerateId(13650),
                    Descr = АдресEmail,
                    Parentext = контрагентId,
                    Ismark = false,
                    Verstamp = 0
                };
                await _context.Sc13650s.AddAsync(sc13650);
                await _context.SaveChangesAsync();
                await _context.РегистрацияИзмененийРаспределеннойИБAsync(13650, sc13650.Id);
                return new Email { Id = sc13650.Id, КонтрагентId = sc13650.Parentext, Адрес = sc13650.Descr.Trim() };
            }

            return null;
        }
        public async Task<Телефон> ТелефонByIdAsync(string Id)
        {
            return await _context.Sc12393s
                .Where(x => x.Id == Id && x.Ismark == false)
                .Select(x => new Телефон
                {
                    Id = x.Id,
                    КонтрагентId = x.Parentext,
                    Номер = x.Descr.Trim()
                })
                .FirstOrDefaultAsync();
        }
        public async Task<Email> EmailByIdAsync(string Id)
        {
            return await _context.Sc13650s
                .Where(x => x.Id == Id && x.Ismark == false)
                .Select(x => new Email
                {
                    Id = x.Id,
                    КонтрагентId = x.Parentext,
                    Адрес = x.Descr.Trim()
                })
                .FirstOrDefaultAsync();
        }
        public async Task<Скидка> НовыйСпрСкидкиAsync(decimal процент)
        {
            var code = _context.Sc426s.Where(x => x.Code.StartsWith(Common.ПрефиксИБ(_context))).Max(x => x.Code);
            if (code == null)
                code = "0";
            else
                code = code.Substring(Common.ПрефиксИБ(_context).Length);
            int next_code = 0;
            if (Int32.TryParse(code, out next_code))
            {
                next_code += 1;
            }
            code = Common.ПрефиксИБ(_context) + next_code.ToString().PadLeft(5 - Common.ПрефиксИБ(_context).Length, '0');

            Sc426 entity = new Sc426
            {
                Id = Common.GenerateId(_context, 426),
                Code = code,
                Descr = (процент < 0 ? "наценка " + (-процент).ToString() : процент.ToString()) + "%",
                Ismark = false,
                Verstamp = 0,
                Sp429 = процент,
            };
            await _context.Sc426s.AddAsync(entity);
            await _context.SaveChangesAsync();
            await _context.РегистрацияИзмененийРаспределеннойИБAsync(426, entity.Id);
            return new Скидка { Id = entity.Id, Процент = entity.Sp429, Наименование = entity.Descr.Trim() };
        }
        public async Task<УсловияДоговора> ПолучитьУсловияДоговораКонтрагентаAsync(string договорId)
        {
            string РознТипЦенId = "";
            bool ПринадлежитРознице = false;
            var ГруппаРозница = (await _context._1sconsts.Where(x => x.Objid == Common.ПустоеЗначение && x.Id == 12179).OrderByDescending(t => t.Date).ThenByDescending(t => t.Time).FirstOrDefaultAsync()).Value;
            if (!string.IsNullOrEmpty(договорId) && !string.IsNullOrEmpty(ГруппаРозница))
            {
                ПринадлежитРознице = await (from договоры in _context.Sc204s
                                            join контрЭлемент in _context.Sc172s on договоры.Parentext equals контрЭлемент.Id
                                            join контрГруппа1 in _context.Sc172s on контрЭлемент.Parentid equals контрГруппа1.Id into _контрГруппа1
                                            from контрГруппа1 in _контрГруппа1.DefaultIfEmpty()
                                            where договоры.Id == договорId
                                            select ((контрЭлемент.Parentid == ГруппаРозница) || (контрГруппа1 != null && контрГруппа1.Parentid == ГруппаРозница)) ? true : false
                                            ).FirstOrDefaultAsync();
                if (ПринадлежитРознице)
                {
                    РознТипЦенId = await _context._1sconsts
                        .Where(x => x.Objid == Common.ПустоеЗначение && x.Id == 6500 && x.Date <= Common.min1cDate)
                        .Select(x => x.Value)
                        .FirstOrDefaultAsync();
                }
            }

            return await (from договоры in _context.Sc204s
                          join контрагенты in _context.Sc172s on договоры.Parentext equals контрагенты.Id
                          join грКонтрагентов in _context.Sc9633s on контрагенты.Sp9631 equals грКонтрагентов.Id into _грКонтрагентов
                          from грКонтрагентов in _грКонтрагентов.DefaultIfEmpty()
                          join типыЦен in _context.Sc219s on договоры.Sp1948 equals типыЦен.Id into _типыЦен
                          from типыЦен in _типыЦен.DefaultIfEmpty()
                          join договорСкидка in _context.Sc426s on договоры.Sp1920 equals договорСкидка.Id into _договорСкидка
                          from договорСкидка in _договорСкидка.DefaultIfEmpty()
                          where договоры.Id == договорId
                          select new УсловияДоговора
                          {
                              КонтрагентId = контрагенты.Id,
                              ГрКонтрагентовId = грКонтрагентов != null ? грКонтрагентов.Id : Common.ПустоеЗначение,
                              ТипЦенId = ПринадлежитРознице ? РознТипЦенId : типыЦен != null ? типыЦен.Id : Common.ПустоеЗначение,
                              ТипЦен = ПринадлежитРознице ? "розничные" : типыЦен != null ? типыЦен.Descr.Trim().ToLower() : "не установлен",
                              ПроцентНаценкиТипаЦен = ПринадлежитРознице ? 0 : типыЦен != null ? типыЦен.Sp221 : 0,
                              Экспорт = контрагенты.Sp12916 == 1,
                              КолонкаСкидкиId = грКонтрагентов != null ? грКонтрагентов.Sp11782 : Common.ПустоеЗначение,
                              СкидкаОтсрочка = договорСкидка != null ? договорСкидка.Sp429 : 0,
                          }).FirstOrDefaultAsync();
        }
        public async Task<УсловияДисконтКарты> ПолучитьУсловияДисконтКартыAsync(string картаId)
        {
            var Карта = await _context.Sc8667s.FirstOrDefaultAsync(x => x.Id == картаId);
            var Конст = await _context._1sconsts.Where(x => x.Objid == картаId && x.Id == 11700).OrderByDescending(t => t.Date).FirstOrDefaultAsync();
            УсловияДисконтКарты условия = new УсловияДисконтКарты()
            {
                Наименование = Карта.Descr.Trim(),
                Закрыта = Карта.Sp9860 == 1,
                ТипЦен = Карта.Sp9860 == 1 ? "розничные" : Конст != null ? Конст.Value == "2" ? "особые" : Конст.Value == "3" ? "оптовые" : "розничные" : "розничные",
                ПроцентСкидки = 0,
                Корпоративная = Карта.Sp9859 == 1,
                ФИО = Карта.Sp8917,
                Накоплено = await _context.Rg8677s.Where(x => x.Period == Common.GetRegTA(_context) && x.Sp8678 == картаId).DefaultIfEmpty().SumAsync(x => x.Sp8679) -
                            await _context.Rg8677s.Where(x => x.Period == Common.GetRegTA(_context) && x.Sp8678 == картаId).DefaultIfEmpty().SumAsync(x => x.Sp8680)
            };

            if (!условия.Закрыта)
            {
                if (условия.ТипЦен == "розничные")
                {
                    switch (условия.Наименование)
                    {
                        case "Universal":
                            условия.ПроцентСкидки = 5;
                            break;
                        case "Скидка 3%":
                            условия.ПроцентСкидки = 3;
                            break;
                        case "Скидка 5%":
                            условия.ПроцентСкидки = 5;
                            break;
                        case "Скидка 7%":
                            условия.ПроцентСкидки = 7;
                            break;
                        case "Скидка 10%":
                            условия.ПроцентСкидки = 10;
                            break;
                        default:
                            var спрПределы = await _context.Sc11674s.Where(x => x.Ismark == false && x.Sp11671 <= условия.Накоплено)
                                .OrderByDescending(x => x.Sp11671)
                                .FirstOrDefaultAsync();
                            условия.ПроцентСкидки = спрПределы != null ? спрПределы.Sp11672 : 0;
                            спрПределы = await _context.Sc11674s.Where(x => x.Ismark == false && x.Sp11671 > условия.Накоплено)
                                .OrderBy(x => x.Sp11671)
                                .FirstOrDefaultAsync();
                            условия.СледующийПредел = спрПределы != null ? спрПределы.Sp11671 : -1;
                            условия.СледующаяСкидка = спрПределы != null ? спрПределы.Sp11672 : 0;
                            break;
                    }
                    условия.УсловияБрендов = from sc13151 in _context.Sc13151s
                                             join sc8840 in _context.Sc8840s on sc13151.Sp13147 equals sc8840.Id
                                             join sc426 in _context.Sc426s on sc13151.Sp13148 equals sc426.Id
                                             where sc13151.Parentext == картаId && sc13151.Ismark == false
                                             select new УсловияБрендов
                                             {
                                                 БрендId = sc8840.Id,
                                                 БрендНаименование = sc8840.Descr.Trim(),
                                                 ДопУсловияПроцент = sc426.Sp429
                                             };
                }
            }
            return условия;
        }
        public IQueryable<УсловияБрендов> ПолучитьУсловияБрендов(УсловияДоговора данныеДоговора)
        {
            return from бренды in _context.Sc8840s
                   join спрСкидки in _context.Sc426s on бренды.Sp11254 equals спрСкидки.Id into _спрСкидки
                   from спрСкидки in _спрСкидки.DefaultIfEmpty()
                   join допУсловияКлиента in _context.Sc9671s.Where(x => x.Parentext == данныеДоговора.КонтрагентId && x.Sp11255 == 1 && x.Ismark == false) on бренды.Id equals допУсловияКлиента.Sp9667 into _допУсловияКлиента
                   from допУсловияКлиента in _допУсловияКлиента.DefaultIfEmpty()
                   join спрСкидкиУслКлиента in _context.Sc426s on допУсловияКлиента.Sp9668 equals спрСкидкиУслКлиента.Id into _спрСкидкиУслКлиента
                   from спрСкидкиУслКлиента in _спрСкидкиУслКлиента.DefaultIfEmpty()
                   join колонкиСкидок in _context.Sc11791s.Where(x => x.Ismark == false) on бренды.Id equals колонкиСкидок.Sp11784 into _колонкиСкидок
                   from колонкиСкидок in _колонкиСкидок.DefaultIfEmpty()
                   join беспОтсрочка in _context.Sc12695s.Where(x => x.Parentext == данныеДоговора.ГрКонтрагентовId && x.Ismark == false) on бренды.Id equals беспОтсрочка.Sp12693 into _беспОтсрочка
                   from беспОтсрочка in _беспОтсрочка.DefaultIfEmpty()
                   join беспДоставка in _context.Sc12692s.Where(x => x.Parentext == данныеДоговора.ГрКонтрагентовId && x.Ismark == false) on бренды.Id equals беспДоставка.Sp12690 into _беспДоставка
                   from беспДоставка in _беспДоставка.DefaultIfEmpty()
                   select new УсловияБрендов
                   {
                       БрендId = бренды.Id,
                       БрендНаименование = бренды.Descr.Trim(),
                       БазаБренда = бренды.Sp11668,
                       //СкидкаВсем = _context.Sc426s.Where(t => t.Id == бренды.Sp11254).Select(y => y.Sp429).FirstOrDefault(),
                       СкидкаВсем = спрСкидки != null ? спрСкидки.Sp429 : 0,
                       //ДопУсловияПроцент = допУсловияКлиента != null ? _context.Sc426s.Where(t => t.Id == допУсловияКлиента.Sp9668).Select(y => y.Sp429).FirstOrDefault() : 0,
                       ДопУсловияПроцент = допУсловияКлиента != null ? (спрСкидкиУслКлиента != null ? спрСкидкиУслКлиента.Sp429 : 0) : 0,
                       БеспОтсрочка = беспОтсрочка != null,
                       БеспДоставка = беспДоставка != null,
                       КолонкаСкидок = данныеДоговора.КолонкаСкидкиId == Common.ПустоеЗначение ? 0 :
                                       колонкиСкидок == null ? 0 :
                                       Common.ВидыКолонокСкидок[данныеДоговора.КолонкаСкидкиId] == "Колонка1" ? колонкиСкидок.Sp11785 :
                                       Common.ВидыКолонокСкидок[данныеДоговора.КолонкаСкидкиId] == "Колонка2" ? колонкиСкидок.Sp11786 :
                                       Common.ВидыКолонокСкидок[данныеДоговора.КолонкаСкидкиId] == "Колонка3" ? колонкиСкидок.Sp11787 :
                                       Common.ВидыКолонокСкидок[данныеДоговора.КолонкаСкидкиId] == "Колонка4" ? колонкиСкидок.Sp11788 :
                                       Common.ВидыКолонокСкидок[данныеДоговора.КолонкаСкидкиId] == "Колонка5" ? колонкиСкидок.Sp11789 : 0
                   };
        }
        public async Task<ИнфоУсловия> ПолучитьИнфоУсловияAsync(string договорId, string картаId)
        {
            var инфо = new ИнфоУсловия();
            var ДанныеДоговора = await ПолучитьУсловияДоговораКонтрагентаAsync(договорId);
            List<УсловияБрендов> ДанныеБрендов = new List<УсловияБрендов>();
            string типЦен = "розничные";
            if (ДанныеДоговора != null)
            {
                типЦен = ДанныеДоговора.ТипЦен;
                инфо.ТипЦен = ДанныеДоговора.ТипЦен.FirstCharToUpper();
                инфо.ПроцентСкидкиЗаОтсрочку = ДанныеДоговора.ТипЦен == "оптовые" ? ДанныеДоговора.СкидкаОтсрочка : 0;
                инфо.ПроцентСкидкиЗаДоставку = 0; //???
                инфо.Экспорт = ДанныеДоговора.Экспорт;
                if (ДанныеДоговора.ТипЦен == "оптовые")
                {
                    ДанныеБрендов = ПолучитьУсловияБрендов(ДанныеДоговора).AsEnumerable()
                        .Where(x =>
                        x.СкидкаВсем != 0m ||
                        x.ДопУсловияПроцент != 0m ||
                        x.БеспОтсрочка == true ||
                        x.БеспДоставка == true ||
                        (x.ДопУсловияПроцент == 0m && x.КолонкаСкидок != 0m))
                        .OrderBy(y => y.БрендНаименование)
                        .ToList();
                }
            }
            if (типЦен == "розничные" && !string.IsNullOrEmpty(картаId))
            {
                var ДанныеКарты = await ПолучитьУсловияДисконтКартыAsync(картаId);
                if (ДанныеКарты != null)
                {
                    инфо.ТипЦен = ДанныеКарты.ТипЦен.FirstCharToUpper();
                    инфо.ДисконтнаяКарта = ДанныеКарты;
                    if (ДанныеКарты.УсловияБрендов != null)
                        ДанныеБрендов = await ДанныеКарты.УсловияБрендов.ToListAsync();
                }
            }
            foreach (var инфоБренда in ДанныеБрендов)
            {
                инфо.УсловияБрендов.Add(new ИнфоУсловияБренда
                {
                    Наименование = инфоБренда.БрендНаименование,
                    ПроцентСкидки = (инфоБренда.ДопУсловияПроцент != 0 ? инфоБренда.ДопУсловияПроцент : инфоБренда.КолонкаСкидок) + инфоБренда.СкидкаВсем,
                    БеспОтсрочка = инфоБренда.БеспОтсрочка,
                    БеспДоставка = инфоБренда.БеспДоставка
                });
            }
            return инфо;
        }
        public IQueryable<ДолгиКонтрагента> ДолгиКонтрагентов(List<string> СписокФирмId, DateTime RegDate, string КонтрагентId)
        {
            if (RegDate <= Common.min1cDate)
                RegDate = Common.GetRegTA(_context);

            var регистр = (from регПокупатели in _context.Rg4335s
                           where регПокупатели.Period == RegDate && СписокФирмId.Contains(регПокупатели.Sp4322)
                           select new
                           {
                               ДоговорId = регПокупатели.Sp4323,
                               СуммаПокупатели = (int)(регПокупатели.Sp4328 * 100),
                               СуммаПоставщика = 0,
                               Сумма = регПокупатели.Sp4328
                           })
                           .Concat
                           (from регПоставщики in _context.Rg4314s
                            where регПоставщики.Period == RegDate && СписокФирмId.Contains(регПоставщики.Sp4305)
                            select new
                            {
                                ДоговорId = регПоставщики.Sp4306,
                                СуммаПокупатели = 0,
                                СуммаПоставщика = (int)(-регПоставщики.Sp4310 * 100),
                                Сумма = регПоставщики.Sp4310
                            });
            return from r in регистр
                   join спрДоговоры in _context.Sc204s on r.ДоговорId equals спрДоговоры.Id
                   where string.IsNullOrEmpty(КонтрагентId) ? true : спрДоговоры.Parentext == КонтрагентId
                   group r by спрДоговоры.Parentext into gr
                   //where gr.Sum(x => x.Сумма) != 0
                   select new ДолгиКонтрагента
                   {
                       Контрагент = new Контрагент { Id = gr.Key },
                       ДолгПокупателя = (decimal)gr.Sum(x => x.СуммаПокупатели) / 100,
                       ДолгПередПоставщиком = (decimal)gr.Sum(x => x.СуммаПоставщика) / 100,
                       Долг = gr.Sum(x => x.Сумма)
                   };
        }
        private static string GetSqlQuery(string Docs, string DocsCorrection)
        {
            return @"
                select 
                    m.IdDoc, 
                    m.DocName, 
                    m.DocNo, 
                    m.DocDate, 
                    m.DateOplata, 
                    m.Cost, 
                    case when (m.Cost > {0} and SUM(m1.previousSum) is null) then {0} when SUM(m1.Cost) > {0} then {0} - SUM(m1.previousSum) else m.Cost end as SumDolg, 
                    case when m.DateOplata < GETDATE() then 2 else 0 end + case when SUM(m1.Cost) > {0} then 1 else 0 end as Flag
                from (" +
                Docs +
                "union all " +
                DocsCorrection +
                ") m " +
                "join (" +
                "select t.DateOplata_time_iddoc, t.Cost, LAG(t.Cost) over (order by t.DateOplata_time_iddoc desc) as previousSum from (" +
                Docs +
                "union all " +
                DocsCorrection +
                ") t " +
                ") m1 ON m1.DateOplata_time_iddoc >= m.DateOplata_time_iddoc " +
                "GROUP BY m.IdDoc, m.DocName, m.DocNo, m.DocDate, m.DateOplata, m.Cost " +
                "HAVING (m.Cost > {0} and SUM(m1.previousSum) is null) or SUM(m1.Cost) <= {0} or (SUM(m1.Cost) > {0} and SUM(m1.previousSum) < {0}) "
                ;
        }
        public async Task<List<Долги>> ДолгиКонтрагентовПросрочкаAsync(string ФирмаId, string МенеджерId, string ГруппаId, string КонтрагентId,
            bool groupГруппа, bool groupКонтрагент, bool groupДокументы, bool толькоПросроченные, bool толькоДокументыНеОбнаружены)
        {
            List<string> СписокФирмId;
            if (string.IsNullOrEmpty(ФирмаId))
                СписокФирмId = new List<string>() { Common.FirmaIP, Common.FirmaStPlus, Common.FirmaSS };
            else
                СписокФирмId = new List<string>() { ФирмаId };

            List<string> КодыОперацииПоступление = new List<string>();
            КодыОперацииПоступление.AddRange(Common.КодОперации.Where(x => x.Value == "Поступление ТМЦ (купля-продажа)").Select(y => y.Key));
            КодыОперацииПоступление.Add(Common.КодОперации.FirstOrDefault(x => x.Value == "Отчет по ОтветХранению").Key);
            string codeOperation = string.Join(",", КодыОперацииПоступление.Select(x => "'" + x + "'").ToArray());
            List<string> КодыОперацииРеализация = new List<string>();
            КодыОперацииРеализация.AddRange(Common.КодОперации.Where(x => x.Value == "Реализация (купля-продажа)").Select(y => y.Key));
            string codeOperationRashod = string.Join(",", КодыОперацииРеализация.Select(x => "'" + x + "'").ToArray());

            string id36_ПоступлениеТМЦ_Вид = " 17Y";
            string id36_РеализацияТМЦ_Вид = " 18R";
            var ВидСвойстваМенеджер = await _context.Sc546s.FirstOrDefaultAsync(x => x.Descr.Trim() == "Менеджер");

            string фирмыСтрокой = string.Join(",", СписокФирмId.Select(x => "'" + x + "'").ToArray());
            string sqlDocRashod = @"
                select
                    doc.IdDoc,
                    'Реализация (купля-продажа)' as DocName,
                    j.Docno as DocNo,
                    CONVERT(datetime, left(j.date_time_iddoc,8),112) as DocDate,
                    doc.SP1588 as DateOplata,
                    convert(char(8), doc.SP1588, 112) + right(j.date_time_iddoc,15) as DateOplata_time_iddoc,
                    doc.SP1604 as Cost
                from DH1611 as doc
                inner join _1SJOURN as j on doc.IdDoc = j.IdDoc
                left join 
                    (select docCor.IdDoc, docCor.SP13362 as docOsnov from DH13369 as docCor
                    inner join _1SJOURN as j2 on docCor.IdDoc = j2.IdDoc
                    where j2.closed & 1 = 1) docCor
                    on docCor.docOsnov = {1} + doc.IdDoc
                where j.closed & 1 = 1 and j.Sp4056 in (" + фирмыСтрокой + @")
                    and doc.Sp3338 in (" + codeOperationRashod + @")
                    and docCor.IdDoc is null 
                    and doc.SP1583 = {2} ";
            string sqlDocPrihod = @"
                select
                    doc.IdDoc,
                    case when doc.Sp3333 = '   7G6   ' then 'Отчет по ОтветХранению' else 'Поступление ТМЦ (купля-продажа)' end as DocName,
                    j.Docno as DocNo,
                    CONVERT(datetime, left(j.date_time_iddoc,8),112) as DocDate,
                    doc.SP1560 as DateOplata,
                    convert(char(8), doc.SP1560, 112) + right(j.date_time_iddoc,15) as DateOplata_time_iddoc,
                    doc.SP1574 as Cost
                from DH1582 as doc
                inner join _1SJOURN as j on doc.IdDoc = j.IdDoc
                left join 
                    (select docCor.IdDoc, docCor.SP13362 as docOsnov from DH13369 as docCor
                    inner join _1SJOURN as j2 on docCor.IdDoc = j2.IdDoc
                    where j2.closed & 1 = 1) docCor
                    on docCor.docOsnov = {1} + doc.IdDoc
                where j.closed & 1 = 1 and j.Sp4056 in (" + фирмыСтрокой + @")
                    and doc.Sp3333 in (" + codeOperation + @")
                    and docCor.IdDoc is null 
                    and doc.SP1555 = {2} ";
            string sqlDocCorrection = @"
                select
                    doc.IdDoc,
                    'Корректировка Сроков Оплаты' as DocName,
                    j.Docno as DocNo,
                    CONVERT(datetime, left(j.date_time_iddoc,8),112) as DocDate,
                    docRows.SP13366 as DateOplata,
                    convert(char(8), docRows.SP13366, 112) + right(j.date_time_iddoc,15) as DateOplata_time_iddoc,
                    docRows.SP13367 as Cost
                from DH13369 as doc
                inner join _1SJOURN as j on doc.IdDoc = j.IdDoc
                inner join DT13369 as docRows on doc.IdDoc = docRows.IdDoc
                where j.closed & 1 = 1 and j.Sp4056 in (" + фирмыСтрокой + @")
                    and SUBSTRING(doc.SP13362, 1, 4) = {1}
                    and doc.SP13363 = {2} ";
            var долгиПолные = from дКонтрагентов in ДолгиКонтрагентов(СписокФирмId, DateTime.MinValue, КонтрагентId)
                              join спрКонтрагенты in _context.Sc172s on дКонтрагентов.Контрагент.Id equals спрКонтрагенты.Id
                              join спрСвваКонтрагентов in _context.Sc558s.Where(x => x.Sp559 == ВидСвойстваМенеджер.Id) on спрКонтрагенты.Id equals спрСвваКонтрагентов.Parentext into _спрСвваКонтрагентов
                              from спрСвваКонтрагентов in _спрСвваКонтрагентов.DefaultIfEmpty()
                              join спрЗначениеСвойства in _context.Sc556s on спрСвваКонтрагентов.Sp560 equals спрЗначениеСвойства.Id into _спрЗначениеСвойства
                              from спрЗначениеСвойства in _спрЗначениеСвойства.DefaultIfEmpty()
                              where (string.IsNullOrEmpty(МенеджерId) ? true : спрЗначениеСвойства.Id == МенеджерId) &&
                                (string.IsNullOrEmpty(ГруппаId) ? true : спрКонтрагенты.Sp9631 == ГруппаId)
                              select new Долги
                              {
                                  Контрагент = new Контрагент { Id = спрКонтрагенты.Id, Наименование = спрКонтрагенты.Descr.Trim(), ГруппаКонтрагентов = спрКонтрагенты.Sp9631 },
                                  Менеджер = спрЗначениеСвойства != null ? new Менеджер { Id = спрЗначениеСвойства.Id, Наименование = спрЗначениеСвойства.Descr.Trim() } : new Менеджер { Id = "<не указан>", Наименование = "<не указан>" },
                                  Долг = дКонтрагентов.Долг,
                                  ДолгПокупателя = дКонтрагентов.ДолгПокупателя,
                                  ДолгПередПоставщиком = дКонтрагентов.ДолгПередПоставщиком,
                              };
            if (groupГруппа)
            {
                долгиПолные = from d in долгиПолные
                              join спрГруппыКонтрагентов in _context.Sc9633s on d.Контрагент.ГруппаКонтрагентов equals спрГруппыКонтрагентов.Id into _спрГруппыКонтрагентов
                              from спрГруппыКонтрагентов in _спрГруппыКонтрагентов.DefaultIfEmpty()
                              select new Долги
                              {
                                  Контрагент = d.Контрагент,
                                  Менеджер = d.Менеджер,
                                  Группа = спрГруппыКонтрагентов != null ? new ГруппаКонтрагентов { Id = спрГруппыКонтрагентов.Id, Наименование = спрГруппыКонтрагентов.Descr.Trim() } : new ГруппаКонтрагентов { Id = "<не указан>", Наименование = "<не указан>" },
                                  Долг = d.Долг,
                                  ДолгПокупателя = d.ДолгПокупателя,
                                  ДолгПередПоставщиком = d.ДолгПередПоставщиком
                              };
            }

            List<Долги> СписокДолгов = await долгиПолные.ToListAsync();
            foreach (var долг in СписокДолгов)
            {
                if (долг.ДолгПокупателя > 0)
                    долг.ДокументыРеализации = from doc in _context.VzDolgs.FromSqlRaw(GetSqlQuery(sqlDocRashod, sqlDocCorrection), долг.ДолгПокупателя, id36_РеализацияТМЦ_Вид, долг.Контрагент.Id)
                                               where (толькоПросроченные ? doc.Flag > 0 : true)
                                               orderby doc.DateOplata
                                               select new ДокументДолга
                                               {
                                                   IdDoc = doc.IdDoc,
                                                   DocНазвание = doc.DocName,
                                                   DocNo = doc.DocNo,
                                                   DocDate = doc.DocDate,
                                                   ДатаОплаты = doc.DateOplata,
                                                   ОтсрочкаДней = (doc.DateOplata - doc.DocDate).TotalDays,
                                                   СуммаДокумента = doc.Cost,
                                                   СуммаТекущегоДолга = doc.Flag < 2 ? doc.SumDolg : 0,
                                                   СуммаПросроченногоДолга = doc.Flag > 1 ? doc.SumDolg : 0
                                               };
                if (долг.ДолгПередПоставщиком > 0)
                    долг.ДокументыПоступления = from doc in _context.VzDolgs.FromSqlRaw(GetSqlQuery(sqlDocPrihod, sqlDocCorrection), долг.ДолгПередПоставщиком, id36_ПоступлениеТМЦ_Вид, долг.Контрагент.Id)
                                                where (толькоПросроченные ? doc.Flag > 0 : true)
                                                orderby doc.DateOplata
                                                select new ДокументДолга
                                                {
                                                    IdDoc = doc.IdDoc,
                                                    DocНазвание = doc.DocName,
                                                    DocNo = doc.DocNo,
                                                    DocDate = doc.DocDate,
                                                    ДатаОплаты = doc.DateOplata,
                                                    ОтсрочкаДней = (doc.DateOplata - doc.DocDate).TotalDays,
                                                    СуммаДокумента = doc.Cost,
                                                    СуммаТекущегоДолга = doc.Flag < 2 ? doc.SumDolg : 0,
                                                    СуммаПросроченногоДолга = doc.Flag > 1 ? doc.SumDolg : 0
                                                };

                долг.ПокупателиТекущийДолг = долг.ДокументыРеализации.Sum(x => x.СуммаТекущегоДолга);
                долг.ПокупателиПросроченныйДолг = долг.ДокументыРеализации.Sum(x => x.СуммаПросроченногоДолга);

                долг.ПоставщикиТекущийДолг = долг.ДокументыПоступления.Sum(x => x.СуммаТекущегоДолга);
                долг.ПоставщикиПросроченныйДолг = долг.ДокументыПоступления.Sum(x => x.СуммаПросроченногоДолга);

                if (толькоПросроченные)
                {
                    долг.Долг = (долг.ПокупателиТекущийДолг + долг.ПокупателиПросроченныйДолг) -
                        (долг.ПоставщикиТекущийДолг + долг.ПоставщикиПросроченныйДолг);
                    if (долг.ДокументыРеализации.Count() > 0)
                        долг.ДолгПокупателя = долг.ДокументыРеализации.Sum(x => x.СуммаТекущегоДолга) +
                            долг.ДокументыРеализации.Sum(x => x.СуммаПросроченногоДолга);
                    if (долг.ДокументыПоступления.Count() > 0)
                        долг.ДолгПередПоставщиком = долг.ДокументыПоступления.Sum(x => x.СуммаТекущегоДолга) +
                            долг.ДокументыПоступления.Sum(x => x.СуммаПросроченногоДолга);
                }
            }
            if (толькоПросроченные)
                СписокДолгов = СписокДолгов.Where(x => x.ПокупателиПросроченныйДолг != 0 || x.ПоставщикиПросроченныйДолг != 0)
                    .Select(x => x).ToList();
            if (толькоДокументыНеОбнаружены)
                СписокДолгов = СписокДолгов.Where(x =>
                    (x.ДолгПокупателя != 0 &&
                    (x.ДокументыРеализации.Sum(y => y.СуммаТекущегоДолга) + x.ДокументыРеализации.Sum(y => y.СуммаПросроченногоДолга)) != x.ДолгПокупателя) ||
                    (x.ДолгПередПоставщиком != 0 &&
                    (x.ДокументыПоступления.Sum(y => y.СуммаТекущегоДолга) + x.ДокументыПоступления.Sum(y => y.СуммаПросроченногоДолга)) != x.ДолгПередПоставщиком))
                    .Select(x => x).ToList();
            return СписокДолгов;
        }
        public async Task<IOrderedEnumerable<ДолгиТаблица>> ДолгиМенеджеровAsync(string ФирмаId, string МенеджерId, string ГруппаId, string КонтрагентId,
            bool groupГруппа, bool groupКонтрагент, bool groupДокументы, bool толькоПросроченные, bool толькоДокументыНеОбнаружены)
        {
            var СписокДолгов = await ДолгиКонтрагентовПросрочкаAsync(ФирмаId, МенеджерId, ГруппаId, КонтрагентId,
                groupГруппа, groupКонтрагент, groupДокументы, толькоПросроченные, толькоДокументыНеОбнаружены);
            IOrderedEnumerable<ДолгиТаблица> result;
            if (groupГруппа && groupКонтрагент)
                result = СписокДолгов.GroupBy(g => new { Менеджер = g.Менеджер.Id, Группа = g.Группа.Id, Контрагент = g.Контрагент.Id })
                    .ThenBy(g => new { Менеджер = g.Менеджер.Id, Группа = g.Группа.Id, Контрагент = (string)null })
                    .ThenBy(g => new { Менеджер = g.Менеджер.Id, Группа = (string)null, Контрагент = (string)null })
                    //.ThenBy(g => new { Менеджер = (string)null, Группа = (string)null, Контрагент = (string)null })
                    .Select(s => new ДолгиТаблица
                    {
                        Флаг = (string.IsNullOrEmpty(s.Key.Группа) && string.IsNullOrEmpty(s.Key.Контрагент)) ? 0 :
                            string.IsNullOrEmpty(s.Key.Контрагент) ? 1 : 2,
                        Наименование = (string.IsNullOrEmpty(s.Key.Группа) && string.IsNullOrEmpty(s.Key.Контрагент)) ? s.FirstOrDefault(x => x.Менеджер.Id == s.Key.Менеджер).Менеджер.Наименование :
                            string.IsNullOrEmpty(s.Key.Контрагент) ? s.FirstOrDefault(x => x.Группа.Id == s.Key.Группа).Группа.Наименование : s.FirstOrDefault(x => x.Контрагент.Id == s.Key.Контрагент).Контрагент.Наименование,
                        Менеджер = string.IsNullOrEmpty(s.Key.Менеджер) ? "" : s.FirstOrDefault(x => x.Менеджер.Id == s.Key.Менеджер).Менеджер.Наименование,
                        Группа = string.IsNullOrEmpty(s.Key.Группа) ? "" : s.FirstOrDefault(x => x.Группа.Id == s.Key.Группа).Группа.Наименование,
                        Контрагент = string.IsNullOrEmpty(s.Key.Контрагент) ? "" : s.FirstOrDefault(x => x.Контрагент.Id == s.Key.Контрагент).Контрагент.Наименование,
                        ДокументыРеализации = (!string.IsNullOrEmpty(s.Key.Контрагент) && groupДокументы) ? s.SelectMany(x => x.ДокументыРеализации) : new List<ДокументДолга>(),
                        ДокументыПоступления = (!string.IsNullOrEmpty(s.Key.Контрагент) && groupДокументы) ? s.SelectMany(x => x.ДокументыПоступления) : new List<ДокументДолга>(),
                        Count = s.Count(),
                        Долг = s.Sum(a => a.Долг),
                        Покупатели_Долг = s.Sum(a => a.ДолгПокупателя),
                        Покупатели_ТекущийДолг = s.Sum(a => a.ПокупателиТекущийДолг),
                        Покупатели_ПросроченныйДолг = s.Sum(a => a.ПокупателиПросроченныйДолг),
                        Поставщики_Долг = s.Sum(a => a.ДолгПередПоставщиком),
                        Поставщики_ТекущийДолг = s.Sum(a => a.ПоставщикиТекущийДолг),
                        Поставщики_ПросроченныйДолг = s.Sum(a => a.ПоставщикиПросроченныйДолг),
                    })
                    .OrderBy(x => x.Менеджер)
                    .ThenBy(x => x.Группа)
                    .ThenBy(x => x.Контрагент)
                    ;
            else if (groupГруппа)
                result = СписокДолгов.GroupBy(g => new { Менеджер = g.Менеджер.Id, Группа = g.Группа.Id })
                    .ThenBy(g => new { Менеджер = g.Менеджер.Id, Группа = (string)null })
                    .Select(s => new ДолгиТаблица
                    {
                        Флаг = string.IsNullOrEmpty(s.Key.Группа) ? 0 : 1,
                        Наименование = string.IsNullOrEmpty(s.Key.Группа) ? s.FirstOrDefault(x => x.Менеджер.Id == s.Key.Менеджер).Менеджер.Наименование :
                            s.FirstOrDefault(x => x.Группа.Id == s.Key.Группа).Группа.Наименование,
                        Менеджер = string.IsNullOrEmpty(s.Key.Менеджер) ? "" : s.FirstOrDefault(x => x.Менеджер.Id == s.Key.Менеджер).Менеджер.Наименование,
                        Группа = string.IsNullOrEmpty(s.Key.Группа) ? "" : s.FirstOrDefault(x => x.Группа.Id == s.Key.Группа).Группа.Наименование,
                        ДокументыРеализации = new List<ДокументДолга>(),
                        ДокументыПоступления = new List<ДокументДолга>(),
                        Count = s.Count(),
                        Долг = s.Sum(a => a.Долг),
                        Покупатели_Долг = s.Sum(a => a.ДолгПокупателя),
                        Покупатели_ТекущийДолг = s.Sum(a => a.ПокупателиТекущийДолг),
                        Покупатели_ПросроченныйДолг = s.Sum(a => a.ПокупателиПросроченныйДолг),
                        Поставщики_Долг = s.Sum(a => a.ДолгПередПоставщиком),
                        Поставщики_ТекущийДолг = s.Sum(a => a.ПоставщикиТекущийДолг),
                        Поставщики_ПросроченныйДолг = s.Sum(a => a.ПоставщикиПросроченныйДолг),
                    })
                    .OrderBy(x => x.Менеджер)
                    .ThenBy(x => x.Группа)
                    ;
            else if (groupКонтрагент)
                result = СписокДолгов.GroupBy(g => new { Менеджер = g.Менеджер.Id, Контрагент = g.Контрагент.Id })
                    .ThenBy(g => new { Менеджер = g.Менеджер.Id, Контрагент = (string)null })
                    .Select(s => new ДолгиТаблица
                    {
                        Флаг = string.IsNullOrEmpty(s.Key.Контрагент) ? 0 : 1,
                        Наименование = string.IsNullOrEmpty(s.Key.Контрагент) ? s.FirstOrDefault(x => x.Менеджер.Id == s.Key.Менеджер).Менеджер.Наименование :
                            s.FirstOrDefault(x => x.Контрагент.Id == s.Key.Контрагент).Контрагент.Наименование,
                        Менеджер = string.IsNullOrEmpty(s.Key.Менеджер) ? "" : s.FirstOrDefault(x => x.Менеджер.Id == s.Key.Менеджер).Менеджер.Наименование,
                        Контрагент = string.IsNullOrEmpty(s.Key.Контрагент) ? "" : s.FirstOrDefault(x => x.Контрагент.Id == s.Key.Контрагент).Контрагент.Наименование,
                        ДокументыРеализации = (!string.IsNullOrEmpty(s.Key.Контрагент) && groupДокументы) ? s.SelectMany(x => x.ДокументыРеализации) : new List<ДокументДолга>(),
                        ДокументыПоступления = (!string.IsNullOrEmpty(s.Key.Контрагент) && groupДокументы) ? s.SelectMany(x => x.ДокументыПоступления) : new List<ДокументДолга>(),
                        Count = s.Count(),
                        Долг = s.Sum(a => a.Долг),
                        Покупатели_Долг = s.Sum(a => a.ДолгПокупателя),
                        Покупатели_ТекущийДолг = s.Sum(a => a.ПокупателиТекущийДолг),
                        Покупатели_ПросроченныйДолг = s.Sum(a => a.ПокупателиПросроченныйДолг),
                        Поставщики_Долг = s.Sum(a => a.ДолгПередПоставщиком),
                        Поставщики_ТекущийДолг = s.Sum(a => a.ПоставщикиТекущийДолг),
                        Поставщики_ПросроченныйДолг = s.Sum(a => a.ПоставщикиПросроченныйДолг),
                    })
                    .OrderBy(x => x.Менеджер)
                    .ThenBy(x => x.Контрагент)
                    ;
            else
                result = СписокДолгов.GroupBy(g => g.Менеджер.Id)
                    .Select(s => new ДолгиТаблица
                    {
                        Флаг = 0,
                        Наименование = s.FirstOrDefault(x => x.Менеджер.Id == s.Key).Менеджер.Наименование,
                        ДокументыРеализации = new List<ДокументДолга>(),
                        ДокументыПоступления = new List<ДокументДолга>(),
                        Count = s.Count(),
                        Долг = s.Sum(a => a.Долг),
                        Покупатели_Долг = s.Sum(a => a.ДолгПокупателя),
                        Покупатели_ТекущийДолг = s.Sum(a => a.ПокупателиТекущийДолг),
                        Покупатели_ПросроченныйДолг = s.Sum(a => a.ПокупателиПросроченныйДолг),
                        Поставщики_Долг = s.Sum(a => a.ДолгПередПоставщиком),
                        Поставщики_ТекущийДолг = s.Sum(a => a.ПоставщикиТекущийДолг),
                        Поставщики_ПросроченныйДолг = s.Sum(a => a.ПоставщикиПросроченныйДолг),
                    })
                    .OrderBy(x => x.Наименование)
                    ;

            return result;
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
        public async Task<Контрагент> ПолучитьПоИННAsync(string инн, bool строгоеСоответствие=false)
        {
            if (строгоеСоответствие)
                return (await _context.Sc172s.FirstOrDefaultAsync(x => x.Ismark == false && x.Sp8380.Trim() == инн)).Map();
            else
                return (await _context.Sc172s.FirstOrDefaultAsync(x => x.Ismark == false && EF.Functions.Like(x.Sp8380, $"{инн}%"))).Map();
        }
        public IQueryable<Телефон> Телефоны(string контрагентId = null)
        {
            return _context.Sc12393s
                .Where(x => x.Ismark == false && (string.IsNullOrEmpty(контрагентId) ? true : x.Parentext == контрагентId))
                .Select(x => new Телефон
                {
                    Id = x.Id,
                    КонтрагентId = x.Parentext,
                    Номер = x.Descr.Trim()
                });
        }
        public IQueryable<Email> Emails(string контрагентId = null)
        {
            return _context.Sc13650s
                .Where(x => x.Ismark == false && (string.IsNullOrEmpty(контрагентId) ? true : x.Parentext == контрагентId))
                .Select(x => new Email
                {
                    Id = x.Id,
                    КонтрагентId = x.Parentext,
                    Адрес = x.Descr.Trim()
                });
        }
    }
}
