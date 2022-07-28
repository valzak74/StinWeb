using StinClasses.Models;
using System;
using System.Linq;

namespace StinClasses.Справочники
{
    public class ГрафикМаршрутов
    {
        public string Id { get; set; }
        public string Код { get; set; }
        public string Наименование { get; set; }
        public string Ответственный { get; set; }
        public string НомерНаправления { get; set; }
    }
    public interface IГрафикМаршрутов : IDisposable
    {
        string ПолучитьКодМаршрута(DateTime shipDate, string направлениеId);
    }
    public class ГрафикМаршрутовEntity : IГрафикМаршрутов
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
        public ГрафикМаршрутовEntity(StinDbContext context)
        {
            _context = context;
        }
        public string ПолучитьКодМаршрута(DateTime shipDate, string направлениеId)
        {
            foreach (var entity in _context.Sc14082s.Where(x => !x.Ismark && (x.Sp14078.Date == shipDate.Date)))
            {
                var направления = entity.Sp14080.Split(',').Select(x => x.Trim()).ToList();
                if (направления.Contains(направлениеId))
                    return entity.Code.Trim();
            }
            return "";
        }
    }
}
