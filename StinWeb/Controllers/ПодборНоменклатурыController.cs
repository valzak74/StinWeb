using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.DataManager;
using StinWeb.Models.Repository.Справочники;
using System.Globalization;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class ПодборНоменклатурыController : Controller
    {
        private НоменклатураRepository _номенклатура;
        private ФирмаRepository _фирмаRepository;
        public ПодборНоменклатурыController(StinDbContext context)
        {
            _номенклатура = new НоменклатураRepository(context);
            _фирмаRepository = new ФирмаRepository(context);
        }
        protected override void Dispose(bool disposing)
        {
            _номенклатура.Dispose();
            _фирмаRepository.Dispose();
            base.Dispose(disposing);
        }
        [HttpGet]
        public async Task<PartialViewResult> IndexНоменклатура(
            int manual,
            string фирмаId,
            string складId,
            string договорId,
            string картаId,
            bool доставка,
            int типДоставки,
            string search
            )
        {
            if (manual == 0)
            {
                return PartialView("_IndexНоменклатура", Enumerable.Empty<Models.DataManager.Справочники.Номенклатура>().AsQueryable());
            }
            else
            {
                List<string> СписокФирм = new List<string>();
                if (!string.IsNullOrEmpty(фирмаId))
                {
                    фирмаId = фирмаId.Replace('_', ' ');
                    СписокФирм = await _фирмаRepository.ПолучитьСписокРазрешенныхФирмAsync(фирмаId);
                }
                if (!string.IsNullOrEmpty(складId))
                    складId = складId.Replace('_', ' ');
                if (!string.IsNullOrEmpty(договорId))
                    договорId = договорId.Replace('_', ' ');
                if (!string.IsNullOrEmpty(картаId))
                    картаId = картаId.Replace('_', ' ');
                var model = await _номенклатура.ВсяНоменклатураБезПапокБрендЦенаОстаткиAsync(СписокФирм, складId, договорId, картаId, доставка, типДоставки, search);
                return PartialView("_IndexНоменклатура", model);
            }
        }
        public JsonResult GetTableLevel(string id)
        {
            var nomList = _номенклатура.GetAllWithBrendWithCost((id == "#" ? Common.ПустоеЗначение : id.Replace('_', ' '))).AsEnumerable();
            var items = (from spr in nomList
                         select new
                         {
                             id = spr.Id.Replace(' ', '_'),
                             parent = id,
                             text = spr.Наименование,
                             children = spr.IsFolder,
                             icon = spr.IsFolder == false ? "jstree-file" : "",
                             data = spr.IsFolder == false ? new
                             {
                                 artikl = spr.Артикул,
                                 brend = spr.Производитель,
                                 price = spr.Цена.Оптовая.ToString("C")
                             } : null
                         })
                .OrderBy(x => x.text);
            return Json(items);
        }
        [HttpPost]
        public IActionResult ДобавитьВПодбор(string sessionKey, string id, string наименование, string артикул, string производитель, string цена, decimal количество)
        {
            HttpContext.Session.AddOrUpdateObjectAsJson(sessionKey, new Корзина 
            { 
                Id = id, 
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
                var item = корзина.Find(x => x.Id == id);
                if (item != null)
                {
                    корзина.Remove(item);
                    HttpContext.Session.SetObjectAsJson(key, корзина);
                }
            }
            return Ok();
        }
        [HttpPost]
        public async Task<PartialViewResult> ИнфоНоменклатуры(string id)
        {
            var entity = await _номенклатура.GetByIdAsync(id);
            ViewBag.Название = "арт. " + entity.Sp85.Trim() + " " + entity.Descr.Trim();
            ViewBag.Характеристики = entity.Sp8848.Trim();
            return PartialView("_ImageGallery", _номенклатура.GetImages(id));
        }
        [HttpGet]
        public async Task<IActionResult> GetImage(int rowId)
        {
            return File(await _номенклатура.GetImageAsync(rowId), "image/jpg");
        }
    }
}
