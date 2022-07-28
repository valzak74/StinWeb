using Microsoft.AspNetCore.Mvc;
using NonFactors.Mvc.Lookup;
using StinWeb.Lookups;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Справочники.Мастерская;
using StinWeb.Models.DataManager.Документы;
using StinWeb.Models.Repository.Интерфейсы;
using StinWeb.Models.Repository.Справочники;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.Repository.Документы;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class ОбщийController : Controller
    {
        private StinDbContext _context;
        private IНоменклатура _номенклатураRepository;
        private IСклад _складRepository;
        private IКонтрагент _контрагентRepository;
        private IДокумент _документRepository;
        private IДокументМастерской _документМастерской;

        public ОбщийController(StinDbContext context)
        {
            this._context = context;
            this._номенклатураRepository = new НоменклатураRepository(context);
            this._складRepository = new СкладRepository(context);
            this._контрагентRepository = new КонтрагентRepository(context);
            this._документRepository = new ДокументRepository(context);
            this._документМастерской = new ДокументМастерскойRepository(context);
        }
        protected override void Dispose(bool disposing)
        {
            _номенклатураRepository.Dispose();
            _контрагентRepository.Dispose();
            _складRepository.Dispose();
            _документRepository.Dispose();
            _документМастерской.Dispose();
            _context.Dispose();
            base.Dispose(disposing);
        }
        [HttpGet("НоменклатураБезЗапчастей")]
        public JsonResult НоменклатураБезЗапчастей(LookupFilter filter, string фАртикул, string фПроизводитель)
        {
            filter.AdditionalFilters["РежимВыбора"] = РежимВыбора.ПоТовару;
            filter.AdditionalFilters["Артикул"] = фАртикул;
            if (фПроизводитель == "ЛЮБОЙ ПРОИЗВОДИТЕЛЬ")
                filter.AdditionalFilters["Производитель"] = null;
            else
                filter.AdditionalFilters["Производитель"] = фПроизводитель;

            return Json(new НоменклатураLookup(_context) { Filter = filter }.GetData());
        }
        [HttpGet("Неисправность")]
        public JsonResult Неисправность(LookupFilter filter)
        {
            return Json(new НеисправностьLookup(_context) { Filter = filter }.GetData());
        }
        [HttpGet("ПриложенныйДокумент")]
        public JsonResult ПриложенныйДокумент(LookupFilter filter)
        {
            return Json(new ПриложенныйДокументLookup(_context) { Filter = filter }.GetData());
        }
        [HttpGet("Контрагент")]
        public JsonResult Контрагент(LookupFilter filter)
        {
            return Json(new КонтрагентLookup(_context) { Filter = filter }.GetData());
        }
        [HttpGet("Телефон")]
        public JsonResult ТелефонКонтрагента(LookupFilter filter, string контрагентId)
        {
            filter.AdditionalFilters["КонтрагентId"] = контрагентId;
            return Json(new ТелефонLookup(_context) { Filter = filter }.GetData());
        }
        [HttpGet("Email")]
        public JsonResult EmailКонтрагента(LookupFilter filter, string контрагентId)
        {
            filter.AdditionalFilters["КонтрагентId"] = контрагентId;
            return Json(new EmailLookup(_context) { Filter = filter }.GetData());
        }
        [HttpPost]
        public IActionResult ПолучитьФильтрПроизводителей(РежимВыбора режим)
        {
            return Ok(_номенклатураRepository.GetBrendsForFilter(режим));
        }
        [HttpPost("ПолучитьПодСклады")]
        public IActionResult ПолучитьПодСклады(string СкладId)
        {
            return Ok(_складRepository.ПолучитьПодСклады(СкладId).ToList());
        }
        [HttpPost("ПолучитьОбщиеПараметрыДокумента")]
        public async Task<IActionResult> ПолучитьОбщиеПараметрыДокумента(string IdDoc)
        {
            ОбщиеРеквизиты общиеРеквизиты = await _документRepository.ОбщиеРеквизитыAsync(IdDoc);
            return Ok(общиеРеквизиты);
        }
        [HttpPost("ПроверкаДублированияИНН")]
        public async Task<IActionResult> ПроверкаДублированияИННAsync(string ИНН, string КПП)
        {
            Контрагент НайденныйКонтрагент = null;
            if (!string.IsNullOrEmpty(КПП))
            {
                НайденныйКонтрагент = await _контрагентRepository.ПолучитьПоИННAsync(ИНН + "/" + КПП, true);
                if (НайденныйКонтрагент == null)
                    НайденныйКонтрагент = await _контрагентRepository.ПолучитьПоИННAsync(ИНН + @"\" + КПП, true);
            }
            else
                НайденныйКонтрагент = await _контрагентRepository.ПолучитьПоИННAsync(ИНН, true);
            return Ok(new
            {
                isValid = НайденныйКонтрагент == null,
                message = НайденныйКонтрагент == null ? "" : "ИНН используется \"" + НайденныйКонтрагент.Наименование + "\""
            });
        }
        [HttpPost("НовыйКонтрагент")]
        public async Task<IActionResult> НовыйКонтрагентAsync(int ВидКонтрагента, string Наименование, string ИНН, string КПП,
            string Адрес, string Телефон, string Email)
        {
            return Ok(await _контрагентRepository.НовыйКонтрагентAsync(ВидКонтрагента, Наименование, ИНН, КПП, Адрес, Телефон, Email));
        }
        [HttpPost("НовыйТелефон")]
        public async Task<IActionResult> НовыйТелефонAsync(string контрагентId, string номерТелефона)
        {
            return Ok(await _контрагентRepository.НовыйТелефонAsync(контрагентId, номерТелефона));
        }
        [HttpPost("НовыйEmail")]
        public async Task<IActionResult> НовыйEmailAsync(string контрагентId, string адресEmail)
        {
            return Ok(await _контрагентRepository.НовыйEmailAsync(контрагентId, адресEmail));
        }
        [HttpPost("ПолучитьФормуДокумента")]
        public IActionResult ПолучитьФормуДокумента(string родитель, bool просмотр, string idDoc, int видДок, string докОснованиеId, int видДокОснование, string параметр)
        {
            HttpContext.Session.SetObjectAsJson("мнТабличнаяЧасть", null);
            HttpContext.Session.SetObjectAsJson("ПодборРабот", null);

            return ViewComponent("Документы", new
            {
                заполнениеДок = true,
                родитель = родитель,
                просмотр = просмотр,
                idDoc = idDoc,
                видДок = видДок,
                докОснованиеId = докОснованиеId,
                видДокОснование = видДокОснование,
                параметр = параметр
            });
        }
        [HttpPost("АктивироватьИзделие")]
        public async Task<IActionResult> АктивироватьИзделиеAsync(string квитанцияНомер, decimal квитанцияДата, string idDoc, int видДокумента)
        {
            var Инфо = await _документМастерской.АктивироватьИзделиеAsync(квитанцияНомер, квитанцияДата, User.FindFirstValue("UserId"), idDoc);
            switch (видДокумента)
            {
                case 10080:
                    return Ok(Инфо);
                case 13737:
                    //Изменение статуса
                    var РазрешенныйСтатусДляИзменения = Common.СтатусПартии.Where(x => x.Value == "Сортировка" ||
                                                                                       x.Value == "Претензия на рассмотрении" ||
                                                                                       x.Value == "Претензия отклонена" ||
                                                                                       x.Value == "Замена по претензии" ||
                                                                                       x.Value == "Восстановление по претензии" ||
                                                                                       x.Value == "Возврат денег по претензии" ||
                                                                                       x.Value == "Доукомплектация по претензии");
                    if (РазрешенныйСтатусДляИзменения.Any(x => x.Key == Инфо.СтатусПартииId))
                        return Ok(Инфо);
                    else
                        return Ok(new ИнформацияИзделия());
            }
            return BadRequest(new ExceptionData { Description = "Тип документа не поддерживается" });
        }
        [HttpGet("СтруктураПодчиненности")]
        public JsonResult СтруктураПодчиненности(string idDoc, bool findRoot)
        {
            if (string.IsNullOrEmpty(idDoc))
                return null;
            var tree = from t in _context.fn_GetTreeById(idDoc.Replace("_", " "), findRoot)
                       join d in _документRepository.ЖурналДокументов(DateTime.MinValue, DateTime.MinValue, null) on t.Iddoc equals d.IdDoc
                       select new
                       {
                           id = t.Iddoc.Replace(' ', '_'),
                           parent = t.Parentid == Common.ПустоеЗначение ? "#" : t.Parentid.Replace(' ', '_'),
                           text = (string.IsNullOrEmpty(d.НазваниеВЖурнале) ? d.Наименование : string.Format(d.НазваниеВЖурнале, d.Наименование)) + " № " + d.НомерДок + " от " + d.ДатаДок.ToString(),
                           //text = "<p>" + s.Docno + "<br/>" + s.Iddocdef.ToString() + "<br/>" + s.DateTimeIddoc + "</p>",
                           //text = "<a><label>label 1 & label 2</label><br/><label>label 2</label></a>",
                           //childrenid = s.ChildIdDoc.Replace(' ', '_'),
                           //vid10 = d.ВидДокумента10,
                           //icon = "jstree-file",
                           state = new { opened = true },
                           type = d.Проведен ? "ok" : (d.Удален ? "er" : "default"), //"child",
                           //li_attr = new { style = "height: auto;" },
                           a_attr = new { style = "height: auto;border: 3px solid " + (d.Проведен ? "#008000" : d.Удален ? "#FF0000" : "#ccc") + ";border-radius: 10px;margin: 20px 0 0;" },
                           data = new
                           {
                               price = "0"
                           }
                       };
            return Json(tree);
        }

    }
}
