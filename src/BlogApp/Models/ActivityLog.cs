namespace BlogApp.Models;

public class ActivityLog
{
    public int Id { get; set; }

    public int? UserId { get; set; } 
    public User? User { get; set; } 

    public string Action { get; set; } = string.Empty; 
    public string Controller { get; set; } = string.Empty; 
    
    public string RequestMethod { get; set; } = string.Empty; 
    public string RequestPath { get; set; } = string.Empty; 
    
    public string IPAddress { get; set; } = string.Empty; 
    
    public string Description { get; set; } = string.Empty; 
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
}

