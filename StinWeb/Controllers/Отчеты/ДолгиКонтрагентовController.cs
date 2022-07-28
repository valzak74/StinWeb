using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Справочники;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using NonFactors.Mvc.Lookup;
using StinWeb.Lookups;
using StinClasses.Models;

namespace StinWeb.Controllers.Отчеты
{
    public class ДолгиКонтрагентовController : Controller
    {
        private StinDbContext _context;
        private UserRepository _userRepository;
        private КонтрагентRepository _контрагентRepository;
        public ДолгиКонтрагентовController(StinDbContext context)
        {
            _context = context;
            _userRepository = new UserRepository(context);
            _контрагентRepository = new КонтрагентRepository(context);
        }
        protected override void Dispose(bool disposing)
        {
            _userRepository.Dispose();
            _контрагентRepository.Dispose();
            _context.Dispose();
            base.Dispose(disposing);
        }
        [Authorize]
        public IActionResult Index()
        {
            if (_context.NeedToOpenPeriod())
                return Redirect("/Home/Error?Description=ПериодНеОткрыт");
            Dictionary<int, string> ВариантыГруппировки = new Dictionary<int, string>();
            ВариантыГруппировки.Add(0, "Менеджеры/Группы/Контрагенты/Документы");
            ВариантыГруппировки.Add(1, "Менеджеры/Группы/Контрагенты");
            ВариантыГруппировки.Add(2, "Менеджеры/Группы");
            ВариантыГруппировки.Add(3, "Менеджеры/Контрагенты");
            ВариантыГруппировки.Add(4, "Менеджеры");
            ViewBag.ВариантыГруппировки = new SelectList(ВариантыГруппировки, "Key", "Value");
            return View("~/Views/Отчеты/ДолгиКонтрагентов.cshtml");
        }
        public async Task<IActionResult> IndexReport(string manual, int sorting, bool needGroup, bool needCustomer, bool needDocument,
            string checkedManager, string checkedGroup, string checkedCustomer, bool needExcel, bool onlyMissed, bool onlyFailedDocs)
        {
            if (string.IsNullOrEmpty(manual))
                return PartialView("~/Views/Отчеты/ДолгиКонтрагентовФормаОтчета.cshtml", new ОтчетПоДолгам() { СортировкаПокупателиПоставщики = sorting == 0, Результаты = Enumerable.Empty<ДолгиТаблица>().OrderBy(x => 1) });
            var result = await _контрагентRepository.ДолгиМенеджеровAsync("", 
                string.IsNullOrEmpty(checkedManager) ? "" : checkedManager.Replace('_',' '), 
                string.IsNullOrEmpty(checkedGroup) ? "" : checkedGroup.Replace('_', ' '), 
                string.IsNullOrEmpty(checkedCustomer) ? "" : checkedCustomer.Replace('_', ' '),
                needGroup, needCustomer, needDocument,
                onlyMissed, onlyFailedDocs);
            bool сортировкаПокупателиПоставщики = sorting == 0;
            if (needExcel)
            {
                var fileName = System.Web.HttpUtility.UrlEncode("ДолгиКонтрагентов_" + DateTime.Now.ToString("dd_MM_yyyy") + ".xls", System.Text.Encoding.UTF8);
                return File(result.CreateExcel("xls", сортировкаПокупателиПоставщики), "application/octet-stream", fileName);
            }
            else
                return PartialView("~/Views/Отчеты/ДолгиКонтрагентовФормаОтчета.cshtml", new ОтчетПоДолгам() { СортировкаПокупателиПоставщики = сортировкаПокупателиПоставщики, Результаты = result });
        }
        [HttpGet]
        public JsonResult ВыбратьМенеджера(LookupFilter filter)
        {
            return Json(new МенеджерLookup(_context) { Filter = filter }.GetData());
        }
        [HttpGet]
        public JsonResult ВыбратьГруппуКонтрагентов(LookupFilter filter)
        {
            return Json(new ГруппаКонтрагентовLookup(_context) { Filter = filter }.GetData());
        }
        [HttpGet]
        public JsonResult ВыбратьКонтрагента(LookupFilter filter)
        {
            return Json(new КонтрагентLookup(_context) { Filter = filter }.GetData());
        }
    }
}
