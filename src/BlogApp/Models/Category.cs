namespace BlogApp.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty; // Kategori adı örn: Teknoloji, Spor...

    // Navigation
    public List<BlogPost>? Posts { get; set; } // Bu kategoriye ait yazılar
}
