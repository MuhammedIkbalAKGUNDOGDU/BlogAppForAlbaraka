namespace BlogApp.Models;

public class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public string Token { get; set; } = string.Empty; // Rastgele token

    public DateTime ExpireAt { get; set; } // Token geçerlilik süresi
}
