using Microsoft.AspNetCore.Mvc;

namespace StinWeb.ViewComponents
{
    public class РаботыViewComponent : ViewComponent
    {
        //private readonly ILogger _logger;
        //private readonly StinDbContext _context;

        //public РаботыViewComponent(StinDbContext context, ILogger<РаботыViewComponent> logger)
        //{
        //    _context = context;
        //    _logger = logger;
        //}
        public РаботыViewComponent()
        { }
        public IViewComponentResult Invoke()
        {
            return View();
        }
        //public IViewComponentResult Invoke(int гарантия, string изделиеId)
        //{
        //    IQueryable<Работа> table;
        //    if (гарантия == 0)
        //    {
        //        table = from sc9875 in _context.Sc9875
        //                join sc11498 in _context.Sc11498 on sc9875.Id equals sc11498.Parentext into _sc11498
        //                from sc11498 in _sc11498.DefaultIfEmpty()
        //                where sc9875.Ismark == false && sc9875.Isfolder == 2 && sc11498 == null
        //                select new Работа
        //                {
        //                    Id = sc9875.Id,
        //                    id = sc9875.Id,
        //                    parent = sc9875.Parentid == Common.ПустоеЗначение ? "#" : sc9875.Parentid,
        //                    text = sc9875.Descr.Trim(),
        //                    Артикул = sc9875.Sp11503.Trim(),
        //                    АртикулОригинал = sc9875.Sp12644.Trim(),
        //                    Наименование = sc9875.Descr.Trim()
        //                };

        //    }
        //    else
        //        table = from sc9875 in _context.Sc9875
        //                join sc11498 in _context.Sc11498 on sc9875.Id equals sc11498.Parentext into _sc11498
        //                from sc11498 in _sc11498.DefaultIfEmpty()
        //                join sc84 in _context.Sc84 on sc11498.Sp11496 equals sc84.Sp8842 into _sc84
        //                from sc84 in _sc84.DefaultIfEmpty()
        //                where sc9875.Ismark == false && sc9875.Isfolder == 2 && sc11498 != null && sc84 != null && sc84.Id == изделиеId
        //                select new Работа
        //                {
        //                    Id = sc9875.Id,
        //                    Артикул = sc9875.Sp11503.Trim(),
        //                    АртикулОригинал = sc9875.Sp12644.Trim(),
        //                    Наименование = sc9875.Descr.Trim()
        //                };

        //    return View(table);
        //}
    }
}
