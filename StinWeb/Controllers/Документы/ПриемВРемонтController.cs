using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StinWeb.Models.DataManager;
using StinWeb.Models.Repository.Интерфейсы.Документы;
using StinWeb.Models.DataManager.Документы.Мастерская;
using StinWeb.Models.Repository.Интерфейсы;
using StinWeb.Models.Repository.Справочники;
using StinWeb.Models.Repository.Документы;
using StinWeb.Models.DataManager.Справочники;
using Microsoft.Extensions.DependencyInjection;
using StinClasses.Models;

namespace StinWeb.Controllers.Документы
{
    public class ПриемВРемонтController : Controller
    {
        private StinDbContext _context;
        private IСклад _складRepository;
        private IПриемВРемонт _приемВРемонт;
        private IАвансоваяОплата _авансоваяОплата;
        private IНаДиагностику _наДиагностику;
        public ПриемВРемонтController(StinDbContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _складRepository = new СкладRepository(context);
            _приемВРемонт = new ПриемВРемонтRepository(context, serviceScopeFactory);
            _авансоваяОплата = new АвансоваяОплатаRepository(context);
            _наДиагностику = new НаДиагностикуRepository(context);
        }
        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            _складRepository.Dispose();
            _приемВРемонт.Dispose();
            _авансоваяОплата.Dispose();
            _наДиагностику.Dispose();
            base.Dispose(disposing);
        }
        [HttpPost("ПечатьПриемВРемонт")]
        [Authorize]
        public async Task<IActionResult> ПечатьПриемВРемонт(ФормаПриемВРемонт doc)
        {
            string message = "";
            try
            {
                var СкладДляРемонта = _складRepository.КонстантаСкладДляРемонта();
                if (doc.Склад.Id == СкладДляРемонта.Id && doc.СтатусПартии == "Принят в ремонт")
                {
                    var ДанныеДляПечати = await _приемВРемонт.ДанныеДляПечатиAsync(doc);
                    message = message.CreateOrUpdateHtmlPrintPage("Квитанция о Приеме в ремонт", ДанныеДляПечати);
                    //message = message.CreateOrUpdateHtmlPrintPage("ПриемВРемонтДляМастера", ДанныеДляПечати);
                }
                else
                    return BadRequest(new ExceptionData { Code = 0, Description = "Печать акта возможна только при приеме в мастерскую" });
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return BadRequest(new ExceptionData { Code = ex.HResult, Description = ex.Message });
            }
            return Ok(message);
        }
        [HttpPost("СоздатьПриемВРемонт")]
        [Authorize]
        public async Task<IActionResult> СоздатьПриемВРемонт(ФормаПриемВРемонт doc)
        {
            ExceptionData result = null;
            string message = "";
            using var docTran = await _context.Database.BeginTransactionAsync();
            try
            {
                var СкладДляРемонта = _складRepository.КонстантаСкладДляРемонта();
                ФормаАвансоваяОплата docАвансоваяОплата = null;
                result = await _приемВРемонт.ЗаписатьПровестиAsync(doc);
                if (result == null && doc.Склад.Id == СкладДляРемонта.Id && doc.СтатусПартии == "Принят в ремонт")
                {
                    List<Корзина> авансовыеРаботы = HttpContext.Session.GetObjectFromJson<List<Корзина>>("ПодборРабот");
                    if (авансовыеРаботы != null && авансовыеРаботы.Count > 0)
                    {
                        docАвансоваяОплата = await _авансоваяОплата.ВводНаОснованииAsync(doc.Общие.IdDoc, doc.Общие.ВидДокумента10, doc.Общие.Автор.Id, авансовыеРаботы);
                        if (docАвансоваяОплата.Ошибка == null)
                            result = await _авансоваяОплата.ЗаписатьПровестиAsync(docАвансоваяОплата);
                    }

                    if (result == null)
                    {
                        var docНаДиагностику = await _наДиагностику.ВводНаОснованииAsync(doc.Общие.IdDoc, doc.Общие.ВидДокумента10, doc.Общие.Автор.Id, авансовыеРаботы);
                        if (docНаДиагностику.Ошибка == null)
                            result = await _наДиагностику.ЗаписатьПровестиAsync(docНаДиагностику);
                    }
                }
                if (result == null && doc.Склад.Id == СкладДляРемонта.Id && doc.СтатусПартии == "Принят в ремонт")
                {
                    var ДанныеДляПечати = await _приемВРемонт.ДанныеДляПечатиAsync(doc);
                    message = message.CreateOrUpdateHtmlPrintPage("Квитанция о Приеме в ремонт", ДанныеДляПечати);
                    //message = message.CreateOrUpdateHtmlPrintPage("ПриемВРемонтДляМастера", ДанныеДляПечати);
                    if (docАвансоваяОплата != null && docАвансоваяОплата.Ошибка == null)
                        message = message.CreateOrUpdateHtmlPrintPage("АктВыполненныхРабот", await _авансоваяОплата.ДанныеДляПечатиAsync(docАвансоваяОплата));
                    result = await _приемВРемонт.ОтправитьСообщенияAsync(doc);
                }
                if (result == null)
                { 
                    docTran.Commit();
                    HttpContext.Session.SetObjectAsJson("ПодборРабот", null);
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
