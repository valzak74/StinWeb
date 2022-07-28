using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.Repository.Документы;
using StinWeb.Models.DataManager.Документы;
using StinClasses.Models;

namespace StinWeb.Controllers.Отчеты
{
    public class ЖурналОбщийController : Controller
    {
        private IДокумент _документRepository;
        public ЖурналОбщийController(StinDbContext context)
        {
            _документRepository = new ДокументRepository(context);
        }
        protected override void Dispose(bool disposing)
        {
            _документRepository.Dispose();
            base.Dispose(disposing);
        }
        public IActionResult Index()
        {
            return View("~/Views/Отчеты/Журнал.cshtml");
        }
        [HttpGet]
        public IActionResult ПолучитьЖурнал(DateTime startDate, DateTime endDate)
        {
            if (startDate == DateTime.MinValue)
                startDate = DateTime.Now.Date;
            if (endDate == DateTime.MinValue)
                endDate = DateTime.Now;
            var result = _документRepository.ЖурналДокументов(startDate, endDate, null);
            if (!result.Any())
                result = Enumerable.Empty<ОбщиеРеквизиты>().AsQueryable();
            return PartialView("~/Views/Shared/Components/ЖурналОбщий/Default.cshtml", result);
        }

    }
}
