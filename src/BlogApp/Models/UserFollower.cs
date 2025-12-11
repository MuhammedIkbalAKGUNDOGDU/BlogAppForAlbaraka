namespace BlogApp.Models;

public class UserFollower
{
    public int Id { get; set; }

    public int FollowerId { get; set; } // Takip eden kullanıcı
    public User? Follower { get; set; }

    public int FollowingId { get; set; } // Takip edilen kullanıcı
    public User? Following { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Takip etme zamanı
}
