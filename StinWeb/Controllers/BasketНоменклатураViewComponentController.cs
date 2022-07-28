using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class BasketНоменклатураViewComponentController : Controller
    {
        private readonly StinDbContext _context;
        private int isExist(string id)
        {
            List<BasketНоменклатура> cart = Common.GetObjectFromJson<List<BasketНоменклатура>>(HttpContext.Session, "cart");
            for (int i = 0; i < cart.Count; i++)
            {
                if ((cart[i].Номенклатура != null) && (cart[i].Номенклатура.Id.Equals(id)))
                {
                    return i;
                }
            }
            return -1;
        }
        public BasketНоменклатураViewComponentController(StinDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult AddBasket(string id)
        {
            Номенклатура номенклатура = (
                from sc84 in _context.Sc84s
                where sc84.Id == id
                select new Номенклатура
                {
                    Id = sc84.Id,
                    Code = sc84.Code,
                    Артикул = sc84.Sp85.Trim(),
                    Наименование = sc84.Descr.Trim()
                }).FirstOrDefault();
            if (Common.GetObjectFromJson<List<BasketНоменклатура>>(HttpContext.Session, "cart") == null)
            {
                List<BasketНоменклатура> cart = new List<BasketНоменклатура>();
                cart.Add(new BasketНоменклатура { Номенклатура = номенклатура, Quantity = 1 });
                Common.SetObjectAsJson(HttpContext.Session, "cart", cart);
            }
            else
            {
                List<BasketНоменклатура> cart = Common.GetObjectFromJson<List<BasketНоменклатура>>(HttpContext.Session, "cart");
                int index = isExist(id);
                if (index != -1)
                {
                    cart[index].Quantity++;
                }
                else
                {
                    cart.Add(new BasketНоменклатура { Номенклатура = номенклатура, Quantity = 1 });
                }
                Common.SetObjectAsJson(HttpContext.Session, "cart", cart);
            }
            return ViewComponent("BasketНоменклатура");
        }
        [HttpPost]
        public IActionResult Remove(string id)
        {
            List<BasketНоменклатура> cart = Common.GetObjectFromJson<List<BasketНоменклатура>>(HttpContext.Session, "cart");
            int index = isExist(id);
            cart.RemoveAt(index);
            Common.SetObjectAsJson(HttpContext.Session, "cart", cart);
            return ViewComponent("BasketНоменклатура");
        }
    }
}
