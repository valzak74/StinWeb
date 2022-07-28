using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinWeb.Models.Repository.Интерфейсы.Регистры;
using StinWeb.Models.Repository.Регистры;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Документы
{
    public class ПриемВРемонтRepository: ДокументМастерскойRepository, IПриемВРемонт
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IРегистр_ОстаткиДоставки _регистр_ОстаткиДоставки;
        private IРегистр_СтопЛистЗЧ _регистр_СтопЛистЗЧ;

        public ПриемВРемонтRepository(StinDbContext context, IServiceScopeFactory serviceScopeFactory) : base(context)
        {
            _регистр_ОстаткиДоставки = new Регистр_ОстаткиДоставки(context);
            _регистр_СтопЛистЗЧ = new Регистр_СтопЛистЗЧ(context);
            _serviceScopeFactory = serviceScopeFactory;
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _регистр_ОстаткиДоставки.Dispose();
                    _регистр_СтопЛистЗЧ.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаПриемВРемонт> НовыйAsync(int UserRowId, string Параметр = null)
        {
            ФормаПриемВРемонт doc = new ФормаПриемВРемонт(ТипыФормы.Новый);
            var Пользователь = await userRepository.GetUserByRowIdAsync(UserRowId);
            if (!string.IsNullOrEmpty(Пользователь.ОсновнаяФирма.Id) && string.IsNullOrEmpty(Пользователь.ОсновнаяФирма.Наименование))
                Пользователь.ОсновнаяФирма = await фирмаRepository.GetEntityByIdAsync(Пользователь.ОсновнаяФирма.Id);
            if (NeedToOpenPeriod())
            {
                doc.Ошибка = new ExceptionData { Description = "Период не открыт!" };
            }
            else
            {
                doc.Общие.Автор = await userRepository.GetUserByRowIdAsync(UserRowId);
                doc.Общие.ВидДокумента10 = 9899;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.НазваниеВЖурнале = "{0} (платный)";
                doc.Общие.Фирма = Пользователь.ОсновнаяФирма;
                doc.Общие.ДатаДок = DateTime.Now;
                doc.ДатаКвитанции = doc.Общие.ДатаДок.Year;
                doc.ДатаПриема = doc.Общие.ДатаДок.Date;
                doc.НомерРемонта = 1;
                doc.СтатусПартииId = "";
                if (!string.IsNullOrEmpty(Параметр) && Параметр == "ПРЕТЕНЗИЯ")
                {
                    doc.Претензия = true;
                    doc.Гарантия = 2;
                }
            }
            if (doc.Ошибка == null || doc.Ошибка.Skip)
            {
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, "9899", 10, doc.Претензия ? Common.FirmaInst : doc.Общие.Фирма.Id);
                doc.НомерКвитанции = doc.Общие.НомерДок;
                if (doc.НомерКвитанции.Length != 7)
                {
                    Regex re = new Regex(@"([a-zA-Z]+)(\d+)");
                    Match result = re.Match(doc.НомерКвитанции);

                    string alphaPart = result.Groups[1].Value;
                    int numberPart = Convert.ToInt32(result.Groups[2].Value);
                    doc.НомерКвитанции = alphaPart + numberPart.ToString().PadLeft(7 - alphaPart.Length, '0');
                }
            }
            return doc;
        }
        public async Task<ФормаПриемВРемонт> ВводНаОснованииAsync(string докОснованиеId, int видДокОснование, 
            int userRowId)
        {
            ФормаПриемВРемонт doc = new ФормаПриемВРемонт(ТипыФормы.НаОсновании);
            if (NeedToOpenPeriod())
            {
                doc.Ошибка = new ExceptionData { Description = "Период не открыт!" };
            }
            else
            {
                doc.Общие.ДокОснование = await ДокОснованиеAsync(докОснованиеId);
                doc.Общие.Автор = await userRepository.GetUserByRowIdAsync(userRowId);
                doc.Общие.ВидДокумента10 = 9899;
                doc.Общие.ВидДокумента36 = Common.Encode36(doc.Общие.ВидДокумента10);
                doc.Общие.Фирма = doc.Общие.ДокОснование.Фирма; 
                                                                    
                doc.Общие.ДатаДок = DateTime.Now;

                if (видДокОснование == 9899) //Прием в ремонт
                {
                    var ДокОснование = await ПриемВРемонтAsync(докОснованиеId);
                    var ИнфоИзделия = await АктивироватьИзделиеAsync(ДокОснование.НомерКвитанции, ДокОснование.ДатаКвитанции, doc.Общие.Автор.Id, null);
                    var РазрешенныйСтатусДляИзменения = Common.СтатусПартии
                        .Where(x => x.Value == "Принят в ремонт" ||
                                    x.Value == "Сортировка" ||
                                    x.Value == "Претензия на рассмотрении" ||
                                    x.Value == "Претензия отклонена" ||
                                    x.Value == "Замена по претензии" ||
                                    x.Value == "Возврат денег по претензии" ||
                                    x.Value == "Восстановление по претензии" ||
                                    x.Value == "Доукомплектация по претензии" ||
                                    x.Value == "Диагностика претензии"
                                    );
                    if (РазрешенныйСтатусДляИзменения.Any(x => x.Key == ИнфоИзделия.СтатусПартииId))
                    {
                        doc.Общие.Комментарий = ИнфоИзделия.Комментарий;
                        doc.НомерКвитанции = ИнфоИзделия.НомерКвитанции;
                        doc.ДатаКвитанции = ИнфоИзделия.ДатаКвитанции;
                        doc.Претензия = ИнфоИзделия.Претензия;
                        doc.Изделие = ИнфоИзделия.Изделие;
                        doc.ЗаводскойНомер = ИнфоИзделия.ЗаводскойНомер;
                        doc.Гарантия = ИнфоИзделия.Гарантия;
                        doc.ДатаПродажи = ИнфоИзделия.ДатаПродажи;
                        doc.ДатаПриема = ИнфоИзделия.ДатаПриема;
                        doc.ДатаОбращения = ИнфоИзделия.ДатаОбращения;
                        doc.НомерРемонта = ИнфоИзделия.НомерРемонта;
                        doc.Комплектность = ИнфоИзделия.Комплектность;
                        doc.Неисправность = ИнфоИзделия.Неисправность;
                        doc.Неисправность2 = ИнфоИзделия.Неисправность2;
                        doc.Неисправность3 = ИнфоИзделия.Неисправность3;
                        doc.Неисправность4 = ИнфоИзделия.Неисправность4;
                        doc.Неисправность5 = ИнфоИзделия.Неисправность5;
                        doc.Заказчик = ИнфоИзделия.Заказчик;
                        doc.Телефон = ИнфоИзделия.Телефон;
                        doc.Email = ИнфоИзделия.Email;
                        doc.Склад = ИнфоИзделия.Склад;
                        doc.ПодСклад = ИнфоИзделия.ПодСклад;
                        doc.Мастер = ИнфоИзделия.Мастер;
                        doc.СкладОткуда = ИнфоИзделия.СкладОткуда;
                        doc.СтатусПартииId = ИнфоИзделия.СтатусПартииId;
                        doc.СпособВозвращенияId = ИнфоИзделия.СпособВозвращенияId;
                        doc.ВнешнийВид = ИнфоИзделия.ВнешнийВид;
                        doc.ПриложенныйДокумент1 = ИнфоИзделия.ПриложенныйДокумент1;
                        doc.ПриложенныйДокумент2 = ИнфоИзделия.ПриложенныйДокумент2;
                        doc.ПриложенныйДокумент3 = ИнфоИзделия.ПриложенныйДокумент3;
                        doc.ПриложенныйДокумент4 = ИнфоИзделия.ПриложенныйДокумент4;
                        doc.ПриложенныйДокумент5 = ИнфоИзделия.ПриложенныйДокумент5;
                        doc.Photos = ИнфоИзделия.ДокПрием.Photos;
                    }
                    else
                    {
                        doc.Ошибка = new ExceptionData { Description = "Нельзя корректировать для статуса " + ИнфоИзделия.СтатусПартии + "!" };
                    }
                }
                else if (видДокОснование == 10080) //ПеремещениеИзделий
                {
                    var ДокОснование = await ПеремещениеИзделийAsync(докОснованиеId);
                    doc.Общие.Комментарий = ДокОснование.Общие.Комментарий.Trim();
                    doc.НомерКвитанции = ДокОснование.НомерКвитанции;
                    doc.ДатаКвитанции = ДокОснование.ДатаКвитанции;
                    doc.Изделие = ДокОснование.Изделие;
                    doc.ЗаводскойНомер = ДокОснование.ЗаводскойНомер.Trim();
                    doc.Гарантия = ДокОснование.Гарантия;
                    doc.НомерРемонта = ДокОснование.НомерРемонта;
                    doc.ДатаПродажи = ДокОснование.ДатаПродажи == Common.min1cDate ? DateTime.MinValue : ДокОснование.ДатаПродажи;
                    doc.ДатаПриема = ДокОснование.ДатаПриема == Common.min1cDate ? DateTime.MinValue : ДокОснование.ДатаПриема;
                    doc.Неисправность = ДокОснование.Неисправность == null ? new Неисправность() : ДокОснование.Неисправность;
                    doc.Неисправность2 = ДокОснование.Неисправность2 == null ? new Неисправность() : ДокОснование.Неисправность2;
                    doc.Неисправность3 = ДокОснование.Неисправность3 == null ? new Неисправность() : ДокОснование.Неисправность3;
                    doc.Неисправность4 = ДокОснование.Неисправность4 == null ? new Неисправность() : ДокОснование.Неисправность4;
                    doc.Неисправность5 = ДокОснование.Неисправность5 == null ? new Неисправность() : ДокОснование.Неисправность5;
                    doc.Заказчик = ДокОснование.Заказчик;
                    doc.Склад = ДокОснование.СкладПолучатель;
                    doc.Мастер = ДокОснование.Мастер;
                    doc.СкладОткуда = ДокОснование.СкладОткуда;

                    string ТекущийСтатус = ДокОснование.СтатусПартииId;
                    string ПринятВРемонтСтатус = Common.СтатусПартии.FirstOrDefault(x => x.Value == "Принят в ремонт").Key;
                    List<string> СтатусыПредПриемки = new List<string>()
                    {
                        { Common.СтатусПартии.FirstOrDefault(x => x.Value == "Сортировка").Key },
                        { Common.СтатусПартии.FirstOrDefault(x => x.Value == "Восстановление по претензии").Key },
                        { Common.СтатусПартии.FirstOrDefault(x => x.Value == "Диагностика претензии").Key },
                    };
                    doc.СтатусПартииId = СтатусыПредПриемки.Contains(ТекущийСтатус) ? ПринятВРемонтСтатус : ДокОснование.СтатусПартииId;
                    var ЖурналДоставки = await складRepository.СформироватьОстаткиДоставки(Common.min1cDate, doc.Общие.ДокОснование.Фирма.Id, doc.Склад.Id, РежимВыбора.Общий, doc.Общие.ДокОснование.Значение, doc.Изделие.Id)
                        .FirstOrDefaultAsync();
                    var ОстатокДоставки = ЖурналДоставки != null ? ЖурналДоставки.Остаток : 0;
                    if (ОстатокДоставки <= 0)
                    {
                        doc.Ошибка = new ExceptionData { Description = "Изделие уже получено!" };
                    }
                    else
                    {
                        //поиск данных в НомерПриемаВРемонт
                        var ПриемId = await регистр_номерПриемаВРемонт.ПолучитьДокументIdAsync(DateTime.MinValue, null, false, doc.НомерКвитанции, doc.ДатаКвитанции);
                        if (!string.IsNullOrEmpty(ПриемId))
                        {
                            var Прием = await ПриемВРемонтAsync(ПриемId);
                            doc.ДатаОбращения = Прием.ДатаОбращения == Common.min1cDate ? DateTime.MinValue : Прием.ДатаОбращения;
                            doc.Комплектность = Прием.Комплектность;
                            doc.Телефон = Прием.Телефон;
                            doc.Email = Прием.Email;
                            doc.ПриложенныйДокумент1 = Прием.ПриложенныйДокумент1;
                            doc.ПриложенныйДокумент2 = Прием.ПриложенныйДокумент2;
                            doc.ПриложенныйДокумент3 = Прием.ПриложенныйДокумент3;
                            doc.ПриложенныйДокумент4 = Прием.ПриложенныйДокумент4;
                            doc.ПриложенныйДокумент5 = Прием.ПриложенныйДокумент5;
                            doc.ВнешнийВид = Прием.ВнешнийВид;
                            doc.СпособВозвращенияId = Прием.СпособВозвращенияId;
                            doc.Претензия = Прием.Претензия;
                            doc.Photos = Прием.Photos;
                        }
                    }
                }
                var Т_Сломан = Common.СтатусПартии.FirstOrDefault(x => x.Value == "Принят в ремонт").Key;
                doc.Общие.НазваниеВЖурнале = doc.Общие.ДокОснование.ВидДокумента10 == doc.Общие.ВидДокумента10 ? "Корректировка квитанции" :
                    !(doc.Общие.ДокОснование.ВидДокумента10 == 10080 && doc.СтатусПартииId == Т_Сломан) ? "Прием из доставки" :
                            (doc.СтатусПартииId == Common.ПустоеЗначение || doc.СтатусПартииId == Т_Сломан) ? (
                                doc.Гарантия == 1 ? "{0} (по гарантии)" :
                                doc.Гарантия == 2 ? "{0} (предпродажный)" :
                                doc.Гарантия == 3 ? "{0} (за свой счет)" :
                                doc.Гарантия == 4 ? "{0} (на экспертизу)" :
                                "{0} (платный)"
                                ) :
                            "Прием на сортировку";
            }
            if (doc.Ошибка == null || doc.Ошибка.Skip)
                doc.Общие.НомерДок = await LockDocNoAsync(doc.Общие.Автор.Id, "9899", 10, doc.Общие.Фирма.Id);
            return doc;
        }
        public async Task<ФормаПриемВРемонт> ПросмотрAsync(string idDoc)
        {
            return await ПриемВРемонтAsync(idDoc);
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаПриемВРемонт doc)
        {
            doc.Склад = складRepository.GetEntityById(doc.Склад.Id);
            doc.Заказчик = контрагентRepository.GetEntityById(doc.Заказчик.Id);
            if (string.IsNullOrEmpty(doc.СтатусПартии))
                doc.СтатусПартииId = Common.СтатусПартии.Where(x => x.Value == "Принят в ремонт").Select(x => x.Key).FirstOrDefault();
            if (doc.Склад.Id != складRepository.КонстантаСкладДляРемонта().Id)
                doc.Мастер.Id = null;
            decimal ГарантийныеДокументы = 0;
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент1.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент1.Наименование))
            {
                doc.ПриложенныйДокумент1 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент1.Id);
                ГарантийныеДокументы += doc.ПриложенныйДокумент1.ФлагГарантии;
            }
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент2.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент2.Наименование))
            {
                doc.ПриложенныйДокумент2 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент2.Id);
                ГарантийныеДокументы += doc.ПриложенныйДокумент2.ФлагГарантии;
            }
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент3.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент3.Наименование))
            {
                doc.ПриложенныйДокумент3 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент3.Id);
                ГарантийныеДокументы += doc.ПриложенныйДокумент3.ФлагГарантии;
            }
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент4.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент4.Наименование))
            {
                doc.ПриложенныйДокумент4 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент4.Id);
                ГарантийныеДокументы += doc.ПриложенныйДокумент4.ФлагГарантии;
            }
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент5.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент5.Наименование))
            {
                doc.ПриложенныйДокумент5 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент5.Id);
                ГарантийныеДокументы += doc.ПриложенныйДокумент5.ФлагГарантии;
            }
            try
            {
                Common.UnLockDocNo(_context, doc.Общие.ВидДокумента10.ToString(), doc.Общие.НомерДок);
                _1sjourn j = Common.GetEntityJourn(_context, 0, 0, 10528, doc.Общие.ВидДокумента10, null, doc.Общие.Наименование,
                    doc.Общие.НомерДок, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    doc.Заказчик.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                Dh9899 docHeader = new Dh9899
                {
                    Iddoc = j.Iddoc,
                    Sp9889 = doc.Заказчик.Id,
                    Sp9890 = doc.Изделие.Id,
                    Sp9891 = doc.ЗаводскойНомер != null ? doc.ЗаводскойНомер : "",
                    Sp9892 = doc.Неисправность.Id == null ? Common.ПустоеЗначение : doc.Неисправность.Id,
                    Sp9893 = doc.Претензия ? doc.Гарантия : (ГарантийныеДокументы > 0 ? doc.Гарантия : 0),
                    Sp9894 = doc.Мастер.Id == null ? Common.ПустоеЗначение : doc.Мастер.Id,
                    Sp9896 = doc.ДатаПродажи == DateTime.MinValue ? Common.min1cDate : doc.ДатаПродажи,
                    Sp9897 = doc.НомерРемонта,
                    Sp10012 = doc.Склад.Id,
                    Sp10013 = doc.ПодСклад.Id,
                    Sp10014 = (doc.Общие.ДокОснование != null && !string.IsNullOrWhiteSpace(doc.Общие.ДокОснование.Значение)) ? doc.Общие.ДокОснование.Значение : Common.ПустоеЗначениеИд13,
                    Sp10108 = doc.НомерКвитанции,
                    Sp10109 = doc.ДатаКвитанции,
                    Sp10110 = doc.СкладОткуда != null ? doc.СкладОткуда.Id : Common.ПустоеЗначение,
                    Sp10111 = doc.СтатусПартииId,
                    Sp10112 = doc.ДатаПриема,
                    Sp10113 = "",
                    Sp10553 = 0,
                    Sp10712 = doc.Неисправность2.Id == null ? Common.ПустоеЗначение : doc.Неисправность2.Id,
                    Sp10713 = doc.Неисправность3.Id == null ? Common.ПустоеЗначение : doc.Неисправность3.Id,
                    Sp10714 = doc.Неисправность4.Id == null ? Common.ПустоеЗначение : doc.Неисправность4.Id,
                    Sp10715 = doc.Неисправность5.Id == null ? Common.ПустоеЗначение : doc.Неисправность5.Id,
                    Sp10716 = Common.ПустоеЗначение,
                    Sp10717 = Common.ПустоеЗначение,
                    Sp10795 = !string.IsNullOrEmpty(doc.Комплектность) ? doc.Комплектность : "",
                    Sp660 = doc.Общие.Комментарий != null ? doc.Общие.Комментарий : "",
                    Sp12394 = doc.Телефон.Id == null ? Common.ПустоеЗначение : doc.Телефон.Id,
                    Sp13651 = doc.Email.Id == null ? Common.ПустоеЗначение : doc.Email.Id,
                    Sp13710 = doc.ДатаОбращения == DateTime.MinValue ? Common.min1cDate : doc.ДатаОбращения,
                    Sp13751 = doc.СпособВозвращенияId,
                    Sp13752 = doc.ПриложенныйДокумент1.Id == null ? Common.ПустоеЗначение : doc.ПриложенныйДокумент1.Id,
                    Sp13753 = doc.ПриложенныйДокумент2.Id == null ? Common.ПустоеЗначение : doc.ПриложенныйДокумент2.Id,
                    Sp13754 = doc.ПриложенныйДокумент3.Id == null ? Common.ПустоеЗначение : doc.ПриложенныйДокумент3.Id,
                    Sp13755 = doc.ПриложенныйДокумент4.Id == null ? Common.ПустоеЗначение : doc.ПриложенныйДокумент4.Id,
                    Sp13756 = doc.ПриложенныйДокумент5.Id == null ? Common.ПустоеЗначение : doc.ПриложенныйДокумент5.Id,
                    Sp13757 = doc.ВнешнийВид,
                    Sp13771 = doc.Претензия ? 1 : 0,
                };
                await _context.Dh9899s.AddAsync(docHeader);

                await _context.SaveChangesAsync();
                await Common.РегистрацияИзмененийРаспределеннойИБAsync(_context, doc.Общие.ВидДокумента10, j.Iddoc);
                if (doc.Общие.ДокОснование != null)
                    await Common.ОбновитьПодчиненныеДокументы(_context, doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = db_ex.HResult, Description = db_ex.InnerException.ToString() };
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = ex.HResult, Description = ex.Message };
            }
            return null;
        }
        public async Task<ExceptionData> ПровестиAsync(ФормаПриемВРемонт doc)
        {
            try
            {
                _1sjourn j = await _context._1sjourns.FirstOrDefaultAsync(x => x.Iddoc == doc.Общие.IdDoc);
                if (j == null)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = "Не обнаружена запись журнала." };
                }
                DateTime startOfMonth = new DateTime(doc.Общие.ДатаДок.Year, doc.Общие.ДатаДок.Month, 1);
                int КоличествоДвижений = 0;
                bool Приход = false;

                var РегистрПартииМастерской_Остатки = await регистр_ПартииМастерской.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, doc.НомерКвитанции, doc.ДатаКвитанции);
                if (РегистрПартииМастерской_Остатки != null)
                    foreach (var r in РегистрПартииМастерской_Остатки)
                    {
                        if (r != null)
                        {
                            КоличествоДвижений++;
                            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA9972_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                "@Гарантия,@Изделие,@ЗавНомер,@СтатусПартии,@Заказчик,@СкладОткуда,@ДатаПриема," +
                                "@НомерКвитанции,@ДатаКвитанции,@Количество," +
                                "1,0,@docDate,@CurPeriod,1,0",
                                new SqlParameter("@num36", doc.Общие.IdDoc),
                                new SqlParameter("@ActNo", КоличествоДвижений),
                                new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                new SqlParameter("@Гарантия", r.Гарантия),
                                new SqlParameter("@Изделие", r.Номенклатура),
                                new SqlParameter("@ЗавНомер", r.ЗавНомер),
                                new SqlParameter("@СтатусПартии", r.СтатусПартии),
                                new SqlParameter("@Заказчик", r.Контрагент),
                                new SqlParameter("@СкладОткуда", r.СкладОткуда),
                                new SqlParameter("@ДатаПриема", r.ДатаПриема),
                                new SqlParameter("@НомерКвитанции", doc.НомерКвитанции),
                                new SqlParameter("@ДатаКвитанции", doc.ДатаКвитанции),
                                new SqlParameter("@Количество", r.Количество),
                                new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                                new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                        }
                    }
                Приход = true;
                КоличествоДвижений++;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA9972_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@Гарантия,@Изделие,@ЗавНомер,@СтатусПартии,@Заказчик,@СкладОткуда,@ДатаПриема," +
                    "@НомерКвитанции,@ДатаКвитанции,@Количество," +
                    "1,0,@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", doc.Общие.IdDoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@Гарантия", doc.Гарантия),
                    new SqlParameter("@Изделие", doc.Изделие.Id),
                    new SqlParameter("@ЗавНомер", string.IsNullOrEmpty(doc.ЗаводскойНомер) ? "" : doc.ЗаводскойНомер),
                    new SqlParameter("@СтатусПартии", doc.СтатусПартииId),
                    new SqlParameter("@Заказчик", doc.Заказчик.Id),
                    new SqlParameter("@СкладОткуда", doc.СкладОткуда != null ? doc.СкладОткуда.Id : Common.ПустоеЗначение),
                    new SqlParameter("@ДатаПриема", doc.ДатаПриема.ToShortDateString()),
                    new SqlParameter("@НомерКвитанции", doc.НомерКвитанции),
                    new SqlParameter("@ДатаКвитанции", doc.ДатаКвитанции),
                    new SqlParameter("@Количество", 1),
                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                var РегистрНомерПриемаРРемонт_Остатки = await регистр_номерПриемаВРемонт.ПолучитьОстаткиAsync(doc.Общие.ДатаДок, doc.Общие.IdDoc, false, doc.НомерКвитанции, doc.ДатаКвитанции);
                if (РегистрНомерПриемаРРемонт_Остатки != null)
                {
                    Приход = false;
                    foreach (var r in РегистрНомерПриемаРРемонт_Остатки)
                    {
                        if (r != null)
                        {
                            КоличествоДвижений++;
                            await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA10471_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                "@НомерКвитанции,@ДатаКвитанции,@ДокПоступления,@Претензия,@Количество," +
                                "@docDate,@CurPeriod,1,0",
                                new SqlParameter("@num36", doc.Общие.IdDoc),
                                new SqlParameter("@ActNo", КоличествоДвижений),
                                new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                new SqlParameter("@НомерКвитанции", doc.НомерКвитанции),
                                new SqlParameter("@ДатаКвитанции", doc.ДатаКвитанции),
                                new SqlParameter("@ДокПоступления", r.ПриемВРемонтId),
                                new SqlParameter("@Претензия", r.Претензия ? 1 : 0),
                                new SqlParameter("@Количество", r.Количество),
                                new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                                new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                        }
                    }
                }
                Приход = true;
                КоличествоДвижений++;
                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA10471_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                    "@НомерКвитанции,@ДатаКвитанции,@ДокПоступления,@Претензия,@Количество," +
                    "@docDate,@CurPeriod,1,0",
                    new SqlParameter("@num36", doc.Общие.IdDoc),
                    new SqlParameter("@ActNo", КоличествоДвижений),
                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                    new SqlParameter("@НомерКвитанции", doc.НомерКвитанции),
                    new SqlParameter("@ДатаКвитанции", doc.ДатаКвитанции),
                    new SqlParameter("@ДокПоступления", doc.Общие.IdDoc),
                    new SqlParameter("@Претензия", doc.Претензия ? 1 : 0),
                    new SqlParameter("@Количество", 1),
                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));

                bool NeedОстаткиИзделий = true;
                if (doc.Общие.ДокОснование != null && doc.Общие.ДокОснование.ВидДокумента10 == doc.Общие.ВидДокумента10)
                    NeedОстаткиИзделий = false;
                if (NeedОстаткиИзделий)
                {
                    Приход = true;
                    КоличествоДвижений++;
                    await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA11049_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                        "@НомерКвитанции,@ДатаКвитанции,@Склад,@ПодСклад,@Мастер,@Количество," +
                        "0,@docDate,@CurPeriod,1,0",
                        new SqlParameter("@num36", doc.Общие.IdDoc),
                        new SqlParameter("@ActNo", КоличествоДвижений),
                        new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                        new SqlParameter("@НомерКвитанции", doc.НомерКвитанции),
                        new SqlParameter("@ДатаКвитанции", doc.ДатаКвитанции),
                        new SqlParameter("@Склад", doc.Склад.Id),
                        new SqlParameter("@ПодСклад", doc.ПодСклад.Id),
                        new SqlParameter("@Мастер", string.IsNullOrEmpty(doc.Мастер.Id) ? Common.ПустоеЗначение : doc.Мастер.Id),
                        new SqlParameter("@Количество", 1),
                        new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                        new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                }

                if (doc.Общие.ДокОснование != null)
                {
                    var РегистрОстаткиДоставки_Остатки = await _регистр_ОстаткиДоставки.ПолучитьОстаткиAsync(
                        doc.Общие.ДатаДок,
                        doc.Общие.IdDoc,
                        false,
                        doc.Склад.Id,
                        doc.Общие.ДокОснование.Значение,
                        new List<string>() { doc.Изделие.Id }
                        );
                    if (РегистрОстаткиДоставки_Остатки != null)
                    {
                        Приход = false;
                        foreach (var r in РегистрОстаткиДоставки_Остатки)
                        {
                            if (r != null)
                            {
                                КоличествоДвижений++;
                                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA8696_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                    "@Фирма,@Номенклатура,@Склад,@ЦенаПрод,@ДокПеремещения,@ЭтоИзделие," +
                                    "@Количество," +
                                    "1,@docDate,@CurPeriod,1,0",
                                    new SqlParameter("@num36", doc.Общие.IdDoc),
                                    new SqlParameter("@ActNo", КоличествоДвижений),
                                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                    new SqlParameter("@Фирма", r.ФирмаId),
                                    new SqlParameter("@Номенклатура", r.НоменклатураId),
                                    new SqlParameter("@Склад", r.СкладId),
                                    new SqlParameter("@ЦенаПрод", r.ЦенаПрод),
                                    new SqlParameter("@ДокПеремещения", r.ДокПеремещенияId13),
                                    new SqlParameter("@ЭтоИзделие", r.ЭтоИзделие),
                                    new SqlParameter("@Количество", r.Количество),
                                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                                j.Rf8696 = true;
                            }
                        }
                    }
                    var РегистрСтопЛистЗЧ_Остатки = await _регистр_СтопЛистЗЧ.ВыбратьДвиженияДокумента(doc.Общие.ДокОснование.IdDoc);
                    if (РегистрСтопЛистЗЧ_Остатки != null)
                    {
                        foreach (var r in РегистрСтопЛистЗЧ_Остатки)
                        {
                            if (r != null)
                            {
                                Приход = r.Количество < 0;
                                КоличествоДвижений++;
                                await _context.Database.ExecuteSqlRawAsync("exec _1sp_RA11055_WriteDocAct @num36,0,@ActNo,@DebetCredit," +
                                    "@Номенклатура,@Склад,@НомерКвитанции,@ДатаКвитанции,@Гарантия,@ДокРезультат," +
                                    "@Количество," +
                                    "@docDate,@CurPeriod,1,0",
                                    new SqlParameter("@num36", doc.Общие.IdDoc),
                                    new SqlParameter("@ActNo", КоличествоДвижений),
                                    new SqlParameter("@DebetCredit", Приход ? 0 : 1),
                                    new SqlParameter("@Номенклатура", r.НоменклатураId),
                                    new SqlParameter("@Склад", doc.Склад.Id),
                                    new SqlParameter("@НомерКвитанции", r.НомерКвитанции),
                                    new SqlParameter("@ДатаКвитанции", r.ДатаКвитанции),
                                    new SqlParameter("@Гарантия", r.Гарантия),
                                    new SqlParameter("@ДокРезультат", r.ДокРезультатId),
                                    new SqlParameter("@Количество", (r.Количество < 0 ? -r.Количество : r.Количество)),
                                    new SqlParameter("@docDate", doc.Общие.ДатаДок.ToShortDateString()),
                                    new SqlParameter("@CurPeriod", startOfMonth.ToShortDateString()));
                                j.Rf11055 = true;
                            }
                        }
                    }
                }

                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Rf9972 = true;
                j.Rf10471 = true;
                j.Rf11049 = NeedОстаткиИзделий;

                _context.Update(j);
                await _context.SaveChangesAsync();

                await Common.ОбновитьВремяТА(_context, j.Iddoc, j.DateTimeIddoc);
                await Common.ОбновитьПоследовательность(_context, j.DateTimeIddoc);
                await _context.ОбновитьСетевуюАктивность();
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = db_ex.HResult, Description = db_ex.InnerException.ToString() };
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = ex.HResult, Description = ex.Message };
            }
            return null;
        }
        public async Task<ExceptionData> ЗаписатьПровестиAsync(ФормаПриемВРемонт doc)
        {
            ExceptionData result = await ЗаписатьAsync(doc);
            if (result == null)
                result = await ПровестиAsync(doc);
            return result;
        }
        public async Task<ПриемВРемонтПечать> ДанныеДляПечатиAsync(ФормаПриемВРемонт doc)
        {
            if (!string.IsNullOrEmpty(doc.Общие.Фирма.Id) && string.IsNullOrEmpty(doc.Общие.Фирма.Наименование))
                doc.Общие.Фирма = фирмаRepository.GetEntityById(doc.Общие.Фирма.Id);
            if (!string.IsNullOrEmpty(doc.Заказчик.Id) && string.IsNullOrEmpty(doc.Заказчик.Наименование))
                doc.Заказчик = await контрагентRepository.GetEntityByIdAsync(doc.Заказчик.Id);
            if (!string.IsNullOrEmpty(doc.Телефон.Id) && string.IsNullOrEmpty(doc.Телефон.Номер))
                doc.Телефон = await контрагентRepository.ТелефонByIdAsync(doc.Телефон.Id);
            if (!string.IsNullOrEmpty(doc.Email.Id) && string.IsNullOrEmpty(doc.Email.Адрес))
                doc.Email = await контрагентRepository.EmailByIdAsync(doc.Email.Id);
            if (!string.IsNullOrEmpty(doc.Мастер.Id) && string.IsNullOrEmpty(doc.Мастер.Наименование))
                doc.Мастер = await мастерскаяRepository.МастерByIdAsync(doc.Мастер.Id);
            if (!string.IsNullOrEmpty(doc.Изделие.Id) && string.IsNullOrEmpty(doc.Изделие.Наименование))
                doc.Изделие = await номенклатураRepository.GetНоменклатураByIdAsync(doc.Изделие.Id);
            if (!string.IsNullOrEmpty(doc.Склад.Id) && string.IsNullOrEmpty(doc.Склад.Наименование))
                doc.Склад = await складRepository.GetEntityByIdAsync(doc.Склад.Id);
            if (!string.IsNullOrEmpty(doc.ПодСклад.Id) && string.IsNullOrEmpty(doc.ПодСклад.Наименование))
                doc.ПодСклад = await складRepository.GetПодСкладByIdAsync(doc.ПодСклад.Id);
            if (!string.IsNullOrEmpty(doc.Неисправность.Id) && string.IsNullOrEmpty(doc.Неисправность.Наименование))
                doc.Неисправность = await мастерскаяRepository.НеисправностьByIdAsync(doc.Неисправность.Id);
            if (!string.IsNullOrEmpty(doc.Неисправность2.Id) && string.IsNullOrEmpty(doc.Неисправность2.Наименование))
                doc.Неисправность2 = await мастерскаяRepository.НеисправностьByIdAsync(doc.Неисправность2.Id);
            if (!string.IsNullOrEmpty(doc.Неисправность3.Id) && string.IsNullOrEmpty(doc.Неисправность3.Наименование))
                doc.Неисправность3 = await мастерскаяRepository.НеисправностьByIdAsync(doc.Неисправность3.Id);
            if (!string.IsNullOrEmpty(doc.Неисправность4.Id) && string.IsNullOrEmpty(doc.Неисправность4.Наименование))
                doc.Неисправность4 = await мастерскаяRepository.НеисправностьByIdAsync(doc.Неисправность4.Id);
            if (!string.IsNullOrEmpty(doc.Неисправность5.Id) && string.IsNullOrEmpty(doc.Неисправность5.Наименование))
                doc.Неисправность5 = await мастерскаяRepository.НеисправностьByIdAsync(doc.Неисправность5.Id);
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент1.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент1.Наименование))
                doc.ПриложенныйДокумент1 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент1.Id);
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент2.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент2.Наименование))
                doc.ПриложенныйДокумент2 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент2.Id);
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент3.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент3.Наименование))
                doc.ПриложенныйДокумент3 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент3.Id);
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент4.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент4.Наименование))
                doc.ПриложенныйДокумент4 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент4.Id);
            if (!string.IsNullOrEmpty(doc.ПриложенныйДокумент5.Id) && string.IsNullOrEmpty(doc.ПриложенныйДокумент5.Наименование))
                doc.ПриложенныйДокумент5 = await мастерскаяRepository.ПриложенныйДокументByIdAsync(doc.ПриложенныйДокумент5.Id);
            decimal ГарантийныеДокументы = (doc.Претензия ? 1 : 0) +
                (doc.ПриложенныйДокумент1 != null ? doc.ПриложенныйДокумент1.ФлагГарантии : 0) +
                (doc.ПриложенныйДокумент2 != null ? doc.ПриложенныйДокумент2.ФлагГарантии : 0) +
                (doc.ПриложенныйДокумент3 != null ? doc.ПриложенныйДокумент3.ФлагГарантии : 0) +
                (doc.ПриложенныйДокумент4 != null ? doc.ПриложенныйДокумент4.ФлагГарантии : 0) +
                (doc.ПриложенныйДокумент5 != null ? doc.ПриложенныйДокумент5.ФлагГарантии : 0);
            return new ПриемВРемонтПечать
            {
                ЮрЛицо = doc.Общие.Фирма.ЮрЛицо.Наименование,
                НомерКвитанции = doc.НомерКвитанции,
                ДатаКвитанции = (int)doc.ДатаКвитанции,
                КвитанцияId = doc.КвитанцияId,
                ТелефонСервиса = (await _context._1sconsts.FirstOrDefaultAsync(x => x.Id == 10711)).Value.Trim(),
                НомерДок = doc.Общие.НомерДок,
                ДатаДок = doc.Общие.ДатаДок.ToString("dd.MM.yyyy"),
                Комментарий = ((ГарантийныеДокументы == 0 || doc.Гарантия == 0) ? "ПЛАТНЫЙ РЕМОНТ " : "") + (string.IsNullOrEmpty(doc.Общие.Комментарий) ? "" : doc.Общие.Комментарий),
                Заказчик = doc.Заказчик.Наименование + " " + doc.Телефон.Номер ?? doc.Email.Адрес,
                ЗаказчикАдрес = string.IsNullOrEmpty(doc.Заказчик.ФактическийАдрес) ? "" : doc.Заказчик.ФактическийАдрес,
                ЗаказчикФИО = doc.Заказчик.Наименование,
                ЗаказчикТелефон = (doc.Телефон != null && !string.IsNullOrEmpty(doc.Телефон.Номер)) ? doc.Телефон.Номер : "",
                ЗаказчикEmail = (doc.Email != null && !string.IsNullOrEmpty(doc.Email.Адрес)) ? doc.Email.Адрес : "",
                Мастер = string.IsNullOrEmpty(doc.Мастер.Id) ? "" : doc.Мастер.Наименование,
                ДатаПродажи = (ГарантийныеДокументы > 0 && doc.Гарантия > 0) ? (doc.ДатаПродажи > Common.min1cDate ? doc.ДатаПродажи.ToString("dd.MM.yyyy") : "") : "Документы, подтверждающие гарантийный период обращения, не предоставлены.",
                Изделие = doc.Изделие.Наименование,
                Артикул = doc.Изделие.Артикул,
                ЗаводскойНомер = doc.ЗаводскойНомер,
                Производитель = doc.Изделие.Производитель,
                НомерРемонта = (int)doc.НомерРемонта,
                Комплектность = doc.Комплектность,
                Склад = doc.Склад.Наименование,
                МестоХранения = doc.ПодСклад.Наименование,
                Неисправность = doc.Неисправность.Наименование,
                Неисправность2 = string.IsNullOrEmpty(doc.Неисправность2.Наименование) ? "" : doc.Неисправность2.Наименование,
                Неисправность3 = string.IsNullOrEmpty(doc.Неисправность3.Наименование) ? "" : doc.Неисправность3.Наименование,
                Неисправность4 = string.IsNullOrEmpty(doc.Неисправность4.Наименование) ? "" : doc.Неисправность4.Наименование,
                Неисправность5 = string.IsNullOrEmpty(doc.Неисправность5.Наименование) ? "" : doc.Неисправность5.Наименование,
                ДатаОбращения = doc.ДатаОбращения.ToString("dd.MM.yyyy"),
                ПриложенныйДокумент1 = string.IsNullOrEmpty(doc.ПриложенныйДокумент1.Наименование) ? "" : doc.ПриложенныйДокумент1.Наименование,
                ПриложенныйДокумент2 = string.IsNullOrEmpty(doc.ПриложенныйДокумент2.Наименование) ? "" : doc.ПриложенныйДокумент2.Наименование,
                ПриложенныйДокумент3 = string.IsNullOrEmpty(doc.ПриложенныйДокумент3.Наименование) ? "" : doc.ПриложенныйДокумент3.Наименование,
                ПриложенныйДокумент4 = string.IsNullOrEmpty(doc.ПриложенныйДокумент4.Наименование) ? "" : doc.ПриложенныйДокумент4.Наименование,
                ПриложенныйДокумент5 = string.IsNullOrEmpty(doc.ПриложенныйДокумент5.Наименование) ? "" : doc.ПриложенныйДокумент5.Наименование,
                ВнешнийВид = doc.ВнешнийВид,
                СпособВозвращения = doc.СпособВозвращения,
            };
        }
        public async Task<ExceptionData> ОтправитьСообщенияAsync(ФормаПриемВРемонт doc)
        {
            if (!мастерскаяRepository.РеестрСообщенийByIdDoc(doc.Общие.ВидДокумента10, doc.Общие.IdDoc))
            {
                try
                {
                    if (!string.IsNullOrEmpty(doc.Телефон.Id))
                    {
                        if (string.IsNullOrEmpty(doc.Телефон.Номер))
                            doc.Телефон = await контрагентRepository.ТелефонByIdAsync(doc.Телефон.Id);
                        if (!string.IsNullOrEmpty(doc.Изделие.Id) && string.IsNullOrEmpty(doc.Изделие.Наименование))
                            doc.Изделие = await номенклатураRepository.GetНоменклатураByIdAsync(doc.Изделие.Id);
                        Common.ОтправитьSms(doc.Телефон.Номер, 9899, doc.Изделие.Наименование, doc.КвитанцияId);
                    }
                    if (!string.IsNullOrEmpty(doc.Email.Id))
                    {
                        if (string.IsNullOrEmpty(doc.Email.Адрес))
                            doc.Email = await контрагентRepository.EmailByIdAsync(doc.Email.Id);
                        string htmlBody = "";
                        htmlBody = htmlBody.CreateOrUpdateHtmlPrintPage("Квитанция о Приеме в ремонт", await ДанныеДляПечатиAsync(doc));
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var EmailSender = scope.ServiceProvider.GetService<IEmailSender>();
                            await EmailSender.SendEmailAsync(doc.Email.Адрес, doc.КвитанцияId, htmlBody);
                        }
                    }
                    await мастерскаяRepository.ЗаписьРеестрСообщенийAsync(doc.Общие.ВидДокумента10, doc.Общие.IdDoc, doc.Телефон, doc.Email);
                }
                catch (Exception e)
                {
                    return new ExceptionData { Code = e.HResult, Description = e.Message };
                }
            }
            return null;
        }
    }
}
