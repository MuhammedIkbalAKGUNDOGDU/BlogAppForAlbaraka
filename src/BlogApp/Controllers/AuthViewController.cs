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

        // GET: AuthView/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // GET: AuthView/ResetPassword
        public IActionResult ResetPassword()
        {
            return View();
        }
    }
}
