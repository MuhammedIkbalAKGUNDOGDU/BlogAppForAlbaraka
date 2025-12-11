using BlogApp.Data;
using BlogApp.DTOs;
using BlogApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Controllers
{
    public class BlogPostController : Controller
    {
        private readonly AppDbContext _context;

        public BlogPostController(AppDbContext context)
        {
            _context = context;
        }

        // GET: BlogPost/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: BlogPost/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BlogPostCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { message = "Başlık ve içerik zorunludur." });
            }

            if (dto.CategoryId <= 0)
            {
                return BadRequest(new { message = "Kategori seçimi zorunludur." });
            }

            // UserId'yi request body'den al (frontend localStorage'dan gönderecek)
            if (dto.UserId <= 0)
            {
                return BadRequest(new { message = "Kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın." });
            }

            // Kategori var mı kontrol et
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = "Geçersiz kategori seçimi." });
            }

            var post = new BlogPost
            {
                UserId = dto.UserId,
                CategoryId = dto.CategoryId,
                Title = dto.Title,
                Content = dto.Content,
                CoverImage = dto.CoverImage,
                IsDraft = true,  // Admin onayı için otomatik true
                CreatedAt = DateTime.UtcNow,
                ViewCount = 0
            };

            _context.BlogPosts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Blog yazısı başarıyla oluşturuldu. Admin onayı bekleniyor.", postId = post.Id });
        }

        // Kullanıcının yazıları
        public IActionResult MyPosts(int userId)
        {
            var posts = _context.BlogPosts
                .Include(p => p.Category)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            ViewBag.UserId = userId;

            return View(posts);
        }
    }
}
