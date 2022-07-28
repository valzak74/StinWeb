using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using StinWeb.Models.DataManager;
using StinWeb.Models.Repository.Интерфейсы;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.Repository.Справочники;
using StinWeb.Models.Repository.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;
using Microsoft.Extensions.DependencyInjection;
using StinClasses.Models;

namespace StinWeb.ViewComponents
{
    public class ДокументыViewComponent : ViewComponent, IDisposable
    {
        private bool disposed = false;
        private IUser _userRepository;
        private IНоменклатура _номенклатураRepository;
        private IСклад _складRepository;
        private IМастерская _мастерскаяRepository;
        private IПриемВРемонт _приемВРемонтRepository;
        private IПеремещениеИзделий _перемещениеИзделийRepository;
        private IИзменениеСтатуса _изменениеСтатуса;
        private IАвансоваяОплата _авансоваяОплата;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _userRepository.Dispose();
                    _номенклатураRepository.Dispose();
                    _складRepository.Dispose();
                    _мастерскаяRepository.Dispose();
                    _приемВРемонтRepository.Dispose();
                    _перемещениеИзделийRepository.Dispose();
                    _изменениеСтатуса.Dispose();
                    _авансоваяОплата.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public ДокументыViewComponent(StinDbContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _userRepository = new UserRepository(context);
            _номенклатураRepository = new НоменклатураRepository(context);
            _складRepository = new СкладRepository(context);
            _мастерскаяRepository = new МастерскаяRepository(context);
            _приемВРемонтRepository = new ПриемВРемонтRepository(context, serviceScopeFactory);
            _перемещениеИзделийRepository = new ПеремещениеИзделийRepository(context);
            _изменениеСтатуса = new ИзменениеСтатусаRepository(context);
            _авансоваяОплата = new АвансоваяОплатаRepository(context);
        }
        public async Task<IViewComponentResult> InvokeAsync(bool заполнениеДок, string родитель, bool просмотр, string idDoc, int видДок, string докОснованиеId, int видДокОснование, string параметр)
        {
            bool новый = string.IsNullOrEmpty(idDoc) && string.IsNullOrEmpty(докОснованиеId);
            var userRowId = Int32.Parse(UserClaimsPrincipal.FindFirstValue("UserRowId"));
            if (заполнениеДок)
            {
                var Пользователь = await _userRepository.GetUserByRowIdAsync(userRowId);
                switch (видДок)
                {
                    case 10080:
                        ФормаПеремещениеИзделий формаПеремещениеИзделий = null;
                        ViewBag.СкладДляРемонта = _складRepository.КонстантаСкладДляРемонта().Id;
                        var СкладСортировкиРемонтов = _складRepository.КонстантаСкладСортировкиРемонтов().Id;
                        var DefaultСкладId = Пользователь.ОсновнойСклад.Id == СкладСортировкиРемонтов ? (string)ViewBag.СкладДляРемонта : СкладСортировкиРемонтов;
                        ViewBag.СкладыДокумента = new SelectList(_складRepository.ПолучитьСкладыМастерские().ToList().AsEnumerable(), "Id", "Наименование", DefaultСкладId);
                        if (просмотр)
                            формаПеремещениеИзделий = await _перемещениеИзделийRepository.ПросмотрAsync(idDoc);
                        else
                        {
                            if (новый)
                                формаПеремещениеИзделий = await _перемещениеИзделийRepository.НовыйAsync(userRowId);
                            //else
                            //    формаПеремещениеИзделий = await _приемВРемонтRepository.ВводНаОснованииAsync(докОснованиеId, видДокОснование);
                        }
                        ViewBag.DefaultPrefix = _мастерскаяRepository.DefaultPrefix(формаПеремещениеИзделий.Общие.Автор.Id, формаПеремещениеИзделий.Общие.Фирма.ЮрЛицо.Префикс);
                        формаПеремещениеИзделий.Общие.Родитель = родитель;
                        if (формаПеремещениеИзделий.Ошибка != null && !формаПеремещениеИзделий.Ошибка.Skip)
                            ModelState.AddModelError("", формаПеремещениеИзделий.Ошибка.Description);
                        return View("ПеремещениеИзделий", формаПеремещениеИзделий);
                    case 13737:
                        ФормаИзменениеСтатуса формаИзменениеСтатуса = null;
                        //ViewBag.ТипыРемонта = new SelectList(Common.ТипРемонта.Where(x => x.Key != 3).AsEnumerable(), "Key", "Value");
                        ViewBag.СкладДляРемонта = _складRepository.КонстантаСкладДляРемонта().Id;
                        var DefaultСтатусПартииId = Common.СтатусПартии.FirstOrDefault(x => x.Value == "Претензия на рассмотрении").Key;
                        ViewBag.СтатусыДокумента = new SelectList(Common.СтатусПартии.Where(x => x.Value == "Претензия на рассмотрении" ||
                                                                                                 x.Value == "Претензия отклонена" ||
                                                                                                 x.Value == "Замена по претензии" ||
                                                                                                 x.Value == "Восстановление по претензии" ||
                                                                                                 x.Value == "Возврат денег по претензии" ||
                                                                                                 x.Value == "Доукомплектация по претензии" ||
                                                                                                 x.Value == "Диагностика претензии")
                            .Select(x => x), "Key", "Value", DefaultСтатусПартииId);
                        if (просмотр)
                            формаИзменениеСтатуса = await _изменениеСтатуса.ПросмотрAsync(idDoc);
                        else
                        {
                            if (новый)
                                формаИзменениеСтатуса = await _изменениеСтатуса.НовыйAsync(userRowId);
                            //else
                            //    формаПеремещениеИзделий = await _приемВРемонтRepository.ВводНаОснованииAsync(докОснованиеId, видДокОснование);
                        }
                        ViewBag.DefaultPrefix = _мастерскаяRepository.DefaultPrefix(формаИзменениеСтатуса.Общие.Автор.Id, "P"); // формаИзменениеСтатуса.Общие.Фирма.ЮрЛицо.Префикс);
                        формаИзменениеСтатуса.Общие.Родитель = родитель;
                        if (формаИзменениеСтатуса.Ошибка != null && !формаИзменениеСтатуса.Ошибка.Skip)
                            ModelState.AddModelError("", формаИзменениеСтатуса.Ошибка.Description);
                        return View("ИзменениеСтатуса", формаИзменениеСтатуса);
                    case 9899:
                        ФормаПриемВРемонт формаПриемВРемонт = null;
                        var ПодборТиповРемонта = Common.ТипРемонта.AsEnumerable();
                        ViewBag.СкладДляРемонта = _складRepository.КонстантаСкладДляРемонта().Id;
                        ViewBag.СкладСортировкиРемонтов = _складRepository.КонстантаСкладСортировкиРемонтов().Id;
                        ViewBag.СпособВозвращения = new SelectList(Common.СпособыВозвращения, "Key", "Value");
                        var мастера = _мастерскаяRepository.Мастера().ToList();
                        мастера.Insert(0, new Models.DataManager.Справочники.Мастерская.Мастер { Id = "", Наименование = "<< НЕ УКАЗАН >>" });
                        ViewBag.МастераДокумента = new SelectList(мастера.AsEnumerable(), "Id", "Наименование");
                        if (просмотр)
                        {
                            формаПриемВРемонт = await _приемВРемонтRepository.ПросмотрAsync(idDoc);
                            ViewBag.ТипыРемонта = new SelectList(ПодборТиповРемонта, "Key", "Value");
                            ViewBag.СкладыДокумента = new SelectList(_складRepository.ПолучитьСклады().ToList().AsEnumerable(), "Id", "Наименование", Пользователь.ОсновнойСклад.Id);
                            ViewBag.ПодСкладыДокумента = new SelectList(_складRepository.ПолучитьПодСклады().ToList().AsEnumerable(), "Id", "Наименование", Пользователь.ОсновнойПодСклад.Id);
                        }
                        else
                        {
                            if (новый)
                                формаПриемВРемонт = await _приемВРемонтRepository.НовыйAsync(userRowId, параметр);
                            else
                                формаПриемВРемонт = await _приемВРемонтRepository.ВводНаОснованииAsync(докОснованиеId, видДокОснование, userRowId);
                            ПодборТиповРемонта = ПодборТиповРемонта.Where(x => x.Key != 3);
                            if (формаПриемВРемонт.Гарантия != 1)
                                ПодборТиповРемонта = ПодборТиповРемонта.Where(x => x.Key != 1);
                            if (формаПриемВРемонт.Гарантия != 2)
                                ПодборТиповРемонта = ПодборТиповРемонта.Where(x => x.Key != 2);
                            ViewBag.СкладыДокумента = new SelectList(_складRepository.ПолучитьРазрешенныеСклады(Пользователь.Id).ToList().AsEnumerable(), "Id", "Наименование", Пользователь.ОсновнойСклад.Id);
                            string СкладId = "";
                            if (формаПриемВРемонт.Склад != null)
                                СкладId = формаПриемВРемонт.Склад.Id;
                            else
                                СкладId = (ViewBag.СкладыДокумента as SelectList).SelectedValue.ToString();
                            if (!string.IsNullOrEmpty(СкладId))
                            {
                                формаПриемВРемонт.ExpressForm = новый && (СкладId == ViewBag.СкладСортировкиРемонтов);
                                ViewBag.ПодСкладыДокумента = new SelectList(_складRepository.ПолучитьПодСклады(СкладId).ToList().AsEnumerable(), "Id", "Наименование", Пользователь.ОсновнойПодСклад.Id);
                                if (формаПриемВРемонт.Гарантия == 2 && (формаПриемВРемонт.СтатусПартииId == Common.СтатусПартии.FirstOrDefault(x => x.Value == "Принят в ремонт").Key || формаПриемВРемонт.СтатусПартииId == "") && СкладId != ViewBag.СкладСортировкиРемонтов)
                                {
                                    ПодборТиповРемонта = ПодборТиповРемонта.Where(x => x.Key != 2);
                                    формаПриемВРемонт.Гарантия = 4;
                                }
                            }
                            else
                                ViewBag.ПодСкладыДокумента = new SelectList(_складRepository.ПолучитьПодСклады().ToList().AsEnumerable(), "Id", "Наименование", Пользователь.ОсновнойПодСклад.Id);
                            ViewBag.ТипыРемонта = new SelectList(ПодборТиповРемонта, "Key", "Value");
                        }
                        формаПриемВРемонт.Общие.Родитель = родитель;
                        if (формаПриемВРемонт.Ошибка != null && !формаПриемВРемонт.Ошибка.Skip)
                            ModelState.AddModelError("", формаПриемВРемонт.Ошибка.Description);
                        return View("ПриемВРемонт", формаПриемВРемонт);
                    case 10054:
                        ФормаАвансоваяОплата формаАвансоваяОплата = null;
                        if (просмотр)
                        {
                            формаАвансоваяОплата = await _авансоваяОплата.ПросмотрAsync(idDoc);
                        }
                        else
                        {

                        }
                        if (формаАвансоваяОплата != null && формаАвансоваяОплата.ТабличнаяЧасть != null && формаАвансоваяОплата.ТабличнаяЧасть.Count > 0)
                        {
                            List<Корзина> корзинаРабот = формаАвансоваяОплата.ТабличнаяЧасть.Select(x => new Корзина 
                            {
                                Id = x.Работа.Id,
                                Наименование = _мастерскаяRepository.РаботаById(x.Работа.Id).Наименование,
                                Quantity = x.Количество,
                                Цена = x.Цена,
                            }).ToList();
                            HttpContext.Session.SetObjectAsJson("ПодборРабот", корзинаРабот);
                        }
                        ViewBag.DefaultPrefix = _мастерскаяRepository.DefaultPrefix(формаАвансоваяОплата.Общие.Автор.Id, формаАвансоваяОплата.Общие.Фирма.ЮрЛицо.Префикс);
                        формаАвансоваяОплата.Общие.Родитель = родитель;
                        if (формаАвансоваяОплата.Ошибка != null && !формаАвансоваяОплата.Ошибка.Skip)
                            ModelState.AddModelError("", формаАвансоваяОплата.Ошибка.Description);
                        return View("АктВыполненныхРабот", формаАвансоваяОплата);
                }
            }
            return View("Default");
        }
    }
}
