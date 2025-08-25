namespace AssetManagement.Domain.Entities;

public class Group
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    // Navigation properties
    public List<UserGroup> UserGroups { get; set; } = new();
}
