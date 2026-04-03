namespace HealthRecord.API.Models.Entities;

public class UserLabItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ItemCode { get; set; } = default!;  // r7.8 NHI code e.g. '09015C'
    public string ItemName { get; set; } = default!;  // r7.10 sub-item e.g. 'CRE(肌酸酐)'
    public string? DisplayName { get; set; }           // user-defined e.g. '肌酸酐'
    public string Unit { get; set; } = "";
    public string Category { get; set; } = "其他";
    public decimal? NormalMin { get; set; }
    public decimal? NormalMax { get; set; }
    public int SortOrder { get; set; }
    public bool IsPreset { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = default!;
    public ICollection<LabResultDetail> LabResults { get; set; } = [];
}
