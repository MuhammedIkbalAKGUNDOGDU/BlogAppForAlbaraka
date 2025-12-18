namespace BlogApp.Models;

public enum UserStatus
{
    Active = 0,
    Suspended = 1,
    Banned = 2
}

public class User
{
    public int Id { get; set; } 

    public string FirstName { get; set; } = string.Empty; 
    public string LastName { get; set; } = string.Empty;  

    public string Email { get; set; } = string.Empty; 
    public string PasswordHash { get; set; } = string.Empty; 

    public string? ProfileImage { get; set; } 

    public string Role { get; set; } = "user"; 

    public bool IsActive { get; set; } = true; 
    
    public UserStatus Status { get; set; } = UserStatus.Active; 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 


    public List<BlogPost>? Posts { get; set; } 
    public List<Comment>? Comments { get; set; } 
    public List<PostLike>? Likes { get; set; } 

    public List<UserFollower>? Followers { get; set; } 
    public List<UserFollower>? Following { get; set; } 
}
