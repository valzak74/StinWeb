using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.DataManager;
using System.Globalization;

namespace StinWeb.Controllers
{
    public class КорзинаController : Controller
    {
        [HttpGet]
        public IActionResult ПолучитьДанные(string key, bool modalVersion, bool showАртикул, bool showПроизводитель, bool showЕдиницы, bool showЦены)
        {
            return ViewComponent("Корзина", new
            {
                key,
                modalVersion,
                showАртикул,
                showПроизводитель,
                showЕдиницы,
                showЦены
            });
        }
        [HttpPost]
        public IActionResult ДобавитьВПодбор(string sessionKey, string id, string наименование, string артикул, string производитель, string цена, decimal количество)
        {
            HttpContext.Session.AddOrUpdateObjectAsJson(sessionKey, new Корзина
            {
                Id = id.Replace('_',' '),
                Наименование = наименование,
                Артикул = артикул,
                Производитель = производитель,
                Quantity = количество,
                Цена = decimal.Parse(string.IsNullOrEmpty(цена) ? "0" : цена, NumberStyles.AllowCurrencySymbol | NumberStyles.Number)
            });
            return Ok();
        }
        [HttpPost]
        public IActionResult УдалитьИзПодбора(string key, string id = "")
        {
            if (string.IsNullOrEmpty(id))
            {
                HttpContext.Session.SetObjectAsJson(key, null);
            }
            else
            {
                var корзина = HttpContext.Session.GetObjectFromJson<List<Корзина>>(key);
                var item = корзина.Find(x => x.Id == id.Replace('_', ' '));
                if (item != null)
                {
                    корзина.Remove(item);
                    HttpContext.Session.SetObjectAsJson(key, корзина);
                }
            }
            return Ok();
        }
    }
}
