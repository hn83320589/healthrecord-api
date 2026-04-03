using HealthRecord.API.Models.DTOs.Nhi;

namespace HealthRecord.API.Services.Interfaces;

public interface INhiImportService
{
    Task<NhiImportResponse> ImportAsync(int userId, string json, string? fileName);
    Task<List<NhiImportLogResponse>> GetLogsAsync(int userId);
    Task RevokeAsync(int userId, int logId);
}
