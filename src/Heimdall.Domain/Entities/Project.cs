namespace Heimdall.Domain.Entities;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
}
