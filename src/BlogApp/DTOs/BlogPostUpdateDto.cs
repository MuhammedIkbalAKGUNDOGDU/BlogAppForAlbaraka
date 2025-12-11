namespace BlogApp.DTOs;

public class BlogPostUpdateDto
{
    public int PostId { get; set; }  // Güncellenecek post ID
    public int UserId { get; set; }  // Yazı sahibi kontrolü için
    public int CategoryId { get; set; }  // Kategori ID
    public string Title { get; set; } = string.Empty;  // Başlık
    public string Content { get; set; } = string.Empty;  // İçerik
    public string? CoverImage { get; set; }  // Kapak resmi (opsiyonel)
}
