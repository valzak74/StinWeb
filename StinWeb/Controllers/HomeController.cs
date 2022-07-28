using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StinWeb.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using StinWeb.Models.DataManager;

namespace StinWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        //[Authorize(Policy = "Сервис")]
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string description)
        {
            if (!string.IsNullOrEmpty(description))
                switch (description)
                {
                    case "ПериодНеОткрыт":
                        description = "Период не открыт. Необходимо выполнить открытие периода в монопольном режиме";
                        break;
                    default:
                        break;
                }
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, Description = description });
        }
        [HttpGet]
        public PartialViewResult IndexКорзина(
            string sessionKey, 
            bool showАртикул = true, 
            bool showПроизводитель = true,
            bool showЕдиницы = false,
            bool showЦены = true,
            bool modal = false)
        {
            var model = new ДанныеКорзины(HttpContext.Session.GetObjectFromJson<List<Корзина>>(sessionKey));
            model.Key = sessionKey;
            model.ShowАртикул = showАртикул;
            model.ShowПроизводитель = showПроизводитель;
            model.ShowЕдиницы = showЕдиницы;
            model.ShowЦены = showЦены;
            model.modalVersion = modal;
            return PartialView("_IndexКорзина", model);
        }
    }
}
