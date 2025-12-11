using Microsoft.EntityFrameworkCore;
using BlogApp.Models;

namespace BlogApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<EmailQueue> EmailQueues { get; set; }  // Email kuyruğu için DbSet

        protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // --- USER EMAIL UNIQUE ---
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique();

    // --- USER FOLLOW SYSTEM (SELF RELATION MANY-TO-MANY) ---
    modelBuilder.Entity<UserFollower>()
        .HasOne(uf => uf.Follower)
        .WithMany(u => u.Following)
        .HasForeignKey(uf => uf.FollowerId)
        .OnDelete(DeleteBehavior.NoAction);

    modelBuilder.Entity<UserFollower>()
        .HasOne(uf => uf.Following)
        .WithMany(u => u.Followers)
        .HasForeignKey(uf => uf.FollowingId)
        .OnDelete(DeleteBehavior.NoAction);
}
    }
}
