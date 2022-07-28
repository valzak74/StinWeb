using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinClasses.Справочники
{
    public class Маршрут
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Наименование { get; set; }
        public DateTime ДатаОтправки { get; set; }
        public Водитель Водитель { get; set; } 
    }
    public interface IМаршрут : IDisposable
    {
        Task<Маршрут> GetМаршрутByIdAsync(string Id);
        Task<Маршрут> GetМаршрутByCodeAsync(string Code);
        Маршрут НовыйЭлемент();
        Task ОбновитьМаршрут(string Id, string ВодительId, DateTime датаОтправки);
    }
    public class МаршрутEntity : IМаршрут
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
        public МаршрутEntity(StinDbContext context)
        {
            _context = context;
        }
        public async Task<Маршрут> GetМаршрутByIdAsync(string Id)
        {
            return await (from m in _context.Sc11555s
                          join _const in _context._1sconsts on m.Id equals _const.Objid into __const
                          from _const in __const.DefaultIfEmpty()
                          join v in _context.Sc10253s on m.Sp11669 equals v.Id into _v
                          from v in _v.DefaultIfEmpty()
                          where m.Id == Id && !m.Ismark
                          orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                          select new Маршрут
                          {
                              Id = m.Id,
                              Code = m.Code,
                              Наименование = _const.Value.Trim(),
                              Водитель = v != null ? new Водитель
                              {
                                  Id = v.Id,
                                  Наименование = v.Descr.Trim(),
                                  ФИО = v.Sp10251.Trim(),
                                  ВодительскоеУдостоверение = v.Sp11813.Trim()
                              } : null,
                              ДатаОтправки = m.Sp11670
                          }).FirstOrDefaultAsync();
        }
        public async Task<Маршрут> GetМаршрутByCodeAsync(string Code)
        {
            return await (from sc11555 in _context.Sc11555s
                          join _const in _context._1sconsts on sc11555.Id equals _const.Objid into __const
                          from _const in __const.DefaultIfEmpty()
                          join v in _context.Sc10253s on sc11555.Sp11669 equals v.Id into _v
                          from v in _v.DefaultIfEmpty()
                          where sc11555.Code == Code && !sc11555.Ismark && _const.Id == 11553
                          orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                          select new Маршрут
                          {
                              Id = sc11555.Id,
                              Code = sc11555.Code,
                              Наименование = _const.Value.Trim(),
                              Водитель = v != null ? new Водитель
                              {
                                  Id = v.Id,
                                  Наименование = v.Descr.Trim(),
                                  ФИО = v.Sp10251.Trim(),
                                  ВодительскоеУдостоверение = v.Sp11813.Trim()
                              } : null,
                              ДатаОтправки = sc11555.Sp11670
                          })
                           .FirstOrDefaultAsync();
        }
        public Маршрут НовыйЭлемент()
        {
            var префиксИБ = _context.ПрефиксИБ();
            var code = _context.Sc11555s.Where(x => x.Code.StartsWith(префиксИБ)).Max(x => x.Code);
            if (code == null)
                code = "0";
            else
                code = code.Substring(префиксИБ.Length);
            if (Int32.TryParse(code, out int next_code))
            {
                next_code += 1;
            }
            code = префиксИБ + next_code.ToString().PadLeft(10 - префиксИБ.Length, '0');
            Sc11555 ТранспортныеМаршруты = new Sc11555
            {
                Id = _context.GenerateId(11555),
                Code = code,
                Ismark = false,
                Verstamp = 0,
                Sp11669 = Common.ПустоеЗначение,
                Sp11670 = Common.min1cDate
            };
            _context.Sc11555s.Add(ТранспортныеМаршруты);
            _context.SaveChanges();
            _context.РегистрацияИзмененийРаспределеннойИБ(11555, ТранспортныеМаршруты.Id);
            return new Маршрут { Id = ТранспортныеМаршруты.Id, Code = ТранспортныеМаршруты.Code };
        }
        public async Task ОбновитьМаршрут(string Id, string ВодительId, DateTime датаОтправки)
        {
            var entity = await _context.Sc11555s.FirstOrDefaultAsync(x => x.Id == Id);
            if (entity != null)
            {
                entity.Sp11669 = ВодительId;
                entity.Sp11670 = датаОтправки;
                _context.Update(entity);
                await _context.SaveChangesAsync();
                _context.РегистрацияИзмененийРаспределеннойИБ(11555, entity.Id);
            }
        }

    }
}
