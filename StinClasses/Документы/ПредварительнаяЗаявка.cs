using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using StinClasses.Регистры;
using StinClasses.Справочники;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses.Документы
{
    public class ФормаПредварительнаяЗаявка
    {
        public ExceptionData Ошибка { get; set; }
        public ОбщиеРеквизиты Общие { get; set; }
        public Склад Склад { get; set; }
        public Контрагент Контрагент { get; set; }
        public Договор Договор { get; set; }
        public БанковскийСчет БанковскийСчет { get; set; }
        public bool ОблагаетсяЕНВД { get; set; }
        public bool УчитыватьНДС { get; set; }
        public bool СуммаВклНДС { get; set; }
        public ТипЦен ТипЦен { get; set; }
        public Скидка Скидка { get; set; }
        public СкидКарта СкидКарта { get; set; }
        public DateTime ДатаОплаты { get; set; }
        public DateTime ДатаОтгрузки { get; set; }
        public string СпособОтгрузки { get; set; }
        public Маршрут Маршрут { get; set; }
        public Order Order { get; set; }
        public List<ФормаПредварительнаяЗаявкаТЧ> ТабличнаяЧасть { get; set; }
        public ФормаПредварительнаяЗаявка()
        {
            Общие = new ОбщиеРеквизиты();
            ТабличнаяЧасть = new List<ФормаПредварительнаяЗаявкаТЧ>();
        }
    }
    public class ФормаПредварительнаяЗаявкаТЧ
    {
        public Номенклатура Номенклатура { get; set; }
        public string НоменклатураMarketplaceId { get; set; }
        public string WarehouseId { get; set; }
        public string PartnerWarehouseId { get; set; }
        public bool Delivery { get; set; }
        public decimal Количество { get; set; }
        public Единица Единица { get; set; }
        public decimal Цена { get; set; }
        public decimal Сумма { get; set; }
        public decimal СуммаСоСкидкой { get; set; }
        public СтавкаНДС СтавкаНДС { get; set; }
        public decimal СуммаНДС { get; set; }
        public decimal Себестоимость { get; set; }
    }
    public interface IПредварительнаяЗаявка : IДокумент
    {
        Task<ФормаПредварительнаяЗаявка> НовыйДокумент(DateTime docDateTime, string фирмаId, string складId, string контрагентId, string договорId,
            string типЦенId, Order order, List<Номенклатура> НоменклатураEntities, OrderRecipientAddress address);
        Task<ExceptionData> ЗаписатьAsync(ФормаПредварительнаяЗаявка doc);
        Task<ExceptionData> ПровестиAsync(ФормаПредварительнаяЗаявка doc);
        Task<List<Tuple<string, string, List<ФормаПредварительнаяЗаявкаТЧ>>>> РаспределитьТоварПоНаличиюAsync(ФормаПредварительнаяЗаявка doc, List<string> CписокСкладовНаличияТовара);
        Task<ФормаПредварительнаяЗаявка> GetФормаПредварительнаяЗаявкаById(string idDoc);
        Task<ФормаПредварительнаяЗаявка> GetФормаПредварительнаяЗаявкаByOrderId(string orderId);
        Task ОбновитьНомерМаршрута(string orderId, string маршрутНаименование);
    }
    public class ПредварительнаяЗаявка : Документ, IПредварительнаяЗаявка
    {
        //private IOrder _order;
        private IРегистрСпросОстатки _регистрСпросОстатки;
        private IРегистрMarketplaceOrders _регистрMarketplaceOrders;
        //private IРегистр_РезервыТМЦ _регистр_РезервыТМЦ;
        //private IРегистр_ПартииНаличие _регистр_ПартииНаличие;
        //private IРегистр_НаборНаСкладе _регистр_НаборНаСкладе;
        //private IКасса _касса;
        //private IОтчетККМ _отчетККМ;
        public ПредварительнаяЗаявка(StinDbContext context) : base(context)
        {
            //_order = new OrderEntity(context);
            _регистрСпросОстатки = new Регистр_СпросОстатки(context);
            _регистрMarketplaceOrders = new Регистр_MarketplaceOrders(context);
            //_регистр_РезервыТМЦ = new Регистр_РезервыТМЦ(context);
            //_регистр_ПартииНаличие = new Регистр_ПартииНаличие(context);
            //_регистр_НаборНаСкладе = new Регистр_НаборНаСкладе(context);
            //_касса = new Касса(context);
            //_отчетККМ = new ОтчетККМ(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    //_order.Dispose();
                    _регистрСпросОстатки.Dispose();
                    _регистрMarketplaceOrders.Dispose();
                    //_регистр_РезервыТМЦ.Dispose();
                    //_регистр_ПартииНаличие.Dispose();
                    //_регистр_НаборНаСкладе.Dispose();
                    //_касса.Dispose();
                    //_отчетККМ.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        public async Task<ФормаПредварительнаяЗаявка> НовыйДокумент(DateTime docDateTime, string фирмаId, string складId, string контрагентId, string договорId,
            string типЦенId, Order order, List<Номенклатура> НоменклатураEntities, OrderRecipientAddress address)
        {
            var фирма = await _фирма.GetEntityByIdAsync(фирмаId);
            var договор = await _контрагент.GetДоговорAsync(договорId);
            Маршрут маршрут = null;
            string адресСтрокой = "";
            string предпочтительныйСпособОплаты = "";
            if (!((order.DeliveryPartnerType == StinDeliveryPartnerType.SHOP) && (order.DeliveryType == StinDeliveryType.PICKUP)))
            {
                string маршрутКод = _графикМаршрутов.ПолучитьКодМаршрута(order.ShipmentDate, "13"); //направление 13 == Самара
                if (!string.IsNullOrEmpty(маршрутКод))
                {
                    маршрут = _маршрут.НовыйЭлемент();
                    маршрут.Наименование = маршрутКод;
                }
                if (address != null)
                {
                    адресСтрокой = address.City + ", " + address.Street + ", " +
                        (!string.IsNullOrEmpty(address.House) ? "д." + address.House + ", " : "") +
                        (!string.IsNullOrEmpty(address.Apartment) ? "кв." + address.Apartment + ", " : "") +
                        (!string.IsNullOrEmpty(address.Block) ? "корп. " + address.Block + ", " : "") +
                        (!string.IsNullOrEmpty(address.Floor) ? "эт." + address.Floor + ", " : "");
                }
            }
            if ((order.DeliveryPartnerType == StinDeliveryPartnerType.SHOP) &&
                (order.PaymentType == StinPaymentType.POSTPAID) &&
                (order.DeliveryType != StinDeliveryType.PICKUP))
            {
                //предпочтительныйСпособОплаты = order.PaymentMethod ==
                switch (order.PaymentMethod)
                {
                    case StinPaymentMethod.CARD_ON_DELIVERY:
                        предпочтительныйСпособОплаты = "(картой) ";
                        break;
                    case StinPaymentMethod.CASH_ON_DELIVERY:
                        предпочтительныйСпособОплаты = "(наличными) ";
                        break;
                    default:
                        предпочтительныйСпособОплаты = "";
                        break;
                }
            }
            var датаДок = docDateTime <= Common.min1cDate ? DateTime.Now : docDateTime;
            var датаОплаты = датаДок.AddDays(договор.ГлубинаОтсрочки);
            var doc = new ФормаПредварительнаяЗаявка
            {
                Общие = new ОбщиеРеквизиты
                {
                    //IdDoc
                    ДокОснование = null,
                    Фирма = фирма,
                    Автор = await _пользователь.GetUserByIdAsync(Common.UserRobot),
                    ВидДокумента10 = 12747,
                    ВидДокумента36 = Common.Encode36(12747),
                    //НазваниеВЖурнале = Common.ВидыОперации.Where(x => x.Key == d.dh.Sp4760).Select(y => y.Value).FirstOrDefault(),
                    //НомерДок = d.j.Docno,
                    ДатаДок = датаДок,
                    Проведен = false,
                    //Комментарий = order.DeliveryPartnerType == DeliveryPartnerType.YANDEX_MARKET ? "Интернет-заказ " + order.Marketplace + " №" + order.MarketplaceId : ((order.DeliveryType == DeliveryType.PICKUP ? "Самовывоз, " : адресСтрокой) + (order.PaymentType == PaymentType.POSTPAID ? "Забрать деньги " : "")),
                    Удален = false
                },
                БанковскийСчет = фирма.Счет,
                Склад = await _склад.GetEntityByIdAsync(складId),
                Контрагент = await _контрагент.GetКонтрагентAsync(контрагентId),
                Договор = договор,
                ОблагаетсяЕНВД = false,
                УчитыватьНДС = фирма.ЮрЛицо.УчитыватьНДС == 1,
                СуммаВклНДС = true,
                ТипЦен = await _контрагент.GetТипЦенAsync(типЦенId),
                Скидка = null,
                ДатаОплаты = order.ShipmentDate < датаОплаты ? датаОплаты : order.ShipmentDate,
                ДатаОтгрузки = order.ShipmentDate < датаДок ? датаДок : order.ShipmentDate,
                //ВидОперации = Common.ВидыОперации.Where(x => x.Key == d.dh.Sp4760).Select(y => y.Value).FirstOrDefault(),
                СкидКарта = null,
                СпособОтгрузки = "Самовывоз",
                Маршрут = маршрут,
                Order = order
            };
            //var requestedCodes = items.Select(x => x.ShopSku).ToList();
            //var НоменклатураList = await _номенклатура.GetНоменклатураByListCodeAsync(requestedCodes);
            foreach (var row in order.Items)
            {
                var номенклатура = НоменклатураEntities.FirstOrDefault(x => x.Id == row.НоменклатураId);
                if (номенклатура != null)
                {
                    var ставкаНДС = doc.УчитыватьНДС ? await _номенклатура.GetСтавкаНДСAsync(номенклатура.Id) : _номенклатура.GetСтавкаНДС(Common.СтавкиНДС.FirstOrDefault(x => x.Value == "Без НДС").Key);
                    var текЦена = (row.ЦенаСоСкидкой > 0 ? row.ЦенаСоСкидкой : row.Цена) + row.Вознаграждение;
                    doc.ТабличнаяЧасть.Add(new ФормаПредварительнаяЗаявкаТЧ
                    {
                        Номенклатура = номенклатура,
                        НоменклатураMarketplaceId = row.Id, //order.MarketplaceId,
                        WarehouseId = row.ИдентификаторСклада,
                        PartnerWarehouseId = row.ИдентификаторСкладаПартнера,
                        Delivery = row.Доставка,
                        Количество = row.Количество,
                        Единица = номенклатура.Единица,
                        Цена = текЦена,
                        СуммаСоСкидкой = (decimal)(текЦена * row.Количество),
                        Сумма = (decimal)(текЦена * row.Количество),
                        СтавкаНДС = ставкаНДС,
                        СуммаНДС = (decimal)(текЦена * row.Количество) * (ставкаНДС.Процент / (100 + ставкаНДС.Процент))
                    });
                }
            }
            if (order.DeliveryPrice > 0)
            {
                var номенклатура = await _номенклатура.GetНоменклатураByIdAsync(Common.УслугаДоставкиId);
                var ставкаНДС = doc.УчитыватьНДС ? await _номенклатура.GetСтавкаНДСAsync(номенклатура.Id) : _номенклатура.GetСтавкаНДС(Common.СтавкиНДС.FirstOrDefault(x => x.Value == "Без НДС").Key);
                doc.ТабличнаяЧасть.Add(new ФормаПредварительнаяЗаявкаТЧ
                {
                    Номенклатура = номенклатура,
                    НоменклатураMarketplaceId = order.MarketplaceId,
                    WarehouseId = "",
                    PartnerWarehouseId = "",
                    Delivery = true,
                    Количество = 1,
                    Единица = номенклатура.Единица,
                    Цена = (decimal)order.DeliveryPrice,
                    СуммаСоСкидкой = (decimal)order.DeliveryPrice,
                    Сумма = (decimal)order.DeliveryPrice,
                    СтавкаНДС = ставкаНДС,
                    СуммаНДС = (decimal)order.DeliveryPrice * (ставкаНДС.Процент / (100 + ставкаНДС.Процент))
                });

            }
            doc.Общие.Комментарий = (order.DeliveryPartnerType != StinDeliveryPartnerType.SHOP) 
                ? "Интернет-заказ " + order.Marketplace + " №" + order.MarketplaceId : 
                    ((order.DeliveryType == StinDeliveryType.PICKUP ? "Самовывоз, " : адресСтрокой) + (order.PaymentType == StinPaymentType.POSTPAID ? "Забрать деньги " + предпочтительныйСпособОплаты + (doc.ТабличнаяЧасть.Sum(x => x.Сумма) - doc.Order.СуммаВозмещения).ToString() : "Оплачено " + doc.ТабличнаяЧасть.Sum(x => x.Сумма).ToString()) + " руб.");
            return doc;
        }
        public async Task<ExceptionData> ЗаписатьAsync(ФормаПредварительнаяЗаявка doc)
        {
            try
            {
                _1sjourn j = GetEntityJourn(0, 0, 4588, 12747, null, "ПредварительнаяЗаявка",
                    null, doc.Общие.ДатаДок,
                    doc.Общие.Фирма.Id,
                    doc.Общие.Автор.Id,
                    doc.Склад.Наименование,
                    doc.Контрагент.Наименование);
                await _context._1sjourns.AddAsync(j);

                doc.Общие.IdDoc = j.Iddoc;
                doc.Общие.DateTimeIdDoc = j.DateTimeIddoc;
                doc.Общие.НомерДок = j.Docno;
                Dh12747 docHeader = new Dh12747
                {
                    Iddoc = j.Iddoc,
                    Sp12711 = Common.ПустоеЗначениеИд13,//докОснование
                    Sp12712 = doc.БанковскийСчет.Id, //БанковскийСчет
                    Sp12713 = doc.Контрагент.Id,
                    Sp12714 = doc.Договор.Id,
                    Sp12715 = Common.ВалютаРубль,
                    Sp12716 = 1, //Курс
                    Sp12717 = doc.УчитыватьНДС ? 1 : 0,
                    Sp12718 = 1, //СуммаВклНДС
                    Sp12719 = 0, //УчитыватьНП
                    Sp12720 = 0, //СуммаВклНП
                    Sp12721 = doc.ТабличнаяЧасть.Sum(x => x.Сумма), //СуммаВзаиморасчетов
                    Sp12722 = doc.ТипЦен.Id, //ТипЦен
                    Sp12723 = doc.Скидка != null ? doc.Скидка.Id : Common.ПустоеЗначение, //Скидка
                    Sp12724 = doc.ДатаОплаты,
                    Sp12725 = doc.ДатаОтгрузки,
                    Sp12726 = doc.Склад.Id,
                    Sp12727 = Common.СпособыРезервирования.FirstOrDefault(x => x.Value == "Резервировать только из текущего остатка").Key,
                    Sp12728 = (doc.СкидКарта == null ? StinClasses.Common.ПустоеЗначение : doc.СкидКарта.Id),
                    Sp12729 = 1, //ПоСтандарту
                    Sp12730 = 0, //ДанаДопСкидка
                    Sp12731 = Common.СпособыОтгрузки.FirstOrDefault(x => x.Value == doc.СпособОтгрузки).Key,
                    Sp12732 = "", //НомерКвитанции
                    Sp12733 = 0, //ДатаКвитанции
                    Sp12734 = doc.Маршрут != null ? doc.Маршрут.Code : "", //ИндМаршрута
                    Sp12735 = doc.Маршрут != null ? doc.Маршрут.Наименование : "", //НомерМаршрута
                    Sp14007 = doc.Order != null ? doc.Order.Id : Common.ПустоеЗначение,
                    Sp12741 = 0, //Сумма
                    Sp12743 = 0, //СуммаНДС
                    Sp12745 = 0, //СуммаНП
                    Sp660 = doc.Общие.Комментарий
                };
                await _context.Dh12747s.AddAsync(docHeader);

                short lineNo = 1;
                foreach (var item in doc.ТабличнаяЧасть)
                {
                    //Единицы ОсновнаяЕдиница = await _номенклатураRepository.ОсновнаяЕдиницаAsync(item.Id);
                    //СтавкаНДС ставкаНДС = await _номенклатураRepository.СтавкаНДСAsync(item.Id);
                    Dt12747 docRow = new Dt12747
                    {
                        Iddoc = j.Iddoc,
                        Lineno = lineNo++,
                        Sp12736 = item.Номенклатура.Id,
                        Sp12737 = item.Количество,
                        Sp12738 = item.Номенклатура.Единица.Id,
                        Sp12739 = item.Номенклатура.Единица.Коэффициент,
                        Sp12740 = item.Цена,
                        Sp12741 = item.Сумма, //.СуммаСоСкидкой,
                        Sp12742 = item.СтавкаНДС.Id,
                        Sp12743 = item.СуммаНДС,
                        Sp12744 = Common.ПустоеЗначение,
                        Sp12745 = 0,
                        Sp13041 = 0
                    };
                    await _context.Dt12747s.AddAsync(docRow);
                }
                _context.РегистрацияИзмененийРаспределеннойИБ(12747, j.Iddoc);
                await _context.SaveChangesAsync();
                //await РегистрацияИзмененийРаспределеннойИБAsync(12747, j.Iddoc);

                await _context.Database.ExecuteSqlRawAsync(
                    "exec _1sp_DH12747_UpdateTotals @num36",
                    new SqlParameter("@num36", j.Iddoc)
                    );
                await _context.SaveChangesAsync();
                if (doc.Общие.ДокОснование != null)
                    await ОбновитьПодчиненныеДокументы(doc.Общие.ДокОснование.Значение, j.DateTimeIddoc, j.Iddoc);
                //склад
                await ОбновитьГрафыОтбора(4747, Common.Encode36(55).PadLeft(4) + doc.Склад.Id, j.DateTimeIddoc, j.Iddoc);
                //контрагент
                await ОбновитьГрафыОтбора(862, Common.Encode36(172).PadLeft(4) + doc.Контрагент.Id, j.DateTimeIddoc, j.Iddoc);
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = db_ex.HResult, Description = db_ex.InnerException.ToString() };
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = ex.HResult, Description = ex.Message };
            }
            return null;
        }
        public async Task<ExceptionData> ПровестиAsync(ФормаПредварительнаяЗаявка doc)
        {
            try
            {
                _1sjourn j = await _context._1sjourns.FirstOrDefaultAsync(x => x.Iddoc == doc.Общие.IdDoc);
                if (j == null)
                {
                    if (_context.Database.CurrentTransaction != null)
                        _context.Database.CurrentTransaction.Rollback();
                    return new ExceptionData { Description = "Не обнаружена запись журнала." };
                }
                int КоличествоДвижений = j.Actcnt;
                var РегистрСпросОстатки_Остатки = await _регистрСпросОстатки.ПолучитьОстаткиAsync(
                    doc.Общие.ДатаДок,
                    doc.Общие.IdDoc,
                    false,
                    doc.Общие.Фирма.Id,
                    doc.Склад.Id,
                    doc.Контрагент.Id
                    );
                foreach (var r in РегистрСпросОстатки_Остатки)
                {
                    КоличествоДвижений++;
                    j.Rf12815 = await _регистрСпросОстатки.ВыполнитьДвижениеОстаткиAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, true,
                        r.ФирмаId, r.НоменклатураId, r.СкладId, r.КонтрагентId, r.Количество);
                }
                if (doc.Order != null && doc.Order.Id != Common.ПустоеЗначение)
                {
                    foreach (var row in doc.ТабличнаяЧасть.Where(x => !x.Номенклатура.ЭтоУслуга))
                    {
                        КоличествоДвижений++;
                        j.Rf14021 = await _регистрMarketplaceOrders.ВыполнитьДвижениеAsync(doc.Общие.IdDoc, doc.Общие.ДатаДок, КоличествоДвижений, false,
                            doc.Общие.Фирма.Id, doc.Order.Id, (decimal)doc.Order.Status * 10 + (decimal)doc.Order.SubStatus, row.Номенклатура.Id,
                            row.НоменклатураMarketplaceId, row.WarehouseId, row.PartnerWarehouseId, row.Delivery,
                            row.Количество * row.Единица.Коэффициент, row.Сумма, row.СуммаСоСкидкой, false);
                    }
                }
                j.Closed = 1;
                j.Actcnt = КоличествоДвижений;
                j.Ds1946 = 2;

                _context.Update(j);
                _context.РегистрацияИзмененийРаспределеннойИБ(doc.Общие.ВидДокумента10, j.Iddoc);
                await _context.SaveChangesAsync();

                //await ОбновитьВремяТА(j.Iddoc, j.DateTimeIddoc);
                //await ОбновитьПоследовательность(j.DateTimeIddoc);
                //await ОбновитьСетевуюАктивность();
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = db_ex.HResult, Description = db_ex.InnerException.ToString() };
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                return new ExceptionData { Code = ex.HResult, Description = ex.Message };
            }
            return null;
        }
        public async Task<List<Tuple<string, string, List<ФормаПредварительнаяЗаявкаТЧ>>>> РаспределитьТоварПоНаличиюAsync(ФормаПредварительнаяЗаявка doc, List<string> CписокСкладовНаличияТовара)
        {
            Контрагент КонтрагентАлко = null;
            IEnumerable<Номенклатура> СписокТоваровДилера = Enumerable.Empty<Номенклатура>();
            if (await _контрагент.ПроверкаНаДилераAsync(doc.Контрагент.Id, "Дилер АЛ-КО КОБЕР", "ДА"))
            {
                string АлкоИНН = "7701190698";
                КонтрагентАлко = await _контрагент.ПолучитьПоИННAsync(АлкоИНН) ?? new Контрагент { Id = "" };
                СписокТоваровДилера = _номенклатура.ПолучитьНоменклатуруПоАгентуПроизводителя(КонтрагентАлко.Id, doc.ТабличнаяЧасть.Where(x => !x.Номенклатура.ЭтоУслуга).Select(x => x.Номенклатура.Id)).AsEnumerable();
            }

            List<string> СписокФирм = await _фирма.ПолучитьСписокРазрешенныхФирмAsync(doc.Общие.Фирма.Id);
            List<string> СписокФирмБезНдс = await _фирма.ПолучитьСписокФирмБезНДС(doc.Общие.Фирма.Id);

            if ((doc.Order != null) &&
                (doc.Order.DeliveryPartnerType != StinDeliveryPartnerType.YANDEX_MARKET) &&
                (doc.Order.DeliveryType == StinDeliveryType.PICKUP))
            {
                int складIndex = CписокСкладовНаличияТовара.IndexOf(doc.Склад.Id);
                if (складIndex >= 0)
                {
                    CписокСкладовНаличияТовара.RemoveAt(складIndex);
                    CписокСкладовНаличияТовара.Insert(0, doc.Склад.Id);
                }
            }

            List<Tuple<string, string, List<ФормаПредварительнаяЗаявкаТЧ>>> ПереченьНаличия = new List<Tuple<string, string, List<ФормаПредварительнаяЗаявкаТЧ>>>();
            List<string> СписокНоменклатуры = doc.ТабличнаяЧасть.Where(x => !x.Номенклатура.ЭтоУслуга).Select(x => x.Номенклатура.Id).ToList();
            var ТзОстатки = (await _номенклатура.ПолучитьСвободныеОстатки(СписокФирм, CписокСкладовНаличияТовара, СписокНоменклатуры)).AsEnumerable();
            IEnumerable<Номенклатура> ТзОстаткиФирмБезНДС = null;
            if (СписокФирмБезНдс.Count > 0)
                ТзОстаткиФирмБезНДС = (await _номенклатура.ПолучитьСвободныеОстатки(СписокФирмБезНдс, CписокСкладовНаличияТовара, СписокНоменклатуры)).AsEnumerable();
            foreach (ФормаПредварительнаяЗаявкаТЧ item in doc.ТабличнаяЧасть.Where(x => !x.Номенклатура.ЭтоУслуга))
            {
                if (СписокТоваровДилера.Any(x => x.Id == item.Номенклатура.Id))
                {
                    var entry = ПереченьНаличия.FirstOrDefault(x => (x.Item1 == doc.Общие.Фирма.Id) && (x.Item2 == "ДилерскаяЗаявка"));
                    if (entry == null)
                        ПереченьНаличия.Add(new(doc.Общие.Фирма.Id, "ДилерскаяЗаявка", new List<ФормаПредварительнаяЗаявкаТЧ> { item }));
                    else
                        entry.Item3.Add(item);
                }
                else
                {
                    decimal ТекОстатокСуммы = item.Сумма; //.СуммаСоСкидкой;
                    decimal Отпустить = item.Количество * item.Единица.Коэффициент;
                    if (Отпустить > 0)
                    {
                        foreach (string firmaId in СписокФирм)
                        {
                            foreach (string склId in CписокСкладовНаличияТовара)
                            {
                                decimal СвободныйОстаток = ТзОстатки
                                    .Where(x => x.Id == item.Номенклатура.Id)
                                    .SelectMany(x => x.Остатки.Where(y => y.ФирмаId == firmaId && y.СкладId == склId).Select(z => z.СвободныйОстаток))
                                    .Sum();
                                decimal МожноОтпустить = Math.Min(Отпустить, СвободныйОстаток);
                                if (МожноОтпустить > 0)
                                {
                                    decimal ОстатокВОсновныхЕдиницах = МожноОтпустить / item.Единица.Коэффициент;
                                    decimal ЦелыйОстатокВОсновныхЕдиницах = decimal.Round(ОстатокВОсновныхЕдиницах);
                                    if (ОстатокВОсновныхЕдиницах != ЦелыйОстатокВОсновныхЕдиницах)
                                        МожноОтпустить = ЦелыйОстатокВОсновныхЕдиницах * item.Единица.Коэффициент;
                                    decimal ДобСумма = ТекОстатокСуммы;
                                    if (МожноОтпустить != Отпустить)
                                        ДобСумма = Math.Round(item.Сумма / (item.Количество * item.Единица.Коэффициент) * МожноОтпустить, 2);
                                    var entryItem = new ФормаПредварительнаяЗаявкаТЧ
                                    {
                                        Номенклатура = item.Номенклатура,
                                        Единица = item.Единица,
                                        Количество = МожноОтпустить / item.Единица.Коэффициент,
                                        Цена = item.Цена,
                                        Сумма = ДобСумма,
                                    };
                                    var entry = ПереченьНаличия.FirstOrDefault(x => (x.Item1 == firmaId) && (x.Item2 == склId));
                                    if (entry == null)
                                        ПереченьНаличия.Add(new(firmaId, склId, new List<ФормаПредварительнаяЗаявкаТЧ> { entryItem }));
                                    else
                                        entry.Item3.Add(entryItem);
                                    ТекОстатокСуммы = ТекОстатокСуммы - ДобСумма;
                                    Отпустить = Отпустить - МожноОтпустить;
                                    if (Отпустить <= 0)
                                        break;
                                }
                            }
                            if (Отпустить <= 0)
                                break;
                        }
                    }
                    if (Отпустить > 0)
                    {
                        foreach (string firmaId in СписокФирмБезНдс)
                        {
                            foreach (string склId in CписокСкладовНаличияТовара)
                            {
                                decimal СвободныйОстаток = ТзОстаткиФирмБезНДС
                                    .Where(x => x.Id == item.Номенклатура.Id)
                                    .SelectMany(x => x.Остатки.Where(y => y.ФирмаId == firmaId && y.СкладId == склId)
                                    .Select(z => z.СвободныйОстаток))
                                    .Sum();
                                decimal МожноОтпустить = Math.Min(Отпустить, СвободныйОстаток);
                                if (МожноОтпустить > 0)
                                {
                                    decimal ОстатокВОсновныхЕдиницах = МожноОтпустить / item.Единица.Коэффициент;
                                    decimal ЦелыйОстатокВОсновныхЕдиницах = decimal.Round(ОстатокВОсновныхЕдиницах);
                                    if (ОстатокВОсновныхЕдиницах != ЦелыйОстатокВОсновныхЕдиницах)
                                        МожноОтпустить = ЦелыйОстатокВОсновныхЕдиницах * item.Единица.Коэффициент;
                                    decimal ДобСумма = ТекОстатокСуммы;
                                    if (МожноОтпустить != Отпустить)
                                        ДобСумма = Math.Round(item.Сумма / (item.Количество * item.Единица.Коэффициент) * МожноОтпустить, 2);
                                    var entryItem = new ФормаПредварительнаяЗаявкаТЧ
                                    {
                                        Номенклатура = item.Номенклатура,
                                        Единица = item.Единица,
                                        Количество = МожноОтпустить / item.Единица.Коэффициент,
                                        Цена = item.Цена,
                                        Сумма = ДобСумма,
                                    };
                                    var entry = ПереченьНаличия.FirstOrDefault(x => (x.Item1 == firmaId) && (x.Item2 == склId));
                                    if (entry == null)
                                        ПереченьНаличия.Add(new(firmaId, склId, new List<ФормаПредварительнаяЗаявкаТЧ> { entryItem }));
                                    else
                                        entry.Item3.Add(entryItem);
                                    ТекОстатокСуммы = ТекОстатокСуммы - ДобСумма;
                                    Отпустить = Отпустить - МожноОтпустить;
                                    if (Отпустить <= 0)
                                        break;
                                }
                            }
                            if (Отпустить <= 0)
                                break;
                        }
                    }
                    if (Отпустить > 0)
                    {
                        var entryItem = new ФормаПредварительнаяЗаявкаТЧ
                        {
                            Номенклатура = item.Номенклатура,
                            Единица = item.Единица,
                            Количество = Отпустить / item.Единица.Коэффициент,
                            Цена = item.Цена,
                            Сумма = ТекОстатокСуммы,
                        };
                        var entry = ПереченьНаличия.FirstOrDefault(x => (x.Item1 == doc.Общие.Фирма.Id) && (x.Item2 == "Спрос"));
                        if (entry == null)
                            ПереченьНаличия.Add(new(doc.Общие.Фирма.Id, "Спрос", new List<ФормаПредварительнаяЗаявкаТЧ> { entryItem }));
                        else
                            entry.Item3.Add(entryItem);
                    }
                }
            }
            //Аналоги
            if (ПереченьНаличия.Where(x => (x.Item1 == doc.Общие.Фирма.Id) && (x.Item2 == "Спрос")).Count() > 0)
            {
                var entryСпрос = ПереченьНаличия.FirstOrDefault(x => (x.Item1 == doc.Общие.Фирма.Id) && (x.Item2 == "Спрос"));
                List<Номенклатура> СписокТоваров = new List<Номенклатура>();
                foreach (Номенклатура н in entryСпрос.Item3.Select(x => x.Номенклатура))
                {
                    СписокТоваров.AddRange(await _номенклатура.АналогиНоменклатурыAsync(н.Id));
                }
                if (СписокТоваров.Count > 0)
                {
                    ТзОстатки = (await _номенклатура.ПолучитьСвободныеОстатки(СписокФирм, CписокСкладовНаличияТовара, СписокТоваров.Select(x => x.Id).ToList())).AsEnumerable();
                    if (СписокФирмБезНдс.Count > 0)
                        ТзОстаткиФирмБезНДС = (await _номенклатура.ПолучитьСвободныеОстатки(СписокФирмБезНдс, CписокСкладовНаличияТовара, СписокТоваров.Select(x => x.Id).ToList())).AsEnumerable();
                    if (ТзОстатки.Count() > 0)
                    {
                        foreach (ФормаПредварительнаяЗаявкаТЧ row in entryСпрос.Item3)
                        {
                            decimal Отпустить = row.Количество;
                            List<Номенклатура> аналоги = await _номенклатура.АналогиНоменклатурыAsync(row.Номенклатура.Id);
                            foreach (Номенклатура н in аналоги)
                            {
                                foreach (string firmaId in СписокФирмБезНдс)
                                {
                                    foreach (string склId in CписокСкладовНаличияТовара)
                                    {
                                        decimal СвободныйОстаток = ТзОстатки
                                            .Where(x => x.Id == н.Id)
                                            .SelectMany(x => x.Остатки.Where(y => y.ФирмаId == firmaId && y.СкладId == склId).Select(z => z.СвободныйОстаток))
                                            .Sum();
                                        var alreadyUse = ПереченьНаличия.Where(x => x.Item1 == firmaId && x.Item2 == склId).SelectMany(x => x.Item3.Where(y => y.Номенклатура.Id == н.Id)).Sum(x => x.Количество);
                                        СвободныйОстаток = Math.Max(СвободныйОстаток - alreadyUse, 0);
                                        decimal МожноОтпустить = Math.Min(Отпустить, СвободныйОстаток);
                                        decimal ОстатокВОсновныхЕдиницах = МожноОтпустить / н.Единица.Коэффициент;
                                        decimal ЦелыйОстатокВОсновныхЕдиницах = decimal.Round(ОстатокВОсновныхЕдиницах);
                                        if (ОстатокВОсновныхЕдиницах != ЦелыйОстатокВОсновныхЕдиницах)
                                            МожноОтпустить = ЦелыйОстатокВОсновныхЕдиницах * н.Единица.Коэффициент;
                                        if (МожноОтпустить > 0)
                                        {
                                            var entryItem = new ФормаПредварительнаяЗаявкаТЧ
                                            {
                                                Номенклатура = н,
                                                Единица = н.Единица,
                                                Количество = МожноОтпустить / н.Единица.Коэффициент,
                                                Цена = row.Цена,
                                                Сумма = row.Цена * МожноОтпустить / н.Единица.Коэффициент,
                                            };
                                            var entry = ПереченьНаличия.FirstOrDefault(x => (x.Item1 == firmaId) && (x.Item2 == склId));
                                            if (entry == null)
                                                ПереченьНаличия.Add(new(firmaId, склId, new List<ФормаПредварительнаяЗаявкаТЧ> { entryItem }));
                                            else
                                            {
                                                ФормаПредварительнаяЗаявкаТЧ строка = entry.Item3.FirstOrDefault(x => x.Номенклатура.Id == н.Id);
                                                if (строка != null)
                                                {
                                                    строка.Количество += МожноОтпустить / н.Единица.Коэффициент;
                                                    строка.Сумма = строка.Цена * строка.Количество / н.Единица.Коэффициент;
                                                }
                                                else
                                                    entry.Item3.Add(entryItem);
                                            }
                                            row.Количество = row.Количество - МожноОтпустить;
                                            row.Сумма = row.Цена * row.Количество / н.Единица.Коэффициент;
                                            Отпустить = Отпустить - МожноОтпустить;
                                            if (Отпустить <= 0)
                                                break;
                                        }
                                    }
                                    if (Отпустить <= 0)
                                        break;
                                }
                            }
                            if (Отпустить > 0)
                            {
                                foreach (string firmaId in СписокФирмБезНдс)
                                {
                                    foreach (Номенклатура н in аналоги)
                                    {
                                        foreach (string склId in CписокСкладовНаличияТовара)
                                        {
                                            decimal СвободныйОстаток = ТзОстаткиФирмБезНДС
                                                .Where(x => x.Id == н.Id)
                                                .SelectMany(x => x.Остатки.Where(y => y.ФирмаId == firmaId && y.СкладId == склId).Select(z => z.СвободныйОстаток))
                                                .Sum();
                                            var alreadyUse = ПереченьНаличия.Where(x => x.Item1 == firmaId && x.Item2 == склId).SelectMany(x => x.Item3.Where(y => y.Номенклатура.Id == н.Id)).Sum(x => x.Количество);
                                            СвободныйОстаток = Math.Max(СвободныйОстаток - alreadyUse, 0);
                                            decimal МожноОтпустить = Math.Min(Отпустить, СвободныйОстаток);
                                            decimal ОстатокВОсновныхЕдиницах = МожноОтпустить / н.Единица.Коэффициент;
                                            decimal ЦелыйОстатокВОсновныхЕдиницах = decimal.Round(ОстатокВОсновныхЕдиницах);
                                            if (ОстатокВОсновныхЕдиницах != ЦелыйОстатокВОсновныхЕдиницах)
                                                МожноОтпустить = ЦелыйОстатокВОсновныхЕдиницах * н.Единица.Коэффициент;
                                            if (МожноОтпустить > 0)
                                            {
                                                var entryItem = new ФормаПредварительнаяЗаявкаТЧ
                                                {
                                                    Номенклатура = н,
                                                    Единица = н.Единица,
                                                    Количество = МожноОтпустить / н.Единица.Коэффициент,
                                                    Цена = row.Цена,
                                                    Сумма = row.Цена * МожноОтпустить / н.Единица.Коэффициент,
                                                };
                                                var entry = ПереченьНаличия.FirstOrDefault(x => (x.Item1 == firmaId) && (x.Item2 == склId));
                                                if (entry == null)
                                                    ПереченьНаличия.Add(new(firmaId, склId, new List<ФормаПредварительнаяЗаявкаТЧ> { entryItem }));
                                                else
                                                {
                                                    ФормаПредварительнаяЗаявкаТЧ строка = entry.Item3.FirstOrDefault(x => x.Номенклатура.Id == н.Id);
                                                    if (строка != null)
                                                    {
                                                        строка.Количество += МожноОтпустить / н.Единица.Коэффициент;
                                                        строка.Сумма = строка.Цена * строка.Количество / н.Единица.Коэффициент;
                                                    }
                                                    else
                                                        entry.Item3.Add(entryItem);
                                                }
                                                row.Количество = row.Количество - МожноОтпустить;
                                                row.Сумма = row.Цена * row.Количество / н.Единица.Коэффициент;
                                                Отпустить = Отпустить - МожноОтпустить;
                                                if (Отпустить <= 0)
                                                    break;
                                            }
                                        }
                                        if (Отпустить <= 0)
                                            break;
                                    }
                                    if (Отпустить <= 0)
                                        break;
                                }
                            }
                        }
                    }
                }
                foreach (var item3 in ПереченьНаличия.Where(x => x.Item2 == "Спрос").Select(x => x.Item3))
                {
                    item3.RemoveAll(x => x.Количество <= 0);
                }
            }
            return ПереченьНаличия;
        }
        public async Task<ФормаПредварительнаяЗаявка> GetФормаПредварительнаяЗаявкаById(string idDoc)
        {
            var d = await (from dh in _context.Dh12747s
                           join j in _context._1sjourns on dh.Iddoc equals j.Iddoc
                           where dh.Iddoc == idDoc
                           select new
                           {
                               dh,
                               j
                           }).FirstOrDefaultAsync();

            var doc = new ФормаПредварительнаяЗаявка
            {
                Общие = new ОбщиеРеквизиты
                {
                    IdDoc = d.dh.Iddoc,
                    ДокОснование = !(string.IsNullOrWhiteSpace(d.dh.Sp12711) || d.dh.Sp12711 == Common.ПустоеЗначениеИд13) ? await ДокОснованиеAsync(d.dh.Sp12711.Substring(4)) : null,
                    Фирма = await _фирма.GetEntityByIdAsync(d.j.Sp4056),
                    Автор = await _пользователь.GetUserByIdAsync(d.j.Sp74),
                    ВидДокумента10 = d.j.Iddocdef,
                    ВидДокумента36 = Common.Encode36(d.j.Iddocdef),
                    НазваниеВЖурнале = "Предварительная заявка",
                    НомерДок = d.j.Docno,
                    ДатаДок = d.j.DateTimeIddoc.ToDateTime(),
                    Проведен = d.j.Closed == 1,
                    Комментарий = d.dh.Sp660,
                    Удален = d.j.Ismark
                },
                БанковскийСчет = await _фирма.ПолучитьБанковскийСчетById(d.dh.Sp12712),
                Склад = await _склад.GetEntityByIdAsync(d.dh.Sp12726),
                Контрагент = await _контрагент.GetКонтрагентAsync(d.dh.Sp12713),
                Договор = await _контрагент.GetДоговорAsync(d.dh.Sp12714),
                ОблагаетсяЕНВД = false,
                УчитыватьНДС = d.dh.Sp12717 == 1,
                СуммаВклНДС = d.dh.Sp12718 == 1,
                ТипЦен = await _контрагент.GetТипЦенAsync(d.dh.Sp12722),
                Скидка = await _контрагент.GetСкидкаAsync(d.dh.Sp12723),
                ДатаОплаты = d.dh.Sp12724,
                ДатаОтгрузки = d.dh.Sp12725,
                СкидКарта = await _контрагент.GetСкидКартаAsync(d.dh.Sp12728),
                СпособОтгрузки = Common.СпособыОтгрузки.FirstOrDefault(x => x.Key == d.dh.Sp12731).Value,
                Маршрут = await _маршрут.GetМаршрутByCodeAsync(d.dh.Sp12734),
                Order = await _order.ПолучитьOrderWithItems(d.dh.Sp14007)
            };
            var ТаблЧасть = (from dt in _context.Dt12747s
                            join dh in _context.Dh12747s on dt.Iddoc equals dh.Iddoc
                            join item in _context.Sc14033s on new { orderId = dh.Sp14007, номенклатураId = dt.Sp12736 } equals new { orderId = item.Parentext, номенклатураId = item.Sp14022 } into _item
                            from item in _item.DefaultIfEmpty()
                            where dt.Iddoc == idDoc
                            select new
                            {
                                НоменклатураId = dt.Sp12736,
                                MarketplaceId = dh.Sp14007,
                                WarehouseId = item != null ? item.Sp14030 : string.Empty,
                                PartnerWarehouseId = item != null ? item.Sp14031 : string.Empty,
                                Delivery = item != null ? item.Sp14027 == 1 : false,
                                Количество = dt.Sp12737,
                                ЕдиницаId = dt.Sp12738,
                                Цена = dt.Sp12740,
                                Сумма = dt.Sp12741,
                                СуммаСоСкидкой = item != null ? (item.Sp14023 * item.Sp14025) : 0,
                                СтавкаНДСId = dt.Sp12742,
                                СуммаНДС = dt.Sp12743
                            }).ToList();
            foreach (var row in ТаблЧасть)
            {
                doc.ТабличнаяЧасть.Add(new ФормаПредварительнаяЗаявкаТЧ
                {
                    Номенклатура = await _номенклатура.GetНоменклатураByIdAsync(row.НоменклатураId),
                    НоменклатураMarketplaceId = row.MarketplaceId,
                    WarehouseId = row.WarehouseId,
                    PartnerWarehouseId = row.PartnerWarehouseId,
                    Delivery = row.Delivery,
                    Количество = row.Количество,
                    Единица = await _номенклатура.GetЕдиницаByIdAsync(row.ЕдиницаId),
                    Цена = row.Цена,
                    СуммаСоСкидкой = row.СуммаСоСкидкой,
                    Сумма = row.Сумма,
                    СтавкаНДС = _номенклатура.GetСтавкаНДС(row.СтавкаНДСId),
                    СуммаНДС = row.СуммаНДС
                });
            }
            return doc;
        }
        public async Task<ФормаПредварительнаяЗаявка> GetФормаПредварительнаяЗаявкаByOrderId(string orderId)
        {
            var idDoc = await _context.Dh12747s.Where(x => x.Sp14007 == orderId).Select(x => x.Iddoc).FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(idDoc))
                return null;
            else
                return await GetФормаПредварительнаяЗаявкаById(idDoc);
        }
        public async Task ОбновитьНомерМаршрута(string orderId, string маршрутНаименование)
        {
            var dh = await _context.Dh12747s
                .FirstOrDefaultAsync(x => (x.Sp14007 == orderId) && (x.Sp12735 != маршрутНаименование));
            if (dh != null)
            {
                dh.Sp12735 = маршрутНаименование;
                _context.Update(dh);
                _context.РегистрацияИзмененийРаспределеннойИБ(12747, dh.Iddoc);
                await _context.SaveChangesAsync();
            }
        }
    }
}
