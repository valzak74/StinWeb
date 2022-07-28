using System.Linq;
using StinWeb.Models.DataManager.Справочники;
using Microsoft.AspNetCore.Mvc;
using StinClasses.Models;

namespace StinWeb.ViewComponents
{
    public class НоменклатураViewComponent : ViewComponent
    {
        private readonly StinDbContext _context;

        public НоменклатураViewComponent(StinDbContext context)
        {
            _context = context;
        }
        public IViewComponentResult Invoke()
        {
            var table = from sc84 in _context.Sc84s
                        where sc84.Isfolder == 2 && sc84.Ismark == false
                        select new Номенклатура { 
                            Id = sc84.Id,
                            Code = sc84.Code,
                            Артикул = sc84.Sp85.Trim(),
                            Наименование = sc84.Descr.Trim()
                        };
            return View(table);
        }
    }
}
