namespace BlogApp.DTOs;

public class BlogPostCreateDto
{
    public int UserId { get; set; } // Frontend'den localStorage'dan gönderilecek
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CoverImage { get; set; } // Opsiyonel
}
