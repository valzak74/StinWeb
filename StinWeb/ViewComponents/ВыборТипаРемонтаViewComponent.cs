using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager;

namespace StinWeb.ViewComponents
{
    public class ВыборТипаРемонтаViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            //ViewBag.Гарантия = гарантия;
            int ТипРемонта;
            try
            {
                ТипРемонта = Common.GetObjectFromJson<int>(HttpContext.Session, "ТипРемонта");
            }
            catch
            {
                ТипРемонта = 0;
            }
            string Требуется = Common.GetObjectFromJson<string>(HttpContext.Session, "Описание");

            ViewBag.Гарантийный = (ТипРемонта == 1 || ТипРемонта == 2);
            ViewBag.Описание = Требуется;
            return View();
        }
    }
}
