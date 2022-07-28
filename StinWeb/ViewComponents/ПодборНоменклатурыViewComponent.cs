using Microsoft.AspNetCore.Mvc;
using StinClasses.Models;

namespace StinWeb.ViewComponents
{
    public class ПодборНоменклатурыViewComponent: ViewComponent
    {
        private readonly StinDbContext _context;
        public ПодборНоменклатурыViewComponent(StinDbContext context)
        {
            this._context = context;
        }
        public IViewComponentResult Invoke(string sessionKey)
        {
            return View("Default", sessionKey);
        }
    }
}
