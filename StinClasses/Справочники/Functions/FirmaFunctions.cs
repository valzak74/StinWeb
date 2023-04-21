using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    internal static class FirmaExtensions
    {
        internal static Фирма Map(Sc4014 фирмы, Sc131 своиЮрЛица, Sc1710 банковскиеСчета, Sc163 банки)
        {
            if ((фирмы == null) || (своиЮрЛица == null))
                return null;
            return new Фирма
            {
                Id = фирмы.Id,
                Наименование = фирмы.Descr.Trim(),
                ЮрЛицо = new ЮрЛицо
                {
                    Id = своиЮрЛица.Id,
                    Наименование = своиЮрЛица.Descr.Trim(),
                    ИНН = своиЮрЛица.Sp135.Trim(),
                    Префикс = своиЮрЛица.Sp145.Trim(),
                    УчитыватьНДС = своиЮрЛица.Sp4828,
                    Адрес = своиЮрЛица.Sp149,
                },
                Счет = new БанковскийСчет
                {
                    Id = банковскиеСчета?.Id ?? "<не указан>",
                    РасчетныйСчет = банковскиеСчета?.Sp4219.Trim() ?? "<не указан>",
                    Банк = банки == null ? null : new Банк
                    {
                        Id = банки.Id,
                        Наименование = банки.Descr.Trim(),
                        КоррСчет = банки.Sp165.Trim() ?? string.Empty,
                        БИК = банки.Code.Trim() ?? string.Empty,
                        Город = банки.Sp164.Trim() ?? string.Empty
                    }
                },
                НетСчетФактуры = фирмы.Sp12015 == 1,
                СистемаНалогооблажения = фирмы.Sp13106.Trim(),
                МестоОнлайнРасчетов = фирмы.Sp14144.Trim(),
                AtolLogin = фирмы.Sp14141.Trim(),
                AtolPassword = фирмы.Sp14142.Trim(),
                AlolGroupCode = фирмы.Sp14143.Trim()
            };
        }
    }
    public class FirmaFunctions : IFirmaFunctions
    {
        IMemoryCache _cache;
        StinDbContext _context;
        readonly string _base36 = Common.Encode36((long)RefBookType.Firma);
        public FirmaFunctions(IMemoryCache cache, StinDbContext context)
        {
            _cache = cache;
            _context = context;
        }
        public async Task<Фирма> GetEntityByIdAsync(string Id, CancellationToken cancellationToken)
        {
            if (!_cache.TryGetValue(_base36 + Id, out Фирма firma))
            {
                var data = await (from фирмы in _context.Sc4014s
                                  join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                                  join банковскиеСчета in _context.Sc1710s on фирмы.Sp4133 equals банковскиеСчета.Id into _банковскиеСчета
                                  from банковскиеСчета in _банковскиеСчета.DefaultIfEmpty()
                                  join банки in _context.Sc163s on банковскиеСчета.Sp1712 equals банки.Id into _банки
                                  from банки in _банки.DefaultIfEmpty()
                                  where фирмы.Id == Id && фирмы.Ismark == false
                                  select new { фирмы, своиЮрЛица, банковскиеСчета, банки }).SingleOrDefaultAsync(cancellationToken);
                firma = FirmaExtensions.Map(data?.фирмы, data?.своиЮрЛица, data?.банковскиеСчета, data?.банки);
                if (firma != null)
                    _cache.Set(_base36 + Id, firma, TimeSpan.FromDays(1));
            }
            return firma;
        }
        public async Task<List<string>> GetListAcseptedAsync(string firmaId = null)
        {
            if (firmaId == null)
                firmaId = "";
            if (!_cache.TryGetValue("AcseptedFirmIds" + firmaId, out List<string> result))
            {
                result = new();
                if (!string.IsNullOrEmpty(firmaId))
                    result.Add(firmaId);
                string ФирмаДляОпта = await _context.ПолучитьЗначениеКонстанты(8959);
                if (!string.IsNullOrEmpty(ФирмаДляОпта) && !result.Contains(ФирмаДляОпта) && (await РазрешенаПерепродажаAsync(firmaId, ФирмаДляОпта)))
                    result.Add(ФирмаДляОпта);
                string ФирмаДляОпта2 = await _context.ПолучитьЗначениеКонстанты(9834);
                if (!string.IsNullOrEmpty(ФирмаДляОпта2) && !result.Contains(ФирмаДляОпта2) && (await РазрешенаПерепродажаAsync(firmaId, ФирмаДляОпта2)))
                    result.Add(ФирмаДляОпта2);
                string ФирмаДляОпта3 = await _context.ПолучитьЗначениеКонстанты(9852);
                if (!string.IsNullOrEmpty(ФирмаДляОпта3) && !result.Contains(ФирмаДляОпта3) && (await РазрешенаПерепродажаAsync(firmaId, ФирмаДляОпта3)))
                    result.Add(ФирмаДляОпта3);
                _cache.Set("AcseptedFirmIds" + firmaId, result, TimeSpan.FromDays(1));
            }
            return result;
        }
        public async Task<decimal> ПолучитьУчитыватьНДСAsync(string Id)
        {
            return await (from фирмы in _context.Sc4014s
                          join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                          where фирмы.Id == Id
                          select своиЮрЛица.Sp4828).FirstOrDefaultAsync();
        }
        async Task<bool> РазрешенаПерепродажаAsync(string fromId, string toId)
        {
            return await ПолучитьУчитыватьНДСAsync(fromId) <= await ПолучитьУчитыватьНДСAsync(toId);
        }
    }
}
