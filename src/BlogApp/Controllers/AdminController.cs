using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using BlogApp.Models;
using BlogApp.DTOs;

namespace BlogApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Login
        public IActionResult Login()
        {
            return View();
        }

        // GET: Admin/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Admin/Login
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Kullanıcı adı ve şifre gereklidir." });
            }

            // Admin kullanıcıyı bul
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == "admin" && u.Email == dto.Username);

            if (admin == null)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı adı veya şifre." });
            }

            // Şifre kontrolü
            var isValidPassword = BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash);
            if (!isValidPassword)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı adı veya şifre." });
            }

            // Admin oturum bilgisini session'a kaydet
            HttpContext.Session.SetString("AdminId", admin.Id.ToString());
            HttpContext.Session.SetString("AdminEmail", admin.Email);

            return Ok(new { message = "Giriş başarılı", redirectUrl = "/Admin/Dashboard" });
        }

        // GET: Admin/Dashboard
        public IActionResult Dashboard()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // GET: Admin/Categories
        public IActionResult Categories()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // GET: Admin/Posts
        public IActionResult Posts()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // GET: Admin/PublishedPosts
        public IActionResult PublishedPosts()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // GET: Admin/Users
        public IActionResult Users()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // POST: Admin/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Çıkış yapıldı", redirectUrl = "/Admin/Login" });
        }

        // Helper method
        private bool IsAdminLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId"));
        }
    }
}
