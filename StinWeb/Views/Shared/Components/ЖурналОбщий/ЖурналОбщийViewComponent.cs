using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.Repository.Документы;
using StinWeb.Models.DataManager.Документы;
using StinClasses.Models;

namespace StinWeb.Views.Shared.Components.ЖурналОбщий
{
    public class ЖурналОбщийViewComponent : ViewComponent 
    {
        private IДокумент _документRepository;
        public ЖурналОбщийViewComponent(StinDbContext context)
        {
            _документRepository = new ДокументRepository(context);
        }
        public IViewComponentResult Invoke(DateTime startDate, DateTime endDate)
        {
            if (startDate == DateTime.MinValue)
                startDate = DateTime.Now.Date;
            if (endDate == DateTime.MinValue)
                endDate = DateTime.Now;
            var result = _документRepository.ЖурналДокументов(startDate, endDate, null);
            if (!result.Any())
                result = Enumerable.Empty<ОбщиеРеквизиты>().AsQueryable();
            return View(result);
        }
    }
}
