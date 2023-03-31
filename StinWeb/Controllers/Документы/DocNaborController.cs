using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StinClasses.Models;
using StinClasses.Справочники;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StinClasses;

namespace StinWeb.Controllers.Документы
{
    public class DocNaborController : Controller
    {
        bool disposed = false;
        StinDbContext _context;
        StinClasses.Справочники.IКладовщик _storekeeper;
        StinClasses.Справочники.IСообщения _сообщения;
        StinClasses.Документы.IНабор _набор;
        public DocNaborController(StinDbContext context)
        {
            _context = context;
            _storekeeper = new КладовщикEntity(context);
            _сообщения = new СообщенияEntity(context);
            _набор = new StinClasses.Документы.Набор(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _storekeeper.Dispose();
                    _сообщения.Dispose();
                    _набор.Dispose();
                    _context.Dispose();
                    base.Dispose(disposing);
                }
            }
            this.disposed = true;
        }
        public async Task<IActionResult> ManageStorekeeper()
        {
            var dataStorekeepers = (await _storekeeper.GetAll("", true)).Select(x => new { Id = x.Id.Replace(' ', '_'), x.Наименование }).ToList();
            dataStorekeepers.Insert(0, new { Id = "", Наименование = "<< НЕ ВЫБРАН >>" });
            
            ViewBag.Storekeepers = new SelectList(dataStorekeepers, "Id", "Наименование");
            return View("ManageStorekeeper");
        }
        [HttpPost]
        public async Task<IActionResult> NaborScan(string barcodeText, string storekeeperId, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(barcodeText) && ((barcodeText.Length == 13) || (barcodeText.Length == 14)) && (barcodeText.Substring(0, 4) == "%97W"))
            {
                var docId = barcodeText.Substring(4).Replace('%', ' ');
                if (barcodeText.Length == 14)
                    docId = docId.Remove(docId.Length - 1);
                var formNabor = await _набор.GetФормаНаборById(docId);
                if (formNabor == null)
                    return StatusCode(502, "Не удалось получить форму набора");
                if (!formNabor.Общие.Проведен)
                    return StatusCode(502, "Набор не проведен");
                if (formNabor.Завершен)
                    return StatusCode(502, "Набор уже завершен");
                if (formNabor.Order?.InternalStatus == 5)
                    return StatusCode(502, "Заказ уже отменен");
                if (!_набор.IsActive(docId))
                    return StatusCode(502, "Набор отменен");
                storekeeperId = storekeeperId?.Replace('_', ' ');
                if (barcodeText.Length == 13) //НачатьСборку
                {
                    if ((formNabor.Кладовщик?.Id == storekeeperId) && (formNabor.StartCompectation > Common.min1cDate))
                        return StatusCode(502, "Набор уже начат в " + formNabor.StartCompectation.ToString("dd-MM-yyyy HH:mm:ss"));
                    if (formNabor.StartCompectation <= Common.min1cDate)
                        formNabor.StartCompectation = DateTime.Now;
                }
                else //Закончить Сборку
                {
                    if (formNabor.StartCompectation <= Common.min1cDate)
                        return StatusCode(502, "Набор не начат");
                    formNabor.Завершен = true;
                    formNabor.EndComplectation = DateTime.Now;
                }
                await _набор.ОбновитьКладовщика(formNabor, storekeeperId);
                var реквизитыПроведенныхДокументов = new List<StinClasses.Документы.ОбщиеРеквизиты>();
                using var tran = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    StinClasses.Документы.ExceptionData result;
                    if (barcodeText.Length == 13) //НачатьСборку
                        result = await _набор.ЗаписатьAsync(formNabor);
                    else
                        result = await _набор.ЗаписатьПровестиAsync(formNabor);
                    if (result != null)
                    {
                        if (_context.Database.CurrentTransaction != null)
                            tran.Rollback();
                        return StatusCode(502, result.Description);
                    }
                    else
                    {
                        if (formNabor.Завершен && (formNabor.Общие.Автор.Id != Common.UserRobot))
                        {
                            var sbSubject = new StringBuilder("Набор готов (");
                            sbSubject.Append(formNabor.Контрагент.Наименование);
                            sbSubject.Append(" ");
                            sbSubject.Append(formNabor.Склад.Наименование);
                            sbSubject.Append("/");
                            sbSubject.Append(formNabor.ПодСклад.Наименование);
                            sbSubject.Append(")");
                            var sbMessage = new StringBuilder("Набор ");
                            sbMessage.Append(formNabor.Общие.НомерДок);
                            sbMessage.Append(" от ");
                            sbMessage.Append(formNabor.Общие.ДатаДок.ToString("dd.MM.yyyy"));
                            sbMessage.AppendLine(" готов");
                            sbMessage.Append("Контрагент: ");
                            sbMessage.AppendLine(formNabor.Контрагент.Наименование);
                            sbMessage.Append("Склад: ");
                            sbMessage.AppendLine(formNabor.Склад.Наименование);
                            sbMessage.Append("Место хранения: ");
                            sbMessage.AppendLine(formNabor.ПодСклад.Наименование);
                            sbMessage.Append("Клиент приглашается за товаром!");
                            await _сообщения.SendMessage(sbSubject.ToString(), sbMessage.ToString(), formNabor.Общие.Автор.Id, Common.UserRobot);
                        }
                        реквизитыПроведенныхДокументов.Add(formNabor.Общие);
                        if (barcodeText.Length == 13) //НачатьСборку
                            await _набор.ОбновитьСетевуюАктивность();
                        else
                            await _набор.ОбновитьАктивность(реквизитыПроведенныхДокументов);
                    }
                    if (_context.Database.CurrentTransaction != null)
                        tran.Commit();

                    string infoClass = barcodeText.Length == 13 ? "text-primary" : "text-success";
                    string info = barcodeText.Length == 13 ? "СБОРКА НАЧАТА" : "ГОТОВ";
                    var sb = new StringBuilder();
                    sb.Append("<h3>Набор №");
                    sb.Append(formNabor.Общие.НомерДок);
                    sb.Append(" от ");
                    sb.Append(formNabor.Общие.ДатаДок.ToString("dd-MM-yyyy"));
                    sb.Append("</h3>");
                    sb.Append("<h3>Контрагент: ");
                    sb.Append(formNabor.Контрагент.Наименование);
                    sb.Append("</h3>");
                    sb.Append("<hr>");
                    sb.Append($"<h1 class='{infoClass}' align='center'>{formNabor.Кладовщик?.Наименование ?? ""} {info}</h1>");
                    return Ok(sb.ToString());
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return StatusCode(502, ex.Message);
                }
            }
            return StatusCode(502, "Недопустимое значение штрихкода");
        }
    }
}
