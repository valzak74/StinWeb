using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.DataManager;

namespace StinWeb.ViewComponents
{
    public class КорзинаViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string key, bool modalVersion, bool isReadOnly, bool showАртикул, bool showПроизводитель, bool showЕдиницы, bool showЦены)
        {
            var data = new ДанныеКорзины(HttpContext.Session.GetObjectFromJson<List<Корзина>>(key));
            data.Key = key;
            data.modalVersion = modalVersion;
            data.isReadOnly = isReadOnly;
            data.ShowАртикул = showАртикул;
            data.ShowПроизводитель = showПроизводитель;
            data.ShowЕдиницы = showЕдиницы;
            data.ShowЦены = showЦены;

            return View(data);
        }
    }
}
