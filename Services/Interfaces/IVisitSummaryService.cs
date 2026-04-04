using HealthRecord.API.Models.DTOs.VisitSummary;

namespace HealthRecord.API.Services.Interfaces;

public interface IVisitSummaryService
{
    Task<VisitSummaryJsonResponse> GetSummaryJsonAsync(int userId, int visitId);
    Task<byte[]> GeneratePdfAsync(int userId, int visitId);
    Task<int?> GetLatestVisitIdAsync(int userId);
}
