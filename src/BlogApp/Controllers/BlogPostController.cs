using BlogApp.Data;
using BlogApp.DTOs;
using BlogApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Filters;

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
        [LogActivity("Blog yazısı oluşturuldu")]
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
                .Include(p => p.Comments)
                .Include(p => p.Likes)
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

        // GET: BlogPost/Edit/{id}
        public IActionResult Edit(int id)
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
        public async Task<IActionResult> GetPublishedPosts([FromQuery] string? search = null, [FromQuery] int? categoryId = null, [FromQuery] string? sortBy = "date")
        {
            var query = _context.BlogPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Where(p => p.IsDraft == false && 
                           p.User != null && 
                           p.User.Status == UserStatus.Active); // Sadece aktif kullanıcıların yazıları

            // Kategori filtresi
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Arama filtresi (başlık, içerik veya yazar adı)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(searchTerm) || 
                    p.Content.ToLower().Contains(searchTerm) ||
                    (p.User != null && (p.User.FirstName.ToLower().Contains(searchTerm) || 
                                        p.User.LastName.ToLower().Contains(searchTerm) ||
                                        (p.User.FirstName + " " + p.User.LastName).ToLower().Contains(searchTerm)))
                );
            }

            // Önce verileri çek
            var posts = await query
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    p.CoverImage,
                    p.CreatedAt,
                    p.ViewCount,
                    p.CategoryId,
                    User = new { p.User!.Id, p.User.FirstName, p.User.LastName, p.User.ProfileImage },
                    Category = new { p.Category!.Id, p.Category.Name },
                    CommentCount = p.Comments != null ? p.Comments.Count : 0,
                    LikeCount = p.Likes != null ? p.Likes.Count : 0
                })
                .ToListAsync();

            // Popülerlik puanı hesaplama katsayıları
            const double LIKE_WEIGHT = 3.0;      // Beğeni katsayısı
            const double COMMENT_WEIGHT = 2.0;  // Yorum katsayısı
            const double VIEW_WEIGHT = 0.1;      // Görüntülenme katsayısı (daha düşük çünkü sayıları çok yüksek olabilir)

            // Arama varsa relevance score'a göre sırala (memory'de)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                posts = posts
                    .Select(p => new
                    {
                        p.Id,
                        p.Title,
                        p.Content,
                        p.CoverImage,
                        p.CreatedAt,
                        p.ViewCount,
                        p.CategoryId,
                        p.User,
                        p.Category,
                        p.CommentCount,
                        p.LikeCount,
                        // Relevance score: başlıkta geçiyorsa 3, yazar adında geçiyorsa 2, içerikte geçiyorsa 1
                        RelevanceScore = (p.Title.ToLower().Contains(searchTerm) ? 3 : 0) +
                                        (p.User != null && (p.User.FirstName.ToLower().Contains(searchTerm) || 
                                                           p.User.LastName.ToLower().Contains(searchTerm) ||
                                                           (p.User.FirstName + " " + p.User.LastName).ToLower().Contains(searchTerm)) ? 2 : 0) +
                                        (p.Content.ToLower().Contains(searchTerm) ? 1 : 0),
                        // Popülerlik puanı: (beğeni * 3) + (yorum * 2) + (görüntülenme * 0.1)
                        PopularityScore = (p.LikeCount * LIKE_WEIGHT) + (p.CommentCount * COMMENT_WEIGHT) + (p.ViewCount * VIEW_WEIGHT)
                    })
                    .OrderByDescending(p => p.RelevanceScore)
                    .ThenByDescending(p => sortBy == "popularity" 
                        ? p.PopularityScore 
                        : (double)p.CreatedAt.Ticks) // Ticks'i double'a çevirerek tip uyumluluğu sağla
                    .Select(p => new
                    {
                        p.Id,
                        p.Title,
                        p.Content,
                        p.CoverImage,
                        p.CreatedAt,
                        p.ViewCount,
                        p.CategoryId,
                        p.User,
                        p.Category,
                        p.CommentCount,
                        p.LikeCount
                    })
                    .ToList();
            }
            else
            {
                // Arama yoksa sıralama tipine göre sırala
                if (sortBy == "popularity")
                {
                    // Popülerlik puanına göre sırala
                    posts = posts
                        .Select(p => new
                        {
                            p.Id,
                            p.Title,
                            p.Content,
                            p.CoverImage,
                            p.CreatedAt,
                            p.ViewCount,
                            p.CategoryId,
                            p.User,
                            p.Category,
                            p.CommentCount,
                            p.LikeCount,
                            // Popülerlik puanı: (beğeni * 3) + (yorum * 2) + (görüntülenme * 0.1)
                            PopularityScore = (p.LikeCount * LIKE_WEIGHT) + (p.CommentCount * COMMENT_WEIGHT) + (p.ViewCount * VIEW_WEIGHT)
                        })
                        .OrderByDescending(p => p.PopularityScore)
                        .ThenByDescending(p => p.CreatedAt) // Aynı puanda tarihe göre
                        .Select(p => new
                        {
                            p.Id,
                            p.Title,
                            p.Content,
                            p.CoverImage,
                            p.CreatedAt,
                            p.ViewCount,
                            p.CategoryId,
                            p.User,
                            p.Category,
                            p.CommentCount,
                            p.LikeCount
                        })
                        .ToList();
                }
                else
                {
                    // Tarihe göre sırala (yeniden eskiye - default)
                    posts = posts
                        .OrderByDescending(p => p.CreatedAt)
                        .ToList();
                }
            }

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
                .Where(p => p.Id == id && 
                           p.IsDraft == false && 
                           p.User != null && 
                           p.User.Status == UserStatus.Active) // Sadece aktif kullanıcıların yazıları
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
        [LogActivity("Yorum eklendi")]
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
        [LogActivity("Blog yazısı beğenildi/beğeni kaldırıldı")]
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

        // GET: api/blogpost/{id}/edit
        [HttpGet("{id}/edit")]
        public async Task<IActionResult> GetPostForEdit(int id, [FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var post = await _context.BlogPosts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound(new { message = "Yazı bulunamadı." });
            }

            // Sadece yazının sahibi düzenleyebilir
            if (post.UserId != userId)
            {
                return Unauthorized(new { message = "Bu yazıyı düzenleme yetkiniz yok." });
            }

            return Ok(new
            {
                post.Id,
                post.Title,
                post.Content,
                post.CoverImage,
                post.CategoryId,
                Category = new { post.Category!.Id, post.Category.Name }
            });
        }

        // PUT: api/blogpost/{id}
        [HttpPut("{id}")]
        [LogActivity("Blog yazısı güncellendi")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] BlogPostUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { message = "Başlık ve içerik zorunludur." });
            }

            if (dto.CategoryId <= 0)
            {
                return BadRequest(new { message = "Kategori seçimi zorunludur." });
            }

            if (dto.UserId <= 0)
            {
                return BadRequest(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            // Yazıyı bul
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound(new { message = "Yazı bulunamadı." });
            }

            // Sadece yazının sahibi güncelleyebilir
            if (post.UserId != dto.UserId)
            {
                return Unauthorized(new { message = "Bu yazıyı düzenleme yetkiniz yok." });
            }

            // Kategori var mı kontrol et
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = "Geçersiz kategori seçimi." });
            }

            // Yazıyı güncelle
            post.CategoryId = dto.CategoryId;
            post.Title = dto.Title.Trim();
            post.Content = dto.Content.Trim();
            post.CoverImage = string.IsNullOrWhiteSpace(dto.CoverImage) ? null : dto.CoverImage.Trim();
            post.UpdatedAt = DateTime.UtcNow;
            
            // Yazı düzenlendiğinde tekrar admin onayına gönder (draft durumuna düşür)
            post.IsDraft = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Yazı başarıyla güncellendi. Admin onayından sonra tekrar yayınlanacaktır.", postId = post.Id });
        }

        // DELETE: api/blogpost/{id}
        [HttpDelete("{id}")]
        [LogActivity("Blog yazısı silindi")]
        public async Task<IActionResult> DeletePost(int id, [FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            // Yazıyı bul
            var post = await _context.BlogPosts
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound(new { message = "Yazı bulunamadı." });
            }

            // Sadece yazının sahibi silebilir
            if (post.UserId != userId)
            {
                return Unauthorized(new { message = "Bu yazıyı silme yetkiniz yok." });
            }

            // İlişkili yorumları sil
            if (post.Comments != null && post.Comments.Any())
            {
                _context.Comments.RemoveRange(post.Comments);
            }

            // İlişkili beğenileri sil
            if (post.Likes != null && post.Likes.Any())
            {
                _context.PostLikes.RemoveRange(post.Likes);
            }

            // Yazıyı sil
            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Yazı başarıyla silindi." });
        }
    }
}
