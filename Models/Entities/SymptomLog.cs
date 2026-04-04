namespace HealthRecord.API.Models.Entities;

public class SymptomLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime LoggedAt { get; set; }
    public string SymptomType { get; set; } = default!;
    public int Severity { get; set; }            // 1-10
    public string? BodyLocation { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Triggers { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
}
