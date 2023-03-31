using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinClasses.Справочники
{
    public interface IФирма : IDisposable
    {
        Фирма GetEntityById(string Id);
        Task<Фирма> GetEntityByIdAsync(string Id);
        Task<List<string>> ПолучитьСписокРазрешенныхФирмAsync(string firmaId = null);
        Task<List<string>> ПолучитьСписокФирмБезНДС(string firmaId);
        Task<string> ПолучитьФирмуДляОптаAsync();
        Task<string> ПолучитьФирмуДляОпта2Async();
        Task<string> ПолучитьФирмуДляОпта3Async();
        Task<decimal> ПолучитьУчитыватьНДСAsync(string Id);
        Task<Фирма> ПолучитьПоИННAsync(string инн, bool строгоеСоответствие = false);
        string ПолучитьПостфикс(string фирмаId);
        Task<БанковскийСчет> ПолучитьБанковскийСчетById(string Id);
    }
    public class Фирма
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public ЮрЛицо ЮрЛицо { get; set; }
        public БанковскийСчет Счет { get; set; }
        public bool НетСчетФактуры { get; set; }
        public string СистемаНалогооблажения { get; set; }
        public string МестоОнлайнРасчетов { get; set; }
        public string AtolLogin { get; set; }
        public string AtolPassword { get; set; }
        public string AlolGroupCode { get; set; }
    }
    public class ЮрЛицо
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public string ИНН { get; set; }
        public string ТолькоИНН => ИНН.IndexOf('/') > 0 ? ИНН.Substring(0, ИНН.IndexOf('/')) : ИНН;
        public string ТолькоКПП => ИНН.IndexOf('/') > 0 ? ИНН.Substring(ИНН.IndexOf('/') + 1) : "";
        public string Префикс { get; set; }
        public decimal УчитыватьНДС { get; set; }
        public string Адрес { get; set; }
    }
    public class БанковскийСчет
    {
        public string Id { get; set; }
        public string РасчетныйСчет { get; set; }
        public Банк Банк { get; set; }
    }
    public class Банк
    {
        public string Id { get; set; }
        public string Наименование { get; set; }
        public string КоррСчет { get; set; }
        public string БИК { get; set; }
        public string Город { get; set; }
    }
    public class ФирмаEntity : IФирма
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
        public ФирмаEntity(StinDbContext context)
        {
            _context = context;
        }
        Фирма Map(Sc4014 фирмы, Sc131 своиЮрЛица, Sc1710 банковскиеСчета, Sc163 банки)
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
        public Фирма GetEntityById(string Id)
        {
            var data = (from фирмы in _context.Sc4014s
                        join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                        join банковскиеСчета in _context.Sc1710s on фирмы.Sp4133 equals банковскиеСчета.Id into _банковскиеСчета
                        from банковскиеСчета in _банковскиеСчета.DefaultIfEmpty()
                        join банки in _context.Sc163s on банковскиеСчета.Sp1712 equals банки.Id into _банки
                        from банки in _банки.DefaultIfEmpty()
                        where фирмы.Id == Id && фирмы.Ismark == false
                        select new { фирмы, своиЮрЛица, банковскиеСчета, банки }).SingleOrDefault();
            return Map(data?.фирмы, data?.своиЮрЛица, data?.банковскиеСчета, data?.банки);
        }
        public async Task<Фирма> GetEntityByIdAsync(string Id)
        {
            var data = await (from фирмы in _context.Sc4014s
                              join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                              join банковскиеСчета in _context.Sc1710s on фирмы.Sp4133 equals банковскиеСчета.Id into _банковскиеСчета
                              from банковскиеСчета in _банковскиеСчета.DefaultIfEmpty()
                              join банки in _context.Sc163s on банковскиеСчета.Sp1712 equals банки.Id into _банки
                              from банки in _банки.DefaultIfEmpty()
                              where фирмы.Id == Id && фирмы.Ismark == false
                              select new { фирмы, своиЮрЛица, банковскиеСчета, банки }).SingleOrDefaultAsync();
            return Map(data?.фирмы, data?.своиЮрЛица, data?.банковскиеСчета, data?.банки);
        }
        public async Task<БанковскийСчет> ПолучитьБанковскийСчетById(string Id)
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
                          }).FirstOrDefaultAsync();
        }
        public async Task<string> ПолучитьФирмуДляОптаAsync()
        {
            return (await _context._1sconsts.OrderBy(x => x.RowId).FirstOrDefaultAsync(x => x.Id == 8959)).Value;
        }
        public async Task<string> ПолучитьФирмуДляОпта2Async()
        {
            return (await _context._1sconsts.OrderBy(x => x.RowId).FirstOrDefaultAsync(x => x.Id == 9834)).Value;
        }
        public async Task<string> ПолучитьФирмуДляОпта3Async()
        {
            return (await _context._1sconsts.OrderBy(x => x.RowId).FirstOrDefaultAsync(x => x.Id == 9852)).Value;
        }
        public async Task<decimal> ПолучитьУчитыватьНДСAsync(string Id)
        {
            return await (from фирмы in _context.Sc4014s
                          join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                          where фирмы.Id == Id
                          select своиЮрЛица.Sp4828).FirstOrDefaultAsync();
        }
        private async Task<bool> РазрешенаПерепродажаAsync(string fromId, string toId)
        {
            return await ПолучитьУчитыватьНДСAsync(fromId) <= await ПолучитьУчитыватьНДСAsync(toId);
        }
        public async Task<List<string>> ПолучитьСписокРазрешенныхФирмAsync(string firmaId = null)
        {
            List<string> результат = new List<string>();
            if (!string.IsNullOrEmpty(firmaId))
                результат.Add(firmaId);
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
        public async Task<List<string>> ПолучитьСписокФирмБезНДС(string firmaId)
        {
            List<string> результат = new List<string>();
            string ФирмаДляОпта = await ПолучитьФирмуДляОптаAsync();
            if (!string.IsNullOrEmpty(ФирмаДляОпта) && !результат.Contains(ФирмаДляОпта) && (!await РазрешенаПерепродажаAsync(firmaId, ФирмаДляОпта)))
                результат.Add(ФирмаДляОпта);
            string ФирмаДляОпта2 = await ПолучитьФирмуДляОпта2Async();
            if (!string.IsNullOrEmpty(ФирмаДляОпта2) && !результат.Contains(ФирмаДляОпта2) && (!await РазрешенаПерепродажаAsync(firmaId, ФирмаДляОпта2)))
                результат.Add(ФирмаДляОпта2);
            string ФирмаДляОпта3 = await ПолучитьФирмуДляОпта3Async();
            if (!string.IsNullOrEmpty(ФирмаДляОпта3) && !результат.Contains(ФирмаДляОпта3) && (!await РазрешенаПерепродажаAsync(firmaId, ФирмаДляОпта3)))
                результат.Add(ФирмаДляОпта3);
            return результат;
        }
        public async Task<Фирма> ПолучитьПоИННAsync(string инн, bool строгоеСоответствие = false)
        {
            return await (from фирмы in _context.Sc4014s
                          join своиЮрЛица in _context.Sc131s on фирмы.Sp4011 equals своиЮрЛица.Id
                          where фирмы.Ismark == false && (строгоеСоответствие ? своиЮрЛица.Sp135.Trim() == инн : EF.Functions.Like(своиЮрЛица.Sp135, $"{инн}%"))
                          select new Фирма
                          {
                              Id = фирмы.Id,
                              Наименование = фирмы.Descr.Trim(),
                              ЮрЛицо = new ЮрЛицо
                              {
                                  Id = фирмы.Sp4011,
                                  Наименование = своиЮрЛица != null ? своиЮрЛица.Descr.Trim() : "<не указан>",
                                  ИНН = своиЮрЛица != null ? своиЮрЛица.Sp135.Trim() : "<не указан>",
                                  Префикс = своиЮрЛица != null ? своиЮрЛица.Sp145.Trim() : "",
                                  УчитыватьНДС = своиЮрЛица != null ? своиЮрЛица.Sp4828 : 1,
                                  Адрес = своиЮрЛица != null ? своиЮрЛица.Sp149.Trim() : "<не указан>",
                              },
                              Счет = new БанковскийСчет { Id = фирмы.Sp4133 },
                              НетСчетФактуры = фирмы.Sp12015 == 1,
                              СистемаНалогооблажения = фирмы.Sp13106.Trim(),
                              МестоОнлайнРасчетов = фирмы.Sp14144.Trim(),
                              AtolLogin = фирмы.Sp14141.Trim(),
                              AtolPassword = фирмы.Sp14142.Trim(),
                              AlolGroupCode = фирмы.Sp14143.Trim()
                          }).FirstOrDefaultAsync();
        }
        public string ПолучитьПостфикс(string фирмаId)
        {
            return (from фирмы in _context.Sc4014s
                    join _const in _context._1sconsts on фирмы.Id equals _const.Objid into __const
                    from _const in __const.DefaultIfEmpty()
                    where фирмы.Id == фирмаId && _const != null && _const.Id == 12006
                    orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                    select _const.Value.Trim()).FirstOrDefault();
        }
    }
}
