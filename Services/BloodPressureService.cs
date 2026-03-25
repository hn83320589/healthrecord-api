using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.BloodPressure;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class BloodPressureService(AppDbContext db) : IBloodPressureService
{
    public async Task<PagedResult<BloodPressureResponse>> GetListAsync(
        int userId, int page, int pageSize, DateTime? from, DateTime? to)
    {
        var query = db.BloodPressureDetails.Where(b => b.UserId == userId);
        if (from.HasValue) query = query.Where(b => b.RecordedAt >= from.Value);
        if (to.HasValue) query = query.Where(b => b.RecordedAt <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => Map(b))
            .ToListAsync();

        return new PagedResult<BloodPressureResponse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<BloodPressureResponse> GetByIdAsync(int userId, int id)
    {
        var entity = await db.BloodPressureDetails
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId)
            ?? throw new KeyNotFoundException("Record not found.");
        return Map(entity);
    }

    public async Task<BloodPressureResponse> CreateAsync(int userId, CreateBloodPressureRequest request)
    {
        var entity = new BloodPressureDetail
        {
            UserId = userId,
            HealthRecordId = request.HealthRecordId,
            RecordedAt = request.RecordedAt,
            Systolic = request.Systolic,
            Diastolic = request.Diastolic,
            Pulse = request.Pulse,
            MeasurementPosition = request.MeasurementPosition,
            Note = request.Note,
            Source = "manual",
            CreatedAt = DateTime.UtcNow
        };

        db.BloodPressureDetails.Add(entity);
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<BloodPressureResponse> UpdateAsync(int userId, int id, UpdateBloodPressureRequest request)
    {
        var entity = await db.BloodPressureDetails
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId)
            ?? throw new KeyNotFoundException("Record not found.");

        if (entity.Source != "manual")
            throw new InvalidOperationException("Only manual records can be updated.");

        entity.RecordedAt = request.RecordedAt;
        entity.Systolic = request.Systolic;
        entity.Diastolic = request.Diastolic;
        entity.Pulse = request.Pulse;
        entity.MeasurementPosition = request.MeasurementPosition;
        entity.Note = request.Note;

        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var entity = await db.BloodPressureDetails
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId)
            ?? throw new KeyNotFoundException("Record not found.");

        if (entity.Source != "manual")
            throw new InvalidOperationException("Only manual records can be deleted.");

        db.BloodPressureDetails.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<BloodPressureStatsResponse> GetStatsAsync(int userId, DateTime? from, DateTime? to)
    {
        var query = db.BloodPressureDetails.Where(b => b.UserId == userId);
        if (from.HasValue) query = query.Where(b => b.RecordedAt >= from.Value);
        if (to.HasValue) query = query.Where(b => b.RecordedAt <= to.Value);

        var records = await query.ToListAsync();
        if (records.Count == 0)
            return new BloodPressureStatsResponse(0, 0, 0, 0, 0, 0, 0, [], 0);

        var dist = records
            .GroupBy(b => ClassifyBp(b.Systolic, b.Diastolic))
            .ToDictionary(g => g.Key, g => g.Count());

        return new BloodPressureStatsResponse(
            Math.Round(records.Average(b => b.Systolic), 1),
            Math.Round(records.Average(b => b.Diastolic), 1),
            Math.Round(records.Average(b => b.Pulse), 1),
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
            .Where(b => b.UserId == userId && b.RecordedAt >= cutoff)
            .OrderBy(b => b.RecordedAt)
            .Select(b => new BloodPressureChartPoint(b.RecordedAt, b.Systolic, b.Diastolic, b.Pulse))
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
        b.Id, b.HealthRecordId, b.RecordedAt, b.Systolic, b.Diastolic, b.Pulse,
        b.MeasurementPosition, b.Source, b.Note, b.CreatedAt);
}
