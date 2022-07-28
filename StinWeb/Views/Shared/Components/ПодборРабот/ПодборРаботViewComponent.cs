using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.DataManager;

namespace StinWeb.ViewComponents
{
    public class ПодборРаботViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(List<КорзинаРабот> data)
        {
            return View(data);
        }
    }
}
