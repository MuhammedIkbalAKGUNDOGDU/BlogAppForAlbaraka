using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BCrypt.Net;
using BlogApp.Data;
using BlogApp.DTOs;
using BlogApp.Models;

namespace BlogApp.Controllers
{
    [Route("api/profile")]
    [ApiController]
    public class ProfileApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileApiController(AppDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------
        // UPDATE PROFILE
        // ----------------------------------------------------
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return BadRequest(new { message = "User not found" });

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                user.LastName = dto.LastName;

            if (!string.IsNullOrWhiteSpace(dto.Email))
                user.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.ProfileImage))
                user.ProfileImage = dto.ProfileImage;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully" });
        }

        // ----------------------------------------------------
        // CHANGE PASSWORD
        // ----------------------------------------------------
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return BadRequest(new { message = "User not found" });

            // eski şifre kontrol
            var valid = BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash);
            if (!valid)
                return BadRequest(new { message = "Old password is incorrect" });

            // yeni şifre
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Password updated successfully" });
        }

        // ----------------------------------------------------
// GET PROFILE (Mevcut Bilgileri Getir)
// ----------------------------------------------------
[HttpGet]
public async Task<IActionResult> GetProfile()
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    var user = await _context.Users
        .AsNoTracking()
        .Where(x => x.Id == userId)
        .Select(x => new
        {
            x.FirstName,
            x.LastName,
            x.Email,
            x.ProfileImage
        })
        .FirstOrDefaultAsync();

    if (user == null)
        return NotFound(new { message = "User not found" });

    return Ok(user);
}

        // GET: api/profile/followers
        [HttpGet("followers")]
        public async Task<IActionResult> GetFollowers([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { message = "Kullanıcı ID gereklidir." });
            }

            var followers = await _context.Set<UserFollower>()
                .Include(uf => uf.Follower)
                .Where(uf => uf.FollowingId == userId)
                .Select(uf => new
                {
                    uf.Follower!.Id,
                    uf.Follower.FirstName,
                    uf.Follower.LastName,
                    uf.Follower.Email,
                    uf.Follower.ProfileImage,
                    uf.CreatedAt
                })
                .OrderByDescending(uf => uf.CreatedAt)
                .ToListAsync();

            return Ok(followers);
        }

        // GET: api/profile/following
        [HttpGet("following")]
        public async Task<IActionResult> GetFollowing([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { message = "Kullanıcı ID gereklidir." });
            }

            var following = await _context.Set<UserFollower>()
                .Include(uf => uf.Following)
                .Where(uf => uf.FollowerId == userId)
                .Select(uf => new
                {
                    uf.Following!.Id,
                    uf.Following.FirstName,
                    uf.Following.LastName,
                    uf.Following.Email,
                    uf.Following.ProfileImage,
                    uf.CreatedAt
                })
                .OrderByDescending(uf => uf.CreatedAt)
                .ToListAsync();

            return Ok(following);
        }
    }
}
