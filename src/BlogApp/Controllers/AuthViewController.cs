using Microsoft.AspNetCore.Mvc;

namespace BlogApp.Controllers
{
    public class AuthViewController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }
    }
}
