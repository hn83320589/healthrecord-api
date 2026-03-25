namespace HealthRecord.API.Models.Entities;

public class EmergencyContact
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = default!;
    public string Relationship { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? Note { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
}
