namespace BlogApp.Models;

public class EmailQueue
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public BlogPost? Post { get; set; }
    public int UserId { get; set; } 
    public User? User { get; set; }
    public string Status { get; set; } = "Pending"; 
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; } = 0;
}
