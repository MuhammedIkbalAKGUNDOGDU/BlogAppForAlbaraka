namespace BlogApp.DTOs;

public class BlogPostUpdateDto
{
    public int PostId { get; set; }  
    public int UserId { get; set; }  
    public int CategoryId { get; set; }  
    public string Title { get; set; } = string.Empty; 
    public string Content { get; set; } = string.Empty;  
    public string? CoverImage { get; set; } 
}
