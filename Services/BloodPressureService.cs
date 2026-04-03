using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.BloodPressure;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using HealthRecordEntity = HealthRecord.API.Models.Entities.HealthRecord;

namespace HealthRecord.API.Services;

public class BloodPressureService(AppDbContext db) : IBloodPressureService
{
    public async Task<PagedResult<BloodPressureResponse>> GetListAsync(
        int userId, int page, int pageSize, DateTime? from, DateTime? to)
    {
        var query = db.BloodPressureDetails
            .Include(b => b.HealthRecord)
            .Where(b => b.HealthRecord.UserId == userId);
        if (from.HasValue) query = query.Where(b => b.HealthRecord.RecordedAt >= from.Value);
        if (to.HasValue) query = query.Where(b => b.HealthRecord.RecordedAt <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.HealthRecord.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => Map(b))
            .ToListAsync();

        return new PagedResult<BloodPressureResponse>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<BloodPressureResponse> GetByIdAsync(int userId, int id)
    {
        var entity = await db.BloodPressureDetails
            .Include(b => b.HealthRecord)
            .FirstOrDefaultAsync(b => b.Id == id && b.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Record not found.");
        return Map(entity);
    }

    public async Task<BloodPressureResponse> CreateAsync(int userId, CreateBloodPressureRequest request)
    {
        var record = new HealthRecordEntity
        {
            UserId = userId,
            RecordType = "blood_pressure",
            RecordedAt = request.RecordedAt,
            Note = request.Note,
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.HealthRecords.Add(record);
        await db.SaveChangesAsync();

        var detail = new BloodPressureDetail
        {
            HealthRecordId = record.Id,
            Systolic = request.Systolic,
            Diastolic = request.Diastolic,
            Pulse = request.Pulse,
            MeasurementPosition = request.MeasurementPosition,
            Arm = request.Arm
        };
        db.BloodPressureDetails.Add(detail);
        await db.SaveChangesAsync();

        detail.HealthRecord = record;
        return Map(detail);
    }

    public async Task<BloodPressureResponse> UpdateAsync(int userId, int id, UpdateBloodPressureRequest request)
    {
        var entity = await db.BloodPressureDetails
            .Include(b => b.HealthRecord)
            .FirstOrDefaultAsync(b => b.Id == id && b.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Record not found.");

        if (entity.HealthRecord.Source != "manual")
            throw new InvalidOperationException("Only manual records can be updated.");

        entity.HealthRecord.RecordedAt = request.RecordedAt;
        entity.HealthRecord.Note = request.Note;
        entity.HealthRecord.UpdatedAt = DateTime.UtcNow;
        entity.Systolic = request.Systolic;
        entity.Diastolic = request.Diastolic;
        entity.Pulse = request.Pulse;
        entity.MeasurementPosition = request.MeasurementPosition;
        entity.Arm = request.Arm;

        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var entity = await db.BloodPressureDetails
            .Include(b => b.HealthRecord)
            .FirstOrDefaultAsync(b => b.Id == id && b.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Record not found.");

        if (entity.HealthRecord.Source != "manual")
            throw new InvalidOperationException("Only manual records can be deleted.");

        db.HealthRecords.Remove(entity.HealthRecord); // CASCADE deletes detail
        await db.SaveChangesAsync();
    }

    public async Task<BloodPressureStatsResponse> GetStatsAsync(int userId, DateTime? from, DateTime? to)
    {
        var query = db.BloodPressureDetails
            .Include(b => b.HealthRecord)
            .Where(b => b.HealthRecord.UserId == userId);
        if (from.HasValue) query = query.Where(b => b.HealthRecord.RecordedAt >= from.Value);
        if (to.HasValue) query = query.Where(b => b.HealthRecord.RecordedAt <= to.Value);

        var records = await query.ToListAsync();
        if (records.Count == 0)
            return new BloodPressureStatsResponse(0, 0, 0, 0, 0, 0, 0, [], 0);

        var dist = records
            .GroupBy(b => ClassifyBp(b.Systolic, b.Diastolic))
            .ToDictionary(g => g.Key, g => g.Count());

        return new BloodPressureStatsResponse(
            Math.Round(records.Average(b => b.Systolic), 1),
            Math.Round(records.Average(b => b.Diastolic), 1),
            Math.Round(records.Average(b => (double)(b.Pulse ?? 0)), 1),
            records.Max(b => b.Systolic),
            records.Min(b => b.Systolic),
            records.Max(b => b.Diastolic),
            records.Min(b => b.Diastolic),
            dist,
            records.Count);
    }

    public async Task<List<BloodPressureChartPoint>> GetChartDataAsync(int userId, string period)
    {
        var cutoff = period switch
        {
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            _ => DateTime.MinValue
        };

        return await db.BloodPressureDetails
            .Include(b => b.HealthRecord)
            .Where(b => b.HealthRecord.UserId == userId && b.HealthRecord.RecordedAt >= cutoff)
            .OrderBy(b => b.HealthRecord.RecordedAt)
            .Select(b => new BloodPressureChartPoint(b.HealthRecord.RecordedAt, b.Systolic, b.Diastolic, b.Pulse))
            .ToListAsync();
    }

    private static string ClassifyBp(int systolic, int diastolic)
    {
        if (systolic >= 180 || diastolic >= 120) return "HypertensiveCrisis";
        if (systolic >= 140 || diastolic >= 90) return "HypertensionStage2";
        if (systolic >= 130 || diastolic >= 80) return "HypertensionStage1";
        if (systolic >= 120 && diastolic < 80) return "Elevated";
        return "Normal";
    }

    private static BloodPressureResponse Map(BloodPressureDetail b) => new(
        b.Id, b.HealthRecordId, b.HealthRecord.RecordedAt,
        b.Systolic, b.Diastolic, b.Pulse, b.MeasurementPosition, b.Arm,
        b.HealthRecord.Source, b.HealthRecord.Note, b.HealthRecord.CreatedAt);
}
