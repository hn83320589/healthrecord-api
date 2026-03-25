using HealthRecord.API.Models.DTOs.BloodPressure;
using HealthRecord.API.Models.DTOs.Common;

namespace HealthRecord.API.Services.Interfaces;

public interface IBloodPressureService
{
    Task<PagedResult<BloodPressureResponse>> GetListAsync(int userId, int page, int pageSize, DateTime? from, DateTime? to);
    Task<BloodPressureResponse> GetByIdAsync(int userId, int id);
    Task<BloodPressureResponse> CreateAsync(int userId, CreateBloodPressureRequest request);
    Task<BloodPressureResponse> UpdateAsync(int userId, int id, UpdateBloodPressureRequest request);
    Task DeleteAsync(int userId, int id);
    Task<BloodPressureStatsResponse> GetStatsAsync(int userId, DateTime? from, DateTime? to);
    Task<List<BloodPressureChartPoint>> GetChartDataAsync(int userId, string period);
}
