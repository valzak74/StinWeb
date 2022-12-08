using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliExpressClasses
{
    public class CreateLogisticsOrderRequest
    {
        public List<LogisticsOrderRequest>? Orders { get; set; }
        public CreateLogisticsOrderRequest() => Orders = new List<LogisticsOrderRequest>();
        public CreateLogisticsOrderRequest(LogisticsOrderRequest singleData) => Orders = new List<LogisticsOrderRequest> { singleData };
    }
    public class CreateLogisticsOrderResponse
    {
        public LogisticsOrderData? Data { get; set; }
        public Error? Error { get; set; }
    }
    public class LogisticsOrderData
    {
        public List<LogisticsOrderResponse>? Orders { get; set; }
        public object? Errors { get; set; }
    }
    public class LogisticsOrderRequest
    {
        public long Trade_order_id { get; set; }
        public decimal Total_length { get; set; }
        public decimal Total_width { get; set; }
        public decimal Total_height { get; set; }
        public decimal Total_weight { get; set; }
        public List<LogisticsItem>? Items { get; set; }
        public LogisticsOrderRequest() => Items = new List<LogisticsItem>();
        public LogisticsOrderRequest(long trade_order_id, decimal total_length, decimal total_width, decimal total_height, decimal total_weight): this()
        {
            Trade_order_id = trade_order_id;
            Total_length = total_length;
            Total_width = total_width;
            Total_height = total_height;
            Total_weight = total_weight;
        }
    }
    public class LogisticsItem
    {
        public long Sku_id { get; set; }
        public int Quantity { get; set; }
    }
    public class LogisticsOrderResponse
    {
        public long Trade_order_id { get; set; }
        public List<LogisticsOrder>? Logistic_orders { get; set; }
    }
}
