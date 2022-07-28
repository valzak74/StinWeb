using StinWeb.Models.Repository.Интерфейсы;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Отчеты;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.DataManager;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Справочники
{
    public class СкладRepository : IСклад, IDisposable
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
        public СкладRepository(StinDbContext context)
        {
            this._context = context;
        }
        private IQueryable<Sc55> ВыборкаСкладов()
        {
            return _context.Sc55s.Where(x => x.Ismark == false);
        }
        public Склад GetEntityById(string Id)
        {
            return _context.Sc55s.FirstOrDefault(x => x.Id == Id && x.Ismark == false).Map();
        }
        public async Task<Склад> GetEntityByIdAsync(string Id)
        {
            return (await _context.Sc55s.FirstOrDefaultAsync(x => x.Id == Id && x.Ismark == false)).Map();
        }
        public Склад КонстантаСкладДляРемонта()
        {
            string id = _context._1sconsts.FirstOrDefault(x => x.Id == 9991).Value;
            return GetEntityById(id);
        }
        public Склад КонстантаСкладСортировкиРемонтов()
        {
            string id = _context._1sconsts.FirstOrDefault(x => x.Id == 13709).Value;
            return GetEntityById(id);
        }
        public ПодСклад GetПодСкладById(string Id)
        {
            return _context.Sc8963s.FirstOrDefault(x => x.Id == Id && x.Ismark == false).Map();
        }
        public async Task<ПодСклад> GetПодСкладByIdAsync(string Id)
        {
            return (await _context.Sc8963s.FirstOrDefaultAsync(x => x.Id == Id && x.Ismark == false)).Map();
        }
        public IQueryable<Склад> ПолучитьСклады()
        {
            return _context.Sc55s
                .Where(x => x.Ismark == false)
                .OrderBy(x => x.Descr)
                .Select(x => x.Map());
        }
        public IQueryable<Склад> ПолучитьСкладыМастерские()
        {
            return from складыМастерские in _context.Sc9995s
                   join склады in _context.Sc55s on складыМастерские.Sp9993 equals склады.Id
                   where складыМастерские.Ismark == false && склады.Ismark == false
                   orderby склады.Descr
                   select склады.Map();
        }
        public IQueryable<Склад> ПолучитьРазрешенныеСклады(string ПользовательId)
        {
            return from sc8836 in _context.Sc8836s
                   join sc55 in _context.Sc55s on sc8836.Sp8838 equals sc55.Id
                   where sc8836.Parentext == ПользовательId && sc8836.Ismark == false && sc55.Ismark == false
                   orderby sc55.Descr
                   select sc55.Map();
        }
        public IQueryable<ПодСклад> ПолучитьПодСклады()
        {
            return _context.Sc8963s
                .Where(x => x.Ismark == false)
                .Select(x => x.Map());
        }
        public IQueryable<ПодСклад> ПолучитьПодСклады(string СкладId)
        {
            return _context.Sc8963s
                .Where(x => x.Ismark == false && x.Parentext == СкладId)
                .OrderBy(x => x.Descr)
                .Select(x => x.Map());
        }
        public IQueryable<ЖурналДоставки> СформироватьОстаткиДоставки(DateTime dateReg, string фирмаId, string складId, РежимВыбора режим, string докId13, string номенклатураId)
        {
            if (dateReg <= Common.min1cDate)
                dateReg = Common.GetRegTA(_context);
            List<string> ВидыДокументов = null;
            if (режим != РежимВыбора.Общий)
            {
                ВидыДокументов = new List<string>();
                ВидыДокументов.Add(Common.Encode36(10054).PadLeft(4)); //Выдача из ремонта
                ВидыДокументов.Add(Common.Encode36(10080).PadLeft(4)); //Перемещение изделий
                ВидыДокументов.Add(Common.Encode36(10062).PadLeft(4)); //Перемещение в мастерскую
            }

            var registry = from остаткиДоставки in _context.Rg8696s
                           where остаткиДоставки.Period == dateReg &&
                               (string.IsNullOrEmpty(фирмаId) ? true : остаткиДоставки.Sp8697 == фирмаId) &&
                               (string.IsNullOrEmpty(складId) ? true : остаткиДоставки.Sp8699 == складId) &&
                               (режим == РежимВыбора.ПоМастерской ? ВидыДокументов.Contains(остаткиДоставки.Sp8715.Substring(0, 4)) : true) &&
                               (режим == РежимВыбора.ПоТовару ? !ВидыДокументов.Contains(остаткиДоставки.Sp8715.Substring(0, 4)) : true) &&
                               (string.IsNullOrEmpty(докId13) ? true : остаткиДоставки.Sp8715 == докId13) &&
                               (string.IsNullOrEmpty(номенклатураId) ? true : остаткиДоставки.Sp8698 == номенклатураId)
                           group остаткиДоставки by остаткиДоставки.Sp8715 into gr
                           where gr.Sum(x => x.Sp8701) != 0
                           select new
                           {
                               IdDoc = gr.Key.Substring(4, 9),
                               ВидДокумента36 = gr.Key.Substring(0, 4),
                               Остаток = gr.Sum(x => x.Sp8701)
                           };
            return from rOst in registry
                   join j in _context._1sjourns on rOst.IdDoc equals j.Iddoc
                   join docПеремещение in _context.Dh1628s on rOst.IdDoc equals docПеремещение.Iddoc into _docПеремещение
                   from docПеремещение in _docПеремещение.DefaultIfEmpty()
                   join docПеремещениеВМастерскую in _context.Dh10062s on rOst.IdDoc equals docПеремещениеВМастерскую.Iddoc into _docПеремещениеВМастерскую
                   from docПеремещениеВМастерскую in _docПеремещениеВМастерскую.DefaultIfEmpty()
                   join docПеремещениеИзделий in _context.Dh10080s on rOst.IdDoc equals docПеремещениеИзделий.Iddoc into _docПеремещениеИзделий
                   from docПеремещениеИзделий in _docПеремещениеИзделий.DefaultIfEmpty()
                   join docВыдачаИзРемонта in _context.Dh10054s on rOst.IdDoc equals docВыдачаИзРемонта.Iddoc into _docВыдачаИзРемонта
                   from docВыдачаИзРемонта in _docВыдачаИзРемонта.DefaultIfEmpty()
                   select new ЖурналДоставки
                   {
                       ОбщиеРеквизиты = new DataManager.Документы.ОбщиеРеквизиты
                       {
                           IdDoc = rOst.IdDoc,
                           ВидДокумента10 = j.Iddocdef,
                           ВидДокумента36 = rOst.ВидДокумента36,
                           НомерДок = docПеремещениеИзделий != null ? docПеремещениеИзделий.Sp10063 + "-" + docПеремещениеИзделий.Sp10064.ToString() : j.Docno,
                           ДатаДок = Common.DateTimeIddoc(j.DateTimeIddoc),
                           Комментарий = docПеремещение != null ? docПеремещение.Sp660.Trim() :
                                  docПеремещениеВМастерскую != null ? docПеремещениеВМастерскую.Sp660.Trim() :
                                  docПеремещениеИзделий != null ? docПеремещениеИзделий.Sp660.Trim() :
                                  docВыдачаИзРемонта != null ? docВыдачаИзРемонта.Sp660.Trim() :
                                  "не поддерживаемый тип документа"
                       },
                       Остаток = rOst.Остаток,
                   };
        }
    }
}
