namespace HealthRecord.API.Models.Entities;

public class MedicationReminder
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? MedicationDetailId { get; set; }
    public string MedicationName { get; set; } = default!;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string RemindTimes { get; set; } = "[\"08:00\"]"; // JSON array
    public string DaysOfWeek { get; set; } = "MON,TUE,WED,THU,FRI,SAT,SUN";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = default!;
    public MedicationDetail? MedicationDetail { get; set; }
    public ICollection<MedicationLog> Logs { get; set; } = [];
}
