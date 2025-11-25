using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Inventory_Management.WebUI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            //var userName = User.Identity?.Name;
            //ViewBag.UserName = userName;
            return View();
        }
    }
}
