namespace HealthRecord.API.Models.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? BloodType { get; set; }
    public string? ChronicConditions { get; set; }
    public string? Allergies { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = [];
    public ICollection<HealthRecord> HealthRecords { get; set; } = [];
    public ICollection<BloodPressureDetail> BloodPressures { get; set; } = [];
    public ICollection<LabResultDetail> LabResults { get; set; } = [];
    public ICollection<MedicationDetail> Medications { get; set; } = [];
    public ICollection<NhiImportLog> NhiImportLogs { get; set; } = [];
    public ICollection<UserLabItem> UserLabItems { get; set; } = [];
}
