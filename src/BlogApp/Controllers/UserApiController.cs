using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using BlogApp.Models;

namespace BlogApp.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserApiController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/user/{id}/follow
        [HttpPost("{id}/follow")]
        public async Task<IActionResult> ToggleFollow(int id, [FromBody] int followerId)
        {
            if (followerId <= 0)
            {
                return BadRequest(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            if (id == followerId)
            {
                return BadRequest(new { message = "Kendinizi takip edemezsiniz." });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            var existingFollow = await _context.Set<UserFollower>()
                .FirstOrDefaultAsync(uf => uf.FollowingId == id && uf.FollowerId == followerId);

            if (existingFollow != null)
            {
                // Takibi kaldır
                _context.Set<UserFollower>().Remove(existingFollow);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Takipten çıkıldı.", isFollowing = false });
            }
            else
            {
                // Takip et
                var follow = new UserFollower
                {
                    FollowerId = followerId,
                    FollowingId = id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Set<UserFollower>().Add(follow);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Takip edildi.", isFollowing = true });
            }
        }

        // GET: api/user/{id}/is-following
        [HttpGet("{id}/is-following")]
        public async Task<IActionResult> IsFollowing(int id, [FromQuery] int followerId)
        {
            if (followerId <= 0)
            {
                return Ok(new { isFollowing = false });
            }

            var isFollowing = await _context.Set<UserFollower>()
                .AnyAsync(uf => uf.FollowingId == id && uf.FollowerId == followerId);

            return Ok(new { isFollowing });
        }
    }
}
