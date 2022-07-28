using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using StinWeb.Models.DataManager;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.Repository.Документы;
using StinClasses.Models;

namespace StinWeb.Controllers.Документы
{
    public class АктВыполненныхРаботController : Controller
    {
        private StinDbContext _context;
        private IАвансоваяОплата _авансоваяОплата;
        public АктВыполненныхРаботController(StinDbContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _авансоваяОплата = new АвансоваяОплатаRepository(context);
        }
        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            _авансоваяОплата.Dispose();
            base.Dispose(disposing);
        }
        [HttpPost("ПечатьАктВыполненныхРабот")]
        [Authorize]
        public async Task<IActionResult> ПечатьАктВыполненныхРабот(ФормаАвансоваяОплата doc)
        {
            string message = "";
            try
            {
                if (doc != null && doc.Ошибка == null)
                {
                    if (doc.ТабличнаяЧасть == null || doc.ТабличнаяЧасть.Count == 0)
                    {
                        List<Корзина> авансовыеРаботы = HttpContext.Session.GetObjectFromJson<List<Корзина>>("ПодборРабот");
                        if (авансовыеРаботы != null && авансовыеРаботы.Count > 0)
                        {
                            doc.ТабличнаяЧасть = авансовыеРаботы.Select(x => new тчАвансоваяОплата 
                            {
                                Работа = new Работа { Id = x.Id, Наименование = x.Наименование },
                                Количество = x.Quantity,
                                Цена = x.Цена,
                                Сумма = x.Сумма
                            }).ToList();
                        }
                    }
                    message = message.CreateOrUpdateHtmlPrintPage("АктВыполненныхРабот", await _авансоваяОплата.ДанныеДляПечатиAsync(doc));
                }    
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return BadRequest(new ExceptionData { Code = ex.HResult, Description = ex.Message });
            }
            return Ok(message);
        }
    }
}
