using System;
using System.Threading.Tasks;
using StinWeb.Models.Repository;
using System.Net;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;

namespace StinWeb.Models.DataManager
{
    public class FileDownloader : IFileDownloader
    {
        public async Task GetAndSaveDataAsync(StinDbContext context, bool mainPhoto, string url, string key)
        {
            try
            {
                Uri link = new Uri(url);
                string fileName = "";
                if (link.Segments != null)
                    fileName = link.Segments[link.Segments.Length - 1];
                if (!string.IsNullOrEmpty(fileName))
                {
                    using (WebClient wClient = new WebClient())
                    {
                        byte[] img = await wClient.DownloadDataTaskAsync(link);
                        var zipped = await Common.CreateZip(img, fileName);
                        VzTovarImage entry = await context.VzTovarImages.FirstOrDefaultAsync(x => x.Id == key && x.Filename.Trim() == fileName);
                        if (entry == null)
                        {
                            entry = new VzTovarImage
                            {
                                Id = key,
                                IsMain = mainPhoto,
                                Filename = fileName,
                                Url = link.AbsoluteUri,
                                Extension = "zip",
                                Photo = zipped
                            };
                            await context.VzTovarImages.AddAsync(entry);
                        }
                        else
                        {
                            entry.Url = link.AbsoluteUri;
                            entry.Extension = "zip";
                            entry.Photo = zipped;
                            context.Update(entry);
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch
            { }
        }
    }
}
