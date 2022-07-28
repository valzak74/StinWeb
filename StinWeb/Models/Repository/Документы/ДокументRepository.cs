using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.Repository.Справочники;
using StinWeb.Models.Repository.Интерфейсы;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Документы
{
    public class ДокументRepository: IДокумент, IDisposable
    {
        private protected StinDbContext _context;
        public IUser userRepository;
        public IФирма фирмаRepository;
        public IКонтрагент контрагентRepository;
        public IНоменклатура номенклатураRepository;
        public IСклад складRepository;
        public IМастерская мастерскаяRepository;
        private protected bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    userRepository.Dispose();
                    фирмаRepository.Dispose();
                    контрагентRepository.Dispose();
                    номенклатураRepository.Dispose();
                    складRepository.Dispose();
                    мастерскаяRepository.Dispose();
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
        public ДокументRepository(StinDbContext context)
        {
            this._context = context;
            this.мастерскаяRepository = new МастерскаяRepository(context);
            this.userRepository = new UserRepository(context);
            this.фирмаRepository = new ФирмаRepository(context);
            this.контрагентRepository = new КонтрагентRepository(context);
            this.номенклатураRepository = new НоменклатураRepository(context);
            this.складRepository = new СкладRepository(context);
        }
        public bool NeedToOpenPeriod()
        {
            return _context.GetDateTA().Month != DateTime.Now.Month;
        }
        public IQueryable<ОбщиеРеквизиты> ЖурналДокументов(DateTime startDate, DateTime endDate, string idDoc)
        {
            string j_startDateTime = startDate == DateTime.MinValue ? "" : startDate.JournalDateTime();
            string j_endDateTime = endDate == DateTime.MinValue ? "" : endDate.JournalDateTime();

            return from j in _context._1sjourns
                   join пользователи in _context.Sc30s on j.Sp74 equals пользователи.Id
                   join фирмы in _context.Sc4014s on j.Sp4056 equals фирмы.Id
                   join docПеремещение in _context.Dh1628s on j.Iddoc equals docПеремещение.Iddoc into _docПеремещение
                   from docПеремещение in _docПеремещение.DefaultIfEmpty()
                   join docПеремещениеВМастерскую in _context.Dh10062s on j.Iddoc equals docПеремещениеВМастерскую.Iddoc into _docПеремещениеВМастерскую
                   from docПеремещениеВМастерскую in _docПеремещениеВМастерскую.DefaultIfEmpty()
                   join docПеремещениеИзделий in _context.Dh10080s on j.Iddoc equals docПеремещениеИзделий.Iddoc into _docПеремещениеИзделий
                   from docПеремещениеИзделий in _docПеремещениеИзделий.DefaultIfEmpty()
                   join docПриемВРемонт in _context.Dh9899s on j.Iddoc equals docПриемВРемонт.Iddoc into _docПриемВРемонт
                   from docПриемВРемонт in _docПриемВРемонт.DefaultIfEmpty()
                   join docИзменениеСтатуса in _context.Dh13737s on j.Iddoc equals docИзменениеСтатуса.Iddoc into _docИзменениеСтатуса
                   from docИзменениеСтатуса in _docИзменениеСтатуса.DefaultIfEmpty()
                   join docНаДиагностику in _context.Dh10995s on j.Iddoc equals docНаДиагностику.Iddoc into _docНаДиагностику
                   from docНаДиагностику in _docНаДиагностику.DefaultIfEmpty()
                   join docРезДиагностики in _context.Dh11037s on j.Iddoc equals docРезДиагностики.Iddoc into _docРезДиагностики
                   from docРезДиагностики in _docРезДиагностики.DefaultIfEmpty()
                   join docСогласие in _context.Dh11101s on j.Iddoc equals docСогласие.Iddoc into _docСогласие
                   from docСогласие in _docСогласие.DefaultIfEmpty()
                   join docЗапросЗапчастей in _context.Dh9927s on j.Iddoc equals docЗапросЗапчастей.Iddoc into _docЗапросЗапчастей
                   from docЗапросЗапчастей in _docЗапросЗапчастей.DefaultIfEmpty()
                   join docПровРаботы in _context.Dh9947s on j.Iddoc equals docПровРаботы.Iddoc into _docПровРаботы
                   from docПровРаботы in _docПровРаботы.DefaultIfEmpty()
                   join docЗавершениеРемонта in _context.Dh10457s on j.Iddoc equals docЗавершениеРемонта.Iddoc into _docЗавершениеРемонта
                   from docЗавершениеРемонта in _docЗавершениеРемонта.DefaultIfEmpty()
                   join docВыдачаИзРемонта in _context.Dh10054s on j.Iddoc equals docВыдачаИзРемонта.Iddoc into _docВыдачаИзРемонта
                   from docВыдачаИзРемонта in _docВыдачаИзРемонта.DefaultIfEmpty()
                   where 
                    (string.IsNullOrEmpty(j_startDateTime) ? true : j.DateTimeIddoc.CompareTo(j_startDateTime) >= 0) && 
                    (string.IsNullOrEmpty(j_endDateTime) ? true : j.DateTimeIddoc.CompareTo(j_endDateTime) <= 0) &&
                    (string.IsNullOrEmpty(idDoc) ? true : j.Iddoc == idDoc)
                   orderby j.DateTimeIddoc
                   select new ОбщиеРеквизиты
                   {
                       IdDoc = j.Iddoc,
                       Удален = j.Ismark,
                       Проведен = j.Closed == 1,
                       ВидДокумента10 = j.Iddocdef,
                       НазваниеВЖурнале = docВыдачаИзРемонта != null ? (
                            docВыдачаИзРемонта.Sp10461 == -1 ? (j.Sp8664.Trim() == "ПредпродажнаяПодготовка" ? "Предпродажная подготовка (касса)" : "Заточка цепи(касса)") :
                            docВыдачаИзРемонта.Sp10461 == -2 ? (docВыдачаИзРемонта.Sp10852 == Common.ПустоеЗначение ? "Закрытие Доп. работ (без оплаты)" : "Закрытие Доп. работ (касса)") :
                            docВыдачаИзРемонта.Sp10461 == -3 ? "Авансовая оплата работ (касса)" :
                            docВыдачаИзРемонта.Sp10461 == 0 ? (docВыдачаИзРемонта.Sp10852 == Common.ПустоеЗначение ? "{0} (без оплаты)" : "{0} (касса)") :
                            docВыдачаИзРемонта.Sp10461 == 1 ? "{0} (р/с)" :
                            docВыдачаИзРемонта.Sp10030 == 1 ? "{0} (по гарантии)" :
                            docВыдачаИзРемонта.Sp10030 == 2 ? "{0} (предпродажный)" :
                            docВыдачаИзРемонта.Sp10030 == 3 ? "{0} (за свой счет)" : ""
                            ) :
                            docПриемВРемонт != null ? (docПриемВРемонт.Sp10014.Substring(0, 4) == " 7MZ" ? "Корректировка квитанции" :
                            (!(string.IsNullOrWhiteSpace(docПриемВРемонт.Sp10014) || docПриемВРемонт.Sp10014 == Common.ПустоеЗначениеИд13)) && (docПриемВРемонт.Sp10014.Substring(0, 4) == " 7S0") && (docПриемВРемонт.Sp10111 != "   7OH   ") ? "Прием из доставки" : //ВидДокОснование = ПеремещениеИзделий и СтатусПартии != Т_Сломан
                            (docПриемВРемонт.Sp10111 == Common.ПустоеЗначение || docПриемВРемонт.Sp10111 == "   7OH   ") ? (
                                docПриемВРемонт.Sp9893 == 1 ? "{0} (по гарантии)" :
                                docПриемВРемонт.Sp9893 == 2 ? "{0} (предпродажный)" :
                                docПриемВРемонт.Sp9893 == 3 ? "{0} (за свой счет)" :
                                docПриемВРемонт.Sp9893 == 4 ? "{0} (на экспертизу)" :
                                "{0} (платный)"
                                ) :
                            docПриемВРемонт.Sp13771 == 1 ? "Регистрация претензии" : "Прием на сортировку"
                            ) :
                            docИзменениеСтатуса != null && !string.IsNullOrWhiteSpace(docИзменениеСтатуса.Sp13735) && docИзменениеСтатуса.Sp13735 != Common.ПустоеЗначение ? (
                                docИзменениеСтатуса.Sp13735 == "   ALS   " ? "Претензия на рассмотрении" :
                                docИзменениеСтатуса.Sp13735 == "   ALT   " ? "Претензия отклонена" :
                                docИзменениеСтатуса.Sp13735 == "   ALU   " ? "Замена по претензии" :
                                docИзменениеСтатуса.Sp13735 == "   ALV   " ? "Восстановление по претензии" :
                                docИзменениеСтатуса.Sp13735 == "   AML   " ? "Возврат денег по претензии" :
                                docИзменениеСтатуса.Sp13735 == "   AMM   " ? "Доукомплектация по претензии" :
                                ""
                            ) :
                            docСогласие != null ? (docСогласие.Sp11089 == "   8KE   " ? "Согласие на платный ремонт" : "Отказ от платного ремонта") :
                            docЗапросЗапчастей != null ? (docЗапросЗапчастей.Sp9907 == "   7OD   " ? "Выдача ЗЧ мастеру" : "Возврат ЗЧ от мастера") :
                            "",
                       НомерДок = j.Docno,
                       ДатаДок = Common.DateTimeIddoc(j.DateTimeIddoc),
                       Информация = docПриемВРемонт != null ? docПриемВРемонт.Sp10108 + "-" + docПриемВРемонт.Sp10109.ToString() :
                            docНаДиагностику != null ? docНаДиагностику.Sp10971 + "-" + docНаДиагностику.Sp10972.ToString() :
                            docВыдачаИзРемонта != null ? docВыдачаИзРемонта.Sp10036 + "-" + docВыдачаИзРемонта.Sp10037.ToString() :
                            docПеремещениеИзделий != null ? docПеремещениеИзделий.Sp10063 + "-" + docПеремещениеИзделий.Sp10064.ToString() :
                            docИзменениеСтатуса != null ? docИзменениеСтатуса.Sp13713 + "-" + docИзменениеСтатуса.Sp13714.ToString() :
                            "",
                       Автор = new User { Id = пользователи.Id, Name = пользователи.Descr.Trim() },
                       Фирма = new Фирма { Id = фирмы.Id, Наименование = фирмы.Descr.Trim() },
                       Комментарий = docПеремещение != null ? docПеремещение.Sp660.Trim() :
                                  docПеремещениеВМастерскую != null ? docПеремещениеВМастерскую.Sp660.Trim() :
                                  docПеремещениеИзделий != null ? docПеремещениеИзделий.Sp660.Trim() :
                                  docПриемВРемонт != null ? docПриемВРемонт.Sp660.Trim() :
                                  docИзменениеСтатуса != null ? docИзменениеСтатуса.Sp660.Trim() :
                                  docНаДиагностику != null ? docНаДиагностику.Sp660.Trim() :
                                  docРезДиагностики != null ? docРезДиагностики.Sp660.Trim() :
                                  docСогласие != null ? docСогласие.Sp660.Trim() :
                                  docЗапросЗапчастей != null ? docЗапросЗапчастей.Sp660.Trim() :
                                  docПровРаботы != null ? docПровРаботы.Sp660.Trim() :
                                  docЗавершениеРемонта != null ? docЗавершениеРемонта.Sp660.Trim() :
                                  docВыдачаИзРемонта != null ? docВыдачаИзРемонта.Sp660.Trim() :
                                  "не поддерживаемый тип документа"
                   };
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
                              ДатаДок = Common.DateTimeIddoc(j.DateTimeIddoc),
                          }).FirstOrDefaultAsync();
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
                ДатаДок = Common.DateTimeIddoc(j.DateTimeIddoc),
                Проведен = j.Closed == 1,
                Фирма = фирмаRepository.GetEntityById(j.Sp4056),
                Автор = await userRepository.GetUserByIdAsync(j.Sp74)
            };
        }
        public async Task<string> LockDocNoAsync(string userId, string ИдентификаторДокDds, int ДлинаНомера = 10, string FirmaId = null, string Год = null)
        {
            string docNo = "";
            if (string.IsNullOrEmpty(FirmaId))
                FirmaId = Common.FirmaSS;
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
        public async Task<ФормаПриемВРемонт> ПриемВРемонтAsync(string idDoc)
        {
            var d = await (from dh9899 in _context.Dh9899s
                           join j in _context._1sjourns on dh9899.Iddoc equals j.Iddoc
                           where dh9899.Iddoc == idDoc
                           select new
                           {
                               dh9899,
                               j
                           }).FirstOrDefaultAsync();
            var doc = new ФормаПриемВРемонт
            {
                Общие = new ОбщиеРеквизиты
                {
                    ТипФормы = ТипыФормы.Просмотр,
                    IdDoc = d.dh9899.Iddoc,
                    ДокОснование = !(string.IsNullOrWhiteSpace(d.dh9899.Sp10014) || d.dh9899.Sp10014 == Common.ПустоеЗначениеИд13) ? await ДокОснованиеAsync(d.dh9899.Sp10014.Substring(4)) : null,
                    Фирма = фирмаRepository.GetEntityById(d.j.Sp4056),
                    Автор = await userRepository.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НазваниеВЖурнале = (!(string.IsNullOrWhiteSpace(d.dh9899.Sp10014) || d.dh9899.Sp10014 == Common.ПустоеЗначениеИд13) && (d.dh9899.Sp10014.Substring(0, 4) == " 7S0") && (d.dh9899.Sp10111 != "   7OH   ")) ? "Прием из доставки" : //ВидДокОснование = ПеремещениеИзделий и СтатусПартии != Т_Сломан
                            (d.dh9899.Sp10111 == Common.ПустоеЗначение || d.dh9899.Sp10111 == "   7OH   ") ? (
                                d.dh9899.Sp9893 == 1 ? "{0} (по гарантии)" :
                                d.dh9899.Sp9893 == 2 ? "{0} (предпродажный)" :
                                d.dh9899.Sp9893 == 3 ? "{0} (за свой счет)" :
                                d.dh9899.Sp9893 == 4 ? "{0} (на экспертизу)" :
                                "{0} (платный)"
                                ) :
                            "Прием на сортировку",
                    НомерДок = d.j.Docno,
                    ДатаДок = Common.DateTimeIddoc(d.j.DateTimeIddoc),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh9899.Sp660,
                },
                ExpressForm = string.IsNullOrWhiteSpace(d.dh9899.Sp9891),
                НомерКвитанции = d.dh9899.Sp10108,
                ДатаКвитанции = d.dh9899.Sp10109,
                Претензия = d.dh9899.Sp13771 == 1,
                Изделие = d.dh9899.Sp9890 != Common.ПустоеЗначение ? await номенклатураRepository.GetНоменклатураByIdAsync(d.dh9899.Sp9890) : null,
                ЗаводскойНомер = d.dh9899.Sp9891,
                Гарантия = d.dh9899.Sp9893,
                ДатаПриема = d.dh9899.Sp10112,
                ДатаПродажи = d.dh9899.Sp9896,
                НомерРемонта = d.dh9899.Sp9897,
                Комплектность = d.dh9899.Sp10795.Trim(),
                Неисправность = d.dh9899.Sp9892 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh9899.Sp9892) : new Неисправность(),
                Неисправность2 = d.dh9899.Sp10712 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh9899.Sp10712) : new Неисправность(),
                Неисправность3 = d.dh9899.Sp10713 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh9899.Sp10713) : new Неисправность(),
                Неисправность4 = d.dh9899.Sp10714 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh9899.Sp10714) : new Неисправность(),
                Неисправность5 = d.dh9899.Sp10715 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh9899.Sp10715) : new Неисправность(),
                Заказчик = d.dh9899.Sp9889 != Common.ПустоеЗначение ? контрагентRepository.GetEntityById(d.dh9899.Sp9889) : new Контрагент(),
                Телефон = d.dh9899.Sp12394 != Common.ПустоеЗначение ? await контрагентRepository.ТелефонByIdAsync(d.dh9899.Sp12394) : new Телефон(),
                Email = d.dh9899.Sp13651 != Common.ПустоеЗначение ? await контрагентRepository.EmailByIdAsync(d.dh9899.Sp13651) : new Email(),
                Мастер = d.dh9899.Sp9894 != Common.ПустоеЗначение ? await мастерскаяRepository.МастерByIdAsync(d.dh9899.Sp9894) : null,
                Склад = d.dh9899.Sp10012 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh9899.Sp10012) : null,
                ПодСклад = d.dh9899.Sp10013 != Common.ПустоеЗначение ? await складRepository.GetПодСкладByIdAsync(d.dh9899.Sp10013) : null,
                СкладОткуда = d.dh9899.Sp10110 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh9899.Sp10110) : null,
                СтатусПартииId = d.dh9899.Sp10111,
                ДатаОбращения = d.dh9899.Sp13710,
                ПриложенныйДокумент1 = d.dh9899.Sp13752 != Common.ПустоеЗначение ? await мастерскаяRepository.ПриложенныйДокументByIdAsync(d.dh9899.Sp13752) : new ПриложенныйДокумент(),
                ПриложенныйДокумент2 = d.dh9899.Sp13753 != Common.ПустоеЗначение ? await мастерскаяRepository.ПриложенныйДокументByIdAsync(d.dh9899.Sp13753) : new ПриложенныйДокумент(),
                ПриложенныйДокумент3 = d.dh9899.Sp13754 != Common.ПустоеЗначение ? await мастерскаяRepository.ПриложенныйДокументByIdAsync(d.dh9899.Sp13754) : new ПриложенныйДокумент(),
                ПриложенныйДокумент4 = d.dh9899.Sp13755 != Common.ПустоеЗначение ? await мастерскаяRepository.ПриложенныйДокументByIdAsync(d.dh9899.Sp13755) : new ПриложенныйДокумент(),
                ПриложенныйДокумент5 = d.dh9899.Sp13756 != Common.ПустоеЗначение ? await мастерскаяRepository.ПриложенныйДокументByIdAsync(d.dh9899.Sp13756) : new ПриложенныйДокумент(),
                ВнешнийВид = d.dh9899.Sp13757,
                СпособВозвращенияId = d.dh9899.Sp13751,
            };
            doc.Photos = await мастерскаяRepository.ФотоПриемВРемонтAsync(doc.КвитанцияId);

            return doc;
        }
        public async Task<ФормаИзменениеСтатуса> ИзменениеСтатусаAsync(string idDoc)
        {
            var d = await (from dh in _context.Dh13737s
                           join j in _context._1sjourns on dh.Iddoc equals j.Iddoc
                           where dh.Iddoc == idDoc
                           select new
                           {
                               dh,
                               j
                           }).FirstOrDefaultAsync();
            ФормаПриемВРемонт ДокОснование = null;
            if (!string.IsNullOrWhiteSpace(d.dh.Sp13718) && d.dh.Sp13718 != Common.ПустоеЗначениеИд13)
                ДокОснование = await ПриемВРемонтAsync(d.dh.Sp13718.Substring(4));
            return new ФормаИзменениеСтатуса
            {
                Общие = new ОбщиеРеквизиты
                {
                    IdDoc = d.dh.Iddoc,
                    ДокОснование = ДокОснование != null ? new ДокОснование
                    {
                        IdDoc = ДокОснование.Общие.IdDoc,
                        ВидДокумента10 = ДокОснование.Общие.ВидДокумента10,
                        НомерДок = ДокОснование.Общие.НомерДок,
                        ДатаДок = ДокОснование.Общие.ДатаДок,
                        Проведен = ДокОснование.Общие.Проведен,
                        Фирма = ДокОснование.Общие.Фирма,
                        Автор = ДокОснование.Общие.Автор
                    } : null,
                    Фирма = фирмаRepository.GetEntityById(d.j.Sp4056),
                    Автор = await userRepository.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НомерДок = d.j.Docno,
                    ДатаДок = Common.DateTimeIddoc(d.j.DateTimeIddoc),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh.Sp660,
                },
                ДанныеКвитанции = new DataManager.Справочники.Мастерская.ИнформацияИзделия
                {
                    ДокОснование = ДокОснование != null ? new ДокОснование
                    {
                        IdDoc = ДокОснование.Общие.IdDoc,
                        ВидДокумента10 = ДокОснование.Общие.ВидДокумента10,
                        НомерДок = ДокОснование.Общие.НомерДок,
                        ДатаДок = ДокОснование.Общие.ДатаДок,
                        Проведен = ДокОснование.Общие.Проведен,
                        Фирма = ДокОснование.Общие.Фирма,
                        Автор = ДокОснование.Общие.Автор
                    } : null,
                    НомерКвитанции = d.dh.Sp13713,
                    ДатаКвитанции = d.dh.Sp13714,
                    Изделие = d.dh.Sp13720 != Common.ПустоеЗначение ? await номенклатураRepository.GetНоменклатураByIdAsync(d.dh.Sp13720) : null,
                    ЗаводскойНомер = d.dh.Sp13721,
                    Гарантия = d.dh.Sp13723,
                    Заказчик = d.dh.Sp13719 != Common.ПустоеЗначение ? контрагентRepository.GetEntityById(d.dh.Sp13719) : null,
                    Email = ДокОснование != null ? ДокОснование.Email : null,
                    Телефон = ДокОснование != null ? ДокОснование.Телефон : null,
                    ДатаПриема = d.dh.Sp13728,
                    ДатаПродажи = d.dh.Sp13724,
                    НомерРемонта = d.dh.Sp13725,
                    Комплектность = ДокОснование != null ? ДокОснование.Комплектность : "",
                    Неисправность = d.dh.Sp13722 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh.Sp13722) : null,
                    Неисправность2 = d.dh.Sp13729 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh.Sp13729) : null,
                    Неисправность3 = d.dh.Sp13730 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh.Sp13730) : null,
                    Неисправность4 = d.dh.Sp13731 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh.Sp13731) : null,
                    Неисправность5 = d.dh.Sp13732 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh.Sp13732) : null,
                    Комментарий = d.dh.Sp660,
                    СтатусПартииId = d.dh.Sp13727,
                    СкладОткуда = d.dh.Sp13726 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh.Sp13726) : null,
                    Мастер = ДокОснование != null ? ДокОснование.Мастер : null,
                    Склад = d.dh.Sp13715 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh.Sp13715) : null,
                    ПодСклад = d.dh.Sp13716 != Common.ПустоеЗначение ? await складRepository.GetПодСкладByIdAsync(d.dh.Sp13716) : null,
                    ДатаОбращения = ДокОснование != null ? ДокОснование.ДатаОбращения : DateTime.MinValue,
                },
                СтатусНовыйId = d.dh.Sp13735,
            };
        }
        public async Task<ФормаПеремещениеИзделий> ПеремещениеИзделийAsync(string idDoc)
        {
            var d = await (from dh10080 in _context.Dh10080s
                           join j in _context._1sjourns on dh10080.Iddoc equals j.Iddoc
                           where dh10080.Iddoc == idDoc
                           select new
                           {
                               dh10080,
                               j
                           }).FirstOrDefaultAsync();
            ФормаПриемВРемонт ДокОснование = null;
            if (!string.IsNullOrWhiteSpace(d.dh10080.Sp10734) && d.dh10080.Sp10734 != Common.ПустоеЗначениеИд13)
                ДокОснование = await ПриемВРемонтAsync(d.dh10080.Sp10734.Substring(4));
            return new ФормаПеремещениеИзделий
            {
                Общие = new ОбщиеРеквизиты
                {
                    IdDoc = d.dh10080.Iddoc,
                    ДокОснование = ДокОснование != null ? new ДокОснование
                    {
                        IdDoc = ДокОснование.Общие.IdDoc,
                        ВидДокумента10 = ДокОснование.Общие.ВидДокумента10,
                        НомерДок = ДокОснование.Общие.НомерДок,
                        ДатаДок = ДокОснование.Общие.ДатаДок,
                        Проведен = ДокОснование.Общие.Проведен,
                        Фирма = ДокОснование.Общие.Фирма,
                        Автор = ДокОснование.Общие.Автор
                    } : null,
                    Фирма = фирмаRepository.GetEntityById(d.j.Sp4056),
                    Автор = await userRepository.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НомерДок = d.j.Docno,
                    ДатаДок = Common.DateTimeIddoc(d.j.DateTimeIddoc),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh10080.Sp660,
                },
                НомерКвитанции = d.dh10080.Sp10063,
                ДатаКвитанции = d.dh10080.Sp10064,
                Заказчик = d.dh10080.Sp10065 != Common.ПустоеЗначение ? контрагентRepository.GetEntityById(d.dh10080.Sp10065) : null,
                Телефон = ДокОснование != null ? ДокОснование.Телефон : null,
                Email = ДокОснование != null ? ДокОснование.Email : null,
                Изделие = d.dh10080.Sp10066 != Common.ПустоеЗначение ? await номенклатураRepository.GetНоменклатураByIdAsync(d.dh10080.Sp10066) : null,
                ЗаводскойНомер = d.dh10080.Sp10067,
                Гарантия = d.dh10080.Sp10069,
                Мастер = d.dh10080.Sp10070 != Common.ПустоеЗначение ? await мастерскаяRepository.МастерByIdAsync(d.dh10080.Sp10070) : null,
                ДатаПриема = d.dh10080.Sp10124,
                ДатаПродажи = d.dh10080.Sp10071,
                НомерРемонта = d.dh10080.Sp10072,
                Комплектность = ДокОснование != null ? ДокОснование.Комплектность : "",
                Склад = d.dh10080.Sp10073 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh10080.Sp10073) : null,
                ПодСклад = d.dh10080.Sp10074 != Common.ПустоеЗначение ? await складRepository.GetПодСкладByIdAsync(d.dh10080.Sp10074) : null,
                СкладПолучатель = d.dh10080.Sp10075 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh10080.Sp10075) : null,
                ПодСкладПолучатель = d.dh10080.Sp10341 != Common.ПустоеЗначение ? await складRepository.GetПодСкладByIdAsync(d.dh10080.Sp10341) : null,
                СкладОткуда = d.dh10080.Sp10076 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh10080.Sp10076) : null,
                СтатусПартииId = d.dh10080.Sp10078,
                НомерМаршрута = string.IsNullOrWhiteSpace(d.dh10080.Sp11832) ? null : new Маршрут { Code = d.dh10080.Sp11832, Наименование = d.dh10080.Sp11833 },
                ВидДокумента = d.dh10080.Sp10340,
                Неисправность = d.dh10080.Sp10068 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10080.Sp10068) : null,
                Неисправность2 = d.dh10080.Sp10728 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10080.Sp10728) : null,
                Неисправность3 = d.dh10080.Sp10729 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10080.Sp10729) : null,
                Неисправность4 = d.dh10080.Sp10730 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10080.Sp10730) : null,
                Неисправность5 = d.dh10080.Sp10731 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10080.Sp10731) : null,
                ДатаОбращения = ДокОснование != null ? ДокОснование.ДатаОбращения : Common.min1cDate,
            };
        }
        public async Task<ФормаНаДиагностику> НаДиагностикуAsync(string idDoc)
        {
            var d = await (from dh10995 in _context.Dh10995s
                           join j in _context._1sjourns on dh10995.Iddoc equals j.Iddoc
                           where dh10995.Iddoc == idDoc
                           select new
                           {
                               dh10995,
                               j
                           }).FirstOrDefaultAsync();
            ФормаПриемВРемонт ДокОснование = null;
            if (!string.IsNullOrWhiteSpace(d.dh10995.Sp10977) && d.dh10995.Sp10977 != Common.ПустоеЗначениеИд13)
                ДокОснование = await ПриемВРемонтAsync(d.dh10995.Sp10977.Substring(4));
            var doc = new ФормаНаДиагностику
            {
                Общие = new ОбщиеРеквизиты
                {
                    ТипФормы = ТипыФормы.Просмотр,
                    IdDoc = d.dh10995.Iddoc,
                    ДокОснование = ДокОснование != null ? new ДокОснование 
                    {
                        IdDoc = ДокОснование.Общие.IdDoc,
                        ВидДокумента10 = ДокОснование.Общие.ВидДокумента10,
                        НомерДок = ДокОснование.Общие.НомерДок,
                        ДатаДок = ДокОснование.Общие.ДатаДок,
                        Проведен = ДокОснование.Общие.Проведен,
                        Фирма = ДокОснование.Общие.Фирма,
                        Автор = ДокОснование.Общие.Автор
                    } : null,
                    Фирма = фирмаRepository.GetEntityById(d.j.Sp4056),
                    Автор = await userRepository.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НомерДок = d.j.Docno,
                    ДатаДок = Common.DateTimeIddoc(d.j.DateTimeIddoc),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh10995.Sp660,
                },
                НомерКвитанции = d.dh10995.Sp10971,
                ДатаКвитанции = d.dh10995.Sp10972,
                Изделие = d.dh10995.Sp10979 != Common.ПустоеЗначение ? await номенклатураRepository.GetНоменклатураByIdAsync(d.dh10995.Sp10979) : null,
                ЗаводскойНомер = d.dh10995.Sp10980,
                Гарантия = d.dh10995.Sp10982,
                ДатаПриема = d.dh10995.Sp10987,
                ДатаПродажи = d.dh10995.Sp10983,
                НомерРемонта = d.dh10995.Sp10984,
                Комплектность = ДокОснование != null ? ДокОснование.Комплектность : "",
                Неисправность = d.dh10995.Sp10981 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10995.Sp10981) : null,
                Неисправность2 = d.dh10995.Sp10988 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10995.Sp10988) : null,
                Неисправность3 = d.dh10995.Sp10989 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10995.Sp10989) : null,
                Неисправность4 = d.dh10995.Sp10990 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10995.Sp10990) : null,
                Неисправность5 = d.dh10995.Sp10991 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10995.Sp10991) : null,
                Заказчик = d.dh10995.Sp10978 != Common.ПустоеЗначение ? контрагентRepository.GetEntityById(d.dh10995.Sp10978) : null,
                Телефон = ДокОснование != null ? ДокОснование.Телефон : null,
                Email = ДокОснование != null ? ДокОснование.Email : null,
                Мастер = d.dh10995.Sp10973 != Common.ПустоеЗначение ? await мастерскаяRepository.МастерByIdAsync(d.dh10995.Sp10973) : null,
                Склад = d.dh10995.Sp10974 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh10995.Sp10974) : null,
                ПодСклад = d.dh10995.Sp10975 != Common.ПустоеЗначение ? await складRepository.GetПодСкладByIdAsync(d.dh10995.Sp10975) : null,
                СкладОткуда = d.dh10995.Sp10985 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh10995.Sp10985) : null,
                СтатусПартииId = d.dh10995.Sp10986,
                ДатаОбращения = ДокОснование != null ? ДокОснование.ДатаОбращения : Common.min1cDate,
            };
            doc.Photos = await мастерскаяRepository.ФотоПриемВРемонтAsync(doc.КвитанцияId);

            return doc;
        }
        public async Task<ФормаАвансоваяОплата> АвансоваяОплатаAsync(string idDoc)
        {
            var d = await (from dh10054 in _context.Dh10054s
                           join j in _context._1sjourns on dh10054.Iddoc equals j.Iddoc
                           where dh10054.Iddoc == idDoc
                           select new
                           {
                               dh10054,
                               j
                           }).FirstOrDefaultAsync();
            ФормаПриемВРемонт ДокОснование = null;
            if (!string.IsNullOrWhiteSpace(d.dh10054.Sp10209))
                ДокОснование = await ПриемВРемонтAsync(d.dh10054.Sp10209.Substring(4));
            var doc = new ФормаАвансоваяОплата
            {
                Общие = new ОбщиеРеквизиты
                {
                    ТипФормы = ТипыФормы.Просмотр,
                    IdDoc = d.dh10054.Iddoc,
                    ДокОснование = ДокОснование != null ? new ДокОснование
                    {
                        IdDoc = ДокОснование.Общие.IdDoc,
                        ВидДокумента10 = ДокОснование.Общие.ВидДокумента10,
                        НомерДок = ДокОснование.Общие.НомерДок,
                        ДатаДок = ДокОснование.Общие.ДатаДок,
                        Проведен = ДокОснование.Общие.Проведен,
                        Фирма = ДокОснование.Общие.Фирма,
                        Автор = ДокОснование.Общие.Автор
                    } : null,
                    Фирма = фирмаRepository.GetEntityById(d.j.Sp4056),
                    Автор = await userRepository.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НомерДок = d.j.Docno,
                    ДатаДок = Common.DateTimeIddoc(d.j.DateTimeIddoc),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh10054.Sp660,
                },
                НомерКвитанции = d.dh10054.Sp10036,
                ДатаКвитанции = d.dh10054.Sp10037,
                Изделие = d.dh10054.Sp10027 != Common.ПустоеЗначение ? await номенклатураRepository.GetНоменклатураByIdAsync(d.dh10054.Sp10027) : null,
                ЗаводскойНомер = d.dh10054.Sp10028,
                Гарантия = d.dh10054.Sp10030,
                ДатаПриема = d.dh10054.Sp10034,
                ДатаПродажи = d.dh10054.Sp10033,
                НомерРемонта = d.dh10054.Sp10035,
                Комплектность = ДокОснование != null ? ДокОснование.Комплектность : "",
                Неисправность = d.dh10054.Sp10029 != Common.ПустоеЗначение ? await мастерскаяRepository.НеисправностьByIdAsync(d.dh10054.Sp10029) : null,
                Неисправность2 = ДокОснование != null ? ДокОснование.Неисправность2 : null,
                Неисправность3 = ДокОснование != null ? ДокОснование.Неисправность3 : null,
                Неисправность4 = ДокОснование != null ? ДокОснование.Неисправность4 : null,
                Неисправность5 = ДокОснование != null ? ДокОснование.Неисправность5 : null,
                Заказчик = d.dh10054.Sp10026 != Common.ПустоеЗначение ? контрагентRepository.GetEntityById(d.dh10054.Sp10026) : null,
                Телефон = ДокОснование != null ? ДокОснование.Телефон : null,
                Email = ДокОснование != null ? ДокОснование.Email : null,
                Склад = d.dh10054.Sp10038 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh10054.Sp10038) : null,
                ПодСклад = d.dh10054.Sp10039 != Common.ПустоеЗначение ? await складRepository.GetПодСкладByIdAsync(d.dh10054.Sp10039) : null,
                СкладОткуда = d.dh10054.Sp10119 != Common.ПустоеЗначение ? await складRepository.GetEntityByIdAsync(d.dh10054.Sp10119) : null,
                СтатусПартииId = d.dh10054.Sp10041,
                ДатаОбращения = ДокОснование != null ? ДокОснование.ДатаОбращения : Common.min1cDate,
            };
            doc.Photos = await мастерскаяRepository.ФотоПриемВРемонтAsync(doc.КвитанцияId);

            doc.ТабличнаяЧасть = await (from dt in _context.Dt10054s
                                        where dt.Iddoc == idDoc
                                        select new тчАвансоваяОплата
                                        {
                                            Работа = new Работа { Id = dt.Sp10049 },
                                            Количество = dt.Sp10763,
                                            Цена = dt.Sp10764,
                                            Сумма = dt.Sp10052
                                        })
                                 .ToListAsync();

            return doc;
        }
    }
}
