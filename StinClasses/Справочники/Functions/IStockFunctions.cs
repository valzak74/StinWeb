using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StinClasses.Справочники.Functions
{
    public interface IStockFunctions
    {
        Task<RefBook> GetStockByIdAsync(string Id, CancellationToken cancellationToken);
        Task<СкладExtended> GetStockExtByIdAsync(string Id, CancellationToken cancellationToken);
        Task<int> NextBusinessDay(string stockId, DateTime checkingDate, int addDays, CancellationToken cancellationToken);
        Task<List<string>> ПолучитьСкладIdОстатковMarketplace();
    }
}
