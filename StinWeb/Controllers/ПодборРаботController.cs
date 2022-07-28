using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Справочники;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class ПодборРаботController : Controller
    {
        private readonly StinDbContext _context;
        public ПодборРаботController(StinDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public PartialViewResult IndexКорзинаРабот(string viewName)
        {
            var basket = Common.GetObjectFromJson<List<КорзинаРабот>>(HttpContext.Session, "ПодборРабот");
            if (basket == null)
                basket = new List<КорзинаРабот>();
            return PartialView(viewName, basket);
        }
        public JsonResult GetTableLevel(string id, string izdelieId, bool garantia)
        {
            string Производитель = "";
            if (garantia)
            {
                Производитель = (from sc84 in _context.Sc84s
                                 where sc84.Id == izdelieId
                                 select sc84.Sp8842).FirstOrDefault();
            }
            var rabList = _context.Sc9875s.FromSqlRaw(@"
            WITH cte_org AS (
                SELECT       
                    s.*
        
                FROM       
                    SC9875 s
	            left join SC11498 c on s.ID = c.PARENTEXT
                WHERE s.ISMARK = 0 and s.ISFOLDER = 2 and " + (garantia == false ? "c.ID is null" : "c.sp11496 = '" + Производитель + "'") + @"
                UNION ALL
                SELECT 
                    e.*
                FROM 
                    SC9875 e
                    INNER JOIN cte_org o 
                        ON o.PARENTID = e.ID
            )
            SELECT distinct * FROM cte_org where Parentid = @ParentId", new SqlParameter("@ParentId", id == "#" ? Common.ПустоеЗначение : id.Replace('_', ' '))).ToList();
            var items = (from spr in rabList
                         from con in _context._1sconsts.Where(x => x.Id == 9872 && x.Objid == spr.Id).OrderByDescending(z => z.Date).Take(1)
                         select new
                         {
                             id = spr.Id.Replace(' ', '_'),
                             parent = id,
                             text = spr.Descr.Trim(),
                             children = spr.Isfolder == 1,
                             icon = spr.Isfolder == 2 ? "jstree-file" : "",
                             data = spr.Isfolder == 2 ? new
                             {
                                 price = con.Value ?? "0"
                             } : null
                         })
                .OrderBy(x => x.text);
            return Json(items);
        }

        [HttpPost]
        public IActionResult ДобавитьВПодбор(string id, decimal цена)
        {
            Работа работа = (
                from sc9875 in _context.Sc9875s
                where sc9875.Id == id.Replace('_', ' ')
                select new Работа
                {
                    Id = sc9875.Id,
                    Артикул = sc9875.Sp11503.Trim(),
                    АртикулОригинал = sc9875.Sp12644.Trim(),
                    Наименование = sc9875.Descr.Trim(),
                    Цена = цена
                }).FirstOrDefault();
            if (Common.GetObjectFromJson<List<КорзинаРабот>>(HttpContext.Session, "ПодборРабот") == null)
            {
                List<КорзинаРабот> cart = new List<КорзинаРабот>();
                cart.Add(new КорзинаРабот { Работа = работа, Quantity = 1, Цена = работа.Цена, Сумма = работа.Цена });
                Common.SetObjectAsJson(HttpContext.Session, "ПодборРабот", cart);
            }
            else
            {
                List<КорзинаРабот> cart = Common.GetObjectFromJson<List<КорзинаРабот>>(HttpContext.Session, "ПодборРабот");
                int index = isExist(id.Replace('_', ' '));
                if (index != -1)
                {
                    cart[index].Quantity++;
                    cart[index].Сумма = cart[index].Цена * cart[index].Quantity;
                }
                else
                {
                    cart.Add(new КорзинаРабот { Работа = работа, Quantity = 1, Цена = работа.Цена, Сумма = работа.Цена });
                }
                Common.SetObjectAsJson(HttpContext.Session, "ПодборРабот", cart);
            }
            return Ok();
        }
        [HttpPost]
        public IActionResult УдалитьИзПодбора(string id = "")
        {
            List<КорзинаРабот> cart = Common.GetObjectFromJson<List<КорзинаРабот>>(HttpContext.Session, "ПодборРабот");
            if (string.IsNullOrEmpty(id))
            {
                cart.Clear();
                Common.SetObjectAsJson(HttpContext.Session, "ПодборРабот", null);
            }
            else
            {
                int index = isExist(id);
                cart.RemoveAt(index);
                Common.SetObjectAsJson(HttpContext.Session, "ПодборРабот", cart);
            }
            return Ok();
        }
        private int isExist(string id)
        {
            List<КорзинаРабот> cart = Common.GetObjectFromJson<List<КорзинаРабот>>(HttpContext.Session, "ПодборРабот");
            for (int i = 0; i < cart.Count; i++)
            {
                if ((cart[i].Работа != null) && (cart[i].Работа.Id.Equals(id)))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
