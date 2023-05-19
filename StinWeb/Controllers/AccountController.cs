using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StinWeb.Models.DataManager.Справочники;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using StinWeb.Models.Repository.Справочники;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class AccountController : Controller
    {
        private UserRepository _userRepository;
        public AccountController(StinDbContext context)
        {
            _userRepository = new UserRepository(context);
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User model)
        {
            if (ModelState.IsValid)
            {
                User user = await _userRepository.GetUserAsync(model.Name, model.Password);

                if (user != null)
                {
                    await Authenticate(user); // аутентификация

                    if (user.Department.ToLower().Trim() == "водители")
                        return RedirectToAction("Console", "ИнтернетЗаказы");
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }
        private async Task Authenticate(User user)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Name),
                new Claim("UserRowId", user.RowId.ToString()),
                new Claim("UserId", user.Id),
                new Claim("Имя", user.FullName),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role),
                new Claim("Отдел", user.Department)
            };
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public async Task<IActionResult> SetUserFromQueryParams(string userName, string password)
        {
            return await Login(new User { Name = userName, Password = password });
        }
        protected override void Dispose(bool disposing)
        {
            _userRepository.Dispose();
            base.Dispose(disposing);
        }
    }
}
