using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using BlogApp.DTOs;
using BlogApp.Models;

namespace BlogApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryController(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories
                .Select(c => new { c.Id, c.Name })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto)
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Kategori adı gereklidir." });
            }

            // Aynı isimde kategori var mı kontrol et
            var exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower());
            if (exists)
            {
                return BadRequest(new { message = "Bu kategori zaten mevcut." });
            }

            var category = new Category
            {
                Name = dto.Name.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kategori başarıyla oluşturuldu.", category = new { category.Id, category.Name } });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Kategori bulunamadı." });
            }

            // Kategoriye ait post var mı kontrol edelim bi önce bakalım
            var hasPosts = await _context.BlogPosts.AnyAsync(p => p.CategoryId == id);
            if (hasPosts)
            {
                return BadRequest(new { message = "Bu kategoriye ait yazılar bulunduğu için silinemez." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kategori başarıyla silindi." });
        }

        private bool IsAdminLoggedIn()
        {
            return !string.IsNullOrEmpty(_httpContextAccessor.HttpContext?.Session.GetString("AdminId"));
        }
    }
}
