using BlogApp.Models;

namespace BlogApp.DTOs;

public class UserStatusUpdateDto
{
    public int UserId { get; set; }
    public UserStatus Status { get; set; }
}
