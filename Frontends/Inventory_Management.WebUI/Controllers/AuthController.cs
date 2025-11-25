using Inventory_Management.Application.Features.Queries.UsersQuery;
using Inventory_Management.WebUI.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Inventory_Management.WebUI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. ViewModel'i Query'ye çevir
            var loginQuery = new LoginUsersQuery // (Not: Sınıf isminizi LoginUserQuery yaptıysanız onu kullanın)
            {
                Email = model.Email,
                Password = model.Password
            };

            try
            {
                var result = await _mediator.Send(loginQuery);

                if (result.IsSuccess) // Giriş Başarılıysa
                {
                    // 1. Kullanıcı Bilgilerini Hazırla
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, result.FullName),
                new Claim(ClaimTypes.Email, model.Email)
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(60)
                    };

                    // 2. Giriş Yap (Cookie Oluştur)
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Index", "Home");
                }

                // --- DÜZELTME BURADA (ELSE BLOĞU EKLENDİ) ---
                else
                {
                    // Giriş başarısızsa (şifre yanlışsa), hatayı göster ve sayfayı tekrar yükle
                    ViewBag.Error = result.ErrorMessage;
                    return View(model); // Burası eksikti!
                }
                // ---------------------------------------------
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Bir hata oluştu: " + ex.Message;
                return View(model);
            }
        }
        // LOGOUT EKLENDİ
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
