namespace HealthRecord.API.Models.Entities;

public class UserLabItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ItemCode { get; set; } = default!;   // NHI 申報代碼，如 '09015C'
    public string ItemName { get; set; } = default!;   // NHI 子項目名稱，如 'CRE(肌酸酐)'
    public string Unit { get; set; } = "";
    public string Category { get; set; } = "其他";
    public decimal? NormalMin { get; set; }
    public decimal? NormalMax { get; set; }
    public bool IsPreset { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = default!;
}
