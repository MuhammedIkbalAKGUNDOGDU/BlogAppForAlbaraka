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

        // GET: BlogPost/Details/{id}
        public IActionResult Details(int id)
        {
            return View();
        }
    }

    [Route("api/blogpost")]
    [ApiController]
    public class BlogPostApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BlogPostApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/blogpost/published
        [HttpGet("published")]
        public async Task<IActionResult> GetPublishedPosts()
        {
            var posts = await _context.BlogPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Where(p => p.IsDraft == false)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    p.CoverImage,
                    p.CreatedAt,
                    p.ViewCount,
                    User = new { p.User!.Id, p.User.FirstName, p.User.LastName, p.User.ProfileImage },
                    Category = new { p.Category!.Name },
                    CommentCount = p.Comments != null ? p.Comments.Count : 0,
                    LikeCount = p.Likes != null ? p.Likes.Count : 0
                })
                .ToListAsync();

            return Ok(posts);
        }

        // GET: api/blogpost/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var post = await _context.BlogPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Comments!)
                    .ThenInclude(c => c.User)
                .Include(p => p.Likes!)
                    .ThenInclude(l => l.User)
                .Where(p => p.Id == id && p.IsDraft == false)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    p.CoverImage,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.ViewCount,
                    User = new 
                    { 
                        p.User!.Id, 
                        p.User.FirstName, 
                        p.User.LastName, 
                        p.User.ProfileImage,
                        p.User.Email
                    },
                    Category = new { p.Category!.Id, p.Category.Name },
                    Comments = p.Comments.OrderByDescending(c => c.CreatedAt).Select(c => new
                    {
                        c.Id,
                        c.Content,
                        c.CreatedAt,
                        User = new { c.User!.Id, c.User.FirstName, c.User.LastName, c.User.ProfileImage }
                    }).ToList(),
                    LikeCount = p.Likes.Count
                })
                .FirstOrDefaultAsync();

            if (post == null)
            {
                return NotFound(new { message = "Yazı bulunamadı." });
            }

            // ViewCount artır
            var postEntity = await _context.BlogPosts.FindAsync(id);
            if (postEntity != null)
            {
                postEntity.ViewCount++;
                await _context.SaveChangesAsync();
            }

            return Ok(post);
        }

        // POST: api/blogpost/{id}/comment
        [HttpPost("{id}/comment")]
        public async Task<IActionResult> AddComment(int id, [FromBody] CommentCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { message = "Yorum içeriği gereklidir." });
            }

            if (dto.UserId <= 0)
            {
                return BadRequest(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound(new { message = "Yazı bulunamadı." });
            }

            var comment = new Comment
            {
                PostId = id,
                UserId = dto.UserId,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Yorumu kullanıcı bilgisiyle birlikte döndür
            var commentWithUser = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.Id == comment.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.CreatedAt,
                    User = new { c.User!.Id, c.User.FirstName, c.User.LastName, c.User.ProfileImage }
                })
                .FirstOrDefaultAsync();

            return Ok(new { message = "Yorum eklendi.", comment = commentWithUser });
        }

        // POST: api/blogpost/{id}/like
        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id, [FromBody] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound(new { message = "Yazı bulunamadı." });
            }

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == id && l.UserId == userId);

            if (existingLike != null)
            {
                // Like'ı kaldır
                _context.PostLikes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Beğeni kaldırıldı.", liked = false });
            }
            else
            {
                // Like ekle
                var like = new PostLike
                {
                    PostId = id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PostLikes.Add(like);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Beğenildi.", liked = true });
            }
        }

        // GET: api/blogpost/{id}/is-liked
        [HttpGet("{id}/is-liked")]
        public async Task<IActionResult> IsLiked(int id, [FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return Ok(new { isLiked = false });
            }

            var isLiked = await _context.PostLikes
                .AnyAsync(l => l.PostId == id && l.UserId == userId);

            return Ok(new { isLiked });
        }
    }
}
