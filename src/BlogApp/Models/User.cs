namespace BlogApp.Models;

public class User
{
    public int Id { get; set; } // Kullanıcı benzersiz ID

    public string FirstName { get; set; } = string.Empty; // Ad
    public string LastName { get; set; } = string.Empty;  // Soyad

    public string Email { get; set; } = string.Empty; // Giriş için kullanılan email
    public string PasswordHash { get; set; } = string.Empty; // Şifre hash (asla plain text tutulmaz)

    public string? ProfileImage { get; set; } // Profil resmi (opsiyonel)

    public string Role { get; set; } = "user"; // "user" veya "admin"

    public bool IsActive { get; set; } = true; // Admin kullanıcıyı dondurursa false olur

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Hesap oluşturma tarihi


    // Navigation Properties (ilişkiler)
    public List<BlogPost>? Posts { get; set; } // Kullanıcının yazıları
    public List<Comment>? Comments { get; set; } // Kullanıcının yorumları
    public List<PostLike>? Likes { get; set; } // Kullanıcının beğendiği yazılar

    public List<UserFollower>? Followers { get; set; } // Bu kullanıcıyı takip edenler
    public List<UserFollower>? Following { get; set; } // Bu kullanıcının takip ettikleri
}
