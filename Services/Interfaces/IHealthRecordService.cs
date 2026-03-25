using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.HealthRecord;

namespace HealthRecord.API.Services.Interfaces;

public interface IHealthRecordService
{
    Task<PagedResult<HealthRecordResponse>> GetListAsync(int userId, int page, int pageSize, DateTime? from, DateTime? to);
    Task<HealthRecordDetailResponse> GetByIdAsync(int userId, int id);
    Task DeleteAsync(int userId, int id);
}
