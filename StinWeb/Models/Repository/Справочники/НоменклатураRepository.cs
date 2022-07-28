using StinWeb.Models.Repository.Интерфейсы;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Data.SqlClient;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Справочники
{
    public class НоменклатураRepository : IНоменклатура, IDisposable
    {
        private StinDbContext _context;
        private КонтрагентRepository _контрагентRepository;

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _контрагентRepository.Dispose();
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
        public НоменклатураRepository(StinDbContext context)
        {
            this._context = context;
            this._контрагентRepository = new КонтрагентRepository(context);
        }
        public IQueryable<Sc84> GetAll()
        {
            return _context.Sc84s.Where(x => x.Ismark == false);
        }
        public Sc84 GetById(string Id)
        {
            return _context.Sc84s.FirstOrDefault(x => x.Ismark == false && x.Id == Id);
        }
        public async Task<Sc84> GetByIdAsync(string Id)
        {
            return await _context.Sc84s.FirstOrDefaultAsync(x => x.Ismark == false && x.Id == Id);
        }
        public async Task<Номенклатура> GetНоменклатураByIdAsync(string Id)
        {
            return await (from sc84 in _context.Sc84s
                          join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                          from sc8840 in _sc8840.DefaultIfEmpty()
                          where sc84.Isfolder == 2 && sc84.Ismark == false && sc84.Id == Id
                          select sc84.Map(sc8840)).FirstOrDefaultAsync();
        }
        public IQueryable<Производитель> GetAllBrends()
        {
            return _context.Sc8840s.Where(x => x.Ismark == false).OrderBy(x => x.Descr).Select(x => new Производитель
            {
                Id = x.Id,
                Наименование = x.Descr.Trim()
            });
        }
        public IQueryable<string> GetBrendsForFilter(РежимВыбора режим)
        {
            return _context.Sc8840s.Where(x =>
                x.Ismark == false &&
                !string.IsNullOrWhiteSpace(x.Descr) &&
                !x.Descr.Trim().EndsWith("*") &&
                (режим == РежимВыбора.Общий ? true : режим == РежимВыбора.ПоМастерской ? x.Descr.ToUpper().Contains(" ЗАПЧАСТИ") : !x.Descr.ToUpper().Contains(" ЗАПЧАСТИ")))
                .OrderBy(x => x.Descr)
                .Select(x => x.Descr.Trim());
        }
        public IQueryable<Номенклатура> GetAllWithBrend()
        {
            return from sc84 in GetAll()
                   join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                   from sc8840 in _sc8840.DefaultIfEmpty()
                   where sc84.Ismark == false && sc84.Isfolder == 2
                   select new Номенклатура
                   {
                       Id = sc84.Id,
                       ParentId = sc84.Parentid,
                       IsFolder = sc84.Isfolder == 1,
                       Code = sc84.Code.Trim(),
                       Наименование = sc84.Descr.Trim(),
                       Артикул = sc84.Sp85.Trim(),
                       ПроизводительId = sc84.Sp8842,
                       Производитель = sc8840 != null ? sc8840.Descr.Trim() : "<не указан>"
                   };
        }
        public IQueryable<Номенклатура> GetAllWithBrendБезПапкиЗапчасти()
        {
            return from sc84 in _context.VzNomenNoZaps
                   join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                   from sc8840 in _sc8840.DefaultIfEmpty()
                   where sc84.Ismark == false && sc84.Isfolder == 2
                   select new Номенклатура
                   {
                       Id = sc84.Id,
                       ParentId = sc84.Parentid,
                       IsFolder = sc84.Isfolder == 1,
                       Code = sc84.Code.Trim(),
                       Наименование = sc84.Descr.Trim(),
                       Артикул = sc84.Sp85.Trim(),
                       ПроизводительId = sc84.Sp8842,
                       Производитель = sc8840 != null ? sc8840.Descr.Trim() : "<не указан>"
                   };
        }
        public IQueryable<Номенклатура> GetAllWithBrendWithCost(string parentId)
        {
            return from sc84 in _context.Sc84s
                   join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                   from sc8840 in _sc8840.DefaultIfEmpty()
                   join vzTovar in _context.VzTovars on sc84.Id equals vzTovar.Id into _vzTovar
                   from vzTovar in _vzTovar.DefaultIfEmpty()
                   where sc84.Ismark == false && sc84.Parentid == parentId
                   select sc84.Map(sc8840, vzTovar);
        }
        public IQueryable<Номенклатура> ВсяНоменклатураБезПапок()
        {
            return from sc84 in _context.Sc84s
                   join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                   from sc8840 in _sc8840.DefaultIfEmpty()
                   where sc84.Isfolder == 2 && sc84.Ismark == false
                   select sc84.Map(sc8840);
        }
        public IQueryable<Номенклатура> ВсяНоменклатураБезПапокБрендЦена()
        {
            return from sc84 in _context.Sc84s
                   join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                   from sc8840 in _sc8840.DefaultIfEmpty()
                   join vzTovar in _context.VzTovars on sc84.Id equals vzTovar.Id into _vzTovar
                   from vzTovar in _vzTovar.DefaultIfEmpty()
                   where sc84.Ismark == false && sc84.Isfolder == 2
                   select new Номенклатура
                   {
                       Id = sc84.Id,
                       ParentId = sc84.Parentid,
                       IsFolder = sc84.Isfolder == 1,
                       Code = sc84.Code.Trim(),
                       Наименование = sc84.Descr.Trim(),
                       Артикул = sc84.Sp85.Trim(),
                       ПроизводительId = sc84.Sp8842,
                       Производитель = sc8840 != null ? sc8840.Descr.Trim() : "<не указан>",
                       Статус = sc84.Sp208 == 1 ? СтатусыНоменклатуры.СнятСПроизводства : (sc84.Sp10397 == 1 ? СтатусыНоменклатуры.ПодЗаказ : СтатусыНоменклатуры.Обычный),
                       Цена = new Цены
                       {
                           Закупочная = vzTovar != null ? vzTovar.Zakup ?? 0 : 0,
                           Оптовая = vzTovar != null ? vzTovar.Opt ?? 0 : 0,
                           Розничная = vzTovar != null ? vzTovar.Rozn ?? 0 : 0,
                           ОптСП = vzTovar != null ? vzTovar.OptSp ?? 0 : 0,
                           РозСП = vzTovar != null ? vzTovar.RoznSp ?? 0 : 0,
                       },
                   };
        }
        public async Task<IQueryable<Номенклатура>> НоменклатураЦенаКлиентаAsync(List<string> Ids, string договорId, string картаId, bool доставка, int типДоставки, string search)
        {
            if (string.IsNullOrEmpty(договорId))
            {
                string КонстантаПредварительныйКлиент = _context._1sconsts.FirstOrDefault(x => x.Id == 9248).Value;
                договорId = _context.Sc172s.FirstOrDefault(x => x.Id == КонстантаПредварительныйКлиент).Sp667 ?? Common.ПустоеЗначение;
            }
            DateTime dateReg = Common.GetRegTA(_context);
            List<string> СписокФирмId = new List<string>() { Common.FirmaIP, Common.FirmaStPlus, Common.FirmaSS };
            string ОсобыйТипЦен = (await _context._1sconsts.FirstOrDefaultAsync(x => x.Id == 9303)).Value;
            decimal БазаОсобойНаценки = (await _context.Sc219s.FirstOrDefaultAsync(x => x.Id == ОсобыйТипЦен && x.Ismark == false)).Sp221;
            decimal Порог;
            if (!Decimal.TryParse(_context._1sconsts.FirstOrDefault(x => x.Id == 12572).Value, out Порог))
                Порог = 0;
            decimal процентДоставка = 0;
            if (доставка)
            {
                процентДоставка = await _контрагентRepository.ПолучитьПроцентСкидкиЗаДоставкуAsync(договорId, типДоставки);
            }
            var ДанныеДоговора = await _контрагентRepository.ПолучитьУсловияДоговораКонтрагентаAsync(договорId);
            var ДанныеБрендов = _контрагентRepository.ПолучитьУсловияБрендов(ДанныеДоговора);
            УсловияДисконтКарты ДанныеКарты = null;
            if (ДанныеДоговора.ТипЦен == "розничные" && !string.IsNullOrEmpty(картаId))
            {
                ДанныеКарты = await _контрагентRepository.ПолучитьУсловияДисконтКартыAsync(картаId);
            }
            string ТипЦен = ДанныеКарты != null ? ДанныеКарты.ТипЦен : ДанныеДоговора.ТипЦен;
            var queryСебестоимость = from rg328 in _context.Rg328s
                                     where rg328.Period == dateReg && СписокФирмId.Contains(rg328.Sp4061)
                                     group rg328 by rg328.Sp331 into g
                                     select new
                                     {
                                         НоменклатураId = g.Key,
                                         Себестоимость = (decimal?)(g.Sum(x => x.Sp342) != 0 ? g.Sum(x => x.Sp421) / g.Sum(x => x.Sp342) : 0)
                                     };

            return from sc84 in _context.Sc84s
                   join sc75 in _context.Sc75s on sc84.Sp94 equals sc75.Id
                   join sc41 in _context.Sc41s on sc75.Sp79 equals sc41.Id
                   join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                   from sc8840 in _sc8840.DefaultIfEmpty()
                   join vzTovar in _context.VzTovars on sc84.Id equals vzTovar.Id into _vzTovar
                   from vzTovar in _vzTovar.DefaultIfEmpty()
                   join данныеБренда in ДанныеБрендов on sc84.Sp8842 equals данныеБренда.БрендId into _данныеБренда
                   from данныеБренда in _данныеБренда.DefaultIfEmpty()
                   join sebest in queryСебестоимость on sc84.Id equals sebest.НоменклатураId into _sebest
                   from sebest in _sebest.DefaultIfEmpty()
                   where sc84.Ismark == false && sc84.Isfolder == 2 &&
                   (Ids != null && Ids.Count > 0 ? Ids.Contains(sc84.Id) : true) &&
                   (string.IsNullOrEmpty(search) ? true :
                   (sc84.Sp85.StartsWith(search) || sc84.Descr.StartsWith(search) || sc8840.Descr.StartsWith(search)))
                   orderby sc84.Descr
                   let Коэффициент = sc75.Sp78 == 0 ? 1 : sc75.Sp78
                   let Себестоимость = sebest != null ? (sebest.Себестоимость ?? 0) * Коэффициент : 0
                   let ЦенаОпт = vzTovar != null ? vzTovar.Opt ?? 0 : 0
                   let ЦенаОсобая = vzTovar != null ? (vzTovar.Opt * (100 + БазаОсобойНаценки + (данныеБренда != null ? данныеБренда.БазаБренда : 0)) / 100) ?? 0 : 0
                   let ЦенаЗакуп = vzTovar != null ? vzTovar.Zakup ?? 0 : 0
                   let ЦенаРозн = vzTovar != null ? vzTovar.Rozn ?? 0 : 0
                   let ЦенаОптСП = vzTovar != null ? vzTovar.OptSp ?? 0 : 0
                   let ЦенаРознСП = vzTovar != null ? vzTovar.RoznSp ?? 0 : 0
                   let ЦенаСП = ТипЦен == "оптовые" ? ЦенаОптСП :
                                ТипЦен == "закупочные" ? 0 : ЦенаРознСП
                   let ЦенаПорогВерх = ТипЦен == "оптовые" ? ЦенаОпт :
                                       ТипЦен == "особые" ? ЦенаОсобая :
                                       ТипЦен == "закупочные" ? ЦенаЗакуп :
                                       ЦенаРозн
                   let скидКартаСкидка = ДанныеКарты != null ? (ДанныеКарты.УсловияБрендов.FirstOrDefault(t => t.БрендId == sc84.Sp8842) ?? new УсловияБрендов()).ДопУсловияПроцент : 0
                   let ЦенаПорог = ЦенаПорогВерх == 0 ? Себестоимость * (100 + Порог + (данныеБренда != null ? данныеБренда.БазаБренда : 0)) / 100 : Math.Min(ЦенаПорогВерх, Себестоимость == 0 ? ЦенаПорогВерх : Math.Max(Себестоимость, ЦенаЗакуп) * (100 + Порог + (данныеБренда != null ? данныеБренда.БазаБренда : 0)) / 100)
                   let Цена = ТипЦен == "оптовые" ? Math.Max(ЦенаПорог, ЦенаОпт == 0 ? 0 : ЦенаОпт - (данныеБренда != null ? (данныеБренда.ДопУсловияПроцент != 0 ? данныеБренда.ДопУсловияПроцент : данныеБренда.КолонкаСкидок) : 0) / 100 * ЦенаОпт) :
                              ТипЦен == "особые" ? Math.Max(ЦенаПорог, ЦенаОсобая) :
                              ТипЦен == "закупочные" ? ЦенаЗакуп :
                              Math.Max(ЦенаПорог, ЦенаРозн == 0 ? ЦенаОпт : Math.Max(ЦенаРозн - скидКартаСкидка / 100 * ЦенаРозн, ЦенаОпт))
                   let СкидкаОтсрочка = ТипЦен == "оптовые" ? (данныеБренда != null ? (данныеБренда.БеспОтсрочка ? 0 : ДанныеДоговора.СкидкаОтсрочка) : ДанныеДоговора.СкидкаОтсрочка) : 0
                   let СкидкаДоставка = ТипЦен == "оптовые" ? (данныеБренда != null ? (данныеБренда.БеспДоставка ? 0 : процентДоставка) : процентДоставка) : 
                                        ТипЦен == "розничные" ? процентДоставка : 0
                   let Скидка = ТипЦен == "оптовые" ? СкидкаОтсрочка + СкидкаДоставка + данныеБренда.СкидкаВсем :
                                ТипЦен == "розничные" ? (ДанныеКарты != null ? ДанныеКарты.ПроцентСкидки : 0) + СкидкаДоставка : 0
                   let ЦенаСоСкидкой = ТипЦен == "розничные" ? (Цена - Цена * Скидка / 100 < ЦенаОпт ? ЦенаОпт : Цена - Цена * Скидка / 100) : Цена - Цена * Скидка / 100
                   let ЦенаКлиента = ЦенаСП > 0 ? Math.Min(ЦенаСП, ЦенаСоСкидкой) : ЦенаСоСкидкой
                   select new Номенклатура
                   {
                       Id = sc84.Id,
                       ParentId = sc84.Parentid,
                       IsFolder = sc84.Isfolder == 1,
                       Code = sc84.Code.Trim(),
                       Наименование = sc84.Descr.Trim(),
                       Единица = new Единицы
                       {
                           Id = sc75.Id,
                           Наименование = sc41.Descr.Trim(),
                           Коэффициент = sc75.Sp78 == 0 ? 1 : sc75.Sp78
                       },
                       Артикул = sc84.Sp85.Trim(),
                       ПроизводительId = sc84.Sp8842,
                       Производитель = sc8840 != null ? sc8840.Descr.Trim() : "<не указан>",
                       Статус = sc84.Sp208 == 1 ? СтатусыНоменклатуры.СнятСПроизводства : (sc84.Sp10397 == 1 ? СтатусыНоменклатуры.ПодЗаказ : СтатусыНоменклатуры.Обычный),
                       Цена = new Цены
                       {
                           Закупочная = ЦенаЗакуп,
                           Оптовая = ЦенаОпт,
                           Особая = ЦенаОсобая,
                           Розничная = ЦенаРозн,
                           ОптСП = ЦенаОптСП,
                           РозСП = ЦенаРознСП,
                           Клиента = ДанныеДоговора.Экспорт ? ЦенаКлиента - ЦенаКлиента * 20 / 120 : ЦенаКлиента,
                           СП = ЦенаСП,
                           Себестоимость = Себестоимость,
                           Порог = ЦенаПорог
                       },
                   };
        }
        public async Task<IQueryable<Номенклатура>> ВсяНоменклатураБезПапокБрендЦенаОстаткиAsync(List<string> СписокФирмId, string складId, string договорId, string картаId, bool доставка, int типДоставки, string search)
        {
            DateTime dateReg = Common.GetRegTA(_context);
            if (СписокФирмId == null || СписокФирмId.Count == 0)
                СписокФирмId = new List<string>() { Common.FirmaIP, Common.FirmaStPlus, Common.FirmaSS };
            var _номк = await НоменклатураЦенаКлиентаAsync(null, договорId, картаId, доставка, типДоставки, search);
            var queryОстаток = from rg405 in _context.Rg405s
                               where rg405.Period == dateReg && rg405.Sp418 == складId && СписокФирмId.Contains(rg405.Sp4062)
                               group rg405 by rg405.Sp408 into g
                               select new
                               {
                                   НоменклатураId = g.Key,
                                   Остаток = (decimal?)g.Sum(x => x.Sp411)
                               };
            var queryОстатокОтстой = from rg405 in _context.Rg405s
                                     where rg405.Period == dateReg && rg405.Sp418 == Common.СкладОтстой
                                     group rg405 by rg405.Sp408 into g
                                     select new
                                     {
                                         НоменклатураId = g.Key,
                                         ОстатокОтстой = (decimal?)g.Sum(x => x.Sp411)
                                     };
            var queryОстатокВсего = from rg405 in _context.Rg405s
                                    where rg405.Period == dateReg
                                    group rg405 by rg405.Sp408 into g
                                    select new
                                    {
                                        НоменклатураId = g.Key,
                                        ОстатокВсего = (decimal?)g.Sum(x => x.Sp411)
                                    };
            var queryОстатокАвСп = from rg11055 in _context.Rg11055s
                                   where rg11055.Period == dateReg && rg11055.Sp11051 == складId
                                   group rg11055 by rg11055.Sp11050 into g
                                   select new
                                   {
                                       НоменклатураId = g.Key,
                                       ОстатокАвСп = (decimal?)g.Sum(x => x.Sp11054)
                                   };
            var queryРезерв = from rg4480 in _context.Rg4480s
                              where rg4480.Period == dateReg && rg4480.Sp4476 == складId
                              group rg4480 by rg4480.Sp4477 into g
                              select new
                              {
                                  НоменклатураId = g.Key,
                                  Резерв = (decimal?)g.Sum(x => x.Sp4479)
                              };
            var queryРезервВсего = from rg4480 in _context.Rg4480s
                                   where rg4480.Period == dateReg
                                   group rg4480 by rg4480.Sp4477 into g
                                   select new
                                   {
                                       НоменклатураId = g.Key,
                                       РезервВсего = (decimal?)g.Sum(x => x.Sp4479)
                                   };
            var queryОжидаемыйПриход = from rg464 in _context.Rg464s
                                       where rg464.Period == dateReg && rg464.Sp13166 != 2
                                       group rg464 by rg464.Sp466 into g
                                       select new
                                       {
                                           НоменклатураId = g.Key,
                                           ОжидаемыйПриход = (decimal?)g.Sum(x => x.Sp4471)
                                       };
            return from н in _номк
                   join rОстатки in queryОстаток on н.Id equals rОстатки.НоменклатураId into _rОстатки
                   from rОстатки in _rОстатки.DefaultIfEmpty()
                   join rОстатокОтстой in queryОстатокОтстой on н.Id equals rОстатокОтстой.НоменклатураId into _rОстатокОтстой
                   from rОстатокОтстой in _rОстатокОтстой.DefaultIfEmpty()
                   join rОстатокВсего in queryОстатокВсего on н.Id equals rОстатокВсего.НоменклатураId into _rОстатокВсего
                   from rОстатокВсего in _rОстатокВсего.DefaultIfEmpty()
                   join rОстатокАвСп in queryОстатокАвСп on н.Id equals rОстатокАвСп.НоменклатураId into _rОстатокАвСп
                   from rОстатокАвСп in _rОстатокАвСп.DefaultIfEmpty()
                   join rРезерв in queryРезерв on н.Id equals rРезерв.НоменклатураId into _rРезерв
                   from rРезерв in _rРезерв.DefaultIfEmpty()
                   join rРезервВсего in queryРезервВсего on н.Id equals rРезервВсего.НоменклатураId into _rРезервВсего
                   from rРезервВсего in _rРезервВсего.DefaultIfEmpty()
                   join rОжидаемыйПриход in queryОжидаемыйПриход on н.Id equals rОжидаемыйПриход.НоменклатураId into _rОжидаемыйПриход
                   from rОжидаемыйПриход in _rОжидаемыйПриход.DefaultIfEmpty()
                   select new Номенклатура
                   {
                       Id = н.Id,
                       ParentId = н.ParentId,
                       IsFolder = н.IsFolder,
                       Code = н.Code,
                       Наименование = н.Наименование,
                       Единица = н.Единица,
                       Артикул = н.Артикул,
                       ПроизводительId = н.ПроизводительId,
                       Производитель = н.Производитель,
                       Статус = н.Статус,
                       Цена = н.Цена,
                       Регистр = new DataManager.Справочники.Регистры
                       {
                           Остаток = (rОстатки != null ? (rОстатки.Остаток ?? 0) : 0) / н.Единица.Коэффициент,
                           ОстатокОтстой = (rОстатокОтстой != null ? (rОстатокОтстой.ОстатокОтстой ?? 0) : 0) / н.Единица.Коэффициент,
                           ОстатокВсего = (rОстатокВсего != null ? (rОстатокВсего.ОстатокВсего ?? 0) : 0) / н.Единица.Коэффициент,
                           ОстатокАвСп = (rОстатокАвСп != null ? (rОстатокАвСп.ОстатокАвСп ?? 0) : 0) / н.Единица.Коэффициент,
                           Резерв = (rРезерв != null ? (rРезерв.Резерв ?? 0) : 0) / н.Единица.Коэффициент,
                           РезервВсего = (rРезервВсего != null ? (rРезервВсего.РезервВсего ?? 0) : 0) / н.Единица.Коэффициент,
                           ОжидаемыйПриход = (rОжидаемыйПриход != null ? (rОжидаемыйПриход.ОжидаемыйПриход ?? 0) : 0) / н.Единица.Коэффициент,
                       },
                   };
        }
        public IEnumerable<Номенклатура> ПолучитьНоменклатуруПоНаименованию(string наименование)
        {
            return _context.Sc84s.Where(x => x.Descr.ToUpper().Trim() == наименование.ToUpper().Trim())
                .Select(x => new Номенклатура
                {
                    Id = x.Id,
                    ParentId = x.Parentid,
                    IsFolder = x.Isfolder == 1,
                    Code = x.Code.Trim(),
                    Наименование = x.Descr.Trim(),
                    Артикул = x.Sp85.Trim(),
                    ПроизводительId = x.Sp8842,
                    Статус = x.Sp208 == 1 ? СтатусыНоменклатуры.СнятСПроизводства : (x.Sp10397 == 1 ? СтатусыНоменклатуры.ПодЗаказ : СтатусыНоменклатуры.Обычный),
                }).AsEnumerable();
        }
        public IEnumerable<Номенклатура> ПолучитьНоменклатуруПоАртикулу(string артикул)
        {
            return _context.Sc84s.Where(x => x.Sp85.Trim() == артикул)
                .Select(x => new Номенклатура
                {
                    Id = x.Id,
                    ParentId = x.Parentid,
                    IsFolder = x.Isfolder == 1,
                    Code = x.Code.Trim(),
                    Наименование = x.Descr.Trim(),
                    Артикул = x.Sp85.Trim(),
                    ПроизводительId = x.Sp8842,
                    Статус = x.Sp208 == 1 ? СтатусыНоменклатуры.СнятСПроизводства : (x.Sp10397 == 1 ? СтатусыНоменклатуры.ПодЗаказ : СтатусыНоменклатуры.Обычный),
                }).AsEnumerable();
        }
        public IEnumerable<Номенклатура> ПолучитьНоменклатуруПоШтрихКоду(string штрихКод)
        {
            return (from sc75 in _context.Sc75s
                    join sc41 in _context.Sc41s on sc75.Sp79 equals sc41.Id
                    join sc84 in _context.Sc84s on sc75.Parentext equals sc84.Id
                    where sc75.Ismark == false && sc84.Ismark == false && sc75.Sp80.Trim() == штрихКод
                    select new Номенклатура
                    {
                        Id = sc84.Id,
                        Code = sc84.Code.Trim(),
                        IsFolder = false,
                        ParentId = sc84.Parentid,
                        Артикул = sc84.Sp85.Trim(),
                        Наименование = sc84.Descr.Trim(),
                        ПроизводительId = sc84.Sp8842,
                        Единица = new Единицы { Id = sc75.Id, Наименование = sc41.Descr.Trim(), Коэффициент = sc75.Sp78 },
                        Статус = sc84.Sp208 == 1 ? СтатусыНоменклатуры.СнятСПроизводства : (sc84.Sp10397 == 1 ? СтатусыНоменклатуры.ПодЗаказ : СтатусыНоменклатуры.Обычный),
                    }).AsEnumerable();
        }
        public IQueryable<decimal> Остаток(string Id)
        {
            return from rg405 in _context.Rg405s
                   where rg405.Period == Common.GetRegTA(_context) && rg405.Sp408 == Id
                   group rg405 by rg405.Sp408 into _g
                   select _g.Sum(x => x.Sp411);
        }
        public async Task<decimal> ОстатокAsync(string Id)
        {
            return await Остаток(Id).FirstOrDefaultAsync();
        }
        public IQueryable<ТаблицаСвободныхОстатков> ПодготовитьОстатки(DateTime RegDate, List<string> списокФирмId, List<string> списокСкладовId, List<string> списокТоваровId)
        {
            if (RegDate <= Common.min1cDate)
                RegDate = Common.GetRegTA(_context);

            var регистр = (from регОстатки in _context.Rg405s
                           where регОстатки.Period == RegDate &&
                                списокФирмId.Contains(регОстатки.Sp4062) &&
                                списокСкладовId.Contains(регОстатки.Sp418) &&
                                списокТоваровId.Contains(регОстатки.Sp408)
                           select new
                           {
                               ФирмаId = регОстатки.Sp4062,
                               НоменклатураId = регОстатки.Sp408,
                               СкладId = регОстатки.Sp418,
                               Остаток = (int)(регОстатки.Sp411 * 100000),
                               Резерв = 0,
                           })
                           .Concat
                           (from регРезервы in _context.Rg4480s
                            where регРезервы.Period == RegDate &&
                                списокФирмId.Contains(регРезервы.Sp4475) &&
                                списокСкладовId.Contains(регРезервы.Sp4476) &&
                                списокТоваровId.Contains(регРезервы.Sp4477)
                            select new
                            {
                                ФирмаId = регРезервы.Sp4475,
                                НоменклатураId = регРезервы.Sp4477,
                                СкладId = регРезервы.Sp4476,
                                Остаток = 0,
                                Резерв = (int)(регРезервы.Sp4479 * 100000),
                            });
            return from r in регистр
                   group r by new { r.ФирмаId, r.СкладId, r.НоменклатураId } into gr
                   where gr.Sum(x => x.Остаток) - gr.Sum(x => x.Резерв) > 0
                   select new ТаблицаСвободныхОстатков
                   {
                       Фирма = new Фирма { Id = gr.Key.ФирмаId },
                       Номенклатура = new Номенклатура { Id = gr.Key.НоменклатураId },
                       Склад = new Склад { Id = gr.Key.СкладId },
                       Регистры = new DataManager.Справочники.Регистры
                       {
                           Остаток = gr.Sum(x => x.Остаток) / 100000,
                           Резерв = gr.Sum(x => x.Резерв) / 100000,
                       }
                   };
        }
        public IQueryable<ТаблицаСвободныхОстатков> ПодготовитьСвободныеОстатки(DateTime RegDate, List<string> списокФирмId, List<string> списокСкладовId, List<string> списокТоваровId)
        {
            if (RegDate <= Common.min1cDate)
                RegDate = Common.GetRegTA(_context);

            var регистр = (from регОстатки in _context.Rg405s
                           where регОстатки.Period == RegDate &&
                                списокФирмId.Contains(регОстатки.Sp4062) &&
                                списокСкладовId.Contains(регОстатки.Sp418) &&
                                списокТоваровId.Contains(регОстатки.Sp408)
                           select new
                           {
                               НоменклатураId = регОстатки.Sp408,
                               СкладId = регОстатки.Sp418,
                               Остаток = (int)(регОстатки.Sp411 * 100000),
                               Резерв = 0,
                               ОстАвСп = 0
                           })
                           .Concat
                           (from регРезервы in _context.Rg4480s
                            where регРезервы.Period == RegDate &&
                                списокФирмId.Contains(регРезервы.Sp4475) &&
                                списокСкладовId.Contains(регРезервы.Sp4476) &&
                                списокТоваровId.Contains(регРезервы.Sp4477)
                            select new
                            {
                                НоменклатураId = регРезервы.Sp4477,
                                СкладId = регРезервы.Sp4476,
                                Остаток = 0,
                                Резерв = (int)(регРезервы.Sp4479 * 100000),
                                ОстАвСп = 0
                            })
                           .Concat
                           (from регАвСп in _context.Rg11055s
                            where регАвСп.Period == RegDate &&
                                списокСкладовId.Contains(регАвСп.Sp11051) &&
                                списокТоваровId.Contains(регАвСп.Sp11050)
                            select new
                            {
                                НоменклатураId = регАвСп.Sp11050,
                                СкладId = регАвСп.Sp11051,
                                Остаток = 0,
                                Резерв = 0,
                                ОстАвСп = (int)(регАвСп.Sp11054 * 100000)
                            });
            return from r in регистр
                   group r by new { r.СкладId, r.НоменклатураId } into gr
                   where gr.Sum(x => x.Остаток) - gr.Sum(x => x.Резерв) - gr.Sum(x => x.ОстАвСп) > 0
                   select new ТаблицаСвободныхОстатков
                   {
                       Номенклатура = new Номенклатура { Id = gr.Key.НоменклатураId },
                       Склад = new Склад { Id = gr.Key.СкладId },
                       Регистры = new DataManager.Справочники.Регистры
                       {
                           Остаток = gr.Sum(x => x.Остаток) / 100000,
                           Резерв = gr.Sum(x => x.Резерв) / 100000,
                           ОстатокАвСп = gr.Sum(x => x.ОстАвСп) / 100000
                       }
                   };
        }
        public IQueryable<Номенклатура> ПолучитьНоменклатуруПоАгентуПроизводителя(string контрагентId, IEnumerable<string> списокНоменклатурыId)
        {
            return from sc84 in GetAll()
                   join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id
                   where sc8840.Sp13638 == контрагентId && ((списокНоменклатурыId == null || списокНоменклатурыId.Count() == 0) ? true : списокНоменклатурыId.Contains(sc84.Id))
                   select sc84.Map(sc8840);
        }
        public async Task<Единицы> ОсновнаяЕдиницаAsync(string номенклатураId)
        {
            return await (from единицы in _context.Sc75s
                          join океи in _context.Sc41s on единицы.Sp79 equals океи.Id
                          join номенклатура in _context.Sc84s on единицы.Parentext equals номенклатура.Id
                          where номенклатура.Sp94 == единицы.Id && номенклатура.Id == номенклатураId
                          select new Единицы
                          {
                              Id = единицы.Id,
                              Наименование = океи.Code,
                              Коэффициент = единицы.Sp78
                          }).FirstOrDefaultAsync();
        }
        public async Task<СтавкаНДС> СтавкаНДСAsync(string номенклатураId)
        {
            var СтавкаId = await _context.Sc84s
                .Where(x => x.Id == номенклатураId)
                .Select(x => x.Sp103).FirstOrDefaultAsync();
            var СтавкаНаименование = "";
            if (!Common.СтавкиНДС.TryGetValue(СтавкаId, out СтавкаНаименование))
                СтавкаНаименование = "не обнаружена";
            decimal Процент = 0;
            switch (СтавкаНаименование)
            {
                case "10%":
                    Процент = 10;
                    break;
                case "20%":
                    Процент = 20;
                    break;
                case "18%":
                    Процент = 18;
                    break;
                default:
                    Процент = 0;
                    break;
            }
            return new СтавкаНДС { Id = СтавкаId, Наименование = СтавкаНаименование, Процент = Процент };
        }
        public async Task<List<Номенклатура>> АналогиНоменклатурыAsync(string номенклатураId)
        {
            return await _context.Sc552s
                .Where(x => x.Parentext == номенклатураId && x.Ismark == false)
                .Select(x => new Номенклатура { Id = x.Sp11395 })
                .ToListAsync();
        }
        public IQueryable<VzTovarImage> GetImages(string Id)
        {
            return _context.VzTovarImages.Where(x => x.Id == Id).Select(x => x);
        }
        public async Task<byte[]> GetImageAsync(int rowId)
        {
            byte[] image = null;
            var entity = await _context.VzTovarImages.FirstOrDefaultAsync(x => x.RowId == rowId);
            if (entity != null)
            {
                if (!string.IsNullOrEmpty(entity.Extension) && entity.Extension.ToLower().Trim() == "zip")
                {
                    var fileData = (await Common.UnZip(entity.Photo)).FirstOrDefault();
                    image = fileData.Value;
                }
                else
                    image = entity.Photo;
            }
            return image;
        }
        public async Task<(string НоменклатураId, string Message)> CreateNewAsync(bool Testing, string ParentId, string BrendId, string ManagerId,
            string Aртикул, string Наименование, string ЕдиницаНазвание, string Штрихкод,
            bool СнятСПроизводства, bool ПодЗаказ, bool Web,
            string Комментарий, string КраткоеОписание, string ПодробноеОписание, string Характеристики,
            string АртикулОригинал, string ТнВЭД,
            decimal Вес, decimal ВесБрутто, decimal КолМест, decimal Ширина, decimal Высота, decimal Глубина)
        {
            if (Штрихкод.Length > 50)
                return ("", "Длина штрих-кода превышает 50 символов");
            string НоменклатураId = "";
            string Message = "";
            using (var docTran = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    string ПрефиксИБ = Common.ПрефиксИБ(_context);
                    var code = _context.Sc84s.Where(x => x.Code.StartsWith(ПрефиксИБ)).Max(x => x.Code);
                    if (code == null)
                        code = "0";
                    else
                        code = code.Substring(ПрефиксИБ.Length);
                    int next_code = 0;
                    if (Int32.TryParse(code, out next_code))
                    {
                        next_code += 1;
                    }
                    code = ПрефиксИБ + next_code.ToString().PadLeft(9 - ПрефиксИБ.Length, '0');

                    НоменклатураId = Common.GenerateId(_context, 84);
                    Sc84 СпрНоменклатура = new Sc84
                    {
                        Id = НоменклатураId,
                        Parentid = ParentId,
                        Code = code,
                        Descr = Наименование.Length > 50 ? Наименование.Substring(0, 50) : Наименование,
                        Isfolder = 2,
                        Ismark = false,
                        Verstamp = 0,
                        Sp85 = Aртикул.Length > 30 ? Aртикул.Substring(0, 30) : Aртикул,
                        Sp208 = СнятСПроизводства ? 1 : 0,
                        Sp2417 = "   2Q2   ", //Перечисление.ВидНоменклатуры = Товар (пр.ТМЦ)
                        Sp97 = 0,
                        Sp5066 = 0,
                        Sp4427 = Common.ПустоеЗначение,
                        Sp103 = "    I7   ", //Перечисление.СтавкиНДС = 20%
                        Sp104 = "     1   ", //Ставка НП = 0
                        Sp8842 = BrendId,
                        Sp8845 = 0,
                        Sp8848 = Характеристики.Length > 120 ? Характеристики.Substring(0, 120) : Характеристики,
                        Sp8849 = ManagerId,
                        Sp8899 = 1,
                        Sp9304 = "",
                        Sp9305 = Common.min1cDate,
                        Sp10091 = 0,
                        Sp10366 = 0,
                        Sp10397 = ПодЗаказ ? 1 : 0,
                        Sp10406 = 0,
                        Sp10479 = "",
                        Sp10480 = Common.min1cDate,
                        Sp10481 = "",
                        Sp10535 = 1,
                        Sp10784 = 0,
                        Sp11534 = 0,
                        Sp12309 = КраткоеОписание.Length > 180 ? КраткоеОписание.Substring(0, 180) : КраткоеОписание,
                        Sp12643 = АртикулОригинал.Length > 20 ? АртикулОригинал.Substring(0, 20) : АртикулОригинал,
                        Sp12992 = ТнВЭД.Length > 20 ? ТнВЭД.Substring(0, 20) : ТнВЭД,
                        Sp13277 = 0,
                        Sp13501 = Common.ПустоеЗначение,
                        Sp95 = Комментарий,
                        Sp101 = Наименование,
                        Sp12310 = ПодробноеОписание
                    };
                    var ОКЕИ = _context.Sc41s.FirstOrDefault(x => x.Descr.Trim() == ЕдиницаНазвание.ToLower());
                    string ОКЕИ_Id = "";
                    if (ОКЕИ == null)
                        ОКЕИ_Id = "     1   "; //Штука
                    else
                        ОКЕИ_Id = ОКЕИ.Id;

                    Sc75 СпрЕдиницы = new Sc75
                    {
                        Id = Common.GenerateId(_context, 75),
                        Parentext = НоменклатураId,
                        Ismark = false,
                        Verstamp = 0,
                        Sp79 = ОКЕИ_Id,
                        Sp76 = Вес,
                        Sp14056 = ВесБрутто,
                        Sp78 = 1,
                        Sp80 = Штрихкод,
                        Sp14035 = Высота,
                        Sp14036 = Ширина,
                        Sp14037 = Глубина,
                        Sp14063 = КолМест
                    };
                    await _context.Sc75s.AddAsync(СпрЕдиницы);
                    СпрНоменклатура.Sp86 = СпрЕдиницы.Id;
                    СпрНоменклатура.Sp94 = СпрЕдиницы.Id;

                    await _context.Sc84s.AddAsync(СпрНоменклатура);

                    _1sconst СпрКонст = new _1sconst
                    {
                        Objid = НоменклатураId,
                        Id = 12329,
                        Date = DateTime.Now.Date,
                        Value = Web ? "1" : "0",
                        Docid = Common.ПустоеЗначение,
                        Time = 0,
                        Actno = 0,
                        Lineno = 0,
                        Tvalue = ""
                    };
                    await _context._1sconsts.AddAsync(СпрКонст);

                    await _context.SaveChangesAsync();
                    await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 75, СпрЕдиницы.Id);
                    await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 84, СпрНоменклатура.Id);
                    if (!Testing)
                    {
                        docTran.Commit();
                    }
                    else
                    {
                        docTran.Rollback();
                        НоменклатураId = "";
                    }
                }
                catch (SqlException ex)
                {
                    docTran.Rollback();
                    НоменклатураId = "";
                    if (ex.Number == -2)
                        Message = "timeout";
                    else
                        Message = ex.Message + Environment.NewLine + ex.InnerException;
                }
                catch (Exception ex)
                {
                    docTran.Rollback();
                    НоменклатураId = "";
                    Message = ex.Message + Environment.NewLine + ex.InnerException;
                }
            }
            return (НоменклатураId, Message);
        }
        public async Task<string> UpdateAsync(bool Testing, string Id, string ManagerId, string Артикул,
            bool NeedUpdateНаименование, string Наименование,
            bool NeedUpdateШтрихкод, string Штрихкод,
            bool NeedUpdateСнятСПроизводства, bool СнятСПроизводства,
            bool NeedUpdateПодЗаказ, bool ПодЗаказ,
            bool NeedUpdateWeb, bool Web,
            bool NeedUpdateКомментарий, string Комментарий,
            bool NeedUpdateКраткоеОписание, string КраткоеОписание,
            bool NeedUpdateПодробноеОписание, string ПодробноеОписание,
            bool NeedUpdateХарактеристики, string Характеристики,
            bool NeedUpdateАртикулОригинал, string АртикулОригинал,
            bool NeedUpdateТнВэд, string ТнВЭД,
            bool NeedUpdateВес, decimal Вес,
            bool NeedUpdateВесБрутто, decimal ВесБрутто,
            bool NeedUpdateКолМест, decimal КолМест,
            bool NeedUpdateШирина, decimal Ширина,
            bool NeedUpdateВысота, decimal Высота,
            bool NeedUpdateГлубина, decimal Глубина
            )
        {
            if (Штрихкод.Length > 50)
                return "Длина штрих-кода превышает 50 символов";
            Sc84 СпрНоменклатура = await _context.Sc84s.FirstOrDefaultAsync(x => x.Id == Id);
            if (СпрНоменклатура == null)
                return "Элемент не найден по ID";
            string Message = "";
            using (var docTran = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (NeedUpdateНаименование)
                        СпрНоменклатура.Descr = Наименование.Length > 50 ? Наименование.Substring(0, 50) : Наименование;
                    if (NeedUpdateСнятСПроизводства)
                        СпрНоменклатура.Sp208 = СнятСПроизводства ? 1 : 0;
                    if (NeedUpdateПодЗаказ)
                        СпрНоменклатура.Sp10397 = ПодЗаказ ? 1 : 0;
                    if (NeedUpdateКомментарий)
                        СпрНоменклатура.Sp95 = Комментарий;
                    if (NeedUpdateКраткоеОписание)
                        СпрНоменклатура.Sp12309 = КраткоеОписание.Length > 180 ? КраткоеОписание.Substring(0, 180) : КраткоеОписание;
                    if (NeedUpdateПодробноеОписание)
                        СпрНоменклатура.Sp12310 = ПодробноеОписание;
                    if (NeedUpdateХарактеристики)
                        СпрНоменклатура.Sp8848 = Характеристики.Length > 120 ? Характеристики.Substring(0, 120) : Характеристики;
                    if (NeedUpdateАртикулОригинал)
                        СпрНоменклатура.Sp12643 = АртикулОригинал.Length > 20 ? АртикулОригинал.Substring(0, 20) : АртикулОригинал;
                    if (NeedUpdateТнВэд)
                        СпрНоменклатура.Sp12992 = ТнВЭД.Length > 20 ? ТнВЭД.Substring(0, 20) : ТнВЭД;
                    _context.Update(СпрНоменклатура);

                    if (NeedUpdateШтрихкод || NeedUpdateВес || NeedUpdateВесБрутто || NeedUpdateКолМест ||
                        NeedUpdateШирина || NeedUpdateВысота || NeedUpdateГлубина)
                    {
                        Sc75 СпрЕдиницы = await _context.Sc75s.FirstOrDefaultAsync(x => x.Id == СпрНоменклатура.Sp86 && x.Parentext == СпрНоменклатура.Id);
                        if (СпрЕдиницы == null)
                            return "Единица не найдена";
                        if (NeedUpdateШтрихкод)
                            СпрЕдиницы.Sp80 = Штрихкод;
                        if (NeedUpdateВес)
                            СпрЕдиницы.Sp76 = Вес;
                        if (NeedUpdateВесБрутто)
                            СпрЕдиницы.Sp14056 = ВесБрутто;
                        if (NeedUpdateВысота)
                            СпрЕдиницы.Sp14035 = Высота;
                        if (NeedUpdateШирина)
                            СпрЕдиницы.Sp14036 = Ширина;
                        if (NeedUpdateГлубина)
                            СпрЕдиницы.Sp14037 = Глубина;
                        if (NeedUpdateКолМест)
                            СпрЕдиницы.Sp14063 = КолМест;
                        _context.Update(СпрЕдиницы);
                    }

                    if (NeedUpdateWeb)
                    {
                        _1sconst СпрКонст = new _1sconst
                        {
                            Objid = СпрНоменклатура.Id,
                            Id = 12329,
                            Date = DateTime.Now.Date,
                            Value = Web ? "1" : "0",
                            Docid = Common.ПустоеЗначение,
                            Time = 0,
                            Actno = 0,
                            Lineno = 0,
                            Tvalue = ""
                        };
                        await _context._1sconsts.AddAsync(СпрКонст);
                    }

                    await _context.SaveChangesAsync();
                    await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 84, СпрНоменклатура.Id);
                    if (NeedUpdateШтрихкод)
                        await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, 75, СпрНоменклатура.Sp86);

                    if (!Testing)
                        docTran.Commit();
                    else
                        docTran.Rollback();
                }
                catch (SqlException ex)
                {
                    docTran.Rollback();
                    if (ex.Number == -2)
                        Message = "timeout";
                    else
                        Message = ex.Message + Environment.NewLine + ex.InnerException;
                }
                catch (Exception ex)
                {
                    docTran.Rollback();
                    Message = ex.Message + Environment.NewLine + ex.InnerException;
                }
            }
            return Message;
        }
    }
}