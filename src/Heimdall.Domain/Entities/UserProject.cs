namespace Heimdall.Domain.Entities;

public class UserProject
{
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
