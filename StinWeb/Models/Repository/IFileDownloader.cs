using System.Threading.Tasks;
using StinClasses.Models;

namespace StinWeb.Models.Repository
{
    interface IFileDownloader
    {
        Task GetAndSaveDataAsync(StinDbContext context, bool mainPhoto, string url, string key);
    }
}
