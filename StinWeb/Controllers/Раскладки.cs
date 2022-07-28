using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using StinWeb.Models.DataManager;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class Раскладки : Controller
    {
        private readonly StinDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public Раскладки(StinDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetImage(string id)
        {
            var imageData = (from v in _context.VzImages
                             where v.Id == id
                             select new
                             {
                                 image = v.Image,
                                 type = v.Dtype ?? "png"
                             }).FirstOrDefault();
            byte[] image = null;
            string imageType = "image/png";
            if (imageData != null && imageData.image != null)
            {
                imageType = imageData.type.Trim().ToLower();
                if (imageType == "zip")
                {
                    var fileData = (await Common.UnZip(imageData.image)).FirstOrDefault();
                    imageType = Path.GetExtension(fileData.Key).Trim('.').ToLower();
                    image = fileData.Value;
                }
                else
                    image = imageData.image;
                switch (imageType)
                {
                    case "pdf":
                        imageType = "application/pdf";
                        break;
                    case "png":
                        imageType = "image/png";
                        break;
                    default:
                        imageType = "image/png";
                        break;
                }
            }
            else
            {
                string webRootPath = _webHostEnvironment.WebRootPath;
                string contentRootPath = _webHostEnvironment.ContentRootPath;

                string path = System.IO.Path.Combine(webRootPath, "lib", "images", "not-found-image.jpg");
                image = System.IO.File.ReadAllBytes(path);
            }
            return File(image, imageType);
        }
        [HttpPost]
        //public async Task<IActionResult> OnBtnLoad(string id, IList<IFormFile> files, string path)
        public async Task<IActionResult> OnBtnLoad(string НоменклатураId, IFormFile source)
        {
            try
            {
                VzImage vzI = (from vzImages in _context.VzImages
                                where vzImages.Id == НоменклатураId
                                select vzImages).FirstOrDefault();
                if (vzI == null)
                {
                    vzI = new VzImage { Id = НоменклатураId, Dtype = "zip", Image = await Common.CreateZip(source) };
                    await _context.VzImages.AddAsync(vzI);
                }
                else
                {
                    vzI.Image = await Common.CreateZip(source);
                    vzI.Dtype = "zip";
                    _context.Update(vzI);
                }
                //foreach (IFormFile source in files)
                //{
                //string filename = ContentDispositionHeaderValue.Parse(source.ContentDisposition).FileName.Trim('"');
                //string extension = Path.GetExtension(filename).Trim('.').ToLower();
                //using (MemoryStream ms = new MemoryStream())
                //{
                //    await source.CopyToAsync(ms);
                //}
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string ss = ex.Message;
            }
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> OnBtnDelete(string id)
        {
            VzImage vzI = (from vzImages in _context.VzImages
                            where vzImages.Id == id
                            select vzImages).FirstOrDefault();
            if (vzI != null)
            {
                _context.VzImages.Remove(vzI);
                await _context.SaveChangesAsync();
                return Ok();
            }
            else
                return NoContent();
        }

    }
}
