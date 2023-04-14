using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    public class FirmaFunctions : IFirmaFunctions
    {
        IMemoryCache _cache;
        StinDbContext _context;
        readonly string _type36;

        public FirmaFunctions(IMemoryCache cache, StinDbContext context)
        {
            _cache = cache;
            _context = context;
            _type36 = ((long)RefBookType.Firma).Encode36();
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
