using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BlogApp.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View(); 
        }
    }
}
