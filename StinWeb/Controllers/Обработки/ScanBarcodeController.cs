using Microsoft.AspNetCore.Mvc;

namespace StinWeb.Controllers.Обработки
{
    public class ScanBarcodeController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Обработки/ScanBarcode.cshtml");
        }
    }
}
