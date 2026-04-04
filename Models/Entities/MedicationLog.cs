namespace HealthRecord.API.Models.Entities;

public class MedicationLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? ReminderId { get; set; }
    public string MedicationName { get; set; } = default!;
    public string? Dosage { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? TakenAt { get; set; }
    public string Status { get; set; } = "pending"; // pending | taken | late | skipped | missed
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
    public MedicationReminder? Reminder { get; set; }
}
