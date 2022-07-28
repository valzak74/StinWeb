using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.Repository.Документы;
using StinClasses.Models;

namespace StinWeb.Controllers.Документы
{
    public class ИзменениеСтатусаController : Controller
    {
        private StinDbContext _context;
        private IИзменениеСтатуса _изменениеСтатуса;
        public ИзменениеСтатусаController(StinDbContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _изменениеСтатуса = new ИзменениеСтатусаRepository(context);
        }
        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            _изменениеСтатуса.Dispose();
            base.Dispose(disposing);
        }
        [HttpPost("СоздатьИзменениеСтатуса")]
        [Authorize]
        public async Task<IActionResult> СоздатьИзменениеСтатуса(ФормаИзменениеСтатуса doc)
        {
            ExceptionData result = null;
            string message = "";
            using var docTran = await _context.Database.BeginTransactionAsync();
            try
            {
                result = await _изменениеСтатуса.ЗаписатьПровестиAsync(doc);
                if (result == null)
                {
                    docTran.Commit();
                }
                else
                {
                    if (_context.Database.CurrentTransaction != null)
                        docTran.Rollback();
                }
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return BadRequest(new ExceptionData { Code = db_ex.HResult, Description = db_ex.InnerException.ToString() });
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return BadRequest(new ExceptionData { Code = ex.HResult, Description = ex.Message });
            }
            if (result == null)
                return Ok(message);
            else
                return BadRequest(result);
        }
    }
}
