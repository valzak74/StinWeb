using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.IO.Compression;
using System.Net.Http.Headers;
using StinClasses.Models;

namespace StinWeb.Models.DataManager
{
    public static class Common
    {
        public static long Decode36(string input)
        {
            string CharList = "0123456789abcdefghijklmnopqrstuvwxyz";
            var reversed = input.ToLower().Reverse();
            long result = 0;
            int pos = 0;
            foreach (char c in reversed)
            {
                result += CharList.IndexOf(c) * (long)Math.Pow(36, pos);
                pos++;
            }
            return result;
        }
        public static string Encode36(long input)
        {
            if (input < 0)
                return "";
            else
            {
                string CharList = "0123456789abcdefghijklmnopqrstuvwxyz";
                char[] clistarr = CharList.ToUpper().ToCharArray();
                var result = new Stack<char>();
                while (input != 0)
                {
                    result.Push(clistarr[input % 36]);
                    input /= 36;
                }
                return new string(result.ToArray());
            }
        }

        public static readonly DateTime min1cDate = new DateTime(1753, 1, 1);
        public static readonly decimal zero = 0;

        public static readonly string ПустоеЗначение = "     0   ";
        public static readonly string ПустоеЗначениеИд13 = "   0     0   ";

        public static readonly string ВалютаРубль = "     1   ";

        public static readonly string DefaultКасса = "     1   "; //кассовый аппарат в торговом зале

        public static readonly string FirmaStPlus = "     1   ";
        public static readonly string FirmaIP = "     3S  ";
        public static readonly string FirmaSS = "     4S  ";
        public static readonly string FirmaPartner = "  38WJD  ";
        public static readonly string FirmaInst = "     2S  ";

        public static readonly string UserRobot = "    20D  ";

        public static readonly string НоменклатураМусор = "   9U7S  ";
        public static readonly string НоменклатураЗапчасти = "    17S  ";

        public static readonly string КонтрагентИзМастерскойОрганизации = "   1QKS  ";
        public static readonly string КонтрагентИзМастерскойФизЛица = "   1QJS  ";

        public static readonly string ДоговорыУсловияСтандартныеРозничные = "     2S  ";

        public static readonly string СкладОтстой = "     6S  ";

        public static readonly string НумераторПКО = "3376";
        public static readonly string НумераторВыдачаРемонт = "11331";

        private static string _префиксИБ = "";

        public static readonly string ФорматЦены = "{0:#,##0.00;-#,##0.00;''}";
        public static readonly string ФорматКоличества = "{0:#,##0.###;-#,##0.###;''}";
        public static readonly string ФорматЦеныСи = "#,##0.00;-#,##0.00;''";
        public static readonly string ФорматКоличестваСи = "#,##0.###;-#,##0.###;''";
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
        public static void AddOrUpdateObjectAsJson(this ISession session, string key, Корзина value, bool increment = true)
        {
            var корзина = session.GetObjectFromJson<List<Корзина>>(key);
            if (корзина == null)
            {
                корзина = new List<Корзина>();
                корзина.Add(value);
            }
            else
            {
                var item = корзина.Find(x => x.Id == value.Id);
                if (item == null)
                    корзина.Add(value);
                else
                {
                    if (increment)
                        item.Quantity = item.Quantity + value.Quantity;
                    else
                        item.Quantity = value.Quantity;
                    item.Цена = value.Цена;
                }
            }
            session.SetObjectAsJson(key, корзина);
        }
        public static readonly Dictionary<decimal, string> ТипРемонта = new Dictionary<decimal, string>() {
            {0, "Платный" },
            {1, "Гарантийный" },
            {2, "Предпродажный" },
            {3, "За свой счет" },
            {4, "Экспертиза" }
        };
        public static readonly Dictionary<decimal, string> СпособыВозвращения = new Dictionary<decimal, string>()
        {
            {0, "Самостоятельно в сервисе" },
            {1, "Через экспедитора ИП Павлов" }
        };
        public static readonly List<string> Гарантия = new List<string> { "Платный", "Гарантийный", "Предпродажный", "За свой счет", "Экспертиза" };

        public static readonly Dictionary<string, string> СтатусПартии = new Dictionary<string, string>()
        {
            {"","Оформление..." },
            {"   7OH   ","Принят в ремонт" },
            {"   7OI   ","Готов к выдаче" },
            {"   8IM   ","На диагностике" },
            {"   8KG   ","Экспертиза завершена" },
            {"   8KH   ","Отказ от платного ремонта" },
            {"   8IN   ","Ожидание запасных частей" },
            {"   8IO   ","В ремонте" },
            {"   ALM   ","Сортировка" },
            {"   ALS   ","Претензия на рассмотрении" },
            {"   ALT   ","Претензия отклонена" },
            {"   ALU   ","Замена по претензии" },
            {"   ALV   ","Восстановление по претензии" },
            {"   AML   ","Возврат денег по претензии" },
            {"   AMM   ","Доукомплектация по претензии" },
            {"   AMS   ","Диагностика претензии" },
            {"   2TB   ","Товар (купленный)" },
            {"   2TC   ","Товар (принятый)" },
            {"   4XZ   ","Товар (в рознице)" },
        };
        public static readonly Dictionary<string, string> СтавкиНДС = new Dictionary<string, string>()
        {
            {"    I9   ","Без НДС" },
            {"    I8   ","10%" },
            {"    I7   ","20%" },
            {"   6F2   ","18%" },
            {"   9YV   ","0%" },
        };
        public static readonly Dictionary<string, string> КодОперации = new Dictionary<string, string>()
        {
            {"    C2   ","Поступление ТМЦ (купля-продажа)" },
            {"   7XX   ","Поступление ТМЦ (купля-продажа)" },//без списания остатков
            {"   7G6   ","Отчет по ОтветХранению" },
            {"   3AA   ","Реализация (розница, ЕНВД)" },
            {"   16S   ","Реализация (купля-продажа)" },
            {"   7XW   ","Реализация (купля-продажа)" },//без списания остатков
            {"   6PJ   ","Внутреннее перемещение с доставкой" },
            {"   8KE   ","Согласие на платный ремонт" },
            {"   8KF   ","Отказ от платного ремонта" },
            {"   7OD   ","Выдача ЗЧ мастеру" },
            {"   7OE   ","Возврат ЗЧ от мастера" },
        };
        public static readonly Dictionary<string, string> ВидыОперации = new Dictionary<string, string>()
        {
            {"   3O1   ","Неподтвержденный счет" },
            {"   3O2   ","Счет на оплату" },
            {"   3O3   ","Заявка (на согласование)" },
            {"   AD6   ","Заявка (одобренная)" },
            {"   AI8   ","Заявка дилера" },
        };
        public static readonly Dictionary<string, string> СпособыОтгрузки = new Dictionary<string, string>()
        {
            {"   80N   ","Самовывоз" },
            {"   80O   ","Доставка" },
            {"   8MN   ","Межгород доставка" },
            {"   A49   ","Дальняя доставка" },
            {"   A9W   ","Доставка 150 руб." },
            {"   A9X   ","Доставка 500 руб." },
        };
        public static readonly Dictionary<string, string> ВидДолга = new Dictionary<string, string>()
        {
            {"   68Z   ","Долг за работы (в рознице)" },
        };
        public static readonly Dictionary<string, string> СпособыРезервирования = new Dictionary<string, string>()
        {
            {"   64P   ","Резервировать только по заказам" },//Заказы
            {"   64Q   ","Резервировать только из текущего остатка" },//Остаток
            {"   64R   ","Резервировать по заказам и из текущего остатка" },//ЗаказыОстаток
            {"   64S   ","Резервировать из текущего остатка и по заказам" },//ОстатокЗаказы
        };
        public static readonly Dictionary<string, string> ТипыЦен = new Dictionary<string, string>()
        {
            {"     1   ","Закупочная" },
            {"     2   ","Оптовая" },
            {"     4   ","Розничная" },
            {"     6S  ","Сп" },
            {"     PD  ","Особая" },
        };
        public static readonly Dictionary<string, string> ВидыКолонокСкидок = new Dictionary<string, string>()
        {
            {"   93R   ","Колонка1" },
            {"   93S   ","Колонка2" },
            {"   93T   ","Колонка3" },
            {"   93U   ","Колонка4" },
            {"   93V   ","Колонка5" },
        };
        public static readonly Dictionary<int, string> ВидыДокументов = new Dictionary<int, string>()
        {
            {1582,"Поступление ТМЦ" },
            {13369,"Корректировка Сроков Оплаты" },

            {9899,"Прием в ремонт" },
            {13737,"ИзменениеСтатуса" },
            {10080,"Перемещение Изделий" },
            {10995,"На Диагностику" },
            {11037,"Результат диагностики" },
            {11101,"Согласие" },
            {9927,"Запрос запчастей" },
            {9947,"Проведенные работы" },
            {10457,"Завершение ремонта" },
            {10054,"Выдача из ремонта" },

            {1628,"Перемещение ТМЦ" },
            {10062,"Перемещение в Мастерскую" },
        };
        public static readonly List<ВидДокумента> ВидыДокументов2 = new List<ВидДокумента>()
        {
            new ВидДокумента { Вид10 = 1582, Вид36 = "", Идентификатор = "ПоступлениеТМЦ", Представление = "Поступление ТМЦ"},
            new ВидДокумента { Вид10 = 13369, Вид36 = "", Идентификатор = "КорректировкаСроковОплаты", Представление = "Корректировка Сроков Оплаты"},
            new ВидДокумента { Вид10 = 10080, Вид36 = " 7S0", Идентификатор = "ПеремещениеИзделий", Представление = "Перемещение Изделий"},
        };
        public static DateTime GetDateTA(this StinDbContext context)
        {
            return context._1ssystems.First().Curdate;
        }

        public static DateTime GetRegTA(this StinDbContext context)
        {
            DateTime DateTA = GetDateTA(context);
            return new DateTime(DateTA.Year, DateTA.Month, 1);
        }
        public static bool NeedToOpenPeriod(this StinDbContext context)
        {
            return GetDateTA(context).Month != DateTime.Now.Month;
        }
        public static DateTime DateTimeIddoc(this string aValue)
        {
            DateTime result = new DateTime();
            if (aValue.Length >= 14 && DateTime.TryParseExact(aValue.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                //var seconds = Decode36(aValue.Substring(8, 6).Trim()) / 10000;
                //result = result.AddSeconds(seconds);
                var milliSeconds = Decode36(aValue.Substring(8, 6).Trim()) / 10;
                result = result.AddMilliseconds(milliSeconds);
            }
            return result;
        }
        public static string ПрефиксИБ(StinDbContext context)
        {
            if (string.IsNullOrEmpty(_префиксИБ))
                _префиксИБ = (from _const in context._1sconsts
                                where _const.Id == 3701 && _const.Objid == "     0   " && _const.Date <= min1cDate
                                orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                                select _const.Value).FirstOrDefault().Trim();
            return _префиксИБ;
        }
        public static string ПрефиксИБ(this StinDbContext context, Справочники.User user, bool alt = false)
        {
            return context.ПрефиксИБ(user.Id, alt);
        }
        public static string ПрефиксИБ(this StinDbContext context, string userId, bool alt = false)
        {
            var result = (from sc30 in context.Sc30s
                          join sc9506 in context.Sc9506s on sc30.Sp11726 equals sc9506.Id into _sc9506
                          from sc9506 in _sc9506.DefaultIfEmpty()
                          where sc30.Id == userId
                          select new
                          {
                              ПрефиксИБ = sc9506 != null ? sc9506.Sp9504.Trim() : "",
                              ПрефиксИБАльт = sc9506 != null ? sc9506.Sp12925.Trim() : ""
                          }).FirstOrDefault();
            string ПрефиксИБ = (alt && result.ПрефиксИБАльт != "") ? result.ПрефиксИБАльт : result.ПрефиксИБ;
            if (string.IsNullOrEmpty(ПрефиксИБ))
                ПрефиксИБ = (from _const in context._1sconsts
                             where _const.Id == 3701 && _const.Objid == "     0   " && _const.Date <= min1cDate
                             orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                             select _const.Value).FirstOrDefault().Trim();
            return ПрефиксИБ;
        }
        public static bool ПолучитьКвитанцию(string КвитанцияИД, out Квитанция квитанция)
        {
            квитанция = null;
            string[] kv = КвитанцияИД.Split('-');
            if (kv.Length == 2)
            {
                int data;
                if (Int32.TryParse(kv[1], out data))
                    квитанция = new Квитанция
                    {
                        Номер = kv[0],
                        Дата = data
                    };
            }
            return квитанция != null;
        }
        public static string GenerateId(this StinDbContext context, int ВидСпрИД_dds)
        {
            _1suidctl ent = (from idCtl in context._1suidctls
                            where idCtl.Typeid == ВидСпрИД_dds
                            select idCtl).FirstOrDefault();
            if (ent == null)
                ent = new _1suidctl { Typeid = ВидСпрИД_dds, Maxid = ПустоеЗначение };
            string MaxId = ent.Maxid.Substring(0, 6).Trim();
            long num10 = Decode36(MaxId) + 1;

            ent.Maxid = (Encode36(num10) + ПрефиксИБ(context).PadRight(3)).PadLeft(9);
            if (num10 == 1)
                context._1suidctls.Add(ent);
            else
                context.Update(ent);

            return ent.Maxid;
        }
        public static string GenerateIdDoc(StinDbContext context)
        {
            string num36 = context._1sjourns.Max(e => e.Iddoc);
            if (!string.IsNullOrEmpty(num36))
            {
                string suffics = num36.Substring(6);
                num36 = num36.Substring(0, 6).Trim();
                long num10 = Decode36(num36) + 1;
                num36 = (Encode36(num10) + suffics).PadLeft(9);
            }
            return num36;
        }
        public static async Task<byte[]> CreateZip(IFormFile file)
        {
            byte[] result;

            using (var packageStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Create, true))
                {
                    string filename = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    var zipFile = archive.CreateEntry(filename, CompressionLevel.Fastest);
                    using (var zipEntryStream = zipFile.Open())
                    {
                        await file.CopyToAsync(zipEntryStream);
                    }
                }
                result = packageStream.ToArray();
            }
            return result;
        }
        public static async Task<byte[]> CreateZip(byte[] bytes, string fileName)
        {
            byte[] result;

            using (var packageStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Create, true))
                {
                    var zipFile = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                    using (var zipEntryStream = zipFile.Open())
                    {
                        await zipEntryStream.WriteAsync(bytes, 0, bytes.Length); 
                    }
                }
                result = packageStream.ToArray();
            }
            return result;
        }
        public static async Task<byte[]> CreateZip(List<IFormFile> files)
        {
            byte[] result;

            using (var packageStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Create, true))
                {
                    foreach (var virtualFile in files)
                    {
                        string filename = ContentDispositionHeaderValue.Parse(virtualFile.ContentDisposition).FileName.Trim('"');
                        var zipFile = archive.CreateEntry(filename, CompressionLevel.Fastest);
                        using (var zipEntryStream = zipFile.Open())
                        {
                            await virtualFile.CopyToAsync(zipEntryStream);
                        }
                    }
                }
                result = packageStream.ToArray();
            }
            return result;
        }
        public static async Task<Dictionary<string,byte[]>> UnZip(byte[] zippedBuffer)
        {
            Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            using (var zippedStream = new MemoryStream(zippedBuffer))
            {
                using (var archive = new ZipArchive(zippedStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        using (var unzippedEntryStream = entry.Open())
                        {
                            using (var ms = new MemoryStream())
                            {
                                await unzippedEntryStream.CopyToAsync(ms);
                                files.Add(entry.Name, ms.ToArray());
                            }
                        }

                    }
                }
            }
            return files;
        }
        public static string LockDocNo(StinDbContext context, string ИдентификаторДокDds, int ДлинаНомера=10, string FirmaId=null, string Год=null)
        {
            string docNo = "";
            if (string.IsNullOrEmpty(FirmaId))
                FirmaId = FirmaPartner;
            if (string.IsNullOrEmpty(Год))
                Год = DateTime.Now.ToString("yyyy");

            var ФирмаПрефикс = (from sc131 in context.Sc131s
                                join sc4014 in context.Sc4014s on sc131.Id equals sc4014.Sp4011
                                where sc4014.Id == FirmaId
                                select sc131.Sp145.Trim()).FirstOrDefault();
            string prefix = ПрефиксИБ(context) + ФирмаПрефикс;

            string dnPrefixJ = ИдентификаторДокDds.ToString().PadLeft(10) + DateTime.Now.ToString("yyyy").PadRight(8);
            using (var tran = context.Database.BeginTransaction())
            {
                try
                {
                    var j_docNo = (from _j in context._1sjourns
                             where _j.Dnprefix == dnPrefixJ && string.Compare(_j.Docno, prefix) >= 0 && _j.Docno.Substring(0, prefix.Length) == prefix
                             orderby _j.Dnprefix descending, _j.Docno descending
                             select _j.Docno).FirstOrDefault();
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
                        needNextNo = context._1sdnlocks.Any(y => y.Dnprefix == dnPrefix && y.Docno == docNo);
                    }
                    _1sdnlock lock_table = new _1sdnlock
                    {
                        Dnprefix = dnPrefix,
                        Docno = docNo
                    };
                    context._1sdnlocks.Add(lock_table);
                    context.SaveChanges();
                    tran.Commit();
                }
                catch
                {
                    docNo = "";
                    tran.Rollback();
                }
            }
            return docNo;
        }
        public static void UnLockDocNo(StinDbContext context, string ИдентификаторДокDds, string DocNo, string Год=null)
        {
            if (string.IsNullOrEmpty(Год))
                Год = DateTime.Now.ToString("yyyy");
            string dnPrefix = ИдентификаторДокDds.ToString().PadLeft(10) + Год.PadRight(18);
            var row = (from lock_table in context._1sdnlocks
                       where lock_table.Dnprefix == dnPrefix && lock_table.Docno == DocNo
                       select lock_table).FirstOrDefault();
            if (row != null)
            {
                context._1sdnlocks.Remove(row);
                context.SaveChanges();
            }
        }
        public static _1sconst ИзменитьПериодическиеРеквизиты(this StinDbContext context, string objId, int реквизитDds, 
            string docId, DateTime dateTime, string value, int actNo, short lineNo = 0)
        {
            return new _1sconst
            {
                Objid = objId,//для транспортных маршрутов это Id элемента справочника ТранспортныеМаршруты
                Id = реквизитDds,
                Date = dateTime.Date,
                Time = (dateTime.Hour * 3600 * 10000) + (dateTime.Minute * 60 * 10000) + (dateTime.Second * 10000),
                Docid = docId,
                Value = value,//маршрут "э7777" или " 1W9 1TRBVS  " для документа ЗаявкаПокупателя с docId = " 1TRBVS  "
                Actno = actNo,//номер движения документа
                Lineno = lineNo,//Номер строки документа (заполняется при вызове метода ПривязыватьСтроку(), если привязка не выполнена или непериодическое значение - заполняется нулем
                Tvalue = "", //Заполняется только для неопределенных реквизитов, для типов данных 1С (когда длина ID равна 23 символам)
            };
        }
        public static Справочники.Маршрут СоздатьЭлементМаршрута(this StinDbContext context)
        {
            var code = context.Sc11555s.Where(x => x.Code.StartsWith(Common.ПрефиксИБ(context))).Max(x => x.Code);
            if (code == null)
                code = "0";
            else
                code = code.Substring(Common.ПрефиксИБ(context).Length);
            int next_code = 0;
            if (Int32.TryParse(code, out next_code))
            {
                next_code += 1;
            }
            code = Common.ПрефиксИБ(context) + next_code.ToString().PadLeft(10 - Common.ПрефиксИБ(context).Length, '0');
            Sc11555 ТранспортныеМаршруты = new Sc11555
            {
                Id = GenerateId(context, 11555),
                Code = code,
                Ismark = false,
                Verstamp = 0,
                Sp11669 = ПустоеЗначение,
                Sp11670 = min1cDate
            };
            context.Sc11555s.Add(ТранспортныеМаршруты);
            context.SaveChanges();
            context.РегистрацияИзмененийРаспределеннойИБ(11555, ТранспортныеМаршруты.Id);
            return new Справочники.Маршрут { Id = ТранспортныеМаршруты.Id, Code = ТранспортныеМаршруты.Code };
        }
        public static string ПолучитьТипРемонта(int intStatus)
        {
            return Гарантия.Select((s, i) => new { i, s }).Where(x => x.i == intStatus).Select(x => x.s).FirstOrDefault();
        }
        public static void ОтправитьSms(string Phone, int ВидДок_dds, string Изделие, string КвитанцияId)
        {
            string message = КвитанцияId + ". ";
            switch (ВидДок_dds)
            {
                case 9899: //Прием в ремонт
                    message += "Изделие \"" + Изделие + "\" принято на проверку качества. ";
                    break;
                case 11037: //Результат диагностики
                    break;

            }
            message += "СЦ СТИН-Сервис, Не отвечайте на данное сообщение. Тел. контакта " + Startup.sConfiguration["Settings:ServicePhones"];

            string path = Startup.sConfiguration["Settings:SmsFolder"].TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar + "7" + Phone + ".txt";
            
            using (StreamWriter sw = new StreamWriter(path, true, Encoding.GetEncoding(1251)))
            {
                sw.WriteLine(Regex.Replace(message, @"\t|\n|\r", ""));
            }
        }
        public static string JournalDateTime(this DateTime dateTime)
        {
            if (dateTime <= min1cDate)
                dateTime = DateTime.Now;
            var h = dateTime.Hour;
            var m = dateTime.Minute;
            var s = dateTime.Second;
            var ms = dateTime.Millisecond;
            var time = (h * 3600 * 10000) + (m * 60 * 10000) + (s * 10000) + (ms * 10);
            var timestr = Encode36(time).PadLeft(6);
            return dateTime.ToString("yyyyMMdd") + timestr;

        }
        public static _1sjourn GetEntityJourn(StinDbContext context, byte Проведен, int КоличествоДвижений, int ЖурналИД_md, int ВидДокИД_dds, string Нумератор, string ВидДок, string НомерДок, DateTime dateTime, 
            string ФирмаИД,
            string ПользовательИД,
            string СкладНазвание,
            string КонтрагентНазвание)
        {
            string num36 = GenerateIdDoc(context);
            if (dateTime <= min1cDate)
                dateTime = DateTime.Now;
            //var datestr = dateTime.ToString("yyyyMMdd");
            //var h = dateTime.Hour;
            //var m = dateTime.Minute;
            //var s = dateTime.Second;
            //var ms = dateTime.Millisecond;
            //var time = (h * 3600 * 10000) + (m * 60 * 10000) + (s * 10000) + (ms * 10);
            //var timestr = Encode36(time).PadLeft(6);
            var dateTimeIddoc = dateTime.JournalDateTime() + num36;
            if (string.IsNullOrEmpty(Нумератор))
                Нумератор = ВидДокИД_dds.ToString();
            var dnPrefix = Нумератор.PadLeft(10) + dateTime.ToString("yyyy").PadRight(8);
            var ФирмаЮрЛицо = (from sc131 in context.Sc131s
                               join sc4014 in context.Sc4014s on sc131.Id equals sc4014.Sp4011
                               where sc4014.Id == ФирмаИД
                               select new
                               {
                                   Фирма = sc4014.Descr.Trim(),
                                   ЮрЛицоId = sc131.Id,
                                   ЮрЛицоПрефикс = sc131.Sp145.Trim()
                               }).FirstOrDefault();
            string prefixDB = context.ПрефиксИБ(ПользовательИД);
            string prefix = prefixDB + ФирмаЮрЛицо.ЮрЛицоПрефикс;
            if (string.IsNullOrEmpty(НомерДок))
            {
                НомерДок = (from _j in context._1sjourns
                            where _j.Dnprefix == dnPrefix && string.Compare(_j.Docno, prefix) >= 0 && _j.Docno.Substring(0, prefix.Length) == prefix
                            orderby _j.Dnprefix descending, _j.Docno descending
                            select _j.Docno).FirstOrDefault();
                if (НомерДок == null)
                    НомерДок = "0";
                else
                    НомерДок = НомерДок.Substring(prefix.Length);

                string dnPrefixLockTable = Нумератор.PadLeft(10) + dateTime.ToString("yyyy").PadRight(18);
                string lockNumber = (from lock_table in context._1sdnlocks
                                     where lock_table.Dnprefix == dnPrefix && string.Compare(lock_table.Docno, prefix) >= 0 && lock_table.Docno.Substring(0, prefix.Length) == prefix
                                     orderby lock_table.Docno descending
                                     select lock_table.Docno).FirstOrDefault();
                if (lockNumber != null)
                {
                    lockNumber = lockNumber.Substring(prefix.Length);
                    if (Convert.ToInt32(lockNumber) > Convert.ToInt32(НомерДок))
                        НомерДок = lockNumber;
                }
                НомерДок = prefix + (Convert.ToInt32(НомерДок) + 1).ToString().PadLeft(10 - prefix.Length, '0');
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
                Sp798 = ПустоеЗначение, //Проект 
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
        public static async Task РегистрацияИзмененийРаспределеннойИБAsync(this StinDbContext context, int ВидДокИД_dds, string IdDoc)
        {
            var signs = (from dbset in context._1sdbsets
                         where dbset.Dbsign.Trim() != ПрефиксИБ(context)
                         select dbset.Dbsign).ToList();
            foreach (string sign in signs)
            {
                await context.Database.ExecuteSqlRawAsync("exec _1sp_RegisterUpdate @sign,@doc_dds,@num36,' '", new SqlParameter("@sign", sign), new SqlParameter("@doc_dds", ВидДокИД_dds), new SqlParameter("@num36", IdDoc));
            }
            await context.SaveChangesAsync();
        }
        public static void РегистрацияИзмененийРаспределеннойИБ(this StinDbContext context, int ВидДокИД_dds, string IdDoc)
        {
            var signs = (from dbset in context._1sdbsets
                         where dbset.Dbsign.Trim() != ПрефиксИБ(context)
                         select dbset.Dbsign).ToList();
            foreach (string sign in signs)
                context.Database.ExecuteSqlRaw("exec _1sp_RegisterUpdate @sign,@doc_dds,@num36,' '", new SqlParameter("@sign", sign), new SqlParameter("@doc_dds", ВидДокИД_dds), new SqlParameter("@num36", IdDoc));
            context.SaveChanges();
        }
        public static async Task ОбновитьПодчиненныеДокументы(StinDbContext context, string ДокОснованиеId13, string DateTimeIddoc, string Iddoc)
        {
            SqlParameter paramParentVal = new SqlParameter("@parentVal", "O1" + ДокОснованиеId13);
            SqlParameter paramDateTimeIddoc = new SqlParameter("@date_time_iddoc", DateTimeIddoc);
            SqlParameter paramNum36 = new SqlParameter("@num36", Iddoc);
            await context.Database.ExecuteSqlRawAsync("exec _1sp__1SCRDOC_Write 0,@parentVal,@date_time_iddoc,@num36,1", paramParentVal, paramDateTimeIddoc, paramNum36);
            await context.SaveChangesAsync();
        }
        public static async Task ОбновитьГрафыОтбора(StinDbContext context, int MdId, string СправочникId13, string DateTimeIddoc, string Iddoc)
        {
            SqlParameter paramMdId = new SqlParameter("@mdId", MdId); //4747 графа для Склад, 862 для Контрагент
            SqlParameter paramParentVal = new SqlParameter("@parentVal", "B1" + СправочникId13);
            SqlParameter paramDateTimeIddoc = new SqlParameter("@date_time_iddoc", DateTimeIddoc);
            SqlParameter paramNum36 = new SqlParameter("@num36", Iddoc);
            await context.Database.ExecuteSqlRawAsync("exec _1sp__1SCRDOC_Write @mdId,@parentVal,@date_time_iddoc,@num36,1", paramMdId, paramParentVal, paramDateTimeIddoc, paramNum36);
            await context.SaveChangesAsync();
        }
        public static async Task ОбновитьВремяТА(StinDbContext context, string Iddoc, string DateTimeIddoc)
        {
            DateTime docDate = DateTime.ParseExact(DateTimeIddoc.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture);
            int docTime = (int)Decode36(DateTimeIddoc.Substring(8, 6));
            var dbValues = (from s in context._1ssystems select new { d = s.Curdate, t = s.Curtime }).FirstOrDefault();
            bool NeedToUpdate = false;
            if (dbValues.d < docDate)
                NeedToUpdate = true;
            else if (dbValues.d == docDate)
                NeedToUpdate = dbValues.t < docTime;
            if (NeedToUpdate)
            {
                await context.Database.ExecuteSqlRawAsync("Update _1SSYSTEM set CURDATE=@P1, CURTIME=@P2, EVENTIDTA=@P3",
                    new SqlParameter("@P1", docDate.ToShortDateString()),
                    new SqlParameter("@P2", docTime),
                    new SqlParameter("@P3", Iddoc));

                await context.SaveChangesAsync();
            }
        }
        public static async Task ОбновитьПоследовательность(StinDbContext context, string DateTimeIddoc)
        {
            _1sstream Последовательность = (from p in context._1sstreams
                                            where p.Id == 1946
                                            select p).FirstOrDefault();
            Последовательность.DateTimeDocid = DateTimeIddoc;
            context.Update(Последовательность);
            await context.SaveChangesAsync();
        }
        public static async Task ОбновитьСетевуюАктивность(this StinDbContext context)
        {
            int СчетчикАктивности = await context._1susers.Select(x => x.Netchgcn).FirstOrDefaultAsync();
            СчетчикАктивности++;
            await context.Database.ExecuteSqlRawAsync("Update _1SUSERS set NETCHGCN=@P1",
                new SqlParameter("@P1", СчетчикАктивности));
            await context.SaveChangesAsync();
        }
    }
    public class ВидДокумента
    {
        public int Вид10 { get; set; }
        public string Вид36 { get; set; }
        public string Идентификатор { get; set; }
        public string Представление { get; set; }
    }
}
