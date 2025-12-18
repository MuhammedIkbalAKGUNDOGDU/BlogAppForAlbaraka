namespace BlogApp.Models;

public class BlogPost
{
    public int Id { get; set; }

    public int UserId { get; set; } 
    public User? User { get; set; } 

    public int CategoryId { get; set; } 
    public Category? Category { get; set; } 

    public string Title { get; set; } = string.Empty; 

    public string Content { get; set; } = string.Empty; 

    public string? CoverImage { get; set; } 

    public bool IsDraft { get; set; } = false; 

    public int ViewCount { get; set; } = 0; 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
    public DateTime? UpdatedAt { get; set; } 

    public List<Comment>? Comments { get; set; }
    public List<PostLike>? Likes { get; set; }
}
