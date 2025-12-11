namespace BlogApp.Models;

public class BlogPost
{
    public int Id { get; set; }

    public int UserId { get; set; } // Yazıyı yazan kullanıcı
    public User? User { get; set; } // Navigation

    public int CategoryId { get; set; } // Yazı kategorisi
    public Category? Category { get; set; } // Navigation

    public string Title { get; set; } = string.Empty; // Yazı başlığı

    public string Content { get; set; } = string.Empty; // Yazı içeriği (uzun text)

    public string? CoverImage { get; set; } // Kapak fotoğrafı (opsiyonel)

    public bool IsDraft { get; set; } = false; // Taslak mı?

    public int ViewCount { get; set; } = 0; // Gösterim sayısı

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Oluşturulma
    public DateTime? UpdatedAt { get; set; } // Güncellenme

    // Navigation
    public List<Comment>? Comments { get; set; }
    public List<PostLike>? Likes { get; set; }
}
