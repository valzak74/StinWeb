using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using StinClasses.Models;
using StinClasses.Регистры;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    internal static class NomenklaturaExtensions
    {
        internal static Номенклатура Map(this Sc84 nomEntity, Sc75 unitEntity, Sc41 okeyEntity, Sc8840 brendEntity = null, VzTovar vzTovarEntity = null)
        {
            if (nomEntity == null)
                return null;
            return new Номенклатура
            {
                Id = nomEntity.Id,
                Code = nomEntity.Code.Trim(),
                Наименование = nomEntity.Descr.Trim(),
                Deleted = nomEntity.Ismark,
                ParentId = nomEntity.Parentid,
                IsFolder = nomEntity.Isfolder == 1,
                ПолнНаименование = nomEntity.Sp101,
                Артикул = nomEntity.Sp85.Trim(),
                ЭтоУслуга = nomEntity.Sp2417 != ВидыНоменклатуры.Товар,
                PickupOnly = nomEntity.Sp14121 == 1,
                Единица = new Единица
                {
                    Id = unitEntity.Id,
                    Code = okeyEntity.Code,
                    Наименование = okeyEntity.Descr.Trim(),
                    Deleted = unitEntity.Ismark,
                    Barcode = unitEntity.Sp80.Trim(),
                    Коэффициент = unitEntity.Sp78 == 0 ? 1 : unitEntity.Sp78
                },
                Производитель = brendEntity != null ? new Производитель
                {
                    Id = brendEntity.Id,
                    Code = brendEntity.Code,
                    Наименование = brendEntity.Descr.Trim(),
                    Deleted = brendEntity.Ismark,
                } : null,
                Цена = vzTovarEntity != null ? new Цены
                {
                    Закупочная = vzTovarEntity.Zakup ?? 0,
                    Оптовая = vzTovarEntity.Opt ?? 0,
                    Розничная = vzTovarEntity.Rozn ?? 0,
                    ОптСП = vzTovarEntity.OptSp ?? 0,
                    РозСП = vzTovarEntity.RoznSp ?? 0
                } : null
            };
        }
    }
    public class NomenklaturaFunctions : INomenklaturaFunctions
    {
        IMemoryCache _cache;
        StinDbContext _context;
        IServiceProvider _serviceProvider;
        public NomenklaturaFunctions(StinDbContext context, IMemoryCache cache, IServiceProvider serviceProvider)
        {
            _cache = cache;
            _context = context;
            _serviceProvider = serviceProvider;
        }
        public async Task<List<Номенклатура>> GetНоменклатураByListIdAsync(List<string> Ids, bool isCode = false)
        {
            var data = await (from sc84 in _context.Sc84s
                              join sc75 in _context.Sc75s on sc84.Sp94 equals sc75.Id
                              join sc41 in _context.Sc41s on sc75.Sp79 equals sc41.Id
                              join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                              from sc8840 in _sc8840.DefaultIfEmpty()
                              join vzTovar in _context.VzTovars on sc84.Id equals vzTovar.Id into _vzTovar
                              from vzTovar in _vzTovar.DefaultIfEmpty()
                              where sc84.Isfolder == 2 && sc84.Ismark == false && Ids.Contains(isCode ? sc84.Code : sc84.Id)
                              select new { sc84, sc75, sc41, sc8840, vzTovar }).ToListAsync();
            List<Номенклатура> result = new List<Номенклатура>();
            data.ForEach(x => result.Add(x.sc84.Map(x.sc75, x.sc41, x.sc8840, x.vzTovar)));
            return result;
        }
        public async Task<IEnumerable<Номенклатура>> ПолучитьСвободныеОстатки(List<string> ФирмаIds, List<string> СкладIds, List<string> НоменклатураIds, bool IsCode = false)
        {
            List<Номенклатура> НоменклатураList = await GetНоменклатураByListIdAsync(НоменклатураIds, IsCode);
            if (IsCode)
                НоменклатураIds = НоменклатураList.Select(x => x.Id).ToList();
            using (var scope = _serviceProvider.CreateScope())
            {
                var registrОстаткиТМЦ = scope.ServiceProvider.GetRequiredService<IРегистрОстаткиТМЦ>();
                var registrРезервыТМЦ = scope.ServiceProvider.GetRequiredService<IРегистрРезервыТМЦ>();
                var registrСтопЛистЗЧ = scope.ServiceProvider.GetRequiredService<IРегистрСтопЛистЗЧ>();
                var ОстаткиТМЦ = await registrОстаткиТМЦ.ПолучитьОстаткиПоСпискуСкладовAsync(
                    DateTime.Now,
                    null,
                    false,
                    ФирмаIds,
                    НоменклатураIds,
                    СкладIds
                    );
                var РезервыТМЦ = await registrРезервыТМЦ.ПолучитьОстаткиAsync(
                    DateTime.Now,
                    null,
                    false,
                    ФирмаIds,
                    null,
                    НоменклатураIds,
                    СкладIds
                    );
                var АвСписок = await registrСтопЛистЗЧ.ПолучитьОстаткиAsync(
                    DateTime.Now,
                    null,
                    false,
                    НоменклатураIds,
                    СкладIds
                    );
                var usedФирма = ОстаткиТМЦ.Select(x => x.ФирмаId).Distinct().ToList();
                usedФирма.AddRange(РезервыТМЦ.Where(x => !usedФирма.Any(y => y == x.ФирмаId)).Select(x => x.ФирмаId).Distinct());

                var usedСклад = ОстаткиТМЦ.Select(x => x.СкладId).Distinct().ToList();
                usedСклад.AddRange(РезервыТМЦ.Where(x => !usedСклад.Any(y => y == x.СкладId)).Select(x => x.СкладId).Distinct());
                usedСклад.AddRange(АвСписок.Where(x => !usedСклад.Any(y => y == x.СкладId)).Select(x => x.СкладId).Distinct());

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
            }
            return НоменклатураList;
        }
        public async Task<IDictionary<string, decimal>> GetReserveByMarketplace(string marketplaceId, IEnumerable<string> nomIds)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var registrРезервыТМЦ = scope.ServiceProvider.GetRequiredService<IРегистрРезервыТМЦ>();
                var registrНаборНаСкладе = scope.ServiceProvider.GetRequiredService<IРегистрНаборНаСкладе>();
                var inReserve = await registrРезервыТМЦ.ПолучитьКоличествоНоменклатурыВРезервахAsync(DateTime.MinValue, null, false, nomIds, marketplaceId);
                var inNabor = await registrНаборНаСкладе.ПолучитьКоличествоНоменклатурыВНаборахAsync(DateTime.MinValue, null, false, nomIds, marketplaceId);
                if (inReserve != null && inNabor != null)
                {
                    foreach (var item in inReserve)
                    {
                        if (inNabor.ContainsKey(item.Key))
                            inNabor[item.Key] += item.Value;
                        else
                            inNabor.Add(item.Key, item.Value);
                    }
                    return inNabor;
                }
                else if (inNabor != null)
                    return inNabor;
                else if (inReserve != null)
                    return inReserve;
            }
            return new Dictionary<string, decimal>();
        }
    }
}
