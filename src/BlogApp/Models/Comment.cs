namespace BlogApp.Models;

public class Comment
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int PostId { get; set; }
    public BlogPost? Post { get; set; }

    public string Content { get; set; } = string.Empty; // Yorum metni

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Yorum tarihi
}
