using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Справочники;
using StinWeb.Models.Repository.Документы;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using Microsoft.Extensions.DependencyInjection;
using StinClasses.Models;

namespace StinWeb.Controllers.Отчеты
{
    public class ЖурналДоставкиController : Controller
    {
        private UserRepository _userRepository;
        private СкладRepository _складRepository;
        private КонтрагентRepository _контрагентRepository;
        private IПриемВРемонт _приемВРемонт;
        public ЖурналДоставкиController(StinDbContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _userRepository = new UserRepository(context);
            _складRepository = new СкладRepository(context);
            _контрагентRepository = new КонтрагентRepository(context);
            _приемВРемонт = new ПриемВРемонтRepository(context, serviceScopeFactory);
        }
        protected override void Dispose(bool disposing)
        {
            _userRepository.Dispose();
            _складRepository.Dispose();
            _контрагентRepository.Dispose();
            _приемВРемонт.Dispose();
            base.Dispose(disposing);
        }
        public IActionResult Index()
        {
            var _пользователь = _userRepository.GetUserById(User.FindFirstValue("UserId"));
            ViewBag.Склады = new SelectList(_складRepository.ПолучитьРазрешенныеСклады(_пользователь.Id), "Id", "Наименование", _пользователь.ОсновнойСклад.Id);
            return View("~/Views/Отчеты/ЖурналДоставки.cshtml");
        }
        [HttpGet]
        public PartialViewResult Сформировать(string складId, short режим)
        {
            return PartialView("_ЖурналДоставки", _складRepository.СформироватьОстаткиДоставки(Common.min1cDate, null, складId, (РежимВыбора)режим, null, null));
        }

    }
}
