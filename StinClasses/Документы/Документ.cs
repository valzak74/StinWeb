using JsonExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StinClasses.Models;
using StinClasses.Справочники;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Документы
{
    [JsonConverter(typeof(DefaultUnknownEnumConverter), (int)NotFound)]
    public enum ВидДокумента
    {
        NotFound = 1,
        Набор = 11948,
        ВозвратИзНабора = 11961,
        ОтменаНабора = 11964,
        ЗаявкаПокупателя = 2457,
        КомплекснаяПродажа = 12542,
        ПродажаКасса = 9074,
        ПродажаБанк = 9109,
        Реализация = 1611,
        СчетФактура = 2051,
        ПеремещениеТМЦ = 1628,
        ВозвратИзДоставки = 8724,
        ПКО = 2196
    }
    public class ОбщиеРеквизиты
    {
        public string IdDoc { get; set; }
        public string DateTimeIdDoc { get; set; }
        public ДокОснование ДокОснование { get; set; }
        public bool Удален { get; set; }
        public bool Проведен { get; set; }
        public int ВидДокумента10 { get; set; }
        public string ВидДокумента36 { get; set; }
        public string Наименование => Common.ВидыДокументов.Where(x => x.Key == ВидДокумента10).Select(y => y.Value).FirstOrDefault();
        private string _названиеВЖурнале;
        public string НазваниеВЖурнале
        {
            get => _названиеВЖурнале;
            set => _названиеВЖурнале = string.IsNullOrEmpty(value) ? Наименование : string.Format(value, Наименование);
        }
        public string Информация { get; set; }
        public string НомерДок { get; set; }
        public DateTime ДатаДок { get; set; }
        public Фирма Фирма { get; set; }
        public Пользователь Автор { get; set; }
        public string Комментарий { get; set; }
    }
    public class ДокОснование
    {
        public string IdDoc { get; set; }
        public int ВидДокумента10 { get; set; }
        public string Значение => Common.Encode36(ВидДокумента10).PadLeft(4) + IdDoc;
        public string Наименование => Common.ВидыДокументов.Where(x => x.Key == ВидДокумента10).Select(y => y.Value).FirstOrDefault();
        public string НомерДок { get; set; }
        public DateTime ДатаДок { get; set; }
        public bool Проведен { get; set; }
        public Фирма Фирма { get; set; }
        public Пользователь Автор { get; set; }
    }
    public class ExceptionData
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public bool Skip { get; set; }
    }
    public interface IДокумент : IDisposable
    {
        bool NeedToOpenPeriod();
        bool IsNew(string idDoc);
        Task<ОбщиеРеквизиты> ОбщиеРеквизитыAsync(string IdDoc);
        _1sjourn GetEntityJourn(byte Проведен, int КоличествоДвижений, int ЖурналИД_md, int ВидДокИД_dds, string Нумератор, string ВидДок, string НомерДок, DateTime dateTime,
               string ФирмаИД,
               string ПользовательИД,
               string СкладНазвание,
               string КонтрагентНазвание);
        Task<ВидДокумента> ПолучитьВидДокумента(string idDoc);
        Task<ДокОснование> ДокОснованиеAsync(string IdDoc);
        Task<string> LockDocNoAsync(string userId, string ИдентификаторДокDds, int ДлинаНомера = 10, string FirmaId = null, string Год = null);
        Task UnLockDocNoAsync(string ИдентификаторДокDds, string DocNo, string Год = null);
        Task<string> НайтиДокументВДеревеAsync(string idDoc, int searchВидДок);
        Task ОбновитьПодчиненныеДокументы(string ДокОснованиеId13, string DateTimeIddoc, string Iddoc);
        Task ОбновитьГрафыОтбора(int MdId, string Id13, string DateTimeIddoc, string Iddoc);
        Task ОбновитьВремяТА(string Iddoc, string DateTimeIddoc);
        Task ОбновитьПоследовательность(string DateTimeIddoc);
        Task ОбновитьСетевуюАктивность();
        Task ОбновитьАктивность(List<ОбщиеРеквизиты> реквизиты);
        Task ОбновитьTotals(int ВидДок, string idDoc);
        Task<Договор> ПолучитьДоговорНабора(string idDoc);
        Task<Склад> ПолучитьСкладНабора(string idDoc);
        Task<Order> ПолучитьOrderНабора(string idDoc);
        Task<ФормаОплатаЧерезЮКасса> GetФормаОплатаЧерезЮКассаById(string idDoc);
        Task<ФормаЗаявкаПокупателя> GetФормаЗаявкаById(string idDoc);
    }
    public class Документ : IДокумент
    {
        private protected StinDbContext _context;
        private protected IПользователь _пользователь;
        private protected IФирма _фирма;
        private protected string _defaultFirmaId = Common.FirmaSS;
        private protected IКонтрагент _контрагент;
        private protected IСклад _склад;
        private protected IМаршрут _маршрут;
        private protected IГрафикМаршрутов _графикМаршрутов;
        private protected IНоменклатура _номенклатура;
        private protected IКладовщик _кладовщик;
        private protected IOrder _order;
        private protected bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _пользователь.Dispose();
                    _фирма.Dispose();
                    _контрагент.Dispose();
                    _номенклатура.Dispose();
                    _склад.Dispose();
                    _маршрут.Dispose();
                    _графикМаршрутов.Dispose();
                    _кладовщик.Dispose();
                    _order.Dispose();
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
        public Документ(StinDbContext context)
        {
            _context = context;
            _маршрут = new МаршрутEntity(context);
            _графикМаршрутов = new ГрафикМаршрутовEntity(context);
            _пользователь = new ПользовательEntity(context);
            _фирма = new ФирмаEntity(context);
            _контрагент = new КонтрагентEntity(context);
            _номенклатура = new НоменклатураEntity(context);
            _склад = new СкладEntity(context);
            _кладовщик = new КладовщикEntity(context);
            _order = new OrderEntity(context);
        }
        public bool NeedToOpenPeriod()
        {
            return _context.GetDateTA().Month != DateTime.Now.Month;
        }
        public async Task<ОбщиеРеквизиты> ОбщиеРеквизитыAsync(string IdDoc)
        {
            return await (from j in _context._1sjourns
                          where j.Iddoc == IdDoc
                          select new ОбщиеРеквизиты
                          {
                              IdDoc = j.Iddoc,
                              ВидДокумента10 = j.Iddocdef,
                              НомерДок = j.Docno,
                              ДатаДок = j.DateTimeIddoc.ToDateTime(),
                          }).FirstOrDefaultAsync();
        }
        public async Task<ВидДокумента> ПолучитьВидДокумента(string idDoc)
        {
            return await _context._1sjourns.Where(x => x.Iddoc == idDoc).Select(x => (ВидДокумента)x.Iddocdef).FirstOrDefaultAsync();
        }
        public async Task<ДокОснование> ДокОснованиеAsync(string IdDoc)
        {
            if (IdDoc == Common.ПустоеЗначение)
                return null;
            var j = await _context._1sjourns.FirstOrDefaultAsync(x => x.Iddoc == IdDoc);
            return new ДокОснование
            {
                IdDoc = j.Iddoc,
                ВидДокумента10 = j.Iddocdef,
                НомерДок = j.Docno.Trim(),
                ДатаДок = j.DateTimeIddoc.ToDateTime(),
                Проведен = j.Closed == 1,
                Фирма = await _фирма.GetEntityByIdAsync(j.Sp4056),
                Автор = await _пользователь.GetUserByIdAsync(j.Sp74)
            };
        }
        public async Task<string> LockDocNoAsync(string userId, string ИдентификаторДокDds, int ДлинаНомера = 10, string FirmaId = null, string Год = null)
        {
            string docNo = "";
            if (string.IsNullOrEmpty(FirmaId))
                FirmaId = _defaultFirmaId;
            if (string.IsNullOrEmpty(Год))
                Год = DateTime.Now.ToString("yyyy");

            var ФирмаПрефикс = await (from sc131 in _context.Sc131s
                                      join sc4014 in _context.Sc4014s on sc131.Id equals sc4014.Sp4011
                                      where sc4014.Id == FirmaId
                                      select sc131.Sp145.Trim()).FirstOrDefaultAsync();
            string prefix = _context.ПрефиксИБ(userId) + ФирмаПрефикс;

            string dnPrefixJ = ИдентификаторДокDds.ToString().PadLeft(10) + DateTime.Now.ToString("yyyy").PadRight(8);
            try
            {
                var j_docNo = await (from _j in _context._1sjourns
                                     where _j.Dnprefix == dnPrefixJ && string.Compare(_j.Docno, prefix) >= 0 && _j.Docno.Substring(0, prefix.Length) == prefix
                                     orderby _j.Dnprefix descending, _j.Docno descending
                                     select _j.Docno).FirstOrDefaultAsync();
                if (j_docNo == null)
                    j_docNo = "0";
                else
                    j_docNo = j_docNo.Substring(prefix.Length);
                int number = Convert.ToInt32(j_docNo);
                string dnPrefix = ИдентификаторДокDds.ToString().PadLeft(10) + Год.PadRight(18);
                bool needNextNo = true;
                while (needNextNo)
                {
                    number++;
                    docNo = prefix + number.ToString().PadLeft(ДлинаНомера - prefix.Length, '0');
                    needNextNo = _context._1sdnlocks.Any(y => y.Dnprefix == dnPrefix && y.Docno == docNo);
                }
                _1sdnlock lock_table = new _1sdnlock
                {
                    Dnprefix = dnPrefix,
                    Docno = docNo
                };
                await _context._1sdnlocks.AddAsync(lock_table);
                await _context.SaveChangesAsync();
            }
            catch
            {
                docNo = "";
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
            }
            return docNo;
        }
        public async Task UnLockDocNoAsync(string ИдентификаторДокDds, string DocNo, string Год = null)
        {
            if (string.IsNullOrEmpty(Год))
                Год = DateTime.Now.ToString("yyyy");
            string dnPrefix = ИдентификаторДокDds.ToString().PadLeft(10) + Год.PadRight(18);
            var row = await _context._1sdnlocks.FirstOrDefaultAsync(x => x.Dnprefix == dnPrefix && x.Docno == DocNo);
            if (row != null)
            {
                _context._1sdnlocks.Remove(row);
                await _context.SaveChangesAsync();
            }
        }
        private string GenerateIdDoc()
        {
            string num36 = _context._1sjourns.Max(e => e.Iddoc);
            if (!string.IsNullOrEmpty(num36))
            {
                string suffics = _context.ПрефиксИБ().PadRight(3); //num36.Substring(6);
                num36 = num36.Substring(0, 6).Trim();
                long num10 = Common.Decode36(num36) + 1;
                num36 = (Common.Encode36(num10) + suffics).PadLeft(9);
            }
            return num36;
        }
        public _1sjourn GetEntityJourn(byte Проведен, int КоличествоДвижений, int ЖурналИД_md, int ВидДокИД_dds, string Нумератор, string ВидДок, string НомерДок, DateTime dateTime,
           string ФирмаИД,
           string ПользовательИД,
           string СкладНазвание,
           string КонтрагентНазвание)
        {
            string num36 = GenerateIdDoc();
            if (dateTime == DateTime.MinValue)
                dateTime = DateTime.Now;
            var datestr = dateTime.ToString("yyyyMMdd");
            var h = dateTime.Hour;
            var m = dateTime.Minute;
            var s = dateTime.Second;
            var ms = dateTime.Millisecond;
            var time = (h * 3600 * 10000) + (m * 60 * 10000) + (s * 10000) + (ms * 10);
            var timestr = Common.Encode36(time).PadLeft(6);
            var dateTimeIddoc = datestr + timestr + num36;
            if (string.IsNullOrEmpty(Нумератор))
                Нумератор = ВидДокИД_dds.ToString();
            var dnPrefix = Нумератор.PadLeft(10) + dateTime.ToString("yyyy").PadRight(8);
            var ФирмаЮрЛицо = (from sc131 in _context.Sc131s
                               join sc4014 in _context.Sc4014s on sc131.Id equals sc4014.Sp4011
                               where sc4014.Id == ФирмаИД
                               select new
                               {
                                   Фирма = sc4014.Descr.Trim(),
                                   ЮрЛицоId = sc131.Id,
                                   ЮрЛицоПрефикс = sc131.Sp145.Trim()
                               }).FirstOrDefault();
            string prefixDB = _context.ПрефиксИБ(ПользовательИД);
            string prefix = prefixDB + ФирмаЮрЛицо.ЮрЛицоПрефикс;
            if (string.IsNullOrEmpty(НомерДок))
            {
                НомерДок = (from _j in _context._1sjourns
                            where _j.Dnprefix == dnPrefix && string.Compare(_j.Docno, prefix) >= 0 && _j.Docno.Substring(0, prefix.Length) == prefix
                            orderby _j.Dnprefix descending, _j.Docno descending
                            select _j.Docno).FirstOrDefault();
                if (НомерДок == null)
                    НомерДок = "0";
                else
                    НомерДок = НомерДок.Substring(prefix.Length);
                string postfix = "";
                if (ВидДокИД_dds == (int)StinClasses.Документы.ВидДокумента.СчетФактура)
                {
                    postfix = _фирма.ПолучитьПостфикс(ФирмаИД);
                    if (postfix == "@")
                        postfix = _пользователь.Постфикс(ПользовательИД);
                    postfix = "/" + postfix;
                    НомерДок = НомерДок.Substring(0, НомерДок.Length - postfix.Length);
                }

                string dnPrefixLockTable = Нумератор.PadLeft(10) + dateTime.ToString("yyyy").PadRight(18);
                string lockNumber = (from lock_table in _context._1sdnlocks
                                     where lock_table.Dnprefix == dnPrefix && string.Compare(lock_table.Docno, prefix) >= 0 && lock_table.Docno.Substring(0, prefix.Length) == prefix
                                     orderby lock_table.Docno descending
                                     select lock_table.Docno).FirstOrDefault();
                if (lockNumber != null)
                {
                    lockNumber = lockNumber.Substring(prefix.Length);
                    if (Convert.ToInt32(lockNumber) > Convert.ToInt32(НомерДок))
                        НомерДок = lockNumber;
                }
                НомерДок = prefix + (Convert.ToInt32(НомерДок) + 1).ToString().PadLeft(10 - prefix.Length - postfix.Length, '0') + postfix;
            }

            return new _1sjourn
            {
                Idjournal = ЖурналИД_md, // 10528,
                Iddoc = num36,
                Iddocdef = ВидДокИД_dds, //11037; //Результат диагностики,
                Appcode = 1, //(1) - опер учет.
                DateTimeIddoc = dateTimeIddoc,
                Dnprefix = dnPrefix,
                Docno = НомерДок, //DY00000012
                Closed = Проведен, //проведен
                Ismark = false, //пометка на удаление
                Actcnt = КоличествоДвижений, //Фактически хранит информацию о количестве движений по всем регистрам + записи периодических реквизитов
                Verstamp = 1, //Количество изменений записи таблицы. Изменением считается любое действие "Изменить (открыть)" + действия при изменении структуры
                Sp74 = ПользовательИД, //Автор
                Sp798 = Common.ПустоеЗначение, //Проект 
                Sp4056 = ФирмаИД, //Фирма
                Sp5365 = ФирмаЮрЛицо.ЮрЛицоId, //ЮрЛицо
                Sp8662 = prefixDB,
                Sp8663 = prefixDB + ";" + (СкладНазвание.Length > (29 - prefixDB.Length) ? СкладНазвание.Substring(0, 29 - prefixDB.Length) : СкладНазвание),
                Sp8664 = prefixDB + ";" + (КонтрагентНазвание.Length > (29 - prefixDB.Length) ? КонтрагентНазвание.Substring(0, 29 - prefixDB.Length) : КонтрагентНазвание),
                Sp8665 = prefixDB + ";" + ВидДок,
                Sp8666 = prefixDB + ";" + (ФирмаЮрЛицо.Фирма.Length > (29 - prefixDB.Length) ? ФирмаЮрЛицо.Фирма.Substring(0, 29 - prefixDB.Length) : ФирмаЮрЛицо.Фирма),
                Sp8720 = "",
                Sp8723 = ""
            };
        }
        public bool IsNew(string idDoc)
        {
            return _context._1sjourns.Any(x => x.Iddoc == idDoc);
        }
        //public async Task РегистрацияИзмененийРаспределеннойИБAsync(int ВидДокИД_dds, string IdDoc)
        //{
        //    string prefix = _context.ПрефиксИБ();
        //    var signs = (from dbset in _context._1sdbsets
        //                 where dbset.Dbsign.Trim() != prefix
        //                 select dbset.Dbsign).ToList();
        //    foreach (var sign in signs)
        //        await _context.Database.ExecuteSqlRawAsync("exec _1sp_RegisterUpdate @sign,@doc_dds,@num36,' '", new SqlParameter("@sign", sign), new SqlParameter("@doc_dds", ВидДокИД_dds), new SqlParameter("@num36", IdDoc));
        //    await _context.SaveChangesAsync();
        //}
        public async Task ОбновитьПодчиненныеДокументы(string ДокОснованиеId13, string DateTimeIddoc, string Iddoc)
        {
            SqlParameter paramParentVal = new SqlParameter("@parentVal", "O1" + ДокОснованиеId13);
            SqlParameter paramDateTimeIddoc = new SqlParameter("@date_time_iddoc", DateTimeIddoc);
            SqlParameter paramNum36 = new SqlParameter("@num36", Iddoc);
            await _context.Database.ExecuteSqlRawAsync("exec _1sp__1SCRDOC_Write 0,@parentVal,@date_time_iddoc,@num36,1", paramParentVal, paramDateTimeIddoc, paramNum36);
            await _context.SaveChangesAsync();
        }
        public async Task ОбновитьГрафыОтбора(int MdId, string Id13, string DateTimeIddoc, string Iddoc)
        {
            SqlParameter paramMdId = new SqlParameter("@mdId", MdId);
            SqlParameter paramId13 = new SqlParameter("@id13", "B1" + Id13);
            SqlParameter paramDateTimeIddoc = new SqlParameter("@date_time_iddoc", DateTimeIddoc);
            SqlParameter paramNum36 = new SqlParameter("@num36", Iddoc);
            await _context.Database.ExecuteSqlRawAsync("exec _1sp__1SCRDOC_Write @mdId,@id13,@date_time_iddoc,@num36,1", paramMdId, paramId13, paramDateTimeIddoc, paramNum36);
            await _context.SaveChangesAsync();
        }
        public async Task ОбновитьВремяТА(string Iddoc, string DateTimeIddoc)
        {
            DateTime docDate = DateTime.ParseExact(DateTimeIddoc.Substring(0, 8), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            int docTime = (int)(DateTimeIddoc.Substring(8, 6).Decode36());
            var dbValues = (from s in _context._1ssystems select new { d = s.Curdate, t = s.Curtime }).FirstOrDefault();
            bool NeedToUpdate = false;
            if (dbValues.d < docDate)
                NeedToUpdate = true;
            else if (dbValues.d == docDate)
                NeedToUpdate = dbValues.t < docTime;
            if (NeedToUpdate)
            {
                await _context.Database.ExecuteSqlRawAsync("Update _1SSYSTEM set CURDATE=@P1, CURTIME=@P2, EVENTIDTA=@P3",
                    new SqlParameter("@P1", docDate.ToShortDateString()),
                    new SqlParameter("@P2", docTime),
                    new SqlParameter("@P3", Iddoc));

                await _context.SaveChangesAsync();
            }
        }
        public async Task ОбновитьПоследовательность(string DateTimeIddoc)
        {
            _1sstream Последовательность = (from p in _context._1sstreams
                                            where p.Id == 1946
                                            select p).FirstOrDefault();
            Последовательность.DateTimeDocid = DateTimeIddoc;
            _context.Update(Последовательность);
            await _context.SaveChangesAsync();
        }
        public async Task ОбновитьСетевуюАктивность()
        {
            int СчетчикАктивности = await _context._1susers.Select(x => x.Netchgcn).FirstOrDefaultAsync();
            СчетчикАктивности++;
            await _context.Database.ExecuteSqlRawAsync("Update _1SUSERS set NETCHGCN=@P1",
                new SqlParameter("@P1", СчетчикАктивности));
            await _context.SaveChangesAsync();
        }
        public async Task ОбновитьАктивность(List<ОбщиеРеквизиты> реквизиты)
        {
            var MaxРеквизиты = реквизиты.FirstOrDefault(r => r.DateTimeIdDoc == реквизиты.Max(x => x.DateTimeIdDoc));
            await ОбновитьВремяТА(MaxРеквизиты.IdDoc, MaxРеквизиты.DateTimeIdDoc);
            await ОбновитьПоследовательность(MaxРеквизиты.DateTimeIdDoc);
            await ОбновитьСетевуюАктивность();
        }
        public async Task<string> НайтиДокументВДеревеAsync(string idDoc, int searchВидДок)
        {
            string result = "";
            string curIdDoc = idDoc;
            while (!string.IsNullOrEmpty(curIdDoc))
            {
                var s = await (from crDoc in _context._1scrdocs
                               where crDoc.Childid == curIdDoc
                               select new
                               {
                                   ВидДок = crDoc.Parentval.Substring(2, 4),
                                   id = crDoc.Parentval.Substring(6, 9)
                               })
                        .FirstOrDefaultAsync();
                curIdDoc = s == null ? "" : s.id;
                if (!string.IsNullOrEmpty(curIdDoc) && s.ВидДок.Trim().Decode36() == searchВидДок)
                {
                    curIdDoc = "";
                    result = s.id;
                }
            }
            return result;
        }
        public async Task ОбновитьTotals(int ВидДок, string idDoc)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "exec _1sp_DH" + ВидДок.ToString() + "_UpdateTotals @num36",
                new SqlParameter("@num36", idDoc)
                );
            await _context.SaveChangesAsync();
        }
        public async Task<Договор> ПолучитьДоговорНабора(string idDoc)
        {
            var договорId = await _context.Dh11948s.Where(x => x.Iddoc == idDoc).Select(x => x.Sp11932).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(договорId))
                return await _контрагент.GetДоговорAsync(договорId);
            return null;
        }
        public async Task<Склад> ПолучитьСкладНабора(string idDoc)
        {
            var складId = await _context.Dh11948s.Where(x => x.Iddoc == idDoc).Select(x => x.Sp11929).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(складId))
                return await _склад.GetEntityByIdAsync(складId);
            return null;
        }
        public async Task<Order> ПолучитьOrderНабора(string idDoc)
        {
            var orderId = await _context.Dh11948s.Where(x => x.Iddoc == idDoc).Select(x => x.Sp14003).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(orderId))
                return await _order.ПолучитьOrderWithItems(orderId);
            return null;
        }
        public async Task<ФормаОплатаЧерезЮКасса> GetФормаОплатаЧерезЮКассаById(string idDoc)
        {
            var d = await (from dh in _context.Dh13849s
                           join j in _context._1sjourns on dh.Iddoc equals j.Iddoc
                           where dh.Iddoc == idDoc
                           select new
                           {
                               dh,
                               j
                           }).FirstOrDefaultAsync();
            var doc = new ФормаОплатаЧерезЮКасса
            {
                Общие = new ОбщиеРеквизиты
                {
                    IdDoc = d.dh.Iddoc,
                    ДокОснование = !(string.IsNullOrWhiteSpace(d.dh.Sp13828) || d.dh.Sp13828 == Common.ПустоеЗначениеИд13) ? await ДокОснованиеAsync(d.dh.Sp13828.Substring(4)) : null,
                    Фирма = await _фирма.GetEntityByIdAsync(d.j.Sp4056),
                    Автор = await _пользователь.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    //НазваниеВЖурнале = Common.ВидыОперации.Where(x => x.Key == d.dh.Sp4760).Select(y => y.Value).FirstOrDefault(),
                    НомерДок = d.j.Docno,
                    ДатаДок = d.j.DateTimeIddoc.ToDateTime(),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh.Sp660,
                    Удален = d.j.Ismark
                },
                ShopId = d.dh.Sp13829.Trim(),
                SecretKey = d.dh.Sp13830.Trim(),
                PaymentId = d.dh.Sp13831.Trim(),
                СостояниеПлатежа = d.dh.Sp13833,
                Телефон = d.dh.Sp13834.Trim(),
                Email = d.dh.Sp13835.Trim(),
                ВидКонтакта = d.dh.Sp13836,
                СообщениеОтправлено = d.dh.Sp13837 == 1,
                УчитыватьНДС = d.dh.Sp13838 == 1,
                СуммаВклНДС = d.dh.Sp13839 == 1,
                Сумма = d.dh.Sp13845,
                СуммаНДС = d.dh.Sp13847,
                ConfirmationUrl = d.dh.Sp13832
            };
            return doc;
        }
        public async Task<ФормаЗаявкаПокупателя> GetФормаЗаявкаById(string idDoc)
        {
            var d = await (from dh in _context.Dh2457s
                           join j in _context._1sjourns on dh.Iddoc equals j.Iddoc
                           where dh.Iddoc == idDoc
                           select new
                           {
                               dh,
                               j
                           }).FirstOrDefaultAsync();
            var doc = new ФормаЗаявкаПокупателя
            {
                Общие = new ОбщиеРеквизиты
                {
                    IdDoc = d.dh.Iddoc,
                    ДокОснование = !(string.IsNullOrWhiteSpace(d.dh.Sp4433) || d.dh.Sp4433 == StinClasses.Common.ПустоеЗначениеИд13) ? await ДокОснованиеAsync(d.dh.Sp4433.Substring(4)) : null,
                    Фирма = await _фирма.GetEntityByIdAsync(d.j.Sp4056),
                    Автор = await _пользователь.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НазваниеВЖурнале = Common.ВидыОперации.Where(x => x.Key == d.dh.Sp4760).Select(y => y.Value).FirstOrDefault(),
                    НомерДок = d.j.Docno,
                    ДатаДок = d.j.DateTimeIddoc.ToDateTime(),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh.Sp660,
                    Удален = d.j.Ismark
                },
                БанковскийСчет = await _контрагент.GetБанковскийСчетAsync(d.dh.Sp2621),
                Контрагент = await _контрагент.GetКонтрагентAsync(d.dh.Sp2434),
                Договор = await _контрагент.GetДоговорAsync(d.dh.Sp2435),
                УчитыватьНДС = d.dh.Sp2439 == 1,
                СуммаВклНДС = d.dh.Sp2440 == 1,
                ТипЦен = await _контрагент.GetТипЦенAsync(d.dh.Sp2444),
                Скидка = await _контрагент.GetСкидкаAsync(d.dh.Sp2445),
                ДатаОплаты = d.dh.Sp2438,
                ДатаОтгрузки = d.dh.Sp4434,
                Склад = await _склад.GetEntityByIdAsync(d.dh.Sp4437),
                ВидОперации = Common.ВидыОперации.FirstOrDefault(x => x.Key == d.dh.Sp4760),
                СкидКарта = await _контрагент.GetСкидКартаAsync(d.dh.Sp8681),
                СпособОтгрузки = Common.СпособыОтгрузки.Where(x => x.Key == d.dh.Sp10382).Select(y => y.Value).FirstOrDefault(),
                Маршрут = await _маршрут.GetМаршрутByCodeAsync(d.dh.Sp11556),
                Order = await _order.ПолучитьOrderWithItems(d.dh.Sp13995)
            };
            var ТаблЧасть = await _context.Dt2457s
                .Where(x => x.Iddoc == idDoc)
                .ToListAsync();
            foreach (var row in ТаблЧасть)
            {
                doc.ТабличнаяЧасть.Add(new ФормаЗаявкаПокупателяТЧ
                {
                    Номенклатура = await _номенклатура.GetНоменклатураByIdAsync(row.Sp2446),
                    Количество = row.Sp2447,
                    Единица = await _номенклатура.GetЕдиницаByIdAsync(row.Sp2448),
                    Цена = row.Sp2450,
                    Сумма = row.Sp2451,
                    СтавкаНДС = _номенклатура.GetСтавкаНДС(row.Sp2454),
                    СуммаНДС = row.Sp2452
                });
            }

            return doc;
        }
    }
}
