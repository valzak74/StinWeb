using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Регистры;
using StinClasses.Models;
using System.Threading;
using HttpExtensions;

namespace StinClasses.Справочники
{
    public static class ВидыНоменклатуры
    {
        public static string Товар { get { return "   2Q2   "; } }
        public static string Услуга { get { return "   1X3   "; } }
        public static string Работа { get { return "   690   "; } }
    }
    public class Номенклатура
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public bool IsFolder { get; set; }
        public string Code { get; set; }
        public string Артикул { get; set; }
        public string Наименование { get; set; }
        public bool ЭтоУслуга { get; set; }
        public bool PickupOnly { get; set; }
        public Производитель Производитель { get; set; }
        public Единица Единица { get; set; }
        public СтатусыНоменклатуры Статус { get; set; }
        public Цены Цена { get; set; }
        public List<Остатки> Остатки { get; set; }
        public Номенклатура()
        {
            Остатки = new List<Остатки>();
        }
    }
    public class Производитель
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
    }
    public class Единица
    {
        public string Id { get; set; }
        public string Код { get; set; }
        public string Наименование { get; set; }
        public decimal Коэффициент { get; set; }
    }
    public class СтавкаНДС
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public decimal Процент { get; set; }
    }
    public class Цены
    {
        public decimal Закупочная { get; set; }
        public decimal Оптовая { get; set; }
        public decimal Розничная { get; set; }
        public decimal Особая { get; set; }
        public decimal ОптСП { get; set; }
        public decimal РозСП { get; set; }
        public decimal Клиента { get; set; }
        public decimal СП { get; set; }
        public decimal Себестоимость { get; set; }
        public decimal Порог { get; set; }

    }
    public class Остатки
    {
        public string ФирмаId { get; set; }
        public string СкладId { get; set; }
        public decimal ОстатокТМЦ { get; set; }
        public decimal РезервТМЦ { get; set; }
        public decimal АварийныйСписок { get; set; }
        public decimal СвободныйОстаток
        {
            get { return Math.Max(ОстатокТМЦ - РезервТМЦ - АварийныйСписок, 0m); }
        }
    }
    public enum СтатусыНоменклатуры : short
    {
        Обычный = 0,
        ПодЗаказ = 1,
        СнятСПроизводства = 2
    }
    public interface IНоменклатура : IDisposable
    {
        Task<List<Номенклатура>> GetНоменклатураByListIdAsync(List<string> Ids);
        Task<List<Номенклатура>> GetНоменклатураByListCodeAsync(List<string> Codes);
        Task<string> GetIdByCode(string code);
        Task<Номенклатура> GetНоменклатураByIdAsync(string Id);
        Task<Единица> GetЕдиницаByIdAsync(string Id);
        СтавкаНДС GetСтавкаНДС(string Id);
        Task<СтавкаНДС> GetСтавкаНДСAsync(string номенклатураId);
        Task<bool> IsОсновноеСвойствоКомиссияAsync(string номенклатураId);
        Task<List<Номенклатура>> ПолучитьСвободныеОстатки(List<string> ФирмаIds, List<string> СкладIds, List<string> НоменклатураIds, bool IsCode = false);
        IQueryable<Номенклатура> ПолучитьНоменклатуруПоАгентуПроизводителя(string контрагентId, IEnumerable<string> списокНоменклатурыId);
        Task<List<Номенклатура>> АналогиНоменклатурыAsync(string номенклатураId);
        Task<decimal> ПолучитьКоличествоМест(string номенклатураId);
        Task<Dictionary<string, decimal>> ПолучитьКвант(List<string> nomenkCodes, CancellationToken cancellationToken);
        Task ОбновитьЗначенияПараметров(string code, string краткоеОписание, string подробноеОписание, string комментарий,
            decimal весБруттоКг,
            decimal длинаМ, decimal ширинаМ, decimal высотаМ,
            IHttpService httpService, List<string> pictureUrls,
            CancellationToken cancellationToken);
    }
    public class НоменклатураEntity : IНоменклатура
    {
        private StinDbContext _context;
        private bool disposed = false;
        private IРегистрОстаткиТМЦ _регистрОстаткиТМЦ;
        private IРегистрРезервыТМЦ _регистрРезервыТМЦ;
        private IРегистрСтопЛистЗЧ _регистрСтопЛистЗЧ;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистрОстаткиТМЦ.Dispose();
                    _регистрРезервыТМЦ.Dispose();
                    _регистрСтопЛистЗЧ.Dispose();
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
        public НоменклатураEntity(StinDbContext context)
        {
            _context = context;
            _регистрОстаткиТМЦ = new Регистр_ОстаткиТМЦ(context);
            _регистрРезервыТМЦ = new Регистр_РезервыТМЦ(context);
            _регистрСтопЛистЗЧ = new Регистр_СтопЛистЗЧ(context);
        }
        public async Task<List<Номенклатура>> GetНоменклатураByListIdAsync(List<string> Ids)
        {
            return await (from sc84 in _context.Sc84s
                          join sc75 in _context.Sc75s on sc84.Sp94 equals sc75.Id
                          join sc41 in _context.Sc41s on sc75.Sp79 equals sc41.Id
                          join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                          from sc8840 in _sc8840.DefaultIfEmpty()
                          join vzTovar in _context.VzTovars on sc84.Id equals vzTovar.Id into _vzTovar
                          from vzTovar in _vzTovar.DefaultIfEmpty()
                          where sc84.Isfolder == 2 && sc84.Ismark == false && Ids.Contains(sc84.Id)
                          select new Номенклатура
                          {
                              Id = sc84.Id,
                              ParentId = sc84.Parentid,
                              IsFolder = sc84.Isfolder == 1,
                              Code = sc84.Code.Trim(),
                              Наименование = sc84.Descr.Trim(),
                              Артикул = sc84.Sp85.Trim(),
                              ЭтоУслуга = sc84.Sp2417 != ВидыНоменклатуры.Товар,
                              PickupOnly = sc84.Sp14121 == 1,
                              Единица = new Единица
                              {
                                  Id = sc75.Id,
                                  Код = sc41.Code,
                                  Наименование = sc41.Descr.Trim(),
                                  Коэффициент = sc75.Sp78 == 0 ? 1 : sc75.Sp78
                              },
                              Производитель = sc8840 != null ? new Производитель
                              {
                                  Id = sc8840.Id,
                                  Наименование = sc8840.Descr.Trim()
                              } : null,
                              Цена = vzTovar != null ? new Цены
                              {
                                  Закупочная = vzTovar.Zakup ?? 0,
                                  Оптовая = vzTovar.Opt ?? 0,
                                  Розничная = vzTovar.Rozn ?? 0,
                                  ОптСП = vzTovar.OptSp ?? 0,
                                  РозСП = vzTovar.RoznSp ?? 0
                              } : null
                          })
                          .ToListAsync();
        }
        public async Task<List<Номенклатура>> GetНоменклатураByListCodeAsync(List<string> Codes)
        {
            return await (from sc84 in _context.Sc84s
                          join sc75 in _context.Sc75s on sc84.Sp94 equals sc75.Id
                          join sc41 in _context.Sc41s on sc75.Sp79 equals sc41.Id
                          join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                          from sc8840 in _sc8840.DefaultIfEmpty()
                          join vzTovar in _context.VzTovars on sc84.Id equals vzTovar.Id into _vzTovar
                          from vzTovar in _vzTovar.DefaultIfEmpty()
                          where sc84.Isfolder == 2 && sc84.Ismark == false && Codes.Contains(sc84.Code)
                          select new Номенклатура
                          {
                              Id = sc84.Id,
                              ParentId = sc84.Parentid,
                              IsFolder = sc84.Isfolder == 1,
                              Code = sc84.Code.Trim(),
                              Наименование = sc84.Descr.Trim(),
                              Артикул = sc84.Sp85.Trim(),
                              ЭтоУслуга = sc84.Sp2417 != ВидыНоменклатуры.Товар,
                              PickupOnly = sc84.Sp14121 == 1,
                              Единица = new Единица
                              {
                                  Id = sc75.Id,
                                  Код = sc41.Code,
                                  Наименование = sc41.Descr.Trim(),
                                  Коэффициент = sc75.Sp78 == 0 ? 1 : sc75.Sp78
                              },
                              Производитель = sc8840 != null ? new Производитель
                              {
                                  Id = sc8840.Id,
                                  Наименование = sc8840.Descr.Trim()
                              } : null,
                              Цена = vzTovar != null ? new Цены
                              {
                                  Закупочная = vzTovar.Zakup ?? 0,
                                  Оптовая = vzTovar.Opt ?? 0,
                                  Розничная = vzTovar.Rozn ?? 0,
                                  ОптСП = vzTovar.OptSp ?? 0,
                                  РозСП = vzTovar.RoznSp ?? 0
                              } : null
                          })
                          .ToListAsync();
        }
        public async Task<string> GetIdByCode(string code)
        {
            return await _context.Sc84s.Where(x => x.Code == code).Select(x => x.Id).FirstOrDefaultAsync();
        }
        public async Task<Номенклатура> GetНоменклатураByIdAsync(string Id)
        {
            return await (from sc84 in _context.Sc84s
                          join sc75 in _context.Sc75s on sc84.Sp94 equals sc75.Id
                          join sc41 in _context.Sc41s on sc75.Sp79 equals sc41.Id
                          join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                          from sc8840 in _sc8840.DefaultIfEmpty()
                          join vzTovar in _context.VzTovars on sc84.Id equals vzTovar.Id into _vzTovar
                          from vzTovar in _vzTovar.DefaultIfEmpty()
                          where sc84.Isfolder == 2 && sc84.Ismark == false && sc84.Id == Id
                          select new Номенклатура
                          {
                              Id = sc84.Id,
                              ParentId = sc84.Parentid,
                              IsFolder = sc84.Isfolder == 1,
                              Code = sc84.Code.Trim(),
                              Наименование = sc84.Descr.Trim(),
                              Артикул = sc84.Sp85.Trim(),
                              ЭтоУслуга = sc84.Sp2417 != ВидыНоменклатуры.Товар,
                              PickupOnly = sc84.Sp14121 == 1,
                              Единица = new Единица
                              {
                                  Id = sc75.Id,
                                  Код = sc41.Code,
                                  Наименование = sc41.Descr.Trim(),
                                  Коэффициент = sc75.Sp78 == 0 ? 1 : sc75.Sp78
                              },
                              Производитель = sc8840 != null ? new Производитель
                              {
                                  Id = sc8840.Id,
                                  Наименование = sc8840.Descr.Trim()
                              } : null,
                              Цена = vzTovar != null ? new Цены
                              {
                                  Закупочная = vzTovar.Zakup ?? 0,
                                  Оптовая = vzTovar.Opt ?? 0,
                                  Розничная = vzTovar.Rozn ?? 0,
                                  ОптСП = vzTovar.OptSp ?? 0,
                                  РозСП = vzTovar.RoznSp ?? 0
                              } : null

                          })
                          .FirstOrDefaultAsync();
        }
        public async Task<Единица> GetЕдиницаByIdAsync(string Id)
        {
            return await (from единицы in _context.Sc75s
                          join океи in _context.Sc41s on единицы.Sp79 equals океи.Id
                          where единицы.Id == Id 
                          select new Единица
                          {
                              Id = единицы.Id,
                              Код = океи.Code,
                              Наименование = океи.Descr.Trim(),
                              Коэффициент = единицы.Sp78
                          })
                         .FirstOrDefaultAsync();
        }
        public СтавкаНДС GetСтавкаНДС(string Id)
        {
            var СтавкаНаименование = "";
            if (!Common.СтавкиНДС.TryGetValue(Id, out СтавкаНаименование))
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
            return new СтавкаНДС { Id = Id, Наименование = СтавкаНаименование, Процент = Процент };
        }
        public async Task<СтавкаНДС> GetСтавкаНДСAsync(string номенклатураId)
        {
            string ставкаId = await _context.Sc84s.Where(x => x.Id == номенклатураId).Select(x => x.Sp103).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(ставкаId))
                return GetСтавкаНДС(ставкаId);
            return null;
        }
        public async Task<bool> IsОсновноеСвойствоКомиссияAsync(string номенклатураId)
        {
            var ВидСвойстваКомиссия = await _context.Sc546s.FirstOrDefaultAsync(x => x.Descr.Trim() == "Категория");
            var ЗначениеКомиссия = (await _context._1sconsts.FirstOrDefaultAsync(x => x.Id == 11418)).Value;
            return await (from спрСвваНоменклатуры in _context.Sc562s
                          join спрЗначениеСвойства in _context.Sc556s on спрСвваНоменклатуры.Sp564 equals спрЗначениеСвойства.Id into _спрЗначениеСвойства
                          from спрЗначениеСвойства in _спрЗначениеСвойства.DefaultIfEmpty()
                          where спрСвваНоменклатуры.Sp563 == ВидСвойстваКомиссия.Id
                             && спрСвваНоменклатуры.Parentext == номенклатураId
                             && спрЗначениеСвойства.Id == ЗначениеКомиссия
                          select спрЗначениеСвойства.Id)
                          .AnyAsync();
        }
        public async Task<List<Номенклатура>> ПолучитьСвободныеОстатки(List<string> ФирмаIds, List<string> СкладIds, List<string> НоменклатураIds, bool IsCode = false)
        {
            List<Номенклатура> НоменклатураList;
            if (IsCode)
            {
                НоменклатураList = await GetНоменклатураByListCodeAsync(НоменклатураIds);
                НоменклатураIds = НоменклатураList.Select(x => x.Id).ToList();
            }
            else
                НоменклатураList = await GetНоменклатураByListIdAsync(НоменклатураIds);
            var ОстаткиТМЦ = await _регистрОстаткиТМЦ.ПолучитьОстаткиПоСпискуСкладовAsync(
                DateTime.Now,
                null,
                false,
                ФирмаIds,
                НоменклатураIds,
                СкладIds
                );
            var РезервыТМЦ = await _регистрРезервыТМЦ.ПолучитьОстаткиAsync(
                DateTime.Now,
                null,
                false,
                ФирмаIds,
                null,
                НоменклатураIds,
                СкладIds
                );
            var АвСписок = await _регистрСтопЛистЗЧ.ПолучитьОстаткиAsync(
                DateTime.Now,
                null,
                false,
                НоменклатураIds,
                СкладIds
                );
            var usedФирма = ОстаткиТМЦ.Select(x => x.ФирмаId).Distinct().ToList();
            usedФирма.AddRange(РезервыТМЦ.Where(x => !usedФирма.Any(y => y == x.ФирмаId)).Select(x => x.ФирмаId));

            var usedСклад = ОстаткиТМЦ.Select(x => x.СкладId).Distinct().ToList();
            usedСклад.AddRange(РезервыТМЦ.Where(x => !usedСклад.Any(y => y == x.СкладId)).Select(x => x.СкладId));
            usedСклад.AddRange(АвСписок.Where(x => !usedСклад.Any(y => y == x.СкладId)).Select(x => x.СкладId));

            НоменклатураList.ForEach(x =>
            {
                usedСклад.ForEach(y =>
                    usedФирма.ForEach(f =>
                        x.Остатки.Add(new Остатки()
                        {
                            ФирмаId = f,
                            СкладId = y,
                            ОстатокТМЦ = ОстаткиТМЦ.Where(z => z.ФирмаId == f && z.СкладId == y && z.НоменклатураId == x.Id).Sum(z => z.Количество),
                            РезервТМЦ = РезервыТМЦ.Where(z => z.ФирмаId == f && z.СкладId == y && z.НоменклатураId == x.Id).Sum(z => z.Количество),
                            АварийныйСписок = АвСписок.Where(z => z.СкладId == y && z.НоменклатураId == x.Id).Sum(z => z.Количество)
                        })
                    )
                );
            });
            return НоменклатураList;
        }
        public IQueryable<Номенклатура> ПолучитьНоменклатуруПоАгентуПроизводителя(string контрагентId, IEnumerable<string> списокНоменклатурыId)
        {
            return from sc84 in _context.Sc84s
                   join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id
                   where !sc84.Ismark && sc8840.Sp13638 == контрагентId && ((списокНоменклатурыId == null || списокНоменклатурыId.Count() == 0) ? true : списокНоменклатурыId.Contains(sc84.Id))
                   select new Номенклатура
                   {
                       Id = sc84.Id,
                       ParentId = sc84.Parentid,
                       IsFolder = sc84.Isfolder == 1,
                       Code = sc84.Code.Trim(),
                       Наименование = sc84.Descr.Trim(),
                       Артикул = sc84.Sp85.Trim(),
                       ЭтоУслуга = sc84.Sp2417 != ВидыНоменклатуры.Товар,
                       PickupOnly = sc84.Sp14121 == 1,
                       Производитель = sc8840 != null ? new Производитель
                       {
                           Id = sc8840.Id,
                           Наименование = sc8840.Descr.Trim()
                       } : null
                   };
        }
        public async Task<List<Номенклатура>> АналогиНоменклатурыAsync(string номенклатураId)
        {
            return await (from аналоги in _context.Sc552s
                          join sc84 in _context.Sc84s on аналоги.Sp11395 equals sc84.Id
                          join sc75 in _context.Sc75s on sc84.Sp94 equals sc75.Id
                          join sc41 in _context.Sc41s on sc75.Sp79 equals sc41.Id
                          where аналоги.Parentext == номенклатураId && !аналоги.Ismark
                          select new Номенклатура
                          {
                              Id = sc84.Id,
                              ParentId = sc84.Parentid,
                              IsFolder = sc84.Isfolder == 1,
                              Code = sc84.Code.Trim(),
                              Наименование = sc84.Descr.Trim(),
                              Артикул = sc84.Sp85.Trim(),
                              ЭтоУслуга = sc84.Sp2417 != ВидыНоменклатуры.Товар,
                              PickupOnly = sc84.Sp14121 == 1,
                              Единица = new Единица
                              {
                                  Id = sc75.Id,
                                  Наименование = sc41.Descr.Trim(),
                                  Коэффициент = sc75.Sp78 == 0 ? 1 : sc75.Sp78
                              }
                          }).ToListAsync();
        }
        public async Task<decimal> ПолучитьКоличествоМест(string номенклатураId)
        {
            return await (from nom in _context.Sc84s
                          join ed in _context.Sc75s on nom.Sp94 equals ed.Id
                          where nom.Id == номенклатураId
                          select ed.Sp14063).FirstOrDefaultAsync();
        }
        public async Task<Dictionary<string, decimal>> ПолучитьКвант(List<string> nomenkCodes, CancellationToken cancellationToken)
        {
            return await _context.Sc84s
                .Where(x => nomenkCodes.Contains(x.Code))
                .Select(x => new
                {
                    Id = x.Id,
                    Quantum = x.Sp14188
                })
                .ToDictionaryAsync(k => k.Id, v => v.Quantum, cancellationToken);
        }
        public async Task ОбновитьЗначенияПараметров(string code, string краткоеОписание, string подробноеОписание, string комментарий,
            decimal весБруттоКг, 
            decimal длинаМ, decimal ширинаМ, decimal высотаМ,
            IHttpService httpService, List<string> pictureUrls,
            CancellationToken cancellationToken)
        {
            var entity = await _context.Sc84s.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
            if (entity != null)
            {
                bool needUpdate = false;
                bool needUpdateЕдиница = false;
                bool needUpdateImage = false;
                Sc75 entЕдиница = null;
                //VzTovarImage entImage = null;
                if (string.IsNullOrWhiteSpace(entity.Sp12309) && !string.IsNullOrWhiteSpace(краткоеОписание))
                {
                    needUpdate = true;
                    entity.Sp12309 = краткоеОписание.StringLimit(180);
                }
                if (string.IsNullOrWhiteSpace(entity.Sp12310) && !string.IsNullOrWhiteSpace(подробноеОписание))
                {
                    needUpdate = true;
                    entity.Sp12310 = подробноеОписание;
                }
                if (string.IsNullOrWhiteSpace(entity.Sp95) && !string.IsNullOrWhiteSpace(комментарий))
                {
                    needUpdate = true;
                    entity.Sp95 = комментарий;
                }
                if (весБруттоКг + длинаМ + ширинаМ + высотаМ > 0)
                {
                    entЕдиница = await _context.Sc75s.FirstOrDefaultAsync(x => x.Id == entity.Sp94, cancellationToken);
                    if (entЕдиница != null)
                    {
                        if ((entЕдиница.Sp14056 == 0) && (весБруттоКг > 0))
                        {
                            needUpdateЕдиница = true;
                            entЕдиница.Sp14056 = весБруттоКг;
                        }
                        if ((entЕдиница.Sp14035 == 0) && (высотаМ > 0))
                        {
                            needUpdateЕдиница = true;
                            entЕдиница.Sp14035 = высотаМ;
                        }
                        if ((entЕдиница.Sp14036 == 0) && (ширинаМ > 0))
                        {
                            needUpdateЕдиница = true;
                            entЕдиница.Sp14036 = ширинаМ;
                        }
                        if ((entЕдиница.Sp14037 == 0) && (длинаМ > 0))
                        {
                            needUpdateЕдиница = true;
                            entЕдиница.Sp14037 = длинаМ;
                        }
                    }
                }
                if (pictureUrls != null)
                {
                    //temp not save images
                    pictureUrls.Clear();

                    foreach (string url in pictureUrls)
                    {
                        VzTovarImage entImage = await _context.VzTovarImages.FirstOrDefaultAsync(x => x.Id == entity.Id && x.Url.Trim() == url, cancellationToken);
                        if (entImage == null)
                        {
                            var bytes = await httpService.DownloadFileAsync(url, code, cancellationToken);
                            if (bytes != null)
                            {
                                Uri link = new Uri(url);
                                string fileName = "";
                                if (link.Segments != null)
                                    fileName = link.Segments[link.Segments.Length - 1];
                                if (fileName == "orig")
                                    fileName = link.Segments[link.Segments.Length - 2];
                                if (!string.IsNullOrEmpty(fileName) && !fileName.Contains('.'))
                                    fileName += ".jpg";
                                var zipped = await bytes.CreateZip(fileName);
                                entImage = new VzTovarImage
                                {
                                    Id = entity.Id,
                                    IsMain = url == pictureUrls[0],
                                    Filename = fileName,
                                    Url = url,
                                    Extension = "zip",
                                    Photo = zipped
                                };
                                await _context.VzTovarImages.AddAsync(entImage, cancellationToken);
                                needUpdateImage = true;
                            }
                        }
                    }
                }
                if (needUpdate || needUpdateЕдиница || needUpdateImage)
                {
                    if (needUpdateЕдиница)
                    {
                        _context.Update(entЕдиница);
                        _context.РегистрацияИзмененийРаспределеннойИБ(75, entЕдиница.Id);
                    }
                    if (needUpdate)
                    {
                        _context.Update(entity);
                        _context.РегистрацияИзмененийРаспределеннойИБ(84, entity.Id);
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
