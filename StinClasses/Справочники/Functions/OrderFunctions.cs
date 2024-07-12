using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    public class OrderFunctions : IOrderFunctions
    {
        StinDbContext _context;
        IMemoryCache _cache;
        RefBookType _type;
        readonly string _type36;
        public OrderFunctions(StinDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
            _type = RefBookType.Order;
            _type36 = ((long)_type).Encode36();
        }
        public async Task<Order> ПолучитьOrder(string orderId)
        {
            throw new NotImplementedException();
        }
        public async Task<Order> ПолучитьOrderByMarketplaceId(string marketplaceId, string marketplaceOrderId)
        {
            throw new NotImplementedException();
        }
        public async Task<Order> ПолучитьOrderWithItems(string orderId)
        {
            throw new NotImplementedException();
        }
        public async Task<Order> ПолучитьOrderWithItems(string marketplaceId, string marketplaceOrderId)
        {
            throw new NotImplementedException();
        }
        public async Task ОбновитьOrderNo(string Id, string OrderNo)
        {
            throw new NotImplementedException();
        }
        public async Task ОбновитьOrderShipmentDate(string Id, DateTime ShipmentDate)
        {
            throw new NotImplementedException();
        }
        public async Task RefreshOrderDeliveryServiceId(string Id, long DeliveryServiceId, string DeliveryServiceName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public async Task UpdateOrderStatus(string id, int newStatus, string errMessage, CancellationToken cancellationToken)
        {
            var entity = await _context.Sc13994s.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity != null)
            {
                entity.Sp13982 = newStatus;
                if (!string.IsNullOrEmpty(errMessage))
                    entity.Sp14055 = errMessage;
                _context.Update(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        public async Task ОбновитьOrderNoAndStatus(string Id, string OrderNo, int NewStatus)
        {
            throw new NotImplementedException();
        }
        public async Task ОбновитьПолучателяЗаказаАдрес(string orderId, string LastName, string FirstName, string MiddleName, string Recipient, string Phone,
            string PostCode, string Country, string City, string Subway, string Street, string House, string Block, string Entrance, string Entryphone, string Floor, string Apartment,
            string CustomerNotes)
        {
            throw new NotImplementedException();
        }
        public async Task<Order> НовыйOrder(string firmaId, EncodeVersion encoding, string authApi, string marketplaceName, string marketplaceId,
            StinPaymentType paymentType, StinPaymentMethod paymentMethod,
            StinDeliveryPartnerType deliveryPartnerType, StinDeliveryType deliveryType, string deliveryServiceId, string deliveryServiceName, double deliveryPrice, double deliverySubsidy,
            string shipmentId, DateTime shipmentDate, string regionId, string regionName, OrderRecipientAddress address, string notes, List<OrderItem> items)
        {
            throw new NotImplementedException();
        }
        public async Task SetOrdersPrinted(List<string> ids)
        {
            var entities = await _context.Sc13994s.Where(x => ids.Contains(x.Id)).ToListAsync();
            foreach (var entity in entities)
            {
                entity.Sp14192 = 1;
                if (_cache.TryGetValue(_type36 + entity.Id, out _))
                    _cache.Set(_type36 + entity.Id, entity, TimeSpan.FromMinutes(1));
                _context.Update(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
            }
            await _context.SaveChangesAsync();
        }
        public async Task<string> SetOrderScanned(DateTime shipDate, string campaignInfo, string barcode, int scanMark, CancellationToken cancellationToken)
        {
            try
            {
                var campaignData = campaignInfo.Split('/');
                var campaignIds = campaignData[0].Split(',').Select(x => x.Replace('_', ' ')).ToList();

                _cache.TryGetValue(_type36 + campaignInfo, out string marketData);
                if (string.IsNullOrEmpty(marketData))
                {
                    marketData = await _context.Sc14042s
                        .Where(x => campaignIds.Contains(x.Id))
                        .GroupBy(x => x.Sp14155.Trim().ToUpper())
                        .Select(gr => gr.Key + (campaignData.Length > 1 ? "_REAL" : ""))
                        .SingleOrDefaultAsync(cancellationToken);
                    if (!string.IsNullOrEmpty(marketData))
                        _cache.Set(_type36 + campaignInfo, marketData, TimeSpan.FromDays(1));
                }
                int logNumber = 1;
                string[] barcodeData;
                string cacheKey = _type36 + campaignInfo + barcode;
                Sc13994 entity = null;
                _cache.TryGetValue(cacheKey, out string entityId);
                if (!string.IsNullOrEmpty(entityId)) 
                    if (!_cache.TryGetValue(_type36 + entityId, out entity))
                        entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Id == entityId, cancellationToken);

                if (entity == null)
                {
                    switch (marketData)
                    {
                        case "OZON":
                            entity = await _context.Sc13994s.FirstOrDefaultAsync(x => (x.Sp13987.Trim() == barcode) || (x.Sp13992.Trim() == barcode), cancellationToken); //ServiceName or RegionName
                            break;
                        case "SBER":
                            barcodeData = barcode.Split('*');
                            if ((barcodeData.Length != 3) || !int.TryParse(barcodeData[2], out logNumber))
                                return "Формат штрихкода не распознан";
                            entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Sp13981.Trim() == barcodeData[1], cancellationToken); //Id (DW0001235-2023)
                            break;
                        case "ЯНДЕКС":
                            barcodeData = barcode.Split('-');
                            if (barcodeData.Length == 2) //old version
                            {
                                if (barcodeData[0].All(x => char.IsDigit(x)))
                                {
                                    entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Code.Trim() == barcodeData[0], cancellationToken);
                                    int.TryParse(barcodeData[1], out logNumber);
                                }
                                else
                                    return "Отсканируйте другой штрихкод на этикетке";
                            }
                            else if (barcodeData.Length == 1 && barcodeData[0].Length == 20) //new version
                            {
                                entity = await (from order in _context.Sc13994s
                                                join item in _context.Sc14033s on order.Id equals item.Parentext
                                                where item.Sp14028.Contains(barcode)
                                                select order).FirstOrDefaultAsync(cancellationToken);
                                logNumber = barcode.GetHashCode();
                            }
                            else
                                return "Формат штрихкода не распознан";
                            break;
                        case "ALIEXPRESS":
                            entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Sp13987.Trim() == barcode); //ServiceName
                            break;
                        case "WILDBERRIES":
                            entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Sp13992.Trim() == barcode); //RegionName
                            break;
                        default:
                            entity = await _context.Sc13994s.FirstOrDefaultAsync(x => x.Code.Trim() == barcode);
                            break;
                    }
                }

                if (entity == null)
                    return "Штрихкод не обнаружен";
                if (!campaignIds.Contains(entity.Sp14038))
                    return "Штрихкод от другого маркетплейс";
                if (entity.Sp13990.Date != shipDate.Date)
                    return "Неверная дата доставки";
                if (entity.Sp13982 == 5)
                    return "Заказ отменен";
                if (entity.Ismark)
                    return "Установлена пометка удаления";
                var logInfo = entity.Sp14255.Trim();
                string logNumberStr = logNumber.ToString();
                if (logInfo.Split(';').Any(x => x == logNumberStr))
                    return "Повторное сканирование";
                entity.Sp14254 += scanMark;
                if (!string.IsNullOrEmpty(logInfo))
                    logInfo += ";";
                logInfo += logNumberStr + ";" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                entity.Sp14255 = logInfo;
                _cache.Set(cacheKey, entity.Id, TimeSpan.FromMinutes(60));
                _cache.Set(_type36 + entity.Id, entity, TimeSpan.FromMinutes(1));
                _context.Update(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
                await _context.SaveChangesAsync(cancellationToken);
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task ClearOrderScanned(DateTime shipDate, List<string> campaignIds, string warehouseId, CancellationToken cancellationToken)
        {
            var entities = await (from x in _context.Sc13994s
                                  join m in _context.Sc14042s on x.Sp14038 equals m.Id
                                  join item in _context.Sc14033s on x.Id equals item.Parentext
                                  join nom in _context.Sc84s on item.Sp14022 equals nom.Id
                                  join markUse in _context.Sc14152s on new { nomId = nom.Id, marketId = m.Id } equals new { nomId = markUse.Parentext, marketId = markUse.Sp14147 } into _markUse
                                  from markUse in _markUse.DefaultIfEmpty()
                                  where campaignIds.Contains(m.Id) && (x.Sp13990.Date == shipDate) && (x.Sp14254 > 0) &&
                                     (!string.IsNullOrEmpty(warehouseId) ? markUse.Sp14190.Trim() == warehouseId :
                                        (!string.IsNullOrWhiteSpace(m.Sp14154) ? markUse.Sp14190.Trim() != m.Sp14154.Trim() :
                                            true))
                                  select x).ToListAsync(cancellationToken);
            foreach (var entity in entities)
            {
                entity.Sp14254 = 0;
                entity.Sp14255 = "";
                if (_cache.TryGetValue(_type36 + entity.Id, out _))
                    _cache.Set(_type36 + entity.Id, entity, TimeSpan.FromMinutes(1));
                _context.Update(entity);
                _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
        public DateTime GetShipmentDateByServiceName(string marketplaceId, string serviceName)
        {
            return _context.Sc13994s.Where(x => (x.Sp14038 == marketplaceId) && (x.Sp13987.Trim() == serviceName)).Select(x => x.Sp13990).Max();
        }
    }
}
