using HttpExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OzonClasses;
using Refresher1C.Models.AsyncTasksQueue;
using StinClasses;
using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Refresher1C.Service
{
    public class AsyncTaskService : IAsyncTaskService
    {
        private readonly StinDbContext _context;
        private readonly ILogger<AsyncTaskService> _logger;
        private IHttpService _httpService;
        private IAsyncTasksQueue _asyncTasksQueue;
        private readonly Dictionary<string, string> _firmProxy;

        public AsyncTaskService(StinDbContext context, 
            IHttpService httpService,
            IConfiguration configuration,
            IAsyncTasksQueue asyncTasksQueue,
            ILogger<AsyncTaskService> logger
           )
        {
            _context = context;
            _httpService = httpService;
            _asyncTasksQueue = asyncTasksQueue;
            _logger = logger;
            _firmProxy = new Dictionary<string, string>();
            foreach (var item in configuration.GetSection("CommonSettings:FirmData").GetChildren())
            {
                var configData = item.AsEnumerable();
                var firmaId = configData.FirstOrDefault(x => x.Key.EndsWith("FirmaId")).Value;
                var proxy = configData.FirstOrDefault(x => x.Key.EndsWith("Proxy")).Value;
                _firmProxy.Add(firmaId, proxy);
            }
        }

        public async Task DownloadPdfLabels(string firmaId, string clientId, string authToken, 
            string orderId, 
            string postingNumber,
            long taskId,
            int tryCount,
            CancellationToken cancellationToken)
        {
            decimal status = 2;
            using var tran = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var entity = await _context.Sc13994s.OrderBy(x => x.RowId).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
                if (entity != null)
                {
                    var (label, needToReturnToQueue, error) = await OzonOperators.GetPackageLabel(_httpService, _firmProxy[firmaId], clientId, authToken,
                        taskId,
                        cancellationToken);
                    if (label != null)
                    {
                        var resultDetails = await OzonOperators.OrderDetails(_httpService, _firmProxy[firmaId], clientId, authToken,
                            postingNumber,
                            cancellationToken);
                        if (resultDetails.Item2 != null)
                            _logger.LogError(resultDetails.Item2);
                        if (resultDetails.Item3 != null)
                        {
                            if (!string.IsNullOrEmpty(resultDetails.Item3))
                                entity.Sp13992 = resultDetails.Item3;
                        }

                        if (((status > 0) && ((entity.Sp13982 == 1) || (entity.Sp13982 == -2))) || ((status < 0) && (entity.Sp13982 > 0)))
                            entity.Sp13982 = status;

                        _context.Update(entity);
                        _context.РегистрацияИзмененийРаспределеннойИБ(13994, entity.Id);

                        var orderBinary = await _context.VzOrderBinaries
                            .OrderBy(x => x.RowId)
                            .FirstOrDefaultAsync(x => (x.Id == orderId) && (x.Extension.Trim().ToUpper() == "LABELS"));
                        if (orderBinary == null)
                        {
                            orderBinary = new VzOrderBinary
                            {
                                Id = orderId,
                                Extension = "LABELS",
                                Binary = label
                            };
                            await _context.VzOrderBinaries.AddAsync(orderBinary);
                        }
                        else
                        {
                            orderBinary.Binary = label;
                            _context.Update(orderBinary);
                        }
                    }
                    else if (needToReturnToQueue)
                    {
                        tryCount++;
                        if (tryCount < 1000)
                        {
                            _asyncTasksQueue.Enqueue(new AsyncTasksQueueDto
                            {
                                Key = orderId,
                                WorkItem = (asyncTaskService, stoppingToken) =>
                                    asyncTaskService.DownloadPdfLabels(firmaId, clientId, authToken, orderId, postingNumber, taskId, tryCount, stoppingToken)
                            });
                        }
                    }
                }
                _context.ОбновитьСетевуюАктивность();
                await _context.SaveChangesAsync(cancellationToken);

                if (_context.Database.CurrentTransaction != null)
                tran.Commit();
            }
            catch (DbUpdateException db_ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(db_ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    _context.Database.CurrentTransaction.Rollback();
                _logger.LogError(ex.Message);
            }
        }
    }
}
