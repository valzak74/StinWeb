using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.Repository.Интерфейсы;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Справочники.Мастерская;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Справочники
{
    public class МастерскаяRepository : IМастерская
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
        public МастерскаяRepository(StinDbContext context)
        {
            this._context = context;
        }
        public async Task<Работа> РаботаByIdAsync(string Id)
        {
            var entity = await _context.Sc9875s.FirstOrDefaultAsync(x => x.Id == Id && x.Ismark == false);
            if (entity != null)
                return new Работа
                {
                    Id = entity.Id,
                    Parent = entity.Parentid,
                    Наименование = entity.Descr.Trim(),
                    Артикул = entity.Sp11503.Trim(),
                    АртикулОригинал = entity.Sp12644.Trim()
                };
            return null;
        }
        public Работа РаботаById(string Id)
        {
            var entity = _context.Sc9875s.FirstOrDefault(x => x.Id == Id && x.Ismark == false);
            if (entity != null)
                return new Работа
                {
                    Id = entity.Id,
                    Parent = entity.Parentid,
                    Наименование = entity.Descr.Trim(),
                    Артикул = entity.Sp11503.Trim(),
                    АртикулОригинал = entity.Sp12644.Trim()
                };
            return null;
        }
        public async Task<Неисправность> НеисправностьByIdAsync(string Id)
        {
            return (await _context.Sc9866s.FirstOrDefaultAsync(x => x.Id == Id && x.Ismark == false)).Map();
        }
        public async Task<ПриложенныйДокумент> ПриложенныйДокументByIdAsync(string Id)
        {
            var entity = await _context.Sc13750s.FirstOrDefaultAsync(x => x.Id == Id && x.Ismark == false);
            if (entity != null)
                return new ПриложенныйДокумент
                {
                    Id = entity.Id,
                    Наименование = entity.Descr.Trim(),
                    ФлагГарантии = entity.Sp13748
                };
            return null;
        }
        public async Task<Мастер> МастерByIdAsync(string Id)
        {
            var entity = await _context.Sc9864s.FirstOrDefaultAsync(x => x.Id == Id && x.Ismark == false);
            if (entity != null)
                return new Мастер
                {
                    Id = entity.Id,
                    Наименование = entity.Descr.Trim()
                };
            return null;
        }
        public IQueryable<Мастер> Мастера()
        {
            return _context.Sc9864s
                .Where(x => x.Ismark == false && x.Isfolder == 2 && x.Descr.Trim() != "")
                .OrderBy(x => x.Descr)
                .Select(x => new Мастер 
                {
                    Id = x.Id,
                    Наименование = x.Descr.Trim()
                });
        }
        public async Task<List<BinaryData>> ФотоПриемВРемонтAsync(string КвитанцияId)
        {
            return await _context.VzPhotos
                .Where(x => x.Id == КвитанцияId)
                .Select(y => new BinaryData
                {
                    Id = y.Id,
                    FileExtension = y.Extension,
                    Body = y.Photo
                })
                .ToListAsync();
        }
        public string DefaultPrefix(string userId, string юрлицоПрефикс)
        {
            return _context.ПрефиксИБ(userId) + юрлицоПрефикс;
        }
        public bool РеестрСообщенийByIdDoc(int ВидДок, string IdDoc)
        {
            return _context.Sc13662s.Any(x => x.Sp13658 == Common.Encode36(ВидДок).PadLeft(4) + IdDoc);
        }
        public async Task ЗаписьРеестрСообщенийAsync(int ВидДок, string IdDoc, Телефон телефон, Email почта)
        {
            Sc13662 реестрСообщений = new Sc13662
            {
                Id = Common.GenerateId(_context, 13662),
                Ismark = false,
                Verstamp = 0,
                Sp13658 = Common.Encode36(ВидДок).PadLeft(4) + IdDoc,
                Sp13659 = (!string.IsNullOrEmpty(телефон.Id) && !string.IsNullOrEmpty(телефон.Номер)) ? 1 : 0,
                Sp13660 = (!string.IsNullOrEmpty(почта.Id) && !string.IsNullOrEmpty(почта.Адрес)) ? 1 : 0
            };
            await _context.Sc13662s.AddAsync(реестрСообщений);
            await _context.SaveChangesAsync();
        }
    }
}
