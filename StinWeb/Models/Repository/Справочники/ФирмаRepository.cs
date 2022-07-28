using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.Repository.Интерфейсы;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Справочники
{
    public class ФирмаRepository : IФирма, IDisposable
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
        public ФирмаRepository(StinDbContext context)
        {
            this._context = context;
        }
        public Фирма GetEntityById(string Id)
        {
            return (from фирмы in _context.Sc4014s
                    join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                    where фирмы.Id == Id && фирмы.Ismark == false
                    select фирмы.Map(своиЮрЛица))
             .FirstOrDefault();
        }
        public async Task<Фирма> GetEntityByIdAsync(string Id)
        {
            return await (from фирмы in _context.Sc4014s
                          join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                          join банковскиеСчета in _context.Sc1710s on фирмы.Sp4133 equals банковскиеСчета.Id into _банковскиеСчета
                          from банковскиеСчета in _банковскиеСчета.DefaultIfEmpty()
                          join банки in _context.Sc163s on банковскиеСчета.Sp1712 equals банки.Id into _банки
                          from банки in _банки.DefaultIfEmpty()
                          where фирмы.Id == Id && фирмы.Ismark == false
                          select new Фирма
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
                                  Id = банковскиеСчета != null ? банковскиеСчета.Id : "<не указан>",
                                  РасчетныйСчет = банковскиеСчета != null ? банковскиеСчета.Sp4219.Trim() : "<не указан>",
                                  Банк = банки == null ? null : new Банк
                                  {
                                      Id = банки.Id,
                                      Наименование = банки.Descr.Trim(),
                                      КоррСчет = банки.Sp165.Trim() ?? string.Empty,
                                      БИК = банки.Code.Trim() ?? string.Empty,
                                      Город = банки.Sp164.Trim() ?? string.Empty
                                  }
                              },
                          }
                ).FirstOrDefaultAsync();
        }
        public async Task<Фирма> ПолучитьПоИННAsync(string инн, bool строгоеСоответствие = false)
        {
            return await (from фирмы in _context.Sc4014s
                          join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                          where фирмы.Ismark == false && (строгоеСоответствие ? своиЮрЛица.Sp135.Trim() == инн : EF.Functions.Like(своиЮрЛица.Sp135, $"{инн}%"))
                          select фирмы.Map(своиЮрЛица)).FirstOrDefaultAsync();
        }
        public List<Фирма> СписокСвоихФирм()
        {
            var result = new List<Фирма>();
            result.Add(GetEntityById(Common.FirmaIP));
            result.Add(GetEntityById(Common.FirmaStPlus));
            result.Add(GetEntityById(Common.FirmaSS));
            return result;
        }
        public async Task<string> ПолучитьФирмуДляОптаAsync()
        {
            return (await _context._1sconsts.FirstOrDefaultAsync(x => x.Id == 8959)).Value;
        }
        public async Task<string> ПолучитьФирмуДляОпта2Async()
        {
            return (await _context._1sconsts.FirstOrDefaultAsync(x => x.Id == 9834)).Value;
        }
        public async Task<string> ПолучитьФирмуДляОпта3Async()
        {
            return (await _context._1sconsts.FirstOrDefaultAsync(x => x.Id == 9852)).Value;
        }
        public async Task<string> ОсновнойБанковскийСчетAsync(string фирмаId)
        {
            return await _context.Sc4014s.Where(x => x.Id == фирмаId).Select(x => x.Sp4133).FirstOrDefaultAsync();
        }
        public async Task<decimal> ПолучитьУчитыватьНДСAsync(string Id)
        {
            return await (from фирмы in _context.Sc4014s
                          join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                          where фирмы.Id == Id
                          select своиЮрЛица.Sp4828).FirstOrDefaultAsync();
        }
        public async Task<bool> РазрешенаПерепродажаAsync(string fromId, string toId)
        {
            return await ПолучитьУчитыватьНДСAsync(fromId) == await ПолучитьУчитыватьНДСAsync(toId);
        }
        public async Task<List<string>> ПолучитьСписокРазрешенныхФирмAsync(string firmaId)
        {
            List<string> результат = new List<string>() { firmaId };
            string ФирмаДляОпта = await ПолучитьФирмуДляОптаAsync();
            if (!string.IsNullOrEmpty(ФирмаДляОпта) && !результат.Contains(ФирмаДляОпта) && (await РазрешенаПерепродажаAsync(firmaId, ФирмаДляОпта)))
                результат.Add(ФирмаДляОпта);
            string ФирмаДляОпта2 = await ПолучитьФирмуДляОпта2Async();
            if (!string.IsNullOrEmpty(ФирмаДляОпта2) && !результат.Contains(ФирмаДляОпта2) && (await РазрешенаПерепродажаAsync(firmaId, ФирмаДляОпта2)))
                результат.Add(ФирмаДляОпта2);
            string ФирмаДляОпта3 = await ПолучитьФирмуДляОпта3Async();
            if (!string.IsNullOrEmpty(ФирмаДляОпта3) && !результат.Contains(ФирмаДляОпта3) && (await РазрешенаПерепродажаAsync(firmaId, ФирмаДляОпта3)))
                результат.Add(ФирмаДляОпта3);
            return результат;
        }
    }
}
