using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinWeb.Models.DataManager;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinWeb.ViewComponents
{
    public class Rg9972DetailsViewComponent : ViewComponent
    {
        private readonly StinDbContext _context;

        public Rg9972DetailsViewComponent(StinDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync(string id)
        {
            var details = 
                from rg9972 in _context.Rg9972s
                 join sc172 in _context.Sc172s on rg9972.Sp9964 equals sc172.Id into _sc172
                 from sc172 in _sc172.DefaultIfEmpty()
                 join sc55 in _context.Sc55s on rg9972.Sp10083 equals sc55.Id into _sc55
                 from sc55 in _sc55.DefaultIfEmpty()
                 join sc84 in _context.Sc84s on rg9972.Sp9960 equals sc84.Id
                 where rg9972.Period == Common.GetRegTA(_context) && rg9972.Sp9970 > 0 && rg9972.Sp9969 + "-" + rg9972.Sp10084.ToString() == id
                 select new ПартииМастерской
                 {
                     Квитанция = rg9972.Sp9969 + "-" + rg9972.Sp10084.ToString(),
                     Изделие = sc84.Descr.Trim(),
                     Артикул = sc84.Sp85.Trim(),
                     ТипРемонта = Common.ПолучитьТипРемонта(Convert.ToInt32(rg9972.Sp9958)) ?? "не распознан",
                     Заказчик = sc172.Descr.Trim() ?? sc55.Descr.Trim()
                 };

            return View(await details.FirstOrDefaultAsync());
        }
    }
}
