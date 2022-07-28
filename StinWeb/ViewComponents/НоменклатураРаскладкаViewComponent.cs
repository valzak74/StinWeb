using System.Linq;
using StinWeb.Models.DataManager.Справочники;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinWeb.ViewComponents
{
    public class НоменклатураРаскладкаViewComponent : ViewComponent
    {
        private readonly StinDbContext _context;
        public НоменклатураРаскладкаViewComponent(StinDbContext context)
        {
            _context = context;
        }
        public IViewComponentResult Invoke()
        {
            var table = from sc84 in _context.Sc84s
                        join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                        from sc8840 in _sc8840.DefaultIfEmpty()
                        where sc84.Isfolder == 2 && sc84.Ismark == false
                        select new Номенклатура
                        {
                            Id = sc84.Id,
                            Code = sc84.Code,
                            Производитель = sc8840.Descr.Trim() ?? "<не указан>",
                            Артикул = sc84.Sp85.Trim(),
                            Наименование = sc84.Descr.Trim()
                        };
            return View(table.AsNoTracking());
        }
    }
}
