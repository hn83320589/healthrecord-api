using HealthRecord.API.Models.DTOs.Visit;

namespace HealthRecord.API.Services.Interfaces;

public interface IVisitRelationService
{
    Task<VisitRelatedResponse> GetVisitRelatedAsync(int userId, int visitId);
    Task<List<VisitTimelineItemDto>> GetTimelineAsync(int userId, DateTime? startDate, DateTime? endDate);
    Task<List<LabResultWithStatusDto>> GetLabsByVisitAsync(int userId, int visitId);
    Task<List<BloodPressureWithDateDto>> GetBpAroundDateAsync(int userId, DateTime date, int dayRange = 3);
}
