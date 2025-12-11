namespace BlogApp.Models;

public class PostLike
{
    public int Id { get; set; }

    public int UserId { get; set; } // Beğenen kullanıcı
    public User? User { get; set; }

    public int PostId { get; set; } // Beğenilen gönderi
    public BlogPost? Post { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Beğenme zamanı
}
