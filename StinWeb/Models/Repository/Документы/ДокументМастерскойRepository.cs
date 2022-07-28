using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.Repository.Интерфейсы.Регистры;
using StinWeb.Models.Repository.Регистры;
using StinWeb.Models.DataManager.Справочники.Мастерская;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinWeb.Models.DataManager;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Документы
{
    public class ДокументМастерскойRepository : ДокументRepository, IДокументМастерской
    {
        public IРегистр_НомерПриемаВРемонт регистр_номерПриемаВРемонт;
        public IРегистр_ПартииМастерской регистр_ПартииМастерской;
        public IРегистр_ОстаткиИзделий регистр_ОстаткиИзделий;
        public ДокументМастерскойRepository(StinDbContext context) : base(context)
        {
            регистр_ПартииМастерской = new Регистр_ПартииМастерской(context);
            регистр_номерПриемаВРемонт = new Регистр_НомерПриемаВРемонтRepository(context);
            регистр_ОстаткиИзделий = new Регистр_ОстаткиИзделий(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    регистр_ПартииМастерской.Dispose();
                    регистр_номерПриемаВРемонт.Dispose();
                    регистр_ОстаткиИзделий.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ИнформацияИзделия> АктивироватьИзделиеAsync(string квитанцияНомер, decimal квитанцияДата, string userId, string idDoc)
        {
            var РазрешенныеСклады = складRepository.ПолучитьРазрешенныеСклады(userId).Select(x => x.Id).ToList();
            DateTime ДатаДок = DateTime.Now;
            if (!string.IsNullOrEmpty(idDoc))
            {
                _1sjourn j = await _context._1sjourns.FirstOrDefaultAsync(x => x.Iddoc == idDoc);
                if (j != null)
                {
                    ДатаДок = Common.DateTimeIddoc(j.DateTimeIddoc);
                }
            }
            var РегистрОстаткиИзделий_Остатки = await регистр_ОстаткиИзделий.ПолучитьОстаткиAsync(ДатаДок, idDoc, false, квитанцияНомер, квитанцияДата, РазрешенныеСклады);
            if (РегистрОстаткиИзделий_Остатки != null && РегистрОстаткиИзделий_Остатки.Count == 1)
            {
                var РегистрПартииМастерской_Остатки = await регистр_ПартииМастерской.ПолучитьОстаткиAsync(ДатаДок, idDoc, false, квитанцияНомер, квитанцияДата);
                if (РегистрПартииМастерской_Остатки != null && РегистрПартииМастерской_Остатки.Count == 1)
                {
                    var ЗаписьРегистраПартииМастерской = РегистрПартииМастерской_Остатки.FirstOrDefault();
                    var ЗаписьРегистраОстаткиИзделий = РегистрОстаткиИзделий_Остатки.FirstOrDefault();
                    var ПриемId = await регистр_номерПриемаВРемонт.ПолучитьДокументIdAsync(ДатаДок, idDoc, false, квитанцияНомер, квитанцияДата);
                    ФормаПриемВРемонт Прием = null;
                    if (!string.IsNullOrEmpty(ПриемId))
                    {
                        Прием = await ПриемВРемонтAsync(ПриемId);
                    }
                    return new ИнформацияИзделия
                    {
                        ДокОснование = new DataManager.Документы.ДокОснование 
                        {
                            IdDoc = Прием != null ? Прием.Общие.IdDoc : "",
                            ВидДокумента10 = Прием != null ? Прием.Общие.ВидДокумента10 : 0,
                            НомерДок = Прием != null ? Прием.Общие.НомерДок : "",
                            ДатаДок = Прием != null ? Прием.Общие.ДатаДок : DateTime.MinValue,
                        },
                        ДокПрием = Прием,
                        НомерКвитанции = квитанцияНомер,
                        ДатаКвитанции = квитанцияДата,
                        Претензия = Прием != null ? Прием.Претензия : false,
                        Изделие = Прием != null ? Прием.Изделие : null,
                        ЗаводскойНомер = Прием != null ? Прием.ЗаводскойНомер : "",
                        Гарантия = ЗаписьРегистраПартииМастерской.Гарантия,
                        Заказчик = Прием != null ? Прием.Заказчик : null,
                        Email = Прием != null ? Прием.Email : null,
                        Телефон = Прием != null ? Прием.Телефон : null,
                        ДатаПриема = Прием != null ? Прием.ДатаПриема : DateTime.MinValue,
                        ДатаПродажи = Прием != null ? Прием.ДатаПродажи : DateTime.MinValue,
                        НомерРемонта = Прием != null ? Прием.НомерРемонта : 1,
                        Комплектность = Прием != null ? Прием.Комплектность : "",
                        Неисправность = Прием != null ? Прием.Неисправность : null,
                        Неисправность2 = Прием != null ? Прием.Неисправность2 : null,
                        Неисправность3 = Прием != null ? Прием.Неисправность3 : null,
                        Неисправность4 = Прием != null ? Прием.Неисправность4 : null,
                        Неисправность5 = Прием != null ? Прием.Неисправность5 : null,
                        Комментарий = Прием != null ? Прием.Общие.Комментарий : "",
                        СтатусПартииId = ЗаписьРегистраПартииМастерской.СтатусПартии,
                        СкладОткуда = Прием != null ? Прием.СкладОткуда : null,
                        Мастер = await мастерскаяRepository.МастерByIdAsync(ЗаписьРегистраОстаткиИзделий.МастерId),
                        Склад = await складRepository.GetEntityByIdAsync(ЗаписьРегистраОстаткиИзделий.СкладId),
                        ПодСклад = await складRepository.GetПодСкладByIdAsync(ЗаписьРегистраОстаткиИзделий.ПодСкладId),
                        ДатаОбращения = Прием != null ? Прием.ДатаОбращения : DateTime.MinValue,
                        ВнешнийВид = Прием != null ? Прием.ВнешнийВид : "",
                        СпособВозвращенияId = Прием != null ? Прием.СпособВозвращенияId : 0,
                        ПриложенныйДокумент1 = Прием != null ? Прием.ПриложенныйДокумент1 : null,
                        ПриложенныйДокумент2 = Прием != null ? Прием.ПриложенныйДокумент2 : null,
                        ПриложенныйДокумент3 = Прием != null ? Прием.ПриложенныйДокумент3 : null,
                        ПриложенныйДокумент4 = Прием != null ? Прием.ПриложенныйДокумент4 : null,
                        ПриложенныйДокумент5 = Прием != null ? Прием.ПриложенныйДокумент5 : null,
                    };
                }
            }
            return new ИнформацияИзделия();
        }
    }
}
