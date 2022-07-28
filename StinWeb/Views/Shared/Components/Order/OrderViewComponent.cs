using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using StinClasses;
using StinClasses.Models;
using StinClasses.Документы;
using StinClasses.Справочники;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Views.Shared.Components.Order
{
    public class ReportOrder
    {
        public StinClasses.Справочники.Order Order { get; set; }
        public int СостояниеПоРегистрам { get; set; }
        public string ИспользуемыеСкладыId { get; set; }
        public string ИспользуемыеСкладыНаименование { get; set; }
        public string АдресСтрокой { get; set; }
        public List<ФормаПредварительнаяЗаявкаТЧ> СоставЗаказа { get; set; }
    }
    public class OrderViewComponent : ViewComponent, IDisposable
    {
        private bool disposed = false;
        private StinDbContext _context;
        private IOrder _order;
        private IСклад _склад;
        private IПредварительнаяЗаявка _предварительнаяЗаявка;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _order.Dispose();
                    _склад.Dispose();
                    _предварительнаяЗаявка.Dispose();
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public OrderViewComponent(StinDbContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _order = new OrderEntity(context);
            _склад = new СкладEntity(context);
            _предварительнаяЗаявка = new ПредварительнаяЗаявка(context);
        }
        public async Task<IViewComponentResult> InvokeAsync(string orderId)
        {
            var order = await _order.ПолучитьOrder(orderId);
            var формаПредварительнаяЗаявка = await _предварительнаяЗаявка.GetФормаПредварительнаяЗаявкаByOrderId(orderId);
            var reportOrder = new ReportOrder();
            reportOrder.Order = order;
            reportOrder.СоставЗаказа = формаПредварительнаяЗаявка.ТабличнаяЧасть;
            DateTime limitDate = DateTime.Today.AddDays(-3);
            DateTime limitSelfDate = DateTime.Today.AddDays(-6);
            DateTime dateRegTA = _context.GetRegTA();
            var dataReg = (
                        (from r in _context.Rg4667s //ЗаказыЗаявки
                         join doc in _context.Dh2457s on r.Sp4664 equals doc.Iddoc
                         where r.Period == dateRegTA &&
                             doc.Sp13995 == orderId
                         group new { r, doc } by new { orderId = doc.Sp13995, маршрутName = doc.Sp11557, складId = doc.Sp4437 } into gr
                         where gr.Sum(x => x.r.Sp4666) != 0
                         select new { orderId = gr.Key.orderId, маршрутName = Convert.ToString(gr.Key.маршрутName.Trim()), складId = Convert.ToString(gr.Key.складId), statusOrder = 1 })
                        .Concat
                        (from r in _context.Rg4674s //Заявки
                         join doc in _context.Dh2457s on r.Sp4671 equals doc.Iddoc
                         where r.Period == dateRegTA &&
                             doc.Sp13995 == orderId
                         group new { r, doc } by new { orderId = doc.Sp13995, маршрутName = doc.Sp11557, складId = doc.Sp4437 } into gr
                         where gr.Sum(x => x.r.Sp4672) != 0
                         select new { orderId = gr.Key.orderId, маршрутName = Convert.ToString(gr.Key.маршрутName.Trim()), складId = Convert.ToString(gr.Key.складId), statusOrder = 2 })
                        .Concat
                        (from r in _context.Rg11973s //НаборНаСкладе
                         join doc in _context.Dh11948s on r.Sp11970 equals doc.Iddoc
                         where r.Period == dateRegTA &&
                              doc.Sp14003 == orderId
                         group new { r, doc } by new { orderId = doc.Sp14003, маршрутName = doc.Sp11935, status = doc.Sp11938, складId = r.Sp11967 } into gr
                         where gr.Sum(x => x.r.Sp11972) != 0
                         select new { orderId = gr.Key.orderId, маршрутName = Convert.ToString(gr.Key.маршрутName.Trim()), складId = Convert.ToString(gr.Key.складId), statusOrder = gr.Key.status == 1 ? 4 : 3 })
                        .Concat
                        (from o in _context.Sc13994s
                         where o.Id == orderId && !o.Ismark && (o.Sp13982 == 5) &&
                            (((o.Sp13988 == (decimal)StinDeliveryType.PICKUP) &&
                            (o.Sp13990 >= limitSelfDate) &&
                            ((StinDeliveryPartnerType)o.Sp13985 == StinDeliveryPartnerType.SHOP)) ||
                            (o.Sp13990 >= limitDate))
                         select new { orderId = o.Id, маршрутName = string.Empty, складId = string.Empty, statusOrder = 5 }))
                         .AsEnumerable();
            reportOrder.СостояниеПоРегистрам = dataReg.Min(x => x.statusOrder);
            List<string> usedStocks = dataReg.Select(x => x.складId).Distinct().ToList();
            reportOrder.ИспользуемыеСкладыId = string.Join(',', usedStocks);
            foreach (var складId in usedStocks)
            {
                var skl = await _склад.GetEntityByIdAsync(складId);
                if (skl != null)
                    reportOrder.ИспользуемыеСкладыНаименование = reportOrder.ИспользуемыеСкладыНаименование.ConditionallyAppend(skl.Наименование);
            }
            string строкаАдреса = "";
            if (reportOrder.Order != null && reportOrder.Order.Address != null)
            {
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.Postcode))
                    строкаАдреса = строкаАдреса.ConditionallyAppend(reportOrder.Order.Address.Postcode);
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.Country))
                    строкаАдреса = строкаАдреса.ConditionallyAppend(reportOrder.Order.Address.Country);
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.City))
                    строкаАдреса = строкаАдреса.ConditionallyAppend(reportOrder.Order.Address.City);
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.Street))
                    строкаАдреса = строкаАдреса.ConditionallyAppend("ул." + reportOrder.Order.Address.Street);
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.House))
                    строкаАдреса = строкаАдреса.ConditionallyAppend("д." + reportOrder.Order.Address.House);
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.Block))
                    строкаАдреса = строкаАдреса.ConditionallyAppend("корп." + reportOrder.Order.Address.Block);
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.Entrance))
                    строкаАдреса = строкаАдреса.ConditionallyAppend("подъезд " + reportOrder.Order.Address.Entrance);
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.Entryphone))
                    строкаАдреса = строкаАдреса.ConditionallyAppend("домофон " + reportOrder.Order.Address.Entryphone);
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.Floor))
                    строкаАдреса = строкаАдреса.ConditionallyAppend("эт." + reportOrder.Order.Address.Floor);
                if (!string.IsNullOrWhiteSpace(reportOrder.Order.Address.Apartment))
                    строкаАдреса = строкаАдреса.ConditionallyAppend("кв." + reportOrder.Order.Address.Apartment);
            }
            reportOrder.АдресСтрокой = строкаАдреса;
            var типыОплат = new List<Tuple<int, string>>();
            типыОплат.Add(new((int)StinClasses.ReceiverPaymentType.Наличными, "Наличными"));
            типыОплат.Add(new((int)StinClasses.ReceiverPaymentType.БанковскойКартой, "Банковской картой"));
            ViewBag.PaymentTypes = new SelectList(типыОплат.AsEnumerable(), "Item1", "Item2");
            return View(reportOrder);
        }
    }
}
